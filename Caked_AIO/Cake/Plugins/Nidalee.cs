using LeagueSharp;
using LeagueSharp.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caked_AIO.Plugins
{
    class Nidalee
    {
        private Spell Q, W, E, R, Q2, W2, E2, R2;
        public Nidalee()
        {
            Q = new Spell(SpellSlot.Q, 1500, TargetSelector.DamageType.Magical) { MinHitChance = HitChance.High };
            W = new Spell(SpellSlot.W, 900);
            E = new Spell(SpellSlot.E, 600);
            R = new Spell(SpellSlot.R);
            Q2 = new Spell(SpellSlot.Q);
            W2 = new Spell(SpellSlot.W);
            E2 = new Spell(SpellSlot.E);
            R2 = new Spell(SpellSlot.R);

            Q.SetSkillshot(0.25f, 40, 1300, true, SkillshotType.SkillshotLine);

            Game.OnUpdate += Game_OnUpdate;
        }

        private void Game_OnUpdate(EventArgs args)
        {
            if (!ObjectManager.Player.IsDead)
            {
                if (Orbwalking.CanMove(100))
                {
                    switch (MenuProvider.Orbwalker.ActiveMode)
                    {
                        case Orbwalking.OrbwalkingMode.Combo:
                            {
                                var Target = TargetSelector.GetTargetNoCollision(Q);
                                if (Q.isReadyPerfectly())
                                {
                                    if(Target != null)
                                        CastSkillshot(Target, Q, HitChance.VeryHigh);
                                }
                                break;
                            }
                    }
                }
            }
        }
        public void CastSkillshot(Obj_AI_Hero t, Spell s, HitChance hc = HitChance.High)
        {
            if (!s.IsSkillshot)
                return;

            PredictionOutput p = s.GetPrediction(t);

            if (s.Collision)
            {
                for (int i = 0; i < p.CollisionObjects.Count; i++)
                    if (!p.CollisionObjects[i].IsDead && (p.CollisionObjects[i].IsEnemy || p.CollisionObjects[i].IsMinion))
                        return;
            }

            if ((t.HasBuffOfType(BuffType.Slow) && p.Hitchance >= HitChance.High) || p.Hitchance == HitChance.Immobile)
                s.Cast(p.CastPosition);
            else if (t.IsRecalling())
                s.Cast(t.ServerPosition);
            else
            {
                if (s.IsReady())
                {
                    s.SPredictionCast(t, hc);
                }
            }
        }
    }
}
