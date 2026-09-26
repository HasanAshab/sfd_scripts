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
	private int meleeAreaAttacksRemaining = 0; // Remaining area damage attacks for current rope
	private const int MAX_MELEE_AREA_ATTACKS_PER_ROPE = 2; // Maximum attacks allowed per rope
	
	// Distance-based damage scaling configuration (smooth interpolation)
	// These define control points for the damage curve:
	// At maxDistance, damage = minMultiplier
	// At midDistance, damage = midMultiplier  
	// At minDistance, damage = maxMultiplier
	private const float MELEE_DAMAGE_MAX_DISTANCE = 30f;     // Farthest effective distance
	private const float MELEE_DAMAGE_MID_DISTANCE = 20f;     // Mid-point distance
	private const float MELEE_DAMAGE_MIN_DISTANCE = 0.5f;    // Point-blank distance
	private const float MELEE_DAMAGE_MIN_MULTIPLIER = 1.5f;  // Damage at max distance
	private const float MELEE_DAMAGE_MID_MULTIPLIER = 2f;    // Damage at mid distance
	private const float MELEE_DAMAGE_MAX_MULTIPLIER = 2.5f;  // Damage at min distance
	
	// --- Gas system configuration ---
	// Gas is piggybacked onto the player's native Energy stat (PlayerModifiers.MaxEnergy /
	// CurrentEnergy), which the game already renders as a bar next to the health bar via
	// SetStatusBarsVisible. That gives us a following, auto-styled meter for free, instead of
	// manually spawning/positioning placeholder objects every tick.
	private const float GAS_MAX_CAPACITY = 100f;           // Maximum gas storage
	private const float GAS_PULL_COST_PER_SECOND = 1.5f;   // Gas consumed per second while pulling
	private const float GAS_THROW_COST = 1f;               // Flat gas cost per hook throw
	private const float GAS_REFILL_FROM_CRATE = 15f;       // Gas gained from breaking crates
	private const string GAS_REFILL_OBJECT_PREFIX = "Crate"; // Object name prefix for gas refill
	
	private float currentGas = 100f; // Current gas amount (starts full) - mirrored into CurrentEnergy
	// ---------------------------
	
	// --- roll power-up system ---
	private bool wasRolling = false; // Track if player was rolling last frame
	private float rollPowerUpEndTime = 0f; // Time when roll power-up expires
	private const float ROLL_POWERUP_DURATION = 2000f; // 2 seconds window after roll
	// ---------------------------

	// --- airborne-since-grab requirement ---
	// Stops players from just planting the hook nearby, standing on the
	// ground next to it, and using the melee area power for free - the power
	// is meant for players actually swinging on the rope. Requires the
	// player's feet to have left the ground at least once (even for a single
	// tick) since the rope was created before OnMeleeAction() will do anything.
	private bool hasLeftGroundSinceGrab = false;
	// ---------------------------

	// --- roll-hit knockdown system (native Fall command) ---
	// When a 4x (roll power-up) melee hit connects, the victim is forced
	// through the game's own "Fall" reaction via PlayerCommand. PlayerCommands
	// only work while the player has no user/AI control - SetInputEnabled(false)
	// is what lifts that control - so we disable the victim's input, queue the
	// Fall command, and schedule their input to be switched back on a few
	// seconds later.
	private const float ROLL_HIT_FALL_INPUT_DISABLE_DURATION = 2000f; // ms the victim's input stays disabled after being forced to fall
	private static List<PendingFallRelease> pendingFallReleases = new List<PendingFallRelease>();

	// Tracks a victim who's had input disabled to force a Fall command, and
	// when their input should be switched back on.
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

	// Re-enables input for any victims whose forced-fall window has expired.
	// Called once per tick from the global GrapplingHook loop (not per
	// RopeController instance, since the victim may belong to a different
	// controller - or to none at all, if they're a bot).
	public static void ProcessPendingFallReleases()
	{
		for(int i = pendingFallReleases.Count - 1; i >= 0; i--)
		{
			PendingFallRelease pending = pendingFallReleases[i];
			if(Game.TotalElapsedGameTime >= pending.ReleaseTime)
			{
				if(pending.Player != null && !pending.Player.IsRemoved)
				{
					pending.Player.SetInputEnabled(true);
				}
				pendingFallReleases.RemoveAt(i);
			}
		}
	}

	// Disables the target's input just long enough to force the native Fall
	// command through, then queues their input to come back on afterward.
	// If they're already in a pending-fall window (e.g. hit again quickly),
	// this just extends that window instead of stacking a second release.
	private void ForceFall(IPlayer target)
	{
		target.SetInputEnabled(false);
		target.ClearCommandQueue();
		target.AddCommand(new PlayerCommand(PlayerCommandType.Fall));
		
		float releaseTime = Game.TotalElapsedGameTime + ROLL_HIT_FALL_INPUT_DISABLE_DURATION;
		PendingFallRelease existing = null;
		foreach(PendingFallRelease p in pendingFallReleases)
		{
			if(p.Player != null && p.Player.UniqueID == target.UniqueID)
			{
				existing = p;
				break;
			}
		}
		
		if(existing != null)
		{
			existing.ReleaseTime = releaseTime;
		}
		else
		{
			pendingFallReleases.Add(new PendingFallRelease(target, releaseTime));
		}
	}
	// ---------------------------

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
	private const float PULL_FORCE = 0.7f;        // force applied toward anchor
	private const float MIN_PULL_DIST = 20f;     // stop pulling when this close
	private const float MIN_HOOK_BREAK_DIST = 60f; // minimum distance before hook can break on collision
	private const float MAX_HOOK_RANGE = 300f;   // maximum range before hook breaks automatically
	// ---------------------------

	// --- rope shrink-lock (ratchet) state ---
	// The DistanceJoint API has no "set length" call - its rest length is
	// baked in from the connected objects' positions at the moment the joint
	// is created. That means an Elastic joint always springs back toward the
	// distance it had when CreateRope() ran, no matter how far PULL_FORCE
	// has since dragged the player in. To make a shrink stick, we destroy and
	// rebuild the joint at the *current* (shorter) distance every so often
	// while reeling in, which resets its rest length each time - a ratchet.
	private float lastRopeShrinkTime = 0f;
	private const float ROPE_SHRINK_INTERVAL_MS = 100f;
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

		// --- Claim the native Energy stat as our gas gauge ---
		// This makes the gas level show up as a real status bar right next to
		// health (via SetStatusBarsVisible), following the player automatically,
		// with no extra objects or per-tick positioning needed on our end.
		// We zero out the recharge/consumption modifiers so nothing else (native
		// or otherwise) changes this value out from under our own gas economy.
		PlayerModifiers mods = ply.GetModifiers();
		mods.MaxEnergy = (int)GAS_MAX_CAPACITY;
		mods.CurrentEnergy = currentGas;
		mods.EnergyRechargeModifier = 0f;
		mods.EnergyConsumptionModifier = 0f;
		ply.SetModifiers(mods);
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
		this.regulatorDistanceJoint = null;
		this.regulatorTargetObjectJoint = null;
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
		// Check if player has enough gas to throw
		if(currentGas < GAS_THROW_COST)
		{
			// Not enough gas - don't throw
			return;
		}
		
		// Consume gas for throwing
		currentGas -= GAS_THROW_COST;
		if(currentGas < 0f) currentGas = 0f;
		
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
	
	// (Re)builds the regulator joint pair - the DistanceJoint that pulls the
	// swing regulator toward the anchor, and the TargetObjectJoint that
	// attaches that DistanceJoint to the regulator. Anchor and
	// playerSwingRegulator themselves are left alone; only the joint that
	// links them is torn down and recreated. Its rest length is implicitly
	// whatever the live distance between anchor and regulator is at the
	// instant this runs, so calling it again after the regulator has been
	// pulled closer locks in the new, shorter length instead of letting the
	// elastic joint spring back to the original grab distance.
	private void BuildRegulatorJoints()
	{
		if(this.regulatorDistanceJoint != null) this.regulatorDistanceJoint.Destroy();
		if(this.regulatorTargetObjectJoint != null) this.regulatorTargetObjectJoint.Destroy();
		
		IObjectDistanceJoint newDistanceJoint = (IObjectDistanceJoint)Game.CreateObject("DistanceJoint");
		newDistanceJoint.SetWorldPosition(anchor.GetWorldPosition());
		newDistanceJoint.SetTargetObject(anchor);
	
		IObjectTargetObjectJoint newTargetObjectJoint = (IObjectTargetObjectJoint)Game.CreateObject("TargetObjectJoint");
		newTargetObjectJoint.SetWorldPosition(playerSwingRegulator.GetWorldPosition() + new Vector2(0f, 8f));
		newTargetObjectJoint.SetTargetObject(playerSwingRegulator);
	
		newDistanceJoint.SetTargetObjectJoint(newTargetObjectJoint);
		newDistanceJoint.SetLineVisual(LineVisual.DJWire);
		newDistanceJoint.SetLengthType(DistanceJointLengthType.Elastic);
		
		this.regulatorDistanceJoint = newDistanceJoint;
		this.regulatorTargetObjectJoint = newTargetObjectJoint;
	}
	
	// Pushes our authoritative currentGas value into the player's native
	// Energy stat, which the game renders as a bar next to health. Cheap
	// enough to call once per tick rather than after every individual
	// gas-changing event.
	private void SyncGasBar()
	{
		PlayerModifiers mods = ply.GetModifiers();
		mods.CurrentEnergy = currentGas;
		ply.SetModifiers(mods);
	}
	
	public void Update()
	{
		if (ply.IsDead) return;
		
		// Check for broken crates near player to refill gas
		CheckForCrateRefill();

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
			// Check if player has gas - if not, stop pulling
			if(currentGas <= 0f)
			{
				pulling = false;
			}
			else
			{
				// Consume gas over time while pulling (1.5f per second = 0.015f per 10ms tick)
				float gasCostThisTick = (GAS_PULL_COST_PER_SECOND / 1000f) * 10f; // 10ms per tick
				currentGas -= gasCostThisTick;
				if(currentGas < 0f) currentGas = 0f;
			}
			
			// Only apply pull force if still pulling (has gas)
			if(pulling)
			{
				float dist = Vector2.Distance(playerSwingRegulator.GetWorldPosition(), anchor.GetWorldPosition());
				if(dist > MIN_PULL_DIST)
				{
					Vector2 dir = anchor.GetWorldPosition() - playerSwingRegulator.GetWorldPosition();
					dir.Normalize();
					
					// Calculate pull force with downward compensation
					// When pulling downward (dir.Y < 0), reduce force to compensate for gravity
					float pullForce = PULL_FORCE;
					if(dir.Y < 0f)
					{
						// dir.Y ranges from 0 (horizontal) to -1 (straight down)
						// Reduce force by 5% per 10% of downward angle
						// So at 100% downward (dir.Y = -1), reduce by 50%
						float downwardFactor = -dir.Y; // 0 to 1, where 1 is straight down
						float forceReduction = downwardFactor * 0.8f; // 0% to 50% reduction
						pullForce = PULL_FORCE * (1f - forceReduction);
					}
					
					Vector2 currentVel = playerSwingRegulator.GetLinearVelocity();
					Vector2 newVel = currentVel + (dir * pullForce);
					playerSwingRegulator.SetLinearVelocity(newVel);
					
					// Periodically rebuild the joint at the current (shorter)
					// distance. The Elastic joint's rest length is fixed at
					// creation time and there's no API to change it directly, so
					// this "ratchets" it inward every ROPE_SHRINK_INTERVAL_MS
					// instead of letting it spring back to the original grab
					// distance whenever PULL_FORCE isn't actively overpowering it.
					if(Game.TotalElapsedGameTime - lastRopeShrinkTime >= ROPE_SHRINK_INTERVAL_MS)
					{
						BuildRegulatorJoints();
						lastRopeShrinkTime = Game.TotalElapsedGameTime;
					}
				}
				else
				{
					pulling = false; // arrived - stop pulling
					// Lock in the final, fully-reeled-in length.
					BuildRegulatorJoints();
				}
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
		
		PlayerModifiers protectionMods = ply.GetModifiers();
		if(shouldHaveProtection)
		{
			// Store original modifier if not already stored
			if(originalImpactDamageMod < 0f && protectionMods.ImpactDamageTakenModifier >= 0f)
			{
				originalImpactDamageMod = protectionMods.ImpactDamageTakenModifier;
			}
			
			// Apply reduced impact damage
			protectionMods.ImpactDamageTakenModifier = 0.5f;
			ply.SetModifiers(protectionMods);
		}
		else if(originalImpactDamageMod >= 0f)
		{
			// Restore original modifier after protection expires
			protectionMods.ImpactDamageTakenModifier = originalImpactDamageMod;
			ply.SetModifiers(protectionMods);
			originalImpactDamageMod = -1f;
			ropeReleaseTime = 0f;
		}
		
		// Detect roll action while on rope to activate power-up window
		if(isOnRope)
		{
			bool isRollingNow = ply.IsRolling || ply.IsRecoveryRolling;
			// Detect roll end (was rolling, now not rolling)
			if(wasRolling && !isRollingNow)
			{
				// Activate 2-second power-up window for 4x damage
				rollPowerUpEndTime = Game.TotalElapsedGameTime + ROLL_POWERUP_DURATION;
			}
			
			wasRolling = isRollingNow;
			
			// Latch true the moment the player's feet leave the ground -
			// even a single mid-air tick counts. Once true it stays true for
			// the rest of this rope (reset happens in CreateRope()).
			if(!ply.IsOnGround)
			{
				hasLeftGroundSinceGrab = true;
			}
		}
		else
		{
			// Reset roll tracking when not on rope
			wasRolling = false;
		}
		
		// Push the current gas value into the player's native Energy stat so
		// the status-bar UI reflects it. One GetModifiers/SetModifiers pair
		// per tick is enough - no per-segment object work required.
		SyncGasBar();
		
		// Update walk key state for next frame
		this.wasWalkingPressed = ply.IsWalking;
	}
	
	// Check for broken crates nearby and refill gas
	private void CheckForCrateRefill()
	{
		// Get all objects in a small radius around the player
		Vector2 playerPos = ply.GetWorldPosition();
		Area checkArea = new Area(playerPos.Y + 15, playerPos.X - 15, playerPos.Y - 15, playerPos.X + 15);
		IObject[] nearbyObjects = Game.GetObjectsByArea(checkArea);
		
		foreach(IObject obj in nearbyObjects)
		{
			// Check if object name starts with the refill prefix and is being destroyed
			if(obj.Name.StartsWith(GAS_REFILL_OBJECT_PREFIX) && obj.DestructionInitiated)
			{
				// Refill gas
				currentGas += GAS_REFILL_FROM_CRATE;
				if(currentGas > GAS_MAX_CAPACITY)
				{
					currentGas = GAS_MAX_CAPACITY;
				}
				
				// Play a sound effect to indicate gas pickup
				Game.PlaySound("StrengthBoostStart", playerPos);
				
				// Remove the crate immediately so it doesn't give gas multiple times
				obj.Remove();
			}
		}
	}
	
	// Calculate smooth damage multiplier based on distance using linear interpolation
	// between three control points: max distance (1.5x), mid distance (2x), min distance (2.5x)
	private float CalculateDistanceMultiplier(float distance)
	{
		// Clamp distance to valid range
		if(distance >= MELEE_DAMAGE_MAX_DISTANCE)
		{
			return MELEE_DAMAGE_MIN_MULTIPLIER; // At or beyond max distance
		}
		else if(distance <= MELEE_DAMAGE_MIN_DISTANCE)
		{
			return MELEE_DAMAGE_MAX_MULTIPLIER; // At or closer than min distance
		}
		else if(distance >= MELEE_DAMAGE_MID_DISTANCE)
		{
			// Interpolate between max distance and mid distance
			// distance 30 -> 20: multiplier 1.5 -> 2.0
			float t = (MELEE_DAMAGE_MAX_DISTANCE - distance) / (MELEE_DAMAGE_MAX_DISTANCE - MELEE_DAMAGE_MID_DISTANCE);
			return MELEE_DAMAGE_MIN_MULTIPLIER + (MELEE_DAMAGE_MID_MULTIPLIER - MELEE_DAMAGE_MIN_MULTIPLIER) * t;
		}
		else
		{
			// Interpolate between mid distance and min distance
			// distance 20 -> 0.5: multiplier 2.0 -> 2.5
			float t = (MELEE_DAMAGE_MID_DISTANCE - distance) / (MELEE_DAMAGE_MID_DISTANCE - MELEE_DAMAGE_MIN_DISTANCE);
			return MELEE_DAMAGE_MID_MULTIPLIER + (MELEE_DAMAGE_MAX_MULTIPLIER - MELEE_DAMAGE_MID_MULTIPLIER) * t;
		}
	}
	
	public void OnMeleeAction()
	{
		// Only apply area damage if on rope and cooldown has passed
		if(!isOnRope) return;
		// Must have actually left the ground at some point on this rope -
		// blocks the "plant the hook, stand on the ground next to it" exploit
		if(!hasLeftGroundSinceGrab) return;
		if(Game.TotalElapsedGameTime - lastMeleeActionTime < MELEE_COOLDOWN) return;
		
		// Check if player has remaining area damage attacks for this rope
		if(meleeAreaAttacksRemaining <= 0) return;
		
		lastMeleeActionTime = Game.TotalElapsedGameTime;
		meleeAreaAttacksRemaining--; // Consume one attack
		
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
		else if(currentMelee == WeaponItem.AXE) weaponDamage = 32f;
		else if(currentMelee == WeaponItem.BAT) weaponDamage = 12f;
		else if(currentMelee == WeaponItem.PIPE) weaponDamage = 14f;
		else if(currentMelee == WeaponItem.BATON) weaponDamage = 10f;
		else if(currentMelee == WeaponItem.HAMMER) weaponDamage = 18f;
		else if(currentMelee == WeaponItem.LEAD_PIPE) weaponDamage = 16f;
		else if(currentMelee == WeaponItem.BOTTLE) weaponDamage = 8f;
		else if(currentMelee == WeaponItem.CHAIN) weaponDamage = 11f;
		
		// Check if roll power-up is active (doubles the final damage)
		bool hasRollPowerUp = Game.TotalElapsedGameTime < rollPowerUpEndTime;
		
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
					// Calculate smooth distance-based damage multiplier
					float distanceMultiplier = CalculateDistanceMultiplier(distance);
					
					// Apply roll power-up (doubles the distance multiplier)
					if(hasRollPowerUp)
					{
						distanceMultiplier *= 2f;
					}
					
					// Calculate final damage
					float totalDamage = weaponDamage * meleeDamageDealt * distanceMultiplier;
					
					target.DealDamage(totalDamage);
					// Play blood effect at the damaged player's position
					Game.PlayEffect(EffectName.Blood, targetPos);
					
					// Play enhanced sound effect if using roll power-up
					if(hasRollPowerUp)
					{
						Game.PlayEffect(EffectName.Blood, targetPos);
						Game.PlayEffect(EffectName.Blood, targetPos);
						Game.PlaySound("KatanaDraw", targetPos);
						
						// 4x roll-powered hit: force the victim through the
						// native Fall reaction (see ForceFall for why input
						// has to be disabled for this to take effect).
						ForceFall(target);
					}
					else
					{
						Game.PlaySound("MacheteDraw", targetPos);
					}
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
		
		this.distanceJoint = null;
		this.targetObjectJoint = null;
		this.regulatorDistanceJoint = null;
		this.regulatorTargetObjectJoint = null;

		anchor = Game.CreateObject("BgValve00E", anchorPos, 0f);
		playerSwingRegulator = Game.CreateObject("StoneDebris00A", ply.GetWorldPosition(), 0f);
		playerSwingRegulator.SetLinearVelocity(ply.GetLinearVelocity());
		
		// Only create the distance joint for the regulator, NOT for the player directly
		// This allows the regulator to be pulled in while the player follows via position sync
		BuildRegulatorJoints();
		
		this.lastRopeShrinkTime = Game.TotalElapsedGameTime;
		
		// Reset melee area attack counter for new rope
		this.meleeAreaAttacksRemaining = MAX_MELEE_AREA_ATTACKS_PER_ROPE;
		
		// Reset roll power-up state for new rope
		this.wasRolling = false;
		this.rollPowerUpEndTime = 0f;
		
		// Reset airborne requirement for new rope - must leave the ground
		// again on this fresh grab before the melee area power can fire
		this.hasLeftGroundSinceGrab = false;
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
		if (ply.IsBot) continue;
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
	
	// Re-enable input for anyone whose forced-fall window (from a 4x
	// roll-powered melee hit) has expired.
	RopeController.ProcessPendingFallReleases();
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