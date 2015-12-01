using System;

using LeagueSharp;
using LeagueSharp.Common;

namespace Caked_AIO
{
    class OrbwalkerTargetIndicator
    {
        internal static void Load()
        {
            MenuProvider.Champion.Drawings.addItem("Draw AutoAttack Target", new Circle(true, System.Drawing.Color.Red), false);

            Drawing.OnDraw += Drawing_OnDraw;

            Console.WriteLine("Caked_AIO: OrbwalkerTargetindicator Loaded.");
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (!ObjectManager.Player.IsDead)
                if (MenuProvider.Champion.Drawings.getCircleValue("Draw AutoAttack Target", false).Active)
                {
                    var OrbwalkerTarget = MenuProvider.Orbwalker.GetTarget();

                    if (OrbwalkerTarget.IsValidTarget())
                        Render.Circle.DrawCircle(OrbwalkerTarget.Position, OrbwalkerTarget.BoundingRadius, MenuProvider.Champion.Drawings.getCircleValue("Draw AutoAttack Target", false).Color);
                }
        }
    }
}
