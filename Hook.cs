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
	private Vector2 aimStartPosition; // player position when aiming began; fully locked while aiming
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
	// ---------------------------

	public RopeController(IPlayer ply)
	{
		this.ply = ply;
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
			direction * 20, 
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
			// Rotate aim continuously using the up/down virtual keys.
			// UP tilts the aim upward, DOWN tilts it downward.
			if(ply.KeyPressed(VirtualKey.AIM_CLIMB_UP))
			{
				this.aimAngle -= AIM_ROTATE_SPEED;
			}
			if(ply.KeyPressed(VirtualKey.AIM_CLIMB_DOWN))
			{
				this.aimAngle += AIM_ROTATE_SPEED;
			}
			
			// Fully freeze the player while aiming: lock position back to where
			// aiming started and zero out velocity entirely. This stops any
			// horizontal/vertical drift AND cancels a jump impulse from the UP
			// key in the same tick it would occur, since UP now doubles as the
			// aim key. (Trade-off: the player won't keep falling if aiming was
			// started mid-air - let me know if you'd rather keep gravity active.)
			ply.SetWorldPosition(this.aimStartPosition);
			ply.SetLinearVelocity(Vector2.Zero);
			
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
			
			if(distanceFromThrow >= MIN_HOOK_BREAK_DIST)
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
		
		// Update walk key state for next frame
		this.wasWalkingPressed = ply.IsWalking;
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
		ropeControllers.Add(new RopeController(ply));
	}
}

public void GrapplingHook(TriggerArgs args)
{
	foreach(RopeController r in ropeControllers)
	{
		r.Update();
	}
}