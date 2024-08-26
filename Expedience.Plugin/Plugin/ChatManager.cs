using System;
using System.Collections.Generic;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin;
using Expedience.UI;

namespace Expedience
{
	public class ChatManager : IDisposable
	{
		private readonly IDalamudPluginInterface _pluginInterface;
		private readonly MainWindow _mainWindow;

		public ChatManager(IDalamudPluginInterface pluginInterface, MainWindow mainWindow)
		{
			_pluginInterface = pluginInterface;
			_mainWindow = mainWindow;
		}

		private readonly Dictionary<string, DalamudLinkPayload> _dutyLinkMap = new();
		private readonly Dictionary<uint, string> _reverseDutyLinkMap = new();
		private uint _nextCommandId = 57300;

		public void PrintDutyCompletionTime(string dutyName, TimeSpan time)
		{
			if (!_dutyLinkMap.TryGetValue(dutyName, out var linkPayload))
			{
				uint commandId = _nextCommandId++;
				linkPayload = _pluginInterface.AddChatLinkHandler(commandId, OnChatLinkClick);
				_dutyLinkMap.Add(dutyName, linkPayload);
				_reverseDutyLinkMap.Add(commandId, dutyName);
			}

			var message = new SeString(new Payload[]
			{
				new TextPayload("Expedience: "),
				linkPayload,
				new UIForegroundPayload(710), // Yellowish or something
				new TextPayload($"{dutyName}"),
				new UIForegroundPayload(0),
				RawPayload.LinkTerminator,
				new TextPayload($" ended. Duration: "),
				new UIForegroundPayload(555), // Purple
				new TextPayload($"{time:hh\\:mm\\:ss\\.fff}"),
			});

			Service.ChatGui.Print(message);
		}

		public static void PrintMessage(string message) => Service.ChatGui.Print(message);

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
			_pluginInterface.RemoveChatLinkHandler();
		}
	}
}
