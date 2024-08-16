using System;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using Expedience.Models;
using Murmur;

namespace Expedience
{
	public class UserManager
	{
		private readonly LocalDbManager _localDbManager;
		private readonly ApiClient _apiClient;
		
		private string _userName;
		private UserInfo _userInfo;
		private readonly IClientState _clientState;

		public string UserName => _userName;
		public UserManager(LocalDbManager localDbManager, ApiClient apiClient, IClientState clientState)
		{
			_localDbManager = localDbManager;
			_apiClient = apiClient;
			_clientState = clientState;
		}

		internal void OnLogin()
		{
			_userInfo = null;
			Task.Factory.StartNew(() => TryPopulateUserName());
		}

		private async Task<bool> TryPopulateUserName()
		{
			try
			{
				var playerName = _clientState.LocalPlayer.Name.TextValue;
				var worldId = _clientState.LocalPlayer.HomeWorld.GameData.RowId;
				var userHash = GetHash($"{worldId}-{playerName}");
				var localUser = await _localDbManager.GetLocalUser(worldId, userHash);

				if (localUser != null)
				{
					_userName = localUser.UserName;
					return true;
				}

				_userName = await _apiClient.GetUserName((int)worldId, userHash);

				if (String.IsNullOrWhiteSpace(_userName) == false)
				{
					await Task.Run(() => _localDbManager.SaveLocalUser((int)worldId, userHash, _userName));
					return true;
				}

				return false;
			}
			catch (Exception ex)
			{
				Service.PluginLog.Error(ex, "Error trying to populate the user name: {errorMessage}", ex.Message);
			}

			return false;
		}

		public UserInfo GetUserInfo()
		{
			try
			{
				if (_userInfo == null)
				{
					var playerName = _clientState.LocalPlayer.Name.TextValue;
					var homeWorld = _clientState.LocalPlayer.HomeWorld.GameData;
					_userInfo = new UserInfo
					{
						UserId = GetHash($"{homeWorld.RowId}-{playerName}"),
						WorldId = (int)homeWorld.RowId,
						WorldName = homeWorld.Name,
						UserName = playerName,
					};
				}

				return _userInfo;
			}
			catch (Exception ex)
			{
				Service.PluginLog.Error(ex, $"Error occurred in GetUserInfo: {ex.Message}");
			}

			return new UserInfo();
		}

		private static string GetHash(string text)
		{
			var inputBytes = Encoding.UTF8.GetBytes(text);
			var hashAlgorithm = MurmurHash.Create128(managed: false);
			var outputBytes = hashAlgorithm.ComputeHash(inputBytes);
			return BitConverter.ToString(outputBytes).Replace("-", "");
		}
	}
}
