using System;
using Dalamud.IoC;
using Dalamud.Plugin.Services;

namespace Expedience
{
    internal class Service
    {
        public static Plugin Plugin { get; set; }

        [PluginService]
        public static IClientState ClientState { get; private set; } = null!;

        [PluginService]
        public static IChatGui ChatGui { get; private set; } = null!;

        [PluginService]
        public static IObjectTable ObjectTable { get; private set; } = null!;

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
