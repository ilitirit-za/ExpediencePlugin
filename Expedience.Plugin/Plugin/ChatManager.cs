using System;
using System.Collections.Generic;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Expedience.UI;

namespace Expedience
{
	public class ChatManager : IDisposable
	{
		private readonly IDalamudPluginInterface pluginInterface;
		private ICommandManager commandManager;
		private readonly IChatGui chatGui;
		private readonly MainWindow _mainWindow;

		public ChatManager(IDalamudPluginInterface pluginInterface, 
			ICommandManager commandManager,
			IChatGui chatGui,
			MainWindow mainWindow)
		{
			this.pluginInterface = pluginInterface;
			this.commandManager = commandManager;
			this.chatGui = chatGui;
			this._mainWindow = mainWindow;
		}

		private readonly Dictionary<string, DalamudLinkPayload> _dutyLinkMap = new Dictionary<string, DalamudLinkPayload>();
		private readonly Dictionary<uint, string> _reverseDutyLinkMap = new Dictionary<uint, string>();
		private uint _nextCommandId = 57300;

		public void PrintDutyCompletionTime(string dutyName, TimeSpan time)
		{
			if (!_dutyLinkMap.TryGetValue(dutyName, out var linkPayload))
			{
				uint commandId = _nextCommandId++;
				linkPayload = pluginInterface.AddChatLinkHandler(commandId, OnChatLinkClick);
				_dutyLinkMap.Add(dutyName, linkPayload);
				_reverseDutyLinkMap.Add(commandId, dutyName);
			}

			var message = new SeString(new Payload[]
			{
				new TextPayload("Expedience: "),
				linkPayload,
				new UIForegroundPayload((ushort)710),
				new TextPayload($"{dutyName}"),
				new UIForegroundPayload(0),
				RawPayload.LinkTerminator,
				new TextPayload($" ended. Duration: "),
				new UIForegroundPayload((ushort)555),
				new TextPayload($"{time.ToString(@"hh\:mm\:ss\.fff")}"),
			});

			chatGui.Print(message);
		}

		public void PrintMessage(string message)
		{
			chatGui.Print(message);
		}

		private void OnChatLinkClick(uint commandId, SeString @string)
		{
			if (_reverseDutyLinkMap.TryGetValue(commandId, out var dutyName))
			{
				_mainWindow.RetrieveDutyRecords(dutyName, changeSearchText: true);
				if (_mainWindow.IsOpen == false)
				{
					_mainWindow.Toggle();
				}
			}
		}

		public void Dispose()
		{
			pluginInterface.RemoveChatLinkHandler();
		}
	}
}
