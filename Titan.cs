// Titan Transformation Script
// When a player picks up strength boost, they transform into a Titan with enhanced abilities
// Titan Types: Pure Titan (50% chance) and Attack Titan (50% chance)

// Configuration: Temporary player name (players with this name are not stored and have special power extract rules)
private const string TEMPORARY_PLAYER_NAME = "com";

// Titan type enum
private enum TitanType
{
    Pure,
    Attack,
    Jaw,
    Armored,
    Cart
}

// Attack Titan hardening mode enum
private enum HardeningMode
{
    Neutral,
    Offensive,
    Defensive
}

// Configuration variables
private const float TRANSFORM_EXPLOSION_RADIUS = 30f;
private const float FALL_IMPACT_RADIUS = 23f;

// Pure Titan stats
private const float PURE_TITAN_HP_BONUS = 0.15f; // 15% HP increase
private const float PURE_TITAN_MELEE_DAMAGE_BONUS = 0.10f; // 10% melee damage increase
private const float PURE_TITAN_MELEE_FORCE_BONUS = 0.25f; // 25% melee force increase
private const float PURE_TITAN_SPEED_PENALTY = 0.30f; // 30% speed reduction (0.7x speed)

// Attack Titan stats
private const float ATTACK_TITAN_HP_BONUS = 0.40f; // 40% HP increase
private const float ATTACK_TITAN_MELEE_DAMAGE_BONUS = 0.20f; // 20% melee damage increase
private const float ATTACK_TITAN_MELEE_FORCE_BONUS = 0.01f; // 1% melee force increase
private const float ATTACK_TITAN_SPEED_PENALTY = 0.20f; // 20% speed reduction
private const float ATTACK_TITAN_ENERGY_BONUS = 0.30f; // 30% energy increase

// Attack Titan hardening costs and bonuses
private const float HARDENING_ENERGY_COST_PER_SECOND = 0.14f; // 14% of max energy per second
private const float HARDENING_OFFENSIVE_DAMAGE_BONUS = 0.35f; // 35% melee damage increase
private const float HARDENING_DEFENSIVE_SPEED_PENALTY = 0.20f; // 20% speed reduction
private const float HARDENING_DEFENSIVE_DEFENSE_BONUS = 0.75f; // 75% damage reduction
private const float HARDENING_OFFENSIVE_TRIGGER_DISTANCE = 25f; // Distance to trigger offensive mode
private const float HARDENING_DEFENSIVE_CHECK_INTERVAL = 2000f; // Check every 2 seconds
private const float HARDENING_OFFENSIVE_MELEE_TIMEOUT = 2000f; // 2 seconds without melee = back to neutral
private const float HARDENING_LOW_ENERGY_THRESHOLD = 0.30f; // 30% energy threshold
private const float HARDENING_AVOID_TRIGGER_CHANCE = 0.80f; // 80% chance to avoid when low energy
private const float HARDENING_SWITCH_TO_NEUTRAL_CHANCE = 0.60f; // 60% chance to switch back when low energy
private const float AIM_DETECTION_RANGE = 800f; // Range for raycast aim detection

// Attack Titan super punch ability
private const int ATTACK_SUPER_PUNCH_COUNT = 3; // 3rd punch is super punch (always staggers)
private const int ATTACK_SUPER_PUNCH_WINDOW = 600; // 600ms window between punches

// Attack Titan slowmo ability
private const float SLOWMO_ENERGY_COST = 0.30f; // 30% of max energy
private const float SLOWMO_ACTIVATION_DELAY = 1000f; // 1 seconds before ending slowmo

// Jaw Titan stats
private const float JAW_TITAN_HP_BONUS = 0.0f; // 0% HP increase
private const float JAW_TITAN_MELEE_DAMAGE_BONUS = 0.25f; // 25% melee damage increase
private const float JAW_TITAN_MELEE_FORCE_BONUS = 0.0f; // 0% melee force increase
private const float JAW_TITAN_SPEED_PENALTY = 0.0f; // 0% speed reduction
private const float JAW_TITAN_ENERGY_BONUS = 0.10f; // 10% energy increase
private const float JAW_TITAN_SIZE = 0.880f; // Fixed size

// Jaw Titan claw ability
private const float JAW_CLAW_BASE_DAMAGE = 7f;
private const float JAW_CLAW_FRONT_MULTIPLIER = 2f; // Front attack multiplier
private const float JAW_CLAW_BACK_MULTIPLIER = 4f; // Backstab multiplier
private const float JAW_CLAW_GIB_THRESHOLD = 0.30f; // 30% overkill = gib

// Jaw Titan hunt ability
private const float JAW_HUNT_VICTIM_STUN_DURATION = 0f; // 0 seconds input disable after hunt

// Jaw Titan bot backstab targeting
private const float JAW_BOT_BACKSTAB_COMBAT_RANGE = 50f;   // Range for in-combat backstab check
private const float JAW_BOT_COMBAT_TIMEOUT_MS = 3000f;     // Time with no melee before considered out-of-combat
private const float JAW_BOT_TARGETING_INTERVAL_MS = 300f;  // How often to re-evaluate target

// Jaw Titan sharp claw ability
private const int JAW_SHARP_CLAW_PUNCH_COUNT = 3; // 3rd punch breaks objects
private const int JAW_SHARP_CLAW_PUNCH_WINDOW = 600; // 600ms window between punches

// Armored Titan stats
private const float ARMORED_TITAN_HP_BONUS = 0.15f; // 15% HP increase
private const float ARMORED_TITAN_MELEE_DAMAGE_BONUS = 0.23f; // 23% melee damage increase
private const float ARMORED_TITAN_MELEE_FORCE_BONUS = 0.60f; // 60% melee force increase
private const float ARMORED_TITAN_SPEED_PENALTY = 0.50f; // 50% speed reduction
private const float ARMORED_TITAN_ENERGY_BONUS = 0f; // 0% energy increase
private const float ARMORED_TITAN_SIZE = 1.300f; // Fixed size

// Armored Titan armor mode bonuses (when OFF)
private const float ARMORED_ARMOR_OFF_SPEED_BONUS = 0.30f; // 30% speed increase
private const float ARMORED_ARMOR_OFF_ENERGY_REGEN_BONUS = 0.15f; // 15% energy regen increase

// Armored Titan armor mode penalties (when ON)
private const float ARMORED_ARMOR_ON_ENERGY_REGEN_PENALTY = 0.50f; // 50% energy regen reduction while armor is active

// Armored Titan armor mode triggers for bots
private const float ARMORED_ARMOR_ON_TRIGGER_DISTANCE = 35f; // Distance to trigger armor on
private const float ARMORED_ARMOR_ON_CHECK_INTERVAL = 1000f; // Check every 1 second

// Armored Titan rush ability
private const float ARMORED_RUSH_DAMAGE_BASE = 7f;
private const float ARMORED_RUSH_TARGET_FORCE_WEIGHT = 0.5f; // Target force multiplier
private const float ARMORED_RUSH_BALANCER_MOD = 0.02f; // balance stab
private const float ARMORED_RUSH_THROW_SCALE = 300f; // Velocity scaling
private const float ARMORED_RUSH_THROW_UP_RATIO = 0.6f; // Upward component
private const float ARMORED_RUSH_HIT_COOLDOWN = 800f; // Cooldown per target in ms

// Armored Titan bot rush triggers
private const float ARMORED_BOT_RUSH_ENERGY_THRESHOLD = 0.80f; // 80% energy to trigger rush
private const float ARMORED_BOT_RUSH_ABORT_ENERGY = 0.20f; // 20% energy to abort rush
private const float ARMORED_BOT_RUSH_CHECK_DISTANCE = 40f; // Distance to check for enemies
private const int ARMORED_BOT_RUSH_MIN_ENEMIES = 2; // Minimum enemies to trigger rush

// Armored Titan super punch ability
private const int ARMORED_SUPER_PUNCH_COUNT = 3; // 3rd punch is super punch
private const int ARMORED_SUPER_PUNCH_WINDOW = 600; // 600ms window between punches
private const float ARMORED_SUPER_PUNCH_BALANCER_MOD = 0.015f; // balance stab

// Armored Titan armor reflection constants
private const float ARMORED_MELEE_RANGE = 30f; // Melee threat detection range
private const float ARMORED_MELEE_Y_TOLERANCE = 24f; // Vertical tolerance for melee
private const float ARMORED_PROJ_LOOKAHEAD_SEC = 0.06f; // Projectile lookahead time
private const float ARMORED_REFLECT_OFFSET = 6f; // Reflected projectile spawn offset
private const float ARMORED_SHIELD_ENERGY_DRAIN_RATIO = 0.50f; // 50% of blocked damage drained from energy

// Cart Titan stats
private const float CART_TITAN_HP_BONUS = -0.40f;           // -40% HP
private const float CART_TITAN_MELEE_DAMAGE_BONUS = 0.0f;   // +0% melee damage
private const float CART_TITAN_MELEE_FORCE_BONUS = 0.0f;    // +0% melee force
private const float CART_TITAN_SPEED_BONUS = 0.30f;         // +30% speed
private const float CART_TITAN_ENERGY_BONUS = 2.0f;         // +200% energy
private const float CART_TITAN_SIZE = 0.920f;               // Fixed size

// Cart Titan Offensive form stat modifiers (applied on top of base CRT)
private const float CART_OFFENSIVE_SPEED_PENALTY = 0.40f;   // -40% speed
private const float CART_OFFENSIVE_HP_BONUS = 0.40f;        // +40% HP

// Cart Titan rescue ally mechanics
private const float CART_RESCUE_LIFT_COST_PER_SEC = 0.005f; // 0.5% of max energy per second
private const float CART_RESCUE_ALLY_DMG_REDUCTION = 0.50f; // 50% all damage reduction while lifted

// Cart Titan uphead gun energy costs
private const float CART_GUN_ENERGY_PER_BULLET_M60 = 0.015f;    // 1.5% per bullet
private const float CART_GUN_ENERGY_PER_BULLET_SNIPER = 0.06f;  // 6% per bullet

// Cart Titan uphead gun attach offset (above titan head)
private const float CART_GUN_OFFSET_Y = 18f;

// Cart Titan aim distance from gun (crosshair distance)
private const float CART_GUN_AIM_DISTANCE = 30f;

// Cart Titan gun pipe (BgRailing02A) default rotation offset in radians (adjustable)
private const float CART_GUN_PIPE_DEFAULT_ROTATION = 7.9f;

// Cart Titan aim crosshair update interval
private const float CART_AIM_UPDATE_INTERVAL = 50f; // 50ms

// Titan size range
private const float TITAN_MIN_SIZE = 0.8f;
private const float TITAN_MAX_SIZE = 1.25f;

// Size-based stat scaling (0.3% per 0.01 size above 0.8)
private const float SIZE_STAT_BONUS_PER_0_01 = 0.003f; // 0.3% = 0.003

// Size-based speed penalty (2% per 0.01 size above 0.8)
private const float SIZE_SPEED_PENALTY_PER_0_01 = 0.02f; // 2% = 0.02

// Grab damage constants
private const float GRAB_DAMAGE_BASE = 7f;
private const float GRAB_DAMAGE_INTERVAL = 50f; // 50ms in milliseconds
private const float GRAB_DAMAGE_MULTIPLIER = 0.15f; // Damage multiplier
private const float GRAB_BLOOD_EFFECT_INTERVAL = 1000f; // Blood effect every 1 second

// Track titan players
private Dictionary<int, TitanType> titanPlayers = new Dictionary<int, TitanType>();
private Dictionary<int, IProfile> originalProfiles = new Dictionary<int, IProfile>();
private Dictionary<int, PlayerModifiers> originalModifiers = new Dictionary<int, PlayerModifiers>();
private Dictionary<int, BotBehavior> originalBotBehaviors = new Dictionary<int, BotBehavior>();
private Dictionary<int, BotBehaviorSet> originalBotBehaviorSets = new Dictionary<int, BotBehaviorSet>();

// Track Attack Titan specific data
private Dictionary<int, HardeningMode> attackTitanHardeningMode = new Dictionary<int, HardeningMode>();
private Dictionary<int, float> attackTitanHardeningEnergyCost = new Dictionary<int, float>();
private Dictionary<int, float> attackTitanLastMeleeTime = new Dictionary<int, float>();
private Dictionary<int, float> attackTitanLastDefensiveCheck = new Dictionary<int, float>();
private Dictionary<int, float> attackTitanSlowmoEndTime = new Dictionary<int, float>();
private Dictionary<int, int> attackTitanPunchCount = new Dictionary<int, int>(); // Track punch count for super punch
private Dictionary<int, float> attackTitanLastPunchTime = new Dictionary<int, float>(); // Track last punch time

// Track player ground state for fall detection
private Dictionary<int, bool> playerWasOnGround = new Dictionary<int, bool>();

// Track player strength boost state to detect when they pick it up
private Dictionary<int, bool> playerHadStrengthBoost = new Dictionary<int, bool>();

// Track titans that are exhausted (out of energy)
private Dictionary<int, bool> titanExhausted = new Dictionary<int, bool>();

// Track when exhausted titans should recover
private Dictionary<int, float> titanExhaustionRecoverTime = new Dictionary<int, float>();

// Exhaustion duration constant
private const float TITAN_EXHAUSTION_DURATION = 3000f; // 3 seconds in milliseconds

// Track titan sizes for stat calculations
private Dictionary<int, float> titanSizes = new Dictionary<int, float>();

// Track grab damage for titans
private Dictionary<int, GrabDamageData> titanGrabDamage = new Dictionary<int, GrabDamageData>();

// Track titans being grabbed (to manage input disable/enable for attackers)
private Dictionary<int, bool> titanInputDisabled = new Dictionary<int, bool>();

// Track victims gibbed by Pure Titan grab (for power extraction)
private Dictionary<int, int> gibbedByPureTitan = new Dictionary<int, int>(); // victimId -> pureTitanId

// Track Jaw Titan specific data
private Dictionary<int, int> jawTitanPunchCount = new Dictionary<int, int>(); // Track punch count for sharp claw
private Dictionary<int, float> jawTitanLastPunchTime = new Dictionary<int, float>(); // Track last punch time
private Dictionary<int, IPlayer> jawTitanHuntVictim = new Dictionary<int, IPlayer>(); // Track hunt victims (dive catches)
private Dictionary<int, float> jawTitanHuntDamageTime = new Dictionary<int, float>(); // Track hunt damage timing
private Dictionary<int, float> jawTitanHuntBloodEffectTime = new Dictionary<int, float>(); // Track hunt blood effect timing
private Dictionary<int, float> jawHuntVictimReenableTime = new Dictionary<int, float>(); // Track when to re-enable victim input
private Dictionary<int, float> jawBotLastCombatTime = new Dictionary<int, float>(); // Track last combat time for bot targeting
private Dictionary<int, IPlayer> jawBotCurrentTarget = new Dictionary<int, IPlayer>(); // Current forced bot target
private Dictionary<int, float> jawBotLastTargetCheck = new Dictionary<int, float>(); // Rate-limit target evaluations

// Track Armored Titan specific data
private Dictionary<int, bool> armoredTitanArmorOn = new Dictionary<int, bool>(); // Track armor state (true = on, false = off)
private Dictionary<int, float> armoredTitanLastArmorCheck = new Dictionary<int, float>(); // Track last armor check time for bots
private Dictionary<int, Dictionary<int, float>> armoredTitanRushHitTimes = new Dictionary<int, Dictionary<int, float>>(); // Track rush hit times per target
private Dictionary<int, bool> armoredTitanIsRushing = new Dictionary<int, bool>(); // Track if bot is currently rushing
private Dictionary<int, int> armoredTitanPunchCount = new Dictionary<int, int>(); // Track punch count for super punch
private Dictionary<int, float> armoredTitanLastPunchTime = new Dictionary<int, float>(); // Track last punch time

// Track player facing directions for Jaw Titan backstab detection
private Dictionary<int, int> playerFacingDirections = new Dictionary<int, int>();

// Track Cart Titan specific data
private enum CartTitanForm { Rescue, Offensive }
private Dictionary<int, CartTitanForm> cartTitanForm = new Dictionary<int, CartTitanForm>();
private Dictionary<int, IObject> cartTitanBackObject = new Dictionary<int, IObject>(); // BgFence01G (Rescue) or drone (Offensive)
private Dictionary<int, IPlayer> cartTitanLiftedAlly = new Dictionary<int, IPlayer>(); // Ally on backpack (Rescue)
private Dictionary<int, float> cartTitanLiftNextCostTime = new Dictionary<int, float>(); // Next energy drain tick for lift
private Dictionary<int, IObject> cartTitanGunPipe = new Dictionary<int, IObject>(); // BgRailing02A gun pipe (Offensive)
private Dictionary<int, bool> cartTitanGunIsSniper = new Dictionary<int, bool>(); // true=Sniper, false=M60
private Dictionary<int, bool> cartTitanBlockHeld = new Dictionary<int, bool>(); // Is block currently held?
private Dictionary<int, float> cartTitanGunAimAngle = new Dictionary<int, float>(); // Stored aim angle offset in radians (relative to forward)
private Dictionary<int, IObjectText> cartTitanCrosshair = new Dictionary<int, IObjectText>(); // '+' crosshair text
private Dictionary<int, float> cartTitanLastBulletFireTime = new Dictionary<int, float>(); // Throttle bullet fire & energy cost

// Track assigned titan powers (does not mean transformed, just assigned power)
private Dictionary<int, TitanType> assignedTitanPowers = new Dictionary<int, TitanType>();

// Track player name to ID mapping for storage/restoration
private Dictionary<string, int> playerNameToId = new Dictionary<string, int>();
private Dictionary<int, string> playerIdToName = new Dictionary<int, string>();

// Distribution mode: "A" = VIP scenario (permanent, 2-day expiry), "B" = normal (30-min expiry)
private string distributionMode = "";

// VIP player names that trigger Scenario A
private static readonly string[] VIP_PLAYER_NAMES = new string[] { "Pakhi", "Xray", "Kokola", "Bichi" };

// Per-player expiry unix-day (Scenario A) or unix-minute (Scenario B)
// Key = player name, Value = expiry timestamp
private Dictionary<string, long> playerPowerExpiry = new Dictionary<string, long>();

// Last time we checked expiry (every 30s is fine)
private float lastExpiryCheckTime = 0f;
private const float EXPIRY_CHECK_INTERVAL = 30000f; // 30 seconds

// Track titan type labels above players' heads
private Dictionary<int, IObjectText> titanTypeLabels = new Dictionary<int, IObjectText>();

// Track bots whose PowerupUsage has been set to false
private HashSet<int> configuredBotPowerupUsage = new HashSet<int>();

// Track players who received startup strength boost
private HashSet<int> givenStartupStrengthBoost = new HashSet<int>();

// Track bot strength boost activation check times
private Dictionary<int, float> botLastStrengthBoostCheck = new Dictionary<int, float>();
private const float BOT_STRENGTH_BOOST_CHECK_INTERVAL = 1000f; // 1 second check interval

// Detach (detransform) tracking
private Dictionary<int, float> botLastDetachCheck = new Dictionary<int, float>();
private const float BOT_DETACH_CHECK_INTERVAL = 1000f; // 1 second check interval
private const float BOT_DETACH_HP_HIGH = 0.15f; // 15% HP = 20% detach chance
private const float BOT_DETACH_HP_LOW = 0.05f; // 5% HP = 90% detach chance
private const float BOT_DETACH_CHANCE_MIN = 0.20f; // 20% chance at 15% HP
private const float BOT_DETACH_CHANCE_MAX = 0.90f; // 90% chance at 5% HP
private Dictionary<int, float> walkKeyPressStartTime = new Dictionary<int, float>();
private const float HUMAN_DETACH_HOLD_TIME = 800f; // 800ms hold walk key to detach
private const uint DETACH_SPAWN_DELAY_MS = 1500; // 1500ms delay between killing titan body and spawning human

// Zombie spawn trigger for detach animation
private IObjectPlayerSpawnTrigger zombieSpawnTrigger = null;

// Helper class for grab damage tracking
private class GrabDamageData
{
    public IPlayer Victim;
    public float NextDamageTime;
    public float TitanMeleeDamage;
    public float NextBloodEffectTime; // Track blood effect timing separately
}

// Random generator
private Random rnd = new Random();

public void OnStartup()
{
    // Initialize shared storage for cooperation with odm.cs script
    IScriptStorage storage = Game.GetSharedStorage("TitanODMSync");
    storage.SetItem("TITAN_PLAYERS", ""); // Initialize with empty string
    
    // Create zombie spawn trigger once for detach animation
    zombieSpawnTrigger = (IObjectPlayerSpawnTrigger)Game.CreateObject("PlayerSpawnTrigger");
    if (zombieSpawnTrigger != null)
    {
        zombieSpawnTrigger.SetActivateOnStartup(false);
        zombieSpawnTrigger.SetEnabled(true);
        zombieSpawnTrigger.SetPredefinedAI(PredefinedAIType.ZombieA);
        zombieSpawnTrigger.SetSpawnWeaponItemRifle(WeaponItem.NONE);
        zombieSpawnTrigger.SetSpawnWeaponItemHandgun(WeaponItem.NONE);
        zombieSpawnTrigger.SetSpawnWeaponItemMelee(WeaponItem.NONE);
        zombieSpawnTrigger.SetSpawnWeaponItemThrown(WeaponItem.NONE);
        zombieSpawnTrigger.SetSpawnWeaponItemPowerup(WeaponItem.NONE);
    }
    
    // Set up update callback to detect strength boost pickups and falls
    Events.UpdateCallback.Start(OnUpdate, 50); // Check every 50ms
    
    // Set up key input callback for Attack Titan abilities
    Events.PlayerKeyInputCallback.Start(OnPlayerKeyInput);
    
    // Set up melee action callback for Attack Titan hardening and Jaw Titan abilities
    Events.PlayerMeleeActionCallback.Start(OnPlayerMeleeAction);
    
    // Set up player death callback for Pure Titan power extract ability
    Events.PlayerDeathCallback.Start(OnPlayerDeath);
    
    // Set up projectile hit callback for Armored Titan reflection
    Events.ProjectileHitCallback.Start(OnProjectileHit);
    
    // Set up player damage callback for Armored Titan safety net
    Events.PlayerDamageCallback.Start(OnPlayerDamage);
    
    // Delay initialization until all players are spawned (500ms wait for other scripts)
    IObjectTimerTrigger initTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    initTimer.SetIntervalTime(500); // 500ms delay to wait for all players
    initTimer.SetRepeatCount(1); // Run once
    initTimer.SetScriptMethod("InitializeTitanDistribution");
    initTimer.Trigger();

    // strength boost
    const float OTHERS_SCALE = 0.7f; // 1.0 = unchanged, 0.2 = others are 5x rarer
    short boost = (short)WeaponItem.STRENGTHBOOST;

    Dictionary<short, int> current = Game.GetWeaponSpawnChances();
    Dictionary<short, int> chances = new Dictionary<short, int>();

    foreach (KeyValuePair<short, int> kv in current)
    {
        if (kv.Key == boost || kv.Value <= 0) continue; // skip boost and disabled items
        int scaled = (int)Math.Round(kv.Value * OTHERS_SCALE);
        chances.Add(kv.Key, Math.Max(1, scaled)); // keep at least 1 so nothing disappears
    }

    chances.Add(boost, 100);
    Game.UpdateWeaponSpawnChances(chances);
}

public void InitializeTitanDistribution(TriggerArgs args)
{
    IPlayer[] allPlayers = Game.GetPlayers();
    
    // Build player name to ID mapping
    playerNameToId.Clear();
    playerIdToName.Clear();
    foreach (IPlayer p in allPlayers)
    {
        if (p != null)
        {
            playerNameToId[p.Name] = p.UniqueID;
            playerIdToName[p.UniqueID] = p.Name;
        }
    }

    // Detect which scenario to use
    bool hasVip = false;
    foreach (IPlayer p in allPlayers)
    {
        if (p == null) continue;
        foreach (string vip in VIP_PLAYER_NAMES)
        {
            if (p.Name.Equals(vip, System.StringComparison.OrdinalIgnoreCase))
            {
                hasVip = true;
                break;
            }
        }
        if (hasVip) break;
    }
    
    distributionMode = hasVip ? "A" : "B";
    Game.ShowChatMessage("[Titan] Mode " + distributionMode, Color.Yellow);
    
    if (distributionMode == "A")
    {
        // Scenario A: permanent per-player powers.
        bool hasStoredPowers = RestoreAssignedPowersFromStorage();
        LoadPlayerExpiryFromStorage();
        
        if (hasStoredPowers && !HasDuplicateHumanoids(allPlayers))
        {
            Game.ShowChatMessage("[Titan] Restoring permanent distribution (Scenario A)...", Color.Green);
            // Check if any restored power expired while the game was away
            CheckScenarioAExpiry();
        }
        else
        {
            Game.ShowChatMessage("[Titan] Distributing powers (Scenario A)...", Color.Green);
            DistributeTitanPowers(args);
        }
    }
    else
    {
        // Scenario B: expires after 30 minutes per player.
        RestoreAssignedPowersFromStorage();
        LoadPlayerExpiryFromStorage();
        
        // Handles existing powers, removes expired, and redistributes only missing humanoids
        HandleScenarioBDistribution(allPlayers);
    }
    
    // Create labels (with expiry info for Scenario A)
    CreateTitanTypeLabels(allPlayers);
    
    // Give strength boost to all humanoid titan owners for this match
    GiveStrengthBoostToHumanoidTitans();
}

private void GiveStrengthBoostToHumanoidTitans()
{
    IPlayer[] allPlayers = Game.GetPlayers();
    foreach (IPlayer player in allPlayers)
    {
        if (player == null || player.IsDead) continue;
        
        int playerId = player.UniqueID;
        if (assignedTitanPowers.ContainsKey(playerId))
        {
            TitanType titanType = assignedTitanPowers[playerId];
            if (titanType != TitanType.Pure && !givenStartupStrengthBoost.Contains(playerId))
            {
                player.GiveWeaponItem(WeaponItem.STRENGTHBOOST);
                givenStartupStrengthBoost.Add(playerId);
            }
        }
    }
}

// Helper method to get today's date as a string (YYYY-MM-DD format)
private string GetTodayDateString()
{
    DateTime now = DateTime.Now;
    return now.Year.ToString() + "-" + 
           now.Month.ToString("00") + "-" + 
           now.Day.ToString("00");
}

// Returns a human-readable power name for chat/popup messages
private string GetTitanPowerName(TitanType titanType)
{
    if (titanType == TitanType.Attack)  return "Attack Titan";
    if (titanType == TitanType.Jaw)     return "Jaw Titan";
    if (titanType == TitanType.Armored) return "Armored Titan";
    if (titanType == TitanType.Cart)    return "Cart Titan";
    return "Pure Titan";
}

// Returns current UTC time as unix day number (days since epoch)
private long GetUnixDay()
{
    DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    return (long)(DateTime.UtcNow - epoch).TotalDays;
}

// Returns current UTC time as unix minute number (minutes since epoch)
private long GetUnixMinute()
{
    DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    return (long)(DateTime.UtcNow - epoch).TotalMinutes;
}

// Returns days remaining until expiry unix-day (0 = expired or same day)
private int DaysUntilExpiry(long expiryUnixDay)
{
    long now = GetUnixDay();
    long diff = expiryUnixDay - now;
    return (int)Math.Max(0, diff);
}

// --- Per-Player Storage Helpers (No Group Key) ---
private IScriptStorage GetCurrentStorage()
{
    string storageName = (distributionMode == "A") ? "TitanPowerStorageA" : "TitanPowerStorageB";
    return Game.GetSharedStorage(storageName);
}

private bool TryGetStoredPlayerPower(string playerName, out TitanType power)
{
    power = TitanType.Pure;
    if (string.IsNullOrEmpty(playerName)) return false;
    IScriptStorage storage = GetCurrentStorage();
    string key = "POWER_" + playerName;
    if (!storage.ContainsKey(key)) return false;
    
    string val = "";
    if (storage.TryGetItemString(key, out val) && !string.IsNullOrEmpty(val))
    {
        int intVal;
        if (int.TryParse(val, out intVal) && intVal > 0)
        {
            power = (TitanType)intVal;
            return true;
        }
    }
    return false;
}

private void SetStoredPlayerPower(string playerName, TitanType power)
{
    if (string.IsNullOrEmpty(playerName) || playerName.ToLower() == TEMPORARY_PLAYER_NAME.ToLower()) return;
    IScriptStorage storage = GetCurrentStorage();
    string key = "POWER_" + playerName;
    if (power == TitanType.Pure)
    {
        // Don't store pure titans (0); clear key if it previously existed
        if (storage.ContainsKey(key))
            storage.SetItem(key, "");
    }
    else
    {
        storage.SetItem(key, ((int)power).ToString());
    }
}

private bool TryGetStoredPlayerExpiry(string playerName, out long expiryTime)
{
    expiryTime = 0;
    if (string.IsNullOrEmpty(playerName)) return false;
    IScriptStorage storage = GetCurrentStorage();
    string key = "EXPIRY_" + playerName;
    if (!storage.ContainsKey(key)) return false;
    
    string val = "";
    if (storage.TryGetItemString(key, out val) && !string.IsNullOrEmpty(val))
    {
        return long.TryParse(val, out expiryTime);
    }
    return false;
}

private void SetStoredPlayerExpiry(string playerName, long expiryTime)
{
    if (string.IsNullOrEmpty(playerName) || playerName.ToLower() == TEMPORARY_PLAYER_NAME.ToLower()) return;
    IScriptStorage storage = GetCurrentStorage();
    storage.SetItem("EXPIRY_" + playerName, expiryTime.ToString());
}

private void RemoveStoredPlayerExpiry(string playerName)
{
    if (string.IsNullOrEmpty(playerName)) return;
    IScriptStorage storage = GetCurrentStorage();
    string key = "EXPIRY_" + playerName;
    storage.SetItem(key, "");
}

public void DistributeTitanPowers(TriggerArgs args)
{
    IPlayer[] allPlayers = Game.GetPlayers();
    
    if (allPlayers.Length == 0)
    {
        Game.ShowChatMessage("[Titan] No players found!", Color.Red);
        return;
    }
    
    assignedTitanPowers.Clear();
    playerPowerExpiry.Clear();
    
    if (distributionMode == "A")
        DistributeScenarioA(allPlayers);
    else
        DistributeScenarioB(allPlayers);
    
    // Save to per-player storage
    SaveAssignedPowersToStorage();
    SaveExpiryToStorage();
    
    Game.ShowChatMessage("[Titan] Powers distributed! Pick up strength boost to transform.", Color.Cyan);
}

// --- Scenario A: VIP present. 50% humanoid per player, all 4 types guaranteed, NO DUPLICATION, 2-day expiry ---
private void DistributeScenarioA(IPlayer[] allPlayers)
{
    List<TitanType> humanoidPool = new List<TitanType> { TitanType.Attack, TitanType.Jaw, TitanType.Armored, TitanType.Cart };
    
    // Shuffle humanoid pool
    for (int i = humanoidPool.Count - 1; i > 0; i--)
    {
        int j = rnd.Next(0, i + 1);
        TitanType tmp = humanoidPool[i]; humanoidPool[i] = humanoidPool[j]; humanoidPool[j] = tmp;
    }
    
    List<IPlayer> eligiblePlayers = new List<IPlayer>();
    foreach (IPlayer p in allPlayers)
    {
        if (p != null && p.Name.ToLower() != TEMPORARY_PLAYER_NAME.ToLower())
            eligiblePlayers.Add(p);
    }
    
    int targetHumanoidCount = Math.Min(humanoidPool.Count, eligiblePlayers.Count);
    
    // First pass: 50% chance per player
    List<IPlayer> humanoidCandidates = new List<IPlayer>();
    List<IPlayer> pureCandidates = new List<IPlayer>();
    
    foreach (IPlayer player in eligiblePlayers)
    {
        if (rnd.Next(0, 2) == 0)
            humanoidCandidates.Add(player);
        else
            pureCandidates.Add(player);
    }
    
    // Shuffle both candidate pools
    for (int i = humanoidCandidates.Count - 1; i > 0; i--)
    {
        int j = rnd.Next(0, i + 1);
        IPlayer tmp = humanoidCandidates[i]; humanoidCandidates[i] = humanoidCandidates[j]; humanoidCandidates[j] = tmp;
    }
    for (int i = pureCandidates.Count - 1; i > 0; i--)
    {
        int j = rnd.Next(0, i + 1);
        IPlayer tmp = pureCandidates[i]; pureCandidates[i] = pureCandidates[j]; pureCandidates[j] = tmp;
    }
    
    // Ensure all 3 humanoid types are covered: pull from pure candidates if not enough won the 50% roll
    while (humanoidCandidates.Count < targetHumanoidCount && pureCandidates.Count > 0)
    {
        IPlayer p = pureCandidates[0];
        pureCandidates.RemoveAt(0);
        humanoidCandidates.Add(p);
    }
    
    // Ensure NO DUPLICATION for Case A: demote excess candidates if more than 3 won the 50% roll
    while (humanoidCandidates.Count > targetHumanoidCount)
    {
        IPlayer p = humanoidCandidates[humanoidCandidates.Count - 1];
        humanoidCandidates.RemoveAt(humanoidCandidates.Count - 1);
        pureCandidates.Add(p);
    }
    
    // Exactly one unique humanoid type per candidate (no duplicates)
    for (int i = 0; i < humanoidCandidates.Count; i++)
    {
        int pid = humanoidCandidates[i].UniqueID;
        assignedTitanPowers[pid] = humanoidPool[i];
    }
    
    // All other players are Pure Titans (do not store pure titans in storage)
    foreach (IPlayer p in pureCandidates)
    {
        assignedTitanPowers[p.UniqueID] = TitanType.Pure;
        playerPowerExpiry.Remove(p.Name);
        SetStoredPlayerPower(p.Name, TitanType.Pure);
        RemoveStoredPlayerExpiry(p.Name);
    }
    
    // Assign 2-day expiry to all humanoid players
    long expiryDay = GetUnixDay() + 2;
    foreach (IPlayer p in humanoidCandidates)
    {
        playerPowerExpiry[p.Name] = expiryDay;
    }
    
    // Save expiry to storage
    SaveExpiryToStorage();
}

// --- Scenario B: No VIP. Player 1+2 get humanoid, rest get remaining types, 30-min expiry ---
// Preserves existing valid humanoids and only redistributes missing ones
private void DistributeScenarioB(IPlayer[] allPlayers)
{
    HandleScenarioBDistribution(allPlayers);
}

private void HandleScenarioBDistribution(IPlayer[] allPlayers)
{
    List<IPlayer> eligible = new List<IPlayer>();
    foreach (IPlayer p in allPlayers)
    {
        if (p != null && p.Name.ToLower() != TEMPORARY_PLAYER_NAME.ToLower())
            eligible.Add(p);
    }
    
    if (eligible.Count == 0) return;
    
    long nowMinute = GetUnixMinute();
    
    // 1. Remove expired humanoid powers
    foreach (IPlayer p in eligible)
    {
        int pid = p.UniqueID;
        if (assignedTitanPowers.ContainsKey(pid) && assignedTitanPowers[pid] != TitanType.Pure)
        {
            long expMinute;
            if (!playerPowerExpiry.TryGetValue(p.Name, out expMinute) || nowMinute >= expMinute)
            {
                // Expired: revert to pure and clear storage
                assignedTitanPowers[pid] = TitanType.Pure;
                playerPowerExpiry.Remove(p.Name);
                SetStoredPlayerPower(p.Name, TitanType.Pure);
                RemoveStoredPlayerExpiry(p.Name);
            }
        }
    }
    
    // 2. Resolve duplicates among present players (if any)
    Dictionary<TitanType, IPlayer> heldOwners = new Dictionary<TitanType, IPlayer>();
    foreach (IPlayer p in eligible)
    {
        int pid = p.UniqueID;
        if (!assignedTitanPowers.ContainsKey(pid))
        {
            assignedTitanPowers[pid] = TitanType.Pure;
            continue;
        }
        
        TitanType power = assignedTitanPowers[pid];
        if (power == TitanType.Pure) continue;
        
        if (heldOwners.ContainsKey(power))
        {
            // Duplicate detected among present players: demote this player to Pure
            assignedTitanPowers[pid] = TitanType.Pure;
            playerPowerExpiry.Remove(p.Name);
            SetStoredPlayerPower(p.Name, TitanType.Pure);
            RemoveStoredPlayerExpiry(p.Name);
        }
        else
        {
            heldOwners[power] = p;
        }
    }
    
    // 3. Find which humanoid titans are MISSING among present players
    List<TitanType> allHumanoidTypes = new List<TitanType> { TitanType.Attack, TitanType.Jaw, TitanType.Armored, TitanType.Cart };
    List<TitanType> missingHumanoids = new List<TitanType>();
    foreach (TitanType t in allHumanoidTypes)
    {
        if (!heldOwners.ContainsKey(t))
            missingHumanoids.Add(t);
    }
    
    // Shuffle missing types
    for (int i = missingHumanoids.Count - 1; i > 0; i--)
    {
        int j = rnd.Next(0, i + 1);
        TitanType tmp = missingHumanoids[i]; missingHumanoids[i] = missingHumanoids[j]; missingHumanoids[j] = tmp;
    }
    
    // If no missing humanoids, all present players have their valid powers
    if (missingHumanoids.Count == 0)
    {
        Game.ShowChatMessage("[Titan] Restoring distribution (Scenario B)...", Color.Green);
        return;
    }
    
    // 4. Find all players who are currently Pure Titans and can receive missing powers
    List<IPlayer> purePlayers = new List<IPlayer>();
    foreach (IPlayer p in eligible)
    {
        int pid = p.UniqueID;
        if (assignedTitanPowers[pid] == TitanType.Pure)
            purePlayers.Add(p);
    }
    
    if (purePlayers.Count == 0)
    {
        Game.ShowChatMessage("[Titan] Restoring distribution (Scenario B)...", Color.Green);
        return;
    }
    
    // 5. If Player 1 or Player 2 are pure, give them missing humanoid powers first
    if (eligible.Count >= 1 && purePlayers.Contains(eligible[0]) && missingHumanoids.Count > 0)
    {
        IPlayer p1 = eligible[0];
        TitanType assignedType = missingHumanoids[0];
        missingHumanoids.RemoveAt(0);
        purePlayers.Remove(p1);
        
        AssignHumanoidPowerScenarioB(p1, assignedType, nowMinute);
    }
    
    if (eligible.Count >= 2 && purePlayers.Contains(eligible[1]) && missingHumanoids.Count > 0)
    {
        IPlayer p2 = eligible[1];
        TitanType assignedType = missingHumanoids[0];
        missingHumanoids.RemoveAt(0);
        purePlayers.Remove(p2);
        
        AssignHumanoidPowerScenarioB(p2, assignedType, nowMinute);
    }
    
    // 6. Rest of players get remaining missing humanoid titan powers randomly
    while (missingHumanoids.Count > 0 && purePlayers.Count > 0)
    {
        TitanType assignedType = missingHumanoids[0];
        missingHumanoids.RemoveAt(0);
        
        int rIdx = rnd.Next(0, purePlayers.Count);
        IPlayer targetPlayer = purePlayers[rIdx];
        purePlayers.RemoveAt(rIdx);
        
        AssignHumanoidPowerScenarioB(targetPlayer, assignedType, nowMinute);
    }
    
    Game.ShowChatMessage("[Titan] Scenario B: Active humanoids set & missing powers distributed.", Color.Green);
}

private void AssignHumanoidPowerScenarioB(IPlayer player, TitanType power, long nowMinute)
{
    int pid = player.UniqueID;
    assignedTitanPowers[pid] = power;
    long expiry = nowMinute + 30L;
    playerPowerExpiry[player.Name] = expiry;
    SetStoredPlayerPower(player.Name, power);
    SetStoredPlayerExpiry(player.Name, expiry);
    Game.ShowChatMessage("[Titan] " + player.Name + " received " + GetTitanPowerName(power) + "!", Color.Cyan);
}

// Checks if any humanoid type is duplicated among present players
private bool HasDuplicateHumanoids(IPlayer[] allPlayers)
{
    List<IPlayer> eligible = new List<IPlayer>();
    foreach (IPlayer p in allPlayers)
        if (p != null && p.Name.ToLower() != TEMPORARY_PLAYER_NAME.ToLower()) eligible.Add(p);
    
    int atkCount = 0;
    int jawCount = 0;
    int armCount = 0;
    int crtCount = 0;
    
    foreach (IPlayer p in eligible)
    {
        int pid = p.UniqueID;
        if (!assignedTitanPowers.ContainsKey(pid)) continue;
        TitanType t = assignedTitanPowers[pid];
        if (t == TitanType.Attack) atkCount++;
        else if (t == TitanType.Jaw) jawCount++;
        else if (t == TitanType.Armored) armCount++;
        else if (t == TitanType.Cart) crtCount++;
    }
    
    return (atkCount > 1 || jawCount > 1 || armCount > 1 || crtCount > 1);
}

private void SaveAssignedPowersToStorage()
{
    foreach (var kvp in assignedTitanPowers)
    {
        int playerId = kvp.Key;
        string playerName = null;
        
        IPlayer player = Game.GetPlayer(playerId);
        if (player != null)
            playerName = player.Name;
        else if (playerIdToName.ContainsKey(playerId))
            playerName = playerIdToName[playerId];
        
        if (string.IsNullOrEmpty(playerName) || playerName.ToLower() == TEMPORARY_PLAYER_NAME.ToLower())
            continue;
        
        SetStoredPlayerPower(playerName, kvp.Value);
    }
}

private bool RestoreAssignedPowersFromStorage()
{
    IPlayer[] allPlayers = Game.GetPlayers();
    playerNameToId.Clear();
    playerIdToName.Clear();
    foreach (IPlayer player in allPlayers)
    {
        if (player != null)
        {
            playerNameToId[player.Name] = player.UniqueID;
            playerIdToName[player.UniqueID] = player.Name;
        }
    }
    
    assignedTitanPowers.Clear();
    int humanoidFound = 0;
    
    foreach (IPlayer player in allPlayers)
    {
        if (player == null || player.Name.ToLower() == TEMPORARY_PLAYER_NAME.ToLower()) continue;
        TitanType power;
        if (TryGetStoredPlayerPower(player.Name, out power))
        {
            assignedTitanPowers[player.UniqueID] = power;
            if (power != TitanType.Pure)
                humanoidFound++;
        }
        else
        {
            assignedTitanPowers[player.UniqueID] = TitanType.Pure;
        }
    }
    
    return (humanoidFound > 0);
}

// --- Expiry helpers ---

private void SaveExpiryToStorage()
{
    foreach (var kvp in playerPowerExpiry)
    {
        string playerName = kvp.Key;
        if (string.IsNullOrEmpty(playerName) || playerName.ToLower() == TEMPORARY_PLAYER_NAME.ToLower())
            continue;
        
        SetStoredPlayerExpiry(playerName, kvp.Value);
    }
}

private void LoadPlayerExpiryFromStorage()
{
    playerPowerExpiry.Clear();
    IPlayer[] allPlayers = Game.GetPlayers();
    foreach (IPlayer p in allPlayers)
    {
        if (p == null || p.Name.ToLower() == TEMPORARY_PLAYER_NAME.ToLower()) continue;
        long exp;
        if (TryGetStoredPlayerExpiry(p.Name, out exp))
        {
            playerPowerExpiry[p.Name] = exp;
        }
    }
}

// Check expiry for all humanoid-power players. Called in OnUpdate every 30s.
private void CheckPowerExpiry(float currentTime)
{
    if (currentTime - lastExpiryCheckTime < EXPIRY_CHECK_INTERVAL) return;
    lastExpiryCheckTime = currentTime;
    
    if (distributionMode == "A")
    {
        CheckScenarioAExpiry();
    }
    // Scenario B expiry is handled at match-start (RestoreAssignedPowersFromStorage path),
    // no mid-match inheritance needed.
}

private void CheckScenarioAExpiry()
{
    long nowDay = GetUnixDay();
    IPlayer[] allPlayers = Game.GetPlayers();
    
    // Build name→player map
    Dictionary<string, IPlayer> nameToPlayer = new Dictionary<string, IPlayer>();
    foreach (IPlayer p in allPlayers)
        if (p != null && !p.IsDead) nameToPlayer[p.Name] = p;
    
    // Find expired humanoid powers
    List<string> expiredNames = new List<string>();
    foreach (var kvp in playerPowerExpiry)
    {
        if (nowDay >= kvp.Value)
            expiredNames.Add(kvp.Key);
    }
    
    foreach (string expiredName in expiredNames)
    {
        // Find the expired player's ID and power
        if (!nameToPlayer.ContainsKey(expiredName)) continue;
        IPlayer expiredPlayer = nameToPlayer[expiredName];
        int expiredId = expiredPlayer.UniqueID;
        if (!assignedTitanPowers.ContainsKey(expiredId)) continue;
        
        TitanType expiredPower = assignedTitanPowers[expiredId];
        if (expiredPower == TitanType.Pure) continue; // Already pure, skip
        
        string powerName = GetTitanPowerName(expiredPower);
        
        // Find a Pure Titan teammate to inherit
        IPlayer inheritor = null;
        PlayerTeam expiredTeam = expiredPlayer.GetTeam();
        List<IPlayer> candidates = new List<IPlayer>();
        
        foreach (IPlayer p in allPlayers)
        {
            if (p == null || p.IsDead || p.UniqueID == expiredId) continue;
            if (!assignedTitanPowers.ContainsKey(p.UniqueID)) continue;
            if (assignedTitanPowers[p.UniqueID] != TitanType.Pure) continue;
            // Same team (or both independent)
            if (p.GetTeam() != expiredTeam && expiredTeam != PlayerTeam.Independent) continue;
            candidates.Add(p);
        }
        
        if (candidates.Count > 0)
            inheritor = candidates[rnd.Next(0, candidates.Count)];
        
        // Transfer power
        assignedTitanPowers[expiredId] = TitanType.Pure;
        playerPowerExpiry.Remove(expiredName);
        RemoveStoredPlayerExpiry(expiredName);
        SetStoredPlayerPower(expiredName, TitanType.Pure);
        
        if (inheritor != null)
        {
            int inheritorId = inheritor.UniqueID;
            assignedTitanPowers[inheritorId] = expiredPower;
            playerPowerExpiry[inheritor.Name] = GetUnixDay() + 2; // Fresh 2-day expiry
            SetStoredPlayerPower(inheritor.Name, expiredPower);
            SetStoredPlayerExpiry(inheritor.Name, GetUnixDay() + 2);
            
            // Update labels
            CreateLabelForPlayer(expiredPlayer, expiredId, TitanType.Pure);
            CreateLabelForPlayer(inheritor, inheritorId, expiredPower);
            
            // Show popup
            int daysLeft = DaysUntilExpiry(playerPowerExpiry[inheritor.Name]);
            Game.ShowPopupMessage(expiredName + "'s " + powerName + " expired. The next chosen inheritor is " + inheritor.Name + "!", Color.Yellow);
            Game.ShowChatMessage(expiredName + "'s " + powerName + " expired. Inherited by " + inheritor.Name + "!", Color.Yellow);
        }
        else
        {
            // No inheritor available — power lost
            CreateLabelForPlayer(expiredPlayer, expiredId, TitanType.Pure);
            Game.ShowChatMessage(expiredName + "'s " + powerName + " expired with no inheritor available.", Color.Yellow);
        }
        
        // Save updated state
        SaveAssignedPowersToStorage();
        SaveExpiryToStorage();
    }
}
// Retired: replaced by DistributeScenarioA and DistributeScenarioB
private void DistributeWithOriginalAlgorithm(IPlayer[] allPlayers) { }


// Retired: replaced by DistributeScenarioA and DistributeScenarioB
private void DistributeWithHalfSplit(IPlayer[] allPlayers) { }


private void CreateTitanTypeLabels(IPlayer[] allPlayers)
{
    foreach (IPlayer player in allPlayers)
    {
        if (player == null) continue;
        
        int playerId = player.UniqueID;
        if (assignedTitanPowers.ContainsKey(playerId))
        {
            CreateLabelForPlayer(player, playerId, assignedTitanPowers[playerId]);
        }
    }
}

private void CreateLabelForPlayer(IPlayer player, int playerId, TitanType titanType)
{
    // Clean up existing label if any
    if (titanTypeLabels.ContainsKey(playerId))
    {
        IObjectText existingLabel = titanTypeLabels[playerId];
        if (existingLabel != null && !existingLabel.IsRemoved)
            existingLabel.Remove();
        titanTypeLabels.Remove(playerId);
    }
    
    if (titanType == TitanType.Pure) return;
    
    string labelText = "";
    Color labelColor = Color.White;
    
    if (titanType == TitanType.Attack)
    {
        labelText = "[ATK";
        labelColor = new Color(255, 100, 100);
    }
    else if (titanType == TitanType.Jaw)
    {
        labelText = "[JAW";
        labelColor = new Color(100, 255, 100);
    }
    else if (titanType == TitanType.Armored)
    {
        labelText = "[ARM";
        labelColor = new Color(255, 255, 0);
    }
    else if (titanType == TitanType.Cart)
    {
        labelText = "[CRT";
        labelColor = new Color(100, 200, 255);
    }
    else return;
    
    // Append expiry info for Scenario A
    if (distributionMode == "A" && playerPowerExpiry.ContainsKey(player.Name))
    {
        int daysLeft = DaysUntilExpiry(playerPowerExpiry[player.Name]);
        labelText += ":" + daysLeft.ToString();
    }
    
    labelText += "]";
    
    Vector2 playerPos = player.GetWorldPosition();
    IObjectText label = (IObjectText)Game.CreateObject("Text",
        new Vector2(playerPos.X, playerPos.Y + 20f));
    
    label.SetText(labelText);
    label.SetTextAlignment(TextAlignment.Middle);
    label.SetTextScale(0.4f);
    label.SetTextColor(labelColor);
    
    titanTypeLabels[playerId] = label;
}

private void UpdateTitanTypeLabels()
{
    // Update label positions to follow players
    foreach (var kvp in titanTypeLabels)
    {
        int playerId = kvp.Key;
        IObjectText label = kvp.Value;
        
        if (label != null && !label.IsRemoved)
        {
            IPlayer player = Game.GetPlayer(playerId);
            if (player != null && !player.IsRemoved && !player.IsDead)
            {
                Vector2 playerPos = player.GetWorldPosition();
                label.SetWorldPosition(new Vector2(playerPos.X, playerPos.Y + 20f));
            }
            else
            {
                // Player removed or died, remove label
                label.Remove();
            }
        }
    }
}

private void TransformToTitan(IPlayer player)
{
    int playerId = player.UniqueID;
    
    // Don't transform if already a titan
    if (titanPlayers.ContainsKey(playerId))
    {
        return;
    }
    
    // Get the assigned titan type for this player
    TitanType titanType = TitanType.Pure; // Default to Pure if not assigned
    if (assignedTitanPowers.ContainsKey(playerId))
    {
        titanType = assignedTitanPowers[playerId];
    }
    
    Vector2 playerPos = player.GetWorldPosition();
    
    // 1. Explosion effect + sound at player position
    Game.PlayEffect(EffectName.Explosion, playerPos);
    Game.PlayEffect(EffectName.CameraShaker, playerPos);
    Game.PlaySound("Explosion", playerPos);
    Game.PlaySound("Explosion", playerPos);
    Game.PlaySound("Explosion", playerPos);
    
    // Make nearby non-titan players fall
    MakeNearbyPlayersFall(playerPos, TRANSFORM_EXPLOSION_RADIUS, playerId);
    
    // 2. Remove strength boost powerup
    player.RemoveWeaponItemType(WeaponItemType.Powerup);
    
    // Store original profile, modifiers, and bot behaviors (if bot)
    originalProfiles[playerId] = player.GetProfile();
    originalModifiers[playerId] = player.GetModifiers();
    
    if (player.IsBot)
    {
        originalBotBehaviors[playerId] = player.GetBotBehavior();
        originalBotBehaviorSets[playerId] = player.GetBotBehaviorSet();
    }
    
    // 3. Transform based on titan type
    if (titanType == TitanType.Pure)
    {
        TransformToPureTitan(player, playerId);
    }
    else if (titanType == TitanType.Attack)
    {
        TransformToAttackTitan(player, playerId);
    }
    else if (titanType == TitanType.Jaw)
    {
        TransformToJawTitan(player, playerId);
    }
    else if (titanType == TitanType.Cart)
    {
        TransformToCartTitan(player, playerId);
    }
    else
    {
        TransformToArmoredTitan(player, playerId);
    }
    
    // Mark as titan
    titanPlayers[playerId] = titanType;
    
    // Update shared storage to notify odm.cs script
    UpdateTitanPlayersStorage();
    
    // Show transformation message
    string titanTypeName = "";
    if (titanType == TitanType.Pure)
    {
        titanTypeName = "PURE TITAN";
    }
    else if (titanType == TitanType.Attack)
    {
        titanTypeName = "ATTACK TITAN";
    }
    else if (titanType == TitanType.Jaw)
    {
        titanTypeName = "JAW TITAN";
    }
    else if (titanType == TitanType.Cart)
    {
        titanTypeName = "CART TITAN";
    }
    else
    {
        titanTypeName = "ARMORED TITAN";
    }
    Game.ShowChatMessage(player.Name + " has transformed into a " + titanTypeName + "!", Color.Red);
}

private void TransformToPureTitan(IPlayer player, int playerId)
{
    // Clear all items FIRST before profile change
    player.RemoveWeaponItemType(WeaponItemType.Rifle);
    player.RemoveWeaponItemType(WeaponItemType.Handgun);
    player.RemoveWeaponItemType(WeaponItemType.Melee);
    player.RemoveWeaponItemType(WeaponItemType.Thrown);
    player.RemoveWeaponItemType(WeaponItemType.Powerup);
    
    // Set Pure Titan profile while keeping player trademarks
    IProfile currentProfile = player.GetProfile();
    IProfile titanProfile = new IProfile()
    {
        Gender = currentProfile.Gender,
        Skin = new IProfileClothingItem(
            currentProfile.Gender == Gender.Female ? "Normal_fem" : "Normal",
            currentProfile.Skin.Color1,
            "ClothingLightPink"
        ),
        Head = currentProfile.Head,
        Accesory = currentProfile.Accesory,
    };
    player.SetProfile(titanProfile);
    
    // Remove strength boost after transformation
    player.SetStrengthBoostTime(0);
    
    // Get modifiers
    PlayerModifiers mods = player.GetModifiers();
    
    // Apply Pure Titan stats
    int newMaxHealth = (int)(mods.MaxHealth * (1f + PURE_TITAN_HP_BONUS));
    int healthIncrease = newMaxHealth - mods.MaxHealth;
    mods.MaxHealth = newMaxHealth;
    mods.CurrentHealth = mods.CurrentHealth + healthIncrease;
    
    mods.MeleeDamageDealtModifier *= (1f + PURE_TITAN_MELEE_DAMAGE_BONUS);
    mods.MeleeForceModifier *= (1f + PURE_TITAN_MELEE_FORCE_BONUS);
    mods.RunSpeedModifier *= (1f - PURE_TITAN_SPEED_PENALTY);
    mods.SprintSpeedModifier *= (1f - PURE_TITAN_SPEED_PENALTY);
    mods.ItemDropMode = 1;
    
    player.SetModifiers(mods);
    
    // Set bot behavior to BotD
    player.SetBotBehavior(new BotBehavior(true, PredefinedAIType.BotD));
    
    // Configure bot behavior
    BotBehaviorSet bS = player.GetBotBehaviorSet();
    bS.SearchItems = 0;
    
    // Configure bot melee actions: 20% attack, 60% grab, 20% wait
    BotMeleeActions meleeActions = new BotMeleeActions();
    meleeActions.Attack = 20;
    meleeActions.Grab = 60;
    meleeActions.Wait = 20;
    bS.SetMeleeActionsAll(meleeActions);
    
    player.SetBotBehaviorSet(bS);
    
    // Apply size-based scaling
    ApplySizeScaling(player, playerId, mods);
}

private void TransformToAttackTitan(IPlayer player, int playerId)
{
    // Clear all items FIRST before profile change
    player.RemoveWeaponItemType(WeaponItemType.Rifle);
    player.RemoveWeaponItemType(WeaponItemType.Handgun);
    player.RemoveWeaponItemType(WeaponItemType.Melee);
    player.RemoveWeaponItemType(WeaponItemType.Thrown);
    player.RemoveWeaponItemType(WeaponItemType.Powerup);
    
    // Set Attack Titan profile
    IProfile titanProfile = new IProfile()
    {
        Gender = Gender.Female,
        Skin = new IProfileClothingItem("Normal_fem", "Skin2", "ClothingLightOrange"),
        Head = new IProfileClothingItem("AviatorHat2_fem", "ClothingDarkGray", "ClothingLightGreen"),
        Accesory = new IProfileClothingItem("Glasses", "ClothingGreen", "ClothingGreen"),
    };
    player.SetProfile(titanProfile);
    
    // Remove strength boost after transformation
    player.SetStrengthBoostTime(0);
    
    // Get modifiers
    PlayerModifiers mods = player.GetModifiers();
    
    // Apply Attack Titan stats
    int newMaxHealth = (int)(mods.MaxHealth * (1f + ATTACK_TITAN_HP_BONUS));
    int healthIncrease = newMaxHealth - mods.MaxHealth;
    mods.MaxHealth = newMaxHealth;
    mods.CurrentHealth = mods.CurrentHealth + healthIncrease;
    
    mods.MeleeDamageDealtModifier *= (1f + ATTACK_TITAN_MELEE_DAMAGE_BONUS);
    mods.MeleeForceModifier *= (1f + ATTACK_TITAN_MELEE_FORCE_BONUS);
    mods.RunSpeedModifier *= (1f - ATTACK_TITAN_SPEED_PENALTY);
    mods.SprintSpeedModifier *= (1f - ATTACK_TITAN_SPEED_PENALTY);
    
    // Attack Titan has increased energy
    mods.MaxEnergy = (int)(mods.MaxEnergy * (1f + ATTACK_TITAN_ENERGY_BONUS));
    mods.CurrentEnergy = (int)(mods.CurrentEnergy * (1f + ATTACK_TITAN_ENERGY_BONUS));
    
    mods.ItemDropMode = 1;
    
    player.SetModifiers(mods);
    
    // Set bot behavior to BotA (better AI for Attack Titan)
    player.SetBotBehavior(new BotBehavior(true, PredefinedAIType.BotA));
    
    // Configure bot behavior
    BotBehaviorSet bS = player.GetBotBehaviorSet();
    bS.SearchItems = 0;
    player.SetBotBehaviorSet(bS);
    
    // Initialize Attack Titan hardening mode to Neutral
    attackTitanHardeningMode[playerId] = HardeningMode.Neutral;
    
    // Initialize Attack Titan punch count
    attackTitanPunchCount[playerId] = 0;
    
    // Apply height-based scaling (different from Pure Titan's random size)
    ApplyAttackTitanHeightScaling(player, playerId, mods);
}

private void TransformToJawTitan(IPlayer player, int playerId)
{
    // Clear all items FIRST before profile change
    player.RemoveWeaponItemType(WeaponItemType.Rifle);
    player.RemoveWeaponItemType(WeaponItemType.Handgun);
    player.RemoveWeaponItemType(WeaponItemType.Melee);
    player.RemoveWeaponItemType(WeaponItemType.Thrown);
    player.RemoveWeaponItemType(WeaponItemType.Powerup);
    
    // Set Jaw Titan profile
    IProfile titanProfile = new IProfile()
    {
        Name = "jawT",
        Gender = Gender.Female,
        Skin = new IProfileClothingItem("Zombie_fem", ""),
        Accesory = new IProfileClothingItem("GoalieMask", ""),
    };
    player.SetProfile(titanProfile);
    
    // Remove strength boost after transformation
    player.SetStrengthBoostTime(0);
    
    // Get modifiers
    PlayerModifiers mods = player.GetModifiers();
    
    // Apply Jaw Titan stats
    int newMaxHealth = (int)(mods.MaxHealth * (1f + JAW_TITAN_HP_BONUS));
    int healthIncrease = newMaxHealth - mods.MaxHealth;
    mods.MaxHealth = newMaxHealth;
    mods.CurrentHealth = mods.CurrentHealth + healthIncrease;
    
    mods.MeleeDamageDealtModifier *= (1f + JAW_TITAN_MELEE_DAMAGE_BONUS);
    mods.MeleeForceModifier *= (1f + JAW_TITAN_MELEE_FORCE_BONUS);
    mods.RunSpeedModifier *= (1f - JAW_TITAN_SPEED_PENALTY);
    mods.SprintSpeedModifier *= (1f - JAW_TITAN_SPEED_PENALTY);
    
    // Jaw Titan has increased energy
    mods.MaxEnergy = (int)(mods.MaxEnergy * (1f + JAW_TITAN_ENERGY_BONUS));
    mods.CurrentEnergy = (int)(mods.CurrentEnergy * (1f + JAW_TITAN_ENERGY_BONUS));
    
    // Set fixed size
    mods.SizeModifier = JAW_TITAN_SIZE;
    
    // Set melee stun immunity
    mods.MeleeStunImmunity = 1;
    
    mods.ItemDropMode = 1;
    
    // Set health to full
    mods.CurrentHealth = mods.MaxHealth;
    
    player.SetModifiers(mods);
    
    // Set bot behavior to BotD
    player.SetBotBehavior(new BotBehavior(true, PredefinedAIType.BotD));
    
    // Configure bot behavior
    BotBehaviorSet bS = player.GetBotBehaviorSet();
    bS.SearchItems = 0;
    
    // Configure bot melee actions for Jaw Titan: Jump 60, Grab 30, with dive behavior
    BotMeleeActions meleeActions = new BotMeleeActions();
    meleeActions.Jump = 50;
    meleeActions.Grab = 30;
    meleeActions.Attack = 20;
    bS.SetMeleeActionsAll(meleeActions);
    
    // Set dive and sprint levels for offensive behavior
    bS.OffensiveDiveLevel = 0.2f; // 20% dive tendency
    bS.OffensiveSprintLevel = 0.7f; // 70% sprint tendency
    
    player.SetBotBehaviorSet(bS);
    
    // Initialize Jaw Titan punch count
    jawTitanPunchCount[playerId] = 0;
    
    // Store titan size
    titanSizes[playerId] = JAW_TITAN_SIZE;
}

private void TransformToArmoredTitan(IPlayer player, int playerId)
{
    // Clear all items FIRST before profile change
    player.RemoveWeaponItemType(WeaponItemType.Rifle);
    player.RemoveWeaponItemType(WeaponItemType.Handgun);
    player.RemoveWeaponItemType(WeaponItemType.Melee);
    player.RemoveWeaponItemType(WeaponItemType.Thrown);
    player.RemoveWeaponItemType(WeaponItemType.Powerup);
    
    // Set Armored Titan profile (armor ON by default)
    IProfile titanProfile = new IProfile()
    {
        Gender = Gender.Male,
        Skin = new IProfileClothingItem("Normal", "Skin5", "ClothingLightGray"),
        Head = new IProfileClothingItem("Buzzcut", "ClothingLightGray"),
        ChestUnder = new IProfileClothingItem("BodyArmor", "ClothingLightGray"),
        Hands = new IProfileClothingItem("Gloves", "ClothingLightGray"),
        Legs = new IProfileClothingItem("Pants", "ClothingLightGray"),
        Feet = new IProfileClothingItem("Boots", "ClothingLightGray"),
        Accesory = new IProfileClothingItem("ClownMakeup", "ClothingLightGray"),
    };
    player.SetProfile(titanProfile);
    
    // Remove strength boost after transformation
    player.SetStrengthBoostTime(0);
    
    // Get modifiers
    PlayerModifiers mods = player.GetModifiers();
    
    // Apply Armored Titan stats
    int newMaxHealth = (int)(mods.MaxHealth * (1f + ARMORED_TITAN_HP_BONUS));
    int healthIncrease = newMaxHealth - mods.MaxHealth;
    mods.MaxHealth = newMaxHealth;
    mods.CurrentHealth = mods.CurrentHealth + healthIncrease;
    
    mods.MeleeDamageDealtModifier *= (1f + ARMORED_TITAN_MELEE_DAMAGE_BONUS);
    mods.MeleeForceModifier *= (1f + ARMORED_TITAN_MELEE_FORCE_BONUS);
    mods.RunSpeedModifier *= (1f - ARMORED_TITAN_SPEED_PENALTY);
    mods.SprintSpeedModifier *= (1f - ARMORED_TITAN_SPEED_PENALTY);
    
    // Armored Titan has increased energy
    mods.MaxEnergy = (int)(mods.MaxEnergy * (1f + ARMORED_TITAN_ENERGY_BONUS));
    mods.CurrentEnergy = (int)(mods.CurrentEnergy * (1f + ARMORED_TITAN_ENERGY_BONUS));
    
    // Set fixed size
    mods.SizeModifier = ARMORED_TITAN_SIZE;
    
    // Set melee stun immunity
    mods.MeleeStunImmunity = 1;
    
    mods.ItemDropMode = 1;
    
    // Set health to full
    mods.CurrentHealth = mods.MaxHealth;
    
    player.SetModifiers(mods);
    
    // Set bot behavior to BotA (tactical AI)
    player.SetBotBehavior(new BotBehavior(true, PredefinedAIType.BotA));
    
    // Configure bot behavior
    BotBehaviorSet bS = player.GetBotBehaviorSet();
    bS.SearchItems = 0;
    
    // Configure bot melee actions for Armored Titan
    BotMeleeActions meleeActions = new BotMeleeActions();
    meleeActions.Attack = 23;
    meleeActions.AttackCombo = 23;
    meleeActions.Block = 23;
    meleeActions.Grab = 23;
    meleeActions.Wait = 8;
    bS.SetMeleeActionsAll(meleeActions);
    
    player.SetBotBehaviorSet(bS);
    
    // Initialize Armored Titan armor state to ON (via ApplyArmoredTitanArmor to ensure modifiers are applied)
    ApplyArmoredTitanArmor(player, playerId, true);
    
    // Initialize rush hit tracking
    armoredTitanRushHitTimes[playerId] = new Dictionary<int, float>();
    
    // Store titan size
    titanSizes[playerId] = ARMORED_TITAN_SIZE;
}

private void ApplySizeScaling(IPlayer player, int playerId, PlayerModifiers mods)
{
    // Generate random size for Pure Titan
    float titanSize = TITAN_MIN_SIZE + (float)(rnd.NextDouble() * (TITAN_MAX_SIZE - TITAN_MIN_SIZE));
    
    // Calculate stat multiplier based on size
    float sizeSteps = (titanSize - TITAN_MIN_SIZE) / 0.01f;
    float statMultiplier = 1f + (sizeSteps * SIZE_STAT_BONUS_PER_0_01);
    
    // Calculate speed penalty based on size
    float speedPenalty = sizeSteps * SIZE_SPEED_PENALTY_PER_0_01;
    float speedMultiplier = 1f - speedPenalty;
    
    // Apply size
    mods.SizeModifier = titanSize;
    
    // Apply size-based stat bonuses
    mods.MaxHealth = (int)(mods.MaxHealth * statMultiplier);
    mods.MeleeDamageDealtModifier *= statMultiplier;
    mods.MeleeForceModifier *= statMultiplier;
    mods.MeleeStunImmunity = 1;
    
    // Apply size-based speed penalty
    mods.RunSpeedModifier *= speedMultiplier;
    mods.SprintSpeedModifier *= speedMultiplier;
    
    // Set health to full
    mods.CurrentHealth = mods.MaxHealth;
    
    player.SetModifiers(mods);
    
    // Store titan size
    titanSizes[playerId] = titanSize;
}

private void ApplyAttackTitanHeightScaling(IPlayer player, int playerId, PlayerModifiers mods)
{
    // Calculate team health totals
    PlayerTeam ownTeam = player.GetTeam();
    float ownTeamTotalHP = 0f;
    float opponentTeamTotalHP = 0f;
    
    IPlayer[] allPlayers = Game.GetPlayers();
    foreach (IPlayer p in allPlayers)
    {
        if (p == null || p.IsDead) continue;
        
        PlayerTeam pTeam = p.GetTeam();
        PlayerModifiers pMods = p.GetModifiers();
        float pHP = pMods.CurrentHealth;
        
        if (ownTeam == PlayerTeam.Independent)
        {
            // If own team is Independent/None, self is own team, everyone else is opponent
            if (p.UniqueID == playerId)
            {
                ownTeamTotalHP += pHP;
            }
            else
            {
                opponentTeamTotalHP += pHP;
            }
        }
        else
        {
            // Normal team logic
            if (pTeam == ownTeam)
            {
                ownTeamTotalHP += pHP;
            }
            else if (pTeam != PlayerTeam.Independent)
            {
                opponentTeamTotalHP += pHP;
            }
        }
    }
    
    // Calculate differ
    float differ = opponentTeamTotalHP / (ownTeamTotalHP > 0f ? ownTeamTotalHP : 1f);
    
    // Calculate size based on differ using interpolation
    float sizeMod;
    bool giveInfiniteStrength = false;
    
    if (differ <= 1f)
    {
        sizeMod = 0.920f;
    }
    else if (differ < 1.7f)
    {
        // Interpolate between 0.920 and 1.250
        float t = (differ - 1f) / (1.7f - 1f);
        sizeMod = 0.920f + (1.250f - 0.920f) * t;
    }
    else if (differ < 2.5f)
    {
        // Interpolate between 1.250 and 1.600
        float t = (differ - 1.7f) / (2.5f - 1.7f);
        sizeMod = 1.250f + (1.600f - 1.250f) * t;
    }
    else if (differ < 3f)
    {
        // Interpolate between 1.600 and 1.600 (stay at 1.600)
        sizeMod = 1.600f;
    }
    else
    {
        // 3 or more: 1.600 + infinite strength boost
        sizeMod = 1.600f;
        giveInfiniteStrength = true;
    }
    
    // Apply size
    mods.SizeModifier = sizeMod;
    
    // Apply same size-based scaling as Pure Titan
    float sizeSteps = (sizeMod - TITAN_MIN_SIZE) / 0.01f;
    float statMultiplier = 1f + (sizeSteps * SIZE_STAT_BONUS_PER_0_01);
    float speedPenalty = sizeSteps * SIZE_SPEED_PENALTY_PER_0_01;
    float speedMultiplier = 1f - speedPenalty;
    
    mods.MaxHealth = (int)(mods.MaxHealth * statMultiplier);
    mods.MeleeDamageDealtModifier *= statMultiplier;
    mods.MeleeForceModifier *= statMultiplier;
    mods.MeleeStunImmunity = 1;
    
    mods.RunSpeedModifier *= speedMultiplier;
    mods.SprintSpeedModifier *= speedMultiplier;
    
    // Set health to full
    mods.CurrentHealth = mods.MaxHealth;
    
    player.SetModifiers(mods);

    // Give infinite strength boost if differ >= 3
    if (giveInfiniteStrength)
    {
        player.SetStrengthBoostTime(999999f);
    }
    
    // Store titan size
    titanSizes[playerId] = sizeMod;
}

public void OnPlayerKeyInput(IPlayer player, VirtualKeyInfo[] keyInfos)
{
    if (player == null || player.IsDead) return;
    
    int playerId = player.UniqueID;
    
    // Detach: human players (non-bot) can detach by holding Walk key for 800ms (non-Pure titans only)
    if (!player.IsBot && titanPlayers.ContainsKey(playerId) && titanPlayers[playerId] != TitanType.Pure)
    {
        foreach (VirtualKeyInfo keyInfo in keyInfos)
        {
            if (keyInfo.Key == VirtualKey.WALKING)
            {
                if (keyInfo.Event == VirtualKeyEvent.Pressed)
                {
                    if (!walkKeyPressStartTime.ContainsKey(playerId))
                    {
                        walkKeyPressStartTime[playerId] = Game.TotalElapsedRealTime;
                    }
                }
                else if (keyInfo.Event == VirtualKeyEvent.Released)
                {
                    walkKeyPressStartTime.Remove(playerId);
                }
            }
        }
    }
    
    // Check if player is an Attack Titan
    if (titanPlayers.ContainsKey(playerId) && titanPlayers[playerId] == TitanType.Attack)
    {
        // Only allow human players to use abilities
        if (player.IsBot) return;
        
        foreach (VirtualKeyInfo keyInfo in keyInfos)
        {
            // DRAW_SPECIAL key: Slowmo ability
            if (keyInfo.Event == VirtualKeyEvent.Pressed && keyInfo.Key == VirtualKey.DRAW_SPECIAL)
            {
                ActivateAttackTitanSlowmo(player, playerId);
            }
            
            // SHEATHE key: Toggle hardening mode
            if (keyInfo.Event == VirtualKeyEvent.Pressed && keyInfo.Key == VirtualKey.SHEATHE)
            {
                ToggleAttackTitanHardening(player, playerId);
            }
        }
        return;
    }
    
    // Check if player is an Armored Titan
    if (titanPlayers.ContainsKey(playerId) && titanPlayers[playerId] == TitanType.Armored)
    {
        // Only allow human players to use abilities
        if (player.IsBot) return;
        
        foreach (VirtualKeyInfo keyInfo in keyInfos)
        {
            // SHEATHE key: Toggle armor
            if (keyInfo.Event == VirtualKeyEvent.Pressed && keyInfo.Key == VirtualKey.SHEATHE)
            {
                ToggleArmoredTitanArmor(player, playerId);
            }
        }
        return;
    }
    
    // Check if player is a Cart Titan
    if (titanPlayers.ContainsKey(playerId) && titanPlayers[playerId] == TitanType.Cart)
    {
        // Only allow human players to use abilities
        if (player.IsBot) return;
        
        foreach (VirtualKeyInfo keyInfo in keyInfos)
        {
            // SHEATHE key: Toggle form (Rescue <-> Offensive)
            if (keyInfo.Event == VirtualKeyEvent.Pressed && keyInfo.Key == VirtualKey.SHEATHE)
            {
                ToggleCartTitanForm(player, playerId);
            }
            
            // DRAW_SPECIAL: Release lifted ally (Rescue form)
            if (keyInfo.Event == VirtualKeyEvent.Pressed && keyInfo.Key == VirtualKey.DRAW_SPECIAL)
            {
                CartTitanReleaseAlly(player, playerId);
            }
            
            // DRAW_RIFLE: Switch to sniper (Offensive form)
            if (keyInfo.Event == VirtualKeyEvent.Pressed && keyInfo.Key == VirtualKey.DRAW_RIFLE)
            {
                CartTitanSwitchGun(playerId, true);
            }
            
            // DRAW_HANDGUN: Switch to M60 (Offensive form)
            if (keyInfo.Event == VirtualKeyEvent.Pressed && keyInfo.Key == VirtualKey.DRAW_HANDGUN)
            {
                CartTitanSwitchGun(playerId, false);
            }
            
            // BLOCK: Enter/exit aim-adjust mode (Offensive form)
            if (keyInfo.Key == VirtualKey.BLOCK)
            {
                bool blockHeld = (keyInfo.Event == VirtualKeyEvent.Pressed);
                CartTitanSetBlockMode(player, playerId, blockHeld);
            }
        }
    }
}

private void ActivateAttackTitanSlowmo(IPlayer player, int playerId)
{
    float currentTime = Game.TotalElapsedRealTime;
    
    // Check if slowmo is already active
    if (attackTitanSlowmoEndTime.ContainsKey(playerId) && attackTitanSlowmoEndTime[playerId] > currentTime)
    {
        // Slowmo already active, ignore
        return;
    }
    
    PlayerModifiers mods = player.GetModifiers();
    
    // Check if player has enough energy (20% of max)
    float energyCost = mods.MaxEnergy * SLOWMO_ENERGY_COST;
    if (mods.CurrentEnergy < energyCost)
    {
        Game.PlaySound("OutOfAmmoHeavy", player.GetWorldPosition());
        return;
    }
    
    // Deduct energy cost
    mods.CurrentEnergy -= (int)energyCost;
    player.SetModifiers(mods);
    
    // Give SLOWMO_5 powerup
    player.GiveWeaponItem(WeaponItem.SLOWMO_5);
    
    // Force player to draw powerup
    QuickCommand(player, new PlayerCommand(PlayerCommandType.DrawPowerup));
    
    // Schedule slowmo end after 2 seconds
    attackTitanSlowmoEndTime[playerId] = currentTime + SLOWMO_ACTIVATION_DELAY;
}

private void ToggleAttackTitanHardening(IPlayer player, int playerId)
{
    // Get current hardening mode
    HardeningMode currentMode = HardeningMode.Neutral;
    if (attackTitanHardeningMode.ContainsKey(playerId))
    {
        currentMode = attackTitanHardeningMode[playerId];
    }
    
    // Toggle between modes: Neutral -> Offensive -> Defensive -> Neutral
    HardeningMode newMode;
    if (currentMode == HardeningMode.Neutral)
    {
        newMode = HardeningMode.Offensive;
    }
    else if (currentMode == HardeningMode.Offensive)
    {
        newMode = HardeningMode.Defensive;
    }
    else
    {
        newMode = HardeningMode.Neutral;
    }
    
    // Apply hardening mode
    ApplyAttackTitanHardening(player, playerId, newMode);
}

private void ApplyAttackTitanHardening(IPlayer player, int playerId, HardeningMode newMode)
{
    HardeningMode oldMode = HardeningMode.Neutral;
    if (attackTitanHardeningMode.ContainsKey(playerId))
    {
        oldMode = attackTitanHardeningMode[playerId];
    }
    
    // Get modifiers
    PlayerModifiers mods = player.GetModifiers();
    
    // Remove old mode effects
    if (oldMode == HardeningMode.Offensive)
    {
        // Remove offensive bonus
        mods.MeleeDamageDealtModifier /= (1f + HARDENING_OFFENSIVE_DAMAGE_BONUS);
    }
    else if (oldMode == HardeningMode.Defensive)
    {
        // Remove defensive effects
        mods.RunSpeedModifier /= (1f - HARDENING_DEFENSIVE_SPEED_PENALTY);
        mods.SprintSpeedModifier /= (1f - HARDENING_DEFENSIVE_SPEED_PENALTY);
        mods.MeleeDamageTakenModifier /= (1f - HARDENING_DEFENSIVE_DEFENSE_BONUS);
        mods.ProjectileDamageTakenModifier /= (1f - HARDENING_DEFENSIVE_DEFENSE_BONUS);
    }
    
    // Apply new mode effects
    if (newMode == HardeningMode.Offensive)
    {
        // Apply offensive bonus
        mods.MeleeDamageDealtModifier *= (1f + HARDENING_OFFENSIVE_DAMAGE_BONUS);
        
        // Set offensive profile
        player.SetProfile(new IProfile()
        {
            Gender = Gender.Female,
            Skin = new IProfileClothingItem("Normal_fem", "Skin2", "ClothingLightOrange"),
            Head = new IProfileClothingItem("AviatorHat2_fem", "ClothingDarkGray", "ClothingLightGreen"),
            Hands = new IProfileClothingItem("SafetyGloves_fem", "ClothingLightGray"),
            Accesory = new IProfileClothingItem("Glasses", "ClothingGreen", "ClothingGreen"),
        });
    }
    else if (newMode == HardeningMode.Defensive)
    {
        // Apply defensive effects
        mods.RunSpeedModifier *= (1f - HARDENING_DEFENSIVE_SPEED_PENALTY);
        mods.SprintSpeedModifier *= (1f - HARDENING_DEFENSIVE_SPEED_PENALTY);
        mods.MeleeDamageTakenModifier *= (1f - HARDENING_DEFENSIVE_DEFENSE_BONUS);
        mods.ProjectileDamageTakenModifier *= (1f - HARDENING_DEFENSIVE_DEFENSE_BONUS);
        
        // Set defensive profile
        player.SetProfile(new IProfile()
        {
            Gender = Gender.Female,
            Skin = new IProfileClothingItem("Normal_fem", "Skin2", "ClothingLightOrange"),
            Head = new IProfileClothingItem("AviatorHat2_fem", "ClothingDarkGray", "ClothingLightGreen"),
            ChestUnder = new IProfileClothingItem("ShirtWithTie_fem", "ClothingLightGray", "ClothingLightGray"),
            Accesory = new IProfileClothingItem("Glasses", "ClothingGreen", "ClothingGreen"),
        });
    }
    else
    {
        // Neutral mode - set default Attack Titan profile
        player.SetProfile(new IProfile()
        {
            Gender = Gender.Female,
            Skin = new IProfileClothingItem("Normal_fem", "Skin2", "ClothingLightOrange"),
            Head = new IProfileClothingItem("AviatorHat2_fem", "ClothingDarkGray", "ClothingLightGreen"),
            Accesory = new IProfileClothingItem("Glasses", "ClothingGreen", "ClothingGreen"),
        });
    }
    
    player.SetModifiers(mods);
    
    // Update hardening mode
    attackTitanHardeningMode[playerId] = newMode;
}

public void OnPlayerMeleeAction(IPlayer player, PlayerMeleeHitArg[] args)
{
    if (player == null || player.IsDead) return;
    
    int playerId = player.UniqueID;
    float currentTime = Game.TotalElapsedRealTime;
    
    // Handle Attack Titan melee
    if (titanPlayers.ContainsKey(playerId) && titanPlayers[playerId] == TitanType.Attack)
    {
        HandleAttackTitanMelee(player, playerId, args, currentTime);
        return;
    }
    
    // Handle Jaw Titan melee
    if (titanPlayers.ContainsKey(playerId) && titanPlayers[playerId] == TitanType.Jaw)
    {
        HandleJawTitanMelee(player, playerId, args, currentTime);
        return;
    }
    
    // Handle Armored Titan melee
    if (titanPlayers.ContainsKey(playerId) && titanPlayers[playerId] == TitanType.Armored)
    {
        HandleArmoredTitanMelee(player, playerId, args, currentTime);
        return;
    }
    
    // Update facing direction for all players (for backstab detection)
    if (!playerFacingDirections.ContainsKey(playerId))
    {
        playerFacingDirections[playerId] = player.FacingDirection;
    }
    else
    {
        playerFacingDirections[playerId] = player.FacingDirection;
    }
}

private void HandleAttackTitanMelee(IPlayer attackTitan, int attackTitanId, PlayerMeleeHitArg[] args, float currentTime)
{
    // Update last melee time for offensive mode timeout
    attackTitanLastMeleeTime[attackTitanId] = currentTime;
    
    // Only track punch combos when not kicking
    bool isPunch = !attackTitan.IsKicking && !attackTitan.IsJumpKicking;
    
    if (isPunch)
    {
        // Update punch count and timing
        if (!attackTitanLastPunchTime.ContainsKey(attackTitanId))
        {
            attackTitanPunchCount[attackTitanId] = 1;
            attackTitanLastPunchTime[attackTitanId] = currentTime;
        }
        else
        {
            float timeSinceLastPunch = currentTime - attackTitanLastPunchTime[attackTitanId];
            if (timeSinceLastPunch <= ATTACK_SUPER_PUNCH_WINDOW)
            {
                // Within window, increment punch count
                attackTitanPunchCount[attackTitanId]++;
            }
            else
            {
                // Outside window, reset to 1
                attackTitanPunchCount[attackTitanId] = 1;
            }
            attackTitanLastPunchTime[attackTitanId] = currentTime;
        }
    }
    
    int currentPunchCount = attackTitanPunchCount.ContainsKey(attackTitanId) ? attackTitanPunchCount[attackTitanId] : 0;
    bool isSuperPunch = isPunch && currentPunchCount >= ATTACK_SUPER_PUNCH_COUNT;
    
    // Process hit targets
    if (args != null && args.Length > 0)
    {
        foreach (PlayerMeleeHitArg hitArg in args)
        {
            if (hitArg.HitObject == null || !hitArg.IsPlayer) continue;
            
            IPlayer hitPlayer = hitArg.HitObject as IPlayer;
            if (hitPlayer == null)
            {
                hitPlayer = Game.GetPlayer(hitArg.HitObject.UniqueID);
            }
            if (hitPlayer == null || hitPlayer.IsDead) continue;
            
            // Super Punch: on third punch, victim will always stagger
            if (isSuperPunch)
            {
                QuickCommand(hitPlayer, new PlayerCommand(PlayerCommandType.Stagger));
            }
        }
    }
    
    // Reset punch count after 3rd punch
    if (isSuperPunch)
    {
        attackTitanPunchCount[attackTitanId] = 0;
    }
}

private void HandleJawTitanMelee(IPlayer jawTitan, int jawTitanId, PlayerMeleeHitArg[] args, float currentTime)
{
    // Update punch count and timing
    if (!jawTitanLastPunchTime.ContainsKey(jawTitanId))
    {
        jawTitanPunchCount[jawTitanId] = 1;
        jawTitanLastPunchTime[jawTitanId] = currentTime;
    }
    else
    {
        float timeSinceLastPunch = currentTime - jawTitanLastPunchTime[jawTitanId];
        if (timeSinceLastPunch <= JAW_SHARP_CLAW_PUNCH_WINDOW)
        {
            // Within window, increment punch count
            jawTitanPunchCount[jawTitanId]++;
        }
        else
        {
            // Outside window, reset to 1
            jawTitanPunchCount[jawTitanId] = 1;
        }
        jawTitanLastPunchTime[jawTitanId] = currentTime;
    }
    
    // Record combat timestamp for bot backstab targeting logic
    if (jawTitan.IsBot)
    {
        jawBotLastCombatTime[jawTitanId] = currentTime;
    }
    
    int currentPunchCount = jawTitanPunchCount[jawTitanId];
    bool isJumpAttack = jawTitan.IsJumpAttacking && !jawTitan.IsJumpKicking && !jawTitan.IsKicking;
    
    foreach (PlayerMeleeHitArg hitArg in args)
    {
        if (hitArg.HitObject == null) continue;
        
        // Handle player hits
        if (hitArg.IsPlayer)
        {
            IPlayer victim = hitArg.HitObject as IPlayer;
            if (victim == null || victim.IsDead) continue;
            
            // Claw ability: extra damage on jump attacks
            if (isJumpAttack)
            {
                HandleJawTitanClawAttack(jawTitan, jawTitanId, victim);
            }
        }
        else
        {
            // Handle object hits - Sharp Claw: 3rd punch breaks objects
            if (currentPunchCount >= JAW_SHARP_CLAW_PUNCH_COUNT)
            {
                IObject hitObject = hitArg.HitObject;
                
                // Destroy the object
                hitObject.Destroy();
                
                // Visual feedback
                Game.PlayEffect(EffectName.DestroyMetal, hitObject.GetWorldPosition());
                Game.PlaySound("DestroyMetal", hitObject.GetWorldPosition());
            }
        }
    }
    
    // Reset punch count after 3rd punch
    if (currentPunchCount >= JAW_SHARP_CLAW_PUNCH_COUNT)
    {
        jawTitanPunchCount[jawTitanId] = 0;
    }
}

private void HandleJawTitanClawAttack(IPlayer jawTitan, int jawTitanId, IPlayer victim)
{
    int victimId = victim.UniqueID;
    
    // Get victim's facing direction (stored before hit to prevent instant turn-around exploitation)
    int victimFacing = playerFacingDirections.ContainsKey(victimId) ? 
                      playerFacingDirections[victimId] : 
                      victim.FacingDirection;
    
    int attackerFacing = jawTitan.FacingDirection;
    
    // Determine if this is a backstab (same facing direction = attacking from behind)
    bool isBackstab = (attackerFacing == victimFacing);
    
    // Calculate damage
    PlayerModifiers jawMods = jawTitan.GetModifiers();
    float baseDamage = JAW_CLAW_BASE_DAMAGE * jawMods.MeleeDamageDealtModifier;
    float damageMultiplier = isBackstab ? JAW_CLAW_BACK_MULTIPLIER : JAW_CLAW_FRONT_MULTIPLIER;
    float totalDamage = baseDamage * damageMultiplier;
    
    // Get victim's health before damage
    PlayerModifiers victimMods = victim.GetModifiers();
    float healthBeforeDamage = victimMods.CurrentHealth;
    
    // Deal damage
    victim.DealDamage(totalDamage);
    
    // Play sound based on attack type
    Vector2 victimPos = victim.GetWorldPosition();
    if (isBackstab)
    {
        Game.PlaySound("KatanaDraw", victimPos);
    }
    else
    {
        Game.PlaySound("MacheteDraw", victimPos);
    }
    
    // Check if victim should be gibbed (took more than 30% extra damage beyond max HP)
    float overkillDamage = totalDamage - healthBeforeDamage;
    float overkillThreshold = victimMods.MaxHealth * JAW_CLAW_GIB_THRESHOLD;
    
    if (victim.IsDead && overkillDamage > overkillThreshold)
    {
        victim.Gib();
    }
}

private void HandleArmoredTitanMelee(IPlayer armoredTitan, int armoredTitanId, PlayerMeleeHitArg[] args, float currentTime)
{
    // Update punch count and timing
    if (!armoredTitanLastPunchTime.ContainsKey(armoredTitanId))
    {
        armoredTitanPunchCount[armoredTitanId] = 1;
        armoredTitanLastPunchTime[armoredTitanId] = currentTime;
    }
    else
    {
        float timeSinceLastPunch = currentTime - armoredTitanLastPunchTime[armoredTitanId];
        if (timeSinceLastPunch <= ARMORED_SUPER_PUNCH_WINDOW)
        {
            // Within window, increment punch count
            armoredTitanPunchCount[armoredTitanId]++;
        }
        else
        {
            // Outside window, reset to 1
            armoredTitanPunchCount[armoredTitanId] = 1;
        }
        armoredTitanLastPunchTime[armoredTitanId] = currentTime;
    }
    
    int currentPunchCount = armoredTitanPunchCount[armoredTitanId];
    
    // Check if this is the 3rd punch (super punch)
    if (currentPunchCount >= ARMORED_SUPER_PUNCH_COUNT)
    {
        float titanForce = GetPlayerForce(armoredTitan);
        float titanHeight = GetPlayerHeight(armoredTitan);
        float titanPower = titanForce * titanHeight;
        int titanFacing = armoredTitan.FacingDirection;
        
        // Apply super punch to all hit players and objects
        foreach (PlayerMeleeHitArg hitArg in args)
        {
            if (hitArg.HitObject == null) continue;
            
            if (hitArg.IsPlayer)
            {
                IPlayer victim = hitArg.HitObject as IPlayer;
                if (victim == null || victim.IsDead) continue;
                
                // Calculate throw power: (Titan Force × Height) - (Target Force × Height)
                float victimForce = GetPlayerForce(victim);
                float victimHeight = GetPlayerHeight(victim);
                float victimPower = victimForce * victimHeight;
                
                float throwPower = (titanPower - (victimPower * ARMORED_RUSH_TARGET_FORCE_WEIGHT)) * ARMORED_SUPER_PUNCH_BALANCER_MOD;
                throwPower = Math.Max(throwPower, 0f); // Never negative
                
                if (throwPower > 0)
                {
                    // Apply fall command
                    QuickCommand(victim, new PlayerCommand(PlayerCommandType.Fall));
                    
                    // Apply throw velocity
                    float velocity = throwPower * ARMORED_RUSH_THROW_SCALE;
                    victim.SetLinearVelocity(new Vector2(titanFacing * velocity, velocity * ARMORED_RUSH_THROW_UP_RATIO));
                    
                    // Visual feedback
                    Game.PlayEffect(EffectName.CameraShaker, victim.GetWorldPosition());
                    Game.PlaySound("DestroyMetal", victim.GetWorldPosition());
                }
            }
            else
            {
                IObject hitObj = hitArg.HitObject;
                if (hitObj.IsRemoved || !hitObj.Destructable) continue;
                if (hitObj.UniqueID == armoredTitanId) continue;
                
                string objName = hitObj.Name;
                if (objName.StartsWith("Bg") || objName.StartsWith("BG")) continue;
                if (objName.StartsWith("SoundArea")) continue;
                if (objName.Contains("Trigger") || objName.Contains("Spawn") ||
                    objName.Contains("Marker")  || objName.Contains("Ladder") ||
                    objName.Contains("Joint")) continue;
                
                // Skip if object is under the titan's feet
                if (hitObj.GetWorldPosition().Y < armoredTitan.GetAABB().Bottom) continue;
                
                float throwPower = titanPower * ARMORED_SUPER_PUNCH_BALANCER_MOD;
                if (throwPower > 0)
                {
                    float velocity = throwPower * ARMORED_RUSH_THROW_SCALE;
                    hitObj.SetLinearVelocity(new Vector2(titanFacing * velocity, velocity * ARMORED_RUSH_THROW_UP_RATIO));
                    
                    // Visual feedback
                    Game.PlayEffect(EffectName.CameraShaker, hitObj.GetWorldPosition());
                    Game.PlaySound("DestroyMetal", hitObj.GetWorldPosition());
                }
            }
        }
        
        // Reset punch count after super punch
        armoredTitanPunchCount[armoredTitanId] = 0;
    }
}

public void OnPlayerDeath(IPlayer deadPlayer)
{
    if (deadPlayer == null) return;
    
    int deadPlayerId = deadPlayer.UniqueID;
    
    Game.ShowChatMessage("OnPlayerDeath called for: " + deadPlayer.Name, Color.Cyan);
    
    // Check if this player was gibbed by a Pure Titan during grab
    if (!gibbedByPureTitan.ContainsKey(deadPlayerId))
    {
        Game.ShowChatMessage("Not in gibbedByPureTitan dictionary", Color.Red);
        return;
    }
    
    Game.ShowChatMessage("Found in gibbedByPureTitan!", Color.Green);
    
    int pureTitanId = gibbedByPureTitan[deadPlayerId];
    
    // Clean up tracking immediately to prevent double processing
    gibbedByPureTitan.Remove(deadPlayerId);
    
    IPlayer pureTitan = Game.GetPlayer(pureTitanId);
    
    // Verify Pure Titan still exists and is still a Pure Titan
    if (pureTitan == null || !titanPlayers.ContainsKey(pureTitanId) || titanPlayers[pureTitanId] != TitanType.Pure)
    {
        Game.ShowChatMessage("Pure Titan check failed", Color.Red);
        return;
    }
    
    // Check if victim had a humanoid titan power
    if (!assignedTitanPowers.ContainsKey(deadPlayerId)) 
    {
        Game.ShowChatMessage("Victim has no assigned power", Color.Red);
        return;
    }
    
    TitanType extractedPower = assignedTitanPowers[deadPlayerId];
    if (extractedPower == TitanType.Pure)
    {
        Game.ShowChatMessage("Victim was Pure Titan", Color.Red);
        return; // Pure titans don't trigger power extract
    }
    
    Game.ShowChatMessage("Extracting power!", Color.Green);
    
    // Extract the power!
    ExtractTitanPower(pureTitan, pureTitanId, deadPlayer, deadPlayerId, extractedPower);
}

public void OnProjectileHit(IProjectile projectile, ProjectileHitArgs args)
{
    // Check if hit an Armored Titan
    if (!args.IsPlayer) return;
    
    IPlayer hitPlayer = Game.GetPlayer(args.HitObjectID);
    if (hitPlayer == null) return;
    
    int hitPlayerId = hitPlayer.UniqueID;
    
    // Check if Armored Titan with armor ON
    if (!titanPlayers.ContainsKey(hitPlayerId) || titanPlayers[hitPlayerId] != TitanType.Armored) return;
    if (!armoredTitanArmorOn.ContainsKey(hitPlayerId) || !armoredTitanArmorOn[hitPlayerId]) return;
    
    // Check if hit from front
    if (!IsArmoredInFront(hitPlayer, args.HitPosition)) return;
    
    // Reflect the projectile
    Vector2 projDir = projectile.Direction;
    Vector2 reflected = new Vector2(-projDir.X, projDir.Y); // Mirror reflection (flip X)
    
    // Spawn reflected projectile
    Vector2 spawnPos = args.HitPosition + reflected * ARMORED_REFLECT_OFFSET;
    IProjectile newProj = Game.SpawnProjectile(projectile.ProjectileItem, spawnPos, reflected);
    
    if (newProj != null)
    {
        newProj.DamageDealtModifier = projectile.DamageDealtModifier;
        // Note: Cannot set InitialOwnerPlayerID (read-only), but projectile will use OwnerPlayerID from spawn
    }
    
    // Remove original projectile
    projectile.FlagForRemoval();
    
    // Play effects
    Game.PlayEffect(EffectName.Sparks, args.HitPosition);
    Game.PlayEffect(EffectName.Block, args.HitPosition);
}

public void OnPlayerDamage(IPlayer player, PlayerDamageArgs args)
{
    // Safety net: refund damage that slipped through armor
    if (player == null) return;
    
    int playerId = player.UniqueID;
    
    // Check if damaged player is a bot that can activate strength boost
    if (player.IsBot && !player.IsDead)
    {
        TryActivateBotStrengthBoost(player, playerId);
    }
    
    // Check if Armored Titan with armor ON
    if (!titanPlayers.ContainsKey(playerId) || titanPlayers[playerId] != TitanType.Armored) return;
    if (!armoredTitanArmorOn.ContainsKey(playerId) || !armoredTitanArmorOn[playerId]) return;
    
    // Only handle melee, projectile, and thrown damage
    Vector2 sourcePos = Vector2.Zero;
    bool hasSource = false;
    
    if (args.DamageType == PlayerDamageEventType.Projectile)
    {
        IProjectile proj = Game.GetProjectile(args.SourceID);
        if (proj != null)
        {
            sourcePos = proj.Position;
            hasSource = true;
        }
    }
    else if (args.DamageType == PlayerDamageEventType.Melee)
    {
        IPlayer attacker = Game.GetPlayer(args.SourceID);
        if (attacker != null)
        {
            sourcePos = attacker.GetWorldPosition();
            hasSource = true;
        }
    }
    else if (args.DamageType == PlayerDamageEventType.Explosion)
    {
        // Thrown weapons (grenades, molotovs) cause explosion damage
        // Use the source position from the explosion
        // We'll consider it from front if no specific direction can be determined
        // For simplicity, check if any thrown weapon is near the player
        Vector2 playerPos = player.GetWorldPosition();
        
        // Simple heuristic: if explosion is in front, block it
        // This is a simplified check - you might want to enhance this
        hasSource = true;
        sourcePos = playerPos + new Vector2(player.FacingDirection * 10f, 0); // Assume front
    }
    
    // If source is from front, refund damage
    if (hasSource && IsArmoredInFront(player, sourcePos))
    {
        player.SetHealth(player.GetHealth() + args.Damage);
        
        // Shield body absorbed damage: cut energy by 20% of the damage amount
        if (args.Damage > 0f)
        {
            PlayerModifiers titanMods = player.GetModifiers();
            float energyDrain = args.Damage * ARMORED_SHIELD_ENERGY_DRAIN_RATIO;
            titanMods.CurrentEnergy = Math.Max(0f, titanMods.CurrentEnergy - energyDrain);
            player.SetModifiers(titanMods);
        }
        
        // Play Sparks effect on melee/projectile/explosion hit
        if (args.DamageType == PlayerDamageEventType.Melee || 
            args.DamageType == PlayerDamageEventType.Projectile ||
            args.DamageType == PlayerDamageEventType.Explosion)
        {
            Game.PlayEffect(EffectName.Sparks, player.GetWorldPosition());
        }
    }
}

private void ExtractTitanPower(IPlayer pureTitan, int pureTitanId, IPlayer victim, int victimId, TitanType extractedPower)
{
    Vector2 pureTitanPos = pureTitan.GetWorldPosition();
    int pureTitanFacing = pureTitan.FacingDirection;
    PlayerTeam pureTitanTeam = pureTitan.GetTeam();
    bool isBot = pureTitan.IsBot;
    string playerName = pureTitan.Name;
    string victimName = (victim != null) ? victim.Name : ""; // Capture now, victim may be removed by spawn time
    
    // Check if the Pure Titan is a temporary player
    bool isTemporaryPlayer = playerName.ToLower() == TEMPORARY_PLAYER_NAME.ToLower();
    
    // Get original profile and modifiers (human form)
    IProfile humanProfile = originalProfiles.ContainsKey(pureTitanId) ? originalProfiles[pureTitanId] : pureTitan.GetProfile();
    PlayerModifiers humanMods = originalModifiers.ContainsKey(pureTitanId) ? originalModifiers[pureTitanId] : new PlayerModifiers();
    
    // Get original bot behaviors
    BotBehavior humanBotBehavior = originalBotBehaviors.ContainsKey(pureTitanId) ? originalBotBehaviors[pureTitanId] : new BotBehavior(true, PredefinedAIType.BotA);
    BotBehaviorSet humanBotBehaviorSet = originalBotBehaviorSets.ContainsKey(pureTitanId) ? originalBotBehaviorSets[pureTitanId] : null;
    
    // Get user controller (for human players) before anything happens
    IUser user = pureTitan.GetUser();
    
    // 1. Detransform Pure Titan with smoke and destruction effect
    Game.PlayEffect(EffectName.Steam, pureTitanPos);
    Game.PlayEffect(EffectName.Steam, pureTitanPos);
    Game.PlaySound("BarrelDestroyed", pureTitanPos);
    
    // Remove old label immediately so it doesn't float above the corpse
    if (titanTypeLabels.ContainsKey(pureTitanId))
    {
        IObjectText label = titanTypeLabels[pureTitanId];
        if (label != null && !label.IsRemoved)
        {
            label.Remove();
        }
        titanTypeLabels.Remove(pureTitanId);
    }
    
    // Preserve old pure titan power before removing
    TitanType tempPureTitanPower = assignedTitanPowers.ContainsKey(pureTitanId) ? assignedTitanPowers[pureTitanId] : TitanType.Pure;
    assignedTitanPowers.Remove(pureTitanId);
    
    // Clean up all titan tracking data for this player BEFORE killing
    CleanupTitanData(pureTitanId);
    
    // Update shared storage immediately to notify odm.cs that titan is dead
    UpdateTitanPlayersStorage();
    
    // Prevent the game from declaring round-over during the 1500ms spawn delay
    // Use the same SuperDSCoopSync storage/keys as SuperDS.cs so CoopGameOver.cs handles it automatically
    Game.AutoVictoryConditionEnabled = false;
    IScriptStorage extractStorage = Game.GetSharedStorage("SuperDSCoopSync");
    extractStorage.SetItem("P2_SPLITTING", true);
    extractStorage.SetItem("P2_SPLIT_TIME", Game.TotalElapsedGameTime + 3000f); // 3s buffer
    
    // Kill the pure titan player immediately (leaving the titan corpse on the ground to steam)
    pureTitan.Kill();
    
    // Determine power name for chat message
    string powerName = "UNKNOWN";
    if (extractedPower == TitanType.Attack) powerName = "ATTACK TITAN";
    else if (extractedPower == TitanType.Jaw) powerName = "JAW TITAN";
    else if (extractedPower == TitanType.Armored) powerName = "ARMORED TITAN";
    
    // The detransformed human spawns after 1500ms of the titan body being killed
    Events.UpdateCallback.Start((float dt) =>
    {
        // Calculate ground position where corpse landed
        Vector2 corpsePos = (pureTitan != null) ? pureTitan.GetWorldPosition() : pureTitanPos;
        Area corpseArea = (pureTitan != null) ? pureTitan.GetAABB() : new Area(corpsePos.Y + 10f, corpsePos.X - 10f, corpsePos.Y - 10f, corpsePos.X + 10f);
        Vector2 spawnPos = corpsePos;
        
        if (pureTitan != null && pureTitan.IsOnGround)
        {
            spawnPos = new Vector2(corpsePos.X, corpseArea.Bottom + 10f);
        }
        else
        {
            RayCastInput rcInput = new RayCastInput(true)
            {
                ClosestHitOnly = true,
                MaskBits = 0x0001,
                FilterOnMaskBits = true
            };
            RayCastResult[] hits = Game.RayCast(corpsePos, corpsePos - new Vector2(0f, 200f), rcInput);
            if (hits.Length > 0 && hits[0].Hit)
            {
                spawnPos = new Vector2(corpsePos.X, hits[0].Position.Y + 10f);
            }
            else
            {
                spawnPos = new Vector2(corpsePos.X, corpseArea.Bottom + 10f);
            }
        }
        
        // Steam effect at spawn position
        Game.PlayEffect(EffectName.Steam, spawnPos);
        
        // Fire the trigger itself to spawn the player with the zombie emerging animation
        IPlayer humanPlayer = null;
        if (zombieSpawnTrigger != null)
        {
            zombieSpawnTrigger.SetWorldPosition(spawnPos);
            zombieSpawnTrigger.SetFaceDirection(pureTitanFacing);
            zombieSpawnTrigger.SetEnabled(true);
            zombieSpawnTrigger.Trigger();
            humanPlayer = zombieSpawnTrigger.GetLastCreatedPlayer();
        }
        if (humanPlayer == null)
        {
            humanPlayer = Game.CreatePlayer(spawnPos);
        }
        if (humanPlayer == null)
        {
            return;
        }
        
        int humanPlayerId = humanPlayer.UniqueID;
        
        // Set team
        humanPlayer.SetTeam(pureTitanTeam);
        
        // Set the human profile
        humanPlayer.SetProfile(humanProfile);
        
        // Set bot name if bot
        if (isBot)
        {
            humanPlayer.SetBotName(playerName);
        }
        
        // Re-attach user controller for human players (temporarily disable input so emerging animation plays without key interruption)
        if (user != null)
        {
            humanPlayer.SetUser(user);
            humanPlayer.SetInputEnabled(false);
        }
        
        // Set full HP and full energy from original modifiers
        if (humanMods.MaxHealth <= 0) humanMods.MaxHealth = 100;
        if (humanMods.MaxEnergy <= 0) humanMods.MaxEnergy = 100;
        humanMods.CurrentHealth = humanMods.MaxHealth;
        humanMods.CurrentEnergy = humanMods.MaxEnergy;
        humanPlayer.SetModifiers(humanMods);
        
        // Set facing direction directly without queuing a command that could cancel the emergence animation
        humanPlayer.SetFaceDirection(pureTitanFacing);
        
        // After 1000ms, restore the real behavior for both bots and human players
        Events.UpdateCallback.Start((float dt2) =>
        {
            if (humanPlayer == null || humanPlayer.IsDead || humanPlayer.IsRemoved) return;
            
            if (isBot)
            {
                humanPlayer.SetBotBehavior(humanBotBehavior);
                if (humanBotBehaviorSet != null)
                {
                    humanBotBehaviorSet.PowerupUsage = false;
                    humanPlayer.SetBotBehaviorSet(humanBotBehaviorSet);
                }
            }
            else
            {
                humanPlayer.SetBotBehavior(new BotBehavior(false, PredefinedAIType.None));
                humanPlayer.SetBotBehaviorActive(false);
                if (user != null)
                {
                    humanPlayer.SetUser(user);
                    humanPlayer.SetInputEnabled(true);
                }
            }
        }, 1000, 1);
        
        // Re-enable auto-victory now that the human player has spawned successfully
        Game.AutoVictoryConditionEnabled = true;
        IScriptStorage postExtractStorage = Game.GetSharedStorage("SuperDSCoopSync");
        postExtractStorage.SetItem("P2_SPLITTING", false);
        
        // Mark as already given startup strength boost so they don't immediately get another one
        givenStartupStrengthBoost.Add(humanPlayerId);
        
        // Clean up old original data references
        originalProfiles.Remove(pureTitanId);
        originalModifiers.Remove(pureTitanId);
        originalBotBehaviors.Remove(pureTitanId);
        originalBotBehaviorSets.Remove(pureTitanId);
        
        // Update name mapping
        playerNameToId[playerName] = humanPlayerId;
        playerIdToName[humanPlayerId] = playerName;
        
        // Ensure the new bot gets PowerupUsage disabled
        if (isBot && !configuredBotPowerupUsage.Contains(humanPlayerId))
        {
            BotBehaviorSet bS = humanPlayer.GetBotBehaviorSet();
            bS.PowerupUsage = false;
            humanPlayer.SetBotBehaviorSet(bS);
            configuredBotPowerupUsage.Add(humanPlayerId);
        }
        
        // 2. Handle power distribution based on whether extractor is temporary
        if (isTemporaryPlayer)
        {
            // Temporary player: Give power to random Pure Titan teammate
            IPlayer randomTeammatePureTitan = GetRandomPureTitanTeammate(humanPlayer);
            
            if (randomTeammatePureTitan != null)
            {
                int teammateId = randomTeammatePureTitan.UniqueID;
                if (assignedTitanPowers.ContainsKey(teammateId))
                {
                    TitanType teammatePower = assignedTitanPowers[teammateId];
                    assignedTitanPowers[teammateId] = extractedPower;
                    assignedTitanPowers[victimId] = teammatePower;
                    assignedTitanPowers[humanPlayerId] = TitanType.Pure;
                    
                    UpdateLabelsAfterPowerSwap(teammateId, victimId);
                    CreateLabelForPlayer(humanPlayer, humanPlayerId, TitanType.Pure);
                    
                    if (distributionMode == "A")
                        playerPowerExpiry[randomTeammatePureTitan.Name] = GetUnixDay() + 2;
                    else
                        playerPowerExpiry[randomTeammatePureTitan.Name] = GetUnixMinute() + 30L;

                    if (!string.IsNullOrEmpty(victimName))
                    {
                        playerPowerExpiry.Remove(victimName);
                        RemoveStoredPlayerExpiry(victimName);
                    }
                    SaveExpiryToStorage();
                    
                    Game.ShowChatMessage(playerName + " extracted the " + powerName + " power and transferred it to " + randomTeammatePureTitan.Name + "!", Color.Yellow);
                }
            }
            else
            {
                assignedTitanPowers[humanPlayerId] = TitanType.Pure;
                CreateLabelForPlayer(humanPlayer, humanPlayerId, TitanType.Pure);
                Game.ShowChatMessage(playerName + " extracted a power but had no teammate to transfer it to!", Color.Red);
            }
        }
        else
        {
            // Normal player: Standard power swap
            assignedTitanPowers[humanPlayerId] = extractedPower; // Extractor gets humanoid power
            assignedTitanPowers[victimId] = tempPureTitanPower; // Victim gets Pure Titan power
            
            // Create label for the new human player
            CreateLabelForPlayer(humanPlayer, humanPlayerId, extractedPower);
            
            // Update victim label if still around
            if (victim != null && !victim.IsRemoved && assignedTitanPowers.ContainsKey(victimId))
            {
                CreateLabelForPlayer(victim, victimId, tempPureTitanPower);
            }
            
            if (distributionMode == "A")
                playerPowerExpiry[playerName] = GetUnixDay() + 2;
            else
                playerPowerExpiry[playerName] = GetUnixMinute() + 30L;

            if (!string.IsNullOrEmpty(victimName))
            {
                playerPowerExpiry.Remove(victimName);
                RemoveStoredPlayerExpiry(victimName);
            }
            SaveExpiryToStorage();
            
            Game.ShowChatMessage(playerName + " extracted the " + powerName + " power from " + victim.Name + "!", Color.Yellow);
        }
        
        // Save updated powers
        SaveAssignedPowersToStorage();
        
        // Additionally force-write the victim's power by name directly, since Game.GetPlayer(victimId)
        // returns null for dead/removed players
        if (!string.IsNullOrEmpty(victimName) && victimName.ToLower() != TEMPORARY_PLAYER_NAME.ToLower())
        {
            TitanType victimNewPower = assignedTitanPowers.ContainsKey(victimId) ? assignedTitanPowers[victimId] : TitanType.Pure;
            SetStoredPlayerPower(victimName, victimNewPower);
        }
        
        // Update shared storage
        UpdateTitanPlayersStorage();
    }, DETACH_SPAWN_DELAY_MS, 1);
}

private void UpdateLabelsAfterPowerSwap(int player1Id, int player2Id)
{
    // Remove old labels
    if (titanTypeLabels.ContainsKey(player1Id))
    {
        IObjectText label = titanTypeLabels[player1Id];
        if (label != null && !label.IsRemoved)
        {
            label.Remove();
        }
        titanTypeLabels.Remove(player1Id);
    }
    
    if (titanTypeLabels.ContainsKey(player2Id))
    {
        IObjectText label = titanTypeLabels[player2Id];
        if (label != null && !label.IsRemoved)
        {
            label.Remove();
        }
        titanTypeLabels.Remove(player2Id);
    }
    
    // Create new labels based on new powers
    IPlayer player1 = Game.GetPlayer(player1Id);
    IPlayer player2 = Game.GetPlayer(player2Id);
    
    if (player1 != null && !player1.IsRemoved && assignedTitanPowers.ContainsKey(player1Id))
    {
        CreateLabelForPlayer(player1, player1Id, assignedTitanPowers[player1Id]);
    }
    
    // Player2 might be dead/removed, only create label if they exist
    if (player2 != null && !player2.IsRemoved && assignedTitanPowers.ContainsKey(player2Id))
    {
        CreateLabelForPlayer(player2, player2Id, assignedTitanPowers[player2Id]);
    }
}

private IPlayer GetRandomPureTitanTeammate(IPlayer player)
{
    // Find all Pure Titan teammates (not transformed, just assigned Pure power)
    PlayerTeam playerTeam = player.GetTeam();
    List<IPlayer> pureTitanTeammates = new List<IPlayer>();
    
    IPlayer[] allPlayers = Game.GetPlayers();
    foreach (IPlayer p in allPlayers)
    {
        if (p == null || p.IsDead || p.UniqueID == player.UniqueID) continue;
        
        // Check if on same team
        PlayerTeam pTeam = p.GetTeam();
        if (playerTeam == PlayerTeam.Independent)
        {
            // Independent players have no teammates
            continue;
        }
        
        if (pTeam != playerTeam) continue;
        
        // Check if has Pure Titan power assigned
        if (assignedTitanPowers.ContainsKey(p.UniqueID) && 
            assignedTitanPowers[p.UniqueID] == TitanType.Pure)
        {
            pureTitanTeammates.Add(p);
        }
    }
    
    // Return random teammate if any found
    if (pureTitanTeammates.Count > 0)
    {
        int randomIndex = rnd.Next(0, pureTitanTeammates.Count);
        return pureTitanTeammates[randomIndex];
    }
    
    return null;
}

private void MakeNearbyPlayersFall(Vector2 explosionPos, float radius, int titanPlayerId)
{
    IPlayer[] allPlayers = Game.GetPlayers();
    
    foreach (IPlayer player in allPlayers)
    {
        if (player == null || player.IsDead) continue;
        if (player.UniqueID == titanPlayerId) continue;
        
        // Skip if already a titan
        if (titanPlayers.ContainsKey(player.UniqueID))
        {
            continue;
        }
        
        Vector2 playerPos = player.GetWorldPosition();
        float distance = Vector2.Distance(explosionPos, playerPos);
        
        if (distance <= radius)
        {
            // Make player fall
            QuickCommand(player, new PlayerCommand(PlayerCommandType.Fall));
        }
    }
}

public void OnUpdate(float elapsed)
{
    IPlayer[] allPlayers = Game.GetPlayers();
    float currentTime = Game.TotalElapsedRealTime;
    
    // Update titan type labels to follow players
    UpdateTitanTypeLabels();
    
    // Check power expiry (Scenario A: 2-day; Scenario B: handled at match start)
    CheckPowerExpiry(currentTime);
    
    foreach (IPlayer player in allPlayers)
    {
        if (player == null || player.IsDead) continue;
        
        int playerId = player.UniqueID;
        
        // Ensure humanoid titan owners get startup strength boost if not yet received
        if (assignedTitanPowers.ContainsKey(playerId) && assignedTitanPowers[playerId] != TitanType.Pure)
        {
            if (!givenStartupStrengthBoost.Contains(playerId))
            {
                player.GiveWeaponItem(WeaponItem.STRENGTHBOOST);
                givenStartupStrengthBoost.Add(playerId);
            }
        }
        
        // Only disable PowerupUsage after a bot has picked up or received a strength boost
        // (If disabled beforehand, bots won't seek/pick up strength boosts and default powerup usage won't work)
        bool hasStrengthBoostItem = player.CurrentPowerupItem.WeaponItem == WeaponItem.STRENGTHBOOST;
        if (player.IsBot && (hasStrengthBoostItem || player.GetStrengthBoostTime() > 0) && !configuredBotPowerupUsage.Contains(playerId))
        {
            BotBehaviorSet bS = player.GetBotBehaviorSet();
            bS.PowerupUsage = false;
            player.SetBotBehaviorSet(bS);
            configuredBotPowerupUsage.Add(playerId);
        }
        
        // Check bot strength boost activation based on HP percentage
        CheckBotStrengthBoostActivation(player, playerId, currentTime);
        
        // Initialize tracking
        if (!playerWasOnGround.ContainsKey(playerId))
        {
            playerWasOnGround[playerId] = player.IsOnGround;
        }
        
        if (!playerHadStrengthBoost.ContainsKey(playerId))
        {
            playerHadStrengthBoost[playerId] = false;
        }
        
        // Check if player just picked up strength boost
        bool hasStrengthBoost = player.GetStrengthBoostTime() > 0;
        bool hadStrengthBoost = playerHadStrengthBoost[playerId];
        
        if (hasStrengthBoost && !hadStrengthBoost)
        {
            // Player just picked up strength boost - transform!
            TransformToTitan(player);
        }
        
        playerHadStrengthBoost[playerId] = hasStrengthBoost;
        
        // Check titan-specific updates
        if (titanPlayers.ContainsKey(playerId))
        {
            TitanType titanType = titanPlayers[playerId];
            
            if (titanType == TitanType.Pure)
            {
                // Pure Titan updates
                CheckTitanExhaustion(player, playerId);
                CheckTitanGrabDamage(player, playerId);
            }
            else if (titanType == TitanType.Attack)
            {
                // Attack Titan updates
                CheckAttackTitanSlowmo(player, playerId, currentTime);
                CheckAttackTitanHardeningEnergy(player, playerId, currentTime);
                CheckAttackTitanBotHardening(player, playerId, currentTime);
                
                // Attack Titan also has exhaustion
                CheckTitanExhaustion(player, playerId);
            }
            else if (titanType == TitanType.Jaw)
            {
                // Jaw Titan updates
                CheckJawTitanHuntDamage(player, playerId, currentTime);
                CheckTitanGrabDamage(player, playerId); // Jaw Titan also has grab damage
                
                // Jaw Titan also has exhaustion
                CheckTitanExhaustion(player, playerId);
                
                // Bot backstab targeting
                if (player.IsBot)
                {
                    CheckJawTitanBotTargeting(player, playerId, currentTime);
                }
            }
            else if (titanType == TitanType.Armored)
            {
                // Armored Titan updates
                CheckArmoredTitanBotArmor(player, playerId, currentTime);
                CheckArmoredTitanRush(player, playerId, currentTime);
                CheckArmoredTitanBotRush(player, playerId, currentTime);
                // No need this, it's already done in OnPlayerDamage
                // CheckArmoredTitanArmor(player, playerId, currentTime);
                
                // Armored Titan also has exhaustion
                CheckTitanExhaustion(player, playerId);
            }
            else if (titanType == TitanType.Cart)
            {
                // Cart Titan updates
                CartTitanUpdate(player, playerId, currentTime);
                
                // Cart Titan also has exhaustion
                CheckTitanExhaustion(player, playerId);
            }
            
            // Bot detach check: non-Pure bot titans may detach based on HP percentage
            if (player.IsBot && titanType != TitanType.Pure)
            {
                CheckBotDetach(player, playerId, currentTime);
            }
            else if (!player.IsBot && titanType != TitanType.Pure)
            {
                // Human detach check: hold walk for 800ms
                CheckHumanDetach(player, playerId, currentTime);
            }
            
            // Melee stun immunity: titans can stun each other but remain immune to regular players
            // If another titan is actively meleeing this titan in range → vulnerable (0)
            // Otherwise → immune (1)
            bool underTitanMeleeThreat = IsUnderMeleeThreatOfTitan(player);
            PlayerModifiers threatMods = player.GetModifiers();
            int desiredImmunity = underTitanMeleeThreat ? 0 : 1;
            if (threatMods.MeleeStunImmunity != desiredImmunity)
            {
                threatMods.MeleeStunImmunity = desiredImmunity;
                player.SetModifiers(threatMods);
            }
        }
        
        // Update facing direction for all players (every update tick for accurate backstab detection)
        playerFacingDirections[playerId] = player.FacingDirection;
        
        // Check if this player needs input re-enabled after Jaw Titan hunt
        if (jawHuntVictimReenableTime.ContainsKey(playerId))
        {
            if (currentTime >= jawHuntVictimReenableTime[playerId])
            {
                if (!player.IsDead)
                {
                    player.AddCommand(new PlayerCommand(PlayerCommandType.StopCrouch));
                    player.SetInputEnabled(true);
                }
                jawHuntVictimReenableTime.Remove(playerId);
            }
        }
    }
}

private void CheckJawTitanHuntDamage(IPlayer jawTitan, int jawTitanId, float currentTime)
{
    // Check for new dive catches
    IPlayer[] allPlayers = Game.GetPlayers();
    
    // Look for victims caught in dive by this Jaw Titan
    foreach (IPlayer potentialVictim in allPlayers)
    {
        if (potentialVictim == null || potentialVictim.IsDead || potentialVictim.UniqueID == jawTitanId) continue;
        
        // Check if victim is caught in dive by this Jaw Titan
        if (potentialVictim.CaughtByPlayerInDiveID == jawTitanId)
        {
            // Start tracking this victim if not already tracked
            if (!jawTitanHuntVictim.ContainsKey(jawTitanId))
            {
                jawTitanHuntVictim[jawTitanId] = potentialVictim;
                jawTitanHuntDamageTime[jawTitanId] = currentTime;
                jawTitanHuntBloodEffectTime[jawTitanId] = currentTime; // Initialize blood effect timer
            }
            break;
        }
    }
    
    // Check if Jaw Titan has a hunt victim
    if (!jawTitanHuntVictim.ContainsKey(jawTitanId))
    {
        return;
    }
    
    IPlayer victim = jawTitanHuntVictim[jawTitanId];
    
    // Check if victim is still valid
    if (victim == null || victim.IsDead || victim.IsRemoved)
    {
        jawTitanHuntVictim.Remove(jawTitanId);
        jawTitanHuntDamageTime.Remove(jawTitanId);
        jawTitanHuntBloodEffectTime.Remove(jawTitanId);
        return;
    }
    
    // Check if Jaw Titan is still diving/catching the victim
    if (victim.CaughtByPlayerInDiveID != jawTitanId)
    {
        // Hunt ended - disable victim's input and crouch until stun ends
        victim.SetInputEnabled(false);
        victim.AddCommand(new PlayerCommand(PlayerCommandType.StartCrouch));
        
        // Track when to re-enable (will be checked in OnUpdate)
        float reenableTime = currentTime + JAW_HUNT_VICTIM_STUN_DURATION;
        jawHuntVictimReenableTime[victim.UniqueID] = reenableTime;
        
        // Clean up hunt tracking
        jawTitanHuntVictim.Remove(jawTitanId);
        jawTitanHuntDamageTime.Remove(jawTitanId);
        jawTitanHuntBloodEffectTime.Remove(jawTitanId);
        return;
    }
    
    // Apply damage every 50ms (same as grab damage)
    if (!jawTitanHuntDamageTime.ContainsKey(jawTitanId))
    {
        jawTitanHuntDamageTime[jawTitanId] = currentTime;
        jawTitanHuntBloodEffectTime[jawTitanId] = currentTime; // Initialize blood effect timer
    }
    
    if (currentTime >= jawTitanHuntDamageTime[jawTitanId])
    {
        // Calculate damage (2.5x as grab damage)
        PlayerModifiers jawMods = jawTitan.GetModifiers();
        float damagePerInterval = GRAB_DAMAGE_BASE * jawMods.MeleeDamageDealtModifier * (GRAB_DAMAGE_MULTIPLIER * 2.5f);
        
        // Check victim health before dealing damage
        PlayerModifiers victimMods = victim.GetModifiers();
        bool willDie = victimMods.CurrentHealth <= damagePerInterval;
        
        // Deal damage
        victim.DealDamage(damagePerInterval);
        
        // Play blood effect on victim (only once per second)
        if (currentTime >= jawTitanHuntBloodEffectTime[jawTitanId])
        {
            Vector2 victimPos = victim.GetWorldPosition();
            Game.PlayEffect(EffectName.Blood, victimPos);
            jawTitanHuntBloodEffectTime[jawTitanId] = currentTime + GRAB_BLOOD_EFFECT_INTERVAL;
        }
        
        // If victim died from hunt damage, gib them
        if (willDie && victim.IsDead)
        {
            victim.Gib();
        }
        
        // Update next damage time
        jawTitanHuntDamageTime[jawTitanId] = currentTime + GRAB_DAMAGE_INTERVAL;
    }
}

// Checks and updates the Jaw Titan bot's forced target using backstab-priority logic:
// Priority 1 (global, out-of-combat): closest enemy with back exposed
// Priority 2 (in-combat, range 50f): any enemy with back exposed
// Priority 3 (in-combat, none in range): keep current target
// Priority 4 (out-of-combat 3s+, none in range): fall back to priority 1
private void CheckJawTitanBotTargeting(IPlayer jawBot, int jawBotId, float currentTime)
{
    // Rate-limit evaluations
    if (jawBotLastTargetCheck.ContainsKey(jawBotId) &&
        currentTime - jawBotLastTargetCheck[jawBotId] < JAW_BOT_TARGETING_INTERVAL_MS)
    {
        return;
    }
    jawBotLastTargetCheck[jawBotId] = currentTime;
    
    PlayerTeam jawTeam = jawBot.GetTeam();
    Vector2 jawPos = jawBot.GetWorldPosition();
    int jawFacing = jawBot.FacingDirection;
    
    // Build list of alive enemies
    IPlayer[] enemies = Game.GetPlayers().Where(p =>
        p != null &&
        !p.IsDead &&
        p.UniqueID != jawBotId &&
        p.GetTeam() != jawTeam
    ).ToArray();
    
    if (enemies.Length == 0)
    {
        jawBot.SetForcedBotTarget(null);
        jawBotCurrentTarget.Remove(jawBotId);
        return;
    }
    
    // A target has its back exposed when it faces the SAME direction as the Jaw Titan
    // (jaw is behind them — they face away from jaw)
    // We use playerFacingDirections (updated every tick) for accuracy
    
    // Determine in-combat state: was a melee hit registered recently?
    bool isInCombat = jawBotLastCombatTime.ContainsKey(jawBotId) &&
                      (currentTime - jawBotLastCombatTime[jawBotId]) < JAW_BOT_COMBAT_TIMEOUT_MS;
    
    // --- Priority 2 / 3: In-combat ---
    if (isInCombat)
    {
        // Look for backstab target within combat range
        IPlayer backstabTarget = null;
        float closestDist = float.MaxValue;
        
        foreach (IPlayer enemy in enemies)
        {
            float dist = Vector2.Distance(jawPos, enemy.GetWorldPosition());
            if (dist > JAW_BOT_BACKSTAB_COMBAT_RANGE) continue;
            
            // Back exposed = enemy faces same direction as the Jaw Titan
            int enemyFacing = playerFacingDirections.ContainsKey(enemy.UniqueID)
                ? playerFacingDirections[enemy.UniqueID]
                : enemy.FacingDirection;
            
            if (enemyFacing == jawFacing && dist < closestDist)
            {
                closestDist = dist;
                backstabTarget = enemy;
            }
        }
        
        if (backstabTarget != null)
        {
            // Priority 2: found a backstab target in range — switch to it
            jawBotCurrentTarget[jawBotId] = backstabTarget;
            jawBot.SetForcedBotTarget(backstabTarget);
        }
        // else Priority 3: keep current target unchanged
        
        return;
    }
    
    // --- Priority 1 / 4: Out-of-combat (or timed out) ---
    // Find globally closest enemy with back exposed
    IPlayer globalBackstab = null;
    float globalClosestDist = float.MaxValue;
    
    foreach (IPlayer enemy in enemies)
    {
        int enemyFacing = playerFacingDirections.ContainsKey(enemy.UniqueID)
            ? playerFacingDirections[enemy.UniqueID]
            : enemy.FacingDirection;
        
        if (enemyFacing != jawFacing) continue; // Not a backstab opportunity
        
        float dist = Vector2.Distance(jawPos, enemy.GetWorldPosition());
        if (dist < globalClosestDist)
        {
            globalClosestDist = dist;
            globalBackstab = enemy;
        }
    }
    
    if (globalBackstab != null)
    {
        jawBotCurrentTarget[jawBotId] = globalBackstab;
        jawBot.SetForcedBotTarget(globalBackstab);
    }
    else
    {
        // No backstab opportunity at all — let AI choose freely
        jawBot.SetForcedBotTarget(null);
        jawBotCurrentTarget.Remove(jawBotId);
    }
}

private void CheckAttackTitanSlowmo(IPlayer player, int playerId, float currentTime)
{
    // Check if slowmo should end
    if (attackTitanSlowmoEndTime.ContainsKey(playerId) && attackTitanSlowmoEndTime[playerId] > 0)
    {
        if (currentTime >= attackTitanSlowmoEndTime[playerId])
        {
            Game.RunCommand("/settime 1");
            attackTitanSlowmoEndTime[playerId] = 0;
        }
    }
}

private void CheckAttackTitanHardeningEnergy(IPlayer player, int playerId, float currentTime)
{
    // Only apply energy cost if in hardening mode (not neutral)
    HardeningMode mode = HardeningMode.Neutral;
    if (!attackTitanHardeningMode.ContainsKey(playerId))
    {
        return;
    }
    
    mode = attackTitanHardeningMode[playerId];
    if (mode == HardeningMode.Neutral)
    {
        return;
    }
    
    // Calculate energy cost per tick (50ms = 0.05 seconds)
    PlayerModifiers mods = player.GetModifiers();
    float energyCostPerTick = (mods.MaxEnergy * HARDENING_ENERGY_COST_PER_SECOND) * 0.05f;
    
    // Deduct energy
    mods.CurrentEnergy -= energyCostPerTick;
    
    // If energy drops to 0 or below, switch back to neutral
    if (mods.CurrentEnergy <= 0)
    {
        mods.CurrentEnergy = 0;
        player.SetModifiers(mods); // Set energy to 0 first
        ApplyAttackTitanHardening(player, playerId, HardeningMode.Neutral); // Then switch mode (will handle modifiers properly)
    }
    else
    {
        // Only set modifiers if we're not switching modes
        player.SetModifiers(mods);
    }
}

private void CheckAttackTitanBotHardening(IPlayer player, int playerId, float currentTime)
{
    // Only for bots
    if (!player.IsBot) return;
    
    // Get current mode
    HardeningMode currentMode = HardeningMode.Neutral;
    if (attackTitanHardeningMode.ContainsKey(playerId))
    {
        currentMode = attackTitanHardeningMode[playerId];
    }
    
    // Get player modifiers for energy check
    PlayerModifiers mods = player.GetModifiers();
    float energyPercent = mods.CurrentEnergy / (float)mods.MaxEnergy;
    
    // Check if should avoid triggering due to low energy
    bool shouldAvoidTrigger = false;
    if (energyPercent < HARDENING_LOW_ENERGY_THRESHOLD)
    {
        if (currentMode == HardeningMode.Neutral)
        {
            // 80% chance to avoid switching to a mode
            shouldAvoidTrigger = rnd.NextDouble() < HARDENING_AVOID_TRIGGER_CHANCE;
        }
        else
        {
            // 60% chance to switch back to neutral
            if (rnd.NextDouble() < HARDENING_SWITCH_TO_NEUTRAL_CHANCE)
            {
                ApplyAttackTitanHardening(player, playerId, HardeningMode.Neutral);
                return;
            }
        }
    }
    
    if (shouldAvoidTrigger) return;
    
    // Check offensive mode trigger: target is close (25f)
    IObject target = player.GetBotTarget();
    if (target != null && !target.IsRemoved)
    {
        Vector2 playerPos = player.GetWorldPosition();
        Vector2 targetPos = target.GetWorldPosition();
        float distance = Vector2.Distance(playerPos, targetPos);
        
        if (distance <= HARDENING_OFFENSIVE_TRIGGER_DISTANCE)
        {
            // Switch to offensive mode
            if (currentMode != HardeningMode.Offensive)
            {
                ApplyAttackTitanHardening(player, playerId, HardeningMode.Offensive);
                attackTitanLastMeleeTime[playerId] = currentTime;
            }
        }
        else if (currentMode == HardeningMode.Offensive)
        {
            // Check if stopped melee for 2 seconds
            if (attackTitanLastMeleeTime.ContainsKey(playerId))
            {
                float timeSinceLastMelee = currentTime - attackTitanLastMeleeTime[playerId];
                if (timeSinceLastMelee >= HARDENING_OFFENSIVE_MELEE_TIMEOUT)
                {
                    ApplyAttackTitanHardening(player, playerId, HardeningMode.Neutral);
                }
            }
        }
    }
    else if (currentMode == HardeningMode.Offensive)
    {
        // No target, check if stopped melee for 2 seconds
        if (attackTitanLastMeleeTime.ContainsKey(playerId))
        {
            float timeSinceLastMelee = currentTime - attackTitanLastMeleeTime[playerId];
            if (timeSinceLastMelee >= HARDENING_OFFENSIVE_MELEE_TIMEOUT)
            {
                ApplyAttackTitanHardening(player, playerId, HardeningMode.Neutral);
            }
        }
    }
    
    // Check defensive mode trigger: enemy aims at bot and no close target
    if (!attackTitanLastDefensiveCheck.ContainsKey(playerId) || 
        currentTime - attackTitanLastDefensiveCheck[playerId] >= HARDENING_DEFENSIVE_CHECK_INTERVAL)
    {
        attackTitanLastDefensiveCheck[playerId] = currentTime;
        
        bool enemyAimingAtBot = false;
        bool hasCloseTarget = false;
        
        // Check if target is close
        if (target != null && !target.IsRemoved)
        {
            Vector2 playerPos = player.GetWorldPosition();
            Vector2 targetPos = target.GetWorldPosition();
            float distance = Vector2.Distance(playerPos, targetPos);
            
            if (distance <= HARDENING_OFFENSIVE_TRIGGER_DISTANCE)
            {
                hasCloseTarget = true;
            }
        }
        
        // Check if any enemy is aiming at this bot
        if (!hasCloseTarget)
        {
            Vector2 botPos = player.GetWorldPosition();
            PlayerTeam botTeam = player.GetTeam();
            
            // Get all players for checking
            IPlayer[] allPlayersForCheck = Game.GetPlayers();
            
            foreach (IPlayer otherPlayer in allPlayersForCheck)
            {
                if (otherPlayer == null || otherPlayer.IsDead || otherPlayer.UniqueID == playerId) continue;
                
                PlayerTeam otherTeam = otherPlayer.GetTeam();
                if (otherTeam == botTeam || otherTeam == PlayerTeam.Independent) continue;
                
                // Use raycast to check if other player is aiming at this bot
                IPlayer aimedTarget = GetAimedPlayer(otherPlayer);
                if (aimedTarget != null && aimedTarget.UniqueID == playerId)
                {
                    enemyAimingAtBot = true;
                    break;
                }
            }
        }
        
        // Apply defensive mode if conditions met
        if (enemyAimingAtBot && !hasCloseTarget)
        {
            if (currentMode != HardeningMode.Defensive)
            {
                ApplyAttackTitanHardening(player, playerId, HardeningMode.Defensive);
            }
        }
        else if (currentMode == HardeningMode.Defensive)
        {
            // Conditions no longer true, switch back to neutral
            ApplyAttackTitanHardening(player, playerId, HardeningMode.Neutral);
        }
    }
}

private void ToggleArmoredTitanArmor(IPlayer player, int playerId)
{
    // Get current armor state
    bool armorOn = true;
    if (armoredTitanArmorOn.ContainsKey(playerId))
    {
        armorOn = armoredTitanArmorOn[playerId];
    }
    
    // Toggle armor
    ApplyArmoredTitanArmor(player, playerId, !armorOn);
}

private void ApplyArmoredTitanArmor(IPlayer player, int playerId, bool armorOn)
{
    // oldArmorState defaults to false when no key exists yet (meaning: no prior effects were applied)
    bool oldArmorState = armoredTitanArmorOn.ContainsKey(playerId) ? armoredTitanArmorOn[playerId] : false;
    
    // Get modifiers
    PlayerModifiers mods = player.GetModifiers();
    
    // Remove old armor state effects
    if (!oldArmorState)
    {
        // Was OFF: remove speed and energy regen bonuses
        mods.RunSpeedModifier /= (1f + ARMORED_ARMOR_OFF_SPEED_BONUS);
        mods.SprintSpeedModifier /= (1f + ARMORED_ARMOR_OFF_SPEED_BONUS);
        mods.EnergyRechargeModifier /= (1f + ARMORED_ARMOR_OFF_ENERGY_REGEN_BONUS);
    }
    else
    {
        // Was ON: remove energy regen penalty
        mods.EnergyRechargeModifier *= (1f + ARMORED_ARMOR_ON_ENERGY_REGEN_PENALTY);
    }
    
    // Apply new armor state effects
    if (!armorOn)
    {
        // Turning OFF: add speed and energy regen bonuses
        mods.RunSpeedModifier *= (1f + ARMORED_ARMOR_OFF_SPEED_BONUS);
        mods.SprintSpeedModifier *= (1f + ARMORED_ARMOR_OFF_SPEED_BONUS);
        mods.EnergyRechargeModifier *= (1f + ARMORED_ARMOR_OFF_ENERGY_REGEN_BONUS);
        
        // Set armor OFF profile
        player.SetProfile(new IProfile()
        {
            Gender = Gender.Male,
            Skin = new IProfileClothingItem("Normal", "Skin5", "ClothingLightGray"),
            Head = new IProfileClothingItem("Buzzcut", "ClothingLightGray"),
        });
    }
    else
    {
        // Turning ON: apply energy regen penalty
        mods.EnergyRechargeModifier /= (1f + ARMORED_ARMOR_ON_ENERGY_REGEN_PENALTY);
        
        // Set armor ON profile
        player.SetProfile(new IProfile()
        {
            Gender = Gender.Male,
            Skin = new IProfileClothingItem("Normal", "Skin5", "ClothingLightGray"),
            Head = new IProfileClothingItem("Buzzcut", "ClothingLightGray"),
            ChestUnder = new IProfileClothingItem("BodyArmor", "ClothingLightGray"),
            Hands = new IProfileClothingItem("Gloves", "ClothingLightGray"),
            Legs = new IProfileClothingItem("Pants", "ClothingLightGray"),
            Feet = new IProfileClothingItem("Boots", "ClothingLightGray"),
            Accesory = new IProfileClothingItem("ClownMakeup", "ClothingLightGray"),
        });
    }
    
    player.SetModifiers(mods);
    
    // Update armor state
    armoredTitanArmorOn[playerId] = armorOn;
}

private void CheckArmoredTitanArmor(IPlayer player, int playerId, float currentTime)
{
    // Only process if armor is ON
    if (!armoredTitanArmorOn.ContainsKey(playerId) || !armoredTitanArmorOn[playerId])
    {
        return;
    }
    
    // Check for front melee and projectile threats to apply damage immunity
    bool meleeThreat = IsArmoredFrontMeleeThreat(player, playerId);
    bool projThreat = IsArmoredFrontProjectileThreat(player, playerId);
    
    PlayerModifiers mods = player.GetModifiers();
    bool modsChanged = false;
    
    // Set damage modifiers based on threats
    float newMeleeDamageTaken = meleeThreat ? 0f : -2f; // 0 = immune, -2 = reset to default
    float newProjDamageTaken = projThreat ? 0f : -2f;
    
    if (mods.MeleeDamageTakenModifier != newMeleeDamageTaken)
    {
        mods.MeleeDamageTakenModifier = newMeleeDamageTaken;
        modsChanged = true;
    }
    
    if (mods.ProjectileDamageTakenModifier != newProjDamageTaken)
    {
        mods.ProjectileDamageTakenModifier = newProjDamageTaken;
        modsChanged = true;
    }
    
    if (modsChanged)
    {
        player.SetModifiers(mods);
    }
}

private void CheckArmoredTitanBotArmor(IPlayer player, int playerId, float currentTime)
{
    // Only for bots
    if (!player.IsBot) return;
    
    // Don't turn armor on while exhausted
    if (titanExhausted.ContainsKey(playerId) && titanExhausted[playerId]) return;
    
    // Check armor state periodically
    if (!armoredTitanLastArmorCheck.ContainsKey(playerId) || 
        currentTime - armoredTitanLastArmorCheck[playerId] >= ARMORED_ARMOR_ON_CHECK_INTERVAL)
    {
        armoredTitanLastArmorCheck[playerId] = currentTime;
        
        bool shouldArmorOn = false;
        
        // Check if target is close
        IObject target = player.GetBotTarget();
        if (target != null && !target.IsRemoved)
        {
            Vector2 playerPos = player.GetWorldPosition();
            Vector2 targetPos = target.GetWorldPosition();
            float distance = Vector2.Distance(playerPos, targetPos);
            
            if (distance <= ARMORED_ARMOR_ON_TRIGGER_DISTANCE)
            {
                shouldArmorOn = true;
            }
        }
        
        // Check if any enemy is aiming at this bot
        if (!shouldArmorOn)
        {
            IPlayer aimedBy = GetPlayerAimingAtBot(player, playerId);
            if (aimedBy != null)
            {
                shouldArmorOn = true;
            }
        }
        
        // Apply armor state if it needs to change
        bool currentArmorOn = armoredTitanArmorOn.ContainsKey(playerId) ? armoredTitanArmorOn[playerId] : true;
        if (currentArmorOn != shouldArmorOn)
        {
            ApplyArmoredTitanArmor(player, playerId, shouldArmorOn);
        }
    }
}

private void CheckArmoredTitanRush(IPlayer player, int playerId, float currentTime)
{
    // Only process if player is sprinting
    if (!player.IsSprinting) return;
    
    // Rush ability is only available when armor is ON
    bool armorOn = armoredTitanArmorOn.ContainsKey(playerId) ? armoredTitanArmorOn[playerId] : true;
    if (!armorOn) return;
    
    // Get titan stats
    PlayerModifiers titanMods = player.GetModifiers();
    float titanForce = GetPlayerForce(player);
    float titanHeight = GetPlayerHeight(player);
    float titanPower = titanForce * titanHeight;
    int titanFacing = player.FacingDirection;
    PlayerTeam titanTeam = player.GetTeam();
    
    // Get titan collision area
    Area titanArea = player.GetAABB();
    
    // Get hit tracking for this titan
    Dictionary<int, float> hitTimes = armoredTitanRushHitTimes.ContainsKey(playerId) ? 
                                      armoredTitanRushHitTimes[playerId] : 
                                      new Dictionary<int, float>();
    
    // Check all players for collisions
    IPlayer[] allPlayers = Game.GetPlayers();
    foreach (IPlayer target in allPlayers)
    {
        if (target == null || target.IsDead || target.UniqueID == playerId) continue;
        
        // Skip teammates (but Independent players have no teammates except themselves)
        PlayerTeam targetTeam = target.GetTeam();
        if (titanTeam != PlayerTeam.Independent && titanTeam == targetTeam) continue;
        
        // Check collision
        if (!titanArea.Intersects(target.GetAABB())) continue;
        
        int targetId = target.UniqueID;
        
        // Check cooldown
        if (hitTimes.ContainsKey(targetId) && currentTime - hitTimes[targetId] < ARMORED_RUSH_HIT_COOLDOWN)
        {
            continue;
        }
        
        // Record hit time
        hitTimes[targetId] = currentTime;
        
        // Calculate throw power
        float targetForce = GetPlayerForce(target);
        float targetHeight = GetPlayerHeight(target);
        float targetPower = targetForce * targetHeight;
        
        float power = (titanPower - (targetPower * ARMORED_RUSH_TARGET_FORCE_WEIGHT)) * ARMORED_RUSH_BALANCER_MOD;
        power = Math.Max(power, 0f);
        
        // Apply throw
        if (power > 0)
        {
            float velocity = power * ARMORED_RUSH_THROW_SCALE;
            target.SetLinearVelocity(new Vector2(titanFacing * velocity, velocity * ARMORED_RUSH_THROW_UP_RATIO));
            
            // Make player fall while being thrown
            QuickCommand(target, new PlayerCommand(PlayerCommandType.Fall));
        }
        
        // Apply damage
        float damage = ARMORED_RUSH_DAMAGE_BASE * titanMods.MeleeDamageDealtModifier;
        target.DealDamage(damage);
    }
    
    // Check all objects for collisions and throw them
    IObject[] allObjects = Game.GetObjectsByArea(titanArea);
    foreach (IObject obj in allObjects)
    {
        if (obj == null || obj.IsRemoved) continue;
        
        // Only throw destructible objects
        if (!obj.Destructable) continue;
        
        // Check collision
        if (!titanArea.Intersects(obj.GetAABB())) continue;
        
        // Skip the titan player itself
        if (obj.UniqueID == playerId) continue;
        
        string objName = obj.Name;
        
        // Skip background/non-physical objects (mirrors ODM script filter)
        if (objName.StartsWith("Bg") || objName.StartsWith("BG")) continue;
        if (objName.StartsWith("SoundArea")) continue;
        if (objName.Contains("Trigger") || objName.Contains("Spawn") ||
            objName.Contains("Marker")  || objName.Contains("Ladder") ||
            objName.Contains("Joint")) continue;
        
        // Skip objects under the titan (e.g. under feet/being stood on) as throwing them launches them up into the titan
        if (obj.GetWorldPosition().Y < titanArea.Bottom || obj.GetAABB().Top <= titanArea.Bottom + 4f) continue;
        
        // Only throw objects in front of titan in rush direction
        float objDx = obj.GetWorldPosition().X - player.GetWorldPosition().X;
        if (objDx != 0 && Math.Sign(objDx) != titanFacing) continue;
        
        // Skip player objects (players are handled separately)
        bool isPlayerObj = false;
        foreach (IPlayer p in allPlayers)
        {
            if (p != null && p.UniqueID == obj.UniqueID)
            {
                isPlayerObj = true;
                break;
            }
        }
        if (isPlayerObj) continue;
        
        int objId = obj.UniqueID;
        
        // Check cooldown
        if (hitTimes.ContainsKey(objId) && currentTime - hitTimes[objId] < ARMORED_RUSH_HIT_COOLDOWN)
        {
            continue;
        }
        
        // Record hit time
        hitTimes[objId] = currentTime;
        
        // Throw the object
        float throwVelocity = titanPower * ARMORED_RUSH_THROW_SCALE * ARMORED_RUSH_BALANCER_MOD;
        obj.SetLinearVelocity(new Vector2(titanFacing * throwVelocity, throwVelocity * ARMORED_RUSH_THROW_UP_RATIO));
        
        // Apply damage to object
        float objDamage = ARMORED_RUSH_DAMAGE_BASE * titanMods.MeleeDamageDealtModifier * 5f;
        obj.DealDamage(objDamage);
    }
    
    // Store updated hit times
    armoredTitanRushHitTimes[playerId] = hitTimes;
}

private void CheckArmoredTitanBotRush(IPlayer player, int playerId, float currentTime)
{
    // Only for bots
    if (!player.IsBot) return;
    
    // Rush ability is only available when armor is ON
    bool armorOn = armoredTitanArmorOn.ContainsKey(playerId) ? armoredTitanArmorOn[playerId] : true;
    if (!armorOn)
    {
        if (armoredTitanIsRushing.ContainsKey(playerId))
            armoredTitanIsRushing[playerId] = false;
        return;
    }
    
    PlayerModifiers mods = player.GetModifiers();
    float energyPercent = mods.CurrentEnergy / (float)mods.MaxEnergy;
    
    // Check if currently rushing
    bool isRushing = armoredTitanIsRushing.ContainsKey(playerId) ? armoredTitanIsRushing[playerId] : false;
    
    if (isRushing)
    {
        // Check abort conditions: energy falls below 20%
        if (energyPercent < ARMORED_BOT_RUSH_ABORT_ENERGY)
        {
            // Abort rush
            armoredTitanIsRushing[playerId] = false;
            return;
        }
        
        // Continue rushing (movement handled by game AI)
        return;
    }
    
    // Check trigger conditions: energy > 80% and multiple enemies in same direction
    if (energyPercent < ARMORED_BOT_RUSH_ENERGY_THRESHOLD) return;
    
    // Count enemies in facing direction within distance
    Vector2 titanPos = player.GetWorldPosition();
    int titanFacing = player.FacingDirection;
    PlayerTeam titanTeam = player.GetTeam();
    int enemyCount = 0;
    
    IPlayer[] allPlayers = Game.GetPlayers();
    foreach (IPlayer otherPlayer in allPlayers)
    {
        if (otherPlayer == null || otherPlayer.IsDead || otherPlayer.UniqueID == playerId) continue;
        
        PlayerTeam otherTeam = otherPlayer.GetTeam();
        if (otherTeam == titanTeam || otherTeam == PlayerTeam.Independent) continue;
        
        Vector2 otherPos = otherPlayer.GetWorldPosition();
        float distance = Vector2.Distance(titanPos, otherPos);
        
        if (distance > ARMORED_BOT_RUSH_CHECK_DISTANCE) continue;
        
        // Check if in facing direction
        float dx = otherPos.X - titanPos.X;
        if (Math.Sign(dx) == titanFacing)
        {
            enemyCount++;
        }
    }
    
    // Trigger rush if enough enemies
    if (enemyCount >= ARMORED_BOT_RUSH_MIN_ENEMIES)
    {
        armoredTitanIsRushing[playerId] = true;
        
        // Command bot to sprint (use Fall as proxy since StartSprinting doesn't exist)
        // Bot AI will handle sprinting behavior naturally when chasing targets
    }
}

private void TryActivateBotStrengthBoost(IPlayer player, int playerId)
{
    if (player.CurrentPowerupItem.WeaponItem != WeaponItem.STRENGTHBOOST) return;
    if (player.GetStrengthBoostTime() > 0) return;
    if (titanPlayers.ContainsKey(playerId)) return;
    
    PlayerModifiers mods = player.GetModifiers();
    float currentHp = player.GetHealth();
    float maxHp = mods.MaxHealth;
    float hpPercent = (maxHp > 0) ? (currentHp / maxHp) : 1f;
    hpPercent = Math.Max(0f, Math.Min(1f, hpPercent));
    
    // HP 100% to 0% = activate 0% to 100%
    float activationChance = 1f - hpPercent;
    
    if (rnd.NextDouble() < activationChance)
    {
        QuickCommand(player, new PlayerCommand(PlayerCommandType.DrawPowerup));
    }
}

private void CheckBotStrengthBoostActivation(IPlayer player, int playerId, float currentTime)
{
    if (!player.IsBot || player.IsDead) return;
    
    // Check periodically
    if (!botLastStrengthBoostCheck.ContainsKey(playerId) || currentTime - botLastStrengthBoostCheck[playerId] >= BOT_STRENGTH_BOOST_CHECK_INTERVAL)
    {
        botLastStrengthBoostCheck[playerId] = currentTime;
        TryActivateBotStrengthBoost(player, playerId);
    }
}

private void OnTitanFalling(IPlayer titan)
{
    Vector2 titanPos = titan.GetWorldPosition();
    
    // Camera shake while falling
    Game.PlayEffect(EffectName.CameraShaker, titanPos);
    
    // Make nearby non-titan players fall
    MakeNearbyPlayersFall(titanPos, FALL_IMPACT_RADIUS, titan.UniqueID);
}

private void CheckTitanExhaustion(IPlayer titan, int titanId)
{
    PlayerModifiers mods = titan.GetModifiers();
    
    // Initialize exhaustion state if not tracked
    if (!titanExhausted.ContainsKey(titanId))
    {
        titanExhausted[titanId] = false;
    }
    
    bool isExhausted = titanExhausted[titanId];
    bool hasNoEnergy = mods.CurrentEnergy <= 0;
    float currentTime = Game.TotalElapsedRealTime;
    
    // Titan just ran out of energy
    if (hasNoEnergy && !isExhausted)
    {
        // Disable input and make titan kneel
        titan.SetInputEnabled(false);
        QuickCommand(titan, new PlayerCommand(PlayerCommandType.DeathKneelInfinite));
        titanExhausted[titanId] = true;
        
        // Turn off armor if Armored Titan
        if (titanPlayers.ContainsKey(titanId) && titanPlayers[titanId] == TitanType.Armored)
        {
            ApplyArmoredTitanArmor(titan, titanId, false);
        }
        
        // Set recovery time to 6 seconds from now
        titanExhaustionRecoverTime[titanId] = currentTime + TITAN_EXHAUSTION_DURATION;
    }
    // Check if titan has finished exhaustion period
    else if (isExhausted && currentTime >= titanExhaustionRecoverTime[titanId])
    {
        // Stop kneeling and re-enable input
        QuickCommand(titan, new PlayerCommand(PlayerCommandType.StopDeathKneel));
        titan.SetInputEnabled(true);
        
        // Restore energy to full
        mods.CurrentEnergy = mods.MaxEnergy;
        titan.SetModifiers(mods);
        
        titanExhausted[titanId] = false;
    }
}

private void CheckTitanGrabDamage(IPlayer titan, int titanId)
{
    if (titan == null || titan.IsDead) return;
    
    // Verify this player is actually a Pure Titan or Jaw Titan before applying grab damage
    // Attack Titans don't have grab damage ability
    if (!titanPlayers.ContainsKey(titanId))
    {
        return;
    }
    
    TitanType titanType = titanPlayers[titanId];
    if (titanType != TitanType.Pure && titanType != TitanType.Jaw)
    {
        // Clean up any leftover grab data if this titan was previously Pure/Jaw and transformed
        if (titanGrabDamage.ContainsKey(titanId))
        {
            titanGrabDamage.Remove(titanId);
        }
        return;
    }
    
    float currentTime = Game.TotalElapsedRealTime;
    
    // Check all players to find who the titan is grabbing
    IPlayer[] allPlayers = Game.GetPlayers();
    IPlayer currentVictim = null;
    
    foreach (IPlayer player in allPlayers)
    {
        if (player == null || player.IsDead || player.UniqueID == titanId) continue;
        
        // Check if this player is caught in a grab specifically by THIS titan
        if (player.IsCaughtByPlayerInGrab && player.CaughtByPlayerInGrabID == titanId)
        {
            currentVictim = player;
            break;
        }
    }
    
    // Initialize or update grab damage tracking
    if (!titanGrabDamage.ContainsKey(titanId))
    {
        if (currentVictim == null) return; // No victim, no tracking needed
        
        // Disable titan's (attacker's) input when grabbing (only for bots)
        if (titan.IsBot)
        {
            DisableTitanInput(titan);
        }
        
        // Get titan's melee damage modifier
        PlayerModifiers titanMods = titan.GetModifiers();
        float titanMeleeDamage = titanMods.MeleeDamageDealtModifier;
        
        // Initialize grab damage data - start damaging immediately (0s delay)
        titanGrabDamage[titanId] = new GrabDamageData()
        {
            Victim = currentVictim,
            NextDamageTime = currentTime, // Start damage immediately
            NextBloodEffectTime = currentTime, // Initialize blood effect timer
            TitanMeleeDamage = titanMeleeDamage
        };
    }
    else
    {
        GrabDamageData grabData = titanGrabDamage[titanId];
        
        // Check if victim changed or grab ended
        if (currentVictim == null || currentVictim.UniqueID != grabData.Victim?.UniqueID)
        {
            // Re-enable titan's (attacker's) input when grab ends (only for bots)
            if (titan.IsBot)
            {
                EnableTitanInput(titan);
            }
            
            // Grab ended or victim changed, reset tracking
            if (currentVictim != null)
            {
                // New victim - start damaging immediately and disable titan's input again (only for bots)
                if (titan.IsBot)
                {
                    DisableTitanInput(titan);
                }
                
                PlayerModifiers titanMods = titan.GetModifiers();
                grabData.Victim = currentVictim;
                grabData.NextDamageTime = currentTime; // Start damage immediately
                grabData.NextBloodEffectTime = currentTime; // Initialize blood effect timer
                grabData.TitanMeleeDamage = titanMods.MeleeDamageDealtModifier;
            }
            else
            {
                // No victim, remove tracking
                titanGrabDamage.Remove(titanId);
            }
            return;
        }
        
        // Apply damage if it's time
        if (currentTime >= grabData.NextDamageTime)
        {
            // Calculate damage: 7 * titanMeleeDamage * 0.15 per 50ms
            float damagePerInterval = GRAB_DAMAGE_BASE * grabData.TitanMeleeDamage * GRAB_DAMAGE_MULTIPLIER;
            
            // Check victim health before dealing damage
            PlayerModifiers victimMods = grabData.Victim.GetModifiers();
            bool willDie = victimMods.CurrentHealth <= damagePerInterval;
            
            // Deal damage
            grabData.Victim.DealDamage(damagePerInterval);
            
            // Play blood effect on victim (only once per second)
            if (currentTime >= grabData.NextBloodEffectTime)
            {
                Vector2 victimPos = grabData.Victim.GetWorldPosition();
                Game.PlayEffect(EffectName.Blood, victimPos);
                grabData.NextBloodEffectTime = currentTime + GRAB_BLOOD_EFFECT_INTERVAL;
            }
            
            // If victim died from grab damage, gib them and re-enable titan's input (only for bots)
            if (willDie && grabData.Victim.IsDead)
            {
                // Track this gib for power extraction (only if victim has humanoid titan power)
                int victimId = grabData.Victim.UniqueID;
                if (assignedTitanPowers.ContainsKey(victimId))
                {
                    TitanType victimPower = assignedTitanPowers[victimId];
                    // Only track if victim had humanoid power and killer is Pure Titan (currently transformed)
                    if (victimPower != TitanType.Pure && titanPlayers.ContainsKey(titanId) && titanPlayers[titanId] == TitanType.Pure)
                    {
                        gibbedByPureTitan[victimId] = titanId;
                        Game.ShowChatMessage("Pure Titan grab gib detected! Victim: " + grabData.Victim.Name, Color.Yellow);
                    }
                }
                
                grabData.Victim.Gib();
                
                if (titan.IsBot)
                {
                    EnableTitanInput(titan);
                }
            }
            
            // Update next damage time
            grabData.NextDamageTime = currentTime + GRAB_DAMAGE_INTERVAL;
        }
    }
}

private void DisableTitanInput(IPlayer titan)
{
    if (titan == null || titan.IsDead) return;
    
    int titanId = titan.UniqueID;
    
    // Check if input is already disabled
    if (titanInputDisabled.ContainsKey(titanId) && titanInputDisabled[titanId])
    {
        return; // Already disabled
    }
    
    // Disable input
    titan.SetInputEnabled(false);
    titanInputDisabled[titanId] = true;
}

private void EnableTitanInput(IPlayer titan)
{
    if (titan == null) return;
    
    int titanId = titan.UniqueID;
    
    // Check if input was disabled by us
    if (!titanInputDisabled.ContainsKey(titanId) || !titanInputDisabled[titanId])
    {
        return; // We didn't disable it
    }
    
    // Re-enable input
    if (!titan.IsRemoved)
    {
        titan.SetInputEnabled(true);
    }
    titanInputDisabled[titanId] = false;
}

private void UpdateTitanPlayersStorage()
{
    // Create a comma-separated list of titan player IDs
    List<string> titanIds = new List<string>();
    
    foreach (var kvp in titanPlayers)
    {
        titanIds.Add(kvp.Key.ToString());
    }
    
    string titanPlayerList = string.Join(",", titanIds.ToArray());
    
    // Update shared storage
    IScriptStorage storage = Game.GetSharedStorage("TitanODMSync");
    storage.SetItem("TITAN_PLAYERS", titanPlayerList);
}

// Helper method to detect which player (if any) is being aimed at using raycast
private IPlayer GetAimedPlayer(IPlayer shooter)
{
    if (shooter == null || shooter.IsDead || !shooter.IsManualAiming) return null;
    
    // Get aim ray starting point and direction
    Vector2 start, dir;
    if (!shooter.GetWeaponMuzzleInfo(out start, out dir))
    {
        // No weapon drawn: fall back to player position + AimVector
        start = shooter.GetWorldPosition();
        dir = shooter.AimVector;
    }
    
    // Normalize direction vector to ensure consistent ray length
    float dirLength = (float)Math.Sqrt(dir.X * dir.X + dir.Y * dir.Y);
    if (dirLength > 0)
    {
        dir = new Vector2(dir.X / dirLength, dir.Y / dirLength);
    }
    
    // Cast ray to detect what the player is aiming at
    RayCastInput input = new RayCastInput(false);
    Vector2 end = start + dir * AIM_DETECTION_RANGE;
    RayCastResult[] results = Game.RayCast(start, end, input);

    // Check results for player hits
    foreach (RayCastResult r in results)
    {
        if (!r.Hit || !r.IsPlayer) continue;
        IPlayer hitPlayer = r.HitObject as IPlayer;
        if (hitPlayer != null && hitPlayer.UniqueID != shooter.UniqueID)
        {
            return hitPlayer;
        }
    }
    
    return null;
}

// Helper method for Armored Titan bot armor check
private IPlayer GetPlayerAimingAtBot(IPlayer bot, int botId)
{
    PlayerTeam botTeam = bot.GetTeam();
    IPlayer[] allPlayers = Game.GetPlayers();
    
    foreach (IPlayer otherPlayer in allPlayers)
    {
        if (otherPlayer == null || otherPlayer.IsDead || otherPlayer.UniqueID == botId) continue;
        
        PlayerTeam otherTeam = otherPlayer.GetTeam();
        if (otherTeam == botTeam || otherTeam == PlayerTeam.Independent) continue;
        
        IPlayer aimedTarget = GetAimedPlayer(otherPlayer);
        if (aimedTarget != null && aimedTarget.UniqueID == botId)
        {
            return otherPlayer;
        }
    }
    
    return null;
}

// Helper methods for Armored Titan force/height calculation
private float GetPlayerForce(IPlayer player)
{
    float force = player.GetModifiers().MeleeForceModifier;
    return (force < 0) ? 1f : force; // -1/-2 = unset, treat as default 1
}

private float GetPlayerHeight(IPlayer player)
{
    float size = player.GetModifiers().SizeModifier;
    return (size < 0) ? 1f : size; // -1/-2 = unset, treat as default 1
}

// Helper method to check if point is in front of Armored Titan
private bool IsArmoredInFront(IPlayer armoredTitan, Vector2 point)
{
    Vector2 titanPos = armoredTitan.GetWorldPosition();
    int facing = armoredTitan.FacingDirection; // -1 (left) or +1 (right)
    float dx = point.X - titanPos.X;

    // Positive = in front of the titan, negative = behind it
    float forwardDistance = dx * facing;
    // In front, OR only slightly behind (attacker just passed the titan)
    return forwardDistance >= -10f;
}

// Helper method to check for front melee threat
private bool IsArmoredFrontMeleeThreat(IPlayer armoredTitan, int armoredTitanId)
{
    Vector2 titanPos = armoredTitan.GetWorldPosition();
    IPlayer[] allPlayers = Game.GetPlayers();
    
    foreach (IPlayer enemy in allPlayers)
    {
        if (enemy == null || enemy.IsDead || enemy.UniqueID == armoredTitanId) continue;
        if (!enemy.IsMeleeAttacking && !enemy.IsKicking) continue;
        
        Vector2 enemyPos = enemy.GetWorldPosition();
        Vector2 d = enemyPos - titanPos;
        
        if (Math.Abs(d.X) <= ARMORED_MELEE_RANGE && 
            Math.Abs(d.Y) <= ARMORED_MELEE_Y_TOLERANCE && 
            IsArmoredInFront(armoredTitan, enemyPos))
        {
            return true;
        }
    }
    
    return false;
}

// Helper method to check for front projectile threat
private bool IsArmoredFrontProjectileThreat(IPlayer armoredTitan, int armoredTitanId)
{
    Vector2 titanPos = armoredTitan.GetWorldPosition();
    Area titanArea = armoredTitan.GetAABB();
    int facing = armoredTitan.FacingDirection;
    
    IProjectile[] projectiles = Game.GetProjectiles();
    
    foreach (IProjectile proj in projectiles)
    {
        if (proj == null || proj.IsRemoved) continue;
        if (proj.InitialOwnerPlayerID == armoredTitanId) continue; // Ignore own projectiles
        
        Vector2 projPos = proj.Position;
        Vector2 projVel = proj.Velocity;
        
        // Predict next position
        Vector2 nextPos = projPos + projVel * ARMORED_PROJ_LOOKAHEAD_SEC;
        
        // Check if projectile is moving toward titan
        bool movingToward = Math.Sign(projVel.X) == -facing;
        
        if (movingToward && 
            IsArmoredInFront(armoredTitan, projPos) &&
            nextPos.X >= titanArea.Left && nextPos.X <= titanArea.Right)
        {
            return true;
        }
    }
    
    return false;
}

private void CheckHumanDetach(IPlayer player, int playerId, float currentTime)
{
    if (player.KeyPressed(VirtualKey.WALKING))
    {
        if (!walkKeyPressStartTime.ContainsKey(playerId))
        {
            walkKeyPressStartTime[playerId] = currentTime;
        }
        else if (currentTime - walkKeyPressStartTime[playerId] >= HUMAN_DETACH_HOLD_TIME)
        {
            walkKeyPressStartTime.Remove(playerId);
            DetachTitan(player, playerId);
        }
    }
    else
    {
        if (walkKeyPressStartTime.ContainsKey(playerId))
        {
            walkKeyPressStartTime.Remove(playerId);
        }
    }
}

private void CheckBotDetach(IPlayer player, int playerId, float currentTime)
{
    // Periodic check
    if (botLastDetachCheck.ContainsKey(playerId) && currentTime - botLastDetachCheck[playerId] < BOT_DETACH_CHECK_INTERVAL)
    {
        return;
    }
    botLastDetachCheck[playerId] = currentTime;
    
    PlayerModifiers mods = player.GetModifiers();
    float hpPercent = (mods.MaxHealth > 0) ? ((float)mods.CurrentHealth / mods.MaxHealth) : 1f;
    hpPercent = Math.Max(0f, Math.Min(1f, hpPercent));
    
    // Only consider detach when HP is between 15% and 5%
    if (hpPercent > BOT_DETACH_HP_HIGH || hpPercent <= 0f) return;
    
    // Calculate detach chance: linearly interpolate from 20% at 15% HP to 90% at 5% HP
    float detachChance;
    if (hpPercent <= BOT_DETACH_HP_LOW)
    {
        detachChance = BOT_DETACH_CHANCE_MAX; // 90% at 5% or below
    }
    else
    {
        // Interpolate: as HP goes from 15% down to 5%, chance goes from 20% up to 90%
        float t = (BOT_DETACH_HP_HIGH - hpPercent) / (BOT_DETACH_HP_HIGH - BOT_DETACH_HP_LOW);
        detachChance = BOT_DETACH_CHANCE_MIN + (BOT_DETACH_CHANCE_MAX - BOT_DETACH_CHANCE_MIN) * t;
    }
    
    if (rnd.NextDouble() < detachChance)
    {
        DetachTitan(player, playerId);
    }
}

private void DetachTitan(IPlayer titanPlayer, int titanPlayerId)
{
    // Prevent detach if not a titan or if Pure Titan
    if (!titanPlayers.ContainsKey(titanPlayerId)) return;
    if (titanPlayers[titanPlayerId] == TitanType.Pure) return;
    
    walkKeyPressStartTime.Remove(titanPlayerId);
    
    Vector2 titanPos = titanPlayer.GetWorldPosition();
    int titanFacing = titanPlayer.FacingDirection;
    PlayerTeam titanTeam = titanPlayer.GetTeam();
    bool isBot = titanPlayer.IsBot;
    string playerName = titanPlayer.Name;
    
    // Get original profile and modifiers (human form)
    IProfile humanProfile = originalProfiles.ContainsKey(titanPlayerId) ? originalProfiles[titanPlayerId] : titanPlayer.GetProfile();
    PlayerModifiers humanMods = originalModifiers.ContainsKey(titanPlayerId) ? originalModifiers[titanPlayerId] : new PlayerModifiers();
    
    // Get original bot behaviors
    BotBehavior humanBotBehavior = originalBotBehaviors.ContainsKey(titanPlayerId) ? originalBotBehaviors[titanPlayerId] : new BotBehavior(true, PredefinedAIType.BotA);
    BotBehaviorSet humanBotBehaviorSet = originalBotBehaviorSets.ContainsKey(titanPlayerId) ? originalBotBehaviorSets[titanPlayerId] : null;
    
    // Get user controller (for human players) before anything happens
    IUser user = titanPlayer.GetUser();
    
    // Play detransformation effects at titan position
    Game.PlayEffect(EffectName.Steam, titanPos);
    Game.PlayEffect(EffectName.Steam, titanPos);
    Game.PlaySound("BarrelDestroyed", titanPos);
    
    // Remove old label immediately so it doesn't float above the corpse
    if (titanTypeLabels.ContainsKey(titanPlayerId))
    {
        IObjectText label = titanTypeLabels[titanPlayerId];
        if (label != null && !label.IsRemoved)
        {
            label.Remove();
        }
        titanTypeLabels.Remove(titanPlayerId);
    }
    
    // Preserve assigned titan power
    TitanType power = TitanType.Pure;
    bool hasPower = false;
    if (assignedTitanPowers.ContainsKey(titanPlayerId))
    {
        power = assignedTitanPowers[titanPlayerId];
        hasPower = true;
        assignedTitanPowers.Remove(titanPlayerId);
    }
    
    // Clean up all titan tracking data for this player BEFORE killing
    CleanupTitanData(titanPlayerId);
    
    // Update shared storage immediately to notify odm.cs that titan is dead
    UpdateTitanPlayersStorage();
    
    // Prevent the game from declaring round-over during the 1500ms spawn delay
    // Use the same SuperDSCoopSync storage/keys as SuperDS.cs so CoopGameOver.cs handles it automatically
    Game.AutoVictoryConditionEnabled = false;
    IScriptStorage detachStorage = Game.GetSharedStorage("SuperDSCoopSync");
    detachStorage.SetItem("P2_SPLITTING", true);
    detachStorage.SetItem("P2_SPLIT_TIME", Game.TotalElapsedGameTime + 3000f); // 3s buffer
    
    // Kill the titan player immediately (leaving the titan corpse on the ground to steam)
    titanPlayer.Kill();
    
    // Show detach message
    Game.ShowChatMessage(playerName + " has detached from their Titan form!", Color.Cyan);
    
    // The detransformed human spawns after 1500ms of the titan body being killed
    Events.UpdateCallback.Start((float dt) =>
    {
        // Calculate ground position where corpse landed
        Vector2 corpsePos = (titanPlayer != null) ? titanPlayer.GetWorldPosition() : titanPos;
        Area corpseArea = (titanPlayer != null) ? titanPlayer.GetAABB() : new Area(corpsePos.Y + 10f, corpsePos.X - 10f, corpsePos.Y - 10f, corpsePos.X + 10f);
        Vector2 spawnPos = corpsePos;
        
        if (titanPlayer != null && titanPlayer.IsOnGround)
        {
            spawnPos = new Vector2(corpsePos.X, corpseArea.Bottom + 10f);
        }
        else
        {
            RayCastInput rcInput = new RayCastInput(true)
            {
                ClosestHitOnly = true,
                MaskBits = 0x0001,
                FilterOnMaskBits = true
            };
            RayCastResult[] hits = Game.RayCast(corpsePos, corpsePos - new Vector2(0f, 200f), rcInput);
            if (hits.Length > 0 && hits[0].Hit)
            {
                spawnPos = new Vector2(corpsePos.X, hits[0].Position.Y + 10f);
            }
            else
            {
                spawnPos = new Vector2(corpsePos.X, corpseArea.Bottom + 10f);
            }
        }
        
        // Steam effect at spawn position
        Game.PlayEffect(EffectName.Steam, spawnPos);
        
        // Fire the trigger itself to spawn the player with the zombie emerging animation
        IPlayer humanPlayer = null;
        if (zombieSpawnTrigger != null)
        {
            zombieSpawnTrigger.SetWorldPosition(spawnPos);
            zombieSpawnTrigger.SetFaceDirection(titanFacing);
            zombieSpawnTrigger.SetEnabled(true);
            zombieSpawnTrigger.Trigger();
            humanPlayer = zombieSpawnTrigger.GetLastCreatedPlayer();
        }
        if (humanPlayer == null)
        {
            humanPlayer = Game.CreatePlayer(spawnPos);
        }
        if (humanPlayer == null)
        {
            return;
        }
        
        int humanPlayerId = humanPlayer.UniqueID;
        
        // Set team
        humanPlayer.SetTeam(titanTeam);
        
        // Set the human profile
        humanPlayer.SetProfile(humanProfile);
        
        // Set bot name if bot
        if (isBot)
        {
            humanPlayer.SetBotName(playerName);
        }
        
        // Re-attach user controller for human players (temporarily disable input so emerging animation plays without key interruption)
        if (user != null)
        {
            humanPlayer.SetUser(user);
            humanPlayer.SetInputEnabled(false);
        }
        
        // Set full HP and full energy from original modifiers
        if (humanMods.MaxHealth <= 0) humanMods.MaxHealth = 100;
        if (humanMods.MaxEnergy <= 0) humanMods.MaxEnergy = 100;
        humanMods.CurrentHealth = humanMods.MaxHealth;
        humanMods.CurrentEnergy = humanMods.MaxEnergy;
        humanPlayer.SetModifiers(humanMods);
        
        // Set facing direction directly without queuing a command that could cancel the emergence animation
        humanPlayer.SetFaceDirection(titanFacing);
        
        // After 1000ms, restore the real behavior for both bots and human players
        Events.UpdateCallback.Start((float dt2) =>
        {
            if (humanPlayer == null || humanPlayer.IsDead || humanPlayer.IsRemoved) return;
            
            if (isBot)
            {
                humanPlayer.SetBotBehavior(humanBotBehavior);
                if (humanBotBehaviorSet != null)
                {
                    humanBotBehaviorSet.PowerupUsage = false;
                    humanPlayer.SetBotBehaviorSet(humanBotBehaviorSet);
                }
            }
            else
            {
                humanPlayer.SetBotBehavior(new BotBehavior(false, PredefinedAIType.None));
                humanPlayer.SetBotBehaviorActive(false);
                if (user != null)
                {
                    humanPlayer.SetUser(user);
                    humanPlayer.SetInputEnabled(true);
                }
            }
        }, 1000, 1);
        
        // Re-enable auto-victory now that the human player has spawned successfully
        Game.AutoVictoryConditionEnabled = true;
        IScriptStorage postDetachStorage = Game.GetSharedStorage("SuperDSCoopSync");
        postDetachStorage.SetItem("P2_SPLITTING", false);
        
        // Mark as already given startup strength boost so they don't immediately get another one
        givenStartupStrengthBoost.Add(humanPlayerId);
        
        // Transfer assigned power to the new player ID
        if (hasPower)
        {
            assignedTitanPowers[humanPlayerId] = power;
            
            // Update label for the new player
            CreateLabelForPlayer(humanPlayer, humanPlayerId, power);
        }
        
        // Clean up old original data references
        originalProfiles.Remove(titanPlayerId);
        originalModifiers.Remove(titanPlayerId);
        originalBotBehaviors.Remove(titanPlayerId);
        originalBotBehaviorSets.Remove(titanPlayerId);
        
        // Update name mapping
        playerNameToId[playerName] = humanPlayerId;
        playerIdToName[humanPlayerId] = playerName;
        
        // Ensure the new bot gets PowerupUsage disabled
        if (isBot && !configuredBotPowerupUsage.Contains(humanPlayerId))
        {
            BotBehaviorSet bS = humanPlayer.GetBotBehaviorSet();
            bS.PowerupUsage = false;
            humanPlayer.SetBotBehaviorSet(bS);
            configuredBotPowerupUsage.Add(humanPlayerId);
        }
        
        // Save updated powers
        SaveAssignedPowersToStorage();
    }, DETACH_SPAWN_DELAY_MS, 1);
}

private void CleanupTitanData(int titanPlayerId)
{
    // Remove from titan players list
    titanPlayers.Remove(titanPlayerId);
    
    // Clean up common titan data
    titanSizes.Remove(titanPlayerId);
    titanGrabDamage.Remove(titanPlayerId);
    titanInputDisabled.Remove(titanPlayerId);
    titanExhausted.Remove(titanPlayerId);
    titanExhaustionRecoverTime.Remove(titanPlayerId);
    playerWasOnGround.Remove(titanPlayerId);
    playerHadStrengthBoost.Remove(titanPlayerId);
    
    // Clean up Attack Titan data
    attackTitanHardeningMode.Remove(titanPlayerId);
    attackTitanHardeningEnergyCost.Remove(titanPlayerId);
    attackTitanLastMeleeTime.Remove(titanPlayerId);
    attackTitanLastDefensiveCheck.Remove(titanPlayerId);
    attackTitanSlowmoEndTime.Remove(titanPlayerId);
    attackTitanPunchCount.Remove(titanPlayerId);
    attackTitanLastPunchTime.Remove(titanPlayerId);
    
    // Clean up Jaw Titan data
    jawTitanPunchCount.Remove(titanPlayerId);
    jawTitanLastPunchTime.Remove(titanPlayerId);
    jawTitanHuntVictim.Remove(titanPlayerId);
    jawTitanHuntDamageTime.Remove(titanPlayerId);
    jawTitanHuntBloodEffectTime.Remove(titanPlayerId);
    jawHuntVictimReenableTime.Remove(titanPlayerId);
    jawBotLastCombatTime.Remove(titanPlayerId);
    jawBotCurrentTarget.Remove(titanPlayerId);
    jawBotLastTargetCheck.Remove(titanPlayerId);
    
    // Clean up Armored Titan data
    armoredTitanArmorOn.Remove(titanPlayerId);
    armoredTitanLastArmorCheck.Remove(titanPlayerId);
    armoredTitanRushHitTimes.Remove(titanPlayerId);
    armoredTitanIsRushing.Remove(titanPlayerId);
    armoredTitanPunchCount.Remove(titanPlayerId);
    armoredTitanLastPunchTime.Remove(titanPlayerId);
    
    // Clean up Cart Titan data
    CleanupCartTitanData(titanPlayerId);
    
    // Clean up bot detach check
    botLastDetachCheck.Remove(titanPlayerId);
    botLastStrengthBoostCheck.Remove(titanPlayerId);
    
    // Clean up facing direction
    playerFacingDirections.Remove(titanPlayerId);
    
    // Clean up strength boost tracking
    givenStartupStrengthBoost.Remove(titanPlayerId);
    
    // Clean up walk key press tracking
    walkKeyPressStartTime.Remove(titanPlayerId);
}

// ============================================================
// CART TITAN IMPLEMENTATION
// ============================================================

private void TransformToCartTitan(IPlayer player, int playerId)
{
    // Clear all items FIRST before profile change
    player.RemoveWeaponItemType(WeaponItemType.Rifle);
    player.RemoveWeaponItemType(WeaponItemType.Handgun);
    player.RemoveWeaponItemType(WeaponItemType.Melee);
    player.RemoveWeaponItemType(WeaponItemType.Thrown);
    player.RemoveWeaponItemType(WeaponItemType.Powerup);
    
    // Set Cart Titan Rescue profile (default form)
    player.SetProfile(GetCartTitanRescueProfile());
    
    // Remove strength boost
    player.SetStrengthBoostTime(0);
    
    // Get modifiers
    PlayerModifiers mods = player.GetModifiers();
    
    // Apply Cart Titan base stats
    int newMaxHealth = (int)(mods.MaxHealth * (1f + CART_TITAN_HP_BONUS));
    newMaxHealth = Math.Max(1, newMaxHealth);
    mods.MaxHealth = newMaxHealth;
    mods.CurrentHealth = newMaxHealth;
    
    mods.MeleeDamageDealtModifier *= (1f + CART_TITAN_MELEE_DAMAGE_BONUS);
    mods.MeleeForceModifier       *= (1f + CART_TITAN_MELEE_FORCE_BONUS);
    mods.RunSpeedModifier          *= (1f + CART_TITAN_SPEED_BONUS);
    mods.SprintSpeedModifier       *= (1f + CART_TITAN_SPEED_BONUS);
    
    // +200% energy
    mods.MaxEnergy = (int)(mods.MaxEnergy * (1f + CART_TITAN_ENERGY_BONUS));
    mods.CurrentEnergy = mods.MaxEnergy;
    
    mods.SizeModifier = CART_TITAN_SIZE;
    mods.ItemDropMode = 1;
    
    player.SetModifiers(mods);
    
    // Bot behaviour
    player.SetBotBehavior(new BotBehavior(true, PredefinedAIType.BotA));
    BotBehaviorSet bS = player.GetBotBehaviorSet();
    bS.SearchItems = 0;
    player.SetBotBehaviorSet(bS);
    
    // Initialise state (Rescue form)
    cartTitanForm[playerId] = CartTitanForm.Rescue;
    cartTitanGunIsSniper[playerId] = false;
    cartTitanBlockHeld[playerId] = false;
    cartTitanGunAimAngle[playerId] = 0f;
    
    // Attach backpack object for Rescue form
    CartTitanAttachBackObject(player, playerId, CartTitanForm.Rescue);
    
    // Store titan size
    titanSizes[playerId] = CART_TITAN_SIZE;
}

private IProfile GetCartTitanRescueProfile()
{
    return new IProfile()
    {
        Gender = Gender.Female,
        Skin = new IProfileClothingItem("Normal_fem", "Skin4", "ClothingLightPink"),
        Accesory = new IProfileClothingItem("SunGlasses", "", "ClothingLightRed"),
    };
}

private IProfile GetCartTitanOffensiveProfile()
{
    return new IProfile()
    {
        Gender = Gender.Female,
        Skin = new IProfileClothingItem("Normal_fem", "Skin4", "ClothingLightPink"),
        Head = new IProfileClothingItem("WeldingHelmet", "ClothingLightGray"),
        Accesory = new IProfileClothingItem("SunGlasses", "", "ClothingLightRed"),
    };
}

// Attach/replace the back-attached object (BgFence01G for Rescue, drone for Offensive)
private void CartTitanAttachBackObject(IPlayer player, int playerId, CartTitanForm form)
{
    // Remove old back object if it exists
    if (cartTitanBackObject.ContainsKey(playerId))
    {
        IObject old = cartTitanBackObject[playerId];
        if (old != null && !old.IsRemoved) old.Remove();
        cartTitanBackObject.Remove(playerId);
    }
    
    string objName = (form == CartTitanForm.Rescue) ? "BgFence01G" : "Drone";
    IObject backObj = Game.CreateObject(objName, player.GetWorldPosition());
    if (backObj != null)
    {
        cartTitanBackObject[playerId] = backObj;
    }
}

// Toggle between Rescue and Offensive form
private void ToggleCartTitanForm(IPlayer player, int playerId)
{
    CartTitanForm currentForm = cartTitanForm.ContainsKey(playerId) ? cartTitanForm[playerId] : CartTitanForm.Rescue;
    CartTitanForm newForm = (currentForm == CartTitanForm.Rescue) ? CartTitanForm.Offensive : CartTitanForm.Rescue;
    
    // Death kneel + sound as mode switch animation
    QuickCommand(player, new PlayerCommand(PlayerCommandType.DeathKneelInfinite));
    Game.PlaySound("ShotgunReload", player.GetWorldPosition());
    
    // Apply new form after brief kneel
    int pid = playerId;
    Events.UpdateCallback.Start((float dt) =>
    {
        IPlayer p = Game.GetPlayer(pid);
        if (p == null || p.IsDead || p.IsRemoved) return;
        if (!titanPlayers.ContainsKey(pid) || titanPlayers[pid] != TitanType.Cart) return;
        
        QuickCommand(p, new PlayerCommand(PlayerCommandType.StopDeathKneel));
        ApplyCartTitanForm(p, pid, newForm);
    }, 600, 1);
}

private void ApplyCartTitanForm(IPlayer player, int playerId, CartTitanForm newForm)
{
    CartTitanForm oldForm = cartTitanForm.ContainsKey(playerId) ? cartTitanForm[playerId] : CartTitanForm.Rescue;
    if (oldForm == newForm) return;
    
    PlayerModifiers mods = player.GetModifiers();
    
    // Remove old form modifiers
    if (oldForm == CartTitanForm.Offensive)
    {
        // Remove offensive speed penalty and HP bonus
        mods.RunSpeedModifier    /= (1f - CART_OFFENSIVE_SPEED_PENALTY);
        mods.SprintSpeedModifier /= (1f - CART_OFFENSIVE_SPEED_PENALTY);
        int hpRemove = (int)(mods.MaxHealth * CART_OFFENSIVE_HP_BONUS / (1f + CART_OFFENSIVE_HP_BONUS));
        mods.MaxHealth -= hpRemove;
        mods.CurrentHealth = Math.Min(mods.CurrentHealth, mods.MaxHealth);
        
        // Kill and clean up the gun pipe
        CartTitanDestroyGunPipe(playerId);
        
        // Remove crosshair
        CartTitanDestroyCrosshair(playerId);
        
        // Reset block mode
        cartTitanBlockHeld[playerId] = false;
        player.SetInputMode(PlayerInputMode.Enabled);
    }
    else // oldForm == Rescue
    {
        // Release ally if carried
        CartTitanReleaseAlly(player, playerId);
    }
    
    // Apply new form
    if (newForm == CartTitanForm.Offensive)
    {
        // Apply speed penalty and HP bonus
        mods.RunSpeedModifier    *= (1f - CART_OFFENSIVE_SPEED_PENALTY);
        mods.SprintSpeedModifier *= (1f - CART_OFFENSIVE_SPEED_PENALTY);
        int hpAdd = (int)(mods.MaxHealth * CART_OFFENSIVE_HP_BONUS);
        mods.MaxHealth += hpAdd;
        mods.CurrentHealth = Math.Min(mods.CurrentHealth + hpAdd, mods.MaxHealth);
        
        player.SetProfile(GetCartTitanOffensiveProfile());
        player.SetModifiers(mods);
        
        // Create gun pipe (BgRailing02A)
        CartTitanCreateGunPipe(player, playerId);
    }
    else // newForm == Rescue
    {
        player.SetProfile(GetCartTitanRescueProfile());
        player.SetModifiers(mods);
    }
    
    cartTitanForm[playerId] = newForm;
    
    // Swap back object
    CartTitanAttachBackObject(player, playerId, newForm);
}

// Create the gun pipe (BgRailing02A) object for Offensive form
private void CartTitanCreateGunPipe(IPlayer cartTitan, int cartTitanId)
{
    // Remove old gun pipe if any
    CartTitanDestroyGunPipe(cartTitanId);
    
    Vector2 spawnPos = cartTitan.GetWorldPosition() + new Vector2(0f, CART_GUN_OFFSET_Y);
    IObject pipeObj = Game.CreateObject("BgRailing02A", spawnPos);
    if (pipeObj != null)
    {
        cartTitanGunPipe[cartTitanId] = pipeObj;
    }
    
    cartTitanGunIsSniper[cartTitanId] = false;
    cartTitanGunAimAngle[cartTitanId] = 0f;
    
    // Create crosshair '+' text object
    CartTitanCreateCrosshair(cartTitanId, spawnPos + new Vector2(cartTitan.FacingDirection * CART_GUN_AIM_DISTANCE, 0f));
}

private void CartTitanDestroyGunPipe(int cartTitanId)
{
    if (cartTitanGunPipe.ContainsKey(cartTitanId))
    {
        IObject pipeObj = cartTitanGunPipe[cartTitanId];
        if (pipeObj != null && !pipeObj.IsRemoved)
        {
            pipeObj.Remove();
        }
        cartTitanGunPipe.Remove(cartTitanId);
    }
}

private void CartTitanSwitchGun(int cartTitanId, bool toSniper)
{
    cartTitanGunIsSniper[cartTitanId] = toSniper;
}

private void CartTitanSetBlockMode(IPlayer cartTitan, int cartTitanId, bool blockHeld)
{
    // Only in Offensive form
    CartTitanForm form = cartTitanForm.ContainsKey(cartTitanId) ? cartTitanForm[cartTitanId] : CartTitanForm.Rescue;
    if (form != CartTitanForm.Offensive) return;
    
    cartTitanBlockHeld[cartTitanId] = blockHeld;
    
    if (blockHeld)
    {
        // Enter read-only: disable cart titan input so JUMP/CROUCH adjust aim instead
        cartTitan.SetInputMode(PlayerInputMode.ReadOnly);
    }
    else
    {
        // Re-enable input
        cartTitan.SetInputMode(PlayerInputMode.Enabled);
    }
}

private void CartTitanLiftAlly(IPlayer cartTitan, int cartTitanId, IPlayer ally, float currentTime)
{
    cartTitanLiftedAlly[cartTitanId] = ally;
    cartTitanLiftNextCostTime[cartTitanId] = currentTime + 1000f; // first cost after 1 second
    
    // Apply damage reduction to ally
    PlayerModifiers allyMods = ally.GetModifiers();
    allyMods.MeleeDamageTakenModifier      *= (1f - CART_RESCUE_ALLY_DMG_REDUCTION);
    allyMods.ProjectileDamageTakenModifier *= (1f - CART_RESCUE_ALLY_DMG_REDUCTION);
    allyMods.ExplosionDamageTakenModifier  *= (1f - CART_RESCUE_ALLY_DMG_REDUCTION);
    allyMods.ImpactDamageTakenModifier     *= (1f - CART_RESCUE_ALLY_DMG_REDUCTION);
    ally.SetModifiers(allyMods);
    
    Game.ShowChatMessage(cartTitan.Name + " lifted " + ally.Name + " onto the backpack!", Color.Cyan);
}

private void CartTitanReleaseAlly(IPlayer cartTitan, int cartTitanId)
{
    if (!cartTitanLiftedAlly.ContainsKey(cartTitanId)) return;
    IPlayer ally = cartTitanLiftedAlly[cartTitanId];
    cartTitanLiftedAlly.Remove(cartTitanId);
    cartTitanLiftNextCostTime.Remove(cartTitanId);
    
    if (ally == null || ally.IsDead || ally.IsRemoved) return;
    
    // Restore damage modifiers
    PlayerModifiers allyMods = ally.GetModifiers();
    if (allyMods.MeleeDamageTakenModifier > 0)
        allyMods.MeleeDamageTakenModifier /= (1f - CART_RESCUE_ALLY_DMG_REDUCTION);
    if (allyMods.ProjectileDamageTakenModifier > 0)
        allyMods.ProjectileDamageTakenModifier /= (1f - CART_RESCUE_ALLY_DMG_REDUCTION);
    if (allyMods.ExplosionDamageTakenModifier > 0)
        allyMods.ExplosionDamageTakenModifier /= (1f - CART_RESCUE_ALLY_DMG_REDUCTION);
    if (allyMods.ImpactDamageTakenModifier > 0)
        allyMods.ImpactDamageTakenModifier /= (1f - CART_RESCUE_ALLY_DMG_REDUCTION);
    ally.SetModifiers(allyMods);
    
    Game.ShowChatMessage(ally.Name + " was released from the backpack.", Color.Cyan);
}

// Create crosshair '+' text at aim position
private void CartTitanCreateCrosshair(int cartTitanId, Vector2 pos)
{
    CartTitanDestroyCrosshair(cartTitanId);
    IObjectText ch = (IObjectText)Game.CreateObject("Text", pos);
    if (ch != null)
    {
        ch.SetText("+");
        ch.SetTextAlignment(TextAlignment.Middle);
        ch.SetTextScale(0.95f);
        ch.SetTextColor(new Color(255, 255, 100));
        cartTitanCrosshair[cartTitanId] = ch;
    }
}

private void CartTitanDestroyCrosshair(int cartTitanId)
{
    if (cartTitanCrosshair.ContainsKey(cartTitanId))
    {
        IObjectText ch = cartTitanCrosshair[cartTitanId];
        if (ch != null && !ch.IsRemoved) ch.Remove();
        cartTitanCrosshair.Remove(cartTitanId);
    }
}

// Per-tick update for Cart Titan
private void CartTitanUpdate(IPlayer player, int playerId, float currentTime)
{
    CartTitanForm form = cartTitanForm.ContainsKey(playerId) ? cartTitanForm[playerId] : CartTitanForm.Rescue;
    
    Vector2 playerPos = player.GetWorldPosition();
    
    // ---- Position back-attached object ----
    if (cartTitanBackObject.ContainsKey(playerId))
    {
        IObject backObj = cartTitanBackObject[playerId];
        if (backObj != null && !backObj.IsRemoved)
        {
            // Place behind the titan (opposite of facing)
            int facing = player.FacingDirection;
            Vector2 backPos = playerPos + new Vector2(-facing * 8f, 4f);
            backObj.SetWorldPosition(backPos);
        }
        else
        {
            cartTitanBackObject.Remove(playerId);
        }
    }
    
    if (form == CartTitanForm.Rescue)
    {
        CartTitanUpdateRescue(player, playerId, currentTime, playerPos);
    }
    else
    {
        CartTitanUpdateOffensive(player, playerId, currentTime, playerPos);
    }
}

private void CartTitanUpdateRescue(IPlayer player, int playerId, float currentTime, Vector2 playerPos)
{
    // If not currently lifting an ally, check if cart titan is holding an ally in a dive
    if (!cartTitanLiftedAlly.ContainsKey(playerId) || cartTitanLiftedAlly[playerId] == null)
    {
        if (player.HoldingPlayerInDiveID > 0)
        {
            IPlayer tackled = Game.GetPlayer(player.HoldingPlayerInDiveID);
            if (tackled != null && !tackled.IsDead && !tackled.IsRemoved && tackled.UniqueID != playerId)
            {
                PlayerTeam crtTeam = player.GetTeam();
                if (crtTeam == PlayerTeam.Independent || tackled.GetTeam() == crtTeam)
                {
                    CartTitanLiftAlly(player, playerId, tackled, currentTime);
                }
            }
        }
        return;
    }
    
    IPlayer ally = cartTitanLiftedAlly[playerId];
    if (ally == null || ally.IsDead || ally.IsRemoved)
    {
        CartTitanReleaseAlly(player, playerId);
        return;
    }
    
    // Position ally on back
    int facing = player.FacingDirection;
    Vector2 allyPos = playerPos + new Vector2(-facing * 6f, 10f);
    ally.SetWorldPosition(allyPos);
    ally.SetLinearVelocity(player.GetLinearVelocity());
    
    // Energy cost: 0.5% of max energy per second
    if (currentTime >= cartTitanLiftNextCostTime[playerId])
    {
        cartTitanLiftNextCostTime[playerId] = currentTime + 1000f;
        PlayerModifiers mods = player.GetModifiers();
        float cost = mods.MaxEnergy * CART_RESCUE_LIFT_COST_PER_SEC;
        mods.CurrentEnergy = Math.Max(0f, mods.CurrentEnergy - cost);
        player.SetModifiers(mods);
    }
}

private void CartTitanUpdateOffensive(IPlayer player, int playerId, float currentTime, Vector2 playerPos)
{
    // Update gun pipe position, angle, and aim
    if (!cartTitanGunPipe.ContainsKey(playerId)) return;
    IObject pipeObj = cartTitanGunPipe[playerId];
    if (pipeObj == null || pipeObj.IsRemoved)
    {
        cartTitanGunPipe.Remove(playerId);
        CartTitanDestroyCrosshair(playerId);
        return;
    }
    
    // Gun pipe rides on titan's head
    Vector2 gunPos = playerPos + new Vector2(0f, CART_GUN_OFFSET_Y);
    pipeObj.SetWorldPosition(gunPos);
    
    // Block/aim-adjust mode: JUMP = aim up, CROUCH_ROLL_DIVE = aim down
    bool blockHeld = cartTitanBlockHeld.ContainsKey(playerId) && cartTitanBlockHeld[playerId];
    if (blockHeld)
    {
        float currentAngle = cartTitanGunAimAngle.ContainsKey(playerId) ? cartTitanGunAimAngle[playerId] : 0f;
        // JUMP aims up (+Y in SFD), CROUCH aims down (-Y in SFD)
        if (player.KeyPressed(VirtualKey.JUMP))
            currentAngle += 0.035f;
        if (player.KeyPressed(VirtualKey.CROUCH_ROLL_DIVE))
            currentAngle -= 0.035f;
            
        // Clamp to avoid aiming completely backwards into the cart titan
        currentAngle = MathHelper.Clamp(currentAngle, -(float)(Math.PI * 0.45), (float)(Math.PI * 0.45));
        cartTitanGunAimAngle[playerId] = currentAngle;
    }
    
    // Calculate aim direction and target relative to current gunPos along a circle
    float aimAngleOffset = cartTitanGunAimAngle.ContainsKey(playerId) ? cartTitanGunAimAngle[playerId] : 0f;
    float cosAngle = (float)Math.Cos(aimAngleOffset);
    float sinAngle = (float)Math.Sin(aimAngleOffset);
    // When facing right (+1): direction is (cos, sin). When facing left (-1): direction is (-cos, sin)
    Vector2 aimDirection = new Vector2(player.FacingDirection * cosAngle, sinAngle);
    Vector2 aimTarget = gunPos + aimDirection * CART_GUN_AIM_DISTANCE;
    
    // Rotate gun pipe based on aim + default rotation offset
    float angle = (float)Math.Atan2(aimDirection.Y, aimDirection.X) + CART_GUN_PIPE_DEFAULT_ROTATION;
    pipeObj.SetAngle(angle);
    
    // Update crosshair position (moves with the titan)
    if (cartTitanCrosshair.ContainsKey(playerId))
    {
        IObjectText ch = cartTitanCrosshair[playerId];
        if (ch != null && !ch.IsRemoved)
        {
            ch.SetWorldPosition(aimTarget);
        }
        else
        {
            cartTitanCrosshair.Remove(playerId);
            CartTitanCreateCrosshair(playerId, aimTarget);
        }
    }
    
    // ATTACK fire: drain energy per bullet and spawn projectile (can fire while adjusting aim / block held)
    bool attackHeld = player.KeyPressed(VirtualKey.ATTACK);
    bool isSniper = cartTitanGunIsSniper.ContainsKey(playerId) && cartTitanGunIsSniper[playerId];
    float fireInterval = isSniper ? 500f : 100f;
    bool canFire = attackHeld &&
        (!cartTitanLastBulletFireTime.ContainsKey(playerId) || currentTime - cartTitanLastBulletFireTime[playerId] >= fireInterval);
        
    if (canFire)
    {
        cartTitanLastBulletFireTime[playerId] = currentTime;
        
        ProjectileItem projType = isSniper ? ProjectileItem.SNIPER : ProjectileItem.M60;
        Vector2 muzzlePos = gunPos + aimDirection * 10f;
        Game.SpawnProjectile(projType, muzzlePos, aimDirection);
        
        PlayerModifiers crtMods = player.GetModifiers();
        float energyCost = isSniper
            ? crtMods.MaxEnergy * CART_GUN_ENERGY_PER_BULLET_SNIPER
            : crtMods.MaxEnergy * CART_GUN_ENERGY_PER_BULLET_M60;
        crtMods.CurrentEnergy = Math.Max(0f, crtMods.CurrentEnergy - energyCost);
        player.SetModifiers(crtMods);
    }
}

private void CleanupCartTitanData(int playerId)
{
    // Release lifted ally (restore modifiers)
    if (cartTitanLiftedAlly.ContainsKey(playerId))
    {
        IPlayer ally = cartTitanLiftedAlly[playerId];
        if (ally != null && !ally.IsDead && !ally.IsRemoved)
        {
            PlayerModifiers allyMods = ally.GetModifiers();
            if (allyMods.MeleeDamageTakenModifier > 0)
                allyMods.MeleeDamageTakenModifier /= (1f - CART_RESCUE_ALLY_DMG_REDUCTION);
            if (allyMods.ProjectileDamageTakenModifier > 0)
                allyMods.ProjectileDamageTakenModifier /= (1f - CART_RESCUE_ALLY_DMG_REDUCTION);
            if (allyMods.ExplosionDamageTakenModifier > 0)
                allyMods.ExplosionDamageTakenModifier /= (1f - CART_RESCUE_ALLY_DMG_REDUCTION);
            if (allyMods.ImpactDamageTakenModifier > 0)
                allyMods.ImpactDamageTakenModifier /= (1f - CART_RESCUE_ALLY_DMG_REDUCTION);
            ally.SetModifiers(allyMods);
        }
        cartTitanLiftedAlly.Remove(playerId);
    }
    cartTitanLiftNextCostTime.Remove(playerId);
    
    // Destroy gun pipe
    CartTitanDestroyGunPipe(playerId);
    
    // Destroy crosshair
    CartTitanDestroyCrosshair(playerId);
    
    // Destroy back object
    if (cartTitanBackObject.ContainsKey(playerId))
    {
        IObject obj = cartTitanBackObject[playerId];
        if (obj != null && !obj.IsRemoved) obj.Remove();
        cartTitanBackObject.Remove(playerId);
    }
    
    cartTitanForm.Remove(playerId);
    cartTitanGunIsSniper.Remove(playerId);
    cartTitanBlockHeld.Remove(playerId);
    cartTitanGunAimAngle.Remove(playerId);
    cartTitanLastBulletFireTime.Remove(playerId);
}

private void QuickCommand(IPlayer player, PlayerCommand command)
{
    bool inputEnabled = player.IsInputEnabled;

    player.AddCommand(command);

    if (inputEnabled)
    {
        player.SetInputEnabled(false);

        Game.Events.StartUpdateCallback(dlt =>
        {
            player?.SetInputEnabled(true);
        }, 1, 1);
    }
}


// Checks if the given player is under a melee threat from ANY direction (before the hit lands) of titans.
// Only enemies count: players on a different team, or everyone if this player is Independent.
private bool IsUnderMeleeThreatOfTitan(IPlayer player)
{
    Vector2 playerPos = player.GetWorldPosition();
    PlayerTeam playerTeam = player.GetTeam();
    int playerId = player.UniqueID;

    foreach (IPlayer enemy in Game.GetPlayers())
    {
        if (enemy == null || enemy.IsDead || enemy.UniqueID == playerId || !titanPlayers.ContainsKey(enemy.UniqueID)) continue;

        // Skip teammates (Independent has no team, so everyone is an enemy)
        if (playerTeam != PlayerTeam.Independent && enemy.GetTeam() == playerTeam) continue;

        // Enemy must be in a melee attack / kick animation
        if (!enemy.IsMeleeAttacking && !enemy.IsKicking &&
            !enemy.IsJumpAttacking && !enemy.IsJumpKicking) continue;

        Vector2 d = enemy.GetWorldPosition() - playerPos;

        if (Math.Abs(d.X) <= ARMORED_MELEE_RANGE &&
            Math.Abs(d.Y) <= ARMORED_MELEE_Y_TOLERANCE)
        {
            return true;
        }
    }

    return false;
}