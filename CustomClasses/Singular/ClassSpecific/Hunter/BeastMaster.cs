﻿using Singular.Dynamics;
using Singular.Helpers;
using Singular.Managers;
using Singular.Settings;
using Styx;
using Styx.Combat.CombatRoutine;
using TreeSharp;
using Action = TreeSharp.Action;

namespace Singular.ClassSpecific.Hunter
{
    public class BeastMaster
    {
        [Class(WoWClass.Hunter)]
        [Spec(TalentSpec.BeastMasteryHunter)]
        [Behavior(BehaviorType.Combat | BehaviorType.Pull)]
        [Context(WoWContext.All)]
        public static Composite CreateBeastMasterCombat()
        {
            return new PrioritySelector(
                new Decorator(
                    ret => !StyxWoW.Me.GotAlivePet,
                    new Action(ret => PetManager.CallPet(SingularSettings.Instance.Hunter.PetSlot))),
                Safers.EnsureTarget(),
                Movement.CreateMoveToLosBehavior(),
                Common.CreateHunterBackPedal(),
                Movement.CreateFaceTargetBehavior(),
                Helpers.Common.CreateAutoAttack(true),
                new Decorator(
                    ret => StyxWoW.Me.CurrentTarget.Distance < 35f,
                    Movement.CreateEnsureMovementStoppedBehavior()),
                Helpers.Common.CreateInterruptSpellCast(ret => StyxWoW.Me.CurrentTarget),
                //Intimidation
                Spell.Cast(
                    "Intimidation", ret => StyxWoW.Me.CurrentTarget.IsAlive && StyxWoW.Me.GotAlivePet &&
                                           (StyxWoW.Me.CurrentTarget.CurrentTarget == null || StyxWoW.Me.CurrentTarget.CurrentTarget == StyxWoW.Me)),
                // Always keep it up on our target!
                Spell.Buff("Hunter's Mark"),
                //Bestial Wrath
                Spell.Cast("Bestial Wrath", ret => StyxWoW.Me.CurrentTarget.IsAlive && StyxWoW.Me.GotAlivePet && !StyxWoW.Me.CurrentTarget.HasAura("Intimidation")),
                Common.CreateHunterTrapOnAddBehavior("Freezing Trap"),
                new Decorator(
                    ret => StyxWoW.Me.CurrentTarget.DistanceSqr < 5 * 5,
                    new PrioritySelector(
                        Common.CreateHunterTrapBehavior("Immolation Trap"),
                        Spell.BuffSelf("Disengage"),
                        Spell.Cast("Raptor Strike")
                        )),
                // Heal pet when below 70
                Spell.Cast(
                    "Mend Pet",
                    ret =>
                    StyxWoW.Me.GotAlivePet &&
                    (StyxWoW.Me.Pet.HealthPercent < 70 || (StyxWoW.Me.Pet.HappinessPercent < 90 && TalentManager.HasGlyph("Mend Pet"))) && !StyxWoW.Me.Pet.HasAura("Mend Pet")),
                Spell.Cast(
                    "Concussive Shot",
                    ret => StyxWoW.Me.CurrentTarget.CurrentTarget == null || StyxWoW.Me.CurrentTarget.CurrentTarget == StyxWoW.Me),
                //Rapid fire on elite 
                Spell.BuffSelf("Rapid Fire", ret => StyxWoW.Me.CurrentTarget.Elite),
                Spell.Buff("Serpent Sting"),
                // Ignore these two when our pet is raging
                Spell.Cast("Focus Fire", ret => !StyxWoW.Me.Pet.HasAura("Bestial Wrath")),
                Spell.Cast("Kill Shot", ret => !StyxWoW.Me.Pet.HasAura("Bestial Wrath")),
                // Basically, cast it whenever its up.
                Spell.Cast("Kill Command"),
                // Only really cast this when we need a sting refresh.
                Spell.Cast(
                    "Cobra Shot",
                    ret => StyxWoW.Me.CurrentTarget.HasAura("Serpent Sting") && StyxWoW.Me.CurrentTarget.Auras["Serpent Sting"].TimeLeft.TotalSeconds < 3),
                // Focus dump on arcane shot, unless our pet has bestial wrath, then we use it for better DPS
                Spell.Cast("Arcane Shot"),
                // For when we have no Focus
                Spell.Cast("Steady Shot"),
                Movement.CreateMoveToTargetBehavior(true, 35f)
                );
        }
    }
}
