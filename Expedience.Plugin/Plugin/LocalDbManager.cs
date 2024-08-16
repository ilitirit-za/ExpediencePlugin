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

namespace Expedience
{
	public class LocalDbManager : IDisposable
	{
		private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
		private readonly PluginDbContext _dbContext;
		private readonly IEncryptor _encryptor;

		public LocalDbManager()
		{
			try
			{
				_dbContext = new PluginDbContext();
				_dbContext.Database.EnsureCreated();
				_encryptor = new Encryptor();
				Service.PluginLog.Debug("Local Db initialized");
			}
			catch (Exception ex)
			{
				Service.PluginLog.Error(ex, "Error in initializing local db");
				throw;
			}
		}

		public IQueryable<LocalRecord> GetLocalRecordsQuery() => _dbContext.LocalRecords.AsQueryable();

		public async Task SaveResultToLocalDb(DutyCompletionResult completionResult)
		{
			await _semaphore.WaitAsync();
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
				_semaphore.Release();
			}
		}

		public async Task<List<LocalRecord>> GetUnuploadedRecords()
		{
			return await _dbContext.LocalRecords.Where(l => !l.IsUploaded).ToListAsync();
		}

		public async Task MarkRecordsAsUploaded(List<LocalRecord> records)
		{
			await _semaphore.WaitAsync();
			try
			{
				foreach (var record in records)
				{
					record.IsUploaded = true;
				}
				await _dbContext.SaveChangesAsync();
			}
			finally
			{
				_semaphore.Release();
			}
		}

		public async Task UploadPendingResults(ApiClient apiClient)
		{
			try
			{
				var records = await GetUnuploadedRecords();
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
		}

		public async Task SaveLocalUser(int worldId, string userHash, string userName)
		{
			await _semaphore.WaitAsync();
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
				_semaphore.Release();
			}
		}

		public async Task<LocalUser> GetLocalUser(uint worldId, string userHash)
		{
			return await _dbContext.LocalUsers
				.FirstOrDefaultAsync(u => u.WorldId == worldId && u.UserHash == userHash);
		}

		public void Dispose()
		{
			_dbContext?.Dispose();
			_semaphore?.Dispose();
		}

	}
}