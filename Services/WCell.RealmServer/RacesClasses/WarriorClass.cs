/*************************************************************************
 *
 *   file		: Warrior.cs
 *   copyright		: (C) The WCell Team
 *   email		: info@wcell.org
 *   last changed	: $LastChangedDate: 2008-04-08 17:02:58 +0800 (Tue, 08 Apr 2008) $
 *   last author	: $LastChangedBy: domiii $
 *   revision		: $Rev: 244 $
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 *************************************************************************/

using WCell.Constants;
using WCell.RealmServer.Entities;

namespace WCell.RealmServer.RacesClasses
{
	/// <summary>
	/// Defines the basics of the Warrior class.
	/// </summary>
	public class WarriorClass : BaseClass
	{
		public override ClassId Id
		{
			get { return ClassId.Warrior; }
		}

		public override PowerType PowerType
		{
			get
			{
				return PowerType.Rage;
			}
		}

		/// <summary>
		/// Calculates attack power for the class at a specific level, Strength and Agility.
		/// </summary>
		/// <param name="level">the player's level</param>
		/// <param name="strength">the player's Strength</param>
		/// <param name="agility">the player's Agility</param>
		/// <returns>the total attack power</returns>
		public override int CalculateMeleeAP(int level, int strength, int agility)
		{
			return (level * 3 + strength * 2 - 20);
		}

		public override int CalculateRangedAP(int level, int strength, int agility)
		{
			return level + agility - 10;
		}


		public override float CalculateMagicCritChance(int level, int intellect)
		{
			//return (intellect / 80f) + /*(Spell Critical Strike Rating/22.08)*/ +2.2f;
			return 0;
		}

		/// <summary>
		/// Calculates the amount of power regeneration for the class at a specific level and Spirit.
		/// </summary>
		/// <param name="level">the player's level</param>
		/// <param name="spirit">the player's Spirit</param>
		/// <returns>the total power regeneration amount</returns>
		public override int CalculatePowerRegen(Character chr)
		{
			// 1 Rage in the client is a value of 10
			if (chr.IsInCombat)
			{
				return 0;
			}
			return -50;
		}

		/// <summary>
		/// Gets the maximum power for the class.
		/// </summary>
		/// <remarks>This method takes the max power calculated by other stats, and either returns that
		/// value, or it returns a hard-coded value. Warriors and Rogues have hard-coded power levels,
		/// so we override this method for them, returning the hard-coded value, but here we just return 'power'.</remarks>
		/// <param name="power">the maximum amount of power the player has</param>
		/// <returns>the maximum amount of power the player should have</returns>
		public override int GetPowerForLevel(int level)
		{
			return 1000;
		}
	}
}
