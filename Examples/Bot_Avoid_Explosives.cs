public void OnStartup()
{
	Events.UpdateCallback.Start(UpdateAvoidExplosives, 200, 0);
}

/// Bot Behavior Updater

	public class AvoidBots
	{
		public IPlayer bot = null;
		public IObject gt = null;
		public float cooldown = 0f;

  		public AvoidBots(IPlayer ply)
  		{
			this.bot = ply;
  		}
	}	

List<AvoidBots> ABots = new List<AvoidBots>();
List<IPlayer> avoidbots = new List<IPlayer>();

public void UpdateAvoidExplosives(float elapsed)
{
	foreach (IPlayer bot in from Q in Game.GetPlayers() where Q != null && !Q.IsDead && Q.IsBot && Q.IsUser && !avoidbots.Contains(Q) select Q)
	{
		ABots.Add(new AvoidBots(bot));
		avoidbots.Add(bot);
		if(Game.IsEditorTest) Game.CreateDialogue("I'M LISTED!", bot, duration:1000f);
	}

	foreach (AvoidBots data in from Q in ABots where !Q.bot.IsDead && Q.bot != null select Q)
	{
		/// Grenade Avoid Script

		IObject streetsweeperwreck = (from Q in Game.GetObjectsByName((new string[]{"StreetsweeperWreck"})) orderby Vector2.Distance(Q.GetWorldPosition(), data.bot.GetWorldPosition()) ascending
		where Q != null && Vector2.Distance(Q.GetWorldPosition(),data.bot.GetWorldPosition()) <= 40 select Q).FirstOrDefault();

		IObjectGrenadeThrown grenade = (from Q in Game.GetObjects<IObjectGrenadeThrown>() orderby Vector2.Distance(Q.GetWorldPosition(), data.bot.GetWorldPosition()) ascending
		where Q != null && Q.ExplosionResultedInDud == false && Vector2.Distance(Q.GetWorldPosition(),data.bot.GetWorldPosition()) <= 40 select Q).FirstOrDefault();

		IObject flammable = (from Q in Game.GetObjectsByName((new string[]{"BarrelExplosive", "Gascan00", "PropaneTank"})) orderby Vector2.Distance(Q.GetWorldPosition(), data.bot.GetWorldPosition()) ascending
		where Q != null && Q.IsBurning && Vector2.Distance(Q.GetWorldPosition(),data.bot.GetWorldPosition()) <= 40 select Q).FirstOrDefault();

		IObjectMineThrown mine = (from Q in Game.GetObjects<IObjectMineThrown>() orderby Vector2.Distance(Q.GetWorldPosition(), data.bot.GetWorldPosition()) ascending
		where Q != null && Q.ExplosionResultedInDud == false && Vector2.Distance(Q.GetWorldPosition(),data.bot.GetWorldPosition()) <= 100 select Q).FirstOrDefault();

		if (streetsweeperwreck != null) 
		{
			IObject node = (from Q in Game.GetObjectsByArea<IObjectPathNode>(new Area(data.bot.GetWorldPosition() - new Vector2(80,70), data.bot.GetWorldPosition() + new Vector2(80,70)),PhysicsLayer.Background)
			orderby Vector2.Distance(Q.GetWorldPosition(), streetsweeperwreck.GetWorldPosition()) descending where Q != null && Q.GetNodeEnabled() == true
			&& (from N in Q.GetPathNodeConnections() where (N.GetConnectionEnabled() == false||N.GetConnectionBlockedByPathZone() == true) select N).FirstOrDefault() == null
			&& ((from P in Game.GetObjectsByArea<IObjectPathNode>(new Area(data.bot.GetWorldPosition() - new Vector2(80,70), data.bot.GetWorldPosition() + new Vector2(80,70)),PhysicsLayer.Background)

			orderby Vector2.Distance(P.GetWorldPosition(), data.bot.GetWorldPosition()) ascending where P != null && (P.GetNodeEnabled() == false||(from O in P.GetPathNodeConnections() where (O.GetConnectionEnabled() == false||O.GetConnectionBlockedByPathZone() == true) select O).FirstOrDefault() != null) && Vector2.Distance(P.GetWorldPosition(), data.bot.GetWorldPosition()) <= 22 select P).FirstOrDefault() != null ?
			((data.bot.GetWorldPosition().X - (from P in Game.GetObjectsByArea<IObjectPathNode>(new Area(data.bot.GetWorldPosition() - new Vector2(80,70), data.bot.GetWorldPosition() + new Vector2(80,70)),PhysicsLayer.Background)

			orderby Vector2.Distance(P.GetWorldPosition(), data.bot.GetWorldPosition()) ascending where P != null && (P.GetNodeEnabled() == false||(from O in P.GetPathNodeConnections() where (O.GetConnectionEnabled() == false||O.GetConnectionBlockedByPathZone() == true) select O).FirstOrDefault() != null) && Vector2.Distance(P.GetWorldPosition(), data.bot.GetWorldPosition()) <= 22 select P).FirstOrDefault().GetWorldPosition().X) >= 0 ? (Q.GetWorldPosition().X - (from P in Game.GetObjectsByArea<IObjectPathNode>(new Area(data.bot.GetWorldPosition() - new Vector2(80,70), data.bot.GetWorldPosition() + new Vector2(80,70)),PhysicsLayer.Background) orderby Vector2.Distance(P.GetWorldPosition(), data.bot.GetWorldPosition()) ascending where P != null && (P.GetNodeEnabled() == false||(from O in P.GetPathNodeConnections() where (O.GetConnectionEnabled() == false||O.GetConnectionBlockedByPathZone() == true) select O).FirstOrDefault() != null) && Vector2.Distance(P.GetWorldPosition(), data.bot.GetWorldPosition()) <= 22 select P).FirstOrDefault().GetWorldPosition().X) >= 0 : (Q.GetWorldPosition().X -
			(from P in Game.GetObjectsByArea<IObjectPathNode>(new Area(data.bot.GetWorldPosition() - new Vector2(80,70), data.bot.GetWorldPosition() + new Vector2(80,70)),PhysicsLayer.Background) orderby Vector2.Distance(P.GetWorldPosition(), data.bot.GetWorldPosition()) ascending where P != null && (P.GetNodeEnabled() == false||(from O in P.GetPathNodeConnections() where (O.GetConnectionEnabled() == false||O.GetConnectionBlockedByPathZone() == true) select O).FirstOrDefault() != null) && Vector2.Distance(P.GetWorldPosition(), data.bot.GetWorldPosition()) <= 22 select P).FirstOrDefault().GetWorldPosition().X) < 0)
			: !Q.IsRemoved) select Q).FirstOrDefault();

			BotBehaviorSet set = data.bot.GetBotBehaviorSet();
			set.ChaseRange = 20;
			set.GuardRange = 9;
			set.ChokePointPlayerCountThreshold = 0;

			set.EliminateEnemies = false;
			set.RangedWeaponUsage = false;
			set.MeleeWeaponUsage = false;
			set.MeleeUsage = false;

			data.bot.SetBotBehaviorSet(set);

			if (node != null && (data.bot.GetGuardTarget() == null||data.bot.GetGuardTarget() != null && (!(data.bot.GetGuardTarget() is IObjectPathNode)
			||data.bot.GetGuardTarget() is IObjectPathNode && Vector2.Distance(data.bot.GetGuardTarget().GetWorldPosition(), streetsweeperwreck.GetWorldPosition()) <= 50)))
			{
				data.bot.SetGuardTarget(node);
				data.cooldown = Game.TotalElapsedGameTime + 2800;
				if (Game.IsEditorTest == true) { Game.CreateDialogue("Evading!", data.bot, duration:1000f); }
			}
		}
		else
		if (grenade != null) 
		{
			IObject node = (from Q in Game.GetObjectsByArea<IObjectPathNode>(new Area(data.bot.GetWorldPosition() - new Vector2(80,70), data.bot.GetWorldPosition() + new Vector2(80,70)),PhysicsLayer.Background)
			orderby Vector2.Distance(Q.GetWorldPosition(), grenade.GetWorldPosition()) descending where Q != null && Q.GetNodeEnabled() == true
			&& (from N in Q.GetPathNodeConnections() where (N.GetConnectionEnabled() == false||N.GetConnectionBlockedByPathZone() == true) select N).FirstOrDefault() == null
			&& ((from P in Game.GetObjectsByArea<IObjectPathNode>(new Area(data.bot.GetWorldPosition() - new Vector2(80,70), data.bot.GetWorldPosition() + new Vector2(80,70)),PhysicsLayer.Background)

			orderby Vector2.Distance(P.GetWorldPosition(), data.bot.GetWorldPosition()) ascending where P != null && (P.GetNodeEnabled() == false||(from O in P.GetPathNodeConnections() where (O.GetConnectionEnabled() == false||O.GetConnectionBlockedByPathZone() == true) select O).FirstOrDefault() != null) && Vector2.Distance(P.GetWorldPosition(), data.bot.GetWorldPosition()) <= 22 select P).FirstOrDefault() != null ?
			((data.bot.GetWorldPosition().X - (from P in Game.GetObjectsByArea<IObjectPathNode>(new Area(data.bot.GetWorldPosition() - new Vector2(80,70), data.bot.GetWorldPosition() + new Vector2(80,70)),PhysicsLayer.Background)

			orderby Vector2.Distance(P.GetWorldPosition(), data.bot.GetWorldPosition()) ascending where P != null && (P.GetNodeEnabled() == false||(from O in P.GetPathNodeConnections() where (O.GetConnectionEnabled() == false||O.GetConnectionBlockedByPathZone() == true) select O).FirstOrDefault() != null) && Vector2.Distance(P.GetWorldPosition(), data.bot.GetWorldPosition()) <= 22 select P).FirstOrDefault().GetWorldPosition().X) >= 0 ? (Q.GetWorldPosition().X - (from P in Game.GetObjectsByArea<IObjectPathNode>(new Area(data.bot.GetWorldPosition() - new Vector2(80,70), data.bot.GetWorldPosition() + new Vector2(80,70)),PhysicsLayer.Background) orderby Vector2.Distance(P.GetWorldPosition(), data.bot.GetWorldPosition()) ascending where P != null && (P.GetNodeEnabled() == false||(from O in P.GetPathNodeConnections() where (O.GetConnectionEnabled() == false||O.GetConnectionBlockedByPathZone() == true) select O).FirstOrDefault() != null) && Vector2.Distance(P.GetWorldPosition(), data.bot.GetWorldPosition()) <= 22 select P).FirstOrDefault().GetWorldPosition().X) >= 0 : (Q.GetWorldPosition().X -
			(from P in Game.GetObjectsByArea<IObjectPathNode>(new Area(data.bot.GetWorldPosition() - new Vector2(80,70), data.bot.GetWorldPosition() + new Vector2(80,70)),PhysicsLayer.Background) orderby Vector2.Distance(P.GetWorldPosition(), data.bot.GetWorldPosition()) ascending where P != null && (P.GetNodeEnabled() == false||(from O in P.GetPathNodeConnections() where (O.GetConnectionEnabled() == false||O.GetConnectionBlockedByPathZone() == true) select O).FirstOrDefault() != null) && Vector2.Distance(P.GetWorldPosition(), data.bot.GetWorldPosition()) <= 22 select P).FirstOrDefault().GetWorldPosition().X) < 0)
			: !Q.IsRemoved) select Q).FirstOrDefault();

			BotBehaviorSet set = data.bot.GetBotBehaviorSet();
			set.ChaseRange = 20;
			set.GuardRange = 9;
			set.ChokePointPlayerCountThreshold = 0;

			set.EliminateEnemies = false;
			set.RangedWeaponUsage = false;
			set.MeleeWeaponUsage = false;
			set.MeleeUsage = false;

			data.bot.SetBotBehaviorSet(set);

			if (node != null && (data.bot.GetGuardTarget() == null||data.bot.GetGuardTarget() != null && (!(data.bot.GetGuardTarget() is IObjectPathNode)
			||data.bot.GetGuardTarget() is IObjectPathNode && Vector2.Distance(data.bot.GetGuardTarget().GetWorldPosition(), grenade.GetWorldPosition()) <= 50)))
			{
				data.bot.SetGuardTarget(node);
				data.cooldown = Game.TotalElapsedGameTime + 2200;
				if (Game.IsEditorTest == true) { Game.CreateDialogue("Evading!", data.bot, duration:1000f); }
			}
		}
		else
		if (flammable != null) 
		{
			IObject node = (from Q in Game.GetObjectsByArea<IObjectPathNode>(new Area(data.bot.GetWorldPosition() - new Vector2(80,70), data.bot.GetWorldPosition() + new Vector2(80,70)),PhysicsLayer.Background)
			orderby Vector2.Distance(Q.GetWorldPosition(), flammable.GetWorldPosition()) descending where Q != null && Q.GetNodeEnabled() == true
			&& (from N in Q.GetPathNodeConnections() where (N.GetConnectionEnabled() == false||N.GetConnectionBlockedByPathZone() == true) select N).FirstOrDefault() == null
			&& ((from P in Game.GetObjectsByArea<IObjectPathNode>(new Area(data.bot.GetWorldPosition() - new Vector2(80,70), data.bot.GetWorldPosition() + new Vector2(80,70)),PhysicsLayer.Background)

			orderby Vector2.Distance(P.GetWorldPosition(), data.bot.GetWorldPosition()) ascending where P != null && (P.GetNodeEnabled() == false||(from O in P.GetPathNodeConnections() where (O.GetConnectionEnabled() == false||O.GetConnectionBlockedByPathZone() == true) select O).FirstOrDefault() != null) && Vector2.Distance(P.GetWorldPosition(), data.bot.GetWorldPosition()) <= 22 select P).FirstOrDefault() != null ?
			((data.bot.GetWorldPosition().X - (from P in Game.GetObjectsByArea<IObjectPathNode>(new Area(data.bot.GetWorldPosition() - new Vector2(80,70), data.bot.GetWorldPosition() + new Vector2(80,70)),PhysicsLayer.Background)

			orderby Vector2.Distance(P.GetWorldPosition(), data.bot.GetWorldPosition()) ascending where P != null && (P.GetNodeEnabled() == false||(from O in P.GetPathNodeConnections() where (O.GetConnectionEnabled() == false||O.GetConnectionBlockedByPathZone() == true) select O).FirstOrDefault() != null) && Vector2.Distance(P.GetWorldPosition(), data.bot.GetWorldPosition()) <= 22 select P).FirstOrDefault().GetWorldPosition().X) >= 0 ? (Q.GetWorldPosition().X - (from P in Game.GetObjectsByArea<IObjectPathNode>(new Area(data.bot.GetWorldPosition() - new Vector2(80,70), data.bot.GetWorldPosition() + new Vector2(80,70)),PhysicsLayer.Background) orderby Vector2.Distance(P.GetWorldPosition(), data.bot.GetWorldPosition()) ascending where P != null && (P.GetNodeEnabled() == false||(from O in P.GetPathNodeConnections() where (O.GetConnectionEnabled() == false||O.GetConnectionBlockedByPathZone() == true) select O).FirstOrDefault() != null) && Vector2.Distance(P.GetWorldPosition(), data.bot.GetWorldPosition()) <= 22 select P).FirstOrDefault().GetWorldPosition().X) >= 0 : (Q.GetWorldPosition().X -
			(from P in Game.GetObjectsByArea<IObjectPathNode>(new Area(data.bot.GetWorldPosition() - new Vector2(80,70), data.bot.GetWorldPosition() + new Vector2(80,70)),PhysicsLayer.Background) orderby Vector2.Distance(P.GetWorldPosition(), data.bot.GetWorldPosition()) ascending where P != null && (P.GetNodeEnabled() == false||(from O in P.GetPathNodeConnections() where (O.GetConnectionEnabled() == false||O.GetConnectionBlockedByPathZone() == true) select O).FirstOrDefault() != null) && Vector2.Distance(P.GetWorldPosition(), data.bot.GetWorldPosition()) <= 22 select P).FirstOrDefault().GetWorldPosition().X) < 0)
			: !Q.IsRemoved) select Q).FirstOrDefault();

			BotBehaviorSet set = data.bot.GetBotBehaviorSet();
			set.ChaseRange = 20;
			set.GuardRange = 9;
			set.ChokePointPlayerCountThreshold = 0;

			set.EliminateEnemies = false;
			set.RangedWeaponUsage = false;
			set.MeleeWeaponUsage = false;
			set.MeleeUsage = false;

			data.bot.SetBotBehaviorSet(set);

			if (node != null && (data.bot.GetGuardTarget() == null||data.bot.GetGuardTarget() != null && (!(data.bot.GetGuardTarget() is IObjectPathNode)
			||data.bot.GetGuardTarget() is IObjectPathNode && Vector2.Distance(data.bot.GetGuardTarget().GetWorldPosition(), flammable.GetWorldPosition()) <= 50)))
			{
				data.bot.SetGuardTarget(node);
				data.cooldown = Game.TotalElapsedGameTime + 1100;
				if (Game.IsEditorTest == true) { Game.CreateDialogue("Evading!", data.bot, duration:1000f); }
			}
		}

		/// Mine Target Script

		if (mine != null)
		{
			if (mine.CustomID == "")
			{
				var w = (IObjectWeldJoint)Game.CreateObject("WeldJoint", mine.GetWorldPosition() + new Vector2(0,8));
				var dest = (IObjectDestroyTargets)Game.CreateObject("DestroyTargets", mine.GetWorldPosition() + new Vector2(0,20));
				var invo = (IObject)Game.CreateObject("InvisibleExplosionBlocker", mine.GetWorldPosition() + new Vector2(-2.5f,0));

				CollisionFilter colf = invo.GetCollisionFilter();
				colf.BlockExplosions = false;

				mine.SetLinearVelocity(mine.GetLinearVelocity() * 1.2f);
				invo.SetMass(0);
				w.SetMass(0);
				dest.SetMass(0);
				invo.SetBodyType(BodyType.Dynamic);
				invo.SetSizeFactor(new Point(2,0));
				invo.SetCollisionFilter(colf);
				dest.AddTriggerDestroyObject(mine);
				dest.AddObjectToDestroy(invo);
				dest.AddObjectToDestroy(w);
				dest.AddObjectToDestroy(dest);
				w.AddTargetObject(mine);
				w.AddTargetObject(invo);
			 	invo.SetTargetAIData(new ObjectAITargetData(110, PlayerTeam.Independent, ObjectAITargetMode.RangeOnly, 1), true);  
				mine.CustomID = "mine";
			}
					
			if (((data.bot.CurrentSecondaryWeapon).WeaponItem == WeaponItem.NONE && (data.bot.CurrentPrimaryWeapon).WeaponItem == WeaponItem.NONE
			||(data.bot.CurrentSecondaryWeapon).WeaponItem != WeaponItem.NONE && (data.bot.CurrentSecondaryWeapon).TotalAmmo == 0 && (data.bot.CurrentPrimaryWeapon).WeaponItem != WeaponItem.NONE && (data.bot.CurrentPrimaryWeapon).TotalAmmo == 0
			||(data.bot.CurrentSecondaryWeapon).WeaponItem == WeaponItem.NONE && (data.bot.CurrentPrimaryWeapon).WeaponItem != WeaponItem.NONE && (data.bot.CurrentPrimaryWeapon).TotalAmmo == 0
			||(data.bot.CurrentSecondaryWeapon).WeaponItem != WeaponItem.NONE && (data.bot.CurrentSecondaryWeapon).TotalAmmo == 0 && (data.bot.CurrentPrimaryWeapon).WeaponItem == WeaponItem.NONE))
			{
				if (data.bot.GetBotTarget() is IObject && data.bot.GetBotTarget().Name.ToLower().Contains("invisibleexplosion") && data.bot.GetBotBehaviorSet().EliminateEnemies == true)
				{
					BotBehaviorSet set = data.bot.GetBotBehaviorSet();
					set.EliminateEnemies = false;
					data.bot.SetBotBehaviorSet(set);
					if (Game.IsEditorTest == true) { Game.CreateDialogue("NOT TARGETING!", data.bot, duration:1000f); }
				}
				else
				if (!(data.bot.GetBotTarget() is IObject && data.bot.GetBotTarget().Name.ToLower().Contains("invisibleexplosion")) && data.bot.GetBotBehaviorSet().EliminateEnemies == false)
				{
					BotBehaviorSet set = data.bot.GetBotBehaviorSet();
					set.EliminateEnemies = true;
					data.bot.SetBotBehaviorSet(set);
					if (Game.IsEditorTest == true) { Game.CreateDialogue("NORMAL ELIM!", data.bot, duration:1000f); }
				}
			}

			if (mine != null && Vector2.Distance(data.bot.GetWorldPosition(), mine.GetWorldPosition()) <= 40
			&& ((data.bot.CurrentSecondaryWeapon).WeaponItem != WeaponItem.NONE && (data.bot.CurrentSecondaryWeapon).TotalAmmo != 0
			||(data.bot.CurrentPrimaryWeapon).WeaponItem != WeaponItem.NONE && (data.bot.CurrentPrimaryWeapon).TotalAmmo != 0)) 
			{
				BotBehaviorSet set = data.bot.GetBotBehaviorSet();

				IObject node = (from Q in Game.GetObjectsByArea<IObjectPathNode>(new Area(data.bot.GetWorldPosition() - new Vector2(80,70), data.bot.GetWorldPosition() + new Vector2(80,70)),PhysicsLayer.Background)
				orderby Vector2.Distance(Q.GetWorldPosition(), mine.GetWorldPosition()) descending where Q != null && Q.GetNodeEnabled() == true
				&& (from N in Q.GetPathNodeConnections() where (N.GetConnectionEnabled() == false||N.GetConnectionBlockedByPathZone() == true) select N).FirstOrDefault() == null
				&& ((from P in Game.GetObjectsByArea<IObjectPathNode>(new Area(data.bot.GetWorldPosition() - new Vector2(80,70), data.bot.GetWorldPosition() + new Vector2(80,70)),PhysicsLayer.Background)

				orderby Vector2.Distance(P.GetWorldPosition(), data.bot.GetWorldPosition()) ascending where P != null && (P.GetNodeEnabled() == false||(from O in P.GetPathNodeConnections() where (O.GetConnectionEnabled() == false||O.GetConnectionBlockedByPathZone() == true) select O).FirstOrDefault() != null) && Vector2.Distance(P.GetWorldPosition(), data.bot.GetWorldPosition()) <= 22 select P).FirstOrDefault() != null ?
				((data.bot.GetWorldPosition().X - (from P in Game.GetObjectsByArea<IObjectPathNode>(new Area(data.bot.GetWorldPosition() - new Vector2(80,70), data.bot.GetWorldPosition() + new Vector2(80,70)),PhysicsLayer.Background)

				orderby Vector2.Distance(P.GetWorldPosition(), data.bot.GetWorldPosition()) ascending where P != null && (P.GetNodeEnabled() == false||(from O in P.GetPathNodeConnections() where (O.GetConnectionEnabled() == false||O.GetConnectionBlockedByPathZone() == true) select O).FirstOrDefault() != null) && Vector2.Distance(P.GetWorldPosition(), data.bot.GetWorldPosition()) <= 22 select P).FirstOrDefault().GetWorldPosition().X) >= 0 ? (Q.GetWorldPosition().X - (from P in Game.GetObjectsByArea<IObjectPathNode>(new Area(data.bot.GetWorldPosition() - new Vector2(80,70), data.bot.GetWorldPosition() + new Vector2(80,70)),PhysicsLayer.Background) orderby Vector2.Distance(P.GetWorldPosition(), data.bot.GetWorldPosition()) ascending where P != null && (P.GetNodeEnabled() == false||(from O in P.GetPathNodeConnections() where (O.GetConnectionEnabled() == false||O.GetConnectionBlockedByPathZone() == true) select O).FirstOrDefault() != null) && Vector2.Distance(P.GetWorldPosition(), data.bot.GetWorldPosition()) <= 22 select P).FirstOrDefault().GetWorldPosition().X) >= 0 : (Q.GetWorldPosition().X -
				(from P in Game.GetObjectsByArea<IObjectPathNode>(new Area(data.bot.GetWorldPosition() - new Vector2(80,70), data.bot.GetWorldPosition() + new Vector2(80,70)),PhysicsLayer.Background) orderby Vector2.Distance(P.GetWorldPosition(), data.bot.GetWorldPosition()) ascending where P != null && (P.GetNodeEnabled() == false||(from O in P.GetPathNodeConnections() where (O.GetConnectionEnabled() == false||O.GetConnectionBlockedByPathZone() == true) select O).FirstOrDefault() != null) && Vector2.Distance(P.GetWorldPosition(), data.bot.GetWorldPosition()) <= 22 select P).FirstOrDefault().GetWorldPosition().X) < 0)
				: !Q.IsRemoved) select Q).FirstOrDefault();

				set.ChaseRange = 20;
				set.GuardRange = 9;
				set.ChokePointPlayerCountThreshold = 0;

				set.EliminateEnemies = false;
				set.RangedWeaponUsage = false;
				set.MeleeWeaponUsage = false;
				set.MeleeUsage = false;

				data.bot.SetBotBehaviorSet(set);

				if (node != null && (data.bot.GetGuardTarget() == null||data.bot.GetGuardTarget() != null && (!(data.bot.GetGuardTarget() is IObjectPathNode)
				||data.bot.GetGuardTarget() is IObjectPathNode && Vector2.Distance(data.bot.GetGuardTarget().GetWorldPosition(), mine.GetWorldPosition()) <= 50)))
				{
					data.bot.SetGuardTarget(node);
					if (Game.IsEditorTest == true) { Game.CreateDialogue("Evading!", data.bot, duration:1000f); }
				}
			}
		}

		if (data.bot.GetGuardTarget() is IObjectPathNode && data.bot.GetBotBehaviorSet().EliminateEnemies == false
		&& (mine == null||mine != null && Vector2.Distance(data.bot.GetWorldPosition(), mine.GetWorldPosition()) > 40) && streetsweeperwreck == null && grenade == null && flammable == null)
		{
			BotBehaviorSet set = data.bot.GetBotBehaviorSet();
			set.EliminateEnemies = true;
			set.RangedWeaponUsage = true;
			set.MeleeWeaponUsage = true;
			set.MeleeUsage = true;
			set.ChokePointPlayerCountThreshold = 1;
			data.bot.SetBotBehaviorSet(set);
			if (Game.IsEditorTest == true) { Game.CreateDialogue("Fighting!", data.bot, duration:1000f); }
		}

		if (data.bot.GetGuardTarget() is IObjectPathNode && data.cooldown < Game.TotalElapsedGameTime
		&& (mine == null||mine != null && Vector2.Distance(data.bot.GetWorldPosition(), mine.GetWorldPosition()) > 40) && streetsweeperwreck == null && grenade == null && flammable == null)
		{
			BotBehaviorSet set = data.bot.GetBotBehaviorSet();
			set.EliminateEnemies = true;
			set.RangedWeaponUsage = true;
			set.MeleeWeaponUsage = true;
			set.MeleeUsage = true;
			set.ChokePointPlayerCountThreshold = 1;
			data.bot.SetBotBehaviorSet(set);
			data.bot.SetGuardTarget(null);
			if (Game.IsEditorTest == true) { Game.CreateDialogue("Not Guarding!", data.bot, duration:1000f); }
		}
	}
}