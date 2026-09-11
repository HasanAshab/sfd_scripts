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

	// --- delayed-grab state ---
	private const float GRAB_DELAY_MS = 100f;
	private bool pendingGrab = false;
	private float grabTime = 0f;
	private Vector2 pendingAnchorPos;

	// --- pull-in state using physics force ---
	private bool pulling = false;
	private const float PULL_FORCE = 0.55f;        // force applied toward anchor
	private const float MIN_PULL_DIST = 20f;     // stop pulling when this close
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
	}
	
	public void Update()
	{
		// Detect walk key press (rising edge)
		if(ply.IsWalking && !this.wasWalkingPressed)
		{
			// Walk key just pressed
			if(hook != null || pendingGrab || isOnRope)
			{
				// Cancel rope if hook is active, pending, or rope is attached
				CancelRope();
			}
			else
			{
				// Throw hook
				hook = Game.CreateObject("Bottle00Broken", ply.GetWorldPosition() + new Vector2(ply.FacingDirection*10, 10), 0f, new Vector2(ply.FacingDirection*20, 20), 0f);
			}
		}
		
		// Check if hook hit something
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
