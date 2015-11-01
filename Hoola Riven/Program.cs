using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;
using ItemData = LeagueSharp.Common.Data.ItemData;

namespace HoolaRiven
{
    public class Program
    {
        static Menu Menu;
        static Orbwalking.Orbwalker Orbwalker;
        static Obj_AI_Hero Player = ObjectManager.Player;
        static HpBarIndicator Indicator = new HpBarIndicator();
        static string IsFirstR = "RivenFengShuiEngine";
        static string IsSecondR = "rivenizunablade";
        static SpellSlot Flash = Player.GetSpellSlot("summonerFlash");
        static Spell Q, Q1, W, E, R;
        static int QStack = 1;
        static bool forceQ;
        static float lastQ;
        static AttackableUnit QTarget = null;
        static bool Dind { get { return Menu.Item("Dind").GetValue<bool>(); } }
        static bool DrawCB { get { return Menu.Item("DrawCB").GetValue<bool>(); } }
        static bool KillstealW { get { return Menu.Item("killstealw").GetValue<bool>(); } }
        static bool KillstealR { get { return Menu.Item("killstealr").GetValue<bool>(); } }
        static bool DrawAlwaysR { get { return Menu.Item("DrawAlwaysR").GetValue<bool>(); } }
        static bool DrawUseHoola { get { return Menu.Item("DrawUseHoola").GetValue<bool>(); } }
        static bool DrawFH { get { return Menu.Item("DrawFH").GetValue<bool>(); } }
        static bool DrawHS { get { return Menu.Item("DrawHS").GetValue<bool>(); } }
        static bool DrawBT { get { return Menu.Item("DrawBT").GetValue<bool>(); } }
        static bool UseHoola { get { return Menu.Item("UseHoola").GetValue<KeyBind>().Active; } }
        static bool AlwaysR { get { return Menu.Item("AlwaysR").GetValue<KeyBind>().Active; } }
        static bool AutoShield { get { return Menu.Item("AutoShield").GetValue<bool>(); } }
        static bool Shield { get { return Menu.Item("Shield").GetValue<bool>(); } }
        static bool KeepQ { get { return Menu.Item("KeepQ").GetValue<bool>(); } }
        static int QD { get { return Menu.Item("QD").GetValue<Slider>().Value; } }
        static int QLD { get { return Menu.Item("QLD").GetValue<Slider>().Value; } }
        static int AutoW { get { return Menu.Item("AutoW").GetValue<Slider>().Value; } }
        static bool ComboW { get { return Menu.Item("ComboW").GetValue<bool>(); } }
        static bool RMaxDam { get { return Menu.Item("RMaxDam").GetValue<bool>(); } }
        static bool RKillable { get { return Menu.Item("RKillable").GetValue<bool>(); } }
        static int LaneW { get { return Menu.Item("LaneW").GetValue<Slider>().Value; } }
        static bool LaneE { get { return Menu.Item("LaneE").GetValue<bool>(); } }
        static bool WInterrupt { get { return Menu.Item("WInterrupt").GetValue<bool>(); } }
        static bool Qstrange { get { return Menu.Item("Qstrange").GetValue<bool>(); } }
        static bool FirstHydra { get { return Menu.Item("FirstHydra").GetValue<bool>(); } }
        static bool LaneQ { get { return Menu.Item("LaneQ").GetValue<bool>(); } }
        static bool Youmu { get { return Menu.Item("youmu").GetValue<bool>(); } }
        

        static void Main(string[] args) { CustomEvents.Game.OnGameLoad += OnGameLoad; }

        static void OnGameLoad(EventArgs args)
        {

            if (Player.ChampionName != "Riven") return;
            Game.PrintChat("Hoola Riven - Loaded Successfully, Good Luck! :):) Remove E");
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 300);
            R = new Spell(SpellSlot.R, 900);
            R.SetSkillshot(0.25f, 45, 1600, false, SkillshotType.SkillshotCone);

            OnMenuLoad();

            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Drawing.OnEndScene += Drawing_OnEndScene;
            Obj_AI_Base.OnProcessSpellCast += OnCast;
            Obj_AI_Base.OnDoCast += OnDoCast;
            Obj_AI_Base.OnDoCast += OnDoCastLC;
            Obj_AI_Base.OnPlayAnimation += OnPlay;
            Obj_AI_Base.OnProcessSpellCast += OnCasting;
            Interrupter2.OnInterruptableTarget += interrupt;
        }
        
        static void Drawing_OnEndScene(EventArgs args)
        {
            foreach (
                var enemy in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(ene => ene.IsValidTarget() && !ene.IsZombie))
            {
                if (Dind)
                {
                    Indicator.unit = enemy;
                    Indicator.drawDmg(getComboDamage(enemy), new SharpDX.ColorBGRA(255, 204, 0, 170));
                }

            }
        }

        static void OnDoCastLC(Obj_AI_Base Sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!Sender.IsMe || !Orbwalking.IsAutoAttack((args.SData.Name))) return;
            QTarget = (Obj_AI_Base) args.Target;
            if (args.Target is Obj_AI_Minion)
            {
                var target = (Obj_AI_Base) args.Target;
                if (target.IsValid)
                {
                    if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
                    {
                        var Minions = MinionManager.GetMinions(70 + 120 + Player.BoundingRadius);
                        if (Q.IsReady() && LaneQ && Minions[0].IsValid)
                        {
                            UseCastItem(300);
                            forcecastQ(Minions[0]);
                        }
                        if ((!Q.IsReady() || (Q.IsReady() && !LaneQ)) && W.IsReady() && LaneW != 0 &&
                            Minions.Count >= LaneW)
                        {
                            UseCastItem(300);
                            Utility.DelayAction.Add(1, () => UseW(500));
                        }
                    }
                }
            }
        }

        static void OnDoCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            var spellName = args.SData.Name;
            if (!sender.IsMe || !Orbwalking.IsAutoAttack(spellName)) return;
            QTarget = (Obj_AI_Base)args.Target;

            if (args.Target is Obj_AI_Minion)
            {
                var target = (Obj_AI_Base) args.Target;
                if (target.IsValid)
                {
                    if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
                    {
                        var Mobs = MinionManager.GetMinions(120 + 70 + Player.BoundingRadius, MinionTypes.All,
                            MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
                        if (Mobs[0].IsValid && Mobs.Count != 0)
                        {
                            if (Q.IsReady())
                            {
                                UseCastItem(300);
                                forcecastQ(Mobs[0]);
                            }
                            else if (W.IsReady())
                            {
                                UseCastItem(300);
                                Utility.DelayAction.Add(1, () => UseW(500));
                            }
                        }
                    }
                }
            }
            if (args.Target is Obj_AI_Turret || args.Target is Obj_Barracks || args.Target is Obj_BarracksDampener || args.Target is Obj_Building) if (args.Target.IsValid && args.Target != null && Q.IsReady() && LaneQ && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear) forcecastQ((Obj_AI_Base)args.Target);
            if (args.Target is Obj_AI_Hero)
            {
                var target = (Obj_AI_Hero)args.Target;
                if (target.IsValid && target != null)
                {
                    if (KillstealR && R.IsReady() && R.Instance.Name == IsSecondR) if (target.Health < (Rdame(target, target.Health) + Player.GetAutoAttackDamage2(target)) && target.Health > Player.GetAutoAttackDamage2(target)) R.Cast(target.Position);
                    if (KillstealW && W.IsReady()) if (target.Health < (W.GetDamage2(target) + Player.GetAutoAttackDamage2(target)) && target.Health > Player.GetAutoAttackDamage2(target)) W.Cast();
                    if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                    {
                        if (Q.IsReady())
                        {
                            UseCastItem(200);
                            forcecastQ(target);
                        }
                        else if (W.IsReady() && InWRange(target))
                        {
                            UseCastItem(200);
                            Utility.DelayAction.Add(1, () => UseW(500));
                        }
                    }
                    if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.FastHarass)
                    {
                        if (W.IsReady() && InWRange(target))
                        {
                            UseCastItem(200);
                            Utility.DelayAction.Add(1, () => UseW(500));
                            Utility.DelayAction.Add(2, () => forcecastQ(target));
                        }
                        else if (Q.IsReady())
                        {
                            UseCastItem(200);
                            forcecastQ(QTarget);
                        }
                    }

                    if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                    {
                        if (QStack == 2 && Q.IsReady())
                        {
                            UseCastItem(200);
                            forcecastQ(QTarget);
                        }
                    }

                    if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Burst)
                    {
                        if (R.IsReady() && R.Instance.Name == IsSecondR)
                        {
                            UseCastItem(500);
                            UseR(500);
                        }
                        else if (Q.IsReady())
                        {
                            UseCastItem(200);
                            forcecastQ(QTarget);
                        }
                    }
                }
            }
        }
        static void OnMenuLoad()
        {
            Menu = new Menu("Hoola Riven", "hoolariven", true);
            Menu ts = Menu.AddSubMenu(new Menu("Target Selector", "Target Selector"));
            TargetSelector.AddToMenu(ts);
            var orbwalker = new Menu("Orbwalk", "rorb");
            Orbwalker = new Orbwalking.Orbwalker(orbwalker);
            Menu.AddSubMenu(orbwalker);
            var Combo = new Menu("Combo", "Combo");

            Combo.AddItem(new MenuItem("AlwaysR", "Always Use R (Toggle)").SetValue(new KeyBind('G', KeyBindType.Toggle)));
            Combo.AddItem(new MenuItem("UseHoola", "Use Hoola Combo Logic (Toggle)").SetValue(new KeyBind('L', KeyBindType.Toggle)));
            Combo.AddItem(new MenuItem("ComboW", "Always use W").SetValue(true));
            Combo.AddItem(new MenuItem("RKillable", "Use R When Target Can Killable").SetValue(true));


            Menu.AddSubMenu(Combo);
            var Lane = new Menu("Lane", "Lane");
            Lane.AddItem(new MenuItem("LaneQ", "Use Q While Laneclear").SetValue(true));
            Lane.AddItem(new MenuItem("LaneW", "Use W X Minion (0 = Don't)").SetValue(new Slider(5, 0, 5)));
            Lane.AddItem(new MenuItem("LaneE", "Use E While Laneclear").SetValue(true));



            Menu.AddSubMenu(Lane);
            var Misc = new Menu("Misc", "Misc");

            Misc.AddItem(new MenuItem("youmu", "Use Youmus When E").SetValue(false));
            Misc.AddItem(new MenuItem("FirstHydra", "Flash Burst Hydra Cast before W").SetValue(false));
            Misc.AddItem(new MenuItem("Qstrange", "Strange Q For Speed").SetValue(false));
            Misc.AddItem(new MenuItem("Winterrupt", "W interrupt").SetValue(true));
            Misc.AddItem(new MenuItem("AutoW", "Auto W When x Enemy").SetValue(new Slider(5, 0, 5)));
            Misc.AddItem(new MenuItem("RMaxDam", "Use Second R Max Damage").SetValue(true));
            Misc.AddItem(new MenuItem("killstealw", "Killsteal W").SetValue(true));
            Misc.AddItem(new MenuItem("killstealr", "Killsteal Second R").SetValue(true));
            Misc.AddItem(new MenuItem("AutoShield", "Auto Cast E").SetValue(true));
            Misc.AddItem(new MenuItem("Shield", "Auto Cast E While LastHit").SetValue(true));
            Misc.AddItem(new MenuItem("KeepQ", "Keep Q Alive").SetValue(true));
            Misc.AddItem(new MenuItem("QD", "First,Second Q Delay").SetValue(new Slider(29, 23, 43)));
            Misc.AddItem(new MenuItem("QLD", "Third Q Delay").SetValue(new Slider(39, 36, 53)));


            Menu.AddSubMenu(Misc);

            var Draw = new Menu("Draw", "Draw");

            Draw.AddItem(new MenuItem("DrawAlwaysR", "Draw Always R Status").SetValue(true));
            Draw.AddItem(new MenuItem("DrawUseHoola", "Draw Hoola Logic Status").SetValue(true));
            Draw.AddItem(new MenuItem("Dind", "Draw Damage Indicator").SetValue(true));
            Draw.AddItem(new MenuItem("DrawCB", "Draw Combo Engage Range").SetValue(true));
            Draw.AddItem(new MenuItem("DrawBT", "Draw Burst Engage Range").SetValue(true));
            Draw.AddItem(new MenuItem("DrawFH", "Draw FastHarass Engage Range").SetValue(true));
            Draw.AddItem(new MenuItem("DrawHS", "Draw Harass Engage Range").SetValue(true));

            Menu.AddSubMenu(Draw);

            var Credit = new Menu("Credit", "Credit");

            Credit.AddItem(new MenuItem("hoola", "Made by Hoola :)"));
            Credit.AddItem(new MenuItem("notfixe", "If High ping will be many buggy"));
            Credit.AddItem(new MenuItem("notfixed", "Not Fixed Anything Yet"));
            Credit.AddItem(new MenuItem("feedback", "So Feedback To Hoola!"));

            Menu.AddSubMenu(Credit);

            Menu.AddToMainMenu();
        }

        static void interrupt(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (sender.IsEnemy && W.IsReady() && sender.IsValidTarget() && !sender.IsZombie && WInterrupt)
            {
                if (sender.IsValidTarget(125 + Player.BoundingRadius + sender.BoundingRadius)) W.Cast();
            }
        }

        static void AutoUseW()
        {
            if (AutoW > 0)
            {
                float wrange = 0;
                if (Player.HasBuff("RivenFengShuiEngine"))
                {
                    wrange = 195 + Player.BoundingRadius + 70;
                    if (Player.CountEnemiesInRange(wrange) >= AutoW)
                    {
                        W.Cast();
                    }
                }
                else
                {
                    wrange = 120 + Player.BoundingRadius + 70;
                    if (Player.CountEnemiesInRange(wrange) >= AutoW)
                    {
                        W.Cast();
                    }
                }
            }
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            statereset();
            UseRMaxDam();
            AutoUseW();
            killsteal();
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo) Combo();
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear) Jungleclear();
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed) Harass();
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.FastHarass) FastHarass();
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Burst) Burst();
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Flee) Flee();
            if (Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Flee) Orbwalker.SetAttack(true);
        }

        static void killsteal()
        {
            if (KillstealW && W.IsReady())
            {
                var targets = HeroManager.Enemies.Where(x => x.IsValidTarget(R.Range) && !x.IsZombie);
                foreach (var target in targets)
                {
                    if (target.Health < W.GetDamage2(target) && InWRange(target))
                        W.Cast();
                }
            }
            if (KillstealR && R.IsReady() && R.Instance.Name == IsSecondR)
            {
                var targets = HeroManager.Enemies.Where(x => x.IsValidTarget(R.Range) && !x.IsZombie);
                foreach (var target in targets)
                {
                    if (target.Health < Rdame(target, target.Health) && (!target.HasBuff("kindrednodeathbuff") && !target.HasBuff("Undying Rage") && !target.HasBuff("JudicatorIntervention")))
                        R.Cast(target.Position);
                }
            }
        }
        static void UseRMaxDam()
        {
            if (RMaxDam && R.IsReady() && R.Instance.Name == IsSecondR)
            {
                var targets = HeroManager.Enemies.Where(x => x.IsValidTarget(R.Range) && !x.IsZombie);
                foreach (var target in targets)
                {
                    if (target.Health / target.MaxHealth <= 0.25 && (!target.HasBuff("kindrednodeathbuff") || !target.HasBuff("Undying Rage") || !target.HasBuff("JudicatorIntervention")))
                        R.Cast(target.Position);
                }
            }
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            if (Player.IsDead)
                return;
            var heropos = Drawing.WorldToScreen(ObjectManager.Player.Position);

            if (DrawCB) Render.Circle.DrawCircle(Player.Position, 250 + Player.AttackRange + 70, E.IsReady() ? Color.FromArgb(120, 0, 170, 255) : Color.IndianRed);
            if (DrawBT && Flash != SpellSlot.Unknown) Render.Circle.DrawCircle(Player.Position, 850, R.IsReady() && Flash.IsReady() ? Color.FromArgb(120, 0, 170, 255) : Color.IndianRed);
            if (DrawFH) Render.Circle.DrawCircle(Player.Position, 450 + Player.AttackRange + 70, E.IsReady() && Q.IsReady() ? Color.FromArgb(120, 0, 170, 255) : Color.IndianRed);
            if (DrawHS) Render.Circle.DrawCircle(Player.Position, 400, Q.IsReady() && W.IsReady() ? Color.FromArgb(120, 0, 170, 255) : Color.IndianRed);
            if (DrawAlwaysR) Drawing.DrawText(heropos.X, heropos.Y + 20, Color.Cyan, AlwaysR ? "Always R On" : "Always R Off");
            if (DrawUseHoola) Drawing.DrawText(heropos.X, heropos.Y + 50, Color.Cyan, UseHoola ? "Hoola Logic On" : "Hoola Logic Off");
        }

        static void Jungleclear()
        {

            var Mobs = MinionManager.GetMinions(250 + Player.AttackRange + 70, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (Mobs.Count <= 0)
                return;

            if (W.IsReady() && E.IsReady() && !Orbwalking.InAutoAttackRange(Mobs[0]))
            {
                Utility.DelayAction.Add(1, () => UseCastItem(200));
                Utility.DelayAction.Add(2, () => UseW(500));
            }
        }

        static void Combo()
        {
            var targetR = TargetSelector.GetTarget(250 + Player.AttackRange + 70, TargetSelector.DamageType.Physical);
            if (R.IsReady() && R.Instance.Name == IsFirstR && Orbwalker.InAutoAttackRange(targetR) && AlwaysR && targetR != null) R.Cast();
            if (R.IsReady() && R.Instance.Name == IsFirstR && W.IsReady() && InWRange(targetR) && ComboW && AlwaysR && targetR != null)
            {
                R.Cast();
                UseW(200);
            }
            if (W.IsReady() && InWRange(targetR) && ComboW && targetR != null) W.Cast();
            if (UseHoola && R.IsReady() && R.Instance.Name == IsFirstR && W.IsReady() && targetR != null && E.IsReady() && targetR.IsValidTarget() && !targetR.IsZombie && (IsKillableR(targetR) || AlwaysR))
            {
                if (!InWRange(targetR))
                {
                    R.Cast();
                    Utility.DelayAction.Add(170, () => UseW(270));
                    Utility.DelayAction.Add(280, () => forcecastQ(targetR));
                }
            }
            else if (!UseHoola && R.IsReady() && R.Instance.Name == IsFirstR && W.IsReady() && targetR != null && E.IsReady() && targetR.IsValidTarget() && !targetR.IsZombie && (IsKillableR(targetR) || AlwaysR))
            {
                if (!InWRange(targetR))
                {
                    R.Cast();
                    Utility.DelayAction.Add(220, () => UseW(200));
                }
            }
            else if (UseHoola && W.IsReady() && E.IsReady())
            {
                if (targetR.IsValidTarget() && targetR != null && !targetR.IsZombie && !InWRange(targetR))
                {
                    Utility.DelayAction.Add(10, () => UseCastItem(200));
                    Utility.DelayAction.Add(220, () => UseW(200));
                    Utility.DelayAction.Add(280, () => forcecastQ(targetR));
                }
            }
            else if (!UseHoola && W.IsReady() && targetR != null && E.IsReady())
            {
                if (targetR.IsValidTarget() && targetR != null && !targetR.IsZombie && !InWRange(targetR))
                {
                    Utility.DelayAction.Add(10, () => UseCastItem(200));
                    Utility.DelayAction.Add(220, () => UseW(200));
                }
            }
        }

        static void Burst()
        {
            var target = TargetSelector.GetSelectedTarget();
            if (target != null && target.IsValidTarget() && !target.IsZombie)
            {
                if (Flash != SpellSlot.Unknown && Flash.IsReady()
                    && R.IsReady() && R.Instance.Name == IsFirstR && (Player.Distance(target.Position) <= 850 && Player.Distance(target.Position) >= 400 + Player.AttackRange + 70) && (!FirstHydra || (FirstHydra && !HasItem())))
                {
                    CastYoumoo();
                    R.Cast();
                    Utility.DelayAction.Add(180, () => FlashW());
                }
                else if (Flash != SpellSlot.Unknown && Flash.IsReady()
                    && R.IsReady() && R.Instance.Name == IsFirstR && (Player.Distance(target.Position) <= 850 && Player.Distance(target.Position) >= 400 + Player.AttackRange + 70) && FirstHydra && HasItem())
                {
                    R.Cast();
                    UseCastItem(500);
                    Utility.DelayAction.Add(280, () => FlashW());
                }
            }
        }

        static void FastHarass()
        {
            if (Q.IsReady())
            {
                var target = TargetSelector.GetTarget(450 + Player.AttackRange + 70, TargetSelector.DamageType.Physical);
                if (target.IsValidTarget() && !target.IsZombie)
                {
                    if (!Orbwalking.InAutoAttackRange(target) && !InWRange(target));
                    Utility.DelayAction.Add(10, () => UseCastItem(200));
                    Utility.DelayAction.Add(170, () => Q.Cast(target.ServerPosition));
                }
            }
        }

        static void Harass()
        {
            var target = TargetSelector.GetTarget(400, TargetSelector.DamageType.Physical);
            if (Q.IsReady() && W.IsReady() && QStack == 1)
            {
                if (target.IsValidTarget() && !target.IsZombie)
                {
                    Q.Cast(target.ServerPosition);
                    UseW(1000);
                }
            }
            if (Q.IsReady()&& QStack == 3 && !Orbwalking.CanAttack() && Orbwalking.CanMove(10))
            {
                var epos = Player.ServerPosition +
                          (Player.ServerPosition - target.ServerPosition).Normalized() * 300;
                Utility.DelayAction.Add(190, () => Q.Cast(epos));
            }
        }

        static void Flee()
        {
            Orbwalker.SetAttack(false);
            var x = Player.Position.Extend(Game.CursorPos, 300);
            if (Q.IsReady() && !Player.IsDashing()) Q.Cast(x);
        }

        static void OnPlay(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args)
        {
            if (sender.IsMe && (Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.None && Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.LastHit))
            {
                switch (args.Animation)
                {
                    case "Spell1a":
                        Utility.DelayAction.Add((QD * 10) + 1, () => Reset());
                        break;
                    case "Spell1b":
                        Utility.DelayAction.Add((QD * 10) + 1, () => Reset());
                        break;
                    case "Spell1c":
                        Utility.DelayAction.Add((QLD * 10) + 3, () => Reset());
                        break;
                }
            }
        }

        static void OnCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;

            if (args.SData.Name.Contains("RivenTriCleave"))
            {
                forceQ = false;
                lastQ = Utils.GameTimeTickCount;
                if (Qstrange && Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.None) Game.SendEmote(Emote.Dance);
                QStack += 1;
            }
            if (args.SData.Name.Contains("RivenFeint"))
            {
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Burst ||
                    Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo ||
                    Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.FastHarass ||
                    Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Flee)
                {
                    if (Youmu)
                    {
                        CastYoumoo();
                    }
                }
            }

            if (args.SData.Name.Contains("rivenizunablade"))
            {
                var target = TargetSelector.GetSelectedTarget();
                if (Q.IsReady() && target.IsValid) forcecastQ(target);
            }
        }

        static void Reset()
        {
            Game.SendEmote(Emote.Dance);
            Orbwalking.LastAATick = 0;
            Player.IssueOrder(GameObjectOrder.MoveTo, Player.Position.Extend(Game.CursorPos, Player.Distance(Game.CursorPos) + 10));
        }

        static bool InWRange(AttackableUnit target)
        {
            if (Player.HasBuff("RivenFengShuiEngine") && target != null)
            {
                return
                    70 + 195 + Player.BoundingRadius >= Player.Distance(target.Position);
            }
            else
            {
                return
                   70 + 120 + Player.BoundingRadius >= Player.Distance(target.Position);
            }
        }

        static void saveq()
        {
            if (QStack != 1)
            {
                if (Q.IsReady())
                {
                    Q.Cast(Game.CursorPos);
                }
            }
        }

        static void statereset()
        {
            if (Utils.GameTimeTickCount - lastQ >= 3650 && QStack != 1 && !Player.IsRecalling() && KeepQ) saveq();
            if (!Q.IsReady(500) || QStack == 4) QStack = 1;
            if (forceQ && Orbwalking.CanMove(5) && QTarget != null && QTarget.IsValidTarget(E.Range + Player.BoundingRadius + 70) && (Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.None || Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.LastHit))
            {
                if (Q.IsReady()) Q.Cast(QTarget.Position);
            }
        }

        static void forcecastQ(AttackableUnit target)
        {
            forceQ = true;
            if (target.IsValidTarget() && target != null) QTarget = target;
            else QTarget = null;
        }

        static void UseR(int t)
        {
            var target = TargetSelector.GetSelectedTarget();
            if (target != null && target.IsValidTarget() && !target.IsZombie && R.IsReady() && R.Instance.Name == IsSecondR)
            {
                for (int i = 0; i < t; i = i + 1)
                {
                    Utility.DelayAction.Add(i, () => R.Cast(target.Position));
                }
            }
        }

        static void UseW(int t)
        {
            for (int i = 0; i < t; i = i + 1)
            {
                if (W.IsReady())
                    Utility.DelayAction.Add(i, () => W.Cast());
            }
        }
        static void UseCastItem(int t)
        {
            for (int i = 0; i < t; i = i + 1)
            {
                if (HasItem())
                    Utility.DelayAction.Add(i, () => CastItem());
            }
        }

        static void FlashW()
        {
            var target = TargetSelector.GetSelectedTarget();
            if (target != null && target.IsValidTarget() && !target.IsZombie)
            {
                W.Cast();
                Utility.DelayAction.Add(10, () => Player.Spellbook.CastSpell(Flash, target.Position));
            }
        }

        static bool HasItem()
        {
            if (ItemData.Tiamat_Melee_Only.GetItem().IsReady() || ItemData.Ravenous_Hydra_Melee_Only.GetItem().IsReady())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        static void CastItem()
        {

            if (ItemData.Tiamat_Melee_Only.GetItem().IsReady())
                ItemData.Tiamat_Melee_Only.GetItem().Cast();
            if (ItemData.Ravenous_Hydra_Melee_Only.GetItem().IsReady())
                ItemData.Ravenous_Hydra_Melee_Only.GetItem().Cast();
        }
        static void CastYoumoo()
        {
            if (ItemData.Youmuus_Ghostblade.GetItem().IsReady())
                ItemData.Youmuus_Ghostblade.GetItem().Cast();
        }
        static void OnCasting(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            var targets = HeroManager.Enemies.Where(x => x.IsValidTarget() && !x.IsZombie && InWRange(x));
            if (sender.IsEnemy && sender.Type == Player.Type && (AutoShield || (Shield && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)))
            {
                var epos = Player.ServerPosition +
                          (Player.ServerPosition - sender.ServerPosition).Normalized() * 300;

                if (Player.Distance(sender.ServerPosition) <= args.SData.CastRange)
                {
                    switch (args.SData.TargettingType)
                    {
                        case SpellDataTargetType.Unit:

                            if (args.Target.NetworkId == Player.NetworkId)
                            {
                                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit && !args.SData.Name.Contains("NasusW"))
                                {
                                    if (E.IsReady()) E.Cast(epos);
                                }
                            }

                            break;
                        case SpellDataTargetType.SelfAoe:

                            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
                            {
                                if (E.IsReady()) E.Cast(epos);
                            }

                            break;
                    }
                    if (args.SData.Name.Contains("IreliaEquilibriumStrike"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (W.IsReady() && InWRange(sender)) W.Cast();
                            else if (E.IsReady()) E.Cast(epos);
                        }
                    }
                    if (args.SData.Name.Contains("TalonCutthroat"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (W.IsReady()) W.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("RenektonPreExecute"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (W.IsReady()) W.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("GarenRPreCast"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast(epos);
                        }
                    }
                    if (args.SData.Name.Contains("GarenQAttack"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("XenZhaoThrust3"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (W.IsReady()) W.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("RengarQ"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("RengarPassiveBuffDash"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("RengarPassiveBuffDashAADummy"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("TwitchEParticle"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("FizzPiercingStrike"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("HungeringStrike"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("YasuoDash"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("KatarinaRTrigger"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (W.IsReady() && InWRange(sender)) W.Cast();
                            else if (E.IsReady()) E.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("YasuoDash"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("KatarinaE"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (W.IsReady()) W.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("MonkeyKingQAttack"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("MonkeyKingSpinToWin"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast();
                            else if (W.IsReady()) W.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("MonkeyKingQAttack"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("MonkeyKingQAttack"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("MonkeyKingQAttack"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast();
                        }
                    }
                }
            }
        }
        
        static double basicdmg(Obj_AI_Base target)
        {
            if (target != null)
            {
                double dmg = 0;
                double passivenhan = 0;
                if (Player.Level >= 18) { passivenhan = 0.5; }
                else if (Player.Level >= 15) { passivenhan = 0.45; }
                else if (Player.Level >= 12) { passivenhan = 0.4; }
                else if (Player.Level >= 9) { passivenhan = 0.35; }
                else if (Player.Level >= 6) { passivenhan = 0.3; }
                else if (Player.Level >= 3) { passivenhan = 0.25; }
                else { passivenhan = 0.2; }
                if (HasItem()) dmg = dmg + Player.GetAutoAttackDamage2(target) * 0.7;
                if (W.IsReady()) dmg = dmg + W.GetDamage2(target);
                if (Q.IsReady())
                {
                    var qnhan = 4 - QStack;
                    dmg = dmg + Q.GetDamage2(target) * qnhan + Player.GetAutoAttackDamage2(target) * qnhan * (1 + passivenhan);
                }
                dmg = dmg + Player.GetAutoAttackDamage2(target) * (1 + passivenhan);
                return dmg;
            }
            else { return 0; }
        }


        static float getComboDamage(Obj_AI_Base enemy)
        {
            if (enemy != null)
            {
                float damage = 0;
                float passivenhan = 0;
                if (Player.Level >= 18) { passivenhan = 0.5f; }
                else if (Player.Level >= 15) { passivenhan = 0.45f; }
                else if (Player.Level >= 12) { passivenhan = 0.4f; }
                else if (Player.Level >= 9) { passivenhan = 0.35f; }
                else if (Player.Level >= 6) { passivenhan = 0.3f; }
                else if (Player.Level >= 3) { passivenhan = 0.25f; }
                else { passivenhan = 0.2f; }
                if (HasItem()) damage = damage + (float)Player.GetAutoAttackDamage2(enemy) * 0.7f;
                if (W.IsReady()) damage = damage + W.GetDamage2(enemy);
                if (Q.IsReady())
                {
                    var qnhan = 4 - QStack;
                    damage = damage + Q.GetDamage2(enemy) * qnhan + (float)Player.GetAutoAttackDamage2(enemy) * qnhan * (1 + passivenhan);
                }
                damage = damage + (float)Player.GetAutoAttackDamage2(enemy) * (1 + passivenhan);
                if (R.IsReady())
                {
                    return damage * 1.2f + R.GetDamage2(enemy);
                }

                return damage;
            }
            else return 0;
        }

        static bool IsKillableR(Obj_AI_Hero target)
        {
            if (RKillable && target.IsValidTarget() && (totaldame(target) >= target.Health
                 && basicdmg(target) <= target.Health) || Player.CountEnemiesInRange(900) >= 2 && (!target.HasBuff("kindrednodeathbuff") && !target.HasBuff("Undying Rage") && !target.HasBuff("JudicatorIntervention")))
            {
                return true;
            }
            else return false;
        }
        static double totaldame(Obj_AI_Base target)
        {
            if (target != null)
            {
                double dmg = 0;
                double passivenhan = 0;
                if (Player.Level >= 18) { passivenhan = 0.5; }
                else if (Player.Level >= 15) { passivenhan = 0.45; }
                else if (Player.Level >= 12) { passivenhan = 0.4; }
                else if (Player.Level >= 9) { passivenhan = 0.35; }
                else if (Player.Level >= 6) { passivenhan = 0.3; }
                else if (Player.Level >= 3) { passivenhan = 0.25; }
                else { passivenhan = 0.2; }
                if (HasItem()) dmg = dmg + Player.GetAutoAttackDamage2(target) * 0.7;
                if (W.IsReady()) dmg = dmg + W.GetDamage2(target);
                if (Q.IsReady())
                {
                    var qnhan = 4 - QStack;
                    dmg = dmg + Q.GetDamage2(target) * qnhan + Player.GetAutoAttackDamage2(target) * qnhan * (1 + passivenhan);
                }
                dmg = dmg + Player.GetAutoAttackDamage2(target) * (1 + passivenhan);
                if (R.IsReady())
                {
                    var rdmg = Rdame(target, target.Health - dmg * 1.2);
                    return dmg * 1.2 + rdmg;
                }
                else return dmg;
            }
            else return 0;
        }
        static double Rdame(Obj_AI_Base target, double health)
        {
            if (target != null)
            {
                var missinghealth = (target.MaxHealth - health) / target.MaxHealth > 0.75 ? 0.75 : (target.MaxHealth - health) / target.MaxHealth;
                var pluspercent = missinghealth * (8 / 3);
                var rawdmg = new double[] { 80, 120, 160 }[R.Level - 1] + 0.6 * Player.FlatPhysicalDamageMod;
                return Player.CalcDamage(target, Damage.DamageType.Physical, rawdmg * (1 + pluspercent));
            }
            else return 0;
        }
    }
}