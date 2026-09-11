// =============================================================================
//  Omni-Directional Mobility (ODM) Gear  -  Attack on Titan style grapple hook
//  Superfighters Deluxe custom map script
// =============================================================================
//
// CONTROLS (per player, works for every player in the map automatically):
//   ACTIVATE (default 'E')  - Fire a grapple hook toward wherever you're aiming.
//                             Press it again while hooked to release early.
//   JUMP (while airborne)   - Burst of gas in your aim direction. Costs Energy.
//   GRAB                    - Hold while hooked to winch/reel yourself toward
//                             the anchor point. Costs Energy.
//   KICK                    - Hold while hooked to let out slack for a wider,
//                             looser swing.
//
// HOW IT WORKS
//   Pressing ACTIVATE raycasts from the player in their aim direction. If it
//   hits solid terrain within MAX_HOOK_RANGE, a "PullJoint" object is spawned
//   at the hit point (pinned in place as a Static body) and linked to the
//   player as its target. The PullJoint's built-in Force / ForcePerDistance
//   pulls the player toward that fixed point every tick, and ordinary gravity
//   does the rest - the combination is what produces the swinging arc, same
//   as a real rope-and-winch. Reeling in/out just raises or lowers
//   ForcePerDistance smoothly. Releasing simply removes the joint.
//
//   Gas is shared with the player's normal Energy stat (same bar used for
//   sprinting/rolling) so it's visible on-screen, recovers using each
//   player's normal energy recharge rate, and pressing GRAB/JUMP too much
//   right after sprinting will leave less gas on hand. If you'd rather ODM
//   gas be fully independent of Energy, swap the GetEnergy()/GetModifiers()
//   calls below for your own tracked float instead.
//
// TUNING
//   Every distance/force/speed value that matters is a constant up top.
//   Force/ForcePerDistance units aren't documented precisely by the API, so
//   treat the numbers below as a reasonable starting point and adjust to
//   taste for your map's scale and gravity.
// =============================================================================

private const VirtualKey GRAPPLE_KEY = VirtualKey.ACTIVATE;
private const VirtualKey BOOST_KEY = VirtualKey.JUMP;
private const VirtualKey REEL_IN_KEY = VirtualKey.GRAB;
private const VirtualKey REEL_OUT_KEY = VirtualKey.KICK;

private const float MAX_HOOK_RANGE = 900f;         // Max reach of the hook raycast
private const float MIN_HOOK_DISTANCE = 60f;       // Auto-release once this close to the anchor
private const float GROUND_GRACE_MS = 150f;        // Delay before landing can auto-release a fresh hook

private const float BASE_PULL_FORCE = 150f;        // Constant baseline pull toward the anchor
private const float BASE_FORCE_PER_DISTANCE = 6f;      // Neutral swing tension
private const float REEL_IN_FORCE_PER_DISTANCE = 18f;  // Winching in (GRAB held)
private const float REEL_OUT_FORCE_PER_DISTANCE = 1.5f; // Slack swing (KICK held)
private const float FORCE_LERP_SPEED = 0.15f;      // Smoothing between the states above

private const float INITIAL_YANK_SPEED = 850f;     // Launch kick applied the instant the hook lands
private const float BOOST_IMPULSE_SPEED = 620f;    // Velocity added per gas boost
private const float BOOST_COOLDOWN_MS = 300f;      // Minimum time between boosts
private const float BOOST_ENERGY_COST = 22f;       // Energy spent per boost
private const float REEL_ENERGY_DRAIN_PER_SEC = 16f; // Energy spent per second while reeling in

private Dictionary<int, OdmGearState> m_states = new Dictionary<int, OdmGearState>();

private class OdmGearState
{
    public IObjectPullJoint Joint = null;
    public bool Attached = false;
    public Vector2 AnchorPosition = Vector2.Zero;
    public float CurrentForcePerDistance = 0f;
    public float AttachTime = 0f;
    public float LastBoostTime = -100000f;
}

public void OnStartup()
{
    Events.PlayerKeyInputCallback.Start(OnPlayerKeyInput);
    Events.PlayerDeathCallback.Start(OnPlayerDeath);
    Events.UpdateCallback.Start(OnUpdate, 0);
}

private OdmGearState GetState(IPlayer player)
{
    OdmGearState state;
    if (!m_states.TryGetValue(player.UniqueID, out state))
    {
        state = new OdmGearState();
        m_states[player.UniqueID] = state;
    }
    return state;
}

public void OnPlayerKeyInput(IPlayer player, VirtualKeyInfo[] keyEvents)
{
    if (player == null || player.IsDead)
    {
        return;
    }

    OdmGearState state = GetState(player);

    for (int i = 0; i < keyEvents.Length; i++)
    {
        VirtualKeyInfo keyInfo = keyEvents[i];

        if (keyInfo.Event != VirtualKeyEvent.Pressed)
        {
            continue;
        }

        if (keyInfo.Key == GRAPPLE_KEY)
        {
            if (state.Attached)
            {
                ReleaseHook(player, state);
            }
            else
            {
                TryFireHook(player, state);
            }
        }
        else if (keyInfo.Key == BOOST_KEY)
        {
            TryBoost(player, state);
        }
    }
}

public void OnPlayerDeath(IPlayer player, PlayerDeathArgs args)
{
    OdmGearState state;
    if (m_states.TryGetValue(player.UniqueID, out state) && state.Attached)
    {
        ReleaseHook(player, state);
    }
}

public void OnUpdate(float elapsed)
{
    if (m_states.Count == 0)
    {
        return;
    }

    float now = Game.TotalElapsedGameTime;

    foreach (KeyValuePair<int, OdmGearState> kvp in m_states)
    {
        OdmGearState state = kvp.Value;

        if (!state.Attached)
        {
            continue;
        }

        IPlayer player = Game.GetPlayer(kvp.Key);

        if (player == null || player.IsDead)
        {
            if (state.Joint != null)
            {
                state.Joint.Remove();
                state.Joint = null;
            }
            state.Attached = false;
            continue;
        }

        // Auto-release once grounded, after a short grace period so firing the
        // hook while standing still doesn't instantly cancel itself.
        if (player.IsOnGround && (now - state.AttachTime) > GROUND_GRACE_MS)
        {
            ReleaseHook(player, state);
            continue;
        }

        // Auto-release once close enough to the anchor point.
        float distToAnchor = Vector2.Distance(player.GetWorldPosition(), state.AnchorPosition);
        if (distToAnchor < MIN_HOOK_DISTANCE)
        {
            ReleaseHook(player, state);
            continue;
        }

        bool reeling = player.KeyPressed(REEL_IN_KEY) && player.GetEnergy() > 0f;
        bool loosening = !reeling && player.KeyPressed(REEL_OUT_KEY);

        float targetForcePerDistance = BASE_FORCE_PER_DISTANCE;

        if (reeling)
        {
            targetForcePerDistance = REEL_IN_FORCE_PER_DISTANCE;

            PlayerModifiers mods = player.GetModifiers();
            float drained = REEL_ENERGY_DRAIN_PER_SEC * (elapsed / 1000f);
            mods.CurrentEnergy = MathHelper.Clamp(mods.CurrentEnergy - drained, 0f, mods.MaxEnergy);
            player.SetModifiers(mods);
        }
        else if (loosening)
        {
            targetForcePerDistance = REEL_OUT_FORCE_PER_DISTANCE;
        }

        state.CurrentForcePerDistance = MathHelper.Lerp(state.CurrentForcePerDistance, targetForcePerDistance, FORCE_LERP_SPEED);
        state.Joint.SetForcePerDistance(state.CurrentForcePerDistance);
    }
}

private void TryFireHook(IPlayer player, OdmGearState state)
{
    Vector2 origin = player.GetWorldPosition();
    Vector2 aimDir = player.AimVector;

    if (aimDir.LengthSquared() < 0.0001f)
    {
        aimDir = new Vector2((float)player.FacingDirection, 0f);
    }
    else
    {
        aimDir.Normalize();
    }

    Vector2 endPoint = origin + aimDir * MAX_HOOK_RANGE;

    RayCastInput input = new RayCastInput(true); // closestHitOnly
    RayCastResult[] results = Game.RayCast(origin, endPoint, input);

    if (results.Length == 0 || !results[0].Hit || results[0].IsPlayer)
    {
        return; // Nothing solid in range - hook whiffs
    }

    Vector2 anchorPos = results[0].Position;

    IObjectPullJoint joint = (IObjectPullJoint)Game.CreateObject("PullJoint", anchorPos);
    joint.SetBodyType(BodyType.Static);
    joint.SetTargetObject(player);
    joint.SetForce(BASE_PULL_FORCE);
    joint.SetForcePerDistance(BASE_FORCE_PER_DISTANCE);
    joint.SetLineVisual(LineVisual.DJSteelWire);

    state.Joint = joint;
    state.Attached = true;
    state.AnchorPosition = anchorPos;
    state.CurrentForcePerDistance = BASE_FORCE_PER_DISTANCE;
    state.AttachTime = Game.TotalElapsedGameTime;

    // Initial "yank" towards the anchor so the hookshot feels snappy immediately.
    Vector2 toAnchor = anchorPos - origin;
    toAnchor.Normalize();
    Vector2 velocity = player.GetLinearVelocity();
    player.SetLinearVelocity(velocity + toAnchor * INITIAL_YANK_SPEED);

    Game.PlayEffect(EffectName.BulletHitMetal, anchorPos);
}

private void ReleaseHook(IPlayer player, OdmGearState state)
{
    if (state.Joint != null)
    {
        state.Joint.Remove();
        state.Joint = null;
    }
    state.Attached = false;

    if (player != null)
    {
        Game.PlayEffect(EffectName.ItemGleam, player.GetWorldPosition());
    }
}

private void TryBoost(IPlayer player, OdmGearState state)
{
    if (player.IsOnGround)
    {
        return; // Don't hijack normal ground jumps
    }

    float now = Game.TotalElapsedGameTime;
    if (now - state.LastBoostTime < BOOST_COOLDOWN_MS)
    {
        return;
    }

    if (player.GetEnergy() < BOOST_ENERGY_COST)
    {
        return;
    }

    Vector2 boostDir = player.AimVector;
    if (boostDir.LengthSquared() < 0.0001f)
    {
        boostDir = new Vector2((float)player.FacingDirection, 0f);
    }
    else
    {
        boostDir.Normalize();
    }

    Vector2 velocity = player.GetLinearVelocity();
    player.SetLinearVelocity(velocity + boostDir * BOOST_IMPULSE_SPEED);

    PlayerModifiers mods = player.GetModifiers();
    mods.CurrentEnergy = MathHelper.Clamp(mods.CurrentEnergy - BOOST_ENERGY_COST, 0f, mods.MaxEnergy);
    player.SetModifiers(mods);

    state.LastBoostTime = now;

    Game.PlayEffect(EffectName.DustTrail, player.GetWorldPosition());
}