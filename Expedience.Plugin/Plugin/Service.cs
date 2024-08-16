using System;
using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace Expedience
{
    internal class Service
    {
        public static Plugin plugin;

        public static Configuration pluginConfig;


        [PluginService]
        public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

        [PluginService]
        public static IFlyTextGui FlyTextGui { get; private set; } = null!;

        [PluginService]
        public static IToastGui ToastGui { get; private set; } = null!;

        [PluginService]
        public static IClientState ClientState { get; private set; } = null!;

        [PluginService]
        public static IChatGui ChatGui { get; private set; } = null!;

        [PluginService]
        public static ISigScanner SigScanner { get; private set; } = null!;

        [PluginService]
        public static IObjectTable ObjectTable { get; private set; } = null!;

        [PluginService]
        public static IFramework Framework { get; private set; } = null!;

        [PluginService]
        public static IGameGui GameGui { get; private set; } = null!;

        [PluginService]
        public static IGameConfig GameConfig { get; private set; } = null!;

        [PluginService] 
        public static IDataManager DataManager { get; private set; } = null!;

        [PluginService]
        public static IDutyState DutyState { get; private set; } = null!;

        [PluginService]
        public static IPartyList PartyList { get; private set; } = null!;

        [PluginService]
        public static IBuddyList BuddyList { get; private set; } = null!;

		[PluginService]
		public static IPluginLog PluginLog { get; private set; } = null!;
	}
}
