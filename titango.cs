
// ----- Spawn interval ramp -----
private const float SPAWN_INTERVAL_START_MS = 50000f; // Interval at game start (40s)
private const float SPAWN_INTERVAL_MIN_MS = 10000f;   // Interval never drops below this (10s)
private const float SPAWN_INTERVAL_DECAY_PER_MIN = 2000f; // Interval shrinks by this much per minute elapsed

private const float EVAPORATE_DELAY_MS = 3500f; // Delay before a dead titan's corpse vanishes

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
// Stationary Team2 guard bot (to prevent auto restart)
// ----------------------------------------------------------------------

private void SpawnStationaryGuard()
{
    Vector2 pos = GetRandomSpawnNodePosition();

    IPlayer guard = Game.CreatePlayer(pos);
    if (guard == null)
    {
        return;
    }

    PlayerModifiers guardmods = guard.GetModifiers();
    guardmods.MaxHealth = 1000;
    guardmods.CurrentHealth = 1000;
    guard.SetModifiers(guardmods);

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
    titan.SetNametagVisible(false);

    // Strength boost: give the powerup, then make the titan use it
    titan.GiveWeaponItem(WeaponItem.STRENGTHBOOST);
    titan.AddCommand(new PlayerCommand(PlayerCommandType.DrawPowerup));

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