using System;

using LeagueSharp;

namespace Caked_AIO
{
    class Initializer
    {
        internal static void Initialize()
        {
            Console.WriteLine("Caked_AIO: HelloWorld!");

            MenuProvider.initialize();

            if (PluginLoader.LoadPlugin(ObjectManager.Player.ChampionName))
            {
                MenuProvider.Champion.Drawings.addItem(" ");
                OrbwalkerTargetIndicator.Load();
                LasthitIndicator.Load();
                Activator.Load();
            }

            AutoQuit.Load();

            Console.WriteLine("SharpShooter: Initialized.");
        }
    }
}
