// =====================================================================
//  SHURIKEN -> ROCK  (Player 1 only)
//
//  * When P1 throws a shuriken, the real shuriken is deleted at once and
//    a big "." text object is thrown instead, from the same spot, in the
//    same direction and with the same speed.
//  * The rock now flies with real projectile physics, integrated every
//    frame (the same kind of motion the shuriken has):
//        vel.Y -= gravity * dt            (gravity pulls it down)
//        vel   *= (1 - drag * dt)         (tiny air resistance)
//        pos   += vel * dt
//    So the flight depends on HOW and FROM WHERE it was thrown:
//      - thrown level      -> flies far and flat, then slowly sinks
//      - thrown upward     -> climbs, slows at the top, falls back down
//      - thrown downward   -> speeds up and hits the ground soon
//      - thrown from a high spot -> stays in the air longer, so it
//        travels further before it lands
//      - the thrower's own speed is part of the launch velocity
//  * The rock breaks ONLY on players: touching any player (except P1)
//    damages them and the rock bursts into 7 small pieces that shrink/
//    fade out and are then deleted. Walls, floors and objects do not
//    stop it; an unhit rock disappears after ROCK_MAX_LIFETIME.
//  * When P1 picks up FIRE AMMO, the next 3 shuriken throws are fire
//    rocks: fire particles on the sides of the rock, and any player it
//    hits is set on fire.
//
//  WHY IT USED TO "JUST DROP":
//    GetLinearVelocity() returns Box2D units (metres/second) but the
//    rock moves in WORLD units. The old script used the Box2D speed as if
//    it were world units, so the throw was ~10x too slow and gravity
//    (in world units) took over immediately. Velocity is now converted
//    with ROCK_UNITS_PER_METER, and gravity is defined in the SAME Box2D
//    units, so both stay consistent.
//
//  MERGING INTO ANOTHER SCRIPT (e.g. SuperDS.cs):
//    - delete the duplicate "private IPlayer p1" line,
//    - call InitShurikenRock() from your own OnStartup().
//  Every other name here is unique (Rock...), so nothing else clashes.
// =====================================================================

// ---------------------------- TUNING ---------------------------------
private const string ROCK_TEXT            = ".";
private const float  ROCK_TEXT_SCALE      = 10f;     // size of the "." (bigger = bigger rock)
private const float  ROCK_HIT_RADIUS      = 6f;      // how close to a player's body counts as "touching"
private const float  ROCK_VISUAL_OFFSET_Y = 30f;      // nudge the dot up/down if it looks off-centre
private const float  ROCK_MAX_LIFETIME    = 6000f;   // ms before an unhit rock disappears (see ROCK_BURST_ON_TIMEOUT)
private const float  ROCK_DAMAGE          = 30f;     // damage dealt to the player it touches

// --- physics (the part that decides how the throw behaves) ---
private const float  ROCK_UNITS_PER_METER = 12.5f;   // world units per Box2D metre (starting value, see auto-calibration)
private bool         ROCK_AUTO_CALIBRATE  = true;    // measure the real units-per-metre from player movement
private const float  ROCK_GRAVITY_MS2     = 18f;     // gravity in Box2D m/s^2. Bigger = heavier stone, smaller = flatter shuriken-like flight
private const float  ROCK_AIR_DRAG        = 0.08f;   // fraction of speed lost per second (0 = none)
private const float  ROCK_LAUNCH_BOOST    = 1f;      // multiplies the shuriken's own throw speed (1 = same speed)
private const float  ROCK_MAX_FRAME_MS    = 50f;     // longest physics step; keeps lag spikes from tunnelling

private bool         ROCK_BURST_ON_TIMEOUT = false;  // false = an unhit rock just disappears after its lifetime; true = it bursts in mid-air

private const int    ROCK_FRAGMENT_COUNT  = 7;      // pieces the rock bursts into
private const float  FRAGMENT_LIFETIME    = 700f;    // ms the pieces live (shrink + fade) before deletion
private const float  FRAGMENT_SCALE       = 0.45f;   // piece size relative to the rock
private const float  FRAGMENT_MIN_SPEED   = 60f;     // world units / second
private const float  FRAGMENT_MAX_SPEED   = 150f;    // world units / second
private const float  FRAGMENT_SPREAD      = 2.6f;    // radians the pieces fan out over (~150 degrees)

private const int    FIRE_THROWS_PER_PICKUP        = 3;     // fire rocks granted per fire ammo pickup
private bool         FIRE_AMMO_ONLY_FROM_PICKUP    = false; // true = ignore fire ammo given by scripts
private const float  FIRE_PARTICLE_INTERVAL        = 40f;   // ms between fire particles
private const float  FIRE_SIDE_OFFSET              = 7f;    // how far from the rock the flames appear

private const float  THROW_DETECT_RANGE   = 50f;     // a new shuriken this close to P1 counts as P1's
private const float  MIN_REAL_THROW_MS    = 3f;      // shuriken slower than this (m/s) = no real velocity yet -> use aim
private const float  FALLBACK_THROW_MS    = 25f;     // fallback throw speed in m/s (used only if the shuriken has no velocity)
private const string SHURIKEN_NAME_HINT   = "shuriken";
private bool         DEBUG_LOG_NEW_OBJECTS = false;  // true = print names of objects created near P1
private bool         DEBUG_LOG_PHYSICS     = false;  // true = print the measured units-per-metre and throw speeds
// ---------------------------------------------------------------------

private class RockState
{
    public IObjectText Obj;
    public Vector2 Vel;         // current velocity in WORLD units / second
    public Vector2 Pos;         // current logical position
    public float Age;           // ms since launch
    public bool Fire;
    public bool IsFragment;
    public int OwnerId;
    public float BaseScale;
    public Color BaseColor;
    public float FireTimer;
}

private IPlayer p1 = null;
private int p1FireThrows = 0;

private List<RockState> rocks = new List<RockState>();
private List<IObject> pendingShurikens = new List<IObject>();
private Random rockRng = new Random();

private Color rockColor = new Color(115, 105, 95);
private Color fireRockColor = new Color(255, 120, 20);

// units-per-metre auto-calibration (least-squares fit of player movement)
private float rockUnitsPerMeter = ROCK_UNITS_PER_METER;
private float calSumXY = 0f;
private float calSumXX = 0f;
private int calSamples = 0;
private float calLogTimer = 0f;
private Dictionary<int, Vector2> calPrevPos = new Dictionary<int, Vector2>();

public void OnStartup()
{
    InitShurikenRock();
}

public void InitShurikenRock()
{
    ResolveP1();

    Events.ObjectCreatedCallback.Start(OnRockObjectCreated);
    Events.PlayerWeaponAddedActionCallback.Start(OnRockWeaponAdded);
    Events.UpdateCallback.Start(OnRockUpdate, 0);
}

private void ResolveP1()
{
    if (p1 != null) return;
    IPlayer[] players = Game.GetPlayers();
    if (players.Length >= 1) p1 = players[0];
}

// ---------------------------------------------------------------------
//  Fire ammo pickup -> next 3 shuriken throws are fire throws
// ---------------------------------------------------------------------
public void OnRockWeaponAdded(IPlayer player, PlayerWeaponAddedArg arg)
{
    ResolveP1();
    if (p1 == null || player.UniqueID != p1.UniqueID) return;
    if (arg.WeaponItem != WeaponItem.FIREAMMO) return;
    if (FIRE_AMMO_ONLY_FROM_PICKUP && arg.SourceObjectID == 0) return;

    p1FireThrows = FIRE_THROWS_PER_PICKUP;
    Game.PlayEffect(EffectName.CustomFloatText, p1.GetWorldPosition(), "FIRE x" + p1FireThrows);
}

// ---------------------------------------------------------------------
//  Shuriken thrown by P1 -> delete it and throw a rock instead
// ---------------------------------------------------------------------
public void OnRockObjectCreated(IObject[] objs)
{
    ResolveP1();
    if (p1 == null) return;

    foreach (IObject obj in objs)
    {
        if (obj == null) continue;

        if (DEBUG_LOG_NEW_OBJECTS)
        {
            float dist = Vector2.Distance(obj.GetWorldPosition(), p1.GetWorldPosition());
            if (dist < 100f)
            {
                Game.WriteToConsole("[ShurikenRock] new object: " + obj.Name +
                                    " | missile=" + obj.IsMissile +
                                    " | weaponItem=" + (obj is IObjectWeaponItem));
            }
        }

        if (!IsShurikenThrownByP1(obj)) continue;
        ConvertShurikenToRock(obj);
    }
}

private bool IsShurikenThrownByP1(IObject obj)
{
    string name = obj.Name;
    if (name == null) return false;
    if (name.ToLower().IndexOf(SHURIKEN_NAME_HINT) < 0) return false;

    // A shuriken lying around as a pick-up item is not a throw.
    if (obj is IObjectWeaponItem && !obj.IsMissile) return false;

    // The nearest living player to the new shuriken must be P1 (and close enough).
    Vector2 pos = obj.GetWorldPosition();
    IPlayer nearest = null;
    float best = float.MaxValue;
    foreach (IPlayer pl in Game.GetPlayers())
    {
        if (pl.IsDead) continue;
        float d = Vector2.DistanceSquared(pos, pl.GetWorldPosition());
        if (d < best) { best = d; nearest = pl; }
    }
    if (nearest == null || nearest.UniqueID != p1.UniqueID) return false;
    return best <= THROW_DETECT_RANGE * THROW_DETECT_RANGE;
}

private void ConvertShurikenToRock(IObject shuriken)
{
    Vector2 pos = shuriken.GetWorldPosition();

    // The shuriken's velocity is in Box2D units (metres/second).
    Vector2 velMs = shuriken.GetLinearVelocity();

    // Safety net: if the shuriken has no real throw velocity yet, throw along P1's aim.
    if (velMs.LengthSquared() < MIN_REAL_THROW_MS * MIN_REAL_THROW_MS)
    {
        Vector2 aim = p1.AimVector;
        if (aim.LengthSquared() > 0.0001f) aim.Normalize();
        else aim = new Vector2(p1.FacingDirection, 0f);
        velMs = new Vector2(aim.X, aim.Y + 0.2f) * FALLBACK_THROW_MS;
    }

    // Box2D metres/second -> world units/second (same units the rock moves in).
    Vector2 vel = velMs * (rockUnitsPerMeter * ROCK_LAUNCH_BOOST);

    if (DEBUG_LOG_PHYSICS)
    {
        Game.WriteToConsole("[ShurikenRock] throw: " + velMs.Length().ToString("0.0") + " m/s = " +
                            vel.Length().ToString("0") + " units/s (units per metre = " +
                            rockUnitsPerMeter.ToString("0.00") + ")");
    }

    // Delete the shuriken immediately (re-checked on the next update).
    shuriken.Remove();
    pendingShurikens.Add(shuriken);

    bool fire = false;
    if (p1FireThrows > 0)
    {
        fire = true;
        p1FireThrows--;
    }

    SpawnRock(pos, vel, fire, p1.UniqueID);
}

private void SpawnRock(Vector2 pos, Vector2 vel, bool fire, int ownerId)
{
    RockState r = CreateTextPiece(pos, vel, fire, ROCK_TEXT_SCALE, false);
    r.OwnerId = ownerId;
    rocks.Add(r);
}

private RockState CreateTextPiece(Vector2 pos, Vector2 vel, bool fire, float scale, bool isFragment)
{
    IObjectText t = (IObjectText)Game.CreateObject("Text", pos);
    t.SetText(ROCK_TEXT);
    t.SetTextAlignment(TextAlignment.Middle);
    t.SetTextScale(scale);

    Color c = fire ? fireRockColor : rockColor;
    t.SetTextColor(c);

    RockState r = new RockState();
    r.Obj = t;
    r.Vel = vel;
    r.Pos = pos;
    r.Age = 0f;
    r.Fire = fire;
    r.IsFragment = isFragment;
    r.BaseScale = scale;
    r.BaseColor = c;
    r.FireTimer = 0f;
    return r;
}

// ---------------------------------------------------------------------
//  Physics step (gravity + a little air drag), one frame at a time
// ---------------------------------------------------------------------
private Vector2 StepVelocity(Vector2 vel, float dt)
{
    float gravityUnits = ROCK_GRAVITY_MS2 * rockUnitsPerMeter;   // m/s^2 -> units/s^2
    vel.Y -= gravityUnits * dt;

    float keep = 1f - ROCK_AIR_DRAG * dt;
    if (keep < 0f) keep = 0f;
    return vel * keep;
}

// ---------------------------------------------------------------------
//  Units-per-metre auto-calibration.
//  Players move by real physics, so (distance moved per frame) divided by
//  (Box2D velocity * frame time) gives the world-units-per-metre factor.
//  A least-squares fit over many frames smooths out noise.
// ---------------------------------------------------------------------
private void CalibrateUnitsPerMeter(float elapsedMs)
{
    if (!ROCK_AUTO_CALIBRATE) return;

    float dt = elapsedMs / 1000f;
    if (dt < 0.002f || dt > 0.1f)
    {
        calPrevPos.Clear();   // lag spike / pause: don't measure across it
        return;
    }

    foreach (IPlayer pl in Game.GetPlayers())
    {
        int id = pl.UniqueID;
        if (pl.IsDead)
        {
            calPrevPos.Remove(id);
            continue;
        }

        Vector2 pos = pl.GetWorldPosition();
        Vector2 prev;
        if (calPrevPos.TryGetValue(id, out prev))
        {
            float vx = pl.GetLinearVelocity().X;
            float dx = pos.X - prev.X;
            float expected = vx * dt;

            // only use clear horizontal movement in the direction of travel
            if (Math.Abs(vx) > 2f && dx * expected > 0f)
            {
                float ratio = dx / expected;
                if (ratio > 3f && ratio < 60f)   // ignore teleports / bumps
                {
                    calSumXY = calSumXY * 0.995f + dx * expected;
                    calSumXX = calSumXX * 0.995f + expected * expected;
                    calSamples++;
                }
            }
        }
        calPrevPos[id] = pos;
    }

    if (calSamples >= 30 && calSumXX > 0.0001f)
    {
        float fit = calSumXY / calSumXX;
        if (fit < 3f) fit = 3f;
        if (fit > 60f) fit = 60f;
        rockUnitsPerMeter = fit;
    }

    if (DEBUG_LOG_PHYSICS)
    {
        calLogTimer += elapsedMs;
        if (calLogTimer > 3000f)
        {
            calLogTimer = 0f;
            Game.WriteToConsole("[ShurikenRock] units per metre = " + rockUnitsPerMeter.ToString("0.00") +
                                " (samples " + calSamples + ")");
        }
    }
}

// ---------------------------------------------------------------------
//  Per-frame update: move rocks/pieces, check hits, fire particles
// ---------------------------------------------------------------------
public void OnRockUpdate(float elapsed)
{
    CalibrateUnitsPerMeter(elapsed);

    // Make sure the original shurikens are really gone.
    if (pendingShurikens.Count > 0)
    {
        foreach (IObject s in pendingShurikens)
        {
            if (s != null && !s.IsRemoved && !s.RemovalInitiated) s.Remove();
        }
        pendingShurikens.Clear();
    }

    Vector2 visualOffset = new Vector2(0f, ROCK_VISUAL_OFFSET_Y);
    float stepMs = Math.Min(elapsed, ROCK_MAX_FRAME_MS);
    float dt = stepMs / 1000f;

    for (int i = rocks.Count - 1; i >= 0; i--)
    {
        RockState r = rocks[i];
        if (r.Obj == null || r.Obj.IsRemoved)
        {
            rocks.RemoveAt(i);
            continue;
        }

        r.Age += elapsed;

        Vector2 prev = r.Pos;
        r.Vel = StepVelocity(r.Vel, dt);
        Vector2 next = prev + r.Vel * dt;
        Vector2 velNow = r.Vel;

        // ---------------- burst pieces: shrink, fade, delete ----------------
        if (r.IsFragment)
        {
            float life = r.Age / FRAGMENT_LIFETIME;
            if (life >= 1f)
            {
                r.Obj.Remove();
                rocks.RemoveAt(i);
                continue;
            }

            r.Pos = next;
            r.Obj.SetWorldPosition(next + visualOffset);
            r.Obj.SetTextScale(Math.Max(0.1f, r.BaseScale * (1f - life)));

            if (life > 0.5f)
            {
                float a = 1f - (life - 0.5f) / 0.5f;
                r.Obj.SetTextColor(new Color(r.BaseColor.R, r.BaseColor.G, r.BaseColor.B, (byte)(255f * a)));
            }

            if (r.Fire) EmitFireParticles(r, velNow, false, elapsed);
            continue;
        }

        // ---------------- the rock itself ----------------

        // 1) touching a player?
        IPlayer victim = FindHitPlayer(prev, next, r.OwnerId);
        if (victim != null)
        {
            HitPlayer(victim, r, next, velNow);
            rocks.RemoveAt(i);
            continue;
        }

        // 2) flew too long without hitting a player? (it only ever breaks on players)
        if (r.Age >= ROCK_MAX_LIFETIME)
        {
            if (ROCK_BURST_ON_TIMEOUT) ShatterRock(r, next, velNow);
            else r.Obj.Remove();
            rocks.RemoveAt(i);
            continue;
        }

        // move + flames
        r.Pos = next;
        r.Obj.SetWorldPosition(next + visualOffset);
        if (r.Fire) EmitFireParticles(r, velNow, true, elapsed);
    }
}

// Fire particles on both sides of the rock (perpendicular to its flight direction).
private void EmitFireParticles(RockState r, Vector2 vel, bool sides, float elapsed)
{
    r.FireTimer += elapsed;
    if (r.FireTimer < FIRE_PARTICLE_INTERVAL) return;
    r.FireTimer = 0f;

    if (!sides)
    {
        Game.PlayEffect(EffectName.FireTrail, r.Pos);
        return;
    }

    Vector2 dir = vel;
    if (dir.LengthSquared() > 0.0001f) dir.Normalize();
    else dir = Vector2.UnitX;

    Vector2 side = new Vector2(-dir.Y, dir.X) * FIRE_SIDE_OFFSET;
    Game.PlayEffect(EffectName.FireTrail, r.Pos + side);
    Game.PlayEffect(EffectName.FireTrail, r.Pos - side);
}

// ---------------------------------------------------------------------
//  Hit detection
// ---------------------------------------------------------------------
private IPlayer FindHitPlayer(Vector2 prev, Vector2 next, int ownerId)
{
    foreach (IPlayer pl in Game.GetPlayers())
    {
        if (pl.IsDead || pl.UniqueID == ownerId) continue;

        Area box = pl.GetAABB();
        box.Grow(ROCK_HIT_RADIUS);

        // sample along the path this frame so fast rocks can't skip over a player
        for (int s = 0; s <= 3; s++)
        {
            if (box.Contains(Vector2.Lerp(prev, next, s / 3f))) return pl;
        }
    }
    return null;
}

private void HitPlayer(IPlayer victim, RockState r, Vector2 at, Vector2 impactVel)
{
    victim.DealDamage(ROCK_DAMAGE, r.OwnerId);
    Game.PlayEffect(EffectName.MeleeHitBlunt, at);

    // fire rocks set the damaged player on fire
    if (r.Fire) victim.SetMaxFire();

    ShatterRock(r, at, impactVel);
}

// ---------------------------------------------------------------------
//  Burst into 7 pieces; the pieces are deleted after their animation
// ---------------------------------------------------------------------
private void ShatterRock(RockState r, Vector2 at, Vector2 impactVel)
{
    r.Obj.Remove();
    Game.PlayEffect(EffectName.DestroyDefault, at);
    if (r.Fire) Game.PlayEffect(EffectName.Fire, at);

    // fan the pieces out away from the impact (opposite to the flight direction)
    Vector2 back = -impactVel;
    float baseAngle = (back.LengthSquared() > 0.0001f) ? (float)Math.Atan2(back.Y, back.X) : MathHelper.PIOver2;
    float step = FRAGMENT_SPREAD / (ROCK_FRAGMENT_COUNT - 1);

    for (int k = 0; k < ROCK_FRAGMENT_COUNT; k++)
    {
        float jitter = ((float)rockRng.NextDouble() - 0.5f) * 0.4f;
        float ang = baseAngle - FRAGMENT_SPREAD * 0.5f + step * k + jitter;
        float spd = FRAGMENT_MIN_SPEED + (float)rockRng.NextDouble() * (FRAGMENT_MAX_SPEED - FRAGMENT_MIN_SPEED);

        Vector2 v = new Vector2((float)Math.Cos(ang) * spd, (float)Math.Sin(ang) * spd + 40f);

        RockState piece = CreateTextPiece(at, v, r.Fire, ROCK_TEXT_SCALE * FRAGMENT_SCALE, true);
        piece.OwnerId = r.OwnerId;
        rocks.Add(piece);
    }
}