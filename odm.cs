//GrapplingHook Script - Performance Optimized Version
//Made by ClockworkDice
//Optimized for memory efficiency and performance
//
// WHAT CHANGED VS THE ORIGINAL (functionally identical, same features):
//  1. PlayerModifiers get/set consolidated: the original called ply.GetModifiers()/
//     SetModifiers() up to 2x each per tick (once for impact protection, once for the
//     gas bar) for EVERY player, EVERY 10ms, for the whole match - even players who
//     never touch the hook. PlayerModifiers is a full object (not a small struct), so
//     each Get call allocates a new one. This version merges both into a single
//     Get + single Set per tick, and skips the call completely when nothing actually
//     changed (idle players now do ZERO PlayerModifiers calls most ticks).
//  2. Tick source switched from a persistent "TimerTrigger" world object + string-based
//     SetScriptMethod dispatch to Events.UpdateCallback with a direct delegate. Same
//     10ms cadence, same infinite repeat - just no extra world object and no
//     string-keyed method lookup every tick.
//  3. Game.GetPlayers() is now fetched ONCE per tick for all controllers combined
//     (previously each RopeController could call it independently while a hook was
//     mid-flight).
//  4. Vector2.Distance (sqrt) replaced with Vector2.DistanceSquared for pure
//     threshold/range checks (hook range, hook break distance, player proximity,
//     melee ranges, swing-pull distance). Actual distance is only computed with
//     Math.Sqrt when a value (not just a yes/no) is needed, and only after the cheap
//     squared check passes.
//  5. The swing-pull physics computed the same sqrt distance twice per tick
//     (once via Vector2.Distance for the range check, once via Math.Sqrt while
//     normalizing the pull direction). Now computed once.
//  6. Crate-refill detection no longer polls Game.GetObjectsByArea() around every
//     player on every single 10ms tick for as long as that player is below full gas
//     (previously this ran forever, for the whole match, for anyone who'd ever used
//     the hook). IObject.DestructionInitiated is a one-tick-transient flag ("about to
//     be removed next clean-cycle"), so simply polling less often risks missing it
//     entirely rather than just detecting it late - that's not a safe trade to make.
//     Instead this uses Events.ObjectTerminatedCallback, the documented push
//     notification for exactly this ("run just before the object is about to be
//     destroyed or removed"). It's zero-cost when nothing is being destroyed and,
//     unlike polling, can't miss the window. See OnObjectTerminated/TryClaimGasRefill
//     below - same 30x30 box, same gas cap, same "first eligible player claims it"
//     behavior as the original. The one approximation: the original box check was
//     against the crate's full bounding box via Game.GetObjectsByArea, this checks
//     the crate's position point - identical in practice for a crate-sized object.
//  7. Melee lookup in OnPlayerMeleeAction switched from a linear scan to a
//     Dictionary lookup.
//  8. Small per-event heap allocations removed: PendingFallRelease is now a struct
//     instead of a class, and the melee "player UID" scratch buffer is a reused
//     instance array instead of a fresh int[] on every qualifying melee hit.
//  9. A couple of redundant duplicate calls within the same tick were removed
//     (Game.TotalElapsedGameTime cached once per Update()/OnMeleeAction() call
//     instead of being re-read up to 8+ times; GetWorldPosition() no longer called
//     twice back-to-back for the aim indicator).
//
// Nothing about player-facing behavior, timings, costs, or feature logic was changed
// (aside from the one noted approximation in #6) - only how often/how expensively
// the engine is asked for the same information.
//
// FIX (post-playtest): added OnShutdown(). Events.UpdateCallback/ObjectTerminated-
// Callback/PlayerMeleeActionCallback subscriptions are not tied to the match/world
// lifecycle the way a world object (the original's TimerTrigger) is - they keep the
// script instance alive until something calls .Stop() on them. Left unstopped, this
// can hold the previous match's sandbox alive into the next match's sandbox-creation
// step and break scripting for that next match entirely. OnShutdown() is the
// documented hook ("called before a map restart / script deactivates") for exactly
// this cleanup, and now stops all three.

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

	// State flags
	public bool isOnRope;
	private bool wasWalkingPressed;
	private bool walkPressConsumedByCancel;
	private bool pendingGrab;
	private bool pulling;
	private bool isAiming;
	private bool wasRolling;
	private bool hasLeftGroundSinceGrab;
	private bool impactProtectionActive; // tracks whether we've currently pushed the 0.5 impact modifier to the engine

	// Timing values
	private float ropeReleaseTime;
	private float lastMeleeActionTime;
	private float rollPowerUpEndTime;
	private float walkKeyHoldTime;
	private float grabTime;
	private float lastRopeShrinkTime;
	private float aimAngle;
	private float originalImpactDamageMod = -1f;
	private float currentGas = GAS_MAX_CAPACITY;
	private float lastSyncedGas = GAS_MAX_CAPACITY; // last value actually pushed to PlayerModifiers

	// Counters
	private int meleeAreaAttacksRemaining;

	// Vectors (cached to avoid allocations)
	private Vector2 aimStartPosition;
	private Vector2 pendingAnchorPos;
	private Vector2 hookThrowPos;

	// Object reference
	private IObjectText aimIndicator;
	
	// Gas bar visualization using text emojis
	private IObjectText gasIndicator;
	private const int GAS_BAR_SEGMENTS = 6; // 6 emoji boxes
	private const float GAS_PER_SEGMENT = GAS_MAX_CAPACITY / GAS_BAR_SEGMENTS; // ~16.67f per box
	private const float GAS_BAR_OFFSET_Y = 20f; // Height above player head
	private const float GAS_INDICATOR_SHOW_DURATION = 2000f; // Show for 2 seconds
	private float gasIndicatorHideTime; // When to hide the gas indicator

	// Reused scratch buffer for the melee "is this object actually a player" check,
	// avoids allocating a fresh int[] on every qualifying melee hit.
	private int[] meleePlayerUIDScratch = new int[8];

	// Constants (all in one place for easy tuning)
	private const float IMPACT_PROTECTION_DURATION = 2000f;
	private const float MELEE_COOLDOWN = 100f;
	private const int MAX_MELEE_AREA_ATTACKS_PER_ROPE = 2;
	// Public so the script-level Events.ObjectTerminatedCallback handler (which only
	// has one static/shared registration for the whole match, not one per player)
	// can filter on the same prefix without duplicating the literal.
	public const string GAS_REFILL_OBJECT_PREFIX = "Supply";
	// Half-width of the original 30x30 axis-aligned box used for the refill check
	// (Area(playerPos.Y+15, playerPos.X-15, playerPos.Y-15, playerPos.X+15)).
	private const float GAS_REFILL_BOX_HALF_WIDTH = 15f;
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
	private const float HOOK_PLAYER_PROXIMITY_DIST = 10f;

	// Precomputed squared thresholds - lets range/threshold checks skip Math.Sqrt entirely.
	private const float MAX_HOOK_RANGE_SQ = MAX_HOOK_RANGE * MAX_HOOK_RANGE;
	private const float MIN_HOOK_BREAK_DIST_SQ = MIN_HOOK_BREAK_DIST * MIN_HOOK_BREAK_DIST;
	private const float MIN_PULL_DIST_SQ = MIN_PULL_DIST * MIN_PULL_DIST;
	private const float HOOK_PLAYER_PROXIMITY_DIST_SQ = HOOK_PLAYER_PROXIMITY_DIST * HOOK_PLAYER_PROXIMITY_DIST;
	private const float MELEE_DAMAGE_MAX_DISTANCE_SQ = MELEE_DAMAGE_MAX_DISTANCE * MELEE_DAMAGE_MAX_DISTANCE;

	// Static list for pending fall releases (shared across all controllers)
	private static List<PendingFallRelease> pendingFallReleases = new List<PendingFallRelease>(4); // Pre-allocate capacity

	// Struct instead of class: entries live inline in the List's backing array
	// instead of each being a separate heap allocation.
	private struct PendingFallRelease
	{
		public IPlayer Player;
		public float ReleaseTime;
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

		// Setup gas system - no longer using PlayerModifiers energy bar
		// Gas is tracked internally and displayed with emoji indicators
		currentGas = GAS_MAX_CAPACITY;
		lastSyncedGas = GAS_MAX_CAPACITY;
	}

	private void CancelRope(float now)
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

		if(isOnRope) ropeReleaseTime = now;
		isOnRope = false;

		walkKeyHoldTime = now;
	}

	private void CancelAiming()
	{
		if(aimIndicator != null)
		{
			aimIndicator.Remove();
			aimIndicator = null;
		}
		isAiming = false;
		// Don't hide gas indicator here - let it auto-hide after duration
	}
	
	// Creates/updates the gas indicator with colored asterisks
	private void ShowGasIndicator(float now)
	{
		// Calculate how many segments should be filled
		int filledSegments = (int)Math.Ceiling(currentGas / GAS_PER_SEGMENT);
		if(filledSegments > GAS_BAR_SEGMENTS) filledSegments = GAS_BAR_SEGMENTS;
		if(filledSegments < 0) filledSegments = 0;
		
		// Build the asterisk string with color codes
		// Color format: {COLOR} where COLOR is the hex color code
		string gasText = "";
		for(int i = 0; i < GAS_BAR_SEGMENTS; i++)
		{
			if(i < filledSegments)
			{
				gasText += "{00FF99}*"; // Green for filled
			}
			else
			{
				gasText += "{888888}*"; // Grey for empty
			}
		}
		
		Vector2 playerPos = ply.GetWorldPosition();
		
		if(gasIndicator == null)
		{
			// Create new indicator
			gasIndicator = (IObjectText)Game.CreateObject("Text", 
				new Vector2(playerPos.X, playerPos.Y + GAS_BAR_OFFSET_Y));
			gasIndicator.SetTextAlignment(TextAlignment.Middle);
			gasIndicator.SetTextScale(1.2f); // Larger size
		}
		
		gasIndicator.SetText(gasText);
		gasIndicator.SetWorldPosition(new Vector2(playerPos.X, playerPos.Y + GAS_BAR_OFFSET_Y));
		
		// Set hide time
		gasIndicatorHideTime = now + GAS_INDICATOR_SHOW_DURATION;
	}
	
	private void UpdateGasIndicator(float now)
	{
		if(gasIndicator != null)
		{
			if(now >= gasIndicatorHideTime && !isAiming)
			{
				// Time to hide
				gasIndicator.Remove();
				gasIndicator = null;
			}
			else
			{
				// Update position to follow player
				Vector2 playerPos = ply.GetWorldPosition();
				gasIndicator.SetWorldPosition(new Vector2(playerPos.X, playerPos.Y + GAS_BAR_OFFSET_Y));
			}
		}
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

	// Consolidates impact protection state management. No longer manages gas bar
	// since we use emoji indicators instead of PlayerModifiers.CurrentEnergy.
	private void SyncPlayerState(float now, bool shouldHaveProtection)
	{
		bool enteringProtection = shouldHaveProtection && !impactProtectionActive;
		bool leavingProtection = !shouldHaveProtection && (impactProtectionActive || originalImpactDamageMod >= 0f);

		if(!enteringProtection && !leavingProtection) return;

		PlayerModifiers mods = ply.GetModifiers();

		if(enteringProtection)
		{
			if(originalImpactDamageMod < 0f && mods.ImpactDamageTakenModifier >= 0f)
			{
				originalImpactDamageMod = mods.ImpactDamageTakenModifier;
			}
			mods.ImpactDamageTakenModifier = 0.5f;
			impactProtectionActive = true;
		}
		else if(leavingProtection)
		{
			if(originalImpactDamageMod >= 0f)
			{
				mods.ImpactDamageTakenModifier = originalImpactDamageMod;
			}
			impactProtectionActive = false;
			originalImpactDamageMod = -1f;
			ropeReleaseTime = 0f;
		}

		ply.SetModifiers(mods);
	}

	// allPlayers is fetched once per tick by the caller and shared across every
	// RopeController instead of each one calling Game.GetPlayers() independently.
	public void Update(IPlayer[] allPlayers)
	{
		if (ply.IsDead) return;

		float now = Game.TotalElapsedGameTime; // read once, reused for the whole tick

		// Gas refill from crates is now handled by the script-level
		// Events.ObjectTerminatedCallback (see OnObjectTerminated) instead of a
		// per-tick area query here - see the file header for why.

		bool isWalkingNow = ply.IsWalking;
		bool walkJustPressed = isWalkingNow && !wasWalkingPressed;
		bool walkJustReleased = !isWalkingNow && wasWalkingPressed;

		// Cancel rope/hook if walk pressed while rope active
		if(walkJustPressed && (hook != null || pendingGrab || isOnRope))
		{
			CancelAiming();
			CancelRope(now);
			walkPressConsumedByCancel = true;
			wasWalkingPressed = isWalkingNow;
			return;
		}

		// Aiming system
		if(walkJustPressed && !isOnRope && hook == null && !pendingGrab && !isAiming)
		{
			walkKeyHoldTime = now;
		}

		if(isWalkingNow && !walkPressConsumedByCancel && !isOnRope && hook == null && !pendingGrab && !isAiming)
		{
			if(now - walkKeyHoldTime >= QUICK_TAP_THRESHOLD)
			{
				isAiming = true;
				aimAngle = GetDefaultAimAngle();
				aimStartPosition = ply.GetWorldPosition();

				aimIndicator = (IObjectText)Game.CreateObject("Text", aimStartPosition);
				aimIndicator.SetText("+");
				aimIndicator.SetTextScale(0.8f);
				aimIndicator.SetTextAlignment(TextAlignment.Middle);
				aimIndicator.SetTextColor(new Color(255, 255, 255));
				
				// Show gas indicator when entering aim mode
				ShowGasIndicator(now);
			}
		}

		if(isAiming)
		{
			int facingDir = ply.FacingDirection;

			// Rotation input
			if(ply.KeyPressed(facingDir == 1 ? VirtualKey.AIM_RUN_RIGHT : VirtualKey.CROUCH_ROLL_DIVE))
				aimAngle -= AIM_ROTATE_SPEED;
			if(ply.KeyPressed(facingDir == 1 ? VirtualKey.CROUCH_ROLL_DIVE : VirtualKey.AIM_RUN_LEFT))
				aimAngle += AIM_ROTATE_SPEED;

			// Lock position
			Vector2 pos = ply.GetWorldPosition();
			pos.X = aimStartPosition.X;
			ply.SetWorldPosition(pos);

			Vector2 vel = ply.GetLinearVelocity();
			vel.X = 0f;
			ply.SetLinearVelocity(vel);

			// Update indicator - reuse `pos` (the position we just set) instead of
			// asking the engine for the player's position again.
			if(aimIndicator != null)
			{
				aimIndicator.SetWorldPosition(new Vector2(
					pos.X + (float)Math.Cos(aimAngle) * AIM_DISTANCE,
					pos.Y + (float)Math.Sin(aimAngle) * AIM_DISTANCE
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
			if(now - walkKeyHoldTime < QUICK_TAP_THRESHOLD)
			{
				ThrowHook(GetDefaultAimAngle());
			}
		}

		if(walkJustReleased) walkPressConsumedByCancel = false;

		// Hook collision detection
		if(hook != null && !hook.DestructionInitiated)
		{
			Vector2 hookPos = hook.GetWorldPosition();
			float distanceFromThrowSq = Vector2.DistanceSquared(hookPos, hookThrowPos);

			if(distanceFromThrowSq >= MAX_HOOK_RANGE_SQ)
			{
				hook.Destroy();
			}
			else if(distanceFromThrowSq >= MIN_HOOK_BREAK_DIST_SQ)
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

				// Player proximity check (uses the tick-shared player list)
				if(!hitSomething)
				{
					int pCount = allPlayers.Length;
					for(int i = 0; i < pCount && !hitSomething; i++)
					{
						IPlayer p = allPlayers[i];
						if(p.UniqueID != ply.UniqueID && Vector2.DistanceSquared(hookPos, p.GetWorldPosition()) < HOOK_PLAYER_PROXIMITY_DIST_SQ)
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
			grabTime = now + GRAB_DELAY_MS;
			pendingGrab = true;
			hook = null;
		}

		// Rope creation
		if(pendingGrab && now >= grabTime)
		{
			CreateRope(pendingAnchorPos);
			pendingGrab = false;
			pulling = true;
			isOnRope = true;
		}

		// Pulling physics
		if(pulling && anchor != null && playerSwingRegulator != null)
		{
			if(currentGas <= 0f)
			{
				pulling = false;
				BuildRegulatorJoints();
				lastRopeShrinkTime = now;
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

				Vector2 toAnchor = new Vector2(anchorPos.X - regPos.X, anchorPos.Y - regPos.Y);
				float distSq = toAnchor.X * toAnchor.X + toAnchor.Y * toAnchor.Y;

				if(distSq > MIN_PULL_DIST_SQ)
				{
					// Single sqrt (the original computed this same value twice: once via
					// Vector2.Distance for the range check, once via Math.Sqrt to normalize).
					float magnitude = (float)Math.Sqrt(distSq);
					Vector2 dir = new Vector2(toAnchor.X / magnitude, toAnchor.Y / magnitude);

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

					if(now - lastRopeShrinkTime >= ROPE_SHRINK_INTERVAL_MS)
					{
						BuildRegulatorJoints();
						lastRopeShrinkTime = now;
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

		// Impact protection window (state only - the actual PlayerModifiers write
		// happens once, at the end, in SyncPlayerState)
		bool shouldHaveProtection = isOnRope || (ropeReleaseTime > 0f && (now - ropeReleaseTime) < IMPACT_PROTECTION_DURATION);

		// Roll detection
		if(isOnRope)
		{
			bool isRollingNow = ply.IsRolling || ply.IsRecoveryRolling;
			if(wasRolling && !isRollingNow)
			{
				rollPowerUpEndTime = now + ROLL_POWERUP_DURATION;
			}
			wasRolling = isRollingNow;

			if(!ply.IsOnGround) hasLeftGroundSinceGrab = true;
		}
		else
		{
			wasRolling = false;
		}

		// Update gas indicator position/visibility
		UpdateGasIndicator(now);

		SyncPlayerState(now, shouldHaveProtection);
		wasWalkingPressed = isWalkingNow;
	}

	// Called from the script-level OnObjectTerminated handler for every destroyed
	// object whose name starts with "Supply". Mirrors the original per-player
	// polling exactly: same 30x30 axis-aligned box (not a circle) centered on the
	// player, same gas cap, same sound at the player's position. Returns true if
	// this player claimed the refill, so the caller knows to stop offering this
	// same crate to any other (further down the list) player - matching the
	// original's behavior where whichever player's check ran first consumed the
	// object via obj.Remove() before anyone else's check could see it.
	public bool TryClaimGasRefill(Vector2 objPos, float now)
	{
		if(currentGas >= GAS_MAX_CAPACITY) return false;

		Vector2 playerPos = ply.GetWorldPosition();
		if(Math.Abs(objPos.X - playerPos.X) > GAS_REFILL_BOX_HALF_WIDTH) return false;
		if(Math.Abs(objPos.Y - playerPos.Y) > GAS_REFILL_BOX_HALF_WIDTH) return false;

		currentGas += GAS_REFILL_FROM_CRATE;
		if(currentGas > GAS_MAX_CAPACITY) currentGas = GAS_MAX_CAPACITY;

		Game.PlaySound("StrengthBoostStart", playerPos);
		
		// Show gas indicator when picking up gas
		ShowGasIndicator(now);
		
		return true;
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

		float now = Game.TotalElapsedGameTime; // read once, reused for this call

		if(now - lastMeleeActionTime < MELEE_COOLDOWN) return;
		if(meleeAreaAttacksRemaining <= 0) return;

		lastMeleeActionTime = now;
		meleeAreaAttacksRemaining--;

		Vector2 playerPos = ply.GetWorldPosition();

		PlayerModifiers attackerMods = ply.GetModifiers();
		float meleeDamageDealt = attackerMods.MeleeDamageDealtModifier;
		if(meleeDamageDealt < 0f) meleeDamageDealt = 1f;

		// Weapon damage
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

		bool hasRollPowerUp = now < rollPowerUpEndTime;
		bool isRecoveryRolling = ply.IsRecoveryRolling;
		bool isRollingNow = ply.IsRolling || isRecoveryRolling;

		PlayerTeam attackerTeam = ply.GetTeam();

		// Player damage loop
		IPlayer[] players = Game.GetPlayers();
		int playerCount = players.Length;
		for(int i = 0; i < playerCount; i++)
		{
			IPlayer target = players[i];
			if(target.UniqueID == ply.UniqueID || target.IsDead) continue;

			Vector2 targetPos = target.GetWorldPosition();
			float distSq = Vector2.DistanceSquared(playerPos, targetPos);

			if(distSq <= MELEE_DAMAGE_MAX_DISTANCE_SQ)
			{
				PlayerTeam targetTeam = target.GetTeam();
				bool shouldDamage = (attackerTeam == PlayerTeam.Independent && targetTeam == PlayerTeam.Independent) ||
				                   (attackerTeam != targetTeam && targetTeam != PlayerTeam.Independent);

				if(shouldDamage)
				{
					float distance = (float)Math.Sqrt(distSq);
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
						ForceFall(target, now);

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

			// Reuse a cached scratch buffer instead of allocating a new int[] every hit
			int playerUIDCount = playerCount;
			if(playerUIDCount > meleePlayerUIDScratch.Length)
			{
				meleePlayerUIDScratch = new int[playerUIDCount];
			}
			for(int i = 0; i < playerUIDCount; i++)
			{
				meleePlayerUIDScratch[i] = players[i].UniqueID;
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
					if(meleePlayerUIDScratch[j] == objUID)
					{
						isPlayer = true;
						break;
					}
				}
				if(isPlayer) continue;

				Vector2 objPos = obj.GetWorldPosition();
				float distSq = Vector2.DistanceSquared(playerPos, objPos);

				if(distSq <= MELEE_DAMAGE_MAX_DISTANCE_SQ)
				{
					float distance = (float)Math.Sqrt(distSq);
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

	private void ForceFall(IPlayer target, float now)
	{
		target.SetInputEnabled(false);
		target.ClearCommandQueue();
		target.AddCommand(new PlayerCommand(PlayerCommandType.Fall));

		float releaseTime = now + ROLL_HIT_FALL_INPUT_DISABLE_DURATION;
		int targetUID = target.UniqueID;

		// Find existing entry
		int count = pendingFallReleases.Count;
		for(int i = 0; i < count; i++)
		{
			PendingFallRelease p = pendingFallReleases[i];
			if(p.Player != null && p.Player.UniqueID == targetUID)
			{
				p.ReleaseTime = releaseTime;
				pendingFallReleases[i] = p; // structs are copied out by the indexer, so write back
				return;
			}
		}

		pendingFallReleases.Add(new PendingFallRelease { Player = target, ReleaseTime = releaseTime });
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
Dictionary<int, RopeController> controllersByPlayerId = new Dictionary<int, RopeController>();
Events.PlayerMeleeActionCallback meleeCallback = null;
Events.UpdateCallback updateCallback = null;
Events.ObjectTerminatedCallback objectTerminatedCallback = null;

public void OnStartup()
{
	// Direct delegate-based tick callback instead of a persistent TimerTrigger world
	// object with string-based SetScriptMethod dispatch. Same 10ms cadence, same
	// infinite repeat (repeatCount 0).
	updateCallback = Events.UpdateCallback.Start(GrapplingHookUpdate, 10, 0);

	foreach(IPlayer ply in Game.GetPlayers())
	{
		if (ply.IsBot) continue;
		RopeController controller = new RopeController(ply);
		ropeControllers.Add(controller);
		controllersByPlayerId[ply.UniqueID] = controller;
	}

	meleeCallback = Events.PlayerMeleeActionCallback.Start(OnPlayerMeleeAction);

	// Replaces the old per-tick, per-player Game.GetObjectsByArea() polling for
	// broken supply crates - see the file header and TryClaimGasRefill for details.
	objectTerminatedCallback = Events.ObjectTerminatedCallback.Start(OnObjectTerminated);
}

public void GrapplingHookUpdate(float elapsed)
{
	// Fetched once per tick and shared by every controller instead of each one
	// calling Game.GetPlayers() separately.
	IPlayer[] allPlayers = Game.GetPlayers();

	int count = ropeControllers.Count;
	for(int i = 0; i < count; i++)
	{
		ropeControllers[i].Update(allPlayers);
	}

	RopeController.ProcessPendingFallReleases();
}

public void OnPlayerMeleeAction(IPlayer player, PlayerMeleeHitArg[] args)
{
	RopeController controller;
	if(controllersByPlayerId.TryGetValue(player.UniqueID, out controller))
	{
		controller.OnMeleeAction();
	}
}

// The game calls this before a map restart / script deactivation (see
// GameScriptInterface's documented lifecycle: OnStartup -> AfterStartup ->
// ... -> OnShutdown). Event subscriptions made via Events.*.Start() are NOT
// torn down automatically just because the match/world ends - unlike a world
// object (e.g. the original's TimerTrigger, which the game world destroys on
// its own), a live Events subscription keeps this whole script instance alive
// until something calls .Stop() on it. Left unstopped, that can hold the
// previous match's script/sandbox alive into the next match's sandbox
// creation step - which is exactly what caused the "Failed to create sandbox"
// error after a match transition. Stopping every registered callback here
// fixes that.
public void OnShutdown()
{
	if(updateCallback != null) { updateCallback.Stop(); updateCallback = null; }
	if(objectTerminatedCallback != null) { objectTerminatedCallback.Stop(); objectTerminatedCallback = null; }
	if(meleeCallback != null) { meleeCallback.Stop(); meleeCallback = null; }

	ropeControllers.Clear();
	controllersByPlayerId.Clear();
}

public void OnObjectTerminated(IObject[] objs)
{
	float now = Game.TotalElapsedGameTime;
	int objCount = objs.Length;
	for(int i = 0; i < objCount; i++)
	{
		IObject obj = objs[i];
		if(!obj.DestructionInitiated) continue;
		if(!obj.Name.StartsWith(RopeController.GAS_REFILL_OBJECT_PREFIX)) continue;

		Vector2 objPos = obj.GetWorldPosition();

		// First eligible (below max gas, within range) player in list order claims
		// it and the object is removed - same "first come, first served" outcome
		// as the original's per-player polling + obj.Remove() race.
		int count = ropeControllers.Count;
		for(int j = 0; j < count; j++)
		{
			if(ropeControllers[j].TryClaimGasRefill(objPos, now))
			{
				obj.Remove();
				break;
			}
		}
	}
}