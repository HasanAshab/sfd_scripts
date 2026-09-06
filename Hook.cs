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
	private bool wasWalking = false;

	// --- delayed-grab state ---
	private const float GRAB_DELAY_MS = 400f;
	private bool pendingGrab = false;
	private float grabTime = 0f;
	private Vector2 pendingAnchorPos;

	// --- pull-in state using physics force ---
	private bool pulling = false;
	private const float PULL_FORCE = 30f;        // force applied toward anchor
	private const float MIN_PULL_DIST = 30f;     // stop pulling when this close
	// ---------------------------

	public RopeController(IPlayer ply)
	{
		this.ply = ply;
	}
	public void Update()
	{
		if(!this.wasWalking)
		{
			if(ply.IsWalking)
			{
				if(this.distanceJoint!=null) this.distanceJoint.Destroy();
				if(this.targetObjectJoint!=null) this.targetObjectJoint.Destroy();
				if(this.regulatorDistanceJoint!=null) this.regulatorDistanceJoint.Destroy();
				if(this.regulatorTargetObjectJoint!=null) this.regulatorTargetObjectJoint.Destroy();
				if(this.anchor!=null) this.anchor.Destroy();
				if(this.playerSwingRegulator!=null) this.playerSwingRegulator.Destroy();
				
				this.distanceJoint = null;
				this.targetObjectJoint = null;
				this.anchor = null;
				this.playerSwingRegulator = null;

				this.pendingGrab = false;
				this.pulling = false;
			}
		}
		if(ply.IsWalking && ply.IsBlocking && !this.wasWalking)
		{
			hook = Game.CreateObject("Bottle00Broken", ply.GetWorldPosition() + new Vector2(ply.FacingDirection*10, 10), 0f, new Vector2(ply.FacingDirection*20, 20), 0f);
			this.wasWalking = true;
		}
		if(hook!=null && hook.DestructionInitiated)
		{
			pendingAnchorPos = hook.GetWorldPosition();
			grabTime = Game.TotalElapsedGameTime + GRAB_DELAY_MS;
			pendingGrab = true;
			hook = null;
		}
		if(pendingGrab && Game.TotalElapsedGameTime >= grabTime)
		{
			CreateRope(pendingAnchorPos);
			pendingGrab = false;
			pulling = true; // start pulling the player toward the anchor
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
		if(!ply.IsWalking)
		{
			this.wasWalking = false;
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
