//GrapplingHook Script!
//Made by ClockworkDice

public class RopeController
{
	public IObjectDistanceJoint distanceJoint = null;
	public IObjectTargetObjectJoint targetObjectJoint = null;
	public IObjectDistanceJoint regulatorDistanceJoint = null;
	public IObjectTargetObjectJoint regulatorTargetObjectJoint = null;
	public IPlayer ply;
	public IObject playerSwingRegulator = null;
	public IObject anchor = null;

	public IObject hook = null;

	public bool isOnRope = false;
	private bool wasWalkingPressed = false;
	// True for the remainder of a walk press that was used to cancel a rope/hook,
	// so that press's eventual release isn't misread as a fresh quick-tap throw.
	private bool walkPressConsumedByCancel = false;
	
	private float ropeReleaseTime = 0f; // Time when rope was released
	private const float IMPACT_PROTECTION_DURATION = 2000f; // 2 seconds in milliseconds
	private float originalImpactDamageMod = -1f; // Store original modifier
	
	private float lastMeleeActionTime = 0f; // Track last melee action
	private const float MELEE_COOLDOWN = 100f; // Cooldown between area damage applications

	// --- aiming system ---
	private bool isAiming = false;
	private float aimAngle = 0f;
	private const float AIM_DISTANCE = 25f;
	// Tuned for continuous per-tick rotation (Update runs every 10ms).
	// 0.03 rad/tick ~= 1.7 rad/sec ~= 100 deg/sec. Adjust to taste.
	private const float AIM_ROTATE_SPEED = 0.03f;
	private IObject aimIndicator = null;
	private float walkKeyHoldTime = 0f;
	private const float QUICK_TAP_THRESHOLD = 200f;
	private Vector2 aimStartPosition; // player position when aiming began; X is locked while aiming
	// ---------------------------

	// --- delayed-grab state ---
	private const float GRAB_DELAY_MS = 100f;
	private bool pendingGrab = false;
	private float grabTime = 0f;
	private Vector2 pendingAnchorPos;
	private Vector2 hookThrowPos; // position where hook was thrown from

	// --- pull-in state using physics force ---
	private bool pulling = false;
	private const float PULL_FORCE = 0.55f;        // force applied toward anchor
	private const float MIN_PULL_DIST = 20f;     // stop pulling when this close
	private const float MIN_HOOK_BREAK_DIST = 60f; // minimum distance before hook can break on collision
	private const float MAX_HOOK_RANGE = 300f;   // maximum range before hook breaks automatically
	// ---------------------------

	public RopeController(IPlayer ply)
	{
		this.ply = ply;
		
		// Apply custom clothing to the player's profile
		IProfile profile = ply.GetProfile();
		Gender playerGender = profile.Gender;
		
		// Set ChestOver based on gender
		if(playerGender == Gender.Female)
		{
			profile.ChestOver = new IProfileClothingItem("Jacket_fem", "ClothingOrange", "ClothingOrange");
		}
		else
		{
			profile.ChestOver = new IProfileClothingItem("Jacket", "ClothingOrange", "ClothingOrange");
		}
		
		// Set Feet (same for both genders)
		profile.Feet = new IProfileClothingItem("RidingBoots", "ClothingDarkBrown");
		
		// Set Accessory based on gender
		if(playerGender == Gender.Female)
		{
			profile.Accessory = new IProfileClothingItem("Armband_fem", "ClothingGray");
		}
		else
		{
			profile.Accessory = new IProfileClothingItem("Armband", "ClothingGray");
		}
		
		// Apply the modified profile back to the player
		ply.SetProfile(profile);
	}
	
	private void CancelRope()
	{
		if(this.distanceJoint!=null) this.distanceJoint.Destroy();
		if(this.targetObjectJoint!=null) this.targetObjectJoint.Destroy();
		if(this.regulatorDistanceJoint!=null) this.regulatorDistanceJoint.Destroy();
		if(this.regulatorTargetObjectJoint!=null) this.regulatorTargetObjectJoint.Destroy();
		if(this.anchor!=null) this.anchor.Destroy();
		if(this.playerSwingRegulator!=null) this.playerSwingRegulator.Destroy();
		if(this.hook!=null) this.hook.Destroy();
		
		this.distanceJoint = null;
		this.targetObjectJoint = null;
		this.anchor = null;
		this.playerSwingRegulator = null;
		this.hook = null;
		
		this.pendingGrab = false;
		this.pulling = false;
		
		// If was on rope, set release time for impact protection
		if(this.isOnRope)
		{
			this.ropeReleaseTime = Game.TotalElapsedGameTime;
		}
		this.isOnRope = false;
		
		// Reset hold time relative to *now* so a still-held walk key doesn't
		// instantly read as a long hold and re-trigger aiming.
		// (Do NOT let anything after this overwrite it back to 0 - see CancelAiming.)
		this.walkKeyHoldTime = Game.TotalElapsedGameTime;
	}
	
	private void CancelAiming()
	{
		if(this.aimIndicator != null)
		{
			this.aimIndicator.Remove();
			this.aimIndicator = null;
		}
		this.isAiming = false;
		// NOTE: intentionally NOT touching walkKeyHoldTime here anymore.
		// It used to be reset to 0f, which - when this is called right after
		// CancelRope() in the same tick - wiped out CancelRope's guard and
		// caused aiming to instantly restart while the walk key was still held.
	}
	
	private void ThrowHook(float angleRadians)
	{
		Vector2 direction = new Vector2((float)Math.Cos(angleRadians), (float)Math.Sin(angleRadians));
		hook = Game.CreateObject("Bottle00Broken", 
			ply.GetWorldPosition() + new Vector2(direction.X * 10, 10), 
			0f, 
			direction * 40, 
			0f);
		hookThrowPos = ply.GetWorldPosition();
	}
	
	// Default throw/aim direction: diagonally forward (facing) and downward,
	// equivalent to a raw vector of (FacingDirection*20, 20). Using an angle
	// keeps this compatible with ThrowHook() and the manual-aim system.
	private float GetDefaultAimAngle()
	{
		return (float)Math.Atan2(20f, ply.FacingDirection * 20f);
	}
	
	public void Update()
	{
		// Detect walk key press (rising edge)
		bool walkJustPressed = ply.IsWalking && !this.wasWalkingPressed;
		bool walkJustReleased = !ply.IsWalking && this.wasWalkingPressed;
		
		// Cancel rope/hook if walk pressed while rope active
		if(walkJustPressed && (hook != null || pendingGrab || isOnRope))
		{
			CancelAiming();
			CancelRope();
			this.walkPressConsumedByCancel = true;
			this.wasWalkingPressed = ply.IsWalking;
			return;
		}
		
		// Start tracking hold time when walk pressed (only if nothing is active)
		if(walkJustPressed && !isOnRope && hook == null && !pendingGrab && !isAiming)
		{
			this.walkKeyHoldTime = Game.TotalElapsedGameTime;
		}
		
		// Check if should enter aiming mode (holding walk)
		if(ply.IsWalking && !walkPressConsumedByCancel && !isOnRope && hook == null && !pendingGrab && !isAiming)
		{
			float holdDuration = Game.TotalElapsedGameTime - this.walkKeyHoldTime;
			if(holdDuration >= QUICK_TAP_THRESHOLD)
			{
				// Enter aiming mode
				this.isAiming = true;
				this.aimAngle = GetDefaultAimAngle();
				this.aimStartPosition = ply.GetWorldPosition();
				
				// Create aim indicator
				this.aimIndicator = Game.CreateObject("IsMIcon", ply.GetWorldPosition());
			}
		}
		
		// Update aiming
		if(isAiming)
		{
			// Rotate aim continuously using the left/right virtual keys.
			// These are the same keys that normally move the player left/right,
			// so we cancel out any horizontal velocity they cause below.
			if(ply.KeyPressed(ply.FacingDirection == 1 ? VirtualKey.AIM_RUN_RIGHT : VirtualKey.CROUCH_ROLL_DIVE))
			{
				this.aimAngle -= AIM_ROTATE_SPEED;
			}
			if(ply.KeyPressed(ply.FacingDirection == 1 ? VirtualKey.CROUCH_ROLL_DIVE : VirtualKey.AIM_RUN_LEFT))
			{
				this.aimAngle += AIM_ROTATE_SPEED;
			}
			
			// Prevent the player from actually walking left/right while aiming.
			// Zeroing velocity alone can still let one tick of drift through if
			// the engine moves the player via a direct position step before this
			// script runs - so we also pin X back to where aiming started.
			// Y is left alone so gravity/falling still behaves normally.
			Vector2 pos = ply.GetWorldPosition();
			pos.X = this.aimStartPosition.X;
			ply.SetWorldPosition(pos);
			
			Vector2 vel = ply.GetLinearVelocity();
			vel.X = 0f;
			ply.SetLinearVelocity(vel);
			
			// Update aim indicator position
			if(this.aimIndicator != null)
			{
				Vector2 aimPos = ply.GetWorldPosition() + new Vector2(
					(float)Math.Cos(aimAngle) * AIM_DISTANCE,
					(float)Math.Sin(aimAngle) * AIM_DISTANCE
				);
				this.aimIndicator.SetWorldPosition(aimPos);
			}
			
			// Throw when walk released
			if(walkJustReleased)
			{
				ThrowHook(this.aimAngle);
				CancelAiming();
			}
		}
		else if(walkJustReleased && !walkPressConsumedByCancel && hook == null && !pendingGrab && !isOnRope)
		{
			// Quick tap - throw horizontally
			float holdDuration = Game.TotalElapsedGameTime - this.walkKeyHoldTime;
			if(holdDuration < QUICK_TAP_THRESHOLD)
			{
				float angle = GetDefaultAimAngle();
				ThrowHook(angle);
			}
		}
		
		// The consumed press cycle ends once the key is released - clear the
		// flag so the next fresh press behaves normally again.
		if(walkJustReleased)
		{
			this.walkPressConsumedByCancel = false;
		}
		
		// Check if hook hit something (collided with non-background objects)
		if(hook != null && !hook.DestructionInitiated)
		{
			Vector2 hookPos = hook.GetWorldPosition();
			
			// Check if hook has traveled minimum distance
			float distanceFromThrow = Vector2.Distance(hookPos, hookThrowPos);
			
			// Check if hook exceeded max range - destroy automatically
			if(distanceFromThrow >= MAX_HOOK_RANGE)
			{
				hook.Destroy();
			}
			else if(distanceFromThrow >= MIN_HOOK_BREAK_DIST)
			{
				// Check for collision with objects in a small area around the hook
				Area checkArea = new Area(hookPos.Y + 5, hookPos.X - 5, hookPos.Y - 5, hookPos.X + 5);
				IObject[] nearbyObjects = Game.GetObjectsByArea(checkArea);
				
				bool hitSomething = false;
				foreach(IObject obj in nearbyObjects)
				{
					// Skip the hook itself and background objects
					if(obj.UniqueID != hook.UniqueID && obj.UniqueID != ply.UniqueID && !obj.Name.StartsWith("Bg") && !obj.Name.StartsWith("BG") && !obj.Name.StartsWith("SoundArea") && !obj.Name.Contains("Spawn") && !obj.Name.Contains("Trigger") && !obj.Name.Contains("Ladder") && !obj.Name.Contains("Marker"))
					{
						hitSomething = true;
						break;
					}
				}
				
				// Also check for players near the hook
				if(!hitSomething)
				{
					foreach(IPlayer p in Game.GetPlayers())
					{
						if(p.UniqueID != ply.UniqueID && Vector2.Distance(hookPos, p.GetWorldPosition()) < 10f)
						{
							hitSomething = true;
							break;
						}
					}
				}
				
				// Check for tiles/ground using raycast
				if(!hitSomething)
				{
					RayCastResult[] groundCheck = Game.RayCast(hookPos, hookPos + new Vector2(0, -8), new RayCastInput()
					{
						IncludeOverlap = true
					});
					
					if(groundCheck.Length > 0 && groundCheck[0].Hit && groundCheck[0].HitObject == null) // Hit a tile, not an object
					{
						hitSomething = true;
					}
				}
				
				if(hitSomething)
				{
					// Destroy the hook to trigger rope attachment
					hook.Destroy();
				}
			}
		}
		
		// Check if hook was destroyed (naturally broke or we destroyed it)
		if(hook!=null && hook.DestructionInitiated)
		{
			pendingAnchorPos = hook.GetWorldPosition();
			grabTime = Game.TotalElapsedGameTime + GRAB_DELAY_MS;
			pendingGrab = true;
			hook = null;
		}
		
		// Create rope after delay
		if(pendingGrab && Game.TotalElapsedGameTime >= grabTime)
		{
			CreateRope(pendingAnchorPos);
			pendingGrab = false;
			pulling = true; // start pulling the player toward the anchor
			isOnRope = true;
		}

		// Apply pulling force to the swing regulator which pulls the player
		if(pulling && anchor != null && playerSwingRegulator != null)
		{
			float dist = Vector2.Distance(playerSwingRegulator.GetWorldPosition(), anchor.GetWorldPosition());
			if(dist > MIN_PULL_DIST)
			{
				Vector2 dir = anchor.GetWorldPosition() - playerSwingRegulator.GetWorldPosition();
				dir.Normalize();
				Vector2 currentVel = playerSwingRegulator.GetLinearVelocity();
				Vector2 newVel = currentVel + (dir * PULL_FORCE);
				playerSwingRegulator.SetLinearVelocity(newVel);
			}
			else
			{
				pulling = false; // arrived - stop pulling
			}
		}
		
		// Sync player with swing regulator
		if(playerSwingRegulator!=null)
		{
			ply.SetLinearVelocity(playerSwingRegulator.GetLinearVelocity());
			ply.SetWorldPosition(playerSwingRegulator.GetWorldPosition());
		}
		
		// Handle impact damage protection when on rope or recently released
		bool shouldHaveProtection = isOnRope || (ropeReleaseTime > 0f && (Game.TotalElapsedGameTime - ropeReleaseTime) < IMPACT_PROTECTION_DURATION);
		
		PlayerModifiers mods = ply.GetModifiers();
		if(shouldHaveProtection)
		{
			// Store original modifier if not already stored
			if(originalImpactDamageMod < 0f && mods.ImpactDamageTakenModifier >= 0f)
			{
				originalImpactDamageMod = mods.ImpactDamageTakenModifier;
			}
			
			// Apply reduced impact damage
			mods.ImpactDamageTakenModifier = 0.5f;
			ply.SetModifiers(mods);
		}
		else if(originalImpactDamageMod >= 0f)
		{
			// Restore original modifier after protection expires
			mods.ImpactDamageTakenModifier = originalImpactDamageMod;
			ply.SetModifiers(mods);
			originalImpactDamageMod = -1f;
			ropeReleaseTime = 0f;
		}
		
		// Update walk key state for next frame
		this.wasWalkingPressed = ply.IsWalking;
	}
	
	public void OnMeleeAction()
	{
		// Only apply area damage if on rope and cooldown has passed
		if(!isOnRope) return;
		if(Game.TotalElapsedGameTime - lastMeleeActionTime < MELEE_COOLDOWN) return;
		
		lastMeleeActionTime = Game.TotalElapsedGameTime;
		
		Vector2 playerPos = ply.GetWorldPosition();
		const float MELEE_AREA_RADIUS = 30f;
		
		// Get player's melee damage modifier
		PlayerModifiers attackerMods = ply.GetModifiers();
		float meleeDamageDealt = attackerMods.MeleeDamageDealtModifier;
		if(meleeDamageDealt < 0f) meleeDamageDealt = 1f; // Default if not set
		
		// Determine weapon damage
		float weaponDamage = 7f; // Default unarmed damage
		WeaponItem currentMelee = ply.CurrentMeleeWeapon.WeaponItem;
		
		// Weapon base damages (approximate values from game)
		if(currentMelee == WeaponItem.KNIFE) weaponDamage = 15f;
		else if(currentMelee == WeaponItem.MACHETE) weaponDamage = 20f;
		else if(currentMelee == WeaponItem.KATANA) weaponDamage = 25f;
		else if(currentMelee == WeaponItem.CHAINSAW) weaponDamage = 30f;
		else if(currentMelee == WeaponItem.BAT) weaponDamage = 12f;
		else if(currentMelee == WeaponItem.PIPE) weaponDamage = 14f;
		else if(currentMelee == WeaponItem.BATON) weaponDamage = 10f;
		else if(currentMelee == WeaponItem.HAMMER) weaponDamage = 18f;
		else if(currentMelee == WeaponItem.LEAD_PIPE) weaponDamage = 16f;
		else if(currentMelee == WeaponItem.BOTTLE) weaponDamage = 8f;
		else if(currentMelee == WeaponItem.CHAIN) weaponDamage = 11f;
		
		// Calculate total damage (3x multiplier)
		float totalDamage = weaponDamage * meleeDamageDealt * 3f;
		
		// Get attacker's team
		PlayerTeam attackerTeam = ply.GetTeam();
		
		// Find all players in radius
		foreach(IPlayer target in Game.GetPlayers())
		{
			if(target.UniqueID == ply.UniqueID) continue; // Skip self
			if(target.IsDead) continue; // Skip dead players
			
			Vector2 targetPos = target.GetWorldPosition();
			float distance = Vector2.Distance(playerPos, targetPos);
			
			if(distance <= MELEE_AREA_RADIUS)
			{
				PlayerTeam targetTeam = target.GetTeam();
				
				// Check team conditions: damage if different team OR if both are team "None"
				bool shouldDamage = false;
				if(attackerTeam == PlayerTeam.Independent && targetTeam == PlayerTeam.Independent)
				{
					shouldDamage = true; // Both are "no team"
				}
				else if(attackerTeam != targetTeam && targetTeam != PlayerTeam.Independent)
				{
					shouldDamage = true; // Different teams (and target is not independent)
				}
				
				if(shouldDamage)
				{
					target.DealDamage(totalDamage);
					// Play blood effect at the damaged player's position
					Game.PlayEffect(EffectName.Blood, targetPos);
                	Game.PlaySound("KatanaDraw", targetPos);
				}
			}
		}
	}
	public void CreateRope(Vector2 anchorPos)
	{
		if(this.distanceJoint!=null) this.distanceJoint.Destroy();
		if(this.targetObjectJoint!=null) this.targetObjectJoint.Destroy();
		if(this.regulatorDistanceJoint!=null) this.regulatorDistanceJoint.Destroy();
		if(this.regulatorTargetObjectJoint!=null) this.regulatorTargetObjectJoint.Destroy();
		if(this.anchor!=null) this.anchor.Destroy();
		if(this.playerSwingRegulator!=null) this.playerSwingRegulator.Destroy();

		anchor = Game.CreateObject("BgValve00E", anchorPos, 0f);
		playerSwingRegulator = Game.CreateObject("StoneDebris00A", ply.GetWorldPosition(), 0f);
		playerSwingRegulator.SetLinearVelocity(ply.GetLinearVelocity());
		
		// Only create the distance joint for the regulator, NOT for the player directly
		// This allows the regulator to be pulled in while the player follows via position sync
		IObjectDistanceJoint regulatorDistanceJoint = (IObjectDistanceJoint)Game.CreateObject("DistanceJoint");
		regulatorDistanceJoint.SetWorldPosition(anchor.GetWorldPosition());
		regulatorDistanceJoint.SetTargetObject(anchor);
	
		IObjectTargetObjectJoint regulatorTargetObjectJoint = (IObjectTargetObjectJoint)Game.CreateObject("TargetObjectJoint");
		regulatorTargetObjectJoint.SetWorldPosition(playerSwingRegulator.GetWorldPosition() + new Vector2(0f, 8f));
		regulatorTargetObjectJoint.SetTargetObject(playerSwingRegulator);
	
		regulatorDistanceJoint.SetTargetObjectJoint(regulatorTargetObjectJoint);
		
		// Set visual rope from anchor to player (visual only, no physics constraint on player)
		regulatorDistanceJoint.SetLineVisual(LineVisual.DJWire);
		regulatorDistanceJoint.SetLengthType(DistanceJointLengthType.Elastic);
		
		// Store joints for later use
		this.distanceJoint = null; // not using direct player joint
		this.targetObjectJoint = null;
		this.regulatorDistanceJoint = regulatorDistanceJoint;
		this.regulatorTargetObjectJoint = regulatorTargetObjectJoint;
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
	//Script Specific Startup
	
	foreach(IPlayer ply in Game.GetPlayers())
	{
		if (ply.IsBot()) continue;
		ropeControllers.Add(new RopeController(ply));
	}
	
	// Register melee action callback for area damage
	meleeCallback = Events.PlayerMeleeActionCallback.Start(OnPlayerMeleeAction);
}

public void GrapplingHook(TriggerArgs args)
{
	foreach(RopeController r in ropeControllers)
	{
		r.Update();
	}
}

public void OnPlayerMeleeAction(IPlayer player, PlayerMeleeHitArg[] args)
{
	// Find the controller for this player and trigger area damage if on rope
	foreach(RopeController controller in ropeControllers)
	{
		if(controller.ply.UniqueID == player.UniqueID)
		{
			controller.OnMeleeAction();
			break;
		}
	}
}