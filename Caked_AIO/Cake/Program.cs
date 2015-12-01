using System;

using LeagueSharp.Common;

namespace Caked_AIO
{
    class Program
    {
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Initializer.Initialize();
        }
    }
}