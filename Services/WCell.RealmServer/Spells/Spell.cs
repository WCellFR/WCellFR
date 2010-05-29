/*************************************************************************
 *
 *   file		: Spell.cs
 *   copyright		: (C) The WCell Team
 *   email		: info@wcell.org
 *   last changed	: $LastChangedDate: 2010-04-23 15:13:50 +0200 (fr, 23 apr 2010) $
 *   last author	: $LastChangedBy: dominikseifert $
 *   revision		: $Rev: 1282 $
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 *************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using WCell.Constants;
using WCell.Constants.Items;
using WCell.Constants.Skills;
using WCell.Constants.Spells;
using WCell.RealmServer.Entities;
using WCell.RealmServer.Items;
using WCell.RealmServer.Misc;
using WCell.RealmServer.Modifiers;
using WCell.RealmServer.Skills;
using WCell.RealmServer.Spells.Auras;
using WCell.RealmServer.Talents;
using WCell.Util;
using WCell.Util.Data;
using System.Text.RegularExpressions;
using WCell.Util.Graphics;

namespace WCell.RealmServer.Spells
{
	/// <summary>
	/// Represents any spell action or aura
	/// </summary>
	[DataHolder(RequirePersistantAttr = true)]
	public partial class Spell : IDataHolder, ISpellGroup
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="spell">The special Spell being casted</param>
		/// <param name="caster">The caster casting the spell</param>
		/// <param name="target">The target that the Caster selected (or null)</param>
		/// <param name="targetPos">The targetPos that was selected (or 0,0,0)</param>
		public delegate void SpecialCastHandler(Spell spell, WorldObject caster, WorldObject target, ref Vector3 targetPos);

		/// <summary>
		/// This Range will be used for all Spells that have MaxRange = 0
		/// </summary>
		public static int DefaultSpellRange = 30;

		private static readonly Regex numberRegex = new Regex(@"\d+");

		private static Logger log = LogManager.GetCurrentClassLogger();

		public static readonly Spell[] EmptyArray = new Spell[0];

		#region Harmful SpellEffects
		//public static readonly HashSet<SpellEffectType> HarmfulSpellEffects = new Func<HashSet<SpellEffectType>>(() => {
		//    var effects = new HashSet<SpellEffectType>();

		//    effects.Add(SpellEffectType.Attack);
		//    effects.Add(SpellEffectType.AttackMe);
		//    effects.Add(SpellEffectType.DestroyAllTotems);
		//    effects.Add(SpellEffectType.Dispel);
		//    effects.Add(SpellEffectType.Attack);

		//    return effects;
		//})();
		#endregion

		#region Auto generated Spell Fields
		/// <summary>
		/// whether this is a Combat ability that will be triggered on next weapon strike (like Heroic Strike etc)
		/// </summary>
		public bool IsOnNextStrike;

		/// <summary>
		/// whether this is an ability involving any kind of weapon-attack
		/// </summary>
		public bool IsWeaponAbility;

		/// <summary>
		/// Whether this can trigger an instant Strike
		/// </summary>
		public bool IsStrikeSpell;

		/// <summary>
		/// whether this is actually a passive buff
		/// </summary>
		public bool IsPassive;

		/// <summary>
		/// Whether this is a ranged attack (includes wands)
		/// </summary>
		public bool IsRangedAbility;

		/// <summary>
		/// whether this is a throw (used for any kind of throwing weapon)
		/// </summary>
		public bool IsThrow;

		/// <summary>
		/// whether this is an actual SpellCaster spell
		/// </summary>
		public bool IsProfession;

		/// <summary>
		/// whether this teaches the initial Profession
		/// </summary>
		public bool TeachesApprenticeAbility;

		/// <summary>
		/// whether this is teaching another spell
		/// </summary>
		public bool IsTeachSpell;

		public bool HasIndividualCooldown;

		/// <summary>
		/// Tame Beast (Id: 1515) amongst others
		/// </summary>
		public bool IsTame
		{
            get { return AttributesExB.HasFlag(SpellAttributesExB.TamePet); }
		}

		/// <summary>
		/// Tame Beast (Id: 13481) amongst others
		/// </summary>
		public bool IsTameEffect;

		/// <summary>
		/// Fishing spawns a FishingNode which needs to be removed upon canceling
		/// </summary>
		public bool IsFishing;

		/// <summary>
		/// The spell which teaches this spell (if any)
		/// </summary>
		public Spell LearnSpell;

		/// <summary>
		/// whether Spell's effects don't wear off when dead
		/// </summary>
		public bool PersistsThroughDeath
		{
            get { return AttributesExC.HasFlag(SpellAttributesExC.PersistsThroughDeath); }
		}

		/// <summary>
		/// whether this spell is triggered by another one
		/// </summary>
		public bool IsTriggeredSpell;

		/// <summary>
		/// whether its a food effect
		/// </summary>
		public bool IsFood
		{
			get { return Category == 11; }
		}

		/// <summary>
		/// whether its a drink effect
		/// </summary>
		public bool IsDrink
		{
			get { return Category == 59; }
		}

		/// <summary>
		/// ChainTargets are stored per effect in DBCs, but actually apply to all effects with the same
		/// ImplicitTargetTypes within the Spell
		/// </summary>
		public int ChainTargets;

		/// <summary>
		/// Indicates whether this Spell has at least one harmful effect
		/// </summary>
		public bool HasHarmfulEffects;

		/// <summary>
		/// Indicates whether this Spell has at least one beneficial effect
		/// </summary>
		public bool HasBeneficialEffects;


		public HarmType HarmType;

		/// <summary>
		/// The SpellEffect of this Spell that represents a PersistentAreaAura and thus a DO (or null if it has none)
		/// </summary>
		public SpellEffect DOEffect;

		/// <summary>
		/// whether this is a Rejuvenation or Regrowth effect
		/// </summary>
		public bool IsRejuvenationOrRegrowth;

		/// <summary>
		/// whether this is a Heal-spell
		/// </summary>
		public bool IsHealSpell;

		/// <summary>
		/// Whether this is a weapon ability that attacks with both weapons
		/// </summary>
		public bool IsDualWieldAbility;

		/// <summary>
		/// whether this is a Skinning-Spell
		/// </summary>
		public bool IsSkinning;

		/// <summary>
		/// If this is set for Spells, they will not be casted in the usual manner but instead this Handler will be called.
		/// </summary>
		public SpecialCastHandler SpecialCast;

		/// <summary>
		/// The Talent which this Spell represents one Rank of (every Talent Rank is represented by one Spell)
		/// </summary>
		public TalentEntry Talent;

		private SkillAbility m_Ability;

		/// <summary>
		/// The SkillAbility that this Spell represents
		/// </summary>
		public SkillAbility Ability
		{
			get { return m_Ability; }
			internal set
			{
				m_Ability = value;
				if (value != null && ClassId == 0)
				{
					var clss = Ability.ClassMask.GetIds();
					if (clss.Length == 1)
					{
						ClassId = clss[0];
					}
				}
			}
		}

		/// <summary>
		/// The id of the Skill that this Spell represents (if any)
		/// </summary>
		public SkillId SkillId;

		/// <summary>
		/// The Skill that this Spell represents (if any; is set during Initialization of Skills)
		/// </summary>
		public SkillLine Skill;

		/// <summary>
		/// Tools that are required by this spell (is set during Initialization of Items)
		/// </summary>
		public ItemTemplate[] RequiredTools;

		/// <summary>
		/// The SpellLine this Spell belongs to (or null)
		/// </summary>
		public SpellLine SpellLine;

		public Spell NextRank, PreviousRank;

		/// <summary>
		/// Indicates whether this Spell has any targets at all
		/// </summary>
		public bool HasTargets;

		/// <summary>
		/// Indicates whether this Spell has at least one effect on the caster
		/// </summary>
		public bool CasterIsTarget;

		/// <summary>
		/// Indicates whether this Spell teleports the Uni back to its bound location
		/// </summary>
		public bool IsHearthStoneSpell;

		public bool IsAreaSpell;

		public bool IsDamageSpell;

		public SpellEffect TotemEffect;

		public SpellEffect[] ProcTriggerEffects;

		public EquipmentSlot EquipmentSlot;

		public bool IsFinishingMove;

		public bool ReqDeadTarget;

		/// <summary>
		/// whether this is a channel-spell
		/// </summary>
		public bool IsChanneled;

		public int ChannelAmplitude;

		public bool RequiresCasterOutOfCombat;

		public bool CostsMana;

		/// <summary>
		/// Auras with modifier effects require existing Auras to be re-evaluated
		/// </summary>
		public bool HasModifierEffects;

		/// <summary>
		/// All affecting masks of all Effects
		/// </summary>
		public uint[] AllAffectingMasks = new uint[3];

		public bool HasManaShield;

		public bool IsEnhancer;

		private bool inited;

		public SpellLine Line;
		#endregion

		#region Spell Variables (that may be modified by spell customizations)
		public bool CanCastOnPlayer = true;

		/// <summary>
		/// Whether this is a Spell that is only used to prevent other Spells (cannot be cancelled etc)
		/// </summary>
		public bool IsPreventionDebuff;

		/// <summary>
		/// Whether this is an Aura that can override other instances of itself if they have the same rank (true by default)
		/// </summary>
		public bool CanOverrideEqualAuraRank = true;

		/// <summary>
		/// Spells casted whenever this Spell is casted
		/// </summary>
		public Spell[] TargetTriggerSpells, CasterTriggerSpells;

		/// <summary>
		/// Set of specific Spells which, when used, can proc this Spell.
		/// </summary>
		public HashSet<Spell> CasterProcSpells;

		/// <summary>
		/// Set of specific Spells which can proc this Spell on their targets.
		/// </summary>
		public HashSet<Spell> TargetProcSpells;

		/// <summary>
		/// ProcHandlers to be added to the caster of this Spell
		/// </summary>
		public List<ProcHandlerTemplate> CasterProcHandlers;

		/// <summary>
		/// ProcHandlers to be added to the targets of this Spell
		/// </summary>
		public List<ProcHandlerTemplate> TargetProcHandlers;
		#endregion

		#region Trigger Spells
		/// <summary>
		/// Add Spells to be casted on the targets of this Spell
		/// </summary>
		public void AddTargetTriggerSpells(params SpellId[] spellIds)
		{
			var spells = new Spell[spellIds.Length];
			for (var i = 0; i < spellIds.Length; i++)
			{
				var id = spellIds[i];
				var spell = SpellHandler.Get(id);
				if (spell == null)
				{
					throw new ArgumentException("Invalid SpellId: " + id);
				}
				spells[i] = spell;
			}
			AddTargetTriggerSpells(spells);
		}

		/// <summary>
		/// Add Spells to be casted on the targets of this Spell
		/// </summary>
		public void AddTargetTriggerSpells(params Spell[] spells)
		{
			if (TargetTriggerSpells == null)
			{
				TargetTriggerSpells = spells;
			}
			else
			{
				var oldLen = TargetTriggerSpells.Length;
				Array.Resize(ref TargetTriggerSpells, oldLen + spells.Length);
				Array.Copy(spells, 0, TargetTriggerSpells, oldLen, spells.Length);
			}
		}

		/// <summary>
		/// Add Spells to be casted on the targets of this Spell
		/// </summary>
		public void AddCasterTriggerSpells(params SpellId[] spellIds)
		{
			var spells = new Spell[spellIds.Length];
			for (var i = 0; i < spellIds.Length; i++)
			{
				var id = spellIds[i];
				var spell = SpellHandler.Get(id);
				if (spell == null)
				{
					throw new ArgumentException("Invalid SpellId: " + id);
				}
				spells[i] = spell;
			}
			AddCasterTriggerSpells(spells);
		}

		/// <summary>
		/// Add Spells to be casted on the targets of this Spell
		/// </summary>
		public void AddCasterTriggerSpells(params Spell[] spells)
		{
			if (CasterTriggerSpells == null)
			{
				CasterTriggerSpells = spells;
			}
			else
			{
				var oldLen = CasterTriggerSpells.Length;
				Array.Resize(ref CasterTriggerSpells, oldLen + spells.Length);
				Array.Copy(spells, 0, CasterTriggerSpells, oldLen, spells.Length);
			}
		}
		#endregion

		#region Proc Spells
		/// <summary>
		/// Add Spells which, when used, can proc this Spell 
		/// </summary>
		public void AddCasterProcSpells(params SpellId[] spellIds)
		{
			var spells = new Spell[spellIds.Length];
			for (var i = 0; i < spellIds.Length; i++)
			{
				var id = spellIds[i];
				var spell = SpellHandler.Get(id);
				if (spell == null)
				{
					throw new ArgumentException("Invalid SpellId: " + id);
				}
				spells[i] = spell;
			}
			AddCasterProcSpells(spells);
		}

		/// <summary>
		/// Add Spells which, when used, can proc this Spell 
		/// </summary>
		public void AddCasterProcSpells(params SpellLineId[] spellSetIds)
		{
			var list = new List<Spell>(spellSetIds.Length * 6);
			foreach (var id in spellSetIds)
			{
				var line = SpellLines.GetLine(id);
				list.AddRange(line);
			}
			AddCasterProcSpells(list.ToArray());
		}

		/// <summary>
		/// Add Spells which, when used, can proc this Spell 
		/// </summary>
		public void AddCasterProcSpells(params Spell[] spells)
		{
			if (CasterProcSpells == null)
			{
				CasterProcSpells = new HashSet<Spell>();
			}
			CasterProcSpells.AddRange(spells);
			ProcTriggerFlags |= ProcTriggerFlags.SpellCast;
		}


		/// <summary>
		/// Add Spells which can proc this Spell on their Target
		/// </summary>
		public void AddTargetProcSpells(params SpellId[] spellIds)
		{
			var spells = new Spell[spellIds.Length];
			for (var i = 0; i < spellIds.Length; i++)
			{
				var id = spellIds[i];
				var spell = SpellHandler.Get(id);
				if (spell == null)
				{
					throw new ArgumentException("Invalid SpellId: " + id);
				}
				spells[i] = spell;
			}
			AddTargetProcSpells(spells);
		}

		/// <summary>
		/// Add Spells which can proc this Spell on their Target
		/// </summary>
		public void AddTargetProcSpells(params SpellLineId[] spellSetIds)
		{
			var list = new List<Spell>(spellSetIds.Length * 6);
			foreach (var id in spellSetIds)
			{
				var line = SpellLines.GetLine(id);
				list.AddRange(line);
			}
			AddTargetProcSpells(list.ToArray());
		}

		/// <summary>
		/// Add Spells which can proc this Spell on their Target
		/// </summary>
		public void AddTargetProcSpells(params Spell[] spells)
		{
			if (TargetProcSpells == null)
			{
				TargetProcSpells = new HashSet<Spell>();
			}
			TargetProcSpells.AddRange(spells);
			ProcTriggerFlags |= ProcTriggerFlags.SpellCast;
		}
		#endregion

		#region Custom Proc Handlers
		/// <summary>
		/// Add Handler which, when used, can proc this Spell 
		/// </summary>
		public void AddCasterProcHandler(ProcHandlerTemplate handler)
		{
			if (CasterProcHandlers == null)
			{
				CasterProcHandlers = new List<ProcHandlerTemplate>();
			}
			handler.IsAttackerTriggerer = true;
			CasterProcHandlers.Add(handler);
		}

		/// <summary>
		/// Add Handler which can proc this Spell on their Target
		/// </summary>
		public void AddTargetProcHandler(ProcHandlerTemplate handler)
		{
			if (TargetProcHandlers == null)
			{
				TargetProcHandlers = new List<ProcHandlerTemplate>();
			}
			handler.IsAttackerTriggerer = false;
			TargetProcHandlers.Add(handler);
		}
		#endregion

		/// <summary>
		/// List of Spells to be learnt when this Spell is learnt
		/// </summary>
		public readonly List<Spell> AdditionallyTaughtSpells = new List<Spell>(0);

		#region Field Generation (Generates the value of many fields, based on the 200+ original Spell properties)
		/// <summary>
		/// Sets all default variables
		/// </summary>
		internal void Initialize()
		{
			var learnSpellEffect = GetEffect(SpellEffectType.LearnSpell);
			if (learnSpellEffect == null)
			{
				learnSpellEffect = GetEffect(SpellEffectType.LearnPetSpell);
			}
			if (learnSpellEffect != null && learnSpellEffect.TriggerSpellId != 0)
			{
				IsTeachSpell = true;
			}

			// figure out Trigger spells
			for (var i = 0; i < Effects.Length; i++)
			{
				var effect = Effects[i];
				if (effect.TriggerSpellId != SpellId.None || effect.AuraType == AuraType.PeriodicTriggerSpell)
				{
					var triggeredSpell = SpellHandler.Get((uint)effect.TriggerSpellId);
					if (triggeredSpell != null)
					{
						if (!IsTeachSpell)
						{
							triggeredSpell.IsTriggeredSpell = true;
						}
						else
						{
							LearnSpell = triggeredSpell;
						}
						effect.TriggerSpell = triggeredSpell;
					}
					else
					{
						if (IsTeachSpell)
						{
							IsTeachSpell = GetEffect(SpellEffectType.LearnSpell) != null;
						}
						Effects[i].IsInvalid = true;
					}
				}
			}

			foreach (var effect in Effects)
			{
				if (effect.EffectType == SpellEffectType.PersistantAreaAura || effect.HasTarget(ImplicitTargetType.DynamicObject))
				{
					DOEffect = effect;
					break;
				}
			}

			//foreach (var effect in Effects)
			//{
			//    effect.Initialize();
			//}
		}

		/// <summary>
		/// For all things that depend on info of all spells from first Init-round and other things
		/// </summary>
		internal void Init2()
		{
			if (inited)
			{
				return;
			}
			inited = true;

            IsChanneled = AttributesEx.HasAnyFlag(SpellAttributesEx.Channeled_1 | SpellAttributesEx.Channeled_2) ||	// don't use Enum.HasFlag!
				ChannelInterruptFlags > 0;

            IsPassive = (!IsChanneled && Attributes.HasFlag(SpellAttributes.Passive)) ||
				// tracking spells are also passive		     
						HasEffectWith(effect => effect.AuraType == AuraType.TrackCreatures) ||
						HasEffectWith(effect => effect.AuraType == AuraType.TrackResources) ||
						HasEffectWith(effect => effect.AuraType == AuraType.TrackStealthed);

			foreach (var effect in Effects)
			{
				effect.Init2();
				if (effect.IsHealEffect)
				{
					IsHealSpell = true;
				}
				if (effect.EffectType == SpellEffectType.NormalizedWeaponDamagePlus)
				{
					IsDualWieldAbility = true;
				}
			}

			InitAura();

			if (IsChanneled)
			{
				if (Durations.Min == 0)
				{
					Durations.Min = Durations.Max = 1000;
				}

				foreach (var effect in Effects)
				{
					if (effect.IsPeriodic)
					{
						ChannelAmplitude = effect.Amplitude;
						break;
					}
				}
			}

			IsOnNextStrike = Attributes.HasAnyFlag(SpellAttributes.OnNextMelee | SpellAttributes.OnNextMelee_2);	// don't use Enum.HasFlag!

			IsRangedAbility = !IsTriggeredSpell &&
				(Attributes.HasAnyFlag(SpellAttributes.Ranged) ||
					   AttributesExC.HasFlag(SpellAttributesExC.ShootRangedWeapon));

			IsStrikeSpell = HasEffectWith(effect => effect.IsStrikeEffect);

			IsWeaponAbility = IsRangedAbility || IsOnNextStrike || IsStrikeSpell;

			IsFinishingMove =
				AttributesEx.HasAnyFlag(SpellAttributesEx.FinishingMove) ||
				HasEffectWith(effect => effect.PointsPerComboPoint > 0 && effect.EffectType != SpellEffectType.Dummy);

			TotemEffect = GetFirstEffectWith(effect => effect.HasTarget(
				ImplicitTargetType.TotemAir, ImplicitTargetType.TotemEarth, ImplicitTargetType.TotemFire, ImplicitTargetType.TotemWater));

			// Required Item slot for weapon abilities
			if (RequiredItemClass == ItemClass.Armor && RequiredItemSubClassMask == ItemSubClassMask.Shield)
			{
				EquipmentSlot = EquipmentSlot.OffHand;
			}
			else
			{
				EquipmentSlot =
                    (IsRangedAbility || AttributesExC.HasFlag(SpellAttributesExC.RequiresWand)) ? EquipmentSlot.ExtraWeapon :
                    (AttributesExC.HasFlag(SpellAttributesExC.RequiresOffHandWeapon) ? EquipmentSlot.OffHand :
                    (AttributesExC.HasFlag(SpellAttributesExC.RequiresMainHandWeapon) ? EquipmentSlot.MainHand : EquipmentSlot.End));
			}

			HasIndividualCooldown = CooldownTime > 0 ||
				(IsWeaponAbility && !IsOnNextStrike && EquipmentSlot != EquipmentSlot.End);

			//IsAoe = HasEffectWith((effect) => {
			//    if (effect.ImplicitTargetA == ImplicitTargetType.)
			//        effect.ImplicitTargetA = ImplicitTargetType.None;
			//    if (effect.ImplicitTargetB == ImplicitTargetType.Unused_EnemiesInAreaChanneledWithExceptions)
			//        effect.ImplicitTargetB = ImplicitTargetType.None;
			//    return false;
			//});

			var profEffect = GetEffect(SpellEffectType.SkillStep);
			if (profEffect != null)
			{
				TeachesApprenticeAbility = profEffect.BasePoints == 0;
			}

			IsProfession = !IsRangedAbility && Ability != null && Ability.Skill.Category == SkillCategory.Profession;

			IsEnhancer = SpellClassSet != 0 && !SpellClassMask.Contains(val => val != 0) &&
				HasEffectWith(effect => effect.AffectMask.Contains(val => val != 0));

			IsFishing = HasEffectWith(effect => effect.HasTarget(ImplicitTargetType.SelfFishing));

			HasEffectWith(effect =>
			{
				if (effect.ChainTargets > 0)
				{
					ChainTargets = effect.ChainTargets;
				}
				return false;
			});

			IsSkinning = HasEffectWith(effect => effect.EffectType == SpellEffectType.Skinning);

			IsTameEffect = HasEffectWith(effect => effect.EffectType == SpellEffectType.TameCreature);

			if (Id == 18425)
			{
				ToString();
			}
			if (IsPreventionDebuff || Mechanic.IsNegative())
			{
				HasHarmfulEffects = true;
				HasBeneficialEffects = false;
				HarmType = HarmType.Harmful;
			}
			else
			{
				HasHarmfulEffects = HasEffectWith(effect => effect.HarmType == HarmType.Harmful);
				HasBeneficialEffects = HasEffectWith(effect => effect.HarmType == HarmType.Beneficial);
				if (HasHarmfulEffects != HasBeneficialEffects && !HasEffectWith(effect => effect.HarmType == HarmType.Neutral))
				{
					HarmType = HasHarmfulEffects ? HarmType.Harmful : HarmType.Beneficial;
				}
				else
				{
					HarmType = HarmType.Neutral;
				}
			}

            ReqDeadTarget = TargetFlags.HasAnyFlag(SpellTargetFlags.Corpse | SpellTargetFlags.PvPCorpse | SpellTargetFlags.UnitCorpse);

			CostsMana = PowerCost > 0 || PowerCostPercentage > 0;

			HasTargets = !HasEffectWith(effect => effect.HasTargets);

			CasterIsTarget = HasTargets && HasEffectWith(effect => effect.HasTarget(ImplicitTargetType.Self));

			//HasSingleNotSelfTarget = 

			IsAreaSpell = HasEffectWith(effect => effect.IsAreaEffect);

			IsDamageSpell = HasHarmfulEffects && !HasBeneficialEffects && HasEffectWith(effect =>
					effect.EffectType == SpellEffectType.Attack ||
					effect.EffectType == SpellEffectType.EnvironmentalDamage ||
					effect.EffectType == SpellEffectType.InstantKill ||
					effect.EffectType == SpellEffectType.SchoolDamage ||
					effect.IsStrikeEffect);

			IsHearthStoneSpell = HasEffectWith(effect => effect.HasTarget(ImplicitTargetType.HeartstoneLocation));

			ForeachEffect(effect =>
			{
				if (effect.EffectType == SpellEffectType.Skill)
				{
					SkillId = (SkillId)effect.MiscValue;
				}
			});

			Schools = Utility.GetSetIndices<DamageSchool>((uint)SchoolMask);
			if (Schools.Length == 0)
			{
				Schools = new[] { DamageSchool.Physical };
			}

			RequiresCasterOutOfCombat = !HasHarmfulEffects && CastDelay > 0 &&
                (Attributes.HasFlag(SpellAttributes.CannotBeCastInCombat) ||
                                        AttributesEx.HasFlag(SpellAttributesEx.RemainOutOfCombat) ||
                                        AuraInterruptFlags.HasFlag(AuraInterruptFlags.OnStartAttack));

			if (RequiresCasterOutOfCombat)
			{
				// We fail if being attacked (among others)
				InterruptFlags |= InterruptFlags.OnTakeDamage;
			}

            IsThrow = AttributesExC.HasFlag(SpellAttributesExC.ShootRangedWeapon) &&
                       Attributes.HasFlag(SpellAttributes.Ranged) && Ability != null && Ability.Skill.Id == SkillId.Thrown;

			HasModifierEffects = HasEffectWith(effect => effect.AuraType == AuraType.AddModifierFlat || effect.AuraType == AuraType.AddModifierPercent);
			ForeachEffect(effect =>
			{
				for (var i = 0; i < 3; i++)
				{
					AllAffectingMasks[i] |= effect.AffectMask[i];
				}
			});

			if (Range.MaxDist == 0)
			{
				Range.MaxDist = 5;
			}

			if (RequiredToolIds == null)
			{
				RequiredToolIds = new uint[0];
			}
			else
			{
				if (RequiredToolIds.Length > 0 && (RequiredToolIds[0] > 0 || RequiredToolIds[1] > 0))
				{
					SpellHandler.SpellsRequiringTools.Add(this);
				}
				ArrayUtil.PruneVals(ref RequiredToolIds);
			}

			ArrayUtil.PruneVals(ref RequiredTotemCategories);

			ForeachEffect(effect =>
			{
				if (effect.SpellEffectHandlerCreator != null)
				{
					EffectHandlerCount++;
				}
			});
			//IsHealSpell = HasEffectWith((effect) => effect.IsHealEffect);
		}
		#endregion

		#region Manage Effects
		public void ForeachEffect(Action<SpellEffect> callback)
		{
			for (int i = 0; i < Effects.Length; i++)
			{
				var effect = Effects[i];
				callback(effect);
			}
		}

		public bool HasEffectWith(Predicate<SpellEffect> predicate)
		{
			for (var i = 0; i < Effects.Length; i++)
			{
				var effect = Effects[i];
				if (predicate(effect))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Returns the first SpellEffect of the given Type within this Spell
		/// </summary>
		public SpellEffect GetEffect(SpellEffectType type)
		{
			foreach (var effect in Effects)
			{
				if (effect.EffectType == type)
				{
					return effect;
				}
			}
			return null;
		}

		public SpellEffect GetFirstEffectWith(Predicate<SpellEffect> predicate)
		{
			foreach (var effect in Effects)
			{
				if (predicate(effect))
				{
					return effect;
				}
			}
			return null;
		}

		public List<SpellEffect> GetEffectsWith(Predicate<SpellEffect> predicate)
		{
			List<SpellEffect> effects = null;
			foreach (var effect in Effects)
			{
				if (predicate(effect))
				{
					if (effects == null)
					{
						effects = new List<SpellEffect>();
					}
					effects.Add(effect);
				}
			}
			return effects;
		}

		/// <summary>
		/// Removes the first Effect of the given Type and replace it with a new one which will be returned.
		/// Appends a new one if none of the given type was found.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public SpellEffect ReplaceEffect(SpellEffectType type)
		{
			for (var i = 0; i < Effects.Length; i++)
			{
				var effect = Effects[i];
				if (effect.EffectType == type)
				{
					return Effects[i] = new SpellEffect();
				}
			}
			return AddEffect(SpellEffectType.None);
		}

		/// <summary>
		/// Adds a new Effect to this Spell
		/// </summary>
		/// <returns></returns>
		public SpellEffect AddEffect()
		{
			return AddEffect(SpellEffectType.None);
		}

		/// <summary>
		/// Adds a new Effect to this Spell
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public SpellEffect AddEffect(SpellEffectType type)
		{
			return AddEffect(type, ImplicitTargetType.None);
		}

		/// <summary>
		/// Adds a new Effect to this Spell
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public SpellEffect AddEffect(SpellEffectType type, ImplicitTargetType target)
		{
			var effect = new SpellEffect(this, Effects.Length > 0 ? Effects[Effects.Length - 1].EffectIndex : 0) { EffectType = type };
			var effects = new SpellEffect[Effects.Length + 1];
			Array.Copy(Effects, effects, Effects.Length);
			Effects = effects;
			Effects[effects.Length - 1] = effect;

			effect.ImplicitTargetA = target;
			return effect;
		}

		/// <summary>
		/// Adds a SpellEffect that will trigger the given Spell on oneself
		/// </summary>
		public SpellEffect AddTriggerSpellEffect(SpellId triggerSpell)
		{
			return AddTriggerSpellEffect(triggerSpell, ImplicitTargetType.Self);
		}

		/// <summary>
		/// Adds a SpellEffect that will trigger the given Spell on the given type of target
		/// </summary>
		public SpellEffect AddTriggerSpellEffect(SpellId triggerSpell, ImplicitTargetType targetType)
		{
			var effect = AddEffect(SpellEffectType.TriggerSpell);
			effect.TriggerSpellId = triggerSpell;
			effect.ImplicitTargetA = targetType;
			return effect;
		}

		/// <summary>
		/// Adds a SpellEffect that will be applied to an Aura to be casted on oneself
		/// </summary>
		public SpellEffect AddAuraEffect(AuraType type)
		{
			return AddAuraEffect(type, ImplicitTargetType.Self);
		}

		/// <summary>
		/// Adds a SpellEffect that will be applied to an Aura to be casted on the given type of target
		/// </summary>
		public SpellEffect AddAuraEffect(AuraType type, ImplicitTargetType targetType)
		{
			var effect = AddEffect(SpellEffectType.ApplyAura);
			effect.AuraType = type;
			effect.ImplicitTargetA = targetType;
			return effect;
		}

		/// <summary>
		/// Adds a SpellEffect that will be applied to an Aura to be casted on the given type of target
		/// </summary>
		public SpellEffect AddAuraEffect(AuraEffectHandlerCreator creator)
		{
			return AddAuraEffect(creator, ImplicitTargetType.Self);
		}

		/// <summary>
		/// Adds a SpellEffect that will be applied to an Aura to be casted on the given type of target
		/// </summary>
		public SpellEffect AddAuraEffect(AuraEffectHandlerCreator creator, ImplicitTargetType targetType)
		{
			var effect = AddEffect(SpellEffectType.ApplyAura);
			effect.AuraType = AuraType.Dummy;
			effect.AuraEffectHandlerCreator = creator;
			effect.ImplicitTargetA = targetType;
			return effect;
		}

		public void ClearEffects()
		{
			Effects = new SpellEffect[0];
		}
		#endregion

		/// <summary>
		/// This is pretty ugly, but its the easiest way to find out whether we have a certain spell
		/// until we provide spell overrides
		/// </summary>
		internal void FigureSpellFieldsByNamesOrIds()
		{
			// TODO: Move to PlayerSpells.cs and remove the IsRejuvenationOrRegrowth field
			if (!IsTeachSpell && (Name.Equals("Rejuvenation") || Name.Equals("Regrowth")))
			{
				IsRejuvenationOrRegrowth = true;
			}
		}

		#region Misc Methods & Props
		public bool MatchesMask(uint[] masks)
		{
			for (var i = 0; i < SpellClassMask.Length; i++)
			{
				if ((masks[i] & SpellClassMask[i]) != 0)
				{
					return true;
				}
			}
			return false;
		}

		public int GetMaxLevelDiff(int casterLevel)
		{
			if (MaxLevel >= BaseLevel && MaxLevel < casterLevel)
			{
				return MaxLevel - BaseLevel;
			}
			return Math.Abs(casterLevel - BaseLevel);
		}

		public int CalcPowerCost(Unit caster, DamageSchool school, Spell spell, PowerType type)
		{
			var cost = PowerCost + (PowerCostPerlevel * GetMaxLevelDiff(caster.Level));
			if (PowerCostPercentage > 0)
			{
				cost += (PowerCostPercentage *
					((type == PowerType.Health ? caster.BaseHealth : caster.BasePower))) / 100;
			}

			return caster.GetPowerCost(school, spell, cost);
		}

		public bool ShouldShowToClient()
		{
			return IsRangedAbility || Visual != 0 || Visual2 != 0 || IsChanneled || CastDelay > 0
				// || (!IsPassive && IsAura)
				;
		}

		/// <summary>
		/// Returns the max duration for this Spell in milliseconds, 
		/// including all modifiers.
		/// </summary>
		public int GetDuration(CasterInfo caster)
		{
			return GetDuration(caster, null);
		}

		/// <summary>
		/// Returns the max duration for this Spell in milliseconds, 
		/// including all modifiers.
		/// </summary>
		public int GetDuration(CasterInfo caster, Unit target)
		{
			var millis = Durations.Min;
			//if (Durations.LevelDelta > 0)
			//{
			//	millis += (int)caster.Level * Durations.LevelDelta;
			//	if (Durations.Max > 0 && millis > Durations.Max)
			//	{
			//		millis = Durations.Max;
			//	}
			//}

			if (Durations.Max > Durations.Min && IsFinishingMove && caster.CasterUnit != null)
			{
				// For some finishing moves, Duration depends on Combopoints
				millis += caster.CasterUnit.ComboPoints * ((Durations.Max - Durations.Min) / 5);
			}

			if (target != null && Mechanic != SpellMechanic.None)
			{
				var mod = target.GetMechanicDurationMod(Mechanic);
				if (mod != 0)
				{
					millis = UnitUpdates.GetMultiMod(mod / 100f, millis);
				}
			}

			var chr = caster.Caster as Character;
			if (chr != null)
			{
				millis = chr.PlayerSpells.GetModifiedInt(SpellModifierType.Duration, this, millis);
			}
			return millis;
		}
		#endregion

		#region Procs
		public bool CanProcBeTriggeredBy(IUnitAction action, bool active)
		{
			if (active)
			{
				if (CasterProcSpells != null)
				{
					return action.Spell != null && CasterProcSpells.Contains(action.Spell);
				}
			}
			else if (TargetProcSpells != null)
			{
				return action.Spell != null && TargetProcSpells.Contains(action.Spell);
			}
			
			if (RequiredItemClass != ItemClass.None)
			{
				// check for weapon
				if (!(action is AttackAction))
				{
					return false;
				}

				var aAction = (AttackAction)action;
				if (aAction.Weapon == null || !(aAction.Weapon is Item))
				{
					return false;
				}

				var weapon = ((Item)aAction.Weapon).Template;

			    return weapon.Class == RequiredItemClass &&
					   (RequiredItemSubClassMask == 0 || weapon.SubClassMask.HasAnyFlag(RequiredItemSubClassMask));
			}
			return true;
		}
		#endregion

		#region Verbose / Debug

		/// <summary>
		/// Fully qualified name
		/// </summary>
		public string FullName
		{
			get
			{
				// TODO: Item-spell?
				string fullName;

				bool isTalent = Talent != null;
				bool isSkill = Ability != null;

				if (isTalent)
				{
					fullName = Talent.FullName;
				}
				else
				{
					fullName = Name;
				}

				if (isSkill && !isTalent && Ability.Skill.Category != SkillCategory.Language &&
					Ability.Skill.Category != SkillCategory.Invalid)
				{
					fullName = Ability.Skill.Category + " " + fullName;
				}

				if (IsTeachSpell &&
					!Name.StartsWith("Learn", StringComparison.InvariantCultureIgnoreCase))
				{
					fullName = "Learn " + fullName;
				}
				else if (IsTriggeredSpell)
				{
					fullName = "Effect: " + fullName;
				}

				if (isSkill)
				{
				}
				else if (IsDeprecated)
				{
					fullName = "Unused " + fullName;
				}
				else if (Description != null && Description.Length == 0)
				{
					//fullName = "No Learn " + fullName;
				}


				return fullName;
			}
		}
		/// <summary>
		/// Spells that contain "zzOld", "test", "unused"
		/// </summary>
		public bool IsDeprecated
		{
			get
			{
				return IsDeprecatedSpellName(Name);
			}
		}

		public static bool IsDeprecatedSpellName(string name)
		{
			return name.IndexOf("test", StringComparison.InvariantCultureIgnoreCase) > -1 ||
					   name.StartsWith("zzold", StringComparison.InvariantCultureIgnoreCase) ||
					   name.IndexOf("unused", StringComparison.InvariantCultureIgnoreCase) > -1;
		}

		public override string ToString()
		{
			return FullName + (RankDesc != "" ? " " + RankDesc : "") + " (Id: " + Id + ")";
		}

		#endregion

		#region Dump
		public void Dump(TextWriter writer, string indent)
		{
			writer.WriteLine("Spell: " + this + " [" + SpellId + "]");

			if (Category != 0)
			{
				writer.WriteLine(indent + "Category: " + Category);
			}
			if (Line != null)
			{
				writer.WriteLine(indent + "Line: " + Line);
			}
			if (PreviousRank != null)
			{
				writer.WriteLine(indent + "Previous Rank: " + PreviousRank);
			}
			if (NextRank != null)
			{
				writer.WriteLine(indent + "Next Rank: " + NextRank);
			}
			if (DispelType != 0)
			{
				writer.WriteLine(indent + "DispelType: " + DispelType);
			}
			if (Mechanic != SpellMechanic.None)
			{
				writer.WriteLine(indent + "Mechanic: " + Mechanic);
			}
			if (Attributes != SpellAttributes.None)
			{
				writer.WriteLine(indent + "Attributes: " + Attributes);
			}
			if (AttributesEx != SpellAttributesEx.None)
			{
				writer.WriteLine(indent + "AttributesEx: " + AttributesEx);
			}
			if (AttributesExB != SpellAttributesExB.None)
			{
				writer.WriteLine(indent + "AttributesExB: " + AttributesExB);
			}
			if (AttributesExC != SpellAttributesExC.None)
			{
				writer.WriteLine(indent + "AttributesExC: " + AttributesExC);
			}
			if (AttributesExD != SpellAttributesExD.None)
			{
				writer.WriteLine(indent + "AttributesExD: " + AttributesExD);
			}
			if ((int)ShapeshiftMask != 0)
			{
				writer.WriteLine(indent + "ShapeshiftMask: " + ShapeshiftMask);
			}
			if ((int)ExcludeShapeshiftMask != 0)
			{
				writer.WriteLine(indent + "ExcludeShapeshiftMask: " + ExcludeShapeshiftMask);
			}
			if ((int)TargetFlags != 0)
			{
				writer.WriteLine(indent + "TargetType: " + TargetFlags);
			}
			if ((int)TargetCreatureTypes != 0)
			{
				writer.WriteLine(indent + "TargetUnitTypes: " + TargetCreatureTypes);
			}
			if ((int)RequiredSpellFocus != 0)
			{
				writer.WriteLine(indent + "RequiredSpellFocus: " + RequiredSpellFocus);
			}
			if (FacingFlags != 0)
			{
				writer.WriteLine(indent + "FacingFlags: " + FacingFlags);
			}
			if ((int)RequiredCasterAuraState != 0)
			{
				writer.WriteLine(indent + "CasterAuraState: " + RequiredCasterAuraState);
			}
			if ((int)RequiredTargetAuraState != 0)
			{
				writer.WriteLine(indent + "TargetAuraState: " + RequiredTargetAuraState);
			}
			if ((int)ExcludeCasterAuraState != 0)
			{
				writer.WriteLine(indent + "ExcludeCasterAuraState: " + ExcludeCasterAuraState);
			}
			if ((int)ExcludeTargetAuraState != 0)
			{
				writer.WriteLine(indent + "ExcludeTargetAuraState: " + ExcludeTargetAuraState);
			}

			if (RequiredCasterAuraId != 0)
			{
				writer.WriteLine(indent + "RequiredCasterAuraId: " + RequiredCasterAuraId);
			}
			if (RequiredTargetAuraId != 0)
			{
				writer.WriteLine(indent + "RequiredTargetAuraId: " + RequiredTargetAuraId);
			}
			if (ExcludeCasterAuraId != 0)
			{
				writer.WriteLine(indent + "ExcludeCasterAuraSpellId: " + ExcludeCasterAuraId);
			}
			if (ExcludeTargetAuraId != 0)
			{
				writer.WriteLine(indent + "ExcludeTargetAuraSpellId: " + ExcludeTargetAuraId);
			}


			if ((int)CastDelay != 0)
			{
				writer.WriteLine(indent + "StartTime: " + CastDelay);
			}
			if (CooldownTime > 0)
			{
				writer.WriteLine(indent + "CooldownTime: " + CooldownTime);
			}
			if (categoryCooldownTime > 0)
			{
				writer.WriteLine(indent + "CategoryCooldownTime: " + categoryCooldownTime);
			}

			if ((int)InterruptFlags != 0)
			{
				writer.WriteLine(indent + "InterruptFlags: " + InterruptFlags);
			}
			if ((int)AuraInterruptFlags != 0)
			{
				writer.WriteLine(indent + "AuraInterruptFlags: " + AuraInterruptFlags);
			}
			if ((int)ChannelInterruptFlags != 0)
			{
				writer.WriteLine(indent + "ChannelInterruptFlags: " + ChannelInterruptFlags);
			}
			if ((int)ProcTriggerFlags != 0)
			{
				writer.WriteLine(indent + "ProcTriggerFlags: " + ProcTriggerFlags);
			}
			if ((int)ProcChance != 0 && ProcChance < 100)
			{
				writer.WriteLine(indent + "ProcChance: " + ProcChance);
			}


			if (ProcCharges != 0)
			{
				writer.WriteLine(indent + "ProcCharges: " + ProcCharges);
			}
			if (MaxLevel != 0)
			{
				writer.WriteLine(indent + "MaxLevel: " + MaxLevel);
			}
			if (BaseLevel != 0)
			{
				writer.WriteLine(indent + "BaseLevel: " + BaseLevel);
			}
			if (Level != 0)
			{
				writer.WriteLine(indent + "Level: " + Level);
			}
			if (Durations.Max > 0)
			{
				writer.WriteLine(indent + "Duration: " + Durations.Min + " - " + Durations.Max + " (" + Durations.LevelDelta + ")");
			}
			if (Visual != 0u)
			{
				writer.WriteLine(indent + "Visual: " + Visual);
			}

			if ((int)PowerType != 0)
			{
				writer.WriteLine(indent + "PowerType: " + PowerType);
			}
			if (PowerCost != 0)
			{
				writer.WriteLine(indent + "PowerCost: " + PowerCost);
			}
			if (PowerCostPerlevel != 0)
			{
				writer.WriteLine(indent + "PowerCostPerlevel: " + PowerCostPerlevel);
			}
			if (PowerPerSecond != 0)
			{
				writer.WriteLine(indent + "PowerPerSecond: " + PowerPerSecond);
			}
			if (PowerPerSecondPerLevel != 0)
			{
				writer.WriteLine(indent + "PowerPerSecondPerLevel: " + PowerPerSecondPerLevel);
			}
			if (PowerCostPercentage != 0)
			{
				writer.WriteLine(indent + "PowerCostPercentage: " + PowerCostPercentage);
			}

			if (Range.MinDist != 0 || Range.MaxDist != DefaultSpellRange)
			{
				writer.WriteLine(indent + "Range: " + Range.MinDist + " - " + Range.MaxDist);
			}
			if ((int)ProjectileSpeed != 0)
			{
				writer.WriteLine(indent + "ProjectileSpeed: " + ProjectileSpeed);
			}
			if ((int)ModalNextSpell != 0)
			{
				writer.WriteLine(indent + "ModalNextSpell: " + ModalNextSpell);
			}
			if (MaxStackCount != 0)
			{
				writer.WriteLine(indent + "MaxStackCount: " + MaxStackCount);
			}

			if (RequiredTools != null)
			{
				writer.WriteLine(indent + "RequiredTools:");
				foreach (var tool in RequiredTools)
				{
					writer.WriteLine(indent + "\t" + tool);
				}
			}
			if (RequiredItemClass != ItemClass.None)
			{
				writer.WriteLine(indent + "RequiredItemClass: " + RequiredItemClass);
			}
			if ((int)RequiredItemInventorySlotMask != 0)
			{
				writer.WriteLine(indent + "RequiredItemInventorySlotMask: " + RequiredItemInventorySlotMask);
			}
			if ((int)RequiredItemSubClassMask != -1 && (int)RequiredItemSubClassMask != 0)
			{
				writer.WriteLine(indent + "RequiredItemSubClassMask: " + RequiredItemSubClassMask);
			}


			if ((int)Visual2 != 0)
			{
				writer.WriteLine(indent + "Visual2: " + Visual2);
			}
			if (Priority != 0)
			{
				writer.WriteLine(indent + "Priority: " + Priority);
			}

			if (StartRecoveryCategory != 0)
			{
				writer.WriteLine(indent + "StartRecoveryCategory: " + StartRecoveryCategory);
			}
			if (StartRecoveryTime != 0)
			{
				writer.WriteLine(indent + "StartRecoveryTime: " + StartRecoveryTime);
			}
			if (MaxTargetLevel != 0)
			{
				writer.WriteLine(indent + "MaxTargetLevel: " + MaxTargetLevel);
			}
			if ((int)SpellClassSet != 0)
			{
				writer.WriteLine(indent + "SpellClassSet: " + SpellClassSet);
			}

			if (SpellClassMask[0] != 0 || SpellClassMask[1] != 0 || SpellClassMask[2] != 0)
			{
				writer.WriteLine(indent + "SpellClassMask: {0}{1}{2}", SpellClassMask[0].ToString("X8"), SpellClassMask[1].ToString("X8"), SpellClassMask[2].ToString("X8"));
			}

			/*if ((int)FamilyFlags != 0)
			{
				writer.WriteLine(indent + "FamilyFlags: " + FamilyFlags);
			}*/
			if ((int)MaxTargets != 0)
			{
				writer.WriteLine(indent + "MaxTargets: " + MaxTargets);
			}

			if (StanceBarOrder != 0)
			{
				writer.WriteLine(indent + "StanceBarOrder: " + StanceBarOrder);
			}

			if ((int)DefenseType != 0)
			{
				writer.WriteLine(indent + "DefenseType: " + DefenseType);
			}

			if ((int)PreventionType != 0)
			{
				writer.WriteLine(indent + "PreventionType: " + PreventionType);
			}

			if (DamageMultipliers.Where(mult => mult != 1).Count() > 0)
			{
				writer.WriteLine(indent + "DamageMultipliers: " + DamageMultipliers.ToString(", "));
			}

			for (int i = 0; i < RequiredTotemCategories.Length; i++)
			{
				if (RequiredTotemCategories[i] != 0)
					writer.WriteLine(indent + "RequiredTotemCategoryId[" + i + "]: " + RequiredTotemCategories[i]);
			}

			if ((int)AreaGroupId != 0)
			{
				writer.WriteLine(indent + "AreaGroupId: " + AreaGroupId);
			}

			if ((int)SchoolMask != 0)
			{
				writer.WriteLine(indent + "SchoolMask: " + SchoolMask);
			}

			if (RuneCostId != 0)
			{
				writer.WriteLine(indent + "RuneCostId: " + RuneCostId);
			}
			if (MissileId != 0)
			{
				writer.WriteLine(indent + "MissileId: " + MissileId);
			}


			if (Description.Length > 0)
			{
				writer.WriteLine(indent + "Desc: " + Description);
			}

			if (Reagents.Length > 0)
			{
				writer.WriteLine(indent + "Reagents: " + Reagents.ToString(", "));
			}

			if (Ability != null)
			{
				writer.WriteLine(indent + string.Format("Skill: {0}", Ability.SkillInfo));
			}

			if (Talent != null)
			{
				writer.WriteLine(indent + string.Format("TalentTree: {0}", Talent.Tree));
			}

			writer.WriteLine();
			foreach (var effect in Effects)
			{
				effect.DumpInfo(writer, "\t\t");
			}
		}
		#endregion

		public bool IsBeneficialFor(CasterInfo casterInfo, WorldObject target)
		{
			return HarmType == HarmType.Beneficial || (HarmType == HarmType.Neutral && (casterInfo.Caster == null || !casterInfo.Caster.MayAttack(target)));
		}

		public bool IsHarmfulFor(CasterInfo casterInfo, WorldObject target)
		{
			return HarmType == HarmType.Harmful || (HarmType == HarmType.Neutral && casterInfo.Caster != null && casterInfo.Caster.MayAttack(target));
		}

		public bool IsHarmfulFor(WorldObject caster, WorldObject target)
		{
			return HarmType == HarmType.Harmful || (HarmType == HarmType.Neutral && caster.MayAttack(target));
		}

		public override bool Equals(object obj)
		{
			return obj is Spell && ((Spell)obj).Id == Id;
		}

		public override int GetHashCode()
		{
			return (int)Id;
		}

		#region ISpellGroup
		public IEnumerator<Spell> GetEnumerator()
		{
			return new SingleEnumerator<Spell>(this);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		#endregion
	}
}
