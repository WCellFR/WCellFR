﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WCell.Constants.Spells;
using WCell.Core.Initialization;
using WCell.RealmServer.Entities;
using WCell.RealmServer.Misc;
using WCell.RealmServer.Spells;
using WCell.RealmServer.Spells.Auras;

namespace WCell.Addons.Default.Spells.Priest
{
	public static class PriestFixes
	{
		[Initialization(InitializationPass.Second)]
		public static void FixPriest()
		{
			// only proc on kill that rewards xp or honor
			SpellLineId.PriestShadowSpiritTap.Apply(spell => spell.ProcTriggerFlags = ProcTriggerFlags.GainExperience);

			// Holy Inspiration can be proced when priest casts the given spells
			// TODO: Only cast on crit
			SpellLineId.PriestHolyInspiration.Apply(spell => spell.AddCasterProcSpells(
				SpellLineId.PriestFlashHeal,
				SpellLineId.PriestHeal,
				SpellLineId.PriestGreaterHeal,
				SpellLineId.PriestBindingHeal,
				SpellLineId.PriestDisciplinePenance,
				SpellLineId.PriestPrayerOfMending,
				SpellLineId.PriestPrayerOfHealing,
				SpellLineId.PriestHolyCircleOfHealing));

			// Mind Flay: Assault the target's mind with Shadow energy, causing ${$m3*3} Shadow damage over $d and slowing their movement speed by $s2%.
			SpellLineId.PriestShadowMindFlay.Apply(spell =>
			{
				var effect = spell.AddAuraEffect(AuraType.PeriodicDamage, ImplicitTargetType.SingleEnemy);
				effect.BasePoints = spell.Effects[2].BasePoints * 3;
				effect.Amplitude = spell.Effects[2].Amplitude;
			});

			// Shadow Weaving applies to caster and can also be proc'ed by Mind Flay
			SpellLineId.PriestShadowShadowWeaving.Apply(spell =>
			{
				var effect = spell.GetEffect(AuraType.AddTargetTrigger);
				effect.ImplicitTargetA = ImplicitTargetType.Self;
				effect.AddToEffectMask(SpellLineId.PriestShadowMindFlay);
			});

			// Dispersion also regenerates Mana
			SpellLineId.PriestShadowDispersion.Apply(spell =>
			{
				var effect = spell.AddPeriodicTriggerSpellEffect(SpellId.Dispersion_2, ImplicitTargetType.Self);
				effect.Amplitude = 1000;
			});


			SpellLineId.PriestShadowVampiricEmbrace.Apply(spell =>
			{
				// change Dummy to proc effect
				var effect = spell.Effects[0];
				effect.IsProc = true;
				effect.AuraEffectHandlerCreator = () => new AuraVampiricEmbracerHandler();

				// Set correct flags and set of spells to trigger the proc
				spell.ProcTriggerFlags = ProcTriggerFlags.SpellCast;
				spell.AddCasterProcSpells(
					SpellLineId.PriestShadowMindFlay,
					SpellLineId.PriestShadowWordPain,
					SpellLineId.PriestShadowWordDeath,
					SpellLineId.PriestMindBlast,
					SpellLineId.PriestManaBurn,
					SpellLineId.PriestDevouringPlague,
					SpellLineId.PriestShadowVampiricTouch,
					SpellLineId.PriestMindSear);
			});
		}
	}

	public class AuraVampiricEmbracerHandler : AuraEffectHandler
	{
		public override void OnProc(Unit target, IUnitAction action)
		{
			if (action is IDamageAction)
			{
				var dmgAction = ((IDamageAction)action);
				var healSelfAmount = (dmgAction.Damage * EffectValue) / 100;
				var healPartyAmount = healSelfAmount / 5f;
				Owner.Heal(Owner, healSelfAmount, SpellEffect);
			}
		}
	}
}
