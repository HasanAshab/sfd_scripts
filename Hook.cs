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
			}
		}
		if(ply.IsWalking && ply.IsBlocking && !this.wasWalking)
		{
			//CreateRope(ply.GetWorldPosition() + new Vector2(100*ply.FacingDirection, 100));
			hook = Game.CreateObject("Bottle00Broken", ply.GetWorldPosition() + new Vector2(ply.FacingDirection*10, 10), 0f, new Vector2(ply.FacingDirection*20, 20), 0f);
			this.wasWalking = true;
		}
		if(hook!=null && hook.DestructionInitiated)
		{
			CreateRope(hook.GetWorldPosition());
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
		
		//Set regulator velocity to player velocity at start of swing
		playerSwingRegulator.SetLinearVelocity(ply.GetLinearVelocity());
		
		// Create distanceJoints which will connect to anchor
		IObjectDistanceJoint distanceJoint = (IObjectDistanceJoint)Game.CreateObject("DistanceJoint");
		distanceJoint.SetWorldPosition(anchor.GetWorldPosition());
		distanceJoint.SetTargetObject(anchor);
		IObjectDistanceJoint regulatorDistanceJoint = (IObjectDistanceJoint)Game.CreateObject("DistanceJoint");
		regulatorDistanceJoint.SetWorldPosition(anchor.GetWorldPosition());
		regulatorDistanceJoint.SetTargetObject(anchor);
	
		// Create targetObjectJoints which will connect to playerSwingRegulator
		IObjectTargetObjectJoint targetObjectJoint = (IObjectTargetObjectJoint)Game.CreateObject("TargetObjectJoint");
		targetObjectJoint.SetWorldPosition(ply.GetWorldPosition() + new Vector2(0f, 8f));
		targetObjectJoint.SetTargetObject(ply);
		IObjectTargetObjectJoint regulatorTargetObjectJoint = (IObjectTargetObjectJoint)Game.CreateObject("TargetObjectJoint");
		regulatorTargetObjectJoint.SetWorldPosition(playerSwingRegulator.GetWorldPosition() + new Vector2(0f, 8f));
		regulatorTargetObjectJoint.SetTargetObject(playerSwingRegulator);
	
		// Connect the distanceJoint and the targetObjectJoint together
		distanceJoint.SetTargetObjectJoint(targetObjectJoint);
		regulatorDistanceJoint.SetTargetObjectJoint(regulatorTargetObjectJoint);
		
		// Set line visual and type of joint
		distanceJoint.SetLineVisual(LineVisual.DJWire);
		distanceJoint.SetLengthType(DistanceJointLengthType.Elastic); // NOTE: Elastic means the distanceJoint can shrink (but not grow)
		
		
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
