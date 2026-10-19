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
    Armored
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
private const float ATTACK_TITAN_MELEE_DAMAGE_BONUS = 0.32f; // 32% melee damage increase
private const float ATTACK_TITAN_MELEE_FORCE_BONUS = 0.01f; // 1% melee force increase
private const float ATTACK_TITAN_SPEED_PENALTY = 0.20f; // 20% speed reduction
private const float ATTACK_TITAN_ENERGY_BONUS = 0.30f; // 30% energy increase

// Attack Titan hardening costs and bonuses
private const float HARDENING_ENERGY_COST_PER_SECOND = 0.14f; // 14% of max energy per second
private const float HARDENING_OFFENSIVE_DAMAGE_BONUS = 0.25f; // 25% melee damage increase
private const float HARDENING_OFFENSIVE_STAGGER_CHANCE = 0.70f; // 70% stagger chance
private const float HARDENING_DEFENSIVE_SPEED_PENALTY = 0.20f; // 20% speed reduction
private const float HARDENING_DEFENSIVE_DEFENSE_BONUS = 0.75f; // 75% damage reduction
private const float HARDENING_OFFENSIVE_TRIGGER_DISTANCE = 25f; // Distance to trigger offensive mode
private const float HARDENING_DEFENSIVE_CHECK_INTERVAL = 2000f; // Check every 2 seconds
private const float HARDENING_OFFENSIVE_MELEE_TIMEOUT = 2000f; // 2 seconds without melee = back to neutral
private const float HARDENING_LOW_ENERGY_THRESHOLD = 0.30f; // 30% energy threshold
private const float HARDENING_AVOID_TRIGGER_CHANCE = 0.80f; // 80% chance to avoid when low energy
private const float HARDENING_SWITCH_TO_NEUTRAL_CHANCE = 0.60f; // 60% chance to switch back when low energy
private const float AIM_DETECTION_RANGE = 800f; // Range for raycast aim detection

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
private const float JAW_HUNT_VICTIM_STUN_DURATION = 1000f; // 1 seconds input disable after hunt

// Jaw Titan sharp claw ability
private const int JAW_SHARP_CLAW_PUNCH_COUNT = 3; // 3rd punch breaks objects
private const int JAW_SHARP_CLAW_PUNCH_WINDOW = 600; // 600ms window between punches

// Armored Titan stats
private const float ARMORED_TITAN_HP_BONUS = 0.32f; // 32% HP increase
private const float ARMORED_TITAN_MELEE_DAMAGE_BONUS = 0.25f; // 25% melee damage increase
private const float ARMORED_TITAN_MELEE_FORCE_BONUS = 0.60f; // 60% melee force increase
private const float ARMORED_TITAN_SPEED_PENALTY = 0.50f; // 50% speed reduction
private const float ARMORED_TITAN_ENERGY_BONUS = 0f; // 0% energy increase
private const float ARMORED_TITAN_SIZE = 1.300f; // Fixed size

// Armored Titan armor mode bonuses (when OFF)
private const float ARMORED_ARMOR_OFF_SPEED_BONUS = 0.30f; // 30% speed increase
private const float ARMORED_ARMOR_OFF_ENERGY_REGEN_BONUS = 0.15f; // 15% energy regen increase

// Armored Titan armor mode triggers for bots
private const float ARMORED_ARMOR_ON_TRIGGER_DISTANCE = 35f; // Distance to trigger armor on
private const float ARMORED_ARMOR_ON_CHECK_INTERVAL = 1000f; // Check every 1 second

// Armored Titan rush ability
private const float ARMORED_RUSH_DAMAGE_BASE = 7f;
private const float ARMORED_RUSH_RECOIL_RATIO = 0.5f; // 50% recoil damage
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

// Track Armored Titan specific data
private Dictionary<int, bool> armoredTitanArmorOn = new Dictionary<int, bool>(); // Track armor state (true = on, false = off)
private Dictionary<int, float> armoredTitanLastArmorCheck = new Dictionary<int, float>(); // Track last armor check time for bots
private Dictionary<int, Dictionary<int, float>> armoredTitanRushHitTimes = new Dictionary<int, Dictionary<int, float>>(); // Track rush hit times per target
private Dictionary<int, bool> armoredTitanIsRushing = new Dictionary<int, bool>(); // Track if bot is currently rushing
private Dictionary<int, int> armoredTitanPunchCount = new Dictionary<int, int>(); // Track punch count for super punch
private Dictionary<int, float> armoredTitanLastPunchTime = new Dictionary<int, float>(); // Track last punch time

// Track player facing directions for Jaw Titan backstab detection
private Dictionary<int, int> playerFacingDirections = new Dictionary<int, int>();

// Track assigned titan powers (does not mean transformed, just assigned power)
private Dictionary<int, TitanType> assignedTitanPowers = new Dictionary<int, TitanType>();

// Track player name to ID mapping for storage/restoration
private Dictionary<string, int> playerNameToId = new Dictionary<string, int>();

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
    const float OTHERS_SCALE = 0.75f; // 1.0 = unchanged, 0.2 = others are 5x rarer
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
    // Distribute titan powers using SharedStorage with player group tracking
    // This runs 500ms after OnStartup to ensure all players are spawned
    IScriptStorage persistentStorage = Game.GetSharedStorage("TitanPowerStorage");
    string today = GetTodayDateString();
    
    // Get current player group key (sorted list of non-bot player names)
    string playerGroupKey = GetPlayerGroupKey();
    
    Game.ShowChatMessage("Player Group Key: " + playerGroupKey, Color.Yellow);
    
    // Storage key for this specific player group's distribution date
    string dateKey = "DATE_" + playerGroupKey;
    string lastDistributionDate = "";
    
    if (persistentStorage.ContainsKey(dateKey))
    {
        lastDistributionDate = (string)persistentStorage.GetItem(dateKey);
        Game.ShowChatMessage("Last distribution for this group: " + lastDistributionDate, Color.Cyan);
    }
    else
    {
        Game.ShowChatMessage("No previous distribution found for this group", Color.Cyan);
    }
    
    Game.ShowChatMessage("Today: " + today, Color.Cyan);
    
    if (lastDistributionDate != today)
    {
        // New day for this player group - distribute powers immediately
        Game.ShowChatMessage("Distributing new powers...", Color.Green);
        DistributeTitanPowers(args);
    }
    else
    {
        // Same day - restore from storage for this player group
        Game.ShowChatMessage("Restoring powers from storage...", Color.Green);
        RestoreAssignedPowersFromStorage();
        
        // Recreate labels immediately
        IPlayer[] allPlayers = Game.GetPlayers();
        CreateTitanTypeLabels(allPlayers);
    }
    
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

// Helper method to get player group key (sorted list of non-bot player names)
private string GetPlayerGroupKey()
{
    IPlayer[] allPlayers = Game.GetPlayers();
    List<string> playerNames = new List<string>();
    
    foreach (IPlayer player in allPlayers)
    {
        if (player == null) continue;
        
        // Skip temporary players (case-insensitive)
        if (player.Name.ToLower() == TEMPORARY_PLAYER_NAME.ToLower())
        {
            continue;
        }
        
        playerNames.Add(player.Name);
    }
    
    // Sort alphabetically for consistent key generation
    playerNames.Sort();
    
    // Join with underscore to create unique key
    return string.Join("_", playerNames.ToArray());
}

public void DistributeTitanPowers(TriggerArgs args)
{
    IPlayer[] allPlayers = Game.GetPlayers();
    int totalPlayers = allPlayers.Length;
    
    // Humanoid titans: Attack, Jaw, and Armored (currently 3 types)
    const int HUMANOID_TITAN_COUNT = 3;
    
    if (totalPlayers == 0)
    {
        Game.ShowChatMessage("No players found for titan distribution!", Color.Red);
        return;
    }
    
    // Build player name to ID mapping
    playerNameToId.Clear();
    foreach (IPlayer player in allPlayers)
    {
        if (player != null)
        {
            playerNameToId[player.Name] = player.UniqueID;
        }
    }
    
    // Determine which scenario to use
    if (totalPlayers > HUMANOID_TITAN_COUNT)
    {
        // Scenario 1: More players than humanoid titans - use original algorithm
        DistributeWithOriginalAlgorithm(allPlayers);
    }
    else
    {
        // Scenario 2: Players <= humanoid titans - split half humanoid, half pure
        DistributeWithHalfSplit(allPlayers);
    }
    
    // Create text labels above players' heads with their titan type
    CreateTitanTypeLabels(allPlayers);
    
    // Save assigned powers to SharedStorage for this player group
    SaveAssignedPowersToStorage();
    
    // Save distribution date to SharedStorage for this player group
    IScriptStorage persistentStorage = Game.GetSharedStorage("TitanPowerStorage");
    string playerGroupKey = GetPlayerGroupKey();
    string dateKey = "DATE_" + playerGroupKey;
    persistentStorage.SetItem(dateKey, GetTodayDateString());
    
    Game.ShowChatMessage("Titan powers distributed! Transform by picking up strength boost.", Color.Cyan);
}

private void SaveAssignedPowersToStorage()
{
    IScriptStorage persistentStorage = Game.GetSharedStorage("TitanPowerStorage");
    
    // Get player group key for this distribution set
    string playerGroupKey = GetPlayerGroupKey();
    string powersKey = "POWERS_" + playerGroupKey;
    
    // Serialize assignedTitanPowers dictionary to a string format: "playerName:titanType;playerName:titanType;..."
    // Skip temporary players
    List<string> powerEntries = new List<string>();
    
    foreach (var kvp in assignedTitanPowers)
    {
        int playerId = kvp.Key;
        IPlayer player = Game.GetPlayer(playerId);
        
        // Skip if player not found or player is temporary
        if (player == null || player.Name.ToLower() == TEMPORARY_PLAYER_NAME.ToLower())
        {
            continue;
        }
        
        int titanTypeInt = (int)kvp.Value; // 0=Pure, 1=Attack, 2=Jaw
        powerEntries.Add(player.Name + ":" + titanTypeInt.ToString());
    }
    
    string serialized = string.Join(";", powerEntries.ToArray());
    persistentStorage.SetItem(powersKey, serialized);
}

private void RestoreAssignedPowersFromStorage()
{
    IScriptStorage persistentStorage = Game.GetSharedStorage("TitanPowerStorage");
    
    // Get player group key for this distribution set
    string playerGroupKey = GetPlayerGroupKey();
    string powersKey = "POWERS_" + playerGroupKey;
    
    if (!persistentStorage.ContainsKey(powersKey))
    {
        return;
    }
    
    string serialized = (string)persistentStorage.GetItem(powersKey);
    
    if (string.IsNullOrEmpty(serialized)) return;
    
    // Build player name to ID mapping first
    IPlayer[] allPlayers = Game.GetPlayers();
    playerNameToId.Clear();
    foreach (IPlayer player in allPlayers)
    {
        if (player != null)
        {
            playerNameToId[player.Name] = player.UniqueID;
        }
    }
    
    // Deserialize: "playerName:titanType;playerName:titanType;..."
    assignedTitanPowers.Clear();
    
    string[] powerEntries = serialized.Split(';');
    foreach (string entry in powerEntries)
    {
        if (string.IsNullOrEmpty(entry)) continue;
        
        string[] parts = entry.Split(':');
        if (parts.Length != 2) continue;
        
        string playerName = parts[0];
        int titanTypeInt = int.Parse(parts[1]);
        TitanType titanType = (TitanType)titanTypeInt;
        
        // Find player by name and assign power
        if (playerNameToId.ContainsKey(playerName))
        {
            int playerId = playerNameToId[playerName];
            assignedTitanPowers[playerId] = titanType;
        }
    }
    
    // For players without stored powers (new players or temporary players), assign randomly
    foreach (IPlayer player in allPlayers)
    {
        if (player == null) continue;
        
        int playerId = player.UniqueID;
        if (!assignedTitanPowers.ContainsKey(playerId))
        {
            // Assign Pure Titan to new players/temporary players
            assignedTitanPowers[playerId] = TitanType.Pure;
        }
    }
}
private void DistributeWithOriginalAlgorithm(IPlayer[] allPlayers)
{
    bool hasAttackTitan = false;
    bool hasJawTitan = false;
    bool hasArmoredTitan = false;
    
    // First pass: Assign powers randomly
    foreach (IPlayer player in allPlayers)
    {
        if (player == null) continue;
        
        int playerId = player.UniqueID;
        TitanType assignedType;
        
        // 50% chance to get Pure Titan
        if (rnd.Next(0, 2) == 0)
        {
            assignedType = TitanType.Pure;
        }
        else
        {
            // 50% chance for Attack, Jaw, or Armored (but only one of each total)
            int roll = rnd.Next(0, 3); // 0=Attack, 1=Jaw, 2=Armored
            
            if (roll == 0 && !hasAttackTitan)
            {
                assignedType = TitanType.Attack;
                hasAttackTitan = true;
            }
            else if (roll == 1 && !hasJawTitan)
            {
                assignedType = TitanType.Jaw;
                hasJawTitan = true;
            }
            else if (roll == 2 && !hasArmoredTitan)
            {
                assignedType = TitanType.Armored;
                hasArmoredTitan = true;
            }
            else
            {
                // All special titans assigned or this roll was taken, try others
                if (!hasAttackTitan)
                {
                    assignedType = TitanType.Attack;
                    hasAttackTitan = true;
                }
                else if (!hasJawTitan)
                {
                    assignedType = TitanType.Jaw;
                    hasJawTitan = true;
                }
                else if (!hasArmoredTitan)
                {
                    assignedType = TitanType.Armored;
                    hasArmoredTitan = true;
                }
                else
                {
                    // All special titans assigned, this player gets Pure
                    assignedType = TitanType.Pure;
                }
            }
        }
        
        assignedTitanPowers[playerId] = assignedType;
    }
    
    // Second pass: Ensure we have at least one of each type
    if (!hasAttackTitan || !hasJawTitan || !hasArmoredTitan)
    {
        // Find Pure Titan players to convert
        List<int> pureTitanPlayers = new List<int>();
        foreach (var kvp in assignedTitanPowers)
        {
            if (kvp.Value == TitanType.Pure)
            {
                pureTitanPlayers.Add(kvp.Key);
            }
        }
        
        // Randomly assign missing types from Pure players
        if (pureTitanPlayers.Count > 0)
        {
            int index = 0;
            if (!hasAttackTitan && index < pureTitanPlayers.Count)
            {
                assignedTitanPowers[pureTitanPlayers[index]] = TitanType.Attack;
                hasAttackTitan = true;
                index++;
            }
            if (!hasJawTitan && index < pureTitanPlayers.Count)
            {
                assignedTitanPowers[pureTitanPlayers[index]] = TitanType.Jaw;
                hasJawTitan = true;
                index++;
            }
            if (!hasArmoredTitan && index < pureTitanPlayers.Count)
            {
                assignedTitanPowers[pureTitanPlayers[index]] = TitanType.Armored;
                hasArmoredTitan = true;
            }
        }
    }
}

private void DistributeWithHalfSplit(IPlayer[] allPlayers)
{
    int totalPlayers = allPlayers.Length;
    int humanoidCount = totalPlayers / 2; // Half will be humanoid
    int pureCount = totalPlayers - humanoidCount; // Rest will be pure
    
    // Create a shuffled list of players
    List<IPlayer> shuffledPlayers = new List<IPlayer>(allPlayers);
    
    // Shuffle the player list
    for (int i = shuffledPlayers.Count - 1; i > 0; i--)
    {
        int j = rnd.Next(0, i + 1);
        IPlayer temp = shuffledPlayers[i];
        shuffledPlayers[i] = shuffledPlayers[j];
        shuffledPlayers[j] = temp;
    }
    
    // Assign humanoid titans (Attack, Jaw, and Armored) to first half
    List<TitanType> humanoidTypes = new List<TitanType>();
    
    // Fill humanoid types list with equal distribution of Attack, Jaw, and Armored
    for (int i = 0; i < humanoidCount; i++)
    {
        if (i % 3 == 0)
        {
            humanoidTypes.Add(TitanType.Attack);
        }
        else if (i % 3 == 1)
        {
            humanoidTypes.Add(TitanType.Jaw);
        }
        else
        {
            humanoidTypes.Add(TitanType.Armored);
        }
    }
    
    // Shuffle humanoid types for random assignment
    for (int i = humanoidTypes.Count - 1; i > 0; i--)
    {
        int j = rnd.Next(0, i + 1);
        TitanType temp = humanoidTypes[i];
        humanoidTypes[i] = humanoidTypes[j];
        humanoidTypes[j] = temp;
    }
    
    // Assign humanoid titans to first half of players
    for (int i = 0; i < humanoidCount; i++)
    {
        if (shuffledPlayers[i] != null)
        {
            assignedTitanPowers[shuffledPlayers[i].UniqueID] = humanoidTypes[i];
        }
    }
    
    // Assign Pure Titan to remaining players
    for (int i = humanoidCount; i < shuffledPlayers.Count; i++)
    {
        if (shuffledPlayers[i] != null)
        {
            assignedTitanPowers[shuffledPlayers[i].UniqueID] = TitanType.Pure;
        }
    }
}

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
        {
            existingLabel.Remove();
        }
        titanTypeLabels.Remove(playerId);
    }
    
    // Only create labels for humanoid titans (Attack, Jaw, and Armored)
    if (titanType == TitanType.Pure) return;
    
    string labelText = "";
    Color labelColor = Color.White;
    
    if (titanType == TitanType.Attack)
    {
        labelText = "[ATK]";
        labelColor = new Color(255, 100, 100); // Red
    }
    else if (titanType == TitanType.Jaw)
    {
        labelText = "[JAW]";
        labelColor = new Color(100, 255, 100); // Green
    }
    else if (titanType == TitanType.Armored)
    {
        labelText = "[ARM]";
        labelColor = new Color(255, 255, 0); // Yellow
    }
    else
    {
        return;
    }
    
    // Create text label above player's head
    Vector2 playerPos = player.GetWorldPosition();
    IObjectText label = (IObjectText)Game.CreateObject("Text", 
        new Vector2(playerPos.X, playerPos.Y + 20f)); // 20 units above head
    
    label.SetText(labelText);
    label.SetTextAlignment(TextAlignment.Middle);
    label.SetTextScale(0.4f);
    label.SetTextColor(labelColor);
    
    // Store the label reference
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
        Skin = new IProfileClothingItem("Normal", "Skin5", "ClothingLightGreen"),
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
    
    // Initialize Armored Titan armor state to ON
    armoredTitanArmorOn[playerId] = true;
    
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
    
    // Detach: human players (non-bot) can detach by pressing Walk key (non-Pure titans only)
    if (!player.IsBot && titanPlayers.ContainsKey(playerId) && titanPlayers[playerId] != TitanType.Pure)
    {
        foreach (VirtualKeyInfo keyInfo in keyInfos)
        {
            if (keyInfo.Event == VirtualKeyEvent.Pressed && keyInfo.Key == VirtualKey.WALKING)
            {
                DetachTitan(player, playerId);
                return; // Player is being replaced, stop processing
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
            // ACTIVATE key: Slowmo ability
            if (keyInfo.Event == VirtualKeyEvent.Pressed && keyInfo.Key == VirtualKey.ACTIVATE)
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
        // Update last melee time for offensive mode timeout
        attackTitanLastMeleeTime[playerId] = currentTime;
        
        // Check for offensive hardening stagger chance
        HardeningMode mode = HardeningMode.Neutral;
        if (attackTitanHardeningMode.ContainsKey(playerId))
        {
            mode = attackTitanHardeningMode[playerId];
        }
        
        if (mode == HardeningMode.Offensive && args.Length > 0 && !player.IsKicking && !player.IsJumpKicking)
        {
            // 70% chance to stagger hit players (punches only, not kicks)
            foreach (PlayerMeleeHitArg hitArg in args)
            {
                if (hitArg.HitObject != null && hitArg.IsPlayer)
                {
                    if (rnd.NextDouble() < HARDENING_OFFENSIVE_STAGGER_CHANCE)
                    {
                        IPlayer hitPlayer = Game.GetPlayer(hitArg.HitObject.UniqueID);
                        if (hitPlayer != null && !hitPlayer.IsDead)
                        {
                            QuickCommand(hitPlayer, new PlayerCommand(PlayerCommandType.Stagger));
                        }
                    }
                }
            }
        }
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
    
    // Check if the Pure Titan is a temporary player
    bool isTemporaryPlayer = pureTitan.Name.ToLower() == TEMPORARY_PLAYER_NAME.ToLower();
    
    // 1. Detransform Pure Titan with smoke effect
    Game.PlayEffect(EffectName.Steam, pureTitanPos);
    Game.PlayEffect(EffectName.Steam, pureTitanPos);
    Game.PlaySound("BarrelDestroyed", pureTitanPos);
    
    // Restore original profile and modifiers
    if (originalProfiles.ContainsKey(pureTitanId))
    {
        pureTitan.SetProfile(originalProfiles[pureTitanId]);
        originalProfiles.Remove(pureTitanId);
    }
    
    if (originalModifiers.ContainsKey(pureTitanId))
    {
        pureTitan.SetModifiers(originalModifiers[pureTitanId]);
        originalModifiers.Remove(pureTitanId);
    }
    
    // Restore original bot behavior and behavior set if bot
    if (pureTitan.IsBot)
    {
        if (originalBotBehaviors.ContainsKey(pureTitanId))
        {
            pureTitan.SetBotBehavior(originalBotBehaviors[pureTitanId]);
            originalBotBehaviors.Remove(pureTitanId);
        }
        
        if (originalBotBehaviorSets.ContainsKey(pureTitanId))
        {
            pureTitan.SetBotBehaviorSet(originalBotBehaviorSets[pureTitanId]);
            originalBotBehaviorSets.Remove(pureTitanId);
        }
    }
    
    // Make Pure Titan fall (simulate weakness after detransformation)
    QuickCommand(pureTitan, new PlayerCommand(PlayerCommandType.Fall));
    
    // Remove from titan players list
    titanPlayers.Remove(pureTitanId);
    
    // Clean up titan-specific data
    titanSizes.Remove(pureTitanId);
    titanGrabDamage.Remove(pureTitanId);
    titanInputDisabled.Remove(pureTitanId);
    titanExhausted.Remove(pureTitanId);
    titanExhaustionRecoverTime.Remove(pureTitanId);
    
    // 2. Handle power distribution based on whether extractor is temporary
    if (isTemporaryPlayer)
    {
        // Temporary player: Give power to random Pure Titan teammate
        IPlayer randomTeammatePureTitan = GetRandomPureTitanTeammate(pureTitan);
        
        if (randomTeammatePureTitan != null)
        {
            int teammateId = randomTeammatePureTitan.UniqueID;
            
            // Check if teammate has an assigned power
            if (!assignedTitanPowers.ContainsKey(teammateId))
            {
                Game.ShowChatMessage("Error: Teammate " + randomTeammatePureTitan.Name + " has no assigned power!", Color.Red);
                return;
            }
            
            TitanType teammatePower = assignedTitanPowers[teammateId];
            assignedTitanPowers[teammateId] = extractedPower; // Teammate gets humanoid power
            assignedTitanPowers[victimId] = teammatePower; // Victim gets teammate's old power
            
            // Keep temporary player's power unchanged (stays Pure)
            
            // Update labels
            UpdateLabelsAfterPowerSwap(teammateId, victimId);
            
            // Save updated powers
            SaveAssignedPowersToStorage();
            
            // Update shared storage
            UpdateTitanPlayersStorage();
            
            // Show message
            string powerName = "UNKNOWN";
            if (extractedPower == TitanType.Attack)
            {
                powerName = "ATTACK TITAN";
            }
            else if (extractedPower == TitanType.Jaw)
            {
                powerName = "JAW TITAN";
            }
            else if (extractedPower == TitanType.Armored)
            {
                powerName = "ARMORED TITAN";
            }
            Game.ShowChatMessage(pureTitan.Name + " extracted the " + powerName + " power and transferred it to " + randomTeammatePureTitan.Name + "!", Color.Yellow);
        }
        else
        {
            // No Pure Titan teammate found, power is lost
            Game.ShowChatMessage(pureTitan.Name + " extracted a power but had no teammate to transfer it to!", Color.Red);
        }
    }
    else
    {
        // Normal player: Standard power swap
        // Check if Pure Titan has an assigned power before swapping
        if (!assignedTitanPowers.ContainsKey(pureTitanId))
        {
            Game.ShowChatMessage("Error: Pure Titan " + pureTitan.Name + " has no assigned power!", Color.Red);
            return;
        }
        
        TitanType tempPureTitanPower = assignedTitanPowers[pureTitanId];
        assignedTitanPowers[pureTitanId] = extractedPower; // Pure Titan gets humanoid power
        assignedTitanPowers[victimId] = tempPureTitanPower; // Victim gets Pure Titan power (keep them in dictionary!)
        
        // Update labels
        UpdateLabelsAfterPowerSwap(pureTitanId, victimId);
        
        // Save updated powers to SharedStorage (permanent storage)
        SaveAssignedPowersToStorage();
        
        // Update shared storage
        UpdateTitanPlayersStorage();
        
        // Show message
        string powerName = "UNKNOWN";
        if (extractedPower == TitanType.Attack)
        {
            powerName = "ATTACK TITAN";
        }
        else if (extractedPower == TitanType.Jaw)
        {
            powerName = "JAW TITAN";
        }
        else if (extractedPower == TitanType.Armored)
        {
            powerName = "ARMORED TITAN";
        }
        Game.ShowChatMessage(pureTitan.Name + " extracted the " + powerName + " power from " + victim.Name + "!", Color.Yellow);
    }
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
        
        // Disable default powerup usage on all bots on map (track by bot ID)
        if (player.IsBot && !configuredBotPowerupUsage.Contains(playerId))
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
            }
            else if (titanType == TitanType.Armored)
            {
                // Armored Titan updates
                CheckArmoredTitanArmor(player, playerId, currentTime);
                CheckArmoredTitanBotArmor(player, playerId, currentTime);
                CheckArmoredTitanRush(player, playerId, currentTime);
                CheckArmoredTitanBotRush(player, playerId, currentTime);
                
                // Armored Titan also has exhaustion
                CheckTitanExhaustion(player, playerId);
            }
            
            // Bot detach check: non-Pure bot titans may detach based on HP percentage
            if (player.IsBot && titanType != TitanType.Pure)
            {
                CheckBotDetach(player, playerId, currentTime);
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
        // Calculate damage (same as grab damage)
        PlayerModifiers jawMods = jawTitan.GetModifiers();
        float damagePerInterval = GRAB_DAMAGE_BASE * jawMods.MeleeDamageDealtModifier * GRAB_DAMAGE_MULTIPLIER;
        
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
    bool oldArmorState = armoredTitanArmorOn.ContainsKey(playerId) ? armoredTitanArmorOn[playerId] : true;
    
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
            Skin = new IProfileClothingItem("Normal", "Skin5", "ClothingLightGreen"),
            Head = new IProfileClothingItem("Buzzcut", "ClothingLightGray"),
            Hands = new IProfileClothingItem("Gloves", "ClothingLightGray"),
            Legs = new IProfileClothingItem("Pants", "ClothingLightGray"),
            Feet = new IProfileClothingItem("Boots", "ClothingLightGray"),
            Accesory = new IProfileClothingItem("ClownMakeup", "ClothingLightGray"),
        });
    }
    else
    {
        // Turning ON: set armor ON profile
        player.SetProfile(new IProfile()
        {
            Gender = Gender.Male,
            Skin = new IProfileClothingItem("Normal", "Skin5", "ClothingLightGreen"),
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
    
    // Check if armor is on (no recoil if armor is on)
    bool armorOn = armoredTitanArmorOn.ContainsKey(playerId) ? armoredTitanArmorOn[playerId] : true;
    
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
        
        // Apply recoil if armor is OFF
        if (!armorOn)
        {
            player.DealDamage(damage * ARMORED_RUSH_RECOIL_RATIO);
        }
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
        
        // Apply recoil if armor is OFF
        float damage = ARMORED_RUSH_DAMAGE_BASE * titanMods.MeleeDamageDealtModifier;
        if (!armorOn)
        {
            player.DealDamage(damage * ARMORED_RUSH_RECOIL_RATIO);
        }
    }
    
    // Store updated hit times
    armoredTitanRushHitTimes[playerId] = hitTimes;
}

private void CheckArmoredTitanBotRush(IPlayer player, int playerId, float currentTime)
{
    // Only for bots
    if (!player.IsBot) return;
    
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
    return Math.Sign(dx) == facing;
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
    
    // Clean up all titan tracking data for this player BEFORE killing
    CleanupTitanData(titanPlayerId);
    
    // Create a new human player at the same position
    IPlayer humanPlayer = Game.CreatePlayer(titanPos);
    if (humanPlayer == null)
    {
        titanPlayer.Kill();
        return;
    }
    
    int humanPlayerId = humanPlayer.UniqueID;
    
    // Set team
    humanPlayer.SetTeam(titanTeam);
    
    // Set the human profile
    humanPlayer.SetProfile(humanProfile);
    
    // Set bot behavior to ZombieA immediately after creating the player (for both bot and human players)
    humanPlayer.SetBotBehavior(new BotBehavior(true, PredefinedAIType.ZombieA));
    
    // Set bot name if bot
    if (isBot)
    {
        humanPlayer.SetBotName(playerName);
    }
    
    // Re-attach user controller for human players
    if (user != null)
    {
        humanPlayer.SetUser(user);
    }
    
    // Kill the titan player (leaving the titan corpse on the ground to steam)
    titanPlayer.Kill();
    
    // Set full HP and full energy from original modifiers
    if (humanMods.MaxHealth <= 0) humanMods.MaxHealth = 100;
    if (humanMods.MaxEnergy <= 0) humanMods.MaxEnergy = 100;
    humanMods.CurrentHealth = humanMods.MaxHealth;
    humanMods.CurrentEnergy = humanMods.MaxEnergy;
    humanPlayer.SetModifiers(humanMods);
    
    // Set facing direction
    PlayerCommandFaceDirection faceDir = (titanFacing < 0) ? PlayerCommandFaceDirection.Left : PlayerCommandFaceDirection.Right;
    humanPlayer.AddCommand(new PlayerCommand(PlayerCommandType.FaceAt, faceDir));
    
    // After 1000ms, restore the real behavior for both bots and human players
    Events.UpdateCallback.Start((float dt) =>
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
            }
        }
    }, 1000, 1);
    
    // Mark as already given startup strength boost so they don't immediately get another one
    givenStartupStrengthBoost.Add(humanPlayerId);
    
    // Transfer assigned power to the new player ID
    if (assignedTitanPowers.ContainsKey(titanPlayerId))
    {
        TitanType power = assignedTitanPowers[titanPlayerId];
        assignedTitanPowers.Remove(titanPlayerId);
        assignedTitanPowers[humanPlayerId] = power;
        
        // Update label for the new player
        CreateLabelForPlayer(humanPlayer, humanPlayerId, power);
    }
    
    // Remove old label if exists
    if (titanTypeLabels.ContainsKey(titanPlayerId))
    {
        IObjectText label = titanTypeLabels[titanPlayerId];
        if (label != null && !label.IsRemoved)
        {
            label.Remove();
        }
        titanTypeLabels.Remove(titanPlayerId);
    }
    
    // Clean up old original data references
    originalProfiles.Remove(titanPlayerId);
    originalModifiers.Remove(titanPlayerId);
    originalBotBehaviors.Remove(titanPlayerId);
    originalBotBehaviorSets.Remove(titanPlayerId);
    
    // Update name mapping
    if (playerNameToId.ContainsKey(playerName))
    {
        playerNameToId[playerName] = humanPlayerId;
    }
    
    // Ensure the new bot gets PowerupUsage disabled
    if (isBot && !configuredBotPowerupUsage.Contains(humanPlayerId))
    {
        BotBehaviorSet bS = humanPlayer.GetBotBehaviorSet();
        bS.PowerupUsage = false;
        humanPlayer.SetBotBehaviorSet(bS);
        configuredBotPowerupUsage.Add(humanPlayerId);
    }
    
    // Update shared storage to notify odm.cs
    UpdateTitanPlayersStorage();
    
    // Save updated powers
    SaveAssignedPowersToStorage();
    
    // Make the human fall (weakness after detransformation)
    QuickCommand(humanPlayer, new PlayerCommand(PlayerCommandType.Fall));
    
    // Show detach message
    Game.ShowChatMessage(playerName + " has detached from their Titan form!", Color.Cyan);
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
    
    // Clean up Jaw Titan data
    jawTitanPunchCount.Remove(titanPlayerId);
    jawTitanLastPunchTime.Remove(titanPlayerId);
    jawTitanHuntVictim.Remove(titanPlayerId);
    jawTitanHuntDamageTime.Remove(titanPlayerId);
    jawTitanHuntBloodEffectTime.Remove(titanPlayerId);
    jawHuntVictimReenableTime.Remove(titanPlayerId);
    
    // Clean up Armored Titan data
    armoredTitanArmorOn.Remove(titanPlayerId);
    armoredTitanLastArmorCheck.Remove(titanPlayerId);
    armoredTitanRushHitTimes.Remove(titanPlayerId);
    armoredTitanIsRushing.Remove(titanPlayerId);
    armoredTitanPunchCount.Remove(titanPlayerId);
    armoredTitanLastPunchTime.Remove(titanPlayerId);
    
    // Clean up bot detach check
    botLastDetachCheck.Remove(titanPlayerId);
    botLastStrengthBoostCheck.Remove(titanPlayerId);
    
    // Clean up facing direction
    playerFacingDirections.Remove(titanPlayerId);
    
    // Clean up strength boost tracking
    givenStartupStrengthBoost.Remove(titanPlayerId);
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
