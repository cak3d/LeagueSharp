using System;

using LeagueSharp;

namespace Caked_AIO
{
    class PluginLoader
    {
        internal static bool LoadPlugin(string PluginName)
        {
            if (CanLoadPlugin(PluginName))
            {
                DynamicInitializer.NewInstance(Type.GetType("Caked_AIO.Plugins." + ObjectManager.Player.ChampionName));
                return true;
            }

            return false;
        }

        internal static bool CanLoadPlugin(string PluginName)
        {
            return Type.GetType("Caked_AIO.Plugins." + ObjectManager.Player.ChampionName) != null;
        }
    }
}
