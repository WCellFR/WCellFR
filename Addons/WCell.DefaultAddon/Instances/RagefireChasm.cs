using WCell.Constants.NPCs;
using WCell.Core.Initialization;
using WCell.RealmServer.AI.Brains;
using WCell.RealmServer.Instances;
using WCell.RealmServer.NPCs;
using WCell.RealmServer.Spells;
using WCell.RealmServer.Entities;
using WCell.Constants.Spells;
using WCell.Constants;
using WCell.Util;
using WCell.RealmServer.AI;
using WCell.RealmServer.AI.Actions.States;
using WCell.RealmServer.AI.Actions.Combat;
using System.Collections.Generic;
using System;

namespace WCell.Addons.Default.Instances
{
	public class RagefireChasm : DungeonInstance
	{
		#region Setup Content
		private static NPCEntry oggleflintEntry;
		private static NPCEntry taragamanEntry;
		private static NPCEntry jergoshEntry;
		private static NPCEntry bazzalanEntry;

		[Initialization]
		[DependentInitialization(typeof(NPCMgr))]
		public static void InitNPCs()
		{
			// Oggleflint
			oggleflintEntry = NPCMgr.GetEntry(NPCId.Oggleflint);
			oggleflintEntry.AddSpell(SpellId.Cleave);
			SpellHandler.Apply(spell => { spell.CooldownTime = 5000; }, SpellId.Cleave);

			oggleflintEntry.Activated += oggleflint =>
			{
				var brain = (BaseBrain)oggleflint.Brain;
				var combatAction = (AICombatAction)brain.Actions[BrainState.Combat];
				combatAction.Strategy = new OggleflintAttackAction(oggleflint);
			};

			// Taragaman the Hungerer
			taragamanEntry = NPCMgr.GetEntry(NPCId.TaragamanTheHungerer);
            SpellId[] taragamanSpells = new SpellId[2] { SpellId.Uppercut, SpellId.FireNova };
            taragamanEntry.AddSpells(taragamanSpells);
			SpellHandler.Apply(spell => { spell.CooldownTime = 5000; }, taragamanSpells[1]);
			SpellHandler.Apply(spell => { spell.CooldownTime = 10000; }, taragamanSpells[2]);

			taragamanEntry.Activated += taragaman =>
			{
				var brain = (BaseBrain)taragaman.Brain;
				var combatAction = (AICombatAction)brain.Actions[BrainState.Combat];
				combatAction.Strategy = new TaragamanAttackAction(taragaman);
			};

			// Jergosh the Invoker
			jergoshEntry = NPCMgr.GetEntry(NPCId.JergoshTheInvoker);
            SpellId[] jergoshSpells = new SpellId[2] { SpellId.CurseOfWeakness, SpellId.Immolate };
            jergoshEntry.AddSpells(jergoshSpells);
			SpellHandler.Apply(spell => { spell.CooldownTime = 12000; }, jergoshSpells[1]);
			SpellHandler.Apply(spell => { spell.CooldownTime = 5000; }, jergoshSpells[2]);

			jergoshEntry.Activated += jergosh =>
			{
				var brain = (BaseBrain)jergosh.Brain;
				var combatAction = (AICombatAction)brain.Actions[BrainState.Combat];
				combatAction.Strategy = new JergoshAttackAction(jergosh);
			};

			// Bazzalan
			bazzalanEntry = NPCMgr.GetEntry(NPCId.Bazzalan);
            SpellId[] bazzalanSpells = new SpellId[2] { SpellId.Poison, SpellId.SinisterStrike };
            bazzalanEntry.AddSpells(bazzalanSpells);

			SpellHandler.Apply(spell => { spell.CooldownTime = 10000; }, bazzalanSpells[1]);
			SpellHandler.Apply(spell => { spell.CooldownTime = 12000; }, bazzalanSpells[2]);

			bazzalanEntry.Activated += bazzalan =>
			{
				var brain = (BaseBrain)bazzalan.Brain;
				var combatAction = (AICombatAction)brain.Actions[BrainState.Combat];
				combatAction.Strategy = new BazzalanAttackAction(bazzalan);
			};

		}
		#endregion
	}
}