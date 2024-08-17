using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Expedience.Db;
using Expedience.Db.Models;
using Expedience.Models;
using Expedience.Utils;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Runtime.CompilerServices;

namespace Expedience
{
	public class LocalDbManager : IDisposable
	{
		private readonly SemaphoreSlim _semaphore = new(1, 1);
		private readonly PluginDbContext _dbContext;
		private readonly IEncryptor _encryptor;

		public LocalDbManager(IEncryptor encryptor)
		{
			try
			{
				_dbContext = new PluginDbContext();
				_dbContext.Database.EnsureCreated();
				_encryptor = encryptor;
				Service.PluginLog.Debug("Local Db initialized");
			}
			catch (Exception ex)
			{
				Service.PluginLog.Error(ex, "Error in initializing local db");
				throw;
			}
		}

		public async Task<IQueryable<LocalRecord>> GetLocalRecordsQuery()
		{
			await WaitForSemaphore();
			try
			{
				return _dbContext.LocalRecords.AsQueryable().Where(lr => lr.User == Service.Plugin.GetUserName());
			}
			catch (Exception ex)
			{
				Service.PluginLog.Error(ex, "Error generating local records query");
			}
			finally { ReleaseSemaphore(); }

			return null;
		}

		public async Task SaveResultToLocalDb(DutyCompletionResult completionResult)
		{
			await WaitForSemaphore();

			try
			{
				completionResult.UploadId = Guid.NewGuid();
				var payload = JsonSerializer.Serialize(completionResult);
				var encryptedPayload = _encryptor.Encrypt(payload);

				_dbContext.LocalRecords.Add(new LocalRecord
				{
					TerritoryId = completionResult.DutyInfo.TerritoryId,
					PlaceName = completionResult.DutyInfo.PlaceName,
					ContentName = completionResult.DutyInfo.ContentName,
					Duration = completionResult.CompletionTimeInfo.CompletionTimeText,
					CompletionDate = completionResult.DutyCompletionDateUtc,
					HasEcho = completionResult.DutyInfo.HasEcho,
					IsMinILevel = completionResult.DutyInfo.IsMinILevel,
					IsUnrestricted = completionResult.DutyInfo.IsUnrestricted,
					HasNpcMembers = completionResult.DutyInfo.IsNpcSupported,
					IsUploaded = false,
					PluginVersion = completionResult.ClientInfo.PluginVersion,
					Payload = encryptedPayload,
					User = completionResult.UserInfo.UserName,
				});

				await _dbContext.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				Service.PluginLog.Error(ex, $"Error occurred in SaveResultToLocalDb: {ex.Message}");
				throw;
			}
			finally
			{
				ReleaseSemaphore();
			}
		}

		public async Task MarkRecordsAsUploaded(List<LocalRecord> records)
		{
			await WaitForSemaphore();
			try
			{
				foreach (var record in records)
				{
					record.IsUploaded = true;
				}

				await _dbContext.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				Service.PluginLog.Error(ex, $"Error occurred in MarkRecordsAsUploaded: {ex.Message}");
				throw;
			}
			finally
			{
				ReleaseSemaphore();
			}
		}

		public async Task UploadPendingResults(ApiClient apiClient)
		{
			await WaitForSemaphore();
			try
			{
				var records = await _dbContext.LocalRecords.Where(l => !l.IsUploaded).ToListAsync();
				var resultsToUpload = records.Select(r => r.Payload).ToList();

				if (resultsToUpload.Any())
				{
					await apiClient.UploadDutyCompletionResults(resultsToUpload);
					await MarkRecordsAsUploaded(records);
				}
			}
			catch (Exception ex)
			{
				Service.PluginLog.Error(ex, $"Error occurred in UploadPendingResults: {ex.Message}");
			}
			finally
			{
				ReleaseSemaphore();
			}
		}

		public async Task SaveLocalUser(int worldId, string userHash, string userName)
		{
			await WaitForSemaphore();
			try
			{
				_dbContext.LocalUsers.Add(new LocalUser
				{
					WorldId = worldId,
					UserName = userName,
					UserHash = userHash
				});

				await _dbContext.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				Service.PluginLog.Error(ex, $"Error occurred in SaveLocalUser: {ex.Message}");
				throw;
			}
			finally
			{
				ReleaseSemaphore();
			}
		}

		public async Task<LocalUser> GetLocalUser(uint worldId, string userHash)
		{
			await WaitForSemaphore();
			try
			{
				Service.PluginLog.Info($"Retrieving Local User Name for world {worldId} and hash {userHash}");
				var localUser = await _dbContext.LocalUsers
					.FirstOrDefaultAsync(u => u.WorldId == worldId && u.UserHash == userHash);

				Service.PluginLog.Info($"Retrieved Local User {localUser?.UserName}");

				return localUser;
			}
			finally
			{
				ReleaseSemaphore();
			}
		}

		private int _awaitCount = 0;
		public async Task WaitForSemaphore([CallerMemberName] string callerName = "")
		{
			_awaitCount++;
			Service.PluginLog.Info($"Semaphore awaited by {callerName}. Wait count {_awaitCount}");
			await _semaphore.WaitAsync().ConfigureAwait(false);
		}

		public void ReleaseSemaphore([CallerMemberName] string callerName = "")
		{
			_awaitCount--;
			Service.PluginLog.Info($"Semaphore Released by {callerName}. Wait count {_awaitCount}");
			_semaphore.Release();
		}

		public void Dispose()
		{
			_dbContext?.Dispose();
			_semaphore?.Dispose();
		}
	}
}