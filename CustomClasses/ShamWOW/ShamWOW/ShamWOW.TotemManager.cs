﻿/*
 * NOTE:    DO NOT POST ANY MODIFIED VERSIONS OF THIS TO THE FORUMS.
 * 
 *          DO NOT UTILIZE ANY PORTION OF THIS COMBAT CLASS WITHOUT
 *          THE PRIOR PERMISSION OF AUTHOR.  PERMITTED USE MUST BE
 *          ACCOMPANIED BY CREDIT/ACKNOWLEDGEMENT TO ORIGINAL AUTHOR.
 * 
 * ShamWOW Shaman CC
 * 
 * Author:  Bobby53
 * 
 * See the ShamWOW.chm file for Help
 *D:\DEV\HB\ShamWOW\ShamWOW.TotemManager.cs
 */
#pragma warning disable 642

// #define SHOW_TOTEM_BAR

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using Styx;
using Styx.Helpers;
using Styx.Logic;
using Styx.Logic.Combat;
using Styx.Logic.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Bobby53
{
    enum TotemId
    {
        NONE = 0,

        STRENGTH_OF_EARTH_TOTEM = 8075,
        EARTHBIND_TOTEM = 2484,
        STONESKIN_TOTEM = 8071,
        TREMOR_TOTEM = 8143,
        EARTH_ELEMENTAL_TOTEM = 2062,
        STONECLAW_TOTEM = 5730,

        SEARING_TOTEM = 3599,
        FLAMETONGUE_TOTEM = 8227,
        MAGMA_TOTEM = 8190,
        FIRE_ELEMENTAL_TOTEM = 2894,

        HEALING_STREAM_TOTEM = 5394,
        MANA_SPRING_TOTEM = 5675,
        ELEMENTAL_RESISTANCE_TOTEM = 8184,
        TOTEM_OF_TRANQUIL_MIND = 87718,
        MANA_TIDE_TOTEM = 16190,

        WINDFURY_TOTEM = 8512,
        GROUNDING_TOTEM = 8177,
        WRATH_OF_AIR_TOTEM = 3738,
        SPIRIT_LINK_TOTEM = 98008,
    }

    class Totem
    {
        public int Slot { get; set; }
        public TotemId Id { get; set; }
        public WoWUnit unit { get; set; }
    }

    partial class Shaman
    {

        private static readonly Dictionary<TotemId, int> _dictTotemSlot = new Dictionary<TotemId, int>()
		{
			{   TotemId.STRENGTH_OF_EARTH_TOTEM,    TOTEM_EARTH     },
			{   TotemId.EARTHBIND_TOTEM,            TOTEM_EARTH     },
			{   TotemId.STONESKIN_TOTEM,            TOTEM_EARTH     },
			{   TotemId.TREMOR_TOTEM,               TOTEM_EARTH     },
			{   TotemId.EARTH_ELEMENTAL_TOTEM,      TOTEM_EARTH     },
			{   TotemId.STONECLAW_TOTEM,            TOTEM_EARTH     },

			{   TotemId.SEARING_TOTEM,              TOTEM_FIRE      },
			{   TotemId.FLAMETONGUE_TOTEM,          TOTEM_FIRE      },
			{   TotemId.MAGMA_TOTEM,                TOTEM_FIRE      },
			{   TotemId.FIRE_ELEMENTAL_TOTEM,       TOTEM_FIRE      },

			{   TotemId.HEALING_STREAM_TOTEM,       TOTEM_WATER     },
			{   TotemId.MANA_SPRING_TOTEM,          TOTEM_WATER     },
			{   TotemId.ELEMENTAL_RESISTANCE_TOTEM, TOTEM_WATER     },
			{   TotemId.TOTEM_OF_TRANQUIL_MIND,     TOTEM_WATER     },
			{   TotemId.MANA_TIDE_TOTEM,            TOTEM_WATER     },

			{   TotemId.WINDFURY_TOTEM,             TOTEM_AIR       },
			{   TotemId.GROUNDING_TOTEM,            TOTEM_AIR       },
			{   TotemId.WRATH_OF_AIR_TOTEM,         TOTEM_AIR       },
			{   TotemId.SPIRIT_LINK_TOTEM,          TOTEM_AIR       },
		};

        public const int TOTEM_FIRE = 1;
        public const int TOTEM_EARTH = 2;
        public const int TOTEM_WATER = 3;
        public const int TOTEM_AIR = 4;

        private static WoWPoint _ptTotems = new WoWPoint();
        private static Countdown _TotemCheckTimer = new Countdown();           // timer to delay checking for existing of totems
        public static bool TotemsWereSet { get; set;  }            // flag indicating whether any totems were cast

        public static TotemId[] _totemBar = new TotemId[5];  // 0=None, other=Spell Id
        public static Totem[] _totem = new Totem[5]
        {
            new Totem(),
            new Totem(),
            new Totem(),
            new Totem(),
            new Totem()
        };


        private bool TotemManagerUpdate()
        {
            int countTotems = 0;

            if ( !_TotemCheckTimer.Done)
                return false;

#if USE_OLD_SEARCH
            List<WoWUnit> totemList = (from o in ObjectManager.ObjectList
                   where o is WoWUnit
                   let unit = o.ToUnit()
                   where unit.CreatedByUnitGuid == _me.Guid
                    && unit.CreatureType == WoWCreatureType.Totem
                   select unit
                   ).ToList();

            foreach (WoWUnit totem in totemList)
            {
                int indexTotem = 0;
                try
                {
                    countTotems++;
                    indexTotem = _dictTotemSlot[(TotemId)totem.CreatedBySpellId];
                    _totem[indexTotem] = totem;
                    if ( totem.Distance < _ptTotems.Distance(_me.Location) )
                        _ptTotems = totem.Location;
                }
                catch
                {
                    Shaman.Wlog("NOTIFY SHAMWOW DEVELOPER:  Unknown totem: totem spell id={0}, totem name='{1}', index totem={2}", totem.CreatedBySpellId, totem.Name, indexTotem);
                }
            }
#else

            ResetTotemData();

            foreach (WoWTotemInfo ti in _me.Totems)
            {               
                if (ti.WoWTotem != WoWTotem.None)
                {
                    countTotems++;

                    try
                    {
                        WoWUnit totem = ObjectManager.GetObjectByGuid<WoWUnit>(ti.Guid);
                        int indexTotem = _dictTotemSlot[(TotemId)totem.CreatedBySpellId];

                        _totem[indexTotem].Slot = indexTotem;
                        _totem[indexTotem].unit = totem;
                        _totem[indexTotem].Id = (TotemId)totem.CreatedBySpellId;

                        if (totem.Distance < _ptTotems.Distance(_me.Location))
                            _ptTotems = totem.Location;
                    }
                    catch
                    {
                        if (ti.Spell.Id == 8137)
                            ;   //  its a 'Poison Extraction Totem' (quest item) and can be ignored
                        else
                            Shaman.Wlog("NOTIFY SHAMWOW DEVELOPER:  Unknown totem: totem spell id={0}, totem name='{1}'",  ti.Spell.Id, ti.Name );
                    }
                }
            }

            if ( countTotems > 0 )
            {
                TotemsWereSet = true;
            }

#endif

#if SHOW_TOTEM_COUNT
            if ( countTotems == 0 )
                Shaman.Dlog("TotemManagerUpdate:  found 0 totems");
            else
                Shaman.Dlog("TotemManagerUpdate:  found {0} totems with closest {1:F1} yds away at {2}", countTotems, _ptTotems.Distance(_me.Location), _ptTotems.ToString());
#endif

            return countTotems > 0;
        }

        private bool TotemExist(int indexTotem)
        {
            return _totem[indexTotem].Id != TotemId.NONE;
        }

        private static bool TotemExist(TotemId totemId)
        {
            int indexTotem = _dictTotemSlot[totemId];
            return _totem[indexTotem].Id == totemId;
        }

        public bool HasTotemSpell(TotemId t)
        {
            return SpellManager.HasSpell((int) t);
        }

        private bool TotemCast(TotemId idTotem)
        {
            bool castTotem = Safe_CastSpellIgnoreSilence(null, (int)idTotem);
            if (castTotem)
            {
                TotemsWereSet = true;
                _ptTotems = _me.Location;
                ResetTotemCountdown();

                int indexTotem = _dictTotemSlot[idTotem];
                _totem[indexTotem].Id = idTotem;
                _totem[indexTotem].Slot = indexTotem;
                _totem[indexTotem].unit = null;
            }

            return castTotem;
        }



        private bool TotemBarCast()
        {
            bool castTotem = Safe_CastSpellIgnoreSilence(null, "Call of the Elements" );
            if (castTotem)
            {
                TotemsWereSet = true;
                _ptTotems = _me.Location;
                ResetTotemCountdown();

                for (int indexTotem = 1; indexTotem <= 4; indexTotem++)
                {
                    _totem[indexTotem].Id = _totemBar[indexTotem];
                    _totem[indexTotem].Slot = indexTotem;
                    _totem[indexTotem].unit = null;
                }
            }

            return castTotem;
        }

        private void TotemDestroy(int indexTotem)
        {
            Shaman.RunLUA("DestroyTotem(" + indexTotem + ")");
            _totem[indexTotem].Id = TotemId.NONE ;
            _totem[indexTotem].Slot = 0;
            _totem[indexTotem].unit = null;
        }

        private bool TotemSelect(int indexTotem, string spellName)
        {
            int spellId = -1;
            bool bSelected = false;

            if (spellName.ToLower() == "none")
                spellId = 0;
            else if ( SpellManager.HasSpell( spellName ))
                spellId = SpellManager.Spells[spellName].Id;

            if ( -1 != spellId )
                bSelected = TotemSelect(indexTotem, (TotemId) spellId );

            return bSelected;
        }

        private bool TotemSelect(int indexTotem, TotemId spellId)
        {
            string sLua;

            _totemBar[indexTotem] = spellId;
            // Shaman.Dlog("Totem Bar:  called with #{0} to '{1}'", indexTotem, sSpell);

            if (!SpellManager.HasSpell("Call of the Elements"))
                return false;

            if (spellId == 0)
            {
                Shaman.Dlog("Totem Bar:  removing slot #{0}", indexTotem);
                sLua = String.Format("SetMultiCastSpell({0})", 132 + indexTotem);
                Shaman.RunLUA(sLua);
                StyxWoW.SleepForLagDuration();
                return true;
            }

            if (!HasTotemSpell(spellId))
            {
                Shaman.Dlog("Totem Bar:  cannot set slot #{0} to unknown spell id '{1}'", indexTotem, spellId);
                return false;
            }

            sLua = String.Format("SetMultiCastSpell({0}, {1})", 132 + indexTotem, (int)spellId);
            Shaman.RunLUA(sLua);
            StyxWoW.SleepForLagDuration();

            return true;
        }

        private void TotemSetupBar()
        {
            string[] aBar = new string[5];
            if (IsPVP() )
            {
                aBar[TOTEM_EARTH] = Shaman.cfg.PVP_TotemEarth;
                aBar[TOTEM_FIRE] = Shaman.cfg.PVP_TotemFire;
                aBar[TOTEM_WATER] = Shaman.cfg.PVP_TotemWater;
                aBar[TOTEM_AIR] = Shaman.cfg.PVP_TotemAir;
            }
            else if (Shaman.IsRAF())
            {
                aBar[TOTEM_EARTH] = Shaman.cfg.RAF_TotemEarth;
                aBar[TOTEM_FIRE] = Shaman.cfg.RAF_TotemFire;
                aBar[TOTEM_WATER] = Shaman.cfg.RAF_TotemWater;
                aBar[TOTEM_AIR] = Shaman.cfg.RAF_TotemAir;
            }
            else
            {
                aBar[TOTEM_EARTH] = Shaman.cfg.PVE_TotemEarth;
                aBar[TOTEM_FIRE] = Shaman.cfg.PVE_TotemFire;
                aBar[TOTEM_WATER] = Shaman.cfg.PVE_TotemWater;
                aBar[TOTEM_AIR] = Shaman.cfg.PVE_TotemAir;
            }
            /*
                        Shaman.Dlog("value from cfgdlg:  Earth - {0}", _totemBar[TOTEM_EARTH]);
                        Shaman.Dlog("value from cfgdlg:  Fire  - {0}", _totemBar[TOTEM_FIRE ]);
                        Shaman.Dlog("value from cfgdlg:  Water - {0}", _totemBar[TOTEM_WATER]);
                        Shaman.Dlog("value from cfgdlg:  Air   - {0}", _totemBar[TOTEM_AIR  ]);
            */
            // Earth - if configured spell is None or a real spellname, select that
            if (0 != String.Compare("Auto", aBar[TOTEM_EARTH], true))
                TotemSelect(TOTEM_EARTH, aBar[TOTEM_EARTH]);
            else if (Shaman.typeShaman == Shaman.ShamanType.Enhance && HasTotemSpell(TotemId.STRENGTH_OF_EARTH_TOTEM))
                TotemSelect(TOTEM_EARTH, TotemId.STRENGTH_OF_EARTH_TOTEM);
            else if (IsRAF() && HasTotemSpell(TotemId.STRENGTH_OF_EARTH_TOTEM))
                TotemSelect(TOTEM_EARTH, TotemId.STRENGTH_OF_EARTH_TOTEM);
            else if (HasTotemSpell(TotemId.STONESKIN_TOTEM))
                TotemSelect(TOTEM_EARTH, TotemId.STONESKIN_TOTEM);
            else
                TotemSelect(TOTEM_EARTH, "None");

            // Fire - if configured spell is None or a real spellname, select that
            if (0 != String.Compare("Auto", aBar[TOTEM_FIRE], true))
                TotemSelect(TOTEM_FIRE, aBar[TOTEM_FIRE]);
            else if (HasTotemSpell(TotemId.FLAMETONGUE_TOTEM) && (Shaman.typeShaman == Shaman.ShamanType.Resto || Shaman.IsHealerOnly()))
                TotemSelect(TOTEM_FIRE, TotemId.FLAMETONGUE_TOTEM);
            else if (HasTotemSpell(TotemId.SEARING_TOTEM))
                TotemSelect(TOTEM_FIRE, TotemId.SEARING_TOTEM);
            else
                TotemSelect(TOTEM_FIRE, "None");

            // Water - if configured spell is None or a real spellname, select that
            if (0 != String.Compare("Auto", aBar[TOTEM_WATER], true))
                TotemSelect(TOTEM_WATER, aBar[TOTEM_WATER]);
            else if (HasTotemSpell(TotemId.HEALING_STREAM_TOTEM) && Shaman.IsRAF())
                TotemSelect(TOTEM_WATER, TotemId.HEALING_STREAM_TOTEM);
            else if (_hasGlyphOfHealingStreamTotem && IsPVP())
                TotemSelect(TOTEM_WATER, TotemId.HEALING_STREAM_TOTEM);
            else if (HasTotemSpell(TotemId.ELEMENTAL_RESISTANCE_TOTEM) && IsPVP())
                TotemSelect(TOTEM_WATER, TotemId.ELEMENTAL_RESISTANCE_TOTEM);
            else if (HasTotemSpell(TotemId.HEALING_STREAM_TOTEM))
                TotemSelect(TOTEM_WATER, TotemId.HEALING_STREAM_TOTEM);
            else if (HasTotemSpell(TotemId.MANA_SPRING_TOTEM))
                TotemSelect(TOTEM_WATER, TotemId.MANA_SPRING_TOTEM);
            else
                TotemSelect(TOTEM_WATER, "None");

            // Air - if configured spell is None or a real spellname, select that
            if (0 != String.Compare("Auto", aBar[TOTEM_AIR], true))
                TotemSelect(TOTEM_AIR, aBar[TOTEM_AIR]);
            else if (HasTotemSpell(TotemId.GROUNDING_TOTEM) && Shaman.typeShaman == Shaman.ShamanType.Resto)
                TotemSelect(TOTEM_AIR, TotemId.GROUNDING_TOTEM);
            else if (HasTotemSpell(TotemId.WINDFURY_TOTEM) && Shaman.typeShaman == Shaman.ShamanType.Enhance)
                TotemSelect(TOTEM_AIR, TotemId.WINDFURY_TOTEM);
            else if (HasTotemSpell(TotemId.WRATH_OF_AIR_TOTEM) && (Shaman.typeShaman == Shaman.ShamanType.Elemental || Shaman.typeShaman == Shaman.ShamanType.Resto))
                TotemSelect(TOTEM_AIR, TotemId.WRATH_OF_AIR_TOTEM);
            else if (IsRAF() && HasTotemSpell(TotemId.WINDFURY_TOTEM))
                TotemSelect(TOTEM_AIR, TotemId.WINDFURY_TOTEM);
            else if (IsRAF() && HasTotemSpell(TotemId.GROUNDING_TOTEM))
                TotemSelect(TOTEM_AIR, TotemId.GROUNDING_TOTEM);
            else
                TotemSelect(TOTEM_AIR, "None");

            Shaman.Slog("Totem Bar[Earth]: {0}", _totemBar[TOTEM_EARTH]);
            Shaman.Slog("Totem Bar[Fire ]: {0}", _totemBar[TOTEM_FIRE]);
            Shaman.Slog("Totem Bar[Water]: {0}", _totemBar[TOTEM_WATER]);
            Shaman.Slog("Totem Bar[Air  ]: {0}", _totemBar[TOTEM_AIR]);

        }


        /// <summary>
        /// SetTotemsAsNeeded() - manages casting totems as called for by environment and
        /// user configuration values. This code uses:
        ///     http://www.yawb.info/2009/08/25/learning-about-totems-tips-to-aid-your-growing-shamanism/
        /// as a guideline for totem usage while leveling.
        /// </summary>
        private bool SetTotemsAsNeeded()
        {
            bool castTotem = false;
            bool foundPlayerNeedsTremor = false;
            bool foundPlayerNeedsEarthbind = false;

            try
            {
                if ((_me.IsMoving && !IsFleeing(_me)) || Shaman.cfg.FarmingLowLevel)
                    return false;

                if ( UserDisabledTotems())
                    return false;

                if (TotemsWereSet && !_TotemCheckTimer.Done && !IsFleeing(_me))
                    return false;

                if (TotemsWereSet && _ptTotems.Distance(_me.Location) > Shaman.cfg.DistanceForTotemRecall)
                {
                    Shaman.Dlog("Recalling Totems that were set {0:F1} yds away at point {1}", _ptTotems.Distance(_me.Location), _ptTotems.ToString());
                    RecallTotemsForMana();
                }

                ResetTotemCountdown();

                //-----
                // if you make it past the following gate, only limited tests are needed for each totem
                //-----
                if (IsPVP() || Shaman.IsRAF())
                    ;
                else if (Shaman.IsFightStressful() || !Shaman.cfg.PVE_SaveForStress_TotemsSelected)
                    ;
                else
                {                   
                    Shaman.Dlog("not setting totems until a stressful situation");
                    return false;
                }

                // check which totems exist
                bool bAnyTotemsUp = TotemExist(TOTEM_EARTH)
                                || TotemExist(TOTEM_FIRE)
                                || TotemExist(TOTEM_WATER)
                                || TotemExist(TOTEM_AIR);

                Shaman.Dlog("SetTotemsAsNeeded:  earth: " + BoolToYN(TotemExist(TOTEM_EARTH)) + "  fire: " + BoolToYN(TotemExist(TOTEM_FIRE)) + "  water: " + BoolToYN(TotemExist(TOTEM_WATER)) + "  air: " + BoolToYN(TotemExist(TOTEM_AIR)));

                TotemsWereSet = bAnyTotemsUp;  // TotemsWereSet || removed because only matters if they exist

                // Check if we need an effect counter move 
                //////////////////////////////////////////////////////////////////////
                WoWUnit playerNeedsTremor = null;
                WoWUnit playerNeedsEarthbind = null;

                if (Shaman.NeedsTremor(_me))
                    playerNeedsTremor = _me;
                else if ( InGroup() )
                    playerNeedsTremor = (from p in GroupMembers where p.Distance < 30 && NeedsTremor(p) select p).FirstOrDefault();

                if (playerNeedsTremor != null)
                {
                    foundPlayerNeedsTremor = SpellManager.HasSpell((int) TotemId.TREMOR_TOTEM) && SpellManager.CanCast((int) TotemId.TREMOR_TOTEM);
                    if (foundPlayerNeedsTremor)
                        Shaman.Slog("^Tremor Totem:  needed for fear on {0} @ {1:F1} yds", Safe_UnitName(playerNeedsTremor), playerNeedsTremor.Distance);
                    else
                        Shaman.Dlog("Tremor Totem:  on cooldown, too bad for {0} @ {1:F1} yds", Safe_UnitName(playerNeedsTremor), playerNeedsTremor.Distance);
                }
                else 
                {
                    if (!_hasTalentEarthenPower)
                        ;
                    else if (Shaman.IsSnared(_me))
                        playerNeedsEarthbind = _me;
                    else if (InGroup())
                        playerNeedsEarthbind = (from p in GroupMembers where p.Distance < 10 && IsSnared(p) select p).FirstOrDefault();

                    if (playerNeedsEarthbind != null)
                    {
                        foundPlayerNeedsEarthbind = SpellManager.HasSpell((int) TotemId.EARTHBIND_TOTEM) && SpellManager.CanCast((int) TotemId.EARTHBIND_TOTEM );
                        if (foundPlayerNeedsEarthbind)
                            Shaman.Slog("^Earthbind Totem:  needed for snare on {0} @ {1:F1} yds", Safe_UnitName(playerNeedsEarthbind), playerNeedsEarthbind.Distance);
                        else
                        {
                            Shaman.Dlog("Earthbind Totem:  on cooldown, too bad for {0} @ {1:F1} yds", Safe_UnitName(playerNeedsEarthbind), playerNeedsEarthbind.Distance);
                        }
                    }
                }


                // Totem Bar Set
                // -- add handling for changing totem bar setup temporarily if needed for tremor totem
                //////////////////////////////////////////////////////////////////////
                const string totemBar = "Call of the Elements";
                if (!bAnyTotemsUp && SpellManager.HasSpell(totemBar) && (playerNeedsTremor == null || !playerNeedsTremor.IsMe ))
                {
                    TotemId saveEarthTotemSetup = TotemId.TREMOR_TOTEM;
                    TotemId saveFireTotemSetup = TotemId.MAGMA_TOTEM;

                    // if mobs that fear are found and tremor not in bar setup already, add temporarily just for cast
                    if (foundPlayerNeedsTremor && _totemBar[TOTEM_EARTH] != TotemId.TREMOR_TOTEM)
                    {
                        Slog("^Tremor Totem:  temporarily set with {0}", totemBar);
                        saveEarthTotemSetup = _totemBar[TOTEM_EARTH];
                        TotemSelect(TOTEM_EARTH, TotemId.TREMOR_TOTEM);
                    }

                    if (_count8YardEnemy >= 3 && !IsHealerOnly())
                    {
                        Slog("^Magma Totem:  temporarily set with {0}", totemBar);
                        saveFireTotemSetup = _totemBar[TOTEM_FIRE];
                        TotemSelect(TOTEM_FIRE, TotemId.MAGMA_TOTEM);
                    }

                    if ( TotemBarCast())
                    {
                        castTotem = true;
                        TotemsWereSet = true;
                        _ptTotems = _me.Location;

                        if (IsRAFandTANK())
                            Dlog("SetTotemsAsNeeded: set totems at <{0}> {1:F1} yds from Leader", _ptTotems.ToString(), GroupTank.Distance);
                        else
                            Dlog("SetTotemsAsNeeded: set totems at <{0}>", _ptTotems.ToString());
                    }
                    
                    // if we changed the earth totem on bar, restore back to configured value
                    if (saveEarthTotemSetup != TotemId.TREMOR_TOTEM)
                        TotemSelect(TOTEM_EARTH, saveEarthTotemSetup);

                    if (saveFireTotemSetup != TotemId.MAGMA_TOTEM)
                        TotemSelect(TOTEM_FIRE, saveFireTotemSetup);
                }
                else
                {
                    // CC Break Totems are Highest Priority
                    // These also are cast to overwrite existing Earth Totems
                    //////////////////////////////////////////////////////////////////////
                    if (foundPlayerNeedsTremor && !TotemExist(TotemId.TREMOR_TOTEM) && HasTotemSpell(TotemId.TREMOR_TOTEM) )
                    {
                        castTotem = castTotem || TotemCast(TotemId.TREMOR_TOTEM);
                    }

                    if (!castTotem && foundPlayerNeedsEarthbind && !TotemExist(TotemId.TREMOR_TOTEM) && !TotemExist(TotemId.EARTHBIND_TOTEM))
                    {
                        castTotem = castTotem || TotemCast(TotemId.EARTHBIND_TOTEM );
                    }

                    // Fire Totems
                    //////////////////////////////////////////////////////////////////////
                    if (!IsHealerOnly())
                    {
                        if (_count8YardEnemy >= 3)
                        {
                            if (!TotemExist(TotemId.MAGMA_TOTEM) && !TotemExist(TotemId.FIRE_ELEMENTAL_TOTEM))
                            {
                                if (HasTotemSpell(TotemId.MAGMA_TOTEM))
                                {
                                    castTotem = castTotem || TotemCast(TotemId.MAGMA_TOTEM);
                                }
                            }
                        }
                        else
                        {
                            if (TotemExist(TotemId.MAGMA_TOTEM) && (Safe_IsElite(_me.CurrentTarget) || countEnemy > 1))
                            {
                                if (HasTotemSpell(TotemId.SEARING_TOTEM))
                                {
                                    castTotem = castTotem || TotemCast(TotemId.SEARING_TOTEM);
                                }
                            }
                        }
                    }

                    if (0 != _totemBar[TOTEM_FIRE] && !TotemExist(TOTEM_FIRE))
                    {
                        if (!TotemExist(TOTEM_FIRE) && HasTotemSpell(_totemBar[TOTEM_FIRE]))
                            castTotem = castTotem || TotemCast(_totemBar[TOTEM_FIRE]);
                        if (!TotemExist(TOTEM_FIRE) && Shaman.typeShaman != Shaman.ShamanType.Resto && HasTotemSpell(TotemId.SEARING_TOTEM))
                            castTotem = castTotem || TotemCast(TotemId.SEARING_TOTEM);
                        if (!TotemExist(TOTEM_FIRE) && HasTotemSpell(TotemId.FLAMETONGUE_TOTEM))
                            castTotem = castTotem || TotemCast(TotemId.FLAMETONGUE_TOTEM);
                    }

                    // Earth Totems First
                    //////////////////////////////////////////////////////////////////////
                    // if (foundMobsThatFear && HasTotemSpell(TotemId.TREMOR_TOTEM) && TotemId.TREMOR_TOTEM != (TotemId)_totem[TOTEM_EARTH].CreatedBySpellId)
                    if (!castTotem && 0 != _totemBar[TOTEM_EARTH])
                    {
                        if (!TotemExist(TOTEM_EARTH) && HasTotemSpell(_totemBar[TOTEM_EARTH]))
                            castTotem = castTotem || TotemCast(_totemBar[TOTEM_EARTH]);

                        if (!TotemExist(TOTEM_EARTH) && Shaman.typeShaman == Shaman.ShamanType.Enhance && HasTotemSpell(TotemId.STRENGTH_OF_EARTH_TOTEM))
                            castTotem = castTotem || TotemCast(TotemId.STRENGTH_OF_EARTH_TOTEM);

                        if (!TotemExist(TOTEM_EARTH) && Shaman.typeShaman != Shaman.ShamanType.Enhance && HasTotemSpell(TotemId.STONESKIN_TOTEM))
                            castTotem = castTotem || TotemCast(TotemId.STONESKIN_TOTEM);
                    }

                    // Water Totems
                    //////////////////////////////////////////////////////////////////////
                    if (!castTotem && 0 != _totemBar[TOTEM_WATER])
                    {
                        if (!TotemExist(TOTEM_WATER) && HasTotemSpell(_totemBar[TOTEM_WATER]))
                            castTotem = castTotem || TotemCast(_totemBar[TOTEM_WATER]);

                        if (!TotemExist(TOTEM_WATER) && HasTotemSpell(TotemId.MANA_SPRING_TOTEM))
                            castTotem = castTotem || TotemCast(TotemId.MANA_SPRING_TOTEM);
                    }

                    // Air Totems
                    //////////////////////////////////////////////////////////////////////
                    if (!castTotem && 0 != _totemBar[TOTEM_AIR])
                    {
                        if (!TotemExist(TOTEM_AIR) && HasTotemSpell(_totemBar[TOTEM_WATER]))
                            castTotem = castTotem || TotemCast(_totemBar[TOTEM_AIR]);
                        if (!TotemExist(TOTEM_AIR) && Shaman.typeShaman == Shaman.ShamanType.Resto && HasTotemSpell(TotemId.GROUNDING_TOTEM))
                            castTotem = castTotem || TotemCast(TotemId.GROUNDING_TOTEM);
                        if (!TotemExist(TOTEM_AIR) && Shaman.typeShaman == Shaman.ShamanType.Enhance && HasTotemSpell(TotemId.WINDFURY_TOTEM))
                            castTotem = castTotem || TotemCast(TotemId.WINDFURY_TOTEM);
                        if (!TotemExist(TOTEM_AIR) && Shaman.typeShaman == Shaman.ShamanType.Elemental && HasTotemSpell(TotemId.WRATH_OF_AIR_TOTEM))
                            castTotem = castTotem || TotemCast(TotemId.WRATH_OF_AIR_TOTEM);
                    }

                    TotemsWereSet = TotemsWereSet || TotemExist(TOTEM_EARTH) || TotemExist(TOTEM_FIRE) || TotemExist(TOTEM_WATER) || TotemExist(TOTEM_AIR);

                    if (!bAnyTotemsUp && TotemsWereSet)
                        _ptTotems = _me.Location;
                }
            }
            catch (ThreadAbortException) { throw; }
            catch (Exception e)
            {
                Log(Color.Red, "An Exception occured. Check debug log for details.");
                Logging.WriteDebug("HB EXCEPTION in SetTotemsAsNeeded()");
                Logging.WriteException(e);
            }

            // would like to remove this code, but seeing duplicate totem sets
            if (castTotem)
            {
                ObjectManager.Update();
                TotemManagerUpdate();
            }

            return castTotem;
        }

        /*
         * will replenish selected totem bar slot totem only if totems have already been cast for this fight
         * and no totem for that slot exists. 
         */
        public bool ReplenishTotem( int idxTotem)
        {
            bool castTotem = false;

            if (TotemsWereSet)
            {
                if (TotemId.NONE != _totemBar[idxTotem] && !TotemExist(idxTotem) && HasTotemSpell(_totemBar[idxTotem]))
                {
                    castTotem = TotemCast(_totemBar[idxTotem]);
                }
            }

            return castTotem;
        }

        private double CheckDistanceForRecall()
        {
            double check = Shaman.cfg.DistanceForTotemRecall;
            if (Shaman.cfg.PVE_PullType == ConfigValues.TypeOfPull.Ranged || Shaman.typeShaman != Shaman.ShamanType.Enhance)
            {
                check += _offsetForRangedPull * 0.75;
            }

            return check;
        }

        public void RecallTotemsForMana()
        {
            if (TotemsWereSet)
            {
                if (UserDisabledTotems())
                    return;

                if (SpellManager.HasSpell("Totemic Recall"))
                {
                    WaitForCurrentCastOrGCD();
                    while (!IsGameUnstable() && _me.IsAlive && !SpellManager.CanCast("Totemic Recall"))
                    {
                        Dlog("RecallTotemsForMana:  unable to cast Totemic Recall, so waiting");
                    }

                    Safe_CastSpell("Totemic Recall", SpellRange.NoCheck, SpellWait.Complete);
                }
                else
                {
                    TotemsWereSet = false;

                    if (TotemExist(TOTEM_FIRE))
                        TotemDestroy(TOTEM_FIRE);
                    if (TotemExist(TOTEM_EARTH))
                        TotemDestroy(TOTEM_EARTH);
                    if (TotemExist(TOTEM_WATER))
                        TotemDestroy(TOTEM_WATER);
                    if (TotemExist(TOTEM_AIR))
                        TotemDestroy(TOTEM_AIR);
                }

                ResetTotemData();
                ResetTotemCountdown();
            }
        }

        public void ResetTotemData()
        {
            // reset all data
            TotemsWereSet = false;
            _ptTotems = new WoWPoint();

            for (int indexTotem = 1; indexTotem <= 4; indexTotem++)
            {
                _totem[indexTotem] = new Totem();
            }
        }

        public void ResetTotemCountdown()
        {
            _TotemCheckTimer = new Countdown(750);
        }

        public bool UserDisabledTotems()
        {
            return (TotemId.NONE == _totemBar[TOTEM_EARTH] && TotemId.NONE == _totemBar[TOTEM_FIRE] && TotemId.NONE == _totemBar[TOTEM_WATER] && TotemId.NONE == _totemBar[TOTEM_AIR]);
        }
    }
}
