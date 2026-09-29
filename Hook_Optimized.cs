//GrapplingHook Script - Performance Optimized Version
//Made by ClockworkDice
//Optimized for memory efficiency and performance

public class RopeController
{
	// Object references
	public IObjectDistanceJoint distanceJoint;
	public IObjectTargetObjectJoint targetObjectJoint;
	public IObjectDistanceJoint regulatorDistanceJoint;
	public IObjectTargetObjectJoint regulatorTargetObjectJoint;
	public IPlayer ply;
	public IObject playerSwingRegulator;
	public IObject anchor;
	public IObject hook;

	// State flags (packed for better cache performance)
	public bool isOnRope;
	private bool wasWalkingPressed;
	private bool walkPressConsumedByCancel;
	private bool pendingGrab;
	private bool pulling;
	private bool isAiming;
	private bool wasRolling;
	private bool hasLeftGroundSinceGrab;
	
	// Timing values
	private float ropeReleaseTime;
	private float lastMeleeActionTime;
	private float rollPowerUpEndTime;
	private float walkKeyHoldTime;
	private float grabTime;
	private float lastRopeShrinkTime;
	private float aimAngle;
	private float originalImpactDamageMod = -1f;
	private float currentGas = 100f;
	
	// Counters
	private int meleeAreaAttacksRemaining;
	
	// Vectors (cached to avoid allocations)
	private Vector2 aimStartPosition;
	private Vector2 pendingAnchorPos;
	private Vector2 hookThrowPos;
	
	// Object reference
	private IObjectText aimIndicator;
	
	// Constants (all in one place for easy tuning)
	private const float IMPACT_PROTECTION_DURATION = 2000f;
	private const float MELEE_COOLDOWN = 100f;
	private const int MAX_MELEE_AREA_ATTACKS_PER_ROPE = 2;
	private const float MELEE_DAMAGE_MAX_DISTANCE = 23f;
	private const float MELEE_DAMAGE_MID_DISTANCE = 15f;
	private const float MELEE_DAMAGE_MIN_DISTANCE = 3f;
	private const float MELEE_DAMAGE_MIN_MULTIPLIER = 1.5f;
	private const float MELEE_DAMAGE_MID_MULTIPLIER = 2f;
	private const float MELEE_DAMAGE_MAX_MULTIPLIER = 2.5f;
	private const float MELEE_DAMAGE_OBJECT_MODIFIER = 1f;
	private const float GAS_MAX_CAPACITY = 100f;
	private const float GAS_PULL_COST_PER_SECOND = 10f;
	private const float GAS_THROW_COST = 2f;
	private const float GAS_REFILL_FROM_CRATE = 17f;
	private const string GAS_REFILL_OBJECT_PREFIX = "Supply";
	private const float ROLL_POWERUP_DURATION = 2000f;
	private const float ROLL_HIT_FALL_INPUT_DISABLE_DURATION = 2000f;
	private const float AIM_DISTANCE = 25f;
	private const float AIM_ROTATE_SPEED = 0.06f;
	private const float QUICK_TAP_THRESHOLD = 200f;
	private const float GRAB_DELAY_MS = 100f;
	private const float PULL_FORCE = 0.7f;
	private const float MIN_PULL_DIST = 20f;
	private const float MIN_HOOK_BREAK_DIST = 60f;
	private const float MAX_HOOK_RANGE = 300f;
	private const float ROPE_SHRINK_INTERVAL_MS = 100f;
	private const float GAS_COST_PER_TICK = (GAS_PULL_COST_PER_SECOND / 100f); // Precomputed
	private const float DOWNWARD_FORCE_REDUCTION = 0.8f;

	// Static list for pending fall releases (shared across all controllers)
	private static List<PendingFallRelease> pendingFallReleases = new List<PendingFallRelease>(4); // Pre-allocate capacity

	private class PendingFallRelease
	{
		public IPlayer Player;
		public float ReleaseTime;
		public PendingFallRelease(IPlayer player, float releaseTime)
		{
			this.Player = player;
			this.ReleaseTime = releaseTime;
		}
	}

	public RopeController(IPlayer ply)
	{
		this.ply = ply;
		
		// Apply custom clothing
		IProfile profile = ply.GetProfile();
		Gender playerGender = profile.Gender;
		
		profile.ChestOver = new IProfileClothingItem(
			playerGender == Gender.Female ? "Jacket_fem" : "Jacket",
			"ClothingOrange", "ClothingOrange"
		);
		profile.Feet = new IProfileClothingItem("RidingBoots", "ClothingDarkBrown");
		profile.Accessory = new IProfileClothingItem(
			playerGender == Gender.Female ? "Armband_fem" : "Armband",
			"ClothingGray"
		);
		
		ply.SetProfile(profile);

		// Setup gas system
		PlayerModifiers mods = ply.GetModifiers();
		mods.MaxEnergy = (int)GAS_MAX_CAPACITY;
		mods.CurrentEnergy = currentGas;
		mods.EnergyRechargeModifier = 0f;
		mods.EnergyConsumptionModifier = 0f;
		ply.SetModifiers(mods);
	}
	
	private void CancelRope()
	{
		// Destroy all rope-related objects
		if(distanceJoint != null) { distanceJoint.Destroy(); distanceJoint = null; }
		if(targetObjectJoint != null) { targetObjectJoint.Destroy(); targetObjectJoint = null; }
		if(regulatorDistanceJoint != null) { regulatorDistanceJoint.Destroy(); regulatorDistanceJoint = null; }
		if(regulatorTargetObjectJoint != null) { regulatorTargetObjectJoint.Destroy(); regulatorTargetObjectJoint = null; }
		if(anchor != null) { anchor.Destroy(); anchor = null; }
		if(playerSwingRegulator != null) { playerSwingRegulator.Destroy(); playerSwingRegulator = null; }
		if(hook != null) { hook.Destroy(); hook = null; }
		
		pendingGrab = false;
		pulling = false;
		
		if(isOnRope) ropeReleaseTime = Game.TotalElapsedGameTime;
		isOnRope = false;
		
		walkKeyHoldTime = Game.TotalElapsedGameTime;
	}
	
	private void CancelAiming()
	{
		if(aimIndicator != null)
		{
			aimIndicator.Remove();
			aimIndicator = null;
		}
		isAiming = false;
	}
	
	private void ThrowHook(float angleRadians)
	{
		if(currentGas < GAS_THROW_COST)
		{
			Game.PlaySound("OutOfAmmoHeavy", ply.GetWorldPosition());
			return;
		}
		
		currentGas -= GAS_THROW_COST;
		if(currentGas < 0f) currentGas = 0f;
		
		float cos = (float)Math.Cos(angleRadians);
		float sin = (float)Math.Sin(angleRadians);
		Vector2 playerPos = ply.GetWorldPosition();
		
		hook = Game.CreateObject("Bottle00Broken", 
			new Vector2(playerPos.X + cos * 10, playerPos.Y + 10),
			0f,
			new Vector2(cos * 40, sin * 40),
			0f);
		hookThrowPos = playerPos;
	}
	
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private float GetDefaultAimAngle()
	{
		return (float)Math.Atan2(20f, ply.FacingDirection * 20f);
	}
	
	private void BuildRegulatorJoints()
	{
		if(regulatorDistanceJoint != null) regulatorDistanceJoint.Destroy();
		if(regulatorTargetObjectJoint != null) regulatorTargetObjectJoint.Destroy();
		
		Vector2 anchorPos = anchor.GetWorldPosition();
		Vector2 regPos = playerSwingRegulator.GetWorldPosition();
		
		IObjectDistanceJoint newDistanceJoint = (IObjectDistanceJoint)Game.CreateObject("DistanceJoint");
		newDistanceJoint.SetWorldPosition(anchorPos);
		newDistanceJoint.SetTargetObject(anchor);
	
		IObjectTargetObjectJoint newTargetObjectJoint = (IObjectTargetObjectJoint)Game.CreateObject("TargetObjectJoint");
		newTargetObjectJoint.SetWorldPosition(new Vector2(regPos.X, regPos.Y + 8f));
		newTargetObjectJoint.SetTargetObject(playerSwingRegulator);
	
		newDistanceJoint.SetTargetObjectJoint(newTargetObjectJoint);
		newDistanceJoint.SetLineVisual(LineVisual.DJWire);
		newDistanceJoint.SetLengthType(DistanceJointLengthType.Elastic);
		
		regulatorDistanceJoint = newDistanceJoint;
		regulatorTargetObjectJoint = newTargetObjectJoint;
	}
	
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private void SyncGasBar()
	{
		PlayerModifiers mods = ply.GetModifiers();
		mods.CurrentEnergy = currentGas;
		ply.SetModifiers(mods);
	}
	
	public void Update()
	{
		if (ply.IsDead) return;
		
		// Gas refill check (optimized with early exit)
		if(currentGas < GAS_MAX_CAPACITY) CheckForCrateRefill();

		bool walkJustPressed = ply.IsWalking && !wasWalkingPressed;
		bool walkJustReleased = !ply.IsWalking && wasWalkingPressed;
		
		// Cancel rope/hook if walk pressed while rope active
		if(walkJustPressed && (hook != null || pendingGrab || isOnRope))
		{
			CancelAiming();
			CancelRope();
			walkPressConsumedByCancel = true;
			wasWalkingPressed = ply.IsWalking;
			return;
		}
		
		// Aiming system (optimized flow)
		if(walkJustPressed && !isOnRope && hook == null && !pendingGrab && !isAiming)
		{
			walkKeyHoldTime = Game.TotalElapsedGameTime;
		}
		
		if(ply.IsWalking && !walkPressConsumedByCancel && !isOnRope && hook == null && !pendingGrab && !isAiming)
		{
			if(Game.TotalElapsedGameTime - walkKeyHoldTime >= QUICK_TAP_THRESHOLD)
			{
				isAiming = true;
				aimAngle = GetDefaultAimAngle();
				aimStartPosition = ply.GetWorldPosition();
				
				aimIndicator = (IObjectText)Game.CreateObject("Text", aimStartPosition);
				aimIndicator.SetText("+");
				aimIndicator.SetTextScale(0.8f);
				aimIndicator.SetTextAlignment(TextAlignment.Middle);
				aimIndicator.SetTextColor(new Color(255, 255, 255));
			}
		}
		
		if(isAiming)
		{
			// Rotation input
			if(ply.KeyPressed(ply.FacingDirection == 1 ? VirtualKey.AIM_RUN_RIGHT : VirtualKey.CROUCH_ROLL_DIVE))
				aimAngle -= AIM_ROTATE_SPEED;
			if(ply.KeyPressed(ply.FacingDirection == 1 ? VirtualKey.CROUCH_ROLL_DIVE : VirtualKey.AIM_RUN_LEFT))
				aimAngle += AIM_ROTATE_SPEED;
			
			// Lock position
			Vector2 pos = ply.GetWorldPosition();
			pos.X = aimStartPosition.X;
			ply.SetWorldPosition(pos);
			
			Vector2 vel = ply.GetLinearVelocity();
			vel.X = 0f;
			ply.SetLinearVelocity(vel);
			
			// Update indicator
			if(aimIndicator != null)
			{
				Vector2 playerPos = ply.GetWorldPosition();
				aimIndicator.SetWorldPosition(new Vector2(
					playerPos.X + (float)Math.Cos(aimAngle) * AIM_DISTANCE,
					playerPos.Y + (float)Math.Sin(aimAngle) * AIM_DISTANCE
				));
			}
			
			if(walkJustReleased)
			{
				ThrowHook(aimAngle);
				CancelAiming();
			}
		}
		else if(walkJustReleased && !walkPressConsumedByCancel && hook == null && !pendingGrab && !isOnRope)
		{
			if(Game.TotalElapsedGameTime - walkKeyHoldTime < QUICK_TAP_THRESHOLD)
			{
				ThrowHook(GetDefaultAimAngle());
			}
		}
		
		if(walkJustReleased) walkPressConsumedByCancel = false;
		
		// Hook collision detection (optimized with early exits)
		if(hook != null && !hook.DestructionInitiated)
		{
			Vector2 hookPos = hook.GetWorldPosition();
			float distanceFromThrow = Vector2.Distance(hookPos, hookThrowPos);
			
			if(distanceFromThrow >= MAX_HOOK_RANGE)
			{
				hook.Destroy();
			}
			else if(distanceFromThrow >= MIN_HOOK_BREAK_DIST)
			{
				Area checkArea = new Area(hookPos.Y + 5, hookPos.X - 5, hookPos.Y - 5, hookPos.X + 5);
				IObject[] nearbyObjects = Game.GetObjectsByArea(checkArea);
				
				bool hitSomething = false;
				int objCount = nearbyObjects.Length;
				for(int i = 0; i < objCount && !hitSomething; i++)
				{
					IObject obj = nearbyObjects[i];
					string objName = obj.Name;
					if(obj.UniqueID != hook.UniqueID && obj.UniqueID != ply.UniqueID &&
					   !objName.StartsWith("Bg") && !objName.StartsWith("BG") &&
					   !objName.StartsWith("SoundArea") && !objName.Contains("Spawn") &&
					   !objName.Contains("Trigger") && !objName.Contains("Ladder") &&
					   !objName.Contains("Marker"))
					{
						hitSomething = true;
					}
				}
				
				// Player proximity check
				if(!hitSomething)
				{
					IPlayer[] players = Game.GetPlayers();
					int pCount = players.Length;
					for(int i = 0; i < pCount && !hitSomething; i++)
					{
						IPlayer p = players[i];
						if(p.UniqueID != ply.UniqueID && Vector2.Distance(hookPos, p.GetWorldPosition()) < 10f)
						{
							hitSomething = true;
						}
					}
				}
				
				// Ground check
				if(!hitSomething)
				{
					RayCastResult[] groundCheck = Game.RayCast(hookPos, new Vector2(hookPos.X, hookPos.Y - 8), new RayCastInput() { IncludeOverlap = true });
					if(groundCheck.Length > 0 && groundCheck[0].Hit && groundCheck[0].HitObject == null)
					{
						hitSomething = true;
					}
				}
				
				if(hitSomething) hook.Destroy();
			}
		}
		
		// Hook destroyed check
		if(hook != null && hook.DestructionInitiated)
		{
			pendingAnchorPos = hook.GetWorldPosition();
			grabTime = Game.TotalElapsedGameTime + GRAB_DELAY_MS;
			pendingGrab = true;
			hook = null;
		}
		
		// Rope creation
		if(pendingGrab && Game.TotalElapsedGameTime >= grabTime)
		{
			CreateRope(pendingAnchorPos);
			pendingGrab = false;
			pulling = true;
			isOnRope = true;
		}

		// Pulling physics (optimized calculations)
		if(pulling && anchor != null && playerSwingRegulator != null)
		{
			if(currentGas <= 0f)
			{
				pulling = false;
				BuildRegulatorJoints();
				lastRopeShrinkTime = Game.TotalElapsedGameTime;
			}
			else
			{
				currentGas -= GAS_COST_PER_TICK;
				if(currentGas < 0f) currentGas = 0f;
			}
			
			if(pulling)
			{
				Vector2 regPos = playerSwingRegulator.GetWorldPosition();
				Vector2 anchorPos = anchor.GetWorldPosition();
				float dist = Vector2.Distance(regPos, anchorPos);
				
				if(dist > MIN_PULL_DIST)
				{
					Vector2 dir = new Vector2(anchorPos.X - regPos.X, anchorPos.Y - regPos.Y);
					float magnitude = (float)Math.Sqrt(dir.X * dir.X + dir.Y * dir.Y);
					dir.X /= magnitude;
					dir.Y /= magnitude;
					
					float pullForce = PULL_FORCE;
					if(dir.Y < 0f)
					{
						pullForce *= (1f + dir.Y * DOWNWARD_FORCE_REDUCTION);
					}
					
					Vector2 currentVel = playerSwingRegulator.GetLinearVelocity();
					playerSwingRegulator.SetLinearVelocity(new Vector2(
						currentVel.X + dir.X * pullForce,
						currentVel.Y + dir.Y * pullForce
					));
					
					if(Game.TotalElapsedGameTime - lastRopeShrinkTime >= ROPE_SHRINK_INTERVAL_MS)
					{
						BuildRegulatorJoints();
						lastRopeShrinkTime = Game.TotalElapsedGameTime;
					}
				}
				else
				{
					pulling = false;
					BuildRegulatorJoints();
				}
			}
		}
		
		// Sync player with regulator
		if(playerSwingRegulator != null)
		{
			ply.SetLinearVelocity(playerSwingRegulator.GetLinearVelocity());
			ply.SetWorldPosition(playerSwingRegulator.GetWorldPosition());
		}
		
		// Impact protection
		float currentTime = Game.TotalElapsedGameTime;
		bool shouldHaveProtection = isOnRope || (ropeReleaseTime > 0f && (currentTime - ropeReleaseTime) < IMPACT_PROTECTION_DURATION);
		
		PlayerModifiers protectionMods = ply.GetModifiers();
		if(shouldHaveProtection)
		{
			if(originalImpactDamageMod < 0f && protectionMods.ImpactDamageTakenModifier >= 0f)
			{
				originalImpactDamageMod = protectionMods.ImpactDamageTakenModifier;
			}
			protectionMods.ImpactDamageTakenModifier = 0.5f;
			ply.SetModifiers(protectionMods);
		}
		else if(originalImpactDamageMod >= 0f)
		{
			protectionMods.ImpactDamageTakenModifier = originalImpactDamageMod;
			ply.SetModifiers(protectionMods);
			originalImpactDamageMod = -1f;
			ropeReleaseTime = 0f;
		}
		
		// Roll detection
		if(isOnRope)
		{
			bool isRollingNow = ply.IsRolling || ply.IsRecoveryRolling;
			if(wasRolling && !isRollingNow)
			{
				rollPowerUpEndTime = currentTime + ROLL_POWERUP_DURATION;
			}
			wasRolling = isRollingNow;
			
			if(!ply.IsOnGround) hasLeftGroundSinceGrab = true;
		}
		else
		{
			wasRolling = false;
		}
		
		SyncGasBar();
		wasWalkingPressed = ply.IsWalking;
	}
	
	private void CheckForCrateRefill()
	{
		Vector2 playerPos = ply.GetWorldPosition();
		Area checkArea = new Area(playerPos.Y + 15, playerPos.X - 15, playerPos.Y - 15, playerPos.X + 15);
		IObject[] nearbyObjects = Game.GetObjectsByArea(checkArea);
		
		int count = nearbyObjects.Length;
		for(int i = 0; i < count; i++)
		{
			IObject obj = nearbyObjects[i];
			if(obj.Name.StartsWith(GAS_REFILL_OBJECT_PREFIX) && obj.DestructionInitiated)
			{
				currentGas += GAS_REFILL_FROM_CRATE;
				if(currentGas > GAS_MAX_CAPACITY) currentGas = GAS_MAX_CAPACITY;
				
				Game.PlaySound("StrengthBoostStart", playerPos);
				obj.Remove();
			}
		}
	}
	
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private float CalculateDistanceMultiplier(float distance)
	{
		if(distance >= MELEE_DAMAGE_MAX_DISTANCE) return MELEE_DAMAGE_MIN_MULTIPLIER;
		if(distance <= MELEE_DAMAGE_MIN_DISTANCE) return MELEE_DAMAGE_MAX_MULTIPLIER;
		
		if(distance >= MELEE_DAMAGE_MID_DISTANCE)
		{
			float t = (MELEE_DAMAGE_MAX_DISTANCE - distance) / (MELEE_DAMAGE_MAX_DISTANCE - MELEE_DAMAGE_MID_DISTANCE);
			return MELEE_DAMAGE_MIN_MULTIPLIER + (MELEE_DAMAGE_MID_MULTIPLIER - MELEE_DAMAGE_MIN_MULTIPLIER) * t;
		}
		else
		{
			float t = (MELEE_DAMAGE_MID_DISTANCE - distance) / (MELEE_DAMAGE_MID_DISTANCE - MELEE_DAMAGE_MIN_DISTANCE);
			return MELEE_DAMAGE_MID_MULTIPLIER + (MELEE_DAMAGE_MAX_MULTIPLIER - MELEE_DAMAGE_MID_MULTIPLIER) * t;
		}
	}
	
	public void OnMeleeAction()
	{
		if(!isOnRope || !hasLeftGroundSinceGrab) return;
		if(Game.TotalElapsedGameTime - lastMeleeActionTime < MELEE_COOLDOWN) return;
		if(meleeAreaAttacksRemaining <= 0) return;
		
		lastMeleeActionTime = Game.TotalElapsedGameTime;
		meleeAreaAttacksRemaining--;
		
		Vector2 playerPos = ply.GetWorldPosition();
		
		PlayerModifiers attackerMods = ply.GetModifiers();
		float meleeDamageDealt = attackerMods.MeleeDamageDealtModifier;
		if(meleeDamageDealt < 0f) meleeDamageDealt = 1f;
		
		// Weapon damage (optimized with switch-like behavior)
		WeaponItem currentMelee = ply.CurrentMeleeWeapon.WeaponItem;
		float weaponDamage = 7f;
		if(currentMelee == WeaponItem.AXE) weaponDamage = 32f;
		else if(currentMelee == WeaponItem.KATANA) weaponDamage = 26f;
		else if(currentMelee == WeaponItem.MACHETE) weaponDamage = 19f;
		else if(currentMelee == WeaponItem.LEAD_PIPE) weaponDamage = 16f;
		else if(currentMelee == WeaponItem.KNIFE) weaponDamage = 15f;
		else if(currentMelee == WeaponItem.BAT || currentMelee == WeaponItem.HAMMER) weaponDamage = 14f;
		else if(currentMelee == WeaponItem.PIPE) weaponDamage = 12f;
		else if(currentMelee == WeaponItem.BATON) weaponDamage = 11f;
		else if(currentMelee == WeaponItem.CHAIN) weaponDamage = 10f;
		else if(currentMelee == WeaponItem.BOTTLE) weaponDamage = 9f;
		
		bool hasRollPowerUp = Game.TotalElapsedGameTime < rollPowerUpEndTime;
		bool isRecoveryRolling = ply.IsRecoveryRolling;
		bool isRollingNow = ply.IsRolling || isRecoveryRolling;
		
		PlayerTeam attackerTeam = ply.GetTeam();
		
		// Player damage loop (optimized)
		IPlayer[] players = Game.GetPlayers();
		int playerCount = players.Length;
		for(int i = 0; i < playerCount; i++)
		{
			IPlayer target = players[i];
			if(target.UniqueID == ply.UniqueID || target.IsDead) continue;
			
			Vector2 targetPos = target.GetWorldPosition();
			float distance = Vector2.Distance(playerPos, targetPos);
			
			if(distance <= MELEE_DAMAGE_MAX_DISTANCE)
			{
				PlayerTeam targetTeam = target.GetTeam();
				bool shouldDamage = (attackerTeam == PlayerTeam.Independent && targetTeam == PlayerTeam.Independent) ||
				                   (attackerTeam != targetTeam && targetTeam != PlayerTeam.Independent);
				
				if(shouldDamage)
				{
					float distanceMultiplier = CalculateDistanceMultiplier(distance);
					
					if(hasRollPowerUp)
					{
						distanceMultiplier *= isRecoveryRolling ? 4f : 2f;
					}
					
					float totalDamage = weaponDamage * meleeDamageDealt * distanceMultiplier;
					bool willDie = target.GetHealth() <= totalDamage;
					
					target.DealDamage(totalDamage);
					Game.PlayEffect(EffectName.Blood, targetPos);
					
					if(hasRollPowerUp)
					{
						Game.PlayEffect(EffectName.Blood, targetPos);
						Game.PlayEffect(EffectName.Blood, targetPos);
						Game.PlaySound("KatanaDraw", targetPos);
						ForceFall(target);
						
						if(isRecoveryRolling && willDie) target.Gib();
					}
					else
					{
						Game.PlaySound("MacheteDraw", targetPos);
					}
				}
			}
		}
		
		// Object damage (only if rolling)
		if(isRollingNow || hasRollPowerUp)
		{
			Area objectCheckArea = new Area(
				playerPos.Y + MELEE_DAMAGE_MAX_DISTANCE,
				playerPos.X - MELEE_DAMAGE_MAX_DISTANCE,
				playerPos.Y - MELEE_DAMAGE_MAX_DISTANCE,
				playerPos.X + MELEE_DAMAGE_MAX_DISTANCE
			);
			IObject[] nearbyObjects = Game.GetObjectsByArea(objectCheckArea);
			
			// Cache player UIDs for faster lookup
			int playerUIDCount = playerCount;
			int[] playerUIDs = new int[playerUIDCount];
			for(int i = 0; i < playerUIDCount; i++)
			{
				playerUIDs[i] = players[i].UniqueID;
			}
			
			int objCount = nearbyObjects.Length;
			for(int i = 0; i < objCount; i++)
			{
				IObject obj = nearbyObjects[i];
				if(obj == null || obj.IsRemoved || !obj.Destructable) continue;
				
				string objName = obj.Name;
				if(objName.StartsWith("Bg") || objName.StartsWith("BG")) continue;
				if(objName == "BarrelExplosive" || objName == "BarrelWreck" || objName == "PropaneTank") continue;
				
				int objUID = obj.UniqueID;
				if(objUID == anchor.UniqueID || objUID == playerSwingRegulator.UniqueID) continue;
				if(objName.Contains("Trigger") || objName.Contains("Spawn") || objName.Contains("Marker")) continue;
				
				// Check if object is a player
				bool isPlayer = false;
				for(int j = 0; j < playerUIDCount; j++)
				{
					if(playerUIDs[j] == objUID)
					{
						isPlayer = true;
						break;
					}
				}
				if(isPlayer) continue;
				
				Vector2 objPos = obj.GetWorldPosition();
				float distance = Vector2.Distance(playerPos, objPos);
				
				if(distance <= MELEE_DAMAGE_MAX_DISTANCE)
				{
					float distanceMultiplier = CalculateDistanceMultiplier(distance);
					
					if(hasRollPowerUp)
					{
						distanceMultiplier *= isRecoveryRolling ? 4f : 2f;
					}
					
					float totalObjectDamage = weaponDamage * meleeDamageDealt * distanceMultiplier * MELEE_DAMAGE_OBJECT_MODIFIER;
					obj.DealDamage(totalObjectDamage);
					Game.PlayEffect(EffectName.Smack, objPos);
				}
			}
		}
	}
	
	public void CreateRope(Vector2 anchorPos)
	{
		// Cleanup existing
		if(distanceJoint != null) { distanceJoint.Destroy(); distanceJoint = null; }
		if(targetObjectJoint != null) { targetObjectJoint.Destroy(); targetObjectJoint = null; }
		if(regulatorDistanceJoint != null) { regulatorDistanceJoint.Destroy(); regulatorDistanceJoint = null; }
		if(regulatorTargetObjectJoint != null) { regulatorTargetObjectJoint.Destroy(); regulatorTargetObjectJoint = null; }
		if(anchor != null) { anchor.Destroy(); anchor = null; }
		if(playerSwingRegulator != null) { playerSwingRegulator.Destroy(); playerSwingRegulator = null; }
		
		anchor = Game.CreateObject("BgValve00E", anchorPos, 0f);
		playerSwingRegulator = Game.CreateObject("StoneDebris00A", ply.GetWorldPosition(), 0f);
		playerSwingRegulator.SetLinearVelocity(ply.GetLinearVelocity());
		
		BuildRegulatorJoints();
		
		lastRopeShrinkTime = Game.TotalElapsedGameTime;
		meleeAreaAttacksRemaining = MAX_MELEE_AREA_ATTACKS_PER_ROPE;
		wasRolling = false;
		rollPowerUpEndTime = 0f;
		hasLeftGroundSinceGrab = false;
	}
	
	private void ForceFall(IPlayer target)
	{
		target.SetInputEnabled(false);
		target.ClearCommandQueue();
		target.AddCommand(new PlayerCommand(PlayerCommandType.Fall));
		
		float releaseTime = Game.TotalElapsedGameTime + ROLL_HIT_FALL_INPUT_DISABLE_DURATION;
		
		// Find existing entry
		int count = pendingFallReleases.Count;
		for(int i = 0; i < count; i++)
		{
			PendingFallRelease p = pendingFallReleases[i];
			if(p.Player != null && p.Player.UniqueID == target.UniqueID)
			{
				p.ReleaseTime = releaseTime;
				return;
			}
		}
		
		pendingFallReleases.Add(new PendingFallRelease(target, releaseTime));
	}
	
	public static void ProcessPendingFallReleases()
	{
		float currentTime = Game.TotalElapsedGameTime;
		for(int i = pendingFallReleases.Count - 1; i >= 0; i--)
		{
			PendingFallRelease pending = pendingFallReleases[i];
			if(currentTime >= pending.ReleaseTime)
			{
				if(pending.Player != null && !pending.Player.IsRemoved)
				{
					pending.Player.SetInputEnabled(true);
				}
				pendingFallReleases.RemoveAt(i);
			}
		}
	}
}

List<RopeController> ropeControllers = new List<RopeController>();
Events.PlayerMeleeActionCallback meleeCallback = null;

public void OnStartup()
{
	IObjectTimerTrigger Timer0 = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
	Timer0.SetIntervalTime(10);
	Timer0.SetRepeatCount(0);
	Timer0.SetScriptMethod("GrapplingHook");
	Timer0.Trigger();
	
	foreach(IPlayer ply in Game.GetPlayers())
	{
		if (ply.IsBot) continue;
		ropeControllers.Add(new RopeController(ply));
	}
	
	meleeCallback = Events.PlayerMeleeActionCallback.Start(OnPlayerMeleeAction);
}

public void GrapplingHook(TriggerArgs args)
{
	int count = ropeControllers.Count;
	for(int i = 0; i < count; i++)
	{
		ropeControllers[i].Update();
	}
	
	RopeController.ProcessPendingFallReleases();
}

public void OnPlayerMeleeAction(IPlayer player, PlayerMeleeHitArg[] args)
{
	int playerUID = player.UniqueID;
	int count = ropeControllers.Count;
	for(int i = 0; i < count; i++)
	{
		if(ropeControllers[i].ply.UniqueID == playerUID)
		{
			ropeControllers[i].OnMeleeAction();
			break;
		}
	}
}
