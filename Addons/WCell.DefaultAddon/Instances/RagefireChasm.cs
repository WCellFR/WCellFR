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
		//Bosses
		private static NPCEntry oggleflintEntry;
		private static NPCEntry taragamanEntry;
		private static NPCEntry jergoshEntry;
		private static NPCEntry bazzalanEntry;

		//NPCs
		private static NPCEntry earthborerEntry;
		private static NPCEntry ragefireshamanEntry;
		private static NPCEntry RagefiretroggEntry;
		private static NPCEntry searingbladeenforcerEntry;
		private static NPCEntry searingbladecultistEntry;
		private static NPCEntry searingbladewarlockEntry;
		
		[Initialization]
		[DependentInitialization(typeof(NPCMgr))]
		public static void InitNPCs()
		{
		    private Random random;
			
			// Oggleflint
			oggleflintEntry = NPCMgr.GetEntry(NPCId.Oggleflint);
			oggleflintEntry.AddSpell(SpellId.Cleave);
			SpellHandler.Apply(spell => { spell.CooldownTime = 5000; }, SpellId.Cleave);


			// Taragaman the Hungerer
			taragamanEntry = NPCMgr.GetEntry(NPCId.TaragamanTheHungerer);
            SpellId[] taragamanSpells = new SpellId[2] { SpellId.Uppercut, SpellId.FireNova };
            taragamanEntry.AddSpell(taragamanSpells);
			SpellHandler.Apply(spell => { spell.CooldownTime = 5000; }, taragamanSpells[1]);
			SpellHandler.Apply(spell => { spell.CooldownTime = 10000; }, taragamanSpells[2]);


			// Jergosh the Invoker
			jergoshEntry = NPCMgr.GetEntry(NPCId.JergoshTheInvoker);
            SpellId[] jergoshSpells = new SpellId[2] { SpellId.CurseOfWeakness, SpellId.Immolate };
            jergoshEntry.AddSpells(jergoshSpells);
			SpellHandler.Apply(spell => { spell.CooldownTime = 12000; }, jergoshSpells[1]);
			SpellHandler.Apply(spell => { spell.CooldownTime = 5000; }, jergoshSpells[2]);


			// Bazzalan
			bazzalanEntry = NPCMgr.GetEntry(NPCId.Bazzalan);
            SpellId[] bazzalanSpells = new SpellId[2] { SpellId.Poison, SpellId.SinisterStrike };
            bazzalanEntry.AddSpell(bazzalanSpells);

			SpellHandler.Apply(spell => { spell.CooldownTime = 10000; }, bazzalanSpells[1]);
			SpellHandler.Apply(spell => { spell.CooldownTime = 12000; }, bazzalanSpells[2]);
			
			//NPCs
			//Earthborer
			earthborerEntry = NPCMgr.GetEntry(NPCId.Earthborer);
			earthborerEntry.AddSpell(SpellId.EarthborerAcid);
			SpellHandler.Apply(spell => { spell.CooldownTime = (random.Next(8000, 12000); }, SpellId.EarthborerAcid); //TODO : Check cooldowns
			//RagefireShaman
			ragefireshamanEntry = NPCMgr.GetEntry(NPCId.RagefireShaman);
			SpellId[] ragefireshamanSpells = new SpellId[2] { SpellId.HealingWave, SpellId.LightningBolt };
            jergoshEntry.AddSpells(ragefireshamanSpells);
			SpellHandler.Apply(spell => { spell.CooldownTime = (random.Next(8000, 12000); }, ragefireshamanSpells[1]);//TODO : Check cooldowns & Healing support?!
			SpellHandler.Apply(spell => { spell.CooldownTime = (random.Next(8000, 12000); }, ragefireshamanSpells[2]);//TODO : Check cooldowns
			//RagefireTrogg
			ragefiretroggEntry = NPCMgr.GetEntry(NPCId.RagefireTrogg);
			ragefiretroggEntry.AddSpell(SpellId.Strike);
			SpellHandler.Apply(spell => { spell.CooldownTime = (random.Next(8000, 12000); }, SpellId.Strike); //TODO : Check cooldowns
			//SearingBladeCultist
			searingbladecultistEntry = NPCMgr.GetEntry(NPCId.SearingBladeCultist);
			searingbladecultistEntry.AddSpell(SpellId.CurseOfAgony_4);
			SpellHandler.Apply(spell => { spell.CooldownTime = (random.Next(8000, 12000); }, SpellId.CurseOfAgony_4); //TODO : Check cooldowns
			//SearingBladeEnforcer
			searingbladeenforcerEntry = NPCMgr.GetEntry(NPCId.SearingBladeEnforcer);
			searingbladeenforcerEntry.AddSpell(SpellId.ShieldSlam);
			SpellHandler.Apply(spell => { spell.CooldownTime = (random.Next(5000, 12000); }, SpellId.ShieldSlam); //TODO : Check cooldowns
			//SearingBladeWarlock (!)
			searingbladewarlockEntry = NPCMgr.GetEntry(NPCId.SearingBladeWarlock);
			searingbladewarlockEntry.AddSpell(SpellId.ShadowBolt_31);
			SpellHandler.Apply(spell => { spell.CooldownTime = (random.Next(5000, 10000); }, SpellId.ShadowBolt_31); //TODO : Check cooldowns
			searingbladewarlockEntry.BrainCreator = searingbladewarlock => new SearingBladeWarlockBrain(searingbladewarlock);
		}
		#endregion
		
		public class SearingBladeWarlockBrain : MobBrain
        {
		    public override void OnEnterCombat()
            {
                //Here the warlock will summon a voidwalker
                base.OnEnterCombat();
				private uint onecast = 0
				if (onecast == 0)
				{
					m_owner.SpellCast.Trigger(SpellId.SummonVoidwalker);
					onecast++
				}
				
            }
		}
	}
}