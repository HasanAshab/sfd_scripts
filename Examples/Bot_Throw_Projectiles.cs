// Bots Throw Enhanced v13 - based only on the supplied latest-version file.
// Faster bounded snapshots; shared loot eligibility; crate pursuit;
// stable task ownership; conservative recovery from repeated failed movement.
// The v12 explicit loaded/reserve firearm-ammo workaround is unchanged.
// Replace the old build. Do not run both versions together.
const float UPDATE_INTERVAL_MS = 75f;
const float WORLD_SNAPSHOT_INTERVAL_MS = 75f;
const float BOT_REGISTRATION_INTERVAL_MS = 500f;
const float MINE_HISTORY_PRUNE_INTERVAL_MS = 3000f;
const float CRATE_HISTORY_PRUNE_INTERVAL_MS = 5000f;
const float NEARBY_ITEM_SCAN_INTERVAL_MS = 200f;
const float ITEM_SEARCH_RANGE = 500f;
const float PICKUP_RADIUS = 20f;
const float PICKUP_RADIUS_SQ = PICKUP_RADIUS * PICKUP_RADIUS;
const float CRATE_SMASH_RADIUS = 24f;
const float CRATE_SMASH_RADIUS_SQ = CRATE_SMASH_RADIUS * CRATE_SMASH_RADIUS;
const float CRATE_SMASH_DAMAGE = 18f;
const float LOOT_PRIORITY_RANGE = 150f;
const float LOOT_PRIORITY_RANGE_SQ = LOOT_PRIORITY_RANGE * LOOT_PRIORITY_RANGE;
const float ENGAGED_ENEMY_RANGE = 90f;
const float HOSTILE_DRONE_RANGE = 220f;
const float HOSTILE_DRONE_RANGE_SQ =
    HOSTILE_DRONE_RANGE * HOSTILE_DRONE_RANGE;
const float DRONE_EVADE_DISTANCE = 100f;
const float DRONE_EVADE_COOLDOWN_MS = 650f;
const float DRONE_THROW_COOLDOWN_MS = 900f;
const float DRONE_STUCK_CHECK_MS = 1200f;
const float DRONE_STUCK_PROGRESS = 12f;
const float DRONE_HELD_THROW_MAX_DIST = 115f;
const float DRONE_FINISH_THROW_HEALTH = 24f;
const float DRONE_HELD_THROW_COOLDOWN_MS = 6000f;
const float DRONE_HELD_THROW_RETRY_MS = 1800f;
const float DRONE_CORNERED_THROW_CHANCE = 0.65f;
const float DRONE_FINISH_THROW_CHANCE = 0.45f;
const float CHAINSAW_ATTACK_RANGE = 36f;
const float CHAINSAW_ATTACK_MAX_DY = 22f;
const float THROW_CHECK_INTERVAL_MS = 3000f;
const float PRIORITY_THROW_CHECK_INTERVAL_MS = 100f;
const float THROW_PATH_CHECK_INTERVAL_MS = 350f;
const float THROW_CHANCE = 0.70f;
const float FIRST_ACTION_DELAY_MS = 2000f;
const float ACTION_COOLDOWN_MS = 1000f;
const float MID_MIN_DIST = 42f;
const float MID_MAX_DIST = 170f;
const float MID_MAX_DY = 100f;
const float MINE_MIN_DIST = 20f;
const float MINE_MAX_DIST = 76f;
const float MINE_MAX_DY = 45f;
const int PREP_GRENADE_MS = 260;
const int PREP_MOLOTOV_MS = 340;
const int PREP_MINE_MS = 210;
const int PREP_SHURIKEN_MS = 120;
const int PREP_C4_MS = 250;
const float AIM_Y_GRENADE = 8f;
const float AIM_Y_MOLOTOV = 13f;
const float AIM_Y_MINE = 3f;
const float AIM_Y_SHURIKEN = 3f;
const float AIM_Y_C4 = 6f;
const float C4_DETONATE_DELAY_MS = 1400f;
const float C4_DETONATE_RETRY_MS = 500f;
const float C4_DETONATE_TIMEOUT_MS = 6000f;
const float C4_SELF_SAFE_RANGE = 82f;
const float C4_SELF_SAFE_RANGE_SQ =
    C4_SELF_SAFE_RANGE * C4_SELF_SAFE_RANGE;
const float C4_SAFETY_RETREAT_DISTANCE = 72f;
const float C4_SAFETY_RETRY_MS = 350f;
const float FURNITURE_CHECK_INTERVAL_MS = 2000f;
const float PROP_THROW_MAX_DIST = 115f;
const float PROP_THROW_CHANCE = 0.50f;
const float PROP_THROW_VS_GUN_CHANCE = 0.90f;
const float PROP_RUSH_COOLDOWN_MS = 650f;
const float MELEE_THROW_COOLDOWN_MS = 6000f;
const float MINE_DEPLOY_MIN_DELAY_MS = 500f;
const float MINE_DEPLOY_MAX_DELAY_MS = 1200f;
const float MINE_SCATTER_MIN_DISTANCE = 38f;
const float MINE_SCATTER_MAX_DISTANCE = 82f;
const float MINE_AWARENESS_MIN = 0.50f;
const float MINE_AWARENESS_MAX = 0.90f;
const float MINE_NOTICE_RANGE = 82f;
const float MINE_NOTICE_RANGE_SQ = MINE_NOTICE_RANGE * MINE_NOTICE_RANGE;
const float MINE_AVOID_RANGE = 58f;
const float MINE_AVOID_RANGE_SQ = MINE_AVOID_RANGE * MINE_AVOID_RANGE;
const float MINE_AVOID_DISTANCE = 76f;
const float MINE_AVOID_COOLDOWN_MS = 600f;
const float DRY_RELOAD_GRACE_MS = 900f;
const float GUN_THROW_MAX_DIST = 56f;
const float GUN_THROW_COOLDOWN_MS = 8000f;
const float CRATE_TARGET_RANGE = 350f;
const float CRATE_PRIORITY = 3f;
const float FIRE_SCAN_INTERVAL_MS = 200f;
const float FIRE_INSIDE_RADIUS = 20f;
const float FIRE_AVOID_RADIUS = 46f;
const float FIRE_EVADE_DISTANCE = 78f;
const float FIRE_EVADE_COOLDOWN_MS = 650f;
const float FIRE_DETOUR_FORWARD_DISTANCE = 68f;
const float FIRE_DETOUR_HEIGHT = 34f;
const float FIRE_DETOUR_COOLDOWN_MS = 700f;
const float ENEMY_CLUSTER_RADIUS = 72f;
const float ENEMY_CLUSTER_RADIUS_SQ =
    ENEMY_CLUSTER_RADIUS * ENEMY_CLUSTER_RADIUS;
const float CATCH_RADIUS = 34f;
const float CATCH_RADIUS_SQ = CATCH_RADIUS * CATCH_RADIUS;
const float CATCH_RECORD_LIFETIME_MS = 3500f;
const float CATCH_CHANCE = 0.55f;
const float CATCH_COOLDOWN_MS = 450f;
const float OWN_GRENADE_SAFE_RANGE = 86f;
const float OWN_GRENADE_SAFE_RANGE_SQ =
    OWN_GRENADE_SAFE_RANGE * OWN_GRENADE_SAFE_RANGE;
const float EXPLOSIVE_EVADE_DISTANCE = 72f;
const float EXPLOSIVE_EVADE_COOLDOWN_MS = 450f;
const float EXPLOSIVE_KICK_RANGE_X = 29f;
const float EXPLOSIVE_KICK_RANGE_Y = 17f;
const float GRENADE_RECORD_LIFETIME_MS = 5000f;
const float C4_RECORD_LIFETIME_MS = 120000f;
const int BOT_TAUNT_CHANCE = 5;
const float BOT_TAUNT_COOLDOWN_MS = 8000f;
const float BOT_TAUNT_DELAY_MIN_MS = 600f;
const float BOT_TAUNT_DELAY_MAX_MS = 4200f;
const float BOT_TAUNT_GLOBAL_SPACING_MS = 1400f;
const float GRENADE_WARNING_RANGE = 75f;
const float GRENADE_WARNING_RANGE_SQ =
    GRENADE_WARNING_RANGE * GRENADE_WARNING_RANGE;
const int BOT_WORK_PHASES = 2;
const int MAX_TRACKED_THROWN_ITEMS = 32;
const int MAX_PENDING_BOT_TAUNTS = 24;
const float CLUSTER_SCAN_INTERVAL_MS = 450f;
const float CHAINSAW_CONTROL_MAX_MS = 5000f;
const float LOOT_GUARD_MAX_TIME_MS = 6000f;
const float LOOT_GUARD_STALL_TIME_MS = 1800f;
const float LOOT_GUARD_PROGRESS_DISTANCE = 8f;
const float LOOT_GUARD_IGNORE_TIME_MS = 7000f;
const float LOOT_PRIORITY_MAX_DY = 52f;
const float STALL_CHECK_INTERVAL_MS = 450f;
const float STALL_HORIZONTAL_PROGRESS = 8f;
const float STALL_RECOVERY_COOLDOWN_MS = 2200f;
const float PRIORITY_BACKOFF_RETRY_MS = 900f;
const float PATH_NUDGE_DISTANCE = 48f;
const float PATH_NUDGE_RAY_DISTANCE = 38f;
const float PATH_NUDGE_CONTROL_MS = 420f;
const float OBSTACLE_KICK_MAX_DX = 34f;
const float OBSTACLE_KICK_MAX_DY = 27f;
const float OBSTACLE_SINGLE_KICK_CONTROL_MS = 420f;
const float OBSTACLE_DOUBLE_KICK_CONTROL_MS = 760f;
const float OBSTACLE_DOUBLE_KICK_CHANCE = 0.55f;
const float STALL_UPWARD_PROGRESS = 7f;
const float CLIMB_PROGRESS_GRACE_MS = 1250f;
const float DRONE_GROUND_PROBE_DEPTH = 105f;
const float DRONE_UNSAFE_EDGE_MARGIN = 38f;
const float JUMP_LOOP_BRAKE_MS = 320f;
const float STATIONARY_ACTION_GRACE_MS = 2500f;
const int FAILED_NUDGES_BEFORE_TARGET_RELEASE = 3;
const float FAILED_TARGET_IGNORE_MS = 3000f;
const float LOOT_REVIEW_INTERVAL_MS = 450f;
const float LOOT_RETRY_DELAY_MS = 900f;
const float CRATE_STRIKE_DELAY_MS = 250f;
const float DECISION_LOOP_WINDOW_MS = 5500f;
const float RECOVERY_SETTLE_MS = 900f;
public class BotItemState
{
    public IPlayer Bot;
    public int WorkSlot;
    public IPlayer Target;
    public float NextEnemyReviewAt;
    public IPlayer CombatFocus;
    public float CombatFocusUntil;
    public float NextCombatFocusAt;
    public float LoopWindowAt = -1f;
    public Vector2 LoopWindowPosition;
    public int LoopAirSamples;
    public bool LoopSidestepPending;
    public float CooldownUntil;
    public float NextThrowCheck;
    public float NextClusterScanAt;
    public IPlayer CachedClusterTarget;
    public float NextThrowPathCheckAt;
    public int LastThrowPathTargetID = -1;
    public bool LastThrowPathClear;
    public float NextFurnitureCheck;
    public float NextNearbyItemScanAt;
    public float LastMeleeThrow = -100000f;
    public bool MineDeploymentPending;
    public int MineDeploymentsRemaining;
    public int MineLastScatterDirection;
    public float NextMineDeployAt;
    public WeaponItem LastThrowableItem = WeaponItem.NONE;
    public float MineAwarenessChance;
    public float NextMineAvoidAt;
    public HashSet<int> KnownMineIDs = new HashSet<int>();
    public HashSet<int> IgnoredMineIDs = new HashSet<int>();
    public float GunDrySince = -1f;
    public float LastGunThrow = -100000f;
    public bool C4Pending;
    public float C4DetonateAt;
    public float C4GiveUpAt;
    public bool PowerupPending;
    public float PowerupUseAt;
    public bool LootGuardActive;
    public IObject LootGuardTarget;
    public IObject PreviousGuardTarget;
    public float PreviousGuardRange;
    public float PreviousChaseRange;
    public float LootGuardStartedAt;
    public float LootGuardLastProgressAt;
    public float LootGuardBestDistanceSq;
    public int IgnoredLootObjectID = -1;
    public float IgnoreLootUntil;
    public int[] FailedLootIDs = new int[4];
    public float[] FailedLootUntil = new float[4];
    public int FailedLootCursor;
    public float NextLootReviewAt;
    public float NextLootChoiceAt;
    public float LootGuardLastHealth;
    public IObjectSupplyCrate PendingCrateStrike;
    public float CrateStrikeAt;
    public float RecoverySettleUntil;
    public float DecisionWindowAt = -1f;
    public Vector2 DecisionWindowPosition;
    public Vector2 DecisionLastPosition;
    public int DecisionLastDirection;
    public int DecisionReversals;
    public float DecisionTravel;
    public IObject DroneTarget;
    public IObject PreviousForcedTarget;
    public float NextDroneEvadeAt;
    public float NextDroneThrowAt;
    public float NextDroneHeldThrowAt;
    public float LastDroneHeldThrow = -100000f;
    public bool DroneEscapeLocked;
    public int DroneEscapeDirection;
    public Vector2 DroneProgressPosition;
    public float DroneProgressCheckAt;
    public bool DroneCornered;
    public bool ChainsawAttackActive;
    public float ChainsawCommandStartedAt;
    public float NextFireScanAt;
    public float NextFireEvadeAt;
    public float NextFireDetourAt;
    public bool FireEscapeLocked;
    public int FireEscapeDirection;
    public float NextCatchAt;
    public float NextExplosiveAvoidAt;
    public Vector2 StallAnchorPosition;
    public float LastMeaningfulMovementAtRealTime;
    public float NextStallCheckAtRealTime;
    public float LastStallRecoveryAtRealTime = -100000f;
    public float NextPriorityBackoffAt;
    public int LastPathNudgeDirection;
    public int ConsecutivePathNudges;
    public float ClimbPersistenceUntilRealTime;
    public int AirborneNoProgressChecks;
    public int AirborneDirectionChanges;
    public float LastAirborneVelocityY;
    public int JumpLoopEvidence;
    public float StationaryActionStartedAtRealTime = -1f;
    public int IgnoredEnemyID = -1;
    public float IgnoreEnemyUntilRealTime;
    public float ForceResumeAIAtRealTime = -1f;
    public bool CommandOwned;
    public int CommandPriority;
    public float CommandMinimumUntil;
    public float CommandDeadlineReal;
    public int ActionFacingDirection;
    public float ActionFacingLockUntil;
    public float LastTauntAt = -100000f;
    public bool TauntPending;
    public int DroneTauntObjectID;
    public string DroneTauntMode = "";
    public BotItemState(
        IPlayer bot, float now, float mineAwarenessChance, int workSlot)
    {
        Bot = bot;
        WorkSlot = workSlot;
        MineAwarenessChance = mineAwarenessChance;
        CooldownUntil = now + FIRST_ACTION_DELAY_MS;
        NextThrowCheck = now + FIRST_ACTION_DELAY_MS;
        NextFurnitureCheck = now + FURNITURE_CHECK_INTERVAL_MS;
    }
}
public class PendingBotTaunt
{
    public BotItemState State;
    public string Line;
    public float FireAt;
}
public class IncomingThrownItem
{
    public IObject Item;
    public IPlayer Thrower;
    public WeaponItem Weapon;
    public float CreatedAt;
    public HashSet<int> AttemptedBotIDs = new HashSet<int>();
    public HashSet<int> AttemptedKickBotIDs = new HashSet<int>();
    public HashSet<int> WarnedBotIDs = new HashSet<int>();
    public IncomingThrownItem(
        IObject item, IPlayer thrower, WeaponItem weapon, float now)
    {
        Item = item;
        Thrower = thrower;
        Weapon = weapon;
        CreatedAt = now;
    }
}
List<BotItemState> BotStates = new List<BotItemState>();
List<IncomingThrownItem> IncomingThrownItems =
    new List<IncomingThrownItem>();
Random Rng = new Random();
List<PendingBotTaunt> PendingBotTaunts =
    new List<PendingBotTaunt>();
float NextGlobalTauntAt = 0f;
Events.PlayerWeaponRemovedActionCallback WeaponRemovedEvent = null;
IPlayer[] CachedPlayers = new IPlayer[0];
IObjectWeaponItem[] CachedWeaponItems = new IObjectWeaponItem[0];
IObjectStreetsweeperCrate[] CachedStreetsweeperCrates =
    new IObjectStreetsweeperCrate[0];
IObjectSupplyCrate[] CachedSupplyCrates = new IObjectSupplyCrate[0];
IObjectMineThrown[] CachedMines = new IObjectMineThrown[0];
IObjectStreetsweeper[] CachedStreetsweepers =
    new IObjectStreetsweeper[0];
FireNode[] CachedFireNodes = new FireNode[0];
HashSet<int> ConfiguredSupplyCrateIDs = new HashSet<int>();
HashSet<int> SeenBotIDsScratch = new HashSet<int>();
HashSet<int> ClaimedLootIDsScratch = new HashSet<int>();
float NextWorldSnapshotAt = -1f;
float NextBotRegistrationAt = -1f;
float NextMineHistoryPruneAt = -1f;
float NextCrateHistoryPruneAt = -1f;
int WorldSnapshotPhase = 0;
int BotWorkPhase = 0;
int BackgroundWorkPhase = 0;
int FireWorkPhase = 0;
int ExplosiveWorkPhase = 0;
int MineWorkPhase = 0;
int DroneWorkPhase = 0;
int NextBotWorkSlot = 0;
int UpdateWorkPhase = 0;
int NearbyWorkPhase = 0;
int CombatWorkPhase = 0;
int CatchWorkPhase = 0;
public void OnStartup()
{
    CachedPlayers = Game.GetPlayers();
    RegisterNewBots();
    float now = Game.TotalElapsedGameTime;
    WorldSnapshotPhase = 0;
    UpdateWorkPhase = 0;
    BackgroundWorkPhase = 0;
    BotWorkPhase = 0;
    NearbyWorkPhase = 0;
    CombatWorkPhase = 0;
    CatchWorkPhase = 0;
    NextWorldSnapshotAt = now;
    NextBotRegistrationAt = now + BOT_REGISTRATION_INTERVAL_MS;
    NextMineHistoryPruneAt = now + 5000f;
    NextCrateHistoryPruneAt = now + 5000f;
    WeaponRemovedEvent = Events.PlayerWeaponRemovedActionCallback.Start(
        OnPlayerWeaponRemovedAction);
    Events.UpdateCallback.Start(OnUpdate, (int)UPDATE_INTERVAL_MS);
}
public void OnUpdate(float elapsed)
{
    if (Game.IsGameOver)
        return;
    float now = Game.TotalElapsedGameTime;
    // Exactly one world category per update, independent of bot-work phases.
    RefreshWorldSnapshots();
    DrainThrownEvents();
    ResolveCrateStrikes();
    RecoverBotControl();
    RecoverStalledBots();
    if (now >= NextBotRegistrationAt)
    {
        RegisterNewBots();
        NextBotRegistrationAt = now + BOT_REGISTRATION_INTERVAL_MS;
    }
    // Each lane has its own cursor, so even/odd ticks cannot starve a bot slot.
    if (UpdateWorkPhase == 0)
    {
        BotWorkPhase = NearbyWorkPhase;
        HandleNearbyItems();
        UpdateNearbyLootPriority();
        NearbyWorkPhase = (NearbyWorkPhase + 1) % BOT_WORK_PHASES;
    }
    else
    {
        BotWorkPhase = CombatWorkPhase;
        RunBotCombatItems();
        CombatWorkPhase = (CombatWorkPhase + 1) % BOT_WORK_PHASES;
    }
    UpdateWorkPhase = 1 - UpdateWorkPhase;

    // One hazard family per update. No nested timer wheel.
    if (BackgroundWorkPhase == 0)
    {
        HandleFireAvoidance();
        HandleIncomingThrownItems();
    }
    else if (BackgroundWorkPhase == 1)
    {
        HandleExplosiveSafety();
        HandleIncomingThrownItems();
    }
    else if (BackgroundWorkPhase == 2)
        HandleMineAwareness();
    else
        HandleEnemyStreetsweepers();
    BackgroundWorkPhase = (BackgroundWorkPhase + 1) % 4;
    UpdateBotTaunts();
    RemoveInvalidStates();
}

bool ShouldProcessBot(BotItemState state)
{
    return state != null && state.Bot != null &&
           state.WorkSlot == BotWorkPhase;
}

// Only this method resumes AI after an action owned by this mod.
bool TryBeginBotAction(IPlayer bot, float minimumGameMs, int priority)
{
    BotItemState state = FindState(bot);
    if (state == null || !IsUsableBot(bot) || Game.IsGameOver ||
        bot.IsThrowing || bot.IsHoldingActiveThrowable || bot.InThrowingMode)
        return false;
    if (state.CommandOwned)
    {
        // Emergencies may replace movement, but not an in-progress throw.
        if (priority <= state.CommandPriority)
            return false;
        int requestedFacing = state.ActionFacingDirection;
        ReleaseBotAction(state);
        state.ActionFacingDirection = requestedFacing;
    }
    // Another script may own this bot. Never commandeer its disabled AI.
    if (!bot.GetBotBehaviorActive())
        return false;
    state.CommandOwned = true;
    state.CommandPriority = priority;
    state.CommandMinimumUntil = Game.TotalElapsedGameTime + minimumGameMs;
    state.CommandDeadlineReal = Game.TotalElapsedRealTime + 10000f;
    state.ForceResumeAIAtRealTime = -1f;
    bot.SetBotBehaviorActive(false);
    bot.ClearCommandQueue();
    if (state.ActionFacingDirection != 0)
    {
        bot.SetFaceDirection(state.ActionFacingDirection);
        state.ActionFacingLockUntil =
            Game.TotalElapsedRealTime + minimumGameMs + 350f;
    }
    return true;
}

void ReleaseBotAction(BotItemState state)
{
    if (!state.CommandOwned)
        return;
    IPlayer bot = state.Bot;
    bool chainsaw = state.ChainsawAttackActive;
    state.CommandOwned = false;
    state.PendingCrateStrike = null;
    state.CommandPriority = 0;
    state.ActionFacingDirection = 0;
    state.ActionFacingLockUntil = 0f;
    state.ChainsawAttackActive = false;
    state.ChainsawCommandStartedAt = 0f;
    state.ForceResumeAIAtRealTime = -1f;
    if (bot == null || bot.IsRemoved || Game.IsGameOver)
        return;
    bot.ClearCommandQueue();
    if (chainsaw)
        bot.AddCommand(new PlayerCommand(PlayerCommandType.StopAttackRepeat));
    bot.SetBotBehaviorActive(true);
    // Time spent executing our command is not evidence of a stalled native AI.
    state.StallAnchorPosition = bot.GetWorldPosition();
    state.LastMeaningfulMovementAtRealTime = Game.TotalElapsedRealTime;
}

void RecoverBotControl()
{
    float gameNow = Game.TotalElapsedGameTime;
    float realNow = Game.TotalElapsedRealTime;
    foreach (BotItemState state in BotStates)
    {
        MaintainCombatFocus(state);
        if (!state.CommandOwned)
            continue;
        if (state.ActionFacingDirection != 0 &&
            realNow < state.ActionFacingLockUntil)
            state.Bot.SetFaceDirection(state.ActionFacingDirection);
        bool expired = gameNow >= state.CommandMinimumUntil &&
            (state.ForceResumeAIAtRealTime >= 0f
                ? realNow >= state.ForceResumeAIAtRealTime
                : gameNow >= state.CooldownUntil);
        if (expired || realNow >= state.CommandDeadlineReal ||
            !IsUsableBot(state.Bot))
            ReleaseBotAction(state);
    }
}

void RecoverStalledBots()
{
    float realNow = Game.TotalElapsedRealTime;
    float gameNow = Game.TotalElapsedGameTime;
    foreach (BotItemState state in BotStates)
    {
        IPlayer bot = state.Bot;
        if (!IsUsableBot(bot) || state.CommandOwned || !bot.GetBotBehaviorActive() ||
            realNow < state.NextStallCheckAtRealTime)
            continue;
        state.NextStallCheckAtRealTime = realNow + STALL_CHECK_INTERVAL_MS;
        Vector2 position = bot.GetWorldPosition();
        RefreshEnemyTarget(state);
        if (gameNow < state.RecoverySettleUntil || IsNativeItemAction(bot) ||
            HasForeignBotGoal(state) || state.FireEscapeLocked ||
            state.DroneEscapeLocked || state.C4Pending || IsProductiveCombat(state))
        {
            ResetMovementObservation(state, position, gameNow, realNow);
            continue;
        }
        if (CheckDecisionOscillation(state, position, gameNow, realNow))
            continue;
        if (CheckRepeatedHops(state, position, gameNow, realNow))
            continue;
        float dx = Math.Abs(position.X - state.StallAnchorPosition.X);
        float up = position.Y - state.StallAnchorPosition.Y;
        if (dx >= STALL_HORIZONTAL_PROGRESS || up >= STALL_UPWARD_PROGRESS)
        {
            state.StallAnchorPosition = position;
            state.LastMeaningfulMovementAtRealTime = realNow;
            state.ConsecutivePathNudges = 0;
            if (up >= STALL_UPWARD_PROGRESS)
                state.ClimbPersistenceUntilRealTime = realNow + CLIMB_PROGRESS_GRACE_MS;
            continue;
        }
        // The loot controller times out failed approaches itself. Do not race it.
        if (state.LootGuardActive || !bot.IsOnGround || bot.IsClimbing || bot.IsLedgeGrabbing ||
            realNow < state.ClimbPersistenceUntilRealTime ||
            realNow - state.LastMeaningfulMovementAtRealTime < STATIONARY_ACTION_GRACE_MS ||
            realNow - state.LastStallRecoveryAtRealTime < STALL_RECOVERY_COOLDOWN_MS)
            continue;
        ReleaseFailedDecision(state, position, gameNow, realNow);
    }
}

bool IsProductiveCombat(BotItemState state)
{
    IPlayer bot = state.Bot;
    if (!(bot.IsMeleeAttacking || bot.IsKicking || bot.IsHipFiring ||
          bot.IsManualAiming || bot.IsTakingCover))
        return false;
    IObject nativeTarget = bot.GetBotTarget();
    IObject target = nativeTarget != null && !nativeTarget.IsRemoved
        ? nativeTarget : state.Target;
    if (target == null || target.IsRemoved || target.DestructionInitiated)
        return false;
    IPlayer playerTarget = target as IPlayer;
    if (playerTarget != null && (playerTarget.IsDead || !IsEnemy(bot, playerTarget)))
        return false;
    if ((bot.IsMeleeAttacking || bot.IsKicking) &&
        DistanceSquared(bot.GetWorldPosition(), target.GetWorldPosition()) <= 42f * 42f)
        return HasLootReach(bot, target);
    return HasUsableRangedWeapon(bot) && HasClearThrowPath(state, target);
}

void ResetMovementObservation(BotItemState state, Vector2 position, float gameNow, float realNow)
{
    state.StallAnchorPosition = position;
    state.LastMeaningfulMovementAtRealTime = realNow;
    state.LoopWindowAt = gameNow;
    state.LoopWindowPosition = position;
    state.LoopAirSamples = 0;
    state.LoopSidestepPending = false;
    state.DecisionWindowAt = gameNow;
    state.DecisionWindowPosition = position;
    state.DecisionLastPosition = position;
    state.DecisionLastDirection = 0;
    state.DecisionReversals = 0;
    state.DecisionTravel = 0f;
}

bool CheckDecisionOscillation(BotItemState state, Vector2 position, float gameNow, float realNow)
{
    if (state.DecisionWindowAt < 0f)
    {
        state.DecisionWindowAt = gameNow;
        state.DecisionWindowPosition = position;
        state.DecisionLastPosition = position;
        return false;
    }
    float dx = position.X - state.DecisionLastPosition.X;
    if (Math.Abs(dx) >= 3f)
    {
        int direction = dx > 0f ? 1 : -1;
        if (state.DecisionLastDirection != 0 && state.DecisionLastDirection != direction)
            state.DecisionReversals++;
        state.DecisionLastDirection = direction;
        state.DecisionTravel += Math.Abs(dx);
        state.DecisionLastPosition = position;
    }
    if (gameNow - state.DecisionWindowAt < DECISION_LOOP_WINDOW_MS)
        return false;
    bool loop = state.DecisionReversals >= 4 && state.DecisionTravel >= 70f &&
        Math.Abs(position.X - state.DecisionWindowPosition.X) < 24f &&
        Math.Abs(position.Y - state.DecisionWindowPosition.Y) < 12f &&
        state.Bot.IsOnGround && !state.Bot.IsRolling && !state.Bot.IsDiving &&
        realNow - state.LastStallRecoveryAtRealTime >= STALL_RECOVERY_COOLDOWN_MS;
    state.DecisionWindowAt = gameNow;
    state.DecisionWindowPosition = position;
    state.DecisionLastPosition = position;
    state.DecisionReversals = 0;
    state.DecisionTravel = 0f;
    if (!loop)
        return false;
    ReleaseFailedDecision(state, position, gameNow, realNow);
    return true;
}

void ReleaseFailedDecision(BotItemState state, Vector2 position, float gameNow, float realNow)
{
    RememberFailedLoot(state, state.LootGuardTarget, gameNow, LOOT_GUARD_IGNORE_TIME_MS);
    ClearLootGuard(state);
    ClearCombatFocus(state);
    // Let native AI choose again without reinstalling the same scripted goal immediately.
    state.NextLootChoiceAt = gameNow + LOOT_RETRY_DELAY_MS;
    state.NextCombatFocusAt = gameNow + 3000f;
    state.RecoverySettleUntil = gameNow + RECOVERY_SETTLE_MS;
    state.NextEnemyReviewAt = realNow;
    NudgeStalledBot(state, position, gameNow, realNow);
    state.LastStallRecoveryAtRealTime = realNow;
    ResetMovementObservation(state, position, gameNow, realNow);
}

// Detect repeated returns to the same area, independently of individual hops.
bool CheckRepeatedHops(
    BotItemState state, Vector2 position, float gameNow, float realNow)
{
    IPlayer bot = state.Bot;
    bool busy = IsNativeItemAction(bot) || state.FireEscapeLocked ||
        state.DroneEscapeLocked || state.C4Pending;
    if (busy)
    {
        state.LoopWindowAt = gameNow;
        state.LoopWindowPosition = position;
        state.LoopAirSamples = 0;
        state.LoopSidestepPending = false;
        return false;
    }
    if (state.LoopSidestepPending && bot.IsOnGround)
    {
        state.LoopSidestepPending = false;
        ReleaseFailedDecision(state, position, gameNow, realNow);
        state.LastStallRecoveryAtRealTime = realNow;
        state.LoopWindowAt = gameNow;
        state.LoopWindowPosition = position;
        state.LoopAirSamples = 0;
        return state.CommandOwned;
    }
    if (state.LoopWindowAt < 0f)
    {
        state.LoopWindowAt = gameNow;
        state.LoopWindowPosition = position;
        return false;
    }
    if (!bot.IsOnGround)
        state.LoopAirSamples++;
    if (gameNow - state.LoopWindowAt < 6000f || !bot.IsOnGround)
        return false;
    bool loop = state.LoopAirSamples >= 4 &&
        Math.Abs(position.X - state.LoopWindowPosition.X) < 18f &&
        Math.Abs(position.Y - state.LoopWindowPosition.Y) < 12f &&
        realNow - state.LastStallRecoveryAtRealTime >=
            STALL_RECOVERY_COOLDOWN_MS;
    state.LoopWindowAt = gameNow;
    state.LoopWindowPosition = position;
    state.LoopAirSamples = 0;
    if (!loop)
        return false;
    RememberFailedLoot(state, state.LootGuardTarget, gameNow, LOOT_GUARD_IGNORE_TIME_MS);
    ClearLootGuard(state);
    state.NextLootChoiceAt = gameNow + LOOT_RETRY_DELAY_MS;
    state.NextCombatFocusAt = gameNow + 3000f;
    state.LoopSidestepPending = true;
    ClearCombatFocus(state);
    BrakeJumpLoop(state, position, gameNow, realNow);
    return state.CommandOwned;
}

void ClearCombatFocus(BotItemState state)
{
    IPlayer focus = state.CombatFocus;
    state.CombatFocus = null;
    IPlayer bot = state.Bot;
    if (focus != null && bot != null && !bot.IsRemoved &&
        !Game.IsGameOver && bot.GetForcedBotTarget() == focus)
        bot.SetForcedBotTarget(null);
}

void MaintainCombatFocus(BotItemState state)
{
    if (state.CombatFocus == null)
        return;
    IPlayer bot = state.Bot;
    if (!IsUsableBot(bot) || state.CombatFocus.IsRemoved ||
        state.CombatFocus.IsDead ||
        !IsEnemy(bot, state.CombatFocus) ||
        Game.TotalElapsedGameTime >= state.CombatFocusUntil ||
        state.FireEscapeLocked || state.DroneEscapeLocked ||
        state.DroneTarget != null ||
        (!state.CommandOwned && !bot.GetBotBehaviorActive()) ||
        bot.GetForcedBotTarget() != state.CombatFocus)
        ClearCombatFocus(state);
}

void TryCombatFocus(BotItemState state, float now)
{
    IPlayer bot = state.Bot;
    if (state.LootGuardActive || now < state.RecoverySettleUntil ||
        state.CombatFocus != null || now < state.NextCombatFocusAt ||
        state.Target == null || state.CommandOwned ||
        state.DroneTarget != null || state.FireEscapeLocked ||
        state.DroneEscapeLocked || state.C4Pending ||
        bot.IsMeleeAttacking || bot.IsHipFiring || bot.IsManualAiming ||
        !bot.GetBotBehaviorActive() || bot.GetForcedBotTarget() != null)
        return;
    state.NextCombatFocusAt = now + 9000f;
    // One cached-player pass, at most once per nine seconds per bot.
    int enemies = 0;
    foreach (IPlayer other in CachedPlayers)
    {
        if (other == null || other == bot || other.IsRemoved ||
            other.IsDead || !IsEnemy(bot, other))
            continue;
        enemies++;
        if (enemies > 3)
            return;
    }
    if (enemies == 0)
        return;
    // Respect guard objectives owned by another script.
    IObject guard = bot.GetGuardTarget();
    if (guard != null &&
        (!state.LootGuardActive || guard != state.LootGuardTarget))
        return;
    ClearLootGuard(state);
    state.CombatFocus = state.Target;
    state.CombatFocusUntil = now + 5000f;
    bot.SetForcedBotTarget(state.CombatFocus);
}

void BrakeJumpLoop(
    BotItemState state, Vector2 position, float gameNow, float realNow)
{
    IPlayer bot = state.Bot;
    if (bot == null || bot.IsRemoved)
        return;
    if (!TryBeginBotAction(bot, JUMP_LOOP_BRAKE_MS, 0))
        return;
    state.ForceResumeAIAtRealTime = realNow + JUMP_LOOP_BRAKE_MS;
    state.CooldownUntil = gameNow + JUMP_LOOP_BRAKE_MS;
    state.LastStallRecoveryAtRealTime = realNow;
    state.LastMeaningfulMovementAtRealTime = realNow;
    state.StallAnchorPosition = position;
    state.AirborneNoProgressChecks = 0;
    state.AirborneDirectionChanges = 0;
    state.LastAirborneVelocityY = 0f;
    state.JumpLoopEvidence = 0;
    state.StationaryActionStartedAtRealTime = -1f;
}

void NudgeStalledBot(
    BotItemState state, Vector2 position, float gameNow, float realNow)
{
    IPlayer bot = state.Bot;
    if (bot == null || bot.IsRemoved)
        return;

    if (state.ConsecutivePathNudges >=
            FAILED_NUDGES_BEFORE_TARGET_RELEASE &&
        state.Target != null && !state.Target.IsRemoved)
    {
        state.IgnoredEnemyID = state.Target.UniqueID;
        state.IgnoreEnemyUntilRealTime =
            realNow + FAILED_TARGET_IGNORE_MS;
        state.Target = null;
        state.ConsecutivePathNudges = 0;
    }

    int preferredDirection = bot.FacingDirection >= 0 ? 1 : -1;
    if (state.Target != null && !state.Target.IsRemoved &&
        !state.Target.IsDead)
    {
        preferredDirection =
            state.Target.GetWorldPosition().X >= position.X ? 1 : -1;
    }
    if (state.ConsecutivePathNudges > 0 &&
        preferredDirection == state.LastPathNudgeDirection)
        preferredDirection *= -1;

    int direction = preferredDirection;
    IObject blocker = GetPathNudgeBlocker(bot, position, direction);
    if (TryKickBlockingObject(
            state, blocker, position, gameNow, realNow))
        return;
    if (blocker != null)
    {
        direction *= -1;
        blocker = GetPathNudgeBlocker(bot, position, direction);
        if (TryKickBlockingObject(
                state, blocker, position, gameNow, realNow))
            return;
        if (blocker != null)
        {
            // No safe nudge: leave native AI and its queue untouched.
            return;
        }
    }

    Vector2 escape = new Vector2(
        position.X + direction * PATH_NUDGE_DISTANCE, position.Y);
    if (!HasGroundBelow(bot, position.X + direction * PATH_NUDGE_DISTANCE * 0.5f,
            position.Y, position.Y) ||
        !HasGroundBelow(bot, escape.X, position.Y, escape.Y))
        return;
    if (!TryBeginBotAction(bot, PATH_NUDGE_CONTROL_MS, 0))
        return;
    bot.AddCommand(new PlayerCommand(
        PlayerCommandType.StartMoveToPosition, escape));
    bot.AddCommand(new PlayerCommand(PlayerCommandType.Sprint));
    state.LastPathNudgeDirection = direction;
    state.ConsecutivePathNudges++;
    state.ForceResumeAIAtRealTime = realNow + PATH_NUDGE_CONTROL_MS;
    state.CooldownUntil = gameNow + PATH_NUDGE_CONTROL_MS;
}

IObject GetPathNudgeBlocker(
    IPlayer bot, Vector2 position, int direction)
{
    Vector2 start = new Vector2(position.X, position.Y + 7f);
    Vector2 end = new Vector2(
        position.X + direction * PATH_NUDGE_RAY_DISTANCE,
        position.Y + 7f);
    RayCastInput input = new RayCastInput(true);
    input.IncludeOverlap = false;
    input.ProjectileHit = RayCastFilterMode.True;
    RayCastResult[] hits = Game.RayCast(start, end, input);
    if (hits == null || hits.Length == 0)
        return null;
    foreach (RayCastResult hit in hits)
    {
        if (hit.Hit && hit.ObjectID != bot.UniqueID)
        {
            IObject obstacle = Game.GetObject(hit.ObjectID);
            if (obstacle != null && !obstacle.IsRemoved &&
                !obstacle.RemovalInitiated)
                return obstacle;
        }
    }
    return null;
}

bool TryKickBlockingObject(
    BotItemState state,
    IObject obstacle,
    Vector2 botPos,
    float gameNow,
    float realNow)
{
    IPlayer bot = state.Bot;
    if (bot == null || obstacle == null || obstacle.IsRemoved ||
        obstacle.RemovalInitiated ||
        obstacle.GetBodyType() != BodyType.Dynamic ||
        obstacle is IPlayer || obstacle is IObjectWeaponItem ||
        obstacle is IObjectStreetsweeper ||
        bot.IsBurningInferno || IsActivelyEngaged(state))
        return false;

    Vector2 obstaclePos = obstacle.GetWorldPosition();
    if (Math.Abs(obstaclePos.X - botPos.X) > OBSTACLE_KICK_MAX_DX ||
        Math.Abs(obstaclePos.Y - botPos.Y) > OBSTACLE_KICK_MAX_DY)
        return false;

    FaceObject(bot, obstacle);
    bool doubleKick =
        Rng.NextDouble() < OBSTACLE_DOUBLE_KICK_CHANCE;
    if (!TryBeginBotAction(bot, 450f, 0))
        return false;
    bot.AddCommand(new PlayerCommand(PlayerCommandType.Kick));
    if (doubleKick)
        bot.AddCommand(new PlayerCommand(PlayerCommandType.Kick));
    float controlTime = doubleKick
        ? OBSTACLE_DOUBLE_KICK_CONTROL_MS
        : OBSTACLE_SINGLE_KICK_CONTROL_MS;
    state.ForceResumeAIAtRealTime = realNow + controlTime;
    state.CooldownUntil = gameNow + controlTime;
    return true;
}

bool RefreshWorldSnapshots()
{
    float now = Game.TotalElapsedGameTime;
    if (now < NextWorldSnapshotAt)
        return false;
    // Loose items every 300 game-ms; other categories every 600 game-ms.
    // Includes all ground weapons, regardless of who opened/dropped them.
    if (WorldSnapshotPhase == 0 || WorldSnapshotPhase == 4)
        CachedWeaponItems = Game.GetObjects<IObjectWeaponItem>();
    else if (WorldSnapshotPhase == 1)
        CachedPlayers = Game.GetPlayers();
    else if (WorldSnapshotPhase == 2)
    {
        CachedSupplyCrates = Game.GetObjects<IObjectSupplyCrate>();
        ConfigureSupplyCrates();
    }
    else if (WorldSnapshotPhase == 3)
        CachedFireNodes = Game.GetFireNodes();
    else if (WorldSnapshotPhase == 5)
        CachedMines = Game.GetObjects<IObjectMineThrown>();
    else if (WorldSnapshotPhase == 6)
        CachedStreetsweepers = Game.GetObjects<IObjectStreetsweeper>();
    else
        CachedStreetsweeperCrates = Game.GetObjects<IObjectStreetsweeperCrate>();
    WorldSnapshotPhase = (WorldSnapshotPhase + 1) % 8;
    NextWorldSnapshotAt = now + WORLD_SNAPSHOT_INTERVAL_MS;
    return true;
}
void RegisterNewBots()
{
    float now = Game.TotalElapsedGameTime;
    foreach (IPlayer player in CachedPlayers)
    {
        if (player == null || player.IsRemoved || !player.IsBot)
            continue;
        BotItemState existingState = FindState(player);
        BotBehaviorSet behavior = player.GetBotBehaviorSet();
        if (behavior != null)
        {
            SearchItems wanted =
                SearchItems.Throwable |
                SearchItems.Streetsweeper |
                SearchItems.Powerups;
            SearchItems combined = behavior.SearchItems | wanted;
            bool changed = false;
            if (!behavior.SearchForItems)
            {
                behavior.SearchForItems = true;
                changed = true;
            }
            if (behavior.SearchItems != combined)
            {
                behavior.SearchItems = combined;
                changed = true;
            }
            if (behavior.SearchItemRange != ITEM_SEARCH_RANGE)
            {
                behavior.SearchItemRange = ITEM_SEARCH_RANGE;
                changed = true;
            }
            if (behavior.NavigationRandomPausesLevel != 0f)
            {
                behavior.NavigationRandomPausesLevel = 0f;
                changed = true;
            }
            if (behavior.OffensiveClimbingLevel != 1f)
            {
                behavior.OffensiveClimbingLevel = 1f;
                changed = true;
            }
            if (behavior.OffensiveSprintLevel != 1f)
            {
                behavior.OffensiveSprintLevel = 1f;
                changed = true;
            }
            if (behavior.MeleeWaitTimeLimitMin != 0f)
            {
                behavior.MeleeWaitTimeLimitMin = 0f;
                changed = true;
            }
            if (behavior.MeleeWaitTimeLimitMax != 0f)
            {
                behavior.MeleeWaitTimeLimitMax = 0f;
                changed = true;
            }
            if (behavior.DefensiveRollFireLevel != 1f)
            {
                behavior.DefensiveRollFireLevel = 1f;
                changed = true;
            }
            if (changed)
                player.SetBotBehaviorSet(behavior);
        }
        if (existingState == null)
        {
            BotItemState newState = new BotItemState(
                player,
                now,
                MINE_AWARENESS_MIN + (float)Rng.NextDouble() *
                    (MINE_AWARENESS_MAX - MINE_AWARENESS_MIN),
                NextBotWorkSlot);
            NextBotWorkSlot++;
            if (NextBotWorkSlot >= BOT_WORK_PHASES)
                NextBotWorkSlot = 0;
            newState.NextFireScanAt = now +
                (player.UniqueID & 1) * UPDATE_INTERVAL_MS;
            newState.NextNearbyItemScanAt = now +
                ((player.UniqueID + 1) & 1) * UPDATE_INTERVAL_MS;
            newState.StallAnchorPosition = player.GetWorldPosition();
            newState.LastMeaningfulMovementAtRealTime =
                Game.TotalElapsedRealTime;
            newState.NextStallCheckAtRealTime =
                Game.TotalElapsedRealTime + STALL_CHECK_INTERVAL_MS;
            BotStates.Add(newState);
        }
    }
}
void RemoveInvalidStates()
{
    SeenBotIDsScratch.Clear();
    for (int i = BotStates.Count - 1; i >= 0; i--)
    {
        IPlayer bot = BotStates[i].Bot;
        if (bot == null || bot.IsRemoved ||
            SeenBotIDsScratch.Contains(bot.UniqueID))
            BotStates.RemoveAt(i);
        else
            SeenBotIDsScratch.Add(bot.UniqueID);
    }
    for (int i = PendingBotTaunts.Count - 1; i >= 0; i--)
    {
        PendingBotTaunt pending = PendingBotTaunts[i];
        if (pending == null || pending.State == null ||
            pending.State.Bot == null || pending.State.Bot.IsRemoved)
            PendingBotTaunts.RemoveAt(i);
    }
}
BotItemState FindState(IPlayer player)
{
    if (player == null)
        return null;
    for (int i = 0; i < BotStates.Count; i++)
        if (BotStates[i].Bot != null &&
            BotStates[i].Bot.UniqueID == player.UniqueID)
            return BotStates[i];
    return null;
}
// A bounded inbox keeps object lookups and list mutation out of weapon events.
int[] ThrownEventIDs = new int[64];
IPlayer[] ThrownEventPlayers = new IPlayer[64];
WeaponItem[] ThrownEventWeapons = new WeaponItem[64];
int ThrownEventHead;
int ThrownEventCount;

public void OnPlayerWeaponRemovedAction(
    IPlayer player, PlayerWeaponRemovedArg arg)
{
	// Weapon events can still arrive during the victory/results transition.
	if (Game.IsGameOver)
		return;

    if (player == null || !arg.Thrown || arg.TargetObjectID == 0)
        return;
    if (ThrownEventCount == ThrownEventIDs.Length)
    {
        ThrownEventHead = (ThrownEventHead + 1) % ThrownEventIDs.Length;
        ThrownEventCount--;
    }
    int slot = (ThrownEventHead + ThrownEventCount) % ThrownEventIDs.Length;
    ThrownEventIDs[slot] = arg.TargetObjectID;
    ThrownEventPlayers[slot] = player;
    ThrownEventWeapons[slot] = arg.WeaponItem;
    ThrownEventCount++;
}
void DrainThrownEvents()
{
    for (int work = 0; work < 4 && ThrownEventCount > 0; work++)
    {
        int slot = ThrownEventHead;
        IPlayer player = ThrownEventPlayers[slot];
        int id = ThrownEventIDs[slot];
        WeaponItem weapon = ThrownEventWeapons[slot];
        ThrownEventPlayers[slot] = null;
        ThrownEventHead = (slot + 1) % ThrownEventIDs.Length;
        ThrownEventCount--;
        IObject item = Game.GetObject(id);
        if (item == null || item.IsRemoved || item.RemovalInitiated)
            continue;
        if (IncomingThrownItems.Count >= MAX_TRACKED_THROWN_ITEMS)
            IncomingThrownItems.RemoveAt(0);
        IncomingThrownItems.Add(new IncomingThrownItem(
            item, player, weapon, Game.TotalElapsedRealTime));
    }
}
void HandleFireAvoidance()
{
    float now = Game.TotalElapsedGameTime;
    FireNode[] fires = CachedFireNodes;
    foreach (BotItemState state in BotStates)
    {
        if ((state.WorkSlot & 1) != FireWorkPhase)
            continue;
        IPlayer bot = state.Bot;
        if (!IsUsableBot(bot) || now < state.NextFireScanAt)
            continue;
        state.NextFireScanAt = now + FIRE_SCAN_INTERVAL_MS;
        Vector2 botPos = bot.GetWorldPosition();
        Vector2 nearestFirePos = botPos;
        float nearestDistanceSq = 10000000000f;
        foreach (FireNode fire in fires)
        {
            float distanceSq = DistanceSquared(botPos, fire.Position);
            if (distanceSq < nearestDistanceSq)
            {
                nearestDistanceSq = distanceSq;
                nearestFirePos = fire.Position;
                if (nearestDistanceSq <=
                    FIRE_INSIDE_RADIUS * FIRE_INSIDE_RADIUS)
                    break;
            }
        }
        bool inFire = bot.IsBurningInferno ||
                      nearestDistanceSq <=
                          FIRE_INSIDE_RADIUS * FIRE_INSIDE_RADIUS;
        bool safelyClear = !bot.IsBurningInferno &&
                           nearestDistanceSq >
                               FIRE_INSIDE_RADIUS * FIRE_INSIDE_RADIUS;
        if (safelyClear)
        {
            state.FireEscapeLocked = false;
            state.FireEscapeDirection = 0;
        }
        RefreshEnemyTarget(state);
        if (inFire)
        {
            if (now < state.NextFireEvadeAt)
                continue;
            ClearLootGuard(state);
            if (!state.FireEscapeLocked)
            {
                state.FireEscapeDirection =
                    nearestFirePos.X >= botPos.X ? -1 : 1;
                state.FireEscapeLocked = true;
                QueueBotTaunt(state, "FIRE");
            }
            int direction = state.FireEscapeDirection;
            Vector2 escape = new Vector2(
                botPos.X + direction * FIRE_EVADE_DISTANCE, botPos.Y);
            if (!TryBeginBotAction(bot, 650f, 2))
                continue;
            bot.AddCommand(new PlayerCommand(PlayerCommandType.Roll, 50));
            bot.AddCommand(new PlayerCommand(
                PlayerCommandType.StartMoveToPosition, escape));
            bot.AddCommand(new PlayerCommand(PlayerCommandType.Sprint));
            state.NextFireEvadeAt = now + FIRE_EVADE_COOLDOWN_MS;
            state.CooldownUntil = now + FIRE_EVADE_COOLDOWN_MS;
            state.ForceResumeAIAtRealTime =
                Game.TotalElapsedRealTime + FIRE_EVADE_COOLDOWN_MS;
            continue;
        }
        if (state.Target != null &&
            nearestDistanceSq <= FIRE_AVOID_RADIUS * FIRE_AVOID_RADIUS &&
            now >= state.NextFireDetourAt)
        {
            float targetDx = state.Target.GetWorldPosition().X - botPos.X;
            int travelDirection = targetDx >= 0f ? 1 : -1;
            float fireDx = nearestFirePos.X - botPos.X;
            bool fireAhead = fireDx * travelDirection > 0f &&
                             Math.Abs(nearestFirePos.Y - botPos.Y) <= 32f;
            if (fireAhead)
            {
                Vector2 detour = new Vector2(
                    nearestFirePos.X + travelDirection *
                        FIRE_DETOUR_FORWARD_DISTANCE,
                    Math.Max(botPos.Y, nearestFirePos.Y) +
                        FIRE_DETOUR_HEIGHT);
                if (!TryBeginBotAction(bot, 650f, 2))
                    continue;
                bot.AddCommand(new PlayerCommand(
                    PlayerCommandType.StartMoveToPosition, detour));
                bot.AddCommand(new PlayerCommand(PlayerCommandType.Sprint));
                bot.AddCommand(new PlayerCommand(PlayerCommandType.Jump));
                state.NextFireDetourAt = now + FIRE_DETOUR_COOLDOWN_MS;
                state.CooldownUntil = now + FIRE_DETOUR_COOLDOWN_MS;
                state.ForceResumeAIAtRealTime =
                    Game.TotalElapsedRealTime + FIRE_DETOUR_COOLDOWN_MS;
            }
        }
    }
    FireWorkPhase = FireWorkPhase == 0 ? 1 : 0;
}
void HandleExplosiveSafety()
{
    float now = Game.TotalElapsedGameTime;
    foreach (IncomingThrownItem explosive in IncomingThrownItems)
    {
        if (!IsKickableExplosive(explosive.Weapon) ||
            explosive.Item == null || explosive.Item.IsRemoved ||
            explosive.Item.DestructionInitiated)
            continue;
        IObject item = explosive.Item;
        Vector2 itemPos = item.GetWorldPosition();
        foreach (BotItemState state in BotStates)
        {
            if ((state.WorkSlot & 1) != ExplosiveWorkPhase)
                continue;
            IPlayer bot = state.Bot;
            if (!IsUsableBot(bot))
                continue;
            Vector2 botPos = bot.GetWorldPosition();
            float dx = itemPos.X - botPos.X;
            float dy = itemPos.Y - botPos.Y;
            float explosiveDistanceSq = DistanceSquared(botPos, itemPos);
            if (explosive.Weapon == WeaponItem.GRENADES &&
                explosive.Thrower != bot &&
                explosiveDistanceSq <= GRENADE_WARNING_RANGE_SQ &&
                !explosive.WarnedBotIDs.Contains(bot.UniqueID))
            {
                explosive.WarnedBotIDs.Add(bot.UniqueID);
                QueueBotTaunt(state, "GRENADE_NEAR");
            }
            bool landed = !item.IsMissile &&
                item.GetLinearVelocity().Length() < 9f;
            bool directlyInFront = landed &&
                dx * bot.FacingDirection > 0f &&
                Math.Abs(dx) <= EXPLOSIVE_KICK_RANGE_X &&
                Math.Abs(dy) <= EXPLOSIVE_KICK_RANGE_Y;
            if (directlyInFront && explosive.Thrower != bot &&
                !explosive.AttemptedKickBotIDs.Contains(bot.UniqueID) &&
                now >= state.CooldownUntil)
            {
                if (!TryBeginBotAction(bot, 350f, 2))
                    continue;
                explosive.AttemptedKickBotIDs.Add(bot.UniqueID);
                QueueBotTaunt(state, "KICK_BACK");
                if (explosive.Thrower != null &&
                    !explosive.Thrower.IsRemoved)
                    FaceTarget(bot, explosive.Thrower);
                else
                    FaceObject(bot, item);
                bot.AddCommand(new PlayerCommand(PlayerCommandType.Kick));
                state.CooldownUntil = now + 350f;
                state.ForceResumeAIAtRealTime =
                    Game.TotalElapsedRealTime + 350f;
                continue;
            }
            if (explosive.Weapon == WeaponItem.GRENADES &&
                explosive.Thrower == bot &&
                DistanceSquared(botPos, itemPos) <=
                    OWN_GRENADE_SAFE_RANGE_SQ &&
                now >= state.NextExplosiveAvoidAt)
            {
                int direction = itemPos.X >= botPos.X ? -1 : 1;
                Vector2 escape = new Vector2(
                    botPos.X + direction * EXPLOSIVE_EVADE_DISTANCE,
                    botPos.Y);
                if (!TryBeginBotAction(bot, 350f, 2))
                    continue;
                bot.AddCommand(new PlayerCommand(
                    PlayerCommandType.StartMoveToPosition, escape));
                bot.AddCommand(new PlayerCommand(PlayerCommandType.Sprint));
                state.NextExplosiveAvoidAt =
                    now + EXPLOSIVE_EVADE_COOLDOWN_MS;
                state.CooldownUntil = now + EXPLOSIVE_EVADE_COOLDOWN_MS;
                state.ForceResumeAIAtRealTime =
                    Game.TotalElapsedRealTime +
                    EXPLOSIVE_EVADE_COOLDOWN_MS;
            }
        }
    }
    ExplosiveWorkPhase = ExplosiveWorkPhase == 0 ? 1 : 0;
}
bool IsKickableExplosive(WeaponItem weapon)
{
    return weapon == WeaponItem.GRENADES || weapon == WeaponItem.C4;
}
void HandleIncomingThrownItems()
{
    float realNow = Game.TotalElapsedRealTime;
    for (int i = IncomingThrownItems.Count - 1; i >= 0; i--)
    {
        IncomingThrownItem incoming = IncomingThrownItems[i];
        IObject item = incoming.Item;
        float recordLifetime = GetThrownRecordLifetime(incoming.Weapon);
        if (item == null || item.IsRemoved || item.DestructionInitiated ||
            realNow - incoming.CreatedAt > recordLifetime)
        {
            IncomingThrownItems.RemoveAt(i);
            continue;
        }
        if (!item.IsMissile)
            continue;
        foreach (BotItemState state in BotStates)
        {
            if (state.WorkSlot != CatchWorkPhase)
                continue;
            IPlayer bot = state.Bot;
            if (!IsUsableBot(bot) || state.CommandOwned ||
                !bot.GetBotBehaviorActive() || bot == incoming.Thrower ||
                realNow < state.NextCatchAt ||
                HasAttemptedCatch(incoming, bot.UniqueID))
                continue;
            if (!IsEnemy(bot, incoming.Thrower))
                continue;
            Vector2 botPos = bot.GetWorldPosition();
            Vector2 itemPos = item.GetWorldPosition();
            if (DistanceSquared(botPos, itemPos) > CATCH_RADIUS_SQ)
                continue;
            Vector2 velocity = item.GetLinearVelocity();
            float towardBot = velocity.X * (botPos.X - itemPos.X) +
                              velocity.Y * (botPos.Y - itemPos.Y);
            if (towardBot <= 0f)
                continue;
            RefreshEnemyTarget(state);
            bool engaged = IsActivelyEngaged(state);
            bool oppositeFacing = incoming.Thrower != null &&
                !incoming.Thrower.IsRemoved &&
                bot.GetFaceDirection() != incoming.Thrower.GetFaceDirection();
            if (engaged && !oppositeFacing)
                continue;
            incoming.AttemptedBotIDs.Add(bot.UniqueID);
            state.NextCatchAt = realNow + CATCH_COOLDOWN_MS;
            if (Rng.NextDouble() <= CATCH_CHANCE)
            {
                QueueBotTaunt(state, "CATCH");
                FaceObject(bot, item);
                if (!TryBeginBotAction(bot, 400f, 1))
                    continue;
                bot.AddCommand(new PlayerCommand(PlayerCommandType.Block));
                state.ForceResumeAIAtRealTime = realNow + 220f;
                state.CooldownUntil = Game.TotalElapsedGameTime + 220f;
            }
        }
    }
    CatchWorkPhase = (CatchWorkPhase + 1) % BOT_WORK_PHASES;
}
float GetThrownRecordLifetime(WeaponItem weapon)
{
    if (weapon == WeaponItem.C4)
        return C4_RECORD_LIFETIME_MS;
    if (weapon == WeaponItem.GRENADES)
        return GRENADE_RECORD_LIFETIME_MS;
    return CATCH_RECORD_LIFETIME_MS;
}
bool HasAttemptedCatch(IncomingThrownItem item, int botID)
{
    return item.AttemptedBotIDs.Contains(botID);
}
bool IsActivelyEngaged(BotItemState state)
{
    IPlayer bot = state.Bot;
    if (state.Target == null)
        return false;
    float distance = Vector2.Distance(
        bot.GetWorldPosition(), state.Target.GetWorldPosition());
    return distance <= ENGAGED_ENEMY_RANGE ||
           bot.IsMeleeAttacking ||
           bot.IsHipFiring ||
           bot.IsManualAiming ||
           bot.IsTakingCover;
}
void HandleMineAwareness()
{
    IObjectMineThrown[] mines = CachedMines;
    float now = Game.TotalElapsedGameTime;
    if (now >= NextMineHistoryPruneAt)
    {
        PruneMineHistory(mines);
        NextMineHistoryPruneAt = now + MINE_HISTORY_PRUNE_INTERVAL_MS;
    }
    foreach (BotItemState state in BotStates)
    {
        if ((state.WorkSlot & 1) != MineWorkPhase)
            continue;
        IPlayer bot = state.Bot;
        if (!IsUsableBot(bot) || state.FireEscapeLocked)
            continue;
        Vector2 botPos = bot.GetWorldPosition();
        IObjectMineThrown nearestKnownMine = null;
        float nearestDistanceSq = MINE_NOTICE_RANGE_SQ;
        foreach (IObjectMineThrown mine in mines)
        {
            if (mine == null || mine.IsRemoved ||
                mine.RemovalInitiated || mine.DestructionInitiated)
                continue;
            float distanceSq = DistanceSquared(
                botPos, mine.GetWorldPosition());
            if (distanceSq > MINE_NOTICE_RANGE_SQ)
                continue;
            int mineID = mine.UniqueID;
            bool known = state.KnownMineIDs.Contains(mineID);
            bool ignored = state.IgnoredMineIDs.Contains(mineID);
            if (!known && !ignored)
            {
                if (Rng.NextDouble() <= state.MineAwarenessChance)
                {
                    state.KnownMineIDs.Add(mineID);
                    QueueBotTaunt(state, "MINE_SPOTTED");
                    known = true;
                }
                else
                {
                    state.IgnoredMineIDs.Add(mineID);
                    ignored = true;
                }
            }
            if (known && distanceSq < nearestDistanceSq)
            {
                nearestDistanceSq = distanceSq;
                nearestKnownMine = mine;
            }
        }
        if (nearestKnownMine == null ||
            nearestDistanceSq > MINE_AVOID_RANGE_SQ ||
            now < state.NextMineAvoidAt)
            continue;
        ClearLootGuard(state);
        Vector2 minePos = nearestKnownMine.GetWorldPosition();
        int direction = minePos.X >= botPos.X ? -1 : 1;
        Vector2 escape = new Vector2(
            botPos.X + direction * MINE_AVOID_DISTANCE, botPos.Y);
        if (!TryBeginBotAction(bot, 600f, 2))
            continue;
        if (nearestDistanceSq <= 32f * 32f)
            bot.AddCommand(new PlayerCommand(PlayerCommandType.Roll, 50));
        bot.AddCommand(new PlayerCommand(
            PlayerCommandType.StartMoveToPosition, escape));
        bot.AddCommand(new PlayerCommand(PlayerCommandType.Sprint));
        state.NextMineAvoidAt = now + MINE_AVOID_COOLDOWN_MS;
        state.CooldownUntil = now + 450f;
        state.ForceResumeAIAtRealTime =
            Game.TotalElapsedRealTime + 450f;
    }
    MineWorkPhase = MineWorkPhase == 0 ? 1 : 0;
}

void PruneMineHistory(IObjectMineThrown[] mines)
{
    HashSet<int> activeMineIDs = new HashSet<int>();
    foreach (IObjectMineThrown mine in mines)
    {
        if (mine != null && !mine.IsRemoved &&
            !mine.RemovalInitiated && !mine.DestructionInitiated)
            activeMineIDs.Add(mine.UniqueID);
    }

    foreach (BotItemState state in BotStates)
    {
        PruneMineSet(state.KnownMineIDs, activeMineIDs);
        PruneMineSet(state.IgnoredMineIDs, activeMineIDs);
    }
}

void PruneMineSet(HashSet<int> remembered, HashSet<int> active)
{
    if (remembered.Count == 0)
        return;

    List<int> stale = new List<int>();
    foreach (int id in remembered)
        if (!active.Contains(id))
            stale.Add(id);
    foreach (int id in stale)
        remembered.Remove(id);
}
void HandleEnemyStreetsweepers()
{
    IObjectStreetsweeper[] drones = CachedStreetsweepers;
    float now = Game.TotalElapsedGameTime;
    foreach (BotItemState state in BotStates)
    {
        if ((state.WorkSlot & 1) != DroneWorkPhase)
            continue;
        if (state.CommandOwned)
            continue;
        IPlayer bot = state.Bot;
        if (!IsUsableBot(bot))
        {
            ClearDroneTarget(state);
            continue;
        }
        if (!bot.GetBotBehaviorActive() || IsNativeItemAction(bot) || HasForeignBotGoal(state))
            continue;
        IObjectStreetsweeper nearest = null;
        float nearestDistanceSq = HOSTILE_DRONE_RANGE_SQ;
        Vector2 botPos = bot.GetWorldPosition();
        foreach (IObjectStreetsweeper drone in drones)
        {
            if (drone == null ||
                drone.IsRemoved ||
                drone.RemovalInitiated ||
                drone.DestructionInitiated ||
                drone.GetHealth() <= 0f ||
                !IsEnemyDrone(bot, drone))
                continue;
            float distanceSq = DistanceSquared(
                botPos, drone.GetWorldPosition());
            if (distanceSq < nearestDistanceSq)
            {
                nearestDistanceSq = distanceSq;
                nearest = drone;
            }
        }
        if (nearest == null)
        {
            ClearDroneTarget(state);
            ClearDroneEscape(state);
            state.DroneTauntObjectID = 0;
            state.DroneTauntMode = "";
            continue;
        }
        ClearCombatFocus(state);
        ClearLootGuard(state);
        bool hasRangedWeapon = HasUsableRangedWeapon(bot);
        bool hasAntiDroneThrowable = HasUsableAntiDroneThrowable(bot);
        string droneTauntMode =
            hasRangedWeapon || hasAntiDroneThrowable
                ? "DRONE_ATTACK"
                : "DRONE_THREAT";
        ConsiderDroneTaunt(state, nearest, droneTauntMode);
        if (hasRangedWeapon)
        {
            ClearDroneEscape(state);
            if (state.DroneTarget != nearest)
            {
                ClearDroneTarget(state);
                nearest.SetTargetAIData(
                    new ObjectAITargetData(
                        HOSTILE_DRONE_RANGE,
                        ObjectAITargetMode.RangeOnly),
                    true);
                state.PreviousForcedTarget = bot.GetForcedBotTarget();
                state.DroneTarget = nearest;
                bot.SetForcedBotTarget(nearest);
            }
        }
        else if (hasAntiDroneThrowable)
        {
            ClearDroneEscape(state);
            ClearDroneTarget(state);
            HandleThrowableAgainstDrone(
                state, nearest, nearestDistanceSq, now);
        }
        else
        {
            ClearDroneTarget(state);
            if (TryThrowHeldWeaponAtDrone(
                    state, nearest, nearestDistanceSq, now, false))
            {
                ClearDroneEscape(state);
                continue;
            }
            if (now >= state.NextDroneEvadeAt)
            {
                if (!state.DroneEscapeLocked)
                {
                    Vector2 dronePos = nearest.GetWorldPosition();
                    state.DroneEscapeDirection =
                        dronePos.X >= botPos.X ? -1 : 1;
                    state.DroneEscapeLocked = true;
                    state.DroneProgressPosition = botPos;
                    state.DroneProgressCheckAt =
                        Game.TotalElapsedRealTime + DRONE_STUCK_CHECK_MS;
                }
                UpdateDroneEscapeProgress(state, botPos);
                if (state.DroneCornered)
                {
                    if (TryThrowHeldWeaponAtDrone(
                            state, nearest, nearestDistanceSq, now, true))
                        continue;
                    FightCorneredStreetsweeper(state, nearest, now);
                    continue;
                }
                int direction = state.DroneEscapeDirection;
                IObject safeWeapon = FindSafeAntiDronePickup(
                    bot, direction);
                Vector2 escape = safeWeapon != null
                    ? safeWeapon.GetWorldPosition()
                    : new Vector2(
                        botPos.X + direction * DRONE_EVADE_DISTANCE,
                        botPos.Y);
                escape = LimitDroneEscapeStep(botPos, escape);
                if (!HasSafeDroneEscapeGround(bot, botPos, escape))
                {
                    direction *= -1;
                    safeWeapon = FindSafeAntiDronePickup(bot, direction);
                    escape = safeWeapon != null
                        ? safeWeapon.GetWorldPosition()
                        : new Vector2(
                            botPos.X + direction * DRONE_EVADE_DISTANCE,
                            botPos.Y);
                    escape = LimitDroneEscapeStep(botPos, escape);
                    if (!HasSafeDroneEscapeGround(bot, botPos, escape))
                    {
                        state.DroneCornered = true;
                        state.DroneEscapeDirection = direction;
                        if (TryThrowHeldWeaponAtDrone(
                                state, nearest, nearestDistanceSq,
                                now, true))
                            continue;
                        FightCorneredStreetsweeper(state, nearest, now);
                        continue;
                    }
                    state.DroneEscapeDirection = direction;
                }
                if (!TryBeginBotAction(bot, 400f, 1))
                    continue;
                bot.AddCommand(new PlayerCommand(
                    PlayerCommandType.StartMoveToPosition, escape));
                bot.AddCommand(new PlayerCommand(PlayerCommandType.Sprint));
                state.CooldownUntil =
                    now + DRONE_EVADE_COOLDOWN_MS;
                state.NextDroneEvadeAt = now + DRONE_EVADE_COOLDOWN_MS;
                state.ForceResumeAIAtRealTime =
                    Game.TotalElapsedRealTime + DRONE_EVADE_COOLDOWN_MS;
            }
        }
    }
    DroneWorkPhase = DroneWorkPhase == 0 ? 1 : 0;
}
bool HasSafeDroneEscapeGround(
    IPlayer bot, Vector2 botPos, Vector2 escape)
{
    return HasGroundBelow(bot, escape.X, botPos.Y, escape.Y);
}
Vector2 LimitDroneEscapeStep(Vector2 botPos, Vector2 escape)
{
    float dx = escape.X - botPos.X;
    if (Math.Abs(dx) <= DRONE_UNSAFE_EDGE_MARGIN)
        return escape;
    int direction = dx >= 0f ? 1 : -1;
    return new Vector2(
        botPos.X + direction * DRONE_UNSAFE_EDGE_MARGIN,
        botPos.Y);
}
bool HasGroundBelow(
    IPlayer bot, float probeX, float botY, float destinationY)
{
    float highY = Math.Max(botY, destinationY) + 18f;
    float lowY = Math.Min(botY, destinationY) -
        DRONE_GROUND_PROBE_DEPTH;
    Vector2 start = new Vector2(probeX, highY);
    Vector2 end = new Vector2(probeX, lowY);
    RayCastInput input = new RayCastInput(true);
    input.IncludeOverlap = false;
    input.ProjectileHit = RayCastFilterMode.True;
    RayCastResult[] hits = Game.RayCast(start, end, input);
    if (hits == null || hits.Length == 0)
        return false;
    foreach (RayCastResult hit in hits)
    {
        if (hit.Hit && hit.ObjectID != bot.UniqueID)
            return true;
    }
    return false;
}
bool TryThrowHeldWeaponAtDrone(
    BotItemState state,
    IObjectStreetsweeper drone,
    float distanceSq,
    float now,
    bool cornered)
{
    IPlayer bot = state.Bot;
    if (bot == null || drone == null || !bot.IsOnGround ||
        bot.CurrentWeaponDrawn != WeaponItemType.Melee ||
        distanceSq > DRONE_HELD_THROW_MAX_DIST *
                     DRONE_HELD_THROW_MAX_DIST ||
        now < state.CooldownUntil ||
        now < state.NextDroneHeldThrowAt ||
        now - state.LastDroneHeldThrow < DRONE_HELD_THROW_COOLDOWN_MS)
        return false;
    WeaponItem heldMelee = bot.CurrentMeleeWeapon.WeaponItem;
    WeaponItem heldProp = bot.CurrentMeleeMakeshiftWeapon.WeaponItem;
    bool hasProp = heldProp != WeaponItem.NONE;
    if (heldMelee == WeaponItem.NONE && !hasProp)
        return false;
    bool finishingThrow = drone.GetHealth() <= DRONE_FINISH_THROW_HEALTH;
    if (!cornered && !finishingThrow)
        return false;
    state.NextDroneHeldThrowAt = now + DRONE_HELD_THROW_RETRY_MS;
    float throwChance = cornered
        ? DRONE_CORNERED_THROW_CHANCE
        : DRONE_FINISH_THROW_CHANCE;
    if (Rng.NextDouble() >= throwChance)
        return false;
    if (!HasClearThrowPath(state, drone))
        return false;
    FaceObject(bot, drone);
    if (!ThrowCurrentWeapon(bot))
        return false;
    QueueBotTaunt(state, hasProp ? "PROP_THROW" : "DRONE_ATTACK");
    state.LastDroneHeldThrow = now;
    state.CooldownUntil = now + ACTION_COOLDOWN_MS;
    state.ForceResumeAIAtRealTime =
        Game.TotalElapsedRealTime + ACTION_COOLDOWN_MS;
    return true;
}
void UpdateDroneEscapeProgress(BotItemState state, Vector2 botPos)
{
    float realNow = Game.TotalElapsedRealTime;
    if (Vector2.Distance(botPos, state.DroneProgressPosition) >=
        DRONE_STUCK_PROGRESS)
    {
        state.DroneProgressPosition = botPos;
        state.DroneProgressCheckAt = realNow + DRONE_STUCK_CHECK_MS;
        return;
    }
    if (realNow >= state.DroneProgressCheckAt)
        state.DroneCornered = true;
}
void FightCorneredStreetsweeper(
    BotItemState state, IObjectStreetsweeper drone, float now)
{
    IPlayer bot = state.Bot;
    drone.SetTargetAIData(
        new ObjectAITargetData(
            HOSTILE_DRONE_RANGE,
            ObjectAITargetMode.Default),
        true);
    if (state.DroneTarget != drone)
    {
        ClearDroneTarget(state);
        state.PreviousForcedTarget = bot.GetForcedBotTarget();
        state.DroneTarget = drone;
        bot.SetForcedBotTarget(drone);
    }
    // Forced target is sufficient; do not reset native combat commands.
    state.CooldownUntil = now;
}
bool HasUsableAntiDroneThrowable(IPlayer bot)
{
    WeaponItem item = bot.CurrentThrownItem.WeaponItem;
    return IsUsableThrowable(item) &&
           bot.CurrentThrownItem.CurrentAmmo > 0;
}
void HandleThrowableAgainstDrone(
    BotItemState state,
    IObjectStreetsweeper drone,
    float distanceSq,
    float now)
{
    IPlayer bot = state.Bot;
    WeaponItem throwable = bot.CurrentThrownItem.WeaponItem;
    float distance = (float)Math.Sqrt(distanceSq);
    float minimumDistance = throwable == WeaponItem.MINES
        ? MINE_MIN_DIST
        : (throwable == WeaponItem.SHURIKEN ? 18f : MID_MIN_DIST);
    if (now < state.NextDroneThrowAt || now < state.CooldownUntil)
        return;
    if (distance >= minimumDistance && distance <= MID_MAX_DIST &&
        bot.IsOnGround && HasClearThrowPath(state, drone))
    {
        int prep;
        float elevation;
        GetThrowableSettings(throwable, out prep, out elevation);
        Vector2 aim = GetObjectThrowableAim(
            bot, drone, throwable, elevation);
        FaceObject(bot, drone);
        if (!ThrowThrowable(bot, aim, prep))
            return;
        QueueThrowableTaunt(state, throwable);
        state.NextDroneThrowAt = now + DRONE_THROW_COOLDOWN_MS;
        state.CooldownUntil = now + ACTION_COOLDOWN_MS;
        if (throwable == WeaponItem.C4)
        {
            state.C4Pending = true;
            state.C4DetonateAt = now + C4_DETONATE_DELAY_MS;
            state.C4GiveUpAt = now + C4_DETONATE_TIMEOUT_MS;
        }
    }
    else
    {
        Vector2 botPos = bot.GetWorldPosition();
        Vector2 dronePos = drone.GetWorldPosition();
        int toward = dronePos.X >= botPos.X ? 1 : -1;
        int direction = distance < minimumDistance ? -toward : toward;
        Vector2 reposition = new Vector2(
            botPos.X + direction * 54f, botPos.Y);
        if (!TryBeginBotAction(bot, 400f, 1))
            return;
        bot.AddCommand(new PlayerCommand(
            PlayerCommandType.StartMoveToPosition, reposition));
        bot.AddCommand(new PlayerCommand(PlayerCommandType.Sprint));
        state.CooldownUntil = now + 400f;
        state.ForceResumeAIAtRealTime =
            Game.TotalElapsedRealTime + 400f;
    }
}
Vector2 GetObjectThrowableAim(
    IPlayer bot, IObject target, WeaponItem throwable, float baseElevation)
{
    Vector2 botPos = bot.GetWorldPosition();
    Vector2 targetPos = target.GetWorldPosition();
    return GetPredictedThrowableAim(
        botPos, targetPos, target.GetLinearVelocity(),
        throwable, baseElevation);
}
IObject FindSafeAntiDronePickup(IPlayer bot, int safeDirection)
{
    Vector2 botPos = bot.GetWorldPosition();
    float bestDistanceSq = 400f * 400f;
    IObject best = null;
    foreach (IObjectWeaponItem item in CachedWeaponItems)
    {
        if (item == null || item.IsRemoved || item.RemovalInitiated)
            continue;
        bool usable = item.WeaponItemType == WeaponItemType.Thrown ||
                      item.WeaponItemType == WeaponItemType.Rifle ||
                      item.WeaponItemType == WeaponItemType.Handgun;
        if (!usable)
            continue;
        Vector2 itemPos = item.GetWorldPosition();
        int itemDirection = itemPos.X >= botPos.X ? 1 : -1;
        if (itemDirection != safeDirection)
            continue;
        float itemDistanceSq = DistanceSquared(botPos, itemPos);
        if (itemDistanceSq < bestDistanceSq)
        {
            bestDistanceSq = itemDistanceSq;
            best = item;
        }
    }
    return best;
}
void ClearDroneEscape(BotItemState state)
{
    state.DroneEscapeLocked = false;
    state.DroneEscapeDirection = 0;
    state.DroneProgressPosition = new Vector2(0f, 0f);
    state.DroneProgressCheckAt = 0f;
    state.DroneCornered = false;
}
bool IsEnemyDrone(IPlayer bot, IObjectStreetsweeper drone)
{
    if (drone.GetOwnerPlayerID() == bot.UniqueID)
        return false;
    PlayerTeam botTeam = bot.GetTeam();
    PlayerTeam droneTeam = drone.GetOwnerTeam();
    return botTeam == PlayerTeam.Independent ||
           droneTeam == PlayerTeam.Independent ||
           botTeam != droneTeam;
}
bool HasUsableRangedWeapon(IPlayer bot)
{
    return (bot.CurrentPrimaryRangedWeapon.WeaponItemType ==
                WeaponItemType.Rifle &&
            bot.CurrentPrimaryRangedWeapon.TotalAmmo > 0) ||
           (bot.CurrentSecondaryRangedWeapon.WeaponItemType ==
                WeaponItemType.Handgun &&
            bot.CurrentSecondaryRangedWeapon.TotalAmmo > 0);
}
void ClearDroneTarget(BotItemState state)
{
    if (state == null || state.DroneTarget == null)
        return;
    IPlayer bot = state.Bot;
    if (bot != null && !bot.IsRemoved &&
        bot.GetForcedBotTarget() == state.DroneTarget)
    {
        IObject previous = state.PreviousForcedTarget;
        if (previous != null &&
            (previous.IsRemoved || previous.RemovalInitiated))
            previous = null;
        bot.SetForcedBotTarget(previous);
    }
    state.DroneTarget = null;
    state.PreviousForcedTarget = null;
}
void HandleNearbyItems()
{
    float now = Game.TotalElapsedGameTime;
    foreach (BotItemState state in BotStates)
    {
        if (!ShouldProcessBot(state))
            continue;
        IPlayer bot = state.Bot;
        if (!CanCollectItems(state) || now < state.NextNearbyItemScanAt)
            continue;
        state.NextNearbyItemScanAt = now + NEARBY_ITEM_SCAN_INTERVAL_MS;
        Vector2 botPos = bot.GetWorldPosition();
        IObjectWeaponItem best = null;
        float bestScore = float.MaxValue;
        // Select once, then do one reachability ray. Array order no longer wins.
        foreach (IObjectWeaponItem item in CachedWeaponItems)
        {
            if (!IsDesiredGroundItem(bot, item) ||
                IsFailedLoot(state, item.UniqueID, now))
                continue;
            float distanceSq = DistanceSquared(botPos, item.GetWorldPosition());
            if (distanceSq > PICKUP_RADIUS_SQ)
                continue;
            float score = GetGroundLootScore(bot, item, distanceSq);
            if (score < bestScore)
            {
                bestScore = score;
                best = item;
            }
        }
        if (best != null)
        {
            if (TryPickupGroundItem(state, best))
                continue; // Only one inventory/world action per bot in this pass.
            RememberFailedLoot(state, best, now, 1200f);
        }
        if (state.CommandOwned || !bot.IsOnGround)
            continue;
        bool activated = false;
        foreach (IObjectStreetsweeperCrate drone in CachedStreetsweeperCrates)
        {
            if (!IsAvailableDroneCrate(drone) ||
                DistanceSquared(botPos, drone.GetWorldPosition()) > PICKUP_RADIUS_SQ ||
                !HasLootReach(bot, drone))
                continue;
            drone.SetOwnerPlayer(bot);
            drone.SetOwnerTeam(bot.GetTeam());
            drone.StartActivate();
            drone.SetOwnerPlayer(bot);
            drone.SetOwnerTeam(bot.GetTeam());
            QueueBotTaunt(state, "DRONE_ACTIVATE");
            ClearLootGuard(state);
            activated = true;
            break;
        }
        if (activated || now < state.CooldownUntil ||
            IsActivelyEngaged(state) || state.FireEscapeLocked ||
            state.DroneEscapeLocked || state.C4Pending || HasForeignBotGoal(state) ||
            bot.IsMeleeAttacking || bot.IsHipFiring || bot.IsManualAiming)
            continue;
        IObjectSupplyCrate nearest = null;
        float nearestSq = CRATE_SMASH_RADIUS_SQ;
        foreach (IObjectSupplyCrate crate in CachedSupplyCrates)
        {
            if (!IsUsefulSupplyCrate(bot, crate) ||
                IsFailedLoot(state, crate.UniqueID, now))
                continue;
            float distanceSq = DistanceSquared(botPos, crate.GetWorldPosition());
            if (distanceSq <= nearestSq)
            {
                nearestSq = distanceSq;
                nearest = crate;
            }
        }
        if (nearest == null || !HasLootReach(bot, nearest))
            continue;
        FaceObject(bot, nearest);
        if (!TryBeginBotAction(bot, 650f, 1))
            continue;
        bot.AddCommand(new PlayerCommand(PlayerCommandType.DrawMelee));
        bot.AddCommand(new PlayerCommand(PlayerCommandType.AttackOnce));
        state.PendingCrateStrike = nearest;
        state.CrateStrikeAt = now + CRATE_STRIKE_DELAY_MS;
        state.CooldownUntil = now + 700f;
    }
}

bool IsNativeItemAction(IPlayer bot)
{
    return bot.IsThrowing || bot.IsHoldingActiveThrowable || bot.InThrowingMode ||
        bot.IsDrawingWeapon || bot.IsReloading || bot.IsGrabbing ||
        bot.IsStaggering || bot.IsStunned || bot.IsLayingOnGround;
}

bool CanCollectItems(BotItemState state)
{
    IPlayer bot = state.Bot;
    return IsUsableBot(bot) && !state.CommandOwned &&
        bot.GetBotBehaviorActive() && !IsNativeItemAction(bot);
}

bool IsAvailableDroneCrate(IObjectStreetsweeperCrate crate)
{
    return crate != null && !crate.IsRemoved && !crate.RemovalInitiated &&
        !crate.DestructionInitiated && !crate.IsActivatedAndOpening;
}

bool HasLootReach(IPlayer bot, IObject target)
{
    Vector2 start = bot.GetWorldPosition() + new Vector2(0f, 6f);
    Vector2 end = target.GetWorldPosition();
    RayCastInput input = new RayCastInput(true);
    input.IncludeOverlap = false;
    input.BlockMelee = RayCastFilterMode.True;
    RayCastResult[] hits = Game.RayCast(start, end, input);
    return hits == null || hits.Length == 0 || !hits[0].Hit ||
        hits[0].ObjectID == target.UniqueID;
}

void ResolveCrateStrikes()
{
    float now = Game.TotalElapsedGameTime;
    foreach (BotItemState state in BotStates)
    {
        IObjectSupplyCrate crate = state.PendingCrateStrike;
        if (crate == null || now < state.CrateStrikeAt)
            continue;
        IPlayer bot = state.Bot;
        if (!state.CommandOwned || !IsUsableBot(bot) ||
            !IsAvailableSupplyCrate(crate) || !bot.IsOnGround ||
            bot.IsStaggering || bot.IsStunned ||
            DistanceSquared(bot.GetWorldPosition(), crate.GetWorldPosition()) >
                CRATE_SMASH_RADIUS_SQ)
        {
            state.PendingCrateStrike = null;
            continue;
        }
        if (bot.CurrentWeaponDrawn != WeaponItemType.Melee || !bot.IsMeleeAttacking)
        {
            // Give DrawMelee time to complete, without damaging a remote crate.
            if (now + UPDATE_INTERVAL_MS < state.CommandMinimumUntil)
                state.CrateStrikeAt = now + UPDATE_INTERVAL_MS;
            else
                state.PendingCrateStrike = null;
            continue;
        }
        state.PendingCrateStrike = null;
        if (HasLootReach(bot, crate))
            crate.DealDamage(CRATE_SMASH_DAMAGE, bot.UniqueID);
    }
}
bool TryPickupGroundItem(BotItemState state, IObjectWeaponItem item)
{
    IPlayer bot = state.Bot;
    if (!CanCollectItems(state) || !IsDesiredGroundItem(bot, item) ||
        DistanceSquared(bot.GetWorldPosition(), item.GetWorldPosition()) > PICKUP_RADIUS_SQ ||
        !HasLootReach(bot, item))
        return false;
    WeaponItem weapon = item.WeaponItem;
    WeaponItemType type = item.WeaponItemType;
    int groundAmmo = (int)item.GetCurrentAmmo();
    if (type == WeaponItemType.Thrown)
    {
        ThrownWeaponItem held = bot.CurrentThrownItem;
        bool sameItem = held.WeaponItem == weapon;
        int oldAmmo = sameItem ? Math.Max(0, held.CurrentAmmo) : 0;
        // Refill an existing stack directly. GiveWeaponItem may reject a duplicate.
        if (!sameItem && !bot.GiveWeaponItem(weapon))
            return false;
        int cap = bot.CurrentThrownItem.MaxCarriedAmmo;
        if (cap <= 0)
            return false;
        int combined = (int)Math.Min((long)cap, (long)oldAmmo + groundAmmo);
        bot.SetCurrentThrownItemAmmo(combined);
        if (bot.CurrentThrownItem.WeaponItem != weapon ||
            bot.CurrentThrownItem.CurrentAmmo <= oldAmmo)
            return false;
        if (weapon == WeaponItem.MINES && !state.MineDeploymentPending)
            BeginMineDeployment(state, bot.CurrentThrownItem.CurrentAmmo);
        item.Remove();
        ClearLootGuard(state);
        return true;
    }
    if (type == WeaponItemType.Rifle)
    {
        RangedWeaponItem held = bot.CurrentPrimaryRangedWeapon;
        bool sameItem = held.WeaponItem == weapon;
        if (held.WeaponItem != WeaponItem.NONE && !sameItem)
            return false;
        if (groundAmmo <= 0 ||
            (sameItem && held.TotalAmmo >= held.MaxTotalAmmo))
            return false;
        int oldAmmo = sameItem ? Math.Max(0, held.TotalAmmo) : 0;
        // Keep the existing gun and its modifiers on duplicate pickups.
        if (!sameItem && !bot.GiveWeaponItem(weapon))
            return false;
        RangedWeaponItem current = bot.CurrentPrimaryRangedWeapon;
        int wanted = (int)Math.Min((long)current.MaxTotalAmmo,
            (long)oldAmmo + groundAmmo);
        if (!SetSafeFirearmAmmo(bot, current, wanted, true))
            return false;
        item.Remove();
        ClearLootGuard(state);
        return true;
    }
    if (type == WeaponItemType.Handgun)
    {
        RangedWeaponItem held = bot.CurrentSecondaryRangedWeapon;
        bool sameItem = held.WeaponItem == weapon;
        if (held.WeaponItem != WeaponItem.NONE && !sameItem)
            return false;
        if (groundAmmo <= 0 ||
            (sameItem && held.TotalAmmo >= held.MaxTotalAmmo))
            return false;
        int oldAmmo = sameItem ? Math.Max(0, held.TotalAmmo) : 0;
        // Keep the existing gun and its modifiers on duplicate pickups.
        if (!sameItem && !bot.GiveWeaponItem(weapon))
            return false;
        RangedWeaponItem current = bot.CurrentSecondaryRangedWeapon;
        int wanted = (int)Math.Min((long)current.MaxTotalAmmo,
            (long)oldAmmo + groundAmmo);
        if (!SetSafeFirearmAmmo(bot, current, wanted, false))
            return false;
        item.Remove();
        ClearLootGuard(state);
        return true;
    }
    bool supportedPowerup =
        weapon == WeaponItem.SPEEDBOOST ||
        weapon == WeaponItem.SLOWMO_5 ||
        weapon == WeaponItem.SLOWMO_10;
    bool gunModifier =
        weapon == WeaponItem.LAZER ||
        weapon == WeaponItem.FIREAMMO ||
        weapon == WeaponItem.BOUNCINGAMMO;
    bool eligibleGunModifier = gunModifier &&
        HasAnyFirearm(bot) && !HasAnyGunModifier(bot);
    bool occupiesPowerupSlot = type == WeaponItemType.Powerup;
    if ((!supportedPowerup && !eligibleGunModifier) ||
        (occupiesPowerupSlot && !IsPowerupSlotEmpty(bot)) || !bot.GiveWeaponItem(weapon))
        return false;
    item.Remove();
    if (weapon == WeaponItem.SLOWMO_5 ||
        weapon == WeaponItem.SLOWMO_10)
    {
        state.PowerupPending = true;
        state.PowerupUseAt = Game.TotalElapsedGameTime +
            Rng.Next(5000, 10001);
    }
    else if (occupiesPowerupSlot)
    {
        UsePowerup(bot, state);
    }
    ClearLootGuard(state);
    return true;
}
// Avoid the engine's full-active-mag representation produced by the
// total-ammo overload. RestoreCurrentMagAmmo can loop forever in that state
// when reserve ammo is not full. The explicit loaded/reserve overload runs
// the engine's redistribution callback and retains the bounded total.
bool SetSafeFirearmAmmo(
    IPlayer bot, RangedWeaponItem weapon, int total, bool primary)
{
    if (weapon.MagSize <= 0 || weapon.WeaponMagCapacity <= 0 ||
        weapon.MaxTotalAmmo <= 0)
        return false;
    total = Math.Max(0, Math.Min(total, weapon.MaxTotalAmmo));
    int units = total / weapon.MagSize;
    int loadedUnits = Math.Min(Math.Max(0, units - 1),
        weapon.WeaponMagCapacity - 1);
    int loaded = total % weapon.MagSize + loadedUnits * weapon.MagSize;
    int reserve = (total - loaded) / weapon.MagSize;
    if (primary)
        bot.SetCurrentPrimaryWeaponAmmo(loaded, reserve);
    else
        bot.SetCurrentSecondaryWeaponAmmo(loaded, reserve);
    return true;
}
bool IsThrowableSlotEmpty(IPlayer bot)
{
    ThrownWeaponItem held = bot.CurrentThrownItem;
    // A detonator is useful even with zero ammo: never replace it as an empty stack.
    if (held.WeaponItem == WeaponItem.C4DETONATOR)
        return false;
    return held.WeaponItemType != WeaponItemType.Thrown ||
        held.WeaponItem == WeaponItem.NONE || held.CurrentAmmo <= 0;
}
bool IsPowerupSlotEmpty(IPlayer bot)
{
    PowerupWeaponItem held = bot.CurrentPowerupItem;
    return held.WeaponItemType != WeaponItemType.Powerup ||
           held.WeaponItem == WeaponItem.NONE;
}
bool HasAnyFirearm(IPlayer bot)
{
    return bot.CurrentPrimaryRangedWeapon.WeaponItemType ==
               WeaponItemType.Rifle ||
           bot.CurrentSecondaryRangedWeapon.WeaponItemType ==
               WeaponItemType.Handgun;
}
bool HasAnyGunModifier(IPlayer bot)
{
    RangedWeaponItem primary = bot.CurrentPrimaryRangedWeapon;
    RangedWeaponItem secondary = bot.CurrentSecondaryRangedWeapon;
    bool primaryModified =
        primary.WeaponItemType == WeaponItemType.Rifle &&
        (primary.LazerEquipped ||
         primary.PowerupFireRounds > 0 ||
         primary.PowerupBouncingRounds > 0);
    bool secondaryModified =
        secondary.WeaponItemType == WeaponItemType.Handgun &&
        (secondary.LazerEquipped ||
         secondary.PowerupFireRounds > 0 ||
         secondary.PowerupBouncingRounds > 0);
    return primaryModified || secondaryModified;
}
void UpdateNearbyLootPriority()
{
    float now = Game.TotalElapsedGameTime;
    ClaimedLootIDsScratch.Clear();
    // Validate all reservations first; dead bots and stale targets cannot hoard loot.
    foreach (BotItemState state in BotStates)
    {
        MaintainLootGuard(state, now);
        if (state.LootGuardActive)
            ClaimedLootIDsScratch.Add(state.LootGuardTarget.UniqueID);
    }
    foreach (BotItemState state in BotStates)
    {
        if (!ShouldProcessBot(state) || now < state.NextLootReviewAt)
            continue;
        state.NextLootReviewAt = now + LOOT_REVIEW_INTERVAL_MS;
        IPlayer bot = state.Bot;
        if (!IsUsableBot(bot) || state.CommandOwned ||
            !bot.GetBotBehaviorActive() || IsNativeItemAction(bot))
            continue;
        RefreshEnemyTarget(state);
        if (IsActivelyEngaged(state) || state.CombatFocus != null ||
            state.FireEscapeLocked || state.DroneEscapeLocked ||
            state.DroneTarget != null || state.C4Pending)
        {
            if (state.LootGuardTarget != null)
                ClaimedLootIDsScratch.Remove(state.LootGuardTarget.UniqueID);
            ClearLootGuard(state);
            continue;
        }
        if (state.LootGuardActive || HasForeignBotGoal(state) ||
            now < state.NextLootChoiceAt || now < state.RecoverySettleUntil)
            continue;
        Vector2 position = bot.GetWorldPosition();
        IObject best = null;
        float bestScore = float.MaxValue;
        foreach (IObjectWeaponItem item in CachedWeaponItems)
        {
            if (!IsDesiredGroundItem(bot, item) ||
                !CanChooseLoot(state, item, position, now))
                continue;
            float score = GetGroundLootScore(bot, item,
                DistanceSquared(position, item.GetWorldPosition()));
            if (score < bestScore)
            {
                bestScore = score;
                best = item;
            }
        }
        foreach (IObjectStreetsweeperCrate crate in CachedStreetsweeperCrates)
        {
            if (!IsAvailableDroneCrate(crate) ||
                !CanChooseLoot(state, crate, position, now))
                continue;
            float score = DistanceSquared(position, crate.GetWorldPosition());
            if (score < bestScore)
            {
                bestScore = score;
                best = crate;
            }
        }
        foreach (IObjectSupplyCrate crate in CachedSupplyCrates)
        {
            if (!IsUsefulSupplyCrate(bot, crate) ||
                !CanChooseLoot(state, crate, position, now))
                continue;
            // Prefer a comparable loose item because a crate needs opening first.
            float score = DistanceSquared(position, crate.GetWorldPosition()) + 625f;
            if (score < bestScore)
            {
                bestScore = score;
                best = crate;
            }
        }
        if (best != null)
        {
            BeginLootGuard(state, best);
            if (state.LootGuardActive)
                ClaimedLootIDsScratch.Add(best.UniqueID);
        }
    }
}

bool HasForeignBotGoal(BotItemState state)
{
    IPlayer bot = state.Bot;
    IObject guard = bot.GetGuardTarget();
    if (guard != null && (!state.LootGuardActive || guard != state.LootGuardTarget))
        return true;
    IObject forced = bot.GetForcedBotTarget();
    return forced != null && forced != state.CombatFocus && forced != state.DroneTarget;
}

bool CanChooseLoot(BotItemState state, IObject item, Vector2 position, float now)
{
    return !ClaimedLootIDsScratch.Contains(item.UniqueID) &&
        !IsFailedLoot(state, item.UniqueID, now) &&
        Math.Abs(item.GetWorldPosition().Y - position.Y) <= LOOT_PRIORITY_MAX_DY &&
        DistanceSquared(position, item.GetWorldPosition()) <= LOOT_PRIORITY_RANGE_SQ;
}

bool IsFailedLoot(BotItemState state, int id, float now)
{
    for (int i = 0; i < state.FailedLootIDs.Length; i++)
        if (state.FailedLootIDs[i] == id && now < state.FailedLootUntil[i])
            return true;
    return false;
}

void RememberFailedLoot(BotItemState state, IObject target, float now, float duration)
{
    if (target == null)
        return;
    int slot = state.FailedLootCursor;
    for (int i = 0; i < state.FailedLootIDs.Length; i++)
        if (state.FailedLootIDs[i] == target.UniqueID)
        {
            slot = i;
            break;
        }
    state.FailedLootIDs[slot] = target.UniqueID;
    state.FailedLootUntil[slot] = now + duration;
    state.FailedLootCursor = (slot + 1) % state.FailedLootIDs.Length;
}

void MaintainLootGuard(BotItemState state, float now)
{
    if (!state.LootGuardActive)
        return;
    IPlayer bot = state.Bot;
    IObject target = state.LootGuardTarget;
    bool valid = IsUsableBot(bot) && target != null &&
        !target.IsRemoved && !target.RemovalInitiated && !target.DestructionInitiated;
    if (valid && target is IObjectWeaponItem)
        valid = IsDesiredGroundItem(bot, (IObjectWeaponItem)target);
    if (valid && target is IObjectSupplyCrate)
        valid = IsUsefulSupplyCrate(bot, (IObjectSupplyCrate)target);
    if (valid && target is IObjectStreetsweeperCrate)
        valid = IsAvailableDroneCrate((IObjectStreetsweeperCrate)target);
    if (!valid || bot.GetGuardTarget() != target ||
        (!state.CommandOwned && !bot.GetBotBehaviorActive()))
    {
        ClearLootGuard(state);
        return;
    }
    float distance = Vector2.Distance(bot.GetWorldPosition(), target.GetWorldPosition());
    float bestDistance = (float)Math.Sqrt(state.LootGuardBestDistanceSq);
    bool progress = distance <= bestDistance - LOOT_GUARD_PROGRESS_DISTANCE;
    IObjectSupplyCrate crate = target as IObjectSupplyCrate;
    if (crate != null && crate.GetHealth() < state.LootGuardLastHealth - 0.5f)
    {
        progress = true;
        state.LootGuardLastHealth = crate.GetHealth();
    }
    if (progress)
    {
        state.LootGuardBestDistanceSq = distance * distance;
        state.LootGuardLastProgressAt = now;
    }
    // Actions own their full startup/execution window, including crate swings.
    if (state.CommandOwned || IsNativeItemAction(bot))
    {
        state.LootGuardLastProgressAt = now;
        return;
    }
    float maxTime = crate == null ? LOOT_GUARD_MAX_TIME_MS : 9000f;
    if (now - state.LootGuardLastProgressAt < LOOT_GUARD_STALL_TIME_MS &&
        now - state.LootGuardStartedAt < maxTime)
        return;
    RememberFailedLoot(state, target, now, LOOT_GUARD_IGNORE_TIME_MS);
    ClearLootGuard(state);
    state.NextLootChoiceAt = now + LOOT_RETRY_DELAY_MS;
}
bool IsDesiredGroundItem(IPlayer bot, IObjectWeaponItem item)
{
    if (item == null || item.IsRemoved || item.RemovalInitiated ||
        item.DestructionInitiated || item.IsMissile)
        return false;
    WeaponItem weapon = item.WeaponItem;
    WeaponItemType type = item.WeaponItemType;
    if (type == WeaponItemType.Thrown)
        return item.GetCurrentAmmo() > 0f && CanAcceptThrowable(bot, weapon);
    if (type == WeaponItemType.Rifle || type == WeaponItemType.Handgun)
    {
        RangedWeaponItem held = type == WeaponItemType.Rifle
            ? bot.CurrentPrimaryRangedWeapon : bot.CurrentSecondaryRangedWeapon;
        return item.GetCurrentAmmo() > 0f &&
            (held.WeaponItem == WeaponItem.NONE ||
             (held.WeaponItem == weapon && held.TotalAmmo < held.MaxTotalAmmo));
    }
    if (IsSupportedPowerup(weapon))
        return IsPowerupSlotEmpty(bot);
    // Check the actual item category: instant upgrades need no inventory slot.
    return IsGunModifier(weapon) && HasAnyFirearm(bot) && !HasAnyGunModifier(bot) &&
        (type != WeaponItemType.Powerup || IsPowerupSlotEmpty(bot));
}

bool IsSupportedThrowable(WeaponItem item)
{
    return item == WeaponItem.GRENADES || item == WeaponItem.MOLOTOVS ||
        item == WeaponItem.MINES || item == WeaponItem.C4 || item == WeaponItem.SHURIKEN;
}

bool CanAcceptThrowable(IPlayer bot, WeaponItem weapon)
{
    if (!IsSupportedThrowable(weapon))
        return false;
    ThrownWeaponItem held = bot.CurrentThrownItem;
    if (IsThrowableSlotEmpty(bot))
        return true;
    return held.WeaponItem == weapon && held.CurrentAmmo < held.MaxCarriedAmmo;
}

bool IsSupportedPowerup(WeaponItem weapon)
{
    return weapon == WeaponItem.SPEEDBOOST || weapon == WeaponItem.SLOWMO_5 ||
        weapon == WeaponItem.SLOWMO_10;
}

bool IsGunModifier(WeaponItem weapon)
{
    return weapon == WeaponItem.LAZER || weapon == WeaponItem.FIREAMMO ||
        weapon == WeaponItem.BOUNCINGAMMO;
}

float GetGroundLootScore(IPlayer bot, IObjectWeaponItem item, float distanceSq)
{
    if (item.WeaponItemType == WeaponItemType.Thrown)
        return distanceSq * (IsThrowableSlotEmpty(bot) ? 0.45f : 0.8f);
    if (IsSupportedPowerup(item.WeaponItem))
        return distanceSq * 0.6f;
    return distanceSq;
}

bool IsAvailableSupplyCrate(IObjectSupplyCrate crate)
{
    return crate != null && !crate.IsRemoved && !crate.RemovalInitiated &&
        !crate.DestructionInitiated && crate.Destructable &&
        crate.GetHealth() > 0f && !crate.GetParachuteActive();
}

bool IsUsefulSupplyCrate(IPlayer bot, IObjectSupplyCrate crate)
{
    if (!IsAvailableSupplyCrate(crate))
        return false;
    WeaponItem weapon = crate.GetWeaponItem();
    if (IsSupportedThrowable(weapon))
        return CanAcceptThrowable(bot, weapon);
    if (IsSupportedPowerup(weapon))
        return IsPowerupSlotEmpty(bot);
    if (IsGunModifier(weapon))
        return HasAnyFirearm(bot) && !HasAnyGunModifier(bot);
    RangedWeaponItem primary = bot.CurrentPrimaryRangedWeapon;
    RangedWeaponItem secondary = bot.CurrentSecondaryRangedWeapon;
    if (weapon != WeaponItem.NONE && weapon == primary.WeaponItem)
        return primary.TotalAmmo < primary.MaxTotalAmmo;
    if (weapon != WeaponItem.NONE && weapon == secondary.WeaponItem)
        return secondary.TotalAmmo < secondary.MaxTotalAmmo;
    SupplyCategoryType category = crate.GetSupplyCategoryType();
    if (category == SupplyCategoryType.Thrown)
        return IsThrowableSlotEmpty(bot) ||
            bot.CurrentThrownItem.CurrentAmmo < bot.CurrentThrownItem.MaxCarriedAmmo;
    if (category == SupplyCategoryType.Powerup)
        return IsPowerupSlotEmpty(bot);
    if (category == SupplyCategoryType.Primary)
        return primary.WeaponItem == WeaponItem.NONE;
    if (category == SupplyCategoryType.Secondary)
        return secondary.WeaponItem == WeaponItem.NONE;
    // Unknown/random contents may still fill a missing useful slot.
    return (category == SupplyCategoryType.Any || category == SupplyCategoryType.Default ||
            category == SupplyCategoryType.Supplies) &&
        (IsThrowableSlotEmpty(bot) || IsPowerupSlotEmpty(bot) || !HasUsableRangedWeapon(bot));
}
void BeginLootGuard(BotItemState state, IObject target)
{
    IPlayer bot = state.Bot;
    if (state.LootGuardActive || !IsUsableBot(bot) || target == null ||
        state.CommandOwned || !bot.GetBotBehaviorActive() || HasForeignBotGoal(state) ||
        state.CombatFocus != null || state.DroneTarget != null)
        return;
    BotBehaviorSet behavior = bot.GetBotBehaviorSet();
    if (behavior == null)
        return;
    state.PreviousGuardTarget = bot.GetGuardTarget();
    state.PreviousGuardRange = behavior.GuardRange;
    state.PreviousChaseRange = behavior.ChaseRange;
    state.LootGuardTarget = target;
    state.LootGuardActive = true;
    float now = Game.TotalElapsedGameTime;
    state.LootGuardStartedAt = now;
    state.LootGuardLastProgressAt = now;
    state.LootGuardBestDistanceSq = DistanceSquared(
        bot.GetWorldPosition(), target.GetWorldPosition());
    state.LootGuardLastHealth = target is IObjectSupplyCrate ? target.GetHealth() : 0f;
    // Stop close enough to collect or swing, instead of guarding outside reach.
    behavior.GuardRange = 8f;
    behavior.ChaseRange = ENGAGED_ENEMY_RANGE;
    bot.SetBotBehaviorSet(behavior);
    bot.SetGuardTarget(target);
}
void ClearLootGuard(BotItemState state)
{
    if (state == null || !state.LootGuardActive)
        return;
    IPlayer bot = state.Bot;
    // Do not overwrite a newer objective installed by another script.
    if (bot != null && !bot.IsRemoved && bot.GetGuardTarget() == state.LootGuardTarget)
    {
        BotBehaviorSet behavior = bot.GetBotBehaviorSet();
        if (behavior != null)
        {
            if (Math.Abs(behavior.GuardRange - 8f) < 0.01f)
                behavior.GuardRange = state.PreviousGuardRange;
            if (Math.Abs(behavior.ChaseRange - ENGAGED_ENEMY_RANGE) < 0.01f)
                behavior.ChaseRange = state.PreviousChaseRange;
            bot.SetBotBehaviorSet(behavior);
        }
        IObject previous = state.PreviousGuardTarget;
        if (previous != null && (previous.IsRemoved || previous.RemovalInitiated))
            previous = null;
        bot.SetGuardTarget(previous);
    }
    state.LootGuardActive = false;
    state.LootGuardTarget = null;
    state.PreviousGuardTarget = null;
}
void UsePowerup(IPlayer bot, BotItemState state)
{
    if (bot == null || bot.IsRemoved)
        return;
    if (!TryBeginBotAction(bot, 250f, 1))
        return;
    bot.AddCommand(new PlayerCommand(PlayerCommandType.DrawPowerup));
    state.ForceResumeAIAtRealTime = Game.TotalElapsedRealTime + 250f;
    state.CooldownUntil = Game.TotalElapsedGameTime + 250f;
}
void RunBotCombatItems()
{
    float now = Game.TotalElapsedGameTime;
    foreach (BotItemState state in BotStates)
    {
        if (!ShouldProcessBot(state))
            continue;
        IPlayer bot = state.Bot;
        if (!IsUsableBot(bot))
            continue;
        if (state.CommandOwned)
        {
            if (state.ChainsawAttackActive)
            {
                RefreshEnemyTarget(state);
                if (state.Target == null || state.Target.IsDead ||
                    state.Target.IsRemoved ||
                    bot.CurrentMeleeWeapon.WeaponItem != WeaponItem.CHAINSAW ||
                    DistanceSquared(bot.GetWorldPosition(),
                        state.Target.GetWorldPosition()) >
                            CHAINSAW_ATTACK_RANGE * CHAINSAW_ATTACK_RANGE)
                    ReleaseBotAction(state);
            }
            continue;
        }
        if (!bot.GetBotBehaviorActive() || IsNativeItemAction(bot) ||
            now < state.RecoverySettleUntil)
            continue;
        if (state.PowerupPending && now >= state.PowerupUseAt)
        {
            if (!IsPowerupSlotEmpty(bot))
                UsePowerup(bot, state);
            state.PowerupPending = false;
            if (state.CommandOwned)
                continue;
        }
        DetectMinePickup(state, now);
        if (HandleMineDeployment(state, now))
            continue;
        if (HandlePendingC4(state, now))
            continue;
        RefreshEnemyTarget(state);
        TryCombatFocus(state, now);
        if (state.Target == null)
        {
            if (state.ChainsawAttackActive)
                StopChainsawAttack(state, now);
            continue;
        }
        float distance = Vector2.Distance(
            bot.GetWorldPosition(),
            state.Target.GetWorldPosition());
        float dy = state.Target.GetWorldPosition().Y -
                   bot.GetWorldPosition().Y;
        if (HandleChainsawCombat(state, distance, dy, now))
            continue;
        WeaponItem heldThrowable = bot.CurrentThrownItem.WeaponItem;
        IPlayer throwTarget = state.Target;
        IPlayer clusterTarget = GetCachedEnemyCluster(
            state, heldThrowable, now);
        bool clusterExplosive = clusterTarget != null;
        if (clusterExplosive && clusterTarget != null)
        {
            throwTarget = clusterTarget;
            distance = Vector2.Distance(
                bot.GetWorldPosition(), throwTarget.GetWorldPosition());
            dy = throwTarget.GetWorldPosition().Y -
                 bot.GetWorldPosition().Y;
        }
        bool noGun = !HasUsableRangedWeapon(bot);
        bool rangedAttackSituation = distance >= MID_MIN_DIST &&
                                     distance <= MID_MAX_DIST &&
                                     Math.Abs(dy) <= MID_MAX_DY;
        bool shurikenPriority = heldThrowable == WeaponItem.SHURIKEN &&
                                noGun && rangedAttackSituation;
        bool otherNoGunPriority = heldThrowable != WeaponItem.SHURIKEN &&
                                  noGun &&
                                  IsUsableThrowable(heldThrowable);
        bool priorityThrow = clusterExplosive ||
                             shurikenPriority ||
                             otherNoGunPriority;
        if (priorityThrow &&
            state.NextThrowCheck > now + PRIORITY_THROW_CHECK_INTERVAL_MS)
        {
            state.NextThrowCheck = now;
        }
        if (now >= state.NextThrowCheck)
        {
            state.NextThrowCheck = now + (priorityThrow
                ? PRIORITY_THROW_CHECK_INTERVAL_MS
                : THROW_CHECK_INTERVAL_MS);
            if (bot.IsOnGround &&
                now >= state.CooldownUntil &&
                (!state.C4Pending ||
                 bot.CurrentThrownItem.WeaponItem == WeaponItem.C4) &&
                (priorityThrow || Rng.NextDouble() < THROW_CHANCE))
            {
                WeaponItem throwable = heldThrowable;
                float throwableMinDistance =
                    throwable == WeaponItem.MOLOTOVS && noGun
                        ? 34f
                        : MID_MIN_DIST;
                bool mineRange = distance >= MINE_MIN_DIST &&
                                 distance <= MINE_MAX_DIST &&
                                 Math.Abs(dy) <= MINE_MAX_DY;
                bool midRange = distance >= throwableMinDistance &&
                                distance <= MID_MAX_DIST &&
                                Math.Abs(dy) <= MID_MAX_DY;
                if (((throwable == WeaponItem.MINES && mineRange) ||
                    (throwable != WeaponItem.MINES &&
                     throwable != WeaponItem.NONE &&
                     throwable != WeaponItem.C4DETONATOR &&
                     midRange)) &&
                    HasClearThrowPath(state, throwTarget))
                {
                    int prep;
                    float elevation;
                    GetThrowableSettings(throwable, out prep, out elevation);
                    Vector2 aim = GetThrowableAim(
                        bot, throwTarget, throwable, elevation);
                    FaceTarget(bot, throwTarget);
                    if (!ThrowThrowable(bot, aim, prep))
                        continue;
                    QueueThrowableTaunt(state, throwable);
                    state.CooldownUntil = now + ACTION_COOLDOWN_MS;
                    if (throwable == WeaponItem.C4)
                    {
                        state.C4Pending = true;
                        state.C4DetonateAt = now + C4_DETONATE_DELAY_MS;
                        state.C4GiveUpAt = now + C4_DETONATE_TIMEOUT_MS;
                    }
                    continue;
                }
                if (priorityThrow &&
                    now >= state.NextPriorityBackoffAt &&
                    Math.Abs(dy) <= MID_MAX_DY &&
                    ((throwable == WeaponItem.MINES &&
                     distance < MINE_MIN_DIST) ||
                     (throwable != WeaponItem.MINES &&
                      distance < throwableMinDistance)))
                {
                    BackOffForPriorityThrow(bot, throwTarget);
                    state.NextPriorityBackoffAt =
                        now + PRIORITY_BACKOFF_RETRY_MS;
                    state.CooldownUntil = now + 250f;
                    state.ForceResumeAIAtRealTime =
                        Game.TotalElapsedRealTime + 250f;
                    continue;
                }
            }
        }
        HandleWeaponThrowing(state, distance, now);
    }
}
bool HandleChainsawCombat(
    BotItemState state, float distance, float dy, float now)
{
    IPlayer bot = state.Bot;
    bool hasChainsaw =
        bot.CurrentMeleeWeapon.WeaponItem == WeaponItem.CHAINSAW;
    bool inAttackRange = distance <= CHAINSAW_ATTACK_RANGE &&
                         Math.Abs(dy) <= CHAINSAW_ATTACK_MAX_DY;
    if (!hasChainsaw || !inAttackRange || state.Target == null ||
        state.Target.IsDead || state.Target.IsRemoved ||
        !HasClearThrowPath(state, state.Target))
    {
        if (state.ChainsawAttackActive)
            StopChainsawAttack(state, now);
        return false;
    }
    if (now < state.CooldownUntil)
        return true;
    FaceTarget(bot, state.Target);
    bool commandHadTimeToStart =
        now - state.ChainsawCommandStartedAt >= 450f;
    if (!state.ChainsawAttackActive ||
        (commandHadTimeToStart && !bot.IsMeleeAttacking))
    {
        if (!TryBeginBotAction(bot, CHAINSAW_CONTROL_MAX_MS, 1))
            return true;
        bot.AddCommand(new PlayerCommand(PlayerCommandType.DrawMelee));
        bot.AddCommand(new PlayerCommand(
            PlayerCommandType.StartAttackRepeat));
        state.ChainsawAttackActive = true;
        state.ChainsawCommandStartedAt = now;
        QueueBotTaunt(state, "CHAINSAW");
    }
    return true;
}
void StopChainsawAttack(BotItemState state, float now)
{
    IPlayer bot = state.Bot;
    ReleaseBotAction(state);
    state.ChainsawAttackActive = false;
    state.ChainsawCommandStartedAt = 0f;
    if (now < state.CooldownUntil)
        return;
    if (!TryBeginBotAction(bot, 120f, 1))
        return;
    bot.AddCommand(new PlayerCommand(
        PlayerCommandType.StopAttackRepeat));
    state.CooldownUntil = now + 120f;
    state.ForceResumeAIAtRealTime =
        Game.TotalElapsedRealTime + 120f;
}
Vector2 GetThrowableAim(
    IPlayer bot, IPlayer target, WeaponItem throwable, float baseElevation)
{
    Vector2 botPos = bot.GetWorldPosition();
    Vector2 targetPos = target.GetWorldPosition();
    return GetPredictedThrowableAim(
        botPos, targetPos, target.GetLinearVelocity(),
        throwable, baseElevation);
}
Vector2 GetPredictedThrowableAim(
    Vector2 botPos,
    Vector2 targetPos,
    Vector2 targetVelocity,
    WeaponItem throwable,
    float baseElevation)
{
    float horizontalDistance = Math.Abs(targetPos.X - botPos.X);
    float leadTime;
    if (throwable == WeaponItem.SHURIKEN)
        leadTime = Math.Min(0.28f, 0.10f + horizontalDistance / 900f);
    else if (throwable == WeaponItem.MINES)
        leadTime = Math.Min(0.18f, 0.06f + horizontalDistance / 1100f);
    else
        leadTime = Math.Min(0.48f, 0.18f + horizontalDistance / 650f);

    float leadX = Math.Max(
        -30f, Math.Min(30f, targetVelocity.X * leadTime));
    float leadY = Math.Max(
        -12f, Math.Min(12f, targetVelocity.Y * leadTime));
    Vector2 predicted = new Vector2(
        targetPos.X + leadX, targetPos.Y + leadY);

    float arc = baseElevation;
    if (throwable != WeaponItem.SHURIKEN &&
        throwable != WeaponItem.MINES)
    {
        float predictedDistance = Math.Abs(predicted.X - botPos.X);
        arc += Math.Min(30f, predictedDistance * 0.145f);
        float heightDifference = predicted.Y - botPos.Y;
        if (heightDifference > 0f)
            arc += Math.Min(20f, heightDifference * 0.22f);
        else
            arc += Math.Max(-8f, heightDifference * 0.08f);
    }
    return new Vector2(predicted.X, predicted.Y + arc);
}
void DetectMinePickup(BotItemState state, float now)
{
    WeaponItem current = state.Bot.CurrentThrownItem.WeaponItem;
    if (current == WeaponItem.MINES &&
        state.LastThrowableItem != WeaponItem.MINES)
    {
        BeginMineDeployment(
            state, state.Bot.CurrentThrownItem.CurrentAmmo);
    }
    state.LastThrowableItem = current;
    if (current != WeaponItem.MINES)
        state.MineDeploymentPending = false;
}
void BeginMineDeployment(BotItemState state, int availableMines)
{
    int desired = Rng.Next(2, 4);
    if (availableMines > 0)
        desired = Math.Min(desired, availableMines);
    state.MineDeploymentPending = desired > 0;
    state.MineDeploymentsRemaining = desired;
    state.MineLastScatterDirection = Rng.Next(0, 2) == 0 ? -1 : 1;
    state.NextMineDeployAt = Game.TotalElapsedGameTime +
        Rng.Next((int)MINE_DEPLOY_MIN_DELAY_MS,
                 (int)MINE_DEPLOY_MAX_DELAY_MS + 1);
    state.LastThrowableItem = WeaponItem.MINES;
}
bool HandleMineDeployment(BotItemState state, float now)
{
    if (!state.MineDeploymentPending || now < state.NextMineDeployAt)
        return false;
    IPlayer bot = state.Bot;
    if (bot.CurrentThrownItem.WeaponItem != WeaponItem.MINES ||
        bot.CurrentThrownItem.CurrentAmmo <= 0)
    {
        state.MineDeploymentPending = false;
        return false;
    }
    if (!bot.IsOnGround || now < state.CooldownUntil)
        return true;
    state.MineLastScatterDirection *= -1;
    float distance = MINE_SCATTER_MIN_DISTANCE +
        (float)Rng.NextDouble() *
        (MINE_SCATTER_MAX_DISTANCE - MINE_SCATTER_MIN_DISTANCE);
    Vector2 botPos = bot.GetWorldPosition();
    Vector2 aim = new Vector2(
        botPos.X + state.MineLastScatterDirection * distance,
        botPos.Y + Rng.Next(2, 11));
    if (state.MineLastScatterDirection > 0)
        bot.SetFaceDirection(1);
    else
        bot.SetFaceDirection(-1);
    if (!ThrowThrowable(bot, aim, PREP_MINE_MS))
        return true;
    QueueThrowableTaunt(state, WeaponItem.MINES);
    state.MineDeploymentsRemaining--;
    state.MineDeploymentPending = state.MineDeploymentsRemaining > 0;
    state.NextMineDeployAt = now +
        Rng.Next((int)MINE_DEPLOY_MIN_DELAY_MS,
                 (int)MINE_DEPLOY_MAX_DELAY_MS + 1);
    state.CooldownUntil = now + ACTION_COOLDOWN_MS;
    return true;
}
bool IsUsableThrowable(WeaponItem item)
{
    return item != WeaponItem.NONE &&
           item != WeaponItem.C4DETONATOR;
}
bool IsClusterExplosive(WeaponItem item)
{
    return item == WeaponItem.C4 ||
           item == WeaponItem.GRENADES ||
           item == WeaponItem.MOLOTOVS;
}
IPlayer GetCachedEnemyCluster(
    BotItemState state, WeaponItem heldThrowable, float now)
{
    if (!IsClusterExplosive(heldThrowable))
    {
        state.CachedClusterTarget = null;
        return null;
    }
    IPlayer cached = state.CachedClusterTarget;
    bool cachedValid = cached != null && !cached.IsRemoved &&
        !cached.IsDead && IsEnemy(state.Bot, cached);
    if (now < state.NextClusterScanAt)
        return cachedValid ? cached : null;
    state.NextClusterScanAt = now + CLUSTER_SCAN_INTERVAL_MS;
    IPlayer found;
    if (TryFindEnemyCluster(state.Bot, out found))
        state.CachedClusterTarget = found;
    else
        state.CachedClusterTarget = null;
    return state.CachedClusterTarget;
}
bool TryFindEnemyCluster(IPlayer bot, out IPlayer clusterTarget)
{
    clusterTarget = null;
    float bestDistance = 100000f;
    Vector2 botPos = bot.GetWorldPosition();
    IPlayer[] players = CachedPlayers;
    foreach (IPlayer candidate in players)
    {
        if (candidate == null || candidate.IsRemoved || candidate.IsDead ||
            candidate == bot || !IsEnemy(bot, candidate))
            continue;
        Vector2 candidatePos = candidate.GetWorldPosition();
        float distance = Vector2.Distance(botPos, candidatePos);
        if (distance < MID_MIN_DIST || distance > MID_MAX_DIST ||
            Math.Abs(candidatePos.Y - botPos.Y) > MID_MAX_DY)
            continue;
        bool hasNearbyEnemy = false;
        foreach (IPlayer neighbor in players)
        {
            if (neighbor == null || neighbor == candidate ||
                neighbor.IsRemoved || neighbor.IsDead || neighbor == bot ||
                !IsEnemy(bot, neighbor))
                continue;
            if (DistanceSquared(
                candidatePos, neighbor.GetWorldPosition()) <=
                    ENEMY_CLUSTER_RADIUS_SQ)
            {
                hasNearbyEnemy = true;
                break;
            }
        }
        if (hasNearbyEnemy && distance < bestDistance)
        {
            bestDistance = distance;
            clusterTarget = candidate;
        }
    }
    return clusterTarget != null;
}
void BackOffForPriorityThrow(IPlayer bot, IPlayer target)
{
    Vector2 botPos = bot.GetWorldPosition();
    int direction = target.GetWorldPosition().X >= botPos.X ? -1 : 1;
    Vector2 backOff = new Vector2(botPos.X + direction * 48f, botPos.Y);
    if (!TryBeginBotAction(bot, 400f, 1))
        return;
    bot.AddCommand(new PlayerCommand(
        PlayerCommandType.StartMoveToPosition, backOff));
    bot.AddCommand(new PlayerCommand(PlayerCommandType.Sprint));
}
void GetThrowableSettings(WeaponItem item, out int prep, out float elevation)
{
    prep = PREP_GRENADE_MS;
    elevation = AIM_Y_GRENADE;
    if (item == WeaponItem.MOLOTOVS)
    {
        prep = PREP_MOLOTOV_MS;
        elevation = AIM_Y_MOLOTOV;
    }
    else if (item == WeaponItem.MINES)
    {
        prep = PREP_MINE_MS;
        elevation = AIM_Y_MINE;
    }
    else if (item == WeaponItem.SHURIKEN)
    {
        prep = PREP_SHURIKEN_MS;
        elevation = AIM_Y_SHURIKEN;
    }
    else if (item == WeaponItem.C4)
    {
        prep = PREP_C4_MS;
        elevation = AIM_Y_C4;
    }
}
bool ThrowThrowable(IPlayer bot, Vector2 aim, int prepMs)
{
    if (!TryBeginBotAction(bot, prepMs + 600f, 3))
        return false;
    bot.AddCommand(new PlayerCommand(PlayerCommandType.DrawThrowable));
    bot.AddCommand(new PlayerCommand(PlayerCommandType.StartAimAtPrecise, aim));
    bot.AddCommand(new PlayerCommand(PlayerCommandType.PrepareThrow, prepMs));
    bot.AddCommand(new PlayerCommand(PlayerCommandType.Throw));
    bot.AddCommand(new PlayerCommand(PlayerCommandType.StopAim));
    return true;
}
bool DetonateC4(IPlayer bot)
{
    if (!TryBeginBotAction(bot, 450f, 3))
        return false;
    bot.AddCommand(new PlayerCommand(PlayerCommandType.DrawThrowable));
    bot.AddCommand(new PlayerCommand(PlayerCommandType.AttackOnce));
    return true;
}
bool HandlePendingC4(BotItemState state, float now)
{
    if (!state.C4Pending || now < state.C4DetonateAt)
        return false;
    IPlayer bot = state.Bot;
    WeaponItem held = bot.CurrentThrownItem.WeaponItem;
    if (held == WeaponItem.C4DETONATOR)
    {
        IObject unsafeC4 = FindUnsafeOwnC4(bot);
        if (unsafeC4 != null)
        {
            if (now >= state.C4GiveUpAt)
            {
                state.C4Pending = false;
                ReleaseBotAction(state);
                state.CooldownUntil = now;
                return false;
            }
            Vector2 botPos = bot.GetWorldPosition();
            Vector2 c4Pos = unsafeC4.GetWorldPosition();
            int direction = c4Pos.X >= botPos.X ? -1 : 1;
            Vector2 escape = new Vector2(
                botPos.X + direction * C4_SAFETY_RETREAT_DISTANCE,
                botPos.Y);
            if (!TryBeginBotAction(bot, 400f, 1))
                return true;
            bot.AddCommand(new PlayerCommand(
                PlayerCommandType.StartMoveToPosition, escape));
            bot.AddCommand(new PlayerCommand(PlayerCommandType.Sprint));
            state.C4DetonateAt = now + C4_SAFETY_RETRY_MS;
            state.CooldownUntil = now + C4_SAFETY_RETRY_MS;
            state.ForceResumeAIAtRealTime =
                Game.TotalElapsedRealTime + C4_SAFETY_RETRY_MS;
            return true;
        }
        if (!DetonateC4(bot))
        {
            state.C4DetonateAt = now + C4_DETONATE_RETRY_MS;
            return true;
        }
        state.C4Pending = false;
        state.CooldownUntil = now + ACTION_COOLDOWN_MS;
        return true;
    }
    if (held == WeaponItem.C4)
    {
        if (now >= state.C4GiveUpAt)
        {
            state.C4Pending = false;
            return false;
        }
        state.C4DetonateAt = now + C4_DETONATE_RETRY_MS;
        return false;
    }
    if (IsThrowableSlotEmpty(bot) &&
        bot.GiveWeaponItem(WeaponItem.C4DETONATOR))
    {
        state.C4DetonateAt = now + C4_DETONATE_RETRY_MS;
        return false;
    }
    if (now < state.C4GiveUpAt)
    {
        state.C4DetonateAt = now + C4_DETONATE_RETRY_MS;
    }
    else
    {
        state.C4Pending = false;
    }
    return false;
}
IObject FindUnsafeOwnC4(IPlayer bot)
{
    IObject nearest = null;
    float nearestDistanceSq = C4_SELF_SAFE_RANGE_SQ;
    Vector2 botPos = bot.GetWorldPosition();
    foreach (IncomingThrownItem thrown in IncomingThrownItems)
    {
        if (thrown.Weapon != WeaponItem.C4 ||
            thrown.Thrower != bot || thrown.Item == null ||
            thrown.Item.IsRemoved || thrown.Item.DestructionInitiated)
            continue;
        float distanceSq = DistanceSquared(
            botPos, thrown.Item.GetWorldPosition());
        if (distanceSq <= nearestDistanceSq)
        {
            nearestDistanceSq = distanceSq;
            nearest = thrown.Item;
        }
    }
    return nearest;
}
void HandleWeaponThrowing(BotItemState state, float distance, float now)
{
    IPlayer bot = state.Bot;
    if (now < state.CooldownUntil)
        return;
    if (bot.CurrentWeaponDrawn == WeaponItemType.Thrown ||
        bot.IsHoldingActiveThrowable)
        return;
    if (now >= state.NextFurnitureCheck)
    {
        state.NextFurnitureCheck = now + FURNITURE_CHECK_INTERVAL_MS;
        WeaponItem prop = bot.CurrentMeleeMakeshiftWeapon.WeaponItem;
        if (bot.CurrentWeaponDrawn == WeaponItemType.Melee &&
            prop != WeaponItem.NONE &&
            now - state.LastMeleeThrow >= MELEE_THROW_COOLDOWN_MS)
        {
            bool enemyHasGun = HasUsableRangedWeapon(state.Target);
            float throwChance = enemyHasGun
                ? PROP_THROW_VS_GUN_CHANCE
                : PROP_THROW_CHANCE;
            bool chooseThrow = Rng.NextDouble() < throwChance;
            if (chooseThrow && distance <= PROP_THROW_MAX_DIST &&
                HasClearThrowPath(state, state.Target))
            {
                FaceTarget(bot, state.Target);
                if (!ThrowCurrentWeapon(bot))
                    return;
                QueueBotTaunt(state, "PROP_THROW");
                state.LastMeleeThrow = now;
                state.CooldownUntil = now + ACTION_COOLDOWN_MS;
            }
            else if (!chooseThrow)
            {
                RushWithProp(bot, state.Target, enemyHasGun);
                state.CooldownUntil = now + PROP_RUSH_COOLDOWN_MS;
                state.ForceResumeAIAtRealTime =
                    Game.TotalElapsedRealTime + PROP_RUSH_COOLDOWN_MS;
            }
            return;
        }
    }
    if (bot.CurrentWeaponDrawn == WeaponItemType.Rifle ||
        bot.CurrentWeaponDrawn == WeaponItemType.Handgun)
    {
        int clipAmmo = GetClipAmmo(bot);
        if (clipAmmo > 0)
        {
            state.GunDrySince = -1f;
        }
        else if (clipAmmo == 0)
        {
            RangedWeaponItem gun = bot.CurrentWeaponDrawn == WeaponItemType.Rifle
                ? bot.CurrentPrimaryRangedWeapon : bot.CurrentSecondaryRangedWeapon;
            if (gun.TotalAmmo > 0 || bot.IsReloading)
            {
                state.GunDrySince = -1f;
                return;
            }
            if (state.GunDrySince < 0f)
                state.GunDrySince = now;
            if (now - state.GunDrySince >= DRY_RELOAD_GRACE_MS &&
                distance < GUN_THROW_MAX_DIST &&
                now - state.LastGunThrow >= GUN_THROW_COOLDOWN_MS &&
                HasClearThrowPath(state, state.Target))
            {
                FaceTarget(bot, state.Target);
                if (!ThrowCurrentWeapon(bot))
                    return;
                state.LastGunThrow = now;
                state.GunDrySince = -1f;
                state.CooldownUntil = now + ACTION_COOLDOWN_MS;
            }
        }
    }
    else
    {
        state.GunDrySince = -1f;
    }
}
void RushWithProp(IPlayer bot, IPlayer target, bool rollTowardArmedEnemy)
{
    FaceTarget(bot, target);
    if (!TryBeginBotAction(bot, 400f, 1))
        return;
    if (rollTowardArmedEnemy)
        bot.AddCommand(new PlayerCommand(PlayerCommandType.Roll, 50));
    bot.AddCommand(new PlayerCommand(
        PlayerCommandType.StartMoveToPosition,
        target.GetWorldPosition()));
    bot.AddCommand(new PlayerCommand(PlayerCommandType.Sprint));
    bot.AddCommand(new PlayerCommand(PlayerCommandType.DrawMelee));
}
bool HasClearThrowPath(BotItemState state, IObject target)
{
    IPlayer bot = state.Bot;
    if (bot == null || target == null || target.IsRemoved)
        return false;

    float now = Game.TotalElapsedGameTime;
    if (state.LastThrowPathTargetID == target.UniqueID &&
        now < state.NextThrowPathCheckAt)
        return state.LastThrowPathClear;

    Vector2 start = bot.GetWorldPosition();
    Vector2 end = target.GetWorldPosition();
    start = new Vector2(start.X, start.Y + 6f);
    end = new Vector2(end.X, end.Y + 6f);

    RayCastInput input = new RayCastInput(true);
    input.IncludeOverlap = false;
    input.ProjectileHit = RayCastFilterMode.True;
    RayCastResult[] hits = Game.RayCast(start, end, input);
    bool clear = hits == null || hits.Length == 0 || !hits[0].Hit ||
                 hits[0].ObjectID == target.UniqueID;
    state.LastThrowPathTargetID = target.UniqueID;
    state.LastThrowPathClear = clear;
    state.NextThrowPathCheckAt = now + THROW_PATH_CHECK_INTERVAL_MS;
    return clear;
}
bool ThrowCurrentWeapon(IPlayer bot)
{
    if (!TryBeginBotAction(bot, 700f, 3))
        return false;
    bot.AddCommand(new PlayerCommand(PlayerCommandType.EnterThrowingMode, 110));
    bot.AddCommand(new PlayerCommand(PlayerCommandType.Throw, 70));
    bot.AddCommand(new PlayerCommand(PlayerCommandType.Throw));
    return true;
}
int GetClipAmmo(IPlayer bot)
{
    if (bot.CurrentWeaponDrawn == WeaponItemType.Rifle)
        return bot.CurrentPrimaryWeapon.CurrentAmmo;
    if (bot.CurrentWeaponDrawn == WeaponItemType.Handgun)
        return bot.CurrentSecondaryWeapon.CurrentAmmo;
    return -1;
}
void ConsiderDroneTaunt(
    BotItemState state, IObjectStreetsweeper drone, string mode)
{
    if (state.DroneTauntObjectID == drone.UniqueID &&
        state.DroneTauntMode == mode)
        return;
    state.DroneTauntObjectID = drone.UniqueID;
    state.DroneTauntMode = mode;
    QueueBotTaunt(state, mode);
}
void QueueThrowableTaunt(BotItemState state, WeaponItem throwable)
{
    if (throwable == WeaponItem.GRENADES)
        QueueBotTaunt(state, "GRENADE_THROW");
    else if (throwable == WeaponItem.MOLOTOVS)
        QueueBotTaunt(state, "MOLOTOV_THROW");
    else if (throwable == WeaponItem.C4)
        QueueBotTaunt(state, "C4_THROW");
    else if (throwable == WeaponItem.SHURIKEN)
        QueueBotTaunt(state, "SHURIKEN_THROW");
    else if (throwable == WeaponItem.MINES)
        QueueBotTaunt(state, "MINE_THROW");
}
void QueueBotTaunt(BotItemState state, string category)
{
    if (state == null || !IsUsableBot(state.Bot) || state.TauntPending)
        return;
    if (PendingBotTaunts.Count >= MAX_PENDING_BOT_TAUNTS)
        return;
    float now = Game.TotalElapsedGameTime;
    if (now - state.LastTauntAt < BOT_TAUNT_COOLDOWN_MS ||
        Rng.Next(0, BOT_TAUNT_CHANCE) != 0)
        return;
    string line = GetBotTauntLine(category);
    if (String.IsNullOrEmpty(line))
        return;
    PendingBotTaunt pending = new PendingBotTaunt();
    pending.State = state;
    pending.Line = line;
    pending.FireAt = now + BOT_TAUNT_DELAY_MIN_MS +
        (float)Rng.NextDouble() *
        (BOT_TAUNT_DELAY_MAX_MS - BOT_TAUNT_DELAY_MIN_MS);
    PendingBotTaunts.Add(pending);
    state.TauntPending = true;
}
void UpdateBotTaunts()
{
    if (PendingBotTaunts.Count == 0)
        return;
    for (int i = PendingBotTaunts.Count - 1; i >= 0; i--)
    {
        PendingBotTaunt pending = PendingBotTaunts[i];
        if (pending != null && pending.State != null &&
            IsUsableBot(pending.State.Bot))
            continue;
        if (pending != null && pending.State != null)
            pending.State.TauntPending = false;
        PendingBotTaunts.RemoveAt(i);
    }
    if (PendingBotTaunts.Count == 0)
        return;
    float now = Game.TotalElapsedGameTime;
    if (now < NextGlobalTauntAt)
        return;
    int selectedIndex = -1;
    PendingBotTaunt selected = null;
    for (int i = 0; i < PendingBotTaunts.Count; i++)
    {
        PendingBotTaunt pending = PendingBotTaunts[i];
        if (pending == null || pending.FireAt > now)
            continue;
        if (selected == null || pending.FireAt < selected.FireAt)
        {
            selected = pending;
            selectedIndex = i;
        }
    }
    if (selected == null)
        return;
    PendingBotTaunts.RemoveAt(selectedIndex);
    BotItemState state = selected.State;
    if (state != null)
        state.TauntPending = false;
    if (state == null || !IsUsableBot(state.Bot))
        return;
    Game.CreateDialogue(
        selected.Line,
        Color.White,
        state.Bot,
        showInChat: false);
    state.LastTauntAt = now;
    NextGlobalTauntAt = now + BOT_TAUNT_GLOBAL_SPACING_MS;
}
string GetBotTauntLine(string category)
{
    string[] lines = null;
    if (category == "GRENADE_THROW")
        lines = new string[] { "Frag out!", "Grenade out!", "Fire in the hole!", "Catch this!" };
    else if (category == "MOLOTOV_THROW")
        lines = new string[] { "Molotov out!", "Light 'em up!", "Here's a hot one!", "Burning bottle!" };
    else if (category == "C4_THROW")
        lines = new string[] { "C4 out!", "Special delivery!", "Charge planted!", "Catch this charge!" };
    else if (category == "SHURIKEN_THROW")
        lines = new string[] { "Shuriken out!", "Catch this!", "Sharp delivery!", "Heads up!" };
    else if (category == "MINE_THROW")
        lines = new string[] { "Mine out!", "Watch your step!", "Leaving a surprise!", "Mind the floor!" };
    else if (category == "GRENADE_NEAR")
        lines = new string[] { "Get down, nade!", "Grenade! Move!", "Incoming frag!", "Nade, get clear!" };
    else if (category == "KICK_BACK")
        lines = new string[] { "Right back at you!", "Return to sender!", "You dropped this!", "Not today!" };
    else if (category == "DRONE_ATTACK")
        lines = new string[] { "Damn clanker!", "Scrap that thing!", "Take down the drone!", "Rust bucket!" };
    else if (category == "DRONE_THREAT")
        lines = new string[] { "Damn clanker!", "Clanker's on me!", "Need a weapon!", "That thing has me pinned!" };
    else if (category == "DRONE_ACTIVATE")
        lines = new string[] { "Drone's ours!", "Clanker online!", "Sweep the street!", "Cover us, drone!" };
    else if (category == "MINE_SPOTTED")
        lines = new string[] { "Mine!", "Watch the floor!", "Mind your step!", "There's a mine!" };
    else if (category == "FIRE")
        lines = new string[] { "I'm on fire!", "Too hot!", "Stop, drop, and roll!", "Get me out of this!" };
    else if (category == "CHAINSAW")
        lines = new string[] { "Let's cut this short!", "Time to carve!", "Buzz off!", "This'll get messy!" };
    else if (category == "PROP_THROW")
        lines = new string[] { "Catch!", "Think fast!", "Heads up!", "Special delivery!" };
    else if (category == "CATCH")
        lines = new string[] { "Nice try!", "Got it!", "Mine now!", "Too slow!" };
    if (lines == null || lines.Length == 0)
        return null;
    return lines[Rng.Next(0, lines.Length)];
}
void ConfigureSupplyCrates()
{
    float now = Game.TotalElapsedGameTime;
    if (now >= NextCrateHistoryPruneAt)
    {
        HashSet<int> liveCrateIDs = new HashSet<int>();
        foreach (IObjectSupplyCrate liveCrate in CachedSupplyCrates)
        {
            if (liveCrate != null && !liveCrate.IsRemoved &&
                !liveCrate.RemovalInitiated)
                liveCrateIDs.Add(liveCrate.UniqueID);
        }

        List<int> staleCrates = new List<int>();
        foreach (int id in ConfiguredSupplyCrateIDs)
            if (!liveCrateIDs.Contains(id))
                staleCrates.Add(id);
        foreach (int id in staleCrates)
            ConfiguredSupplyCrateIDs.Remove(id);
        NextCrateHistoryPruneAt = now + CRATE_HISTORY_PRUNE_INTERVAL_MS;
    }

    foreach (IObjectSupplyCrate crate in CachedSupplyCrates)
    {
        if (crate == null ||
            crate.IsRemoved ||
            crate.RemovalInitiated ||
            crate.DestructionInitiated ||
            !crate.Destructable ||
            crate.GetHealth() <= 0f ||
            crate.GetParachuteActive() ||
            ConfiguredSupplyCrateIDs.Contains(crate.UniqueID))
            continue;
        crate.SetTargetAIData(
            new ObjectAITargetData(
                CRATE_TARGET_RANGE,
                PlayerTeam.Independent,
                ObjectAITargetMode.MeleeOnly,
                CRATE_PRIORITY),
            true);
        ConfiguredSupplyCrateIDs.Add(crate.UniqueID);
    }
}
bool IsUsableBot(IPlayer bot)
{
    return bot != null &&
           !bot.IsRemoved &&
           !bot.IsDead && bot.IsBot && bot.IsInputEnabled &&
           !bot.IsDisabled;
}
void RefreshEnemyTarget(BotItemState state)
{
    float realNow = Game.TotalElapsedRealTime;
    IPlayer current = state.Target;
    bool valid = current != null && !current.IsRemoved && !current.IsDead &&
        IsEnemy(state.Bot, current) &&
        (current.UniqueID != state.IgnoredEnemyID ||
         realNow >= state.IgnoreEnemyUntilRealTime);
    if (state.CombatFocus != null && !state.CombatFocus.IsRemoved &&
        !state.CombatFocus.IsDead && IsEnemy(state.Bot, state.CombatFocus))
    {
        state.Target = state.CombatFocus;
        return;
    }
    if (valid && realNow < state.NextEnemyReviewAt)
        return;
    state.NextEnemyReviewAt = realNow + 1500f;
    Vector2 position = state.Bot.GetWorldPosition();
    // Prefer continuity unless a different enemy is substantially closer.
    float best = valid
        ? DistanceSquared(position, current.GetWorldPosition()) * 0.64f
        : float.MaxValue;
    state.Target = valid ? current : null;
    foreach (IPlayer candidate in CachedPlayers)
    {
        if (candidate == null || candidate.IsRemoved || candidate.IsDead ||
            candidate == state.Bot || !IsEnemy(state.Bot, candidate) ||
            (candidate.UniqueID == state.IgnoredEnemyID &&
             realNow < state.IgnoreEnemyUntilRealTime))
            continue;
        float distance = DistanceSquared(position, candidate.GetWorldPosition());
        if (distance < best)
        {
            best = distance;
            state.Target = candidate;
        }
    }
}
bool IsEnemy(IPlayer self, IPlayer other)
{
    return self.GetTeam() != other.GetTeam() ||
           self.GetTeam() == PlayerTeam.Independent;
}
void FaceTarget(IPlayer bot, IPlayer target)
{
    if (target == null || bot == null)
        return;
    int direction = target.GetWorldPosition().X >=
        bot.GetWorldPosition().X ? 1 : -1;
    bot.SetFaceDirection(direction);
    BotItemState state = FindState(bot);
    if (state != null)
        state.ActionFacingDirection = direction;
}
void FaceObject(IPlayer bot, IObject target)
{
    if (target == null || bot == null)
        return;
    int direction = target.GetWorldPosition().X >=
        bot.GetWorldPosition().X ? 1 : -1;
    bot.SetFaceDirection(direction);
    BotItemState state = FindState(bot);
    if (state != null)
        state.ActionFacingDirection = direction;
}
float DistanceSquared(Vector2 a, Vector2 b)
{
    float dx = a.X - b.X;
    float dy = a.Y - b.Y;
    return dx * dx + dy * dy;
}