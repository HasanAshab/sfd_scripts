// ============================================================================
// TITANS MOD - Superfighters Deluxe Script
// ============================================================================
// Paste this directly into a Script tab in the Map Editor (same format as
// your SuperDS.cs - just fields + methods, no "using"/class wrapper needed,
// the editor already inherits GameScriptInterface for you).
//
// What it does:
//  - Spawns randomized giant "Titan" bots (Team2) on the map's spawn nodes
//    ("PlayerSpawnArea" tiles). Interval starts at 40s and gradually shrinks
//    as the game goes on (progressive difficulty ramp).
//  - Each titan gets a fully random appearance (gender/skin/clothes/colors -
//    pulled live from the game's own clothing lists, so it's always valid),
//    a random size (1.02x - 1.8x), random HP (70 - 350), and aggressive
//    melee AI.
//  - Also spawns one stationary Team2 "guard" bot on a spawn node at
//    startup with no bot behavior at all, so it never moves.
//  - When a titan dies, it evaporates into steam a few seconds later
//    instead of leaving a corpse.
//  - Tracks and displays how many titans P1 and P2 have each killed.
//
// Tweak the constants below to taste.
// ============================================================================

// ----- Spawn interval ramp -----
private const float SPAWN_INTERVAL_START_MS = 50000f; // Interval at game start (40s)
private const float SPAWN_INTERVAL_MIN_MS = 10000f;   // Interval never drops below this (10s)
private const float SPAWN_INTERVAL_DECAY_PER_MIN = 2000f; // Interval shrinks by this much per minute elapsed

// ----- Titan stats -----
private const float TITAN_SIZE_MIN = 1.02f;
private const float TITAN_SIZE_MAX = 1.8f;
private const int TITAN_HP_MIN = 40;
private const int TITAN_HP_MAX = 250;
private const float EVAPORATE_DELAY_MS = 3500f; // Delay before a dead titan's corpse vanishes

// Pool of predefined AI types to pick from for titan behavior (weighted by repetition).
private static readonly PredefinedAIType[] TitanAITypes = new PredefinedAIType[]
{
    PredefinedAIType.Hulk,
    PredefinedAIType.Hulk,
    PredefinedAIType.MeleeB,
    PredefinedAIType.Grunt,
};

private Random titanRnd = new Random();
private List<IPlayer> titans = new List<IPlayer>();
private HashSet<int> titanEvaporating = new HashSet<int>();
private Dictionary<int, int> titanLastDamager = new Dictionary<int, int>(); // titan UniqueID -> attacker UniqueID
private List<Vector2> spawnNodePositions = new List<Vector2>();
private int titanCounter = 0;

private IPlayer p1 = null;
private IPlayer p2 = null;
private int p1Kills = 0;
private int p2Kills = 0;

public void OnStartup()
{
    CacheSpawnNodes();
    CacheP1P2();

    Events.PlayerDeathCallback.Start(OnTitanCheckDeath);
    Events.PlayerDamageCallback.Start(OnAnyPlayerDamage);

    // Spawn the stationary Team2 guard bot once, standing on a spawn node.
    SpawnStationaryGuard();

    // Spawn the first titan immediately...
    SpawnTitan(0f);

    // ...then keep spawning, with the interval shrinking over time.
    ScheduleNextTitanSpawn();
}

private void CacheSpawnNodes()
{
    spawnNodePositions.Clear();
    IObject[] nodes = Game.GetObjectsByName("PlayerSpawnArea");
    for (int i = 0; i < nodes.Length; i++)
    {
        if (nodes[i] != null)
        {
            spawnNodePositions.Add(nodes[i].GetWorldPosition());
        }
    }
}

private void CacheP1P2()
{
    IPlayer[] players = Game.GetPlayers();
    for (int i = 0; i < players.Length; i++)
    {
        IPlayer p = players[i];
        if (p == null || p.IsBot)
        {
            continue;
        }
        if (p1 == null)
        {
            p1 = p;
        }
        else if (p2 == null)
        {
            p2 = p;
            break;
        }
    }
}

private Vector2 GetRandomSpawnNodePosition()
{
    if (spawnNodePositions.Count == 0)
    {
        Game.WriteToConsole("TitansMod: No 'PlayerSpawnArea' nodes found on this map - spawning at (0,0).");
        return new Vector2(0f, 0f);
    }
    return spawnNodePositions[titanRnd.Next(spawnNodePositions.Count)];
}

// ----------------------------------------------------------------------
// Stationary Team2 guard bot
// ----------------------------------------------------------------------

private void SpawnStationaryGuard()
{
    Vector2 pos = GetRandomSpawnNodePosition();

    IPlayer guard = Game.CreatePlayer(pos);
    if (guard == null)
    {
        return;
    }

    guard.SetTeam(PlayerTeam.Team2);
    guard.SetBotBehavior(new BotBehavior(false, PredefinedAIType.None)); // No AI at all - never moves
    guard.SetBotName("Guard");
    guard.SetNametagVisible(false);
    guard.SetStatusBarsVisible(false);
}

// ----------------------------------------------------------------------
// Titan spawning with a shrinking interval
// ----------------------------------------------------------------------

private void ScheduleNextTitanSpawn()
{
    float elapsedMinutes = Game.TotalElapsedGameTime / 60000f;
    float interval = SPAWN_INTERVAL_START_MS - (SPAWN_INTERVAL_DECAY_PER_MIN * elapsedMinutes);
    if (interval < SPAWN_INTERVAL_MIN_MS)
    {
        interval = SPAWN_INTERVAL_MIN_MS;
    }

    Events.UpdateCallback.Start(TitanSpawnTick, (uint)interval, 1);
}

private void TitanSpawnTick(float elapsed)
{
    SpawnTitan(elapsed);
    ScheduleNextTitanSpawn();
}

public void SpawnTitan(float elapsed)
{
    Vector2 spawnPos = GetRandomSpawnNodePosition();

    IPlayer titan = Game.CreatePlayer(spawnPos);
    if (titan == null)
    {
        return;
    }

    titanCounter++;

    titan.SetTeam(PlayerTeam.Team2);
    titan.SetProfile(GenerateRandomTitanProfile());
    titan.SetBotName("Titan #" + titanCounter);
    titan.SetNametagVisible(true);

    PredefinedAIType aiType = TitanAITypes[titanRnd.Next(TitanAITypes.Length)];
    titan.SetBotBehavior(new BotBehavior(true, aiType));

    PlayerModifiers mods = titan.GetModifiers();
    mods.SizeModifier = TITAN_SIZE_MIN + (float)titanRnd.NextDouble() * (TITAN_SIZE_MAX - TITAN_SIZE_MIN);
    mods.MaxHealth = titanRnd.Next(TITAN_HP_MIN, TITAN_HP_MAX + 1);
    mods.CurrentHealth = mods.MaxHealth;
    mods.MeleeDamageDealtModifier = 2.5f + (float)titanRnd.NextDouble() * 1.5f; // 2.5x - 4x melee damage
    mods.MeleeForceModifier = 3f;
    mods.MeleeDamageTakenModifier = 0.5f;
    mods.ProjectileDamageTakenModifier = 0.6f;
    mods.ExplosionDamageTakenModifier = 0.6f;
    mods.RunSpeedModifier = 0.8f;
    mods.SprintSpeedModifier = 0.8f;
    titan.SetModifiers(mods);

    titans.Add(titan);

    Game.PlayEffect(EffectName.Steam, spawnPos);
    Game.ShowPopupMessage("A Titan has appeared!");
}

// ----------------------------------------------------------------------
// Random AoT-style appearance
// ----------------------------------------------------------------------

private IProfile GenerateRandomTitanProfile()
{
    IProfile profile = new IProfile();
    Gender gender = titanRnd.Next(2) == 0 ? Gender.Male : Gender.Female;

    profile.Name = "Titan";
    profile.Gender = gender;

    // Get random skin color from available skin colors
    string[] skinColors = new string[]
    {
        "Skin1", "Skin2", "Skin3", "Skin4", "Skin5", "Skin6", "Skin7", "Skin8", "Skin9"
    };
    string randomSkinColor = skinColors[titanRnd.Next(skinColors.Length)];

    // Set skin with random color
    profile.Skin = new IProfileClothingItem("Normal", randomSkinColor, "ClothingBrown");

    // Everything else is naked (empty)
    profile.Hands = new IProfileClothingItem();
    profile.Feet = new IProfileClothingItem();
    profile.Legs = new IProfileClothingItem();
    profile.Waist = new IProfileClothingItem();
    profile.ChestOver = new IProfileClothingItem();
    profile.ChestUnder = new IProfileClothingItem();

    // Randomly pick ONE item from the list (or nothing)
    // 0 = SantaMask, 1 = AviatorHat2, 2 = Buzzcut, 3 = Afro, 4 = Nothing
    int itemChoice = titanRnd.Next(5);

    switch (itemChoice)
    {
        case 0:
            profile.Accessory = new IProfileClothingItem("SantaMask", "");
            profile.Head = new IProfileClothingItem();
            break;
        case 1:
            profile.Head = new IProfileClothingItem("AviatorHat2", "ClothingDarkGray", "ClothingLightGray");
            profile.Accessory = new IProfileClothingItem();
            break;
        case 2:
            profile.Head = new IProfileClothingItem("Buzzcut", "ClothingDarkGray");
            profile.Accessory = new IProfileClothingItem();
            break;
        case 3:
            profile.Head = new IProfileClothingItem("Afro", "ClothingLightGray");
            profile.Accessory = new IProfileClothingItem();
            break;
        case 4:
            // Nothing - completely naked
            profile.Head = new IProfileClothingItem();
            profile.Accessory = new IProfileClothingItem();
            break;
    }

    return profile;
}

// ----------------------------------------------------------------------
// Kill tracking (P1 / P2 only)
// ----------------------------------------------------------------------

public void OnAnyPlayerDamage(IPlayer player, PlayerDamageArgs args)
{
    if (player == null || !titans.Contains(player))
    {
        return;
    }

    int attackerID = -1;

    if (args.DamageType == PlayerDamageEventType.Melee)
    {
        attackerID = args.SourceID;
    }
    else if (args.DamageType == PlayerDamageEventType.Projectile)
    {
        IProjectile proj = Game.GetProjectile(args.SourceID);
        if (proj != null)
        {
            attackerID = proj.OwnerPlayerID;
        }
    }

    if (attackerID > 0)
    {
        titanLastDamager[player.UniqueID] = attackerID;
    }
}

private void AttributeTitanKill(IPlayer titan)
{
    int attackerID;
    if (!titanLastDamager.TryGetValue(titan.UniqueID, out attackerID))
    {
        return;
    }

    IPlayer killer = Game.GetPlayer(attackerID);
    if (killer != null)
    {
        if (p1 != null && killer.UniqueID == p1.UniqueID)
        {
            p1Kills++;
            ShowKillStats();
        }
        else if (p2 != null && killer.UniqueID == p2.UniqueID)
        {
            p2Kills++;
            ShowKillStats();
        }
    }

    titanLastDamager.Remove(titan.UniqueID);
}

private void ShowKillStats()
{
    Game.ShowPopupMessage(string.Format("Titans slain - P1: {0}  |  P2: {1}", p1Kills, p2Kills));
}

// ----------------------------------------------------------------------
// Death / cleanup - titans evaporate into steam a few seconds after dying
// ----------------------------------------------------------------------

public void OnTitanCheckDeath(IPlayer player, PlayerDeathArgs args)
{
    if (player == null || !titans.Contains(player))
    {
        return;
    }

    if (args.Killed && !titanEvaporating.Contains(player.UniqueID))
    {
        AttributeTitanKill(player);

        int uid = player.UniqueID;
        titanEvaporating.Add(uid);

        Events.UpdateCallback.Start((float e) =>
        {
            IPlayer corpse = Game.GetPlayer(uid);
            if (corpse != null)
            {
                Game.PlayEffect(EffectName.Steam, corpse.GetWorldPosition());
                corpse.Remove();
                titans.Remove(corpse);
            }
            titanEvaporating.Remove(uid);
        }, (uint)EVAPORATE_DELAY_MS, 1);
    }

    if (args.Removed)
    {
        titans.Remove(player);
        titanEvaporating.Remove(player.UniqueID);
        titanLastDamager.Remove(player.UniqueID);
    }
}