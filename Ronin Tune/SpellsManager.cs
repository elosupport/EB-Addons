﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Constants;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using Mario_s_Lib;
using static RoninTune.Menus;

namespace RoninTune
{
    public static class SpellsManager
    {

        public static Spell.Skillshot Q;
        public static Spell.Active W;
        public static Spell.Targeted E;
        public static Spell.Active R;
        public static Spell.Targeted R1;
        public static Spell.Targeted Smite { get; private set; }
        public static Spell.Skillshot Flash { get; private set; }
        public static Spell.Targeted Ignite { get; private set; }

        /// <summary>
        /// It sets the values to the spells
        /// </summary>
        public static void InitializeSpells()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 1125, SkillShotType.Linear);
            W = new Spell.Active(SpellSlot.W);
            E = new Spell.Targeted(SpellSlot.E, 425);
            R = new Spell.Active(SpellSlot.R, 2500);
            R1 = new Spell.Targeted(SpellSlot.R, R.Range);
            //SMITE IGNITE
            var slot = ObjectManager.Player.GetSpellSlotFromName("summonerdot");
            if (slot != SpellSlot.Unknown)
            {
                Ignite = new Spell.Targeted(slot, 600);
            }
            slot = Player.Instance.GetSpellSlotFromName("smite");
            if (slot != SpellSlot.Unknown)
            {
                Smite = new Spell.Targeted(slot, 500);
            }
            //Q = new Spell.Targeted(SpellSlot.Q, 350);
            //W = new Spell.Active(SpellSlot.W, 200);
            //E = new Spell.Active(SpellSlot.E, 300);
            //R = new Spell.Targeted(SpellSlot.R, 400);


            Obj_AI_Base.OnLevelUp += Obj_AI_Base_OnLevelUp;
        }
        
        public static bool HasSmite()
        {
            return Smite != null && Smite.IsLearned;
        }
        public static bool SmiteIsReady
        {
            get
            {
                return Smite != null && Smite.IsReady();
            }
        }
        public static bool IsInSmiteRange(this Obj_AI_Base target)
        {
            return target.IsValidTarget(Smite.Range + Util.MyHero.BoundingRadius + target.BoundingRadius);
        }
        public static SpellSlot SpellSlotFromName(this AIHeroClient hero, string name)
        {
            foreach (var s in hero.Spellbook.Spells.Where(s => s.Name.ToLower().Contains(name.ToLower())))
            {
                return s.Slot;
            }
            return SpellSlot.Unknown;
        }
        public static bool IsDragon(this Obj_AI_Minion minion)
        {
            return minion.IsValidTarget() && (minion.Name.ToLower().Contains("baron") || minion.Name.ToLower().Contains("dragon"));
        }
        public static float SmiteDamage(this Obj_AI_Base target)
        {
            if (target.IsValidTarget() && SmiteIsReady)
            {
                if (target is AIHeroClient)
                {
                    if (CanUseSmiteOnHeroes)
                    {
                        return Util.MyHero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Smite);
                    }
                }
                else
                {
                    var level = Util.MyHero.Level;
                    return (new[] { 20 * level + 370, 30 * level + 330, 40 * level + 240, 50 * level + 100 }).Max();
                }
            }
            return 0;
        }
        public static bool CanUseSmiteOnHeroes
        {
            get
            {
                if (SmiteIsReady)
                {
                    var name = Smite.Slot.GetSpellDataInst().SData.Name.ToLower();
                    if (name.Contains("smiteduel") || name.Contains("smiteplayerganker"))
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        public static bool IsReady(this SpellSlot slot)
        {
            return slot.GetSpellDataInst().IsReady;
        }
        public static bool IsFirstSpell(this SpellSlot slot)
        {
            return slot.GetSpellDataInst().SData.Name.ToLower().Contains("one");
        }
        public static SpellDataInst GetSpellDataInst(this SpellSlot slot)
        {
            return Util.MyHero.Spellbook.GetSpell(slot);
        }
        #region Damages

        /// <summary>
        /// It will return the damage but you need to set them before getting the damage
        /// </summary>
        /// <param name="target"></param>
        /// <param name="slot"></param>
        /// <returns></returns>
        public static float GetDamage(this Obj_AI_Base target, SpellSlot slot)
        {
            var damageType = DamageType.Magical;
            var AD = Player.Instance.FlatPhysicalDamageMod;
            var AP = Player.Instance.FlatMagicDamageMod;
            var sLevel = Player.GetSpell(slot).Level - 1;

            //You can get the damage information easily on wikia

            var dmg = 0f;

            switch (slot)
            {
                case SpellSlot.Q:
                    if (Q.IsReady())
                    {
                        //Information of Q damage
                        dmg += new float[] {20, 45, 70, 95, 120}[sLevel] + 1f*AD;
                    }
                    break;
                case SpellSlot.W:
                    if (W.IsReady())
                    {
                        //Information of W damage
                        dmg += new float[] {0, 0, 0, 0, 0}[sLevel] + 1f*AD;
                    }
                    break;
                case SpellSlot.E:
                    if (E.IsReady())
                    {
                        //Information of E damage
                        dmg += new float[] {80, 110, 140, 170, 200}[sLevel];
                    }
                    break;
                case SpellSlot.R:
                    if (R.IsReady())
                    {
                        //Information of R damage
                        dmg += new float[] {600, 840, 1080}[sLevel]*0.6f + 1.2f*AP;
                    }
                    break;
            }
            return Player.Instance.CalculateDamageOnUnit(target, damageType, dmg - 10);
        }

        #endregion Damages

        /// <summary>
        /// This event is triggered when a unit levels up
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private static void Obj_AI_Base_OnLevelUp(Obj_AI_Base sender, Obj_AI_BaseLevelUpEventArgs args)
        {
            if (MiscMenu.GetCheckBoxValue("activateAutoLVL") && sender.IsMe)
            {
                var delay = MiscMenu.GetSliderValue("delaySlider");
                Core.DelayAction(LevelUPSpells, delay);

            }
        }

        /// <summary>
        /// It will level up the spell using the values of the comboboxes on the menu as a priority
        /// </summary>
        private static void LevelUPSpells()
        {
            if (Player.Instance.Spellbook.CanSpellBeUpgraded(SpellSlot.R))
            {
                Player.Instance.Spellbook.LevelSpell(SpellSlot.R);
            }

            if (Player.Instance.Spellbook.CanSpellBeUpgraded(GetSlotFromComboBox(MiscMenu.GetComboBoxValue("firstFocus"))))
            {
                Player.Instance.Spellbook.LevelSpell(GetSlotFromComboBox(MiscMenu.GetComboBoxValue("firstFocus")));
            }

            if (Player.Instance.Spellbook.CanSpellBeUpgraded(GetSlotFromComboBox(MiscMenu.GetComboBoxValue("secondFocus"))))
            {
                Player.Instance.Spellbook.LevelSpell(GetSlotFromComboBox(MiscMenu.GetComboBoxValue("secondFocus")));
            }

            if (Player.Instance.Spellbook.CanSpellBeUpgraded(GetSlotFromComboBox(MiscMenu.GetComboBoxValue("thirdFocus"))))
            {
                Player.Instance.Spellbook.LevelSpell(GetSlotFromComboBox(MiscMenu.GetComboBoxValue("thirdFocus")));
            }
        }

        /// <summary>
        /// It will transform the value of the combobox into a SpellSlot
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static SpellSlot GetSlotFromComboBox(this int value)
        {
            switch (value)
            {
                case 0:
                    return SpellSlot.Q;
                case 1:
                    return SpellSlot.W;
                case 2:
                    return SpellSlot.E;
            }
            Chat.Print("Failed getting slot");
            return SpellSlot.Unknown;
        }
    }
}