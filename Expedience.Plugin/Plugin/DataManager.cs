using System.Collections.Generic;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Expedience.Models;
using System;
using Dalamud.Plugin.Services;

namespace Expedience.Services
{
	public class DataManager
	{
		private ExcelSheet<TerritoryType> _territories;
		private ExcelSheet<LogMessage> _logMessages;
		private List<string> _unrestrictedMessages = new();
		private ClientInfo _clientInfo = new ClientInfo();
		
		public DataManager()
		{
			Initialize();
		}

		private void Initialize()
		{
			_territories = Service.DataManager.GetExcelSheet<TerritoryType>();
			_logMessages = Service.DataManager.GetExcelSheet<LogMessage>();

			_unrestrictedMessages.Add(_logMessages.GetRow(4248).Text);
			_unrestrictedMessages.Add(_logMessages.GetRow(4676).Text);

			InitializeClientInfo();
		}

		private void InitializeClientInfo()
		{
			_clientInfo.PluginVersion = typeof(Plugin).Assembly.GetName().Version.ToString();
			_clientInfo.GameLanguage = Service.ClientState.ClientLanguage.ToString();

			try
			{
				_clientInfo.GameVersion = System.IO.File.ReadAllText("ffxivgame.ver");
			}
			catch (System.Exception ex)
			{
				_clientInfo.GameVersion = "";
				Service.PluginLog.Error(ex, ex.Message);
			}
		}

		public TerritoryType GetTerritory(uint territoryTypeId)
		{
			return _territories.GetRow(territoryTypeId);
		}

		public bool IsUnrestrictedMessage(string message)
		{
			return _unrestrictedMessages.Contains(message);
		}

		public ClientInfo ClientInfo => _clientInfo;

		public static string FormatContentName(LazyRow<ContentFinderCondition> contentFinderCondition)
		{
			var dutyName = contentFinderCondition.Value?.Name.RawString;

			if (string.IsNullOrEmpty(dutyName))
			{
				return string.Empty;
			}

			Span<char> chars = dutyName.ToCharArray();
			if (char.IsLower(chars[0]))
			{
				chars[0] = char.ToUpper(chars[0]);
			}

			return new string(chars);
		}
	}
}