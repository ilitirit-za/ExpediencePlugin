using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Expedience.Services;
using Expedience.UI;
using Expedience.Utils;

namespace Expedience
{
	public class Plugin : IDalamudPlugin
	{
		public static string Name => "Expedience";

		private readonly WindowSystem _windowSystem = new("Expedience");
		private readonly MainWindow _mainWindow;
		private readonly ConfigWindow _configWindow;

		private readonly DutyTracker _dutyTracker;
		private readonly DataManager _dataManager;
		private readonly ApiClient _apiClient;
		private readonly LocalDbManager _localDbManager;
		private readonly UserManager _userManager;
		private readonly ChatManager _chatManager;

		private readonly ICommandManager _commandManager;
		private readonly CancellationTokenSource _cts;
		private readonly IDalamudPluginInterface _pluginInterface;

		public Plugin(IDalamudPluginInterface pluginInterface, ICommandManager commandManager)
		{
			Service.Plugin = this;
			_pluginInterface = pluginInterface;
			_pluginInterface.Create<Service>();
			_commandManager = commandManager;
			_localDbManager = new LocalDbManager(new Encryptor());
			_apiClient = new ApiClient(GetApiUrl());

			_userManager = new UserManager(_localDbManager, _apiClient);
			Task.Run(_userManager.TryPopulateUserName);

			_dataManager = new DataManager();
			
			_cts = new CancellationTokenSource();
			Task.Factory.StartNew(() => UploadResults(_cts.Token));

			_mainWindow = new MainWindow(_localDbManager);
			_configWindow = new ConfigWindow(_localDbManager);

			_windowSystem.AddWindow(_mainWindow);
			_windowSystem.AddWindow(_configWindow);

			_chatManager = new ChatManager(pluginInterface, _mainWindow);
			_dutyTracker = new DutyTracker(_chatManager, _dataManager, _localDbManager, _userManager);

			InitializeEventHandlers();
			InitializeCommands(commandManager);
		}

		public string GetVersion() => _dataManager.ClientInfo.PluginVersion;

		private static string GetApiUrl()
		{
			using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Expedience.Resources.ApiUrl.secret");
			using var reader = new StreamReader(stream);
			return reader.ReadToEnd().Trim();
		}

		private void InitializeEventHandlers()
		{
			Service.DutyState.DutyStarted += _dutyTracker.OnDutyStarted;
			Service.DutyState.DutyCompleted += _dutyTracker.OnDutyCompleted;
			Service.ClientState.Login += _userManager.OnLogin;
			Service.ClientState.CfPop += _dutyTracker.OnContentsFinderPop;
			Service.ChatGui.ChatMessage += _dutyTracker.OnChatMessage;
			_pluginInterface.UiBuilder.Draw += OnDraw;
		}

		private void InitializeCommands(ICommandManager commandManager)
		{
			commandManager.AddHandler("/xpd", new(OnCommand) { HelpMessage = "Show Expedience UI" });
			commandManager.AddHandler("/xpdcfg", new(OnCommand) { HelpMessage = "Show Expedience Settings" });
		}

		private void OnCommand(string command, string args)
		{
			if (command == "/xpd")
			{
				_mainWindow.Toggle();
			}
			else if (command == "/xpdcfg")
			{
				_configWindow.Toggle();
			}
		}

		private void OnDraw()
		{
			_windowSystem.Draw();
		}

		private async Task UploadResults(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				await _localDbManager.UploadPendingResults(_apiClient);
				await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
			}
		}

		public void Dispose()
		{
			_cts?.Cancel();
			_cts?.Dispose();

			_windowSystem.RemoveAllWindows();
			_mainWindow?.Dispose();
			_configWindow?.Dispose();
			
			RemoveEventHandlers();
			RemoveCommands();

			_localDbManager.Dispose();
			_chatManager?.Dispose();
		}

		private void RemoveEventHandlers()
		{
			Service.DutyState.DutyStarted -= _dutyTracker.OnDutyStarted;
			Service.DutyState.DutyCompleted -= _dutyTracker.OnDutyCompleted;
			Service.ClientState.Login -= _userManager.OnLogin;
			Service.ClientState.CfPop -= _dutyTracker.OnContentsFinderPop;
			Service.ChatGui.ChatMessage -= _dutyTracker.OnChatMessage;
		}

		private void RemoveCommands()
		{
			_commandManager.RemoveHandler("/xpd");
			_commandManager.RemoveHandler("/xpdcfg");
		}

		internal string GetUserName()
		{
			return _userManager.UserName;
		}
	}
}