//GrapplingHook Script!
//Made by ClockworkDice
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
	private Vector2 pendingPlyPos;   // NEW: snapshot of player pos AT IMPACT
	private Vector2 pendingPlyVel;   // NEW: snapshot of player velocity AT IMPACT
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
			}
		}
		if(ply.IsWalking && ply.IsBlocking && !this.wasWalking)
		{
			hook = Game.CreateObject("Bottle00Broken", ply.GetWorldPosition() + new Vector2(ply.FacingDirection*10, 10), 0f, new Vector2(ply.FacingDirection*20, 20), 0f);
			this.wasWalking = true;
		}
		if(hook!=null && hook.DestructionInitiated)
		{
			// snapshot everything AT THE MOMENT OF IMPACT
			pendingAnchorPos = hook.GetWorldPosition();
			pendingPlyPos = ply.GetWorldPosition();
			pendingPlyVel = ply.GetLinearVelocity();
			grabTime = Game.TotalElapsedGameTime + GRAB_DELAY_MS;
			pendingGrab = true;
			hook = null;
		}
		if(pendingGrab && Game.TotalElapsedGameTime >= grabTime)
		{
			CreateRope(pendingAnchorPos, pendingPlyPos, pendingPlyVel);
			pendingGrab = false;
		}
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
	public void CreateRope(Vector2 anchorPos, Vector2 snapshotPlyPos, Vector2 snapshotPlyVel)
	{
		if(this.distanceJoint!=null) this.distanceJoint.Destroy();
		if(this.targetObjectJoint!=null) this.targetObjectJoint.Destroy();
		if(this.regulatorDistanceJoint!=null) this.regulatorDistanceJoint.Destroy();
		if(this.regulatorTargetObjectJoint!=null) this.regulatorTargetObjectJoint.Destroy();
		if(this.anchor!=null) this.anchor.Destroy();
		if(this.playerSwingRegulator!=null) this.playerSwingRegulator.Destroy();

		anchor = Game.CreateObject("BgValve00E", anchorPos, 0f);
		// spawn the regulator at the SNAPSHOT position, not ply's live (400ms-later) position
		playerSwingRegulator = Game.CreateObject("StoneDebris00A", snapshotPlyPos, 0f);
		playerSwingRegulator.SetLinearVelocity(snapshotPlyVel);
		
		IObjectDistanceJoint distanceJoint = (IObjectDistanceJoint)Game.CreateObject("DistanceJoint");
		distanceJoint.SetWorldPosition(anchor.GetWorldPosition());
		distanceJoint.SetTargetObject(anchor);
		IObjectDistanceJoint regulatorDistanceJoint = (IObjectDistanceJoint)Game.CreateObject("DistanceJoint");
		regulatorDistanceJoint.SetWorldPosition(anchor.GetWorldPosition());
		regulatorDistanceJoint.SetTargetObject(anchor);
	
		// use the SNAPSHOT position to build the rest length, but still target the live ply
		IObjectTargetObjectJoint targetObjectJoint = (IObjectTargetObjectJoint)Game.CreateObject("TargetObjectJoint");
		targetObjectJoint.SetWorldPosition(snapshotPlyPos + new Vector2(0f, 8f));
		targetObjectJoint.SetTargetObject(ply);
		IObjectTargetObjectJoint regulatorTargetObjectJoint = (IObjectTargetObjectJoint)Game.CreateObject("TargetObjectJoint");
		regulatorTargetObjectJoint.SetWorldPosition(playerSwingRegulator.GetWorldPosition() + new Vector2(0f, 8f));
		regulatorTargetObjectJoint.SetTargetObject(playerSwingRegulator);
	
		distanceJoint.SetTargetObjectJoint(targetObjectJoint);
		regulatorDistanceJoint.SetTargetObjectJoint(regulatorTargetObjectJoint);
		
		distanceJoint.SetLineVisual(LineVisual.DJWire);
		distanceJoint.SetLengthType(DistanceJointLengthType.Elastic);
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
