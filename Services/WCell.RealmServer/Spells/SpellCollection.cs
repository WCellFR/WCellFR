/*************************************************************************
 *
 *   file		: SpellCollection.cs
 *   copyright		: (C) The WCell Team
 *   email		: info@wcell.org
 *   last changed	: $LastChangedDate: 2010-01-29 04:07:03 +0100 (fr, 29 jan 2010) $
 *   last author	: $LastChangedBy: dominikseifert $
 *   revision		: $Rev: 1232 $
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 *************************************************************************/

using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using WCell.Constants.Spells;
using WCell.Util.Threading;
using WCell.RealmServer.Database;
using WCell.RealmServer.Entities;
using WCell.Util;

namespace WCell.RealmServer.Spells
{
	/// <summary>
	/// 
	/// </summary>
	public abstract class SpellCollection
	{
		public static readonly int SpellEnhancerCount = (int)Utility.GetMaxEnum<SpellModifierType>() + 1;

		protected Dictionary<uint, Spell> m_byId;

		protected SpellCollection(Unit owner)
			: this(owner, true)
		{

		}

		protected SpellCollection(Unit owner, bool initDictionary)
		{
			if (initDictionary)
				m_byId = new Dictionary<uint, Spell>(60);

			Owner = owner;
		}

		/// <summary>
		/// Required by SpellCollection
		/// </summary>
		public Unit Owner
		{
			get;
			internal protected set;
		}

		/// <summary>
		/// The amount of Spells in this Collection
		/// </summary>
		public int Count
		{
			get { return m_byId.Count; }
		}

		public bool HasSpells
		{
			get { return m_byId.Count > 0; }
		}

		public Dictionary<uint, Spell> SpellsById
		{
			get { return m_byId; }
			internal set { m_byId = value; }
		}

		/// <summary>
		/// Teaches a new spell to the unit. Also sends the spell learning animation, if applicable.
		/// </summary>
		public void AddSpell(uint spellId)
		{
			var spell = SpellHandler.ById[spellId];
			AddSpell(spell);
		}

		/// <summary>
		/// Teaches a new spell to the unit. Also sends the spell learning animation, if applicable.
		/// </summary>
		public void AddSpell(SpellId spellId)
		{
			var spell = SpellHandler.Get(spellId);
			AddSpell(spell);
		}

		/// <summary>
		/// Teaches a new spell to the unit. Also sends the spell learning animation, if applicable.
		/// </summary>
		public virtual void AddSpell(Spell spell)
		{
			//Add(id, spell, true);
			m_byId[spell.Id] = spell;
			OnAdd(spell);
		}

		protected void OnAdd(Spell spell)
		{
			if (spell.IsPassive)
			{
				Owner.SpellCast.TriggerSelf(spell);
			}
			if (spell.AdditionallyTaughtSpells.Count > 0)
			{
				foreach (var spe in spell.AdditionallyTaughtSpells)
				{
					AddSpell(spe);
				}
			}
		}

		/// <summary>
		/// Teaches a new spell to the unit. Also sends the spell learning animation, if applicable.
		/// </summary>
		public void AddSpell(IEnumerable<SpellId> spells)
		{
			foreach (var spell in spells)
			{
				AddSpell(spell);
			}
		}

		/// <summary>
		/// Teaches a new spell to the unit. Also sends the spell learning animation, if applicable.
		/// </summary>
		public void AddSpell(params SpellId[] spells)
		{
			foreach (var spell in spells)
			{
				AddSpell(spell);
			}
		}

		/// <summary>
		/// Teaches a new spell to the unit. Also sends the spell learning animation, if applicable.
		/// </summary>
		public void AddSpell(IEnumerable<Spell> spells)
		{
			foreach (var spell in spells)
			{
				AddSpell(spell);
			}
		}

		/// <summary>
		/// Adds the spell without doing any further checks or adding any spell-related skills or showing animations
		/// </summary>
		public void OnlyAdd(SpellId id)
		{
			m_byId.Add((uint)id, SpellHandler.ById.Get((uint)id));
		}

		public void OnlyAdd(Spell spell)
		{
			m_byId.Add(spell.Id, spell);
		}

		public bool Contains(uint id)
		{
			return m_byId.ContainsKey(id);
		}

		public bool Contains(SpellId id)
		{
			return m_byId.ContainsKey((uint)id);
		}

		public Spell this[SpellId id]
		{
			get
			{
				Spell spell;
				m_byId.TryGetValue((uint)id, out spell);
				return spell;
			}
		}

		public Spell this[uint id]
		{
			get
			{
				Spell spell;
				m_byId.TryGetValue(id, out spell);
				return spell;
			}
		}

		public void Remove(SpellId spellId)
		{
			Replace(SpellHandler.Get(spellId), null);
		}

		public bool Remove(uint spellId)
		{
			Remove((SpellId)spellId);
			return true;
		}

		public virtual void Remove(Spell spell)
		{
			Replace(spell, null);
		}

		public virtual void Clear()
		{
			m_byId.Clear();
		}

		/// <summary>
		/// Only works if you have 2 valid spell ids and oldSpellId already exists.
		/// </summary>
		public void Replace(SpellId oldSpellId, SpellId newSpellId)
		{
			Spell oldSpell, newSpell = SpellHandler.Get(newSpellId);
			if (m_byId.TryGetValue((uint)oldSpellId, out oldSpell))
			{
				Replace(oldSpell, newSpell);
			}
		}

		/// <summary>
		/// Replaces or (if newSpell == null) removes oldSpell; does nothing if oldSpell doesn't exist.
		/// </summary>
		public virtual void Replace(Spell oldSpell, Spell newSpell)
		{
			//if (m_byId.Remove((uint)oldSpell))
			m_byId.Remove(oldSpell.Id);
			if (oldSpell.IsPassive)
			{
				Owner.Auras.Cancel(oldSpell);
			}
			if (newSpell != null)
			{
				AddSpell(newSpell);
			}
		}

		public IEnumerator<Spell> GetEnumerator()
		{
			foreach (var spell in m_byId.Values)
			{
				yield return spell;
			}
		}

		public virtual void AddDefaultSpells()
		{
		}

		public abstract void AddCooldown(Spell spell, Item casterItem);

		public abstract void ClearCooldowns();

		public abstract bool IsReady(Spell spell);

		public abstract void ClearCooldown(Spell spell);
	}
}
