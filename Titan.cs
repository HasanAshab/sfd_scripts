// Titan Transformation Script
// When a player picks up strength boost, they transform into a Titan with enhanced abilities
// Titan Types: Pure Titan (50% chance) and Attack Titan (50% chance)

// Titan type enum
private enum TitanType
{
    Pure,
    Attack,
    Jaw
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
private const float HARDENING_ENERGY_COST_PER_SECOND = 0.30f; // 30% of max energy per second
private const float HARDENING_OFFENSIVE_DAMAGE_BONUS = 0.25f; // 25% melee damage increase
private const float HARDENING_OFFENSIVE_STAGGER_CHANCE = 0.70f; // 70% stagger chance
private const float HARDENING_DEFENSIVE_SPEED_PENALTY = 0.20f; // 20% speed reduction
private const float HARDENING_DEFENSIVE_DEFENSE_BONUS = 0.90f; // 90% damage reduction
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
private const float JAW_TITAN_ENERGY_BONUS = 0.25f; // 25% energy increase
private const float JAW_TITAN_SIZE = 0.880f; // Fixed size

// Jaw Titan claw ability
private const float JAW_CLAW_BASE_DAMAGE = 7f;
private const float JAW_CLAW_FRONT_MULTIPLIER = 2f; // Front attack multiplier
private const float JAW_CLAW_BACK_MULTIPLIER = 4f; // Backstab multiplier
private const float JAW_CLAW_GIB_THRESHOLD = 0.30f; // 30% overkill = gib

// Jaw Titan hunt ability
private const float JAW_HUNT_VICTIM_STUN_DURATION = 2000f; // 2 seconds input disable after hunt

// Jaw Titan sharp claw ability
private const int JAW_SHARP_CLAW_PUNCH_COUNT = 3; // 3rd punch breaks objects
private const int JAW_SHARP_CLAW_PUNCH_WINDOW = 600; // 600ms window between punches

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
private const float TITAN_EXHAUSTION_DURATION = 6000f; // 6 seconds in milliseconds

// Track titan sizes for stat calculations
private Dictionary<int, float> titanSizes = new Dictionary<int, float>();

// Track grab damage for titans
private Dictionary<int, GrabDamageData> titanGrabDamage = new Dictionary<int, GrabDamageData>();

// Track titans being grabbed (to manage input disable/enable for attackers)
private Dictionary<int, bool> titanInputDisabled = new Dictionary<int, bool>();

// Track Jaw Titan specific data
private Dictionary<int, int> jawTitanPunchCount = new Dictionary<int, int>(); // Track punch count for sharp claw
private Dictionary<int, float> jawTitanLastPunchTime = new Dictionary<int, float>(); // Track last punch time
private Dictionary<int, IPlayer> jawTitanHuntVictim = new Dictionary<int, IPlayer>(); // Track hunt victims (dive catches)
private Dictionary<int, float> jawTitanHuntDamageTime = new Dictionary<int, float>(); // Track hunt damage timing
private Dictionary<int, float> jawTitanHuntBloodEffectTime = new Dictionary<int, float>(); // Track hunt blood effect timing
private Dictionary<int, float> jawHuntVictimReenableTime = new Dictionary<int, float>(); // Track when to re-enable victim input

// Track player facing directions for Jaw Titan backstab detection
private Dictionary<int, int> playerFacingDirections = new Dictionary<int, int>();

// Track assigned titan powers (does not mean transformed, just assigned power)
private Dictionary<int, TitanType> assignedTitanPowers = new Dictionary<int, TitanType>();

// Track titan type labels above players' heads
private Dictionary<int, IObjectText> titanTypeLabels = new Dictionary<int, IObjectText>();

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
    
    // Distribute titan powers only once per script activation (not every match)
    // Use session storage to track if distribution has happened (persists across matches but cleared on map change)
    IScriptStorage sessionStorage = Game.SessionStorage;
    if (!sessionStorage.ContainsKey("POWERS_DISTRIBUTED"))
    {
        // Distribute titan powers after 1 second delay (wait for match to start)
        IObjectTimerTrigger distributionTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
        distributionTimer.SetIntervalTime(1000); // 1 second delay
        distributionTimer.SetRepeatCount(1); // Run once
        distributionTimer.SetScriptMethod("DistributeTitanPowers");
        distributionTimer.Trigger();
    }
    else
    {
        // Powers already distributed, restore from storage
        RestoreAssignedPowersFromStorage();
        
        // Recreate labels after 1 second (wait for players to spawn)
        IObjectTimerTrigger labelTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
        labelTimer.SetIntervalTime(1000);
        labelTimer.SetRepeatCount(1);
        labelTimer.SetScriptMethod("RecreateLabelsAfterMatchStart");
        labelTimer.Trigger();
    }

    // strength boost
    const float OTHERS_SCALE = 0.5f; // 1.0 = unchanged, 0.2 = others are 5x rarer
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

public void DistributeTitanPowers(TriggerArgs args)
{
    IPlayer[] allPlayers = Game.GetPlayers();
    int totalPlayers = allPlayers.Length;
    
    // Humanoid titans: Attack and Jaw (currently 2 types)
    const int HUMANOID_TITAN_COUNT = 2;
    
    if (totalPlayers == 0)
    {
        Game.ShowChatMessage("No players found for titan distribution!", Color.Red);
        return;
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
    
    // Save assigned powers to session storage (persists across matches)
    SaveAssignedPowersToStorage();
    
    // Mark powers as distributed in session storage (persists across matches)
    IScriptStorage sessionStorage = Game.SessionStorage;
    sessionStorage.SetItem("POWERS_DISTRIBUTED", "true");
    
    Game.ShowChatMessage("Titan powers distributed! Transform by picking up strength boost.", Color.Cyan);
}

private void SaveAssignedPowersToStorage()
{
    IScriptStorage sessionStorage = Game.SessionStorage;
    
    // Serialize assignedTitanPowers dictionary to a string format: "playerID:titanType,playerID:titanType,..."
    List<string> powerEntries = new List<string>();
    foreach (var kvp in assignedTitanPowers)
    {
        int playerId = kvp.Key;
        int titanTypeInt = (int)kvp.Value; // 0=Pure, 1=Attack, 2=Jaw
        powerEntries.Add(playerId.ToString() + ":" + titanTypeInt.ToString());
    }
    
    string serialized = string.Join(",", powerEntries.ToArray());
    sessionStorage.SetItem("ASSIGNED_POWERS", serialized);
}

private void RestoreAssignedPowersFromStorage()
{
    IScriptStorage sessionStorage = Game.SessionStorage;
    string serialized = (string)sessionStorage.GetItem("ASSIGNED_POWERS");
    
    if (string.IsNullOrEmpty(serialized)) return;
    
    // Deserialize: "playerID:titanType,playerID:titanType,..."
    assignedTitanPowers.Clear();
    
    string[] powerEntries = serialized.Split(',');
    foreach (string entry in powerEntries)
    {
        if (string.IsNullOrEmpty(entry)) continue;
        
        string[] parts = entry.Split(':');
        if (parts.Length != 2) continue;
        
        int playerId = int.Parse(parts[0]);
        int titanTypeInt = int.Parse(parts[1]);
        TitanType titanType = (TitanType)titanTypeInt;
        
        assignedTitanPowers[playerId] = titanType;
    }
}

public void RecreateLabelsAfterMatchStart(TriggerArgs args)
{
    IPlayer[] allPlayers = Game.GetPlayers();
    CreateTitanTypeLabels(allPlayers);
}
private void DistributeWithOriginalAlgorithm(IPlayer[] allPlayers)
{
    bool hasAttackTitan = false;
    bool hasJawTitan = false;
    
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
            // 50% chance for Attack or Jaw (but only one Jaw and one Attack total)
            if (!hasJawTitan && (rnd.Next(0, 2) == 0 || hasAttackTitan))
            {
                assignedType = TitanType.Jaw;
                hasJawTitan = true;
            }
            else if (!hasAttackTitan)
            {
                assignedType = TitanType.Attack;
                hasAttackTitan = true;
            }
            else
            {
                // Both special titans assigned, this player gets Pure
                assignedType = TitanType.Pure;
            }
        }
        
        assignedTitanPowers[playerId] = assignedType;
    }
    
    // Second pass: Ensure we have at least one of each type
    if (!hasAttackTitan || !hasJawTitan)
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
    
    // Assign humanoid titans (Attack and Jaw) to first half
    List<TitanType> humanoidTypes = new List<TitanType>();
    
    // Fill humanoid types list with equal distribution of Attack and Jaw
    for (int i = 0; i < humanoidCount; i++)
    {
        if (i % 2 == 0)
        {
            humanoidTypes.Add(TitanType.Attack);
        }
        else
        {
            humanoidTypes.Add(TitanType.Jaw);
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
            TitanType titanType = assignedTitanPowers[playerId];
            
            // Only create labels for humanoid titans (Attack and Jaw)
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
            else
            {
                // Pure Titan - no label
                continue;
            }
            
            // Create text label above player's head
            Vector2 playerPos = player.GetWorldPosition();
            IObjectText label = (IObjectText)Game.CreateObject("Text", 
                new Vector2(playerPos.X, playerPos.Y + 20f)); // 20 units above head
            
            label.SetText(labelText);
            label.SetTextAlignment(TextAlignment.Middle);
            label.SetTextScale(0.233f); // Decreased by 3x from 0.7f
            label.SetTextColor(labelColor);
            
            // Store the label reference
            titanTypeLabels[playerId] = label;
        }
    }
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
    else
    {
        TransformToJawTitan(player, playerId);
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
    else
    {
        titanTypeName = "JAW TITAN";
    }
    Game.ShowChatMessage(player.Name + " has transformed into a " + titanTypeName + "!", Color.Red);
}

private void TransformToPureTitan(IPlayer player, int playerId)
{
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
    
    // Clear all items
    player.RemoveWeaponItemType(WeaponItemType.Rifle);
    player.RemoveWeaponItemType(WeaponItemType.Handgun);
    player.RemoveWeaponItemType(WeaponItemType.Melee);
    player.RemoveWeaponItemType(WeaponItemType.Thrown);
    player.RemoveWeaponItemType(WeaponItemType.Powerup);
    
    // Apply size-based scaling
    ApplySizeScaling(player, playerId, mods);
}

private void TransformToAttackTitan(IPlayer player, int playerId)
{
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
    
    // Clear all items
    player.RemoveWeaponItemType(WeaponItemType.Rifle);
    player.RemoveWeaponItemType(WeaponItemType.Handgun);
    player.RemoveWeaponItemType(WeaponItemType.Melee);
    player.RemoveWeaponItemType(WeaponItemType.Thrown);
    player.RemoveWeaponItemType(WeaponItemType.Powerup);
    
    // Initialize Attack Titan hardening mode to Neutral
    attackTitanHardeningMode[playerId] = HardeningMode.Neutral;
    
    // Apply height-based scaling (different from Pure Titan's random size)
    ApplyAttackTitanHeightScaling(player, playerId, mods);
}

private void TransformToJawTitan(IPlayer player, int playerId)
{
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
    
    // Clear all items
    player.RemoveWeaponItemType(WeaponItemType.Rifle);
    player.RemoveWeaponItemType(WeaponItemType.Handgun);
    player.RemoveWeaponItemType(WeaponItemType.Melee);
    player.RemoveWeaponItemType(WeaponItemType.Thrown);
    player.RemoveWeaponItemType(WeaponItemType.Powerup);
    
    // Initialize Jaw Titan punch count
    jawTitanPunchCount[playerId] = 0;
    
    // Store titan size
    titanSizes[playerId] = JAW_TITAN_SIZE;
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
    
    // Check if player is an Attack Titan
    if (!titanPlayers.ContainsKey(playerId) || titanPlayers[playerId] != TitanType.Attack)
    {
        return;
    }
    
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
        
        if (mode == HardeningMode.Offensive && args.Length > 0)
        {
            // 70% chance to stagger hit players
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
    bool isJumpAttack = jawTitan.IsInMidAir;
    
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

public void OnPlayerDeath(IPlayer deadPlayer)
{
    if (deadPlayer == null) return;
    
    int deadPlayerId = deadPlayer.UniqueID;
    
    // Check if the dead player was a humanoid titan (Attack or Jaw)
    if (!assignedTitanPowers.ContainsKey(deadPlayerId)) return;
    
    TitanType deadPlayerTitanType = assignedTitanPowers[deadPlayerId];
    if (deadPlayerTitanType == TitanType.Pure) return; // Pure titans don't trigger power extract
    
    // Find the killer - check who dealt the last damage
    // We need to find if this was a gib kill by a Pure Titan
    IPlayer killer = null;
    int killerId = -1;
    
    // Search for a Pure Titan that recently damaged this player
    // This is done by checking grab damage data
    foreach (var kvp in titanGrabDamage)
    {
        if (kvp.Value.Victim != null && kvp.Value.Victim.UniqueID == deadPlayerId)
        {
            killerId = kvp.Key;
            killer = Game.GetPlayer(killerId);
            break;
        }
    }
    
    // Also check if killer can be found through any recent titan interaction
    // For simplicity, we check all Pure Titans for proximity
    if (killer == null)
    {
        IPlayer[] allPlayers = Game.GetPlayers();
        Vector2 deadPlayerPos = deadPlayer.GetWorldPosition();
        
        foreach (IPlayer player in allPlayers)
        {
            if (player == null || player.UniqueID == deadPlayerId) continue;
            
            // Check if this is a Pure Titan
            if (titanPlayers.ContainsKey(player.UniqueID) && titanPlayers[player.UniqueID] == TitanType.Pure)
            {
                // Check if close enough (within 50 units)
                Vector2 titanPos = player.GetWorldPosition();
                float distance = Vector2.Distance(deadPlayerPos, titanPos);
                
                if (distance <= 50f)
                {
                    killer = player;
                    killerId = player.UniqueID;
                    break;
                }
            }
        }
    }
    
    // If we found a Pure Titan killer, extract the power
    if (killer != null && killerId != -1)
    {
        // Verify the killer is a Pure Titan
        if (titanPlayers.ContainsKey(killerId) && titanPlayers[killerId] == TitanType.Pure)
        {
            ExtractTitanPower(killer, killerId, deadPlayer, deadPlayerId, deadPlayerTitanType);
        }
    }
}

private void ExtractTitanPower(IPlayer pureTitan, int pureTitanId, IPlayer victim, int victimId, TitanType extractedPower)
{
    Vector2 pureTitanPos = pureTitan.GetWorldPosition();
    
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
    
    // 2. Swap powers: Pure Titan becomes the humanoid titan, victim becomes Pure Titan
    TitanType tempPureTitanPower = assignedTitanPowers[pureTitanId];
    assignedTitanPowers[pureTitanId] = extractedPower; // Pure Titan gets humanoid power
    assignedTitanPowers[victimId] = tempPureTitanPower; // Victim gets Pure Titan power
    
    // 3. Update labels
    UpdateLabelsAfterPowerSwap(pureTitanId, victimId);
    
    // Save updated powers to shared storage (persist across matches)
    SaveAssignedPowersToStorage();
    
    // Update shared storage
    UpdateTitanPlayersStorage();
    
    // Show message
    string powerName = extractedPower == TitanType.Attack ? "ATTACK TITAN" : "JAW TITAN";
    Game.ShowChatMessage(pureTitan.Name + " extracted the " + powerName + " power from " + victim.Name + "!", Color.Yellow);
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
    
    if (player2 != null && !player2.IsRemoved && assignedTitanPowers.ContainsKey(player2Id))
    {
        CreateLabelForPlayer(player2, player2Id, assignedTitanPowers[player2Id]);
    }
}

private void CreateLabelForPlayer(IPlayer player, int playerId, TitanType titanType)
{
    // Only create labels for humanoid titans (Attack and Jaw)
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
                
                // Check titan falling state for impact effect
                bool isFalling = player.IsInMidAir && player.GetLinearVelocity().Y < -5;
                if (isFalling)
                {
                    OnTitanFalling(player);
                }
            }
            else if (titanType == TitanType.Attack)
            {
                // Attack Titan updates
                CheckAttackTitanSlowmo(player, playerId, currentTime);
                CheckAttackTitanHardeningEnergy(player, playerId, currentTime);
                CheckAttackTitanBotHardening(player, playerId, currentTime);
                
                // Attack Titan also has exhaustion
                CheckTitanExhaustion(player, playerId);
                
                // Check titan falling state for impact effect
                bool isFalling = player.IsInMidAir && player.GetLinearVelocity().Y < -5;
                if (isFalling)
                {
                    OnTitanFalling(player);
                }
            }
            else if (titanType == TitanType.Jaw)
            {
                // Jaw Titan updates
                CheckJawTitanHuntDamage(player, playerId, currentTime);
                CheckTitanGrabDamage(player, playerId); // Jaw Titan also has grab damage
                
                // Jaw Titan also has exhaustion
                CheckTitanExhaustion(player, playerId);
                
                // Check titan falling state for impact effect
                bool isFalling = player.IsInMidAir && player.GetLinearVelocity().Y < -5;
                if (isFalling)
                {
                    OnTitanFalling(player);
                }
            }
        }
        
        // Update facing direction for all players (every update tick for accurate backstab detection)
        playerFacingDirections[playerId] = player.FacingDirection;
        
        // Check if this player needs input re-enabled after Jaw Titan hunt
        if (jawHuntVictimReenableTime.ContainsKey(playerId))
        {
            if (currentTime >= jawHuntVictimReenableTime[playerId])
            {
                player.SetInputEnabled(true);
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
        // Hunt ended - disable victim's input for 2 seconds
        victim.SetInputEnabled(false);
        
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
        return;
    }
    
    float currentTime = Game.TotalElapsedRealTime;
    
    // Check all players to find who the titan is grabbing
    IPlayer[] allPlayers = Game.GetPlayers();
    IPlayer currentVictim = null;
    
    foreach (IPlayer player in allPlayers)
    {
        if (player == null || player.IsDead || player.UniqueID == titanId) continue;
        
        // Check if this player is caught by the titan's grab
        if (player.IsCaughtByPlayerInGrab)
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
