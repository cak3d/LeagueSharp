using System;
using System.Linq;
using System.Collections.Generic;

using LeagueSharp;
using LeagueSharp.Common;

namespace Caked_AIO
{
    class PotionData
    {
        internal string BuffName;
        internal int Priority;
        internal ItemId ID;
        internal PotionType[] TypeList;
    }

    class ActiveItemData
    {
        internal string Name;
        internal ItemId ID;
        internal float Range;
        internal bool isTargeted;
        internal When[] When;
        internal ActiveItemType Type;
        internal float MinMyHP = 100;
        internal float MinTargetHP = 100;
        internal Damage.DamageItems DamageItem;
    }

    class CleanserData
    {
        internal string Name;
        internal ItemId ID;
        internal int Priority;
        internal CleanserTarget[] CleanserTargets;
        internal bool isTargeted = false;
        internal float Range = float.MaxValue;
    }

    enum PotionType
    {
        HP,
        MP,
    }

    enum When
    {
        BeforeAttack,
        AfterAttack,
        AntiGapcloser,
        Killsteal
    }

    enum ActiveItemType
    {
        Offensive,
        Defensive
    }

    enum CleanserTarget
    {
        Me,
        Ally
    }

    class Activator
    {
        private static Menu Menu { get { return MenuProvider.MenuInstance.SubMenu("Activator"); } }
        private static PotionData[] PotionList;
        private static ActiveItemData[] ActiveItemList;
        private static CleanserData[] CleanserList;
        private static SpellSlot CleanseSlot;
        private static SpellSlot HealSlot;
        private static BuffType[] MikaelBuffType;

        internal static void Load()
        {
            CleanseSlot = ObjectManager.Player.GetSpellSlot("summonerboost");
            HealSlot = ObjectManager.Player.GetSpellSlot("summonerheal");

            Menu.AddSubMenu(new Menu("Auto Potion", "AutoPotion"));
            Menu.AddSubMenu(new Menu("Cleanser", "Cleanser"));
            Menu.AddSubMenu(new Menu("Offensive", "Offensive"));
            //Menu.AddSubMenu(new Menu("Defensive", "Defensive"));
            Menu.AddSubMenu(new Menu("SummonerSpell", "SummonerSpell"));

            Menu.SubMenu("Cleanser").AddSubMenu(new Menu("BuffType", "BuffType"));
            Menu.SubMenu("Cleanser").SubMenu("BuffType").AddItem(new MenuItem("Cleanser.BuffType.Stun", "Stun (스턴)")).SetValue(true);
            Menu.SubMenu("Cleanser").SubMenu("BuffType").AddItem(new MenuItem("Cleanser.BuffType.Snare", "Snare (속박)")).SetValue(true);
            Menu.SubMenu("Cleanser").SubMenu("BuffType").AddItem(new MenuItem("Cleanser.BuffType.Charm", "Charm (매혹)")).SetValue(true);
            Menu.SubMenu("Cleanser").SubMenu("BuffType").AddItem(new MenuItem("Cleanser.BuffType.Flee", "Flee (공포)")).SetValue(true);
            Menu.SubMenu("Cleanser").SubMenu("BuffType").AddItem(new MenuItem("Cleanser.BuffType.Taunt", "Taunt (도발)")).SetValue(true);
            Menu.SubMenu("Cleanser").SubMenu("BuffType").AddItem(new MenuItem("Cleanser.BuffType.Polymorph", "Polymorph (변이)")).SetValue(true);
            Menu.SubMenu("Cleanser").SubMenu("BuffType").AddItem(new MenuItem("Cleanser.BuffType.Suppression", "Suppression (제압)")).SetValue(true);
            //Menu.SubMenu("Cleanser").SubMenu("BuffType").AddItem(new MenuItem("Cleanser.BuffType.Fear", "Fear (공포)")).SetValue(false);
            Menu.SubMenu("Cleanser").SubMenu("BuffType").AddItem(new MenuItem("Cleanser.BuffType.Slow", "Slow (둔화)")).SetValue(false);
            Menu.SubMenu("Cleanser").SubMenu("BuffType").AddItem(new MenuItem("Cleanser.BuffType.Poison", "Poison (중독)")).SetValue(false);
            Menu.SubMenu("Cleanser").SubMenu("BuffType").AddItem(new MenuItem("Cleanser.BuffType.Blind", "Blind (블라인드)")).SetValue(false);
            Menu.SubMenu("Cleanser").SubMenu("BuffType").AddItem(new MenuItem("Cleanser.BuffType.Silence", "Silence (침묵)")).SetValue(false);

            Menu.SubMenu("Cleanser").AddItem(new MenuItem("Cleanser.Use Humanizer", "Use Humanized Delay")).SetValue(true);
            Menu.SubMenu("Cleanser").AddItem(new MenuItem("Cleanser.Mode", "Mode")).SetValue(new StringList(new string[] { "Combo", "Always" }));

            Menu.SubMenu("AutoPotion").AddItem(new MenuItem("AutoPotion.Use Health Potion", "Use Health Potion")).SetValue(true);
            Menu.SubMenu("AutoPotion").AddItem(new MenuItem("AutoPotion.ifHealthPercent", "if Health Percent <=")).SetValue(new Slider(60, 0, 100));
            Menu.SubMenu("AutoPotion").AddItem(new MenuItem("AutoPotion.Use Mana Potion", "Use Mana Potion")).SetValue(true);
            Menu.SubMenu("AutoPotion").AddItem(new MenuItem("AutoPotion.ifManaPercent", "if Mana Percent <=")).SetValue(new Slider(60, 0, 100));

            Initialize();

            foreach (var item in ActiveItemList)
                Menu.SubMenu("Offensive").AddItem(new MenuItem("Offensive.Use" + item.ID, "Use " + item.Name)).SetValue(true);

            foreach (var item in CleanserList)
                Menu.SubMenu("Cleanser").AddItem(new MenuItem("Cleanser.Use" + item.ID, "Use " + item.Name)).SetValue(true);

            Menu.SubMenu("Cleanser").AddSubMenu(new Menu("Mikael's Crucible Settings", "MikaelSettings"));
            Menu.SubMenu("Cleanser").SubMenu("MikaelSettings").AddItem(new MenuItem("Cleanser.MikaelSettings.ForMe", "Use For Me")).SetValue(true);
            Menu.SubMenu("Cleanser").SubMenu("MikaelSettings").AddItem(new MenuItem("Cleanser.MikaelSettings.ForAlly", "Use For Ally")).SetValue(true);

            Menu.SubMenu("Cleanser").AddItem(new MenuItem("Cleanser.UseCleanse", "Use Cleanse (정화)")).SetValue(true);

            Menu.SubMenu("SummonerSpell").AddSubMenu(new Menu("Heal (회복)", "Heal"));
            Menu.SubMenu("SummonerSpell").SubMenu("Heal").AddItem(new MenuItem("SummonerSpell.Heal.UseHeal", "Use Heal")).SetValue(true);
            Menu.SubMenu("SummonerSpell").SubMenu("Heal").AddItem(new MenuItem("SummonerSpell.Heal.UseForMe", "Use For Me")).SetValue(true);
            Menu.SubMenu("SummonerSpell").SubMenu("Heal").AddItem(new MenuItem("SummonerSpell.Heal.UseForAlly", "Use For Ally")).SetValue(true);
            Menu.SubMenu("SummonerSpell").SubMenu("Heal").AddItem(new MenuItem("SummonerSpell.Heal.ifHealthPercent", "if HealthPercent <=")).SetValue(new Slider(30, 0, 70));

            Game.OnUpdate += Game_OnUpdate;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Obj_AI_Base.OnBuffAdd += Obj_AI_Base_OnBuffAdd;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }

        private static void Initialize()
        {
            PotionList = new PotionData[]
            {
                new PotionData { ID = ItemId.Health_Potion, BuffName = "RegenerationPotion", Priority = 2, TypeList = new PotionType[] { PotionType.HP } },//체력 물약
                //new PotionData { ID = ItemId.Mana_Potion, BuffName = "FlaskOfCrystalWater", Priority = 2, TypeList = new PotionType[] { PotionType.MP }},//마나 물약
                //new PotionData { ID = ItemId.Crystalline_Flask, BuffName = "ItemCrystalFlask", Priority = 1, TypeList = new PotionType[] { PotionType.HP, PotionType.MP }},//플라스크
                new PotionData { ID = (ItemId) 2010, BuffName = "ItemMiniRegenPotion", Priority = 2, TypeList = new PotionType[] { PotionType.HP }},//비스킷
                new PotionData { ID = (ItemId) 2031, BuffName = "ItemCrystalFlask", Priority = 1, TypeList = new PotionType[] { PotionType.HP }},//충전형 물약
                new PotionData { ID = (ItemId) 2032, BuffName = "ItemCrystalFlaskJungle", Priority = 1, TypeList = new PotionType[] { PotionType.HP, PotionType.MP }},//사냥꾼의 물약
                new PotionData { ID = (ItemId) 2033, BuffName = "ItemDarkCrystalFlask", Priority = 1, TypeList = new PotionType[] { PotionType.HP, PotionType.MP }},//부패 물약
            };

            ActiveItemList = new ActiveItemData[]
            {
                new ActiveItemData { Name = "Blade of the Ruined King (몰락한 왕의 검)", ID = ItemId.Blade_of_the_Ruined_King,  Range = 550f, isTargeted = true, Type = ActiveItemType.Offensive, MinMyHP = 85, MinTargetHP = 85, DamageItem = Damage.DamageItems.Botrk, When = new When[] { When.AfterAttack, When.AntiGapcloser, When.Killsteal } },
                new ActiveItemData { Name = "Bilgewater Cutlass (빌지워터 해적검)", ID = ItemId.Bilgewater_Cutlass,  Range = 550f, isTargeted = true, Type = ActiveItemType.Offensive, DamageItem = Damage.DamageItems.Bilgewater, When = new When[] { When.AfterAttack, When.AntiGapcloser, When.Killsteal }},
                new ActiveItemData { Name = "Youmuu's Ghostblade (요우무의 유령검)", ID = ItemId.Youmuus_Ghostblade,  Range = float.MaxValue, isTargeted = false, Type = ActiveItemType.Offensive, When = new When[] { When.BeforeAttack, When.AntiGapcloser }},
                new ActiveItemData { Name = "Ravenous Hydra (굶주린 히드라)", ID = ItemId.Ravenous_Hydra_Melee_Only,  Range = 400f, isTargeted = false, Type = ActiveItemType.Offensive, DamageItem = Damage.DamageItems.Hydra, When = new When[] { When.AfterAttack, When.Killsteal } },
                new ActiveItemData { Name = "Titanic Hydra (거대한 히드라)", ID = (ItemId)3748,  Range = float.MaxValue, isTargeted = false, Type = ActiveItemType.Offensive, When = new When[] { When.AfterAttack } },
                new ActiveItemData { Name = "Tiamat (티아맷)", ID = ItemId.Tiamat_Melee_Only,  Range = 400f, isTargeted = false, Type = ActiveItemType.Offensive, DamageItem = Damage.DamageItems.Tiamat, When = new When[] { When.AfterAttack, When.Killsteal } },
            };

            CleanserList = new CleanserData[]
            {
                new CleanserData { Name = "Quicksilver Sash (수은 장식띠)", ID = ItemId.Quicksilver_Sash, CleanserTargets = new CleanserTarget[] { CleanserTarget.Me }, isTargeted = false,  Priority = 2 },
                new CleanserData { Name = "Mercurial Scimitar (헤르메스의 시미터)", ID = ItemId.Mercurial_Scimitar, CleanserTargets = new CleanserTarget[] { CleanserTarget.Me }, isTargeted = false, Priority = 1 },
                new CleanserData { Name = "Dervish Blade (광신도의 검)", ID = ItemId.Dervish_Blade, CleanserTargets = new CleanserTarget[] { CleanserTarget.Me }, isTargeted = false, Priority = 1 },
                new CleanserData { Name = "Mikael's Crucible (미카엘의 도가니)", ID = ItemId.Mikaels_Crucible, CleanserTargets = new CleanserTarget[] { CleanserTarget.Me, CleanserTarget.Ally }, isTargeted = true, Priority = 3, Range = 750f }
            };

            MikaelBuffType = new BuffType[]
            {
                BuffType.Stun,
                BuffType.Snare,
                BuffType.Taunt,
                BuffType.Flee,
                BuffType.Silence,
                BuffType.Slow
            };
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (!ObjectManager.Player.IsDead)
            {
                if (!ObjectManager.Player.IsRecalling() && !ObjectManager.Player.InFountain())
                {
                    if (Menu.Item("AutoPotion.Use Health Potion").GetValue<bool>())
                        if (ObjectManager.Player.HealthPercent <= Menu.Item("AutoPotion.ifHealthPercent").GetValue<Slider>().Value)
                        {
                            var healthSlot = (from potion in PotionList where potion.TypeList.Contains(PotionType.HP) from item in ObjectManager.Player.InventoryItems where item.Id == potion.ID orderby potion.Priority ascending select item).FirstOrDefault();

                            if (healthSlot != null && !(from potion in PotionList where potion.TypeList.Contains(PotionType.HP) from buff in ObjectManager.Player.Buffs where buff.Name == potion.BuffName && buff.IsActive select potion).Any())
                                ObjectManager.Player.Spellbook.CastSpell(healthSlot.SpellSlot);
                        }

                    if (Menu.Item("AutoPotion.Use Mana Potion").GetValue<bool>())
                        if (ObjectManager.Player.ManaPercent <= Menu.Item("AutoPotion.ifManaPercent").GetValue<Slider>().Value)
                        {
                            var manaSlot = (from potion in PotionList where potion.TypeList.Contains(PotionType.MP) from item in ObjectManager.Player.InventoryItems where item.Id == potion.ID orderby potion.Priority ascending select item).FirstOrDefault();

                            if (manaSlot != null && !(from potion in PotionList where potion.TypeList.Contains(PotionType.MP) from buff in ObjectManager.Player.Buffs where buff.Name == potion.BuffName && buff.IsActive select potion).Any())
                                ObjectManager.Player.Spellbook.CastSpell(manaSlot.SpellSlot);
                        }
                }

                foreach (var Target in HeroManager.Allies.Where(x => x.IsValidTarget() && x.CountEnemiesInRange(500f) >= 2))
                {
                    if (Menu.Item("SummonerSpell.Heal.UseHeal").GetValue<bool>())
                    {
                        if (Target.IsMe)
                        {
                            if (Menu.Item("SummonerSpell.Heal.UseForMe").GetValue<bool>())
                                if (ObjectManager.Player.HealthPercent <= Menu.Item("SummonerSpell.Heal.ifHealthPercent").GetValue<Slider>().Value)
                                    if (HealSlot != SpellSlot.Unknown && HealSlot.IsReady())
                                        ObjectManager.Player.Spellbook.CastSpell(HealSlot);
                        }
                        else
                        if (Target.IsAlly)
                        {
                            if ((Target as Obj_AI_Hero).IsValidTarget(840f, false, ObjectManager.Player.ServerPosition))
                                if (Menu.Item("SummonerSpell.Heal.UseForAlly").GetValue<bool>())
                                    if (Target.HealthPercent <= Menu.Item("SummonerSpell.Heal.ifHealthPercent").GetValue<Slider>().Value)
                                        if (HealSlot != SpellSlot.Unknown && HealSlot.IsReady())
                                            ObjectManager.Player.Spellbook.CastSpell(HealSlot);
                        }
                    }
                }

                foreach (var target in HeroManager.Enemies.Where(x => x.IsValidTarget()))
                {
                    var Item = ActiveItemList.FirstOrDefault(x => Menu.Item("Offensive.Use" + x.ID).GetValue<bool>() && x.When.Contains(When.Killsteal) && Items.CanUseItem((int)x.ID) && target.isKillableAndValidTarget(Damage.GetItemDamage(ObjectManager.Player, target, x.DamageItem), TargetSelector.DamageType.Physical, x.Range));
                    if (Item != null)
                        Items.UseItem((int)Item.ID, Item.isTargeted ? target : null);
                }
            }
        }

        private static void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (args.Unit.IsMe)
            {
                switch (MenuProvider.Orbwalker.ActiveMode)
                {
                    case Orbwalking.OrbwalkingMode.Combo:
                        var Item = ActiveItemList.FirstOrDefault(x => Menu.Item("Offensive.Use" + x.ID).GetValue<bool>() && x.When.Contains(When.BeforeAttack) && Items.CanUseItem((int)x.ID) && args.Target.IsValidTarget(x.Range) && ObjectManager.Player.ManaPercent <= x.MinMyHP && args.Target.ManaPercent <= x.MinTargetHP);
                        if (Item != null)
                            Items.UseItem((int)Item.ID, Item.isTargeted ? args.Target as Obj_AI_Base : null);
                        break;
                }
            }
        }

        private static void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (unit.IsMe)
            {
                switch (MenuProvider.Orbwalker.ActiveMode)
                {
                    case Orbwalking.OrbwalkingMode.Combo:
                        var Item = ActiveItemList.FirstOrDefault(x => Menu.Item("Offensive.Use" + x.ID).GetValue<bool>() && x.When.Contains(When.AfterAttack) && Items.CanUseItem((int)x.ID) && target.IsValidTarget(x.Range) && ObjectManager.Player.ManaPercent <= x.MinMyHP && target.ManaPercent <= x.MinTargetHP);
                        if (Item != null)
                            Items.UseItem((int)Item.ID, Item.isTargeted ? target as Obj_AI_Base : null);
                        break;
                }
            }
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!ObjectManager.Player.IsDead)
                if (ObjectManager.Player.Position.Distance(gapcloser.End) <= 200)
                {
                    var Item = ActiveItemList.FirstOrDefault(x => Menu.Item("Offensive.Use" + x.ID).GetValue<bool>() && x.When.Contains(When.AntiGapcloser) && Items.CanUseItem((int)x.ID) && gapcloser.Sender.IsValidTarget(x.Range));
                    if (Item != null)
                        Items.UseItem((int)Item.ID, Item.isTargeted ? gapcloser.Sender : null);
                }
        }

        private static void Obj_AI_Base_OnBuffAdd(Obj_AI_Base sender, Obj_AI_BaseBuffAddEventArgs args)
        {
            if (sender != null && args != null)
                if (args.Buff.Caster.Type == GameObjectType.obj_AI_Hero)
                {
                    var BuffCaster = args.Buff.Caster as Obj_AI_Hero;

                    if (BuffCaster.ChampionName == "Rammus" && args.Buff.Type == BuffType.Stun)
                        return;

                    if (BuffCaster.ChampionName == "LeeSin" && args.Buff.Type == BuffType.Stun)
                        return;

                    if (BuffCaster.ChampionName == "Alistar" && args.Buff.Type == BuffType.Stun)
                        return;

                    if (Menu.Item("Cleanser.BuffType." + args.Buff.Type.ToString()) != null && Menu.Item("Cleanser.BuffType." + args.Buff.Type.ToString()).GetValue<bool>())
                    {
                        Utility.DelayAction.Add(Menu.Item("Cleanser.Use Humanizer").GetValue<bool>() ? new Random().Next(150, 280) : 20, () =>
                        {
                            if (Menu.Item("Cleanser.Mode").GetValue<StringList>().SelectedValue == "Combo" ? MenuProvider.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo : true)
                            {
                                if (sender.IsMe)
                                {
                                    var item = CleanserList.Where(x => x.CleanserTargets.Contains(CleanserTarget.Me) && Menu.Item("Cleanser.Use" + x.ID).GetValue<bool>() && Items.CanUseItem((int)x.ID)).OrderBy(x => x.Priority).FirstOrDefault();
                                    if (item != null)
                                    {
                                        switch (item.ID)
                                        {
                                            case ItemId.Mikaels_Crucible:
                                                if (Menu.Item("Cleanser.MikaelSettings.ForMe").GetValue<bool>())
                                                    if (MikaelBuffType.Contains(args.Buff.Type))
                                                        Items.UseItem((int)item.ID, sender);
                                                break;
                                            default:
                                                Items.UseItem((int)item.ID, item.isTargeted ? ObjectManager.Player : null);
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        if (Menu.Item("Cleanser.UseCleanse").GetValue<bool>())
                                            if (CleanseSlot != SpellSlot.Unknown && CleanseSlot.IsReady())
                                                ObjectManager.Player.Spellbook.CastSpell(CleanseSlot);
                                    }
                                }
                                else
                                if (sender.IsAlly)
                                {
                                    var item = CleanserList.Where(x => x.CleanserTargets.Contains(CleanserTarget.Ally) && Menu.Item("Cleanser.Use" + x.ID).GetValue<bool>() && Items.CanUseItem((int)x.ID) && sender.IsValidTarget(x.Range, false)).OrderBy(x => x.Priority).FirstOrDefault();
                                    if (item != null)
                                    {
                                        switch (item.ID)
                                        {
                                            case ItemId.Mikaels_Crucible:
                                                if (Menu.Item("Cleanser.MikaelSettings.ForAlly").GetValue<bool>())
                                                    if (MikaelBuffType.Contains(args.Buff.Type))
                                                        Items.UseItem((int)item.ID, sender);
                                                break;
                                            default:
                                                Items.UseItem((int)item.ID, item.isTargeted ? sender : null);
                                                break;
                                        }
                                    }
                                }
                            }
                        });
                    }
                }
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender != null)
                if (args.Target != null)
                    if (sender.IsEnemy)
                        if (sender.Type == GameObjectType.obj_AI_Hero || sender.Type == GameObjectType.obj_AI_Turret)
                            if (args.Target.Type == GameObjectType.obj_AI_Hero)
                            {
                                if (Menu.Item("SummonerSpell.Heal.UseHeal").GetValue<bool>())
                                {
                                    if (args.Target.IsMe)
                                    {
                                        if (Menu.Item("SummonerSpell.Heal.UseForMe").GetValue<bool>())
                                            if (ObjectManager.Player.HealthPercent <= Menu.Item("SummonerSpell.Heal.ifHealthPercent").GetValue<Slider>().Value)
                                                if (HealSlot != SpellSlot.Unknown && HealSlot.IsReady())
                                                    ObjectManager.Player.Spellbook.CastSpell(HealSlot);
                                    }
                                    else
                                    if (args.Target.IsAlly)
                                    {
                                        var Target = args.Target as Obj_AI_Hero;
                                        if (Target.IsValidTarget(850f, false))
                                            if (Menu.Item("SummonerSpell.Heal.UseForAlly").GetValue<bool>())
                                                if (Target.HealthPercent <= Menu.Item("SummonerSpell.Heal.ifHealthPercent").GetValue<Slider>().Value)
                                                    if (HealSlot != SpellSlot.Unknown && HealSlot.IsReady())
                                                        ObjectManager.Player.Spellbook.CastSpell(HealSlot);
                                    }
                                }
                            }
        }
    }
}