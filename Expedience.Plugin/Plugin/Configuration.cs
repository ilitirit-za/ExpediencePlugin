using System;
using Dalamud.Configuration;
using Dalamud.Plugin;


namespace Expedience
{

    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        private static int VersionLatest = 1;

        public int Version { get; set; } = VersionLatest;

        [NonSerialized]
        private IDalamudPluginInterface pluginInterface;

        public void Initialize(IDalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;

            switch (Version)
            {
                case 0:
                    break;

                default: break;
            }
        }

        public void Save()
        {
            pluginInterface.SavePluginConfig(this);
        }
    }
}
