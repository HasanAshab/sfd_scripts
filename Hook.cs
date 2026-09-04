//GrapplingHook Script!
//Made by ClockworkDice
public class RopeController
{
	private enum RopeState { Idle, Attached, Traveling }

	public IPlayer ply;
	private IObject anchorMarker = null; // just a visual marker at the hook point

	private RopeState state = RopeState.Idle;
	private Vector2 targetPosition;
	private float attachTime = 0f;

	private bool wasWalkKeyPressed = false;

	private const float TRAVEL_DELAY_MS = 1000f;   // 1 second pause before you get pulled
	private const float PULL_SPEED = 60f;          // tune to taste
	private const float ARRIVAL_DISTANCE = 15f;     // how close counts as "reached it"
	private const float MAX_ROPE_DISTANCE = 500f;   // max raycast range

	public RopeController(IPlayer ply)
	{
		this.ply = ply;
	}

	public void Update()
	{
		bool walkKeyDown = ply.KeyPressed(VirtualKey.WALKING);
		bool walkKeyJustPressed = walkKeyDown && !this.wasWalkKeyPressed;
		this.wasWalkKeyPressed = walkKeyDown;

		// Requirement 3: firing again at any point cancels whatever was happening before
		if (walkKeyJustPressed)
		{
			FireHook();
		}

		switch (state)
		{
			case RopeState.Attached:
				if (Game.TotalElapsedGameTime - attachTime >= TRAVEL_DELAY_MS)
				{
					state = RopeState.Traveling;
				}
				break;

			case RopeState.Traveling:
				// Requirement 2: blocking cancels the travel mid-way
				if (ply.IsBlocking)
				{
					CancelRope();
					break;
				}

				Vector2 toTarget = targetPosition - ply.GetWorldPosition();
				float dist = toTarget.Length();

				// Requirement 1: reaching the destination cancels the rope
				if (dist <= ARRIVAL_DISTANCE)
				{
					CancelRope();
				}
				else
				{
					Vector2 dir = Vector2.Normalize(toTarget);
					ply.SetLinearVelocity(dir * PULL_SPEED);
				}
				break;
		}
	}

	private void FireHook()
	{
		CancelRope(); // clean up any previous hook/travel first

		Vector2 start = ply.GetWorldPosition();
		Vector2 aim = ply.AimVector;
		Vector2 direction = aim.LengthSquared() > 0.0001f
			? Vector2.Normalize(aim)
			: new Vector2(ply.FacingDirection, 0f);

		Vector2 end = start + direction * MAX_ROPE_DISTANCE;

		RayCastInput input = new RayCastInput(true); // closest hit only
		RayCastResult[] results = Game.RayCast(start, end, input);

		if (results.Length > 0 && results[0].Hit)
		{
			targetPosition = results[0].Position;
			anchorMarker = Game.CreateObject("BgValve00E", targetPosition, 0f);

			state = RopeState.Attached;
			attachTime = Game.TotalElapsedGameTime;
		}
	}

	private void CancelRope()
	{
		if (this.anchorMarker != null) this.anchorMarker.Destroy();
		this.anchorMarker = null;
		this.state = RopeState.Idle;
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
