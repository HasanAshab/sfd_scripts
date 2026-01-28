// Bot special ability tracking
private IPlayer timpaBot = null;
private IPlayer bichiBot = null;
private IPlayer kokolaBot = null;
private IPlayer edurBot = null;
private IPlayer xrayBot = null;
private IPlayer pakhiBot = null;

// Random number generator
private Random random = new Random();

// Timpa kill tracking - track who Timpa has hit and when
private Dictionary<int, float> timpaHitTargets = new Dictionary<int, float>();
private const float TIMPA_KILL_WINDOW = 3000; // 3 seconds to count as Timpa's kill

// Bot ability timers and tracking
private float bichiLastSpawnTime = -1;
private float kokolaLastSpawnTime = -1;
private float pakhiLastRegenTime = -1;
private float xrayLastLaserTime = -1;
private const float BICHI_SPAWN_INTERVAL = 13000; // 13 seconds
private const float KOKOLA_SPAWN_INTERVAL = 7000; // 7 seconds
private const float PAKHI_REGEN_INTERVAL = 500; // 0.5 second
private const float XRAY_LASER_INTERVAL = 10000; // 10 seconds

// Edur size tracking
private float edurOriginalSize = 1.0f;
private int edurOriginalHealth = 100;

// Team assignments (fixed teams)
private bool botsCreated = false;

public void OnStartup()
{
    SetupBots();    
    
    // Set up events
    Events.PlayerDamageCallback.Start(OnPlayerDamage);
    Events.PlayerDeathCallback.Start(OnPlayerDeath);
    Events.UpdateCallback.Start(OnUpdate, 100); // Check every 100ms
}


public void SetupBots()
{
    // Only create bots once
    if (botsCreated) return;
    
    // Get available spawn positions from path nodes
    IObjectPathNode[] pathNodes = Game.GetObjects<IObjectPathNode>().Where(path => 
        path.GetNodeEnabled() && 
        !path.GetIsElevatorNode() &&
        (path.GetPathNodeType() == PathNodeType.Ground || path.GetPathNodeType() == PathNodeType.Platform)
    ).ToArray();
    
    if (pathNodes.Length < 6) return; // Need at least 6 spawn positions
    
    // Sort path nodes by X position to group them spatially
    IObjectPathNode[] sortedNodes = pathNodes.OrderBy(n => n.GetWorldPosition().X).ToArray();
    
    // Team1 spawns on left side (first 3 unique nodes), Team2 on right side (last 3 unique nodes)
    Vector2[] team1Positions = new Vector2[3];
    Vector2[] team2Positions = new Vector2[3];
    
    // Get left side positions for Team1 (Timpa, Bichi, Kokola)
    for (int i = 0; i < 3 && i < sortedNodes.Length; i++)
    {
        team1Positions[i] = sortedNodes[i].GetWorldPosition();
    }
    
    // Get right side positions for Team2 (Edur, Xray, Pakhi)
    int rightStartIndex = Math.Max(sortedNodes.Length - 3, 3);
    for (int i = 0; i < 3; i++)
    {
        int nodeIndex = rightStartIndex + i;
        if (nodeIndex < sortedNodes.Length)
        {
            team2Positions[i] = sortedNodes[nodeIndex].GetWorldPosition();
        }
    }
    
    // Create Team1 bots (Normal AI): Timpa, Bichi, Kokola - left side
    timpaBot = Game.CreatePlayer(team1Positions[0]);
    if (timpaBot != null)
    {
        timpaBot.SetTeam(PlayerTeam.Team1);
        timpaBot.SetBotName("Timpa");
        timpaBot.SetBotBehavior(new BotBehavior(true, PredefinedAIType.BotA));
    }
    
    bichiBot = Game.CreatePlayer(team1Positions[1]);
    if (bichiBot != null)
    {
        bichiBot.SetTeam(PlayerTeam.Team1);
        bichiBot.SetBotName("Bichi");
        bichiBot.SetBotBehavior(new BotBehavior(true, PredefinedAIType.BotA));
    }
    
    kokolaBot = Game.CreatePlayer(team1Positions[2]);
    if (kokolaBot != null)
    {
        kokolaBot.SetTeam(PlayerTeam.Team1);
        kokolaBot.SetBotName("Kokola");
        kokolaBot.SetBotBehavior(new BotBehavior(true, PredefinedAIType.BotA));
    }
    
    // Create Team2 bots (Expert AI): Edur, Xray, Pakhi - right side
    edurBot = Game.CreatePlayer(team2Positions[0]);
    if (edurBot != null)
    {
        edurBot.SetTeam(PlayerTeam.Team2);
        edurBot.SetBotName("Edur");
        edurBot.SetBotBehavior(new BotBehavior(true, PredefinedAIType.BotD));
    }
    
    xrayBot = Game.CreatePlayer(team2Positions[1]);
    if (xrayBot != null)
    {
        xrayBot.SetTeam(PlayerTeam.Team2);
        xrayBot.SetBotName("Xray");
        xrayBot.SetBotBehavior(new BotBehavior(true, PredefinedAIType.BotD));
    }
    
    pakhiBot = Game.CreatePlayer(team2Positions[2]);
    if (pakhiBot != null)
    {
        pakhiBot.SetTeam(PlayerTeam.Team2);
        pakhiBot.SetBotName("Pakhi");
        pakhiBot.SetBotBehavior(new BotBehavior(true, PredefinedAIType.BotD));
    }
    
    botsCreated = true;
    
    // Initialize bot abilities
    InitializeBotAbilities();
}

private void InitializeBotAbilities()
{
    // Store Edur's original stats
    if (edurBot != null)
    {
        PlayerModifiers edurMods = edurBot.GetModifiers();
        edurOriginalSize = edurMods.SizeModifier;
        edurOriginalHealth = edurMods.MaxHealth;
    }
    
    // Give Xray laser pointer
    if (xrayBot != null)
    {
        xrayBot.GiveWeaponItem(WeaponItem.LAZER);
    }
    
    // Initialize timers
    float currentTime = Game.TotalElapsedGameTime;
    bichiLastSpawnTime = currentTime;
    kokolaLastSpawnTime = currentTime;
    pakhiLastRegenTime = currentTime;
    xrayLastLaserTime = currentTime;
}

public void OnPlayerDamage(IPlayer player, PlayerDamageArgs args)
{
    // Track Timpa's hits for kill attribution
    if (timpaBot != null && args.SourceID != 0 && args.SourceID == timpaBot.UniqueID)
    {
        // Timpa hit someone - record the hit time
        float currentTime = Game.TotalElapsedGameTime;
        timpaHitTargets[player.UniqueID] = currentTime;
    }
    
    // Handle Pakhi's HP transfer ability
    if (pakhiBot != null && player.UniqueID == pakhiBot.UniqueID && !pakhiBot.IsDead)
    {
        // Pakhi lost HP - transfer it to the attacker
        if (args.SourceID != 0)
        {
            IPlayer attacker = Game.GetPlayer(args.SourceID);
            if (attacker != null && !attacker.IsDead)
            {
                int damageAmount = (int)args.Damage;
                PlayerModifiers attackerMods = attacker.GetModifiers();
                attackerMods.CurrentHealth = Math.Min(attackerMods.MaxHealth, attackerMods.CurrentHealth + damageAmount);
                attacker.SetModifiers(attackerMods);

                // Visual effect for HP transfer
                Game.PlayEffect(EffectName.Blood, attacker.GetWorldPosition());
            }
        }
    }
}

public void OnPlayerDeath(IPlayer player, PlayerDeathArgs args)
{
    // Handle Timpa's strength boost on kill
    if (timpaBot != null && !timpaBot.IsDead)
    {
        // Check if Timpa hit this player within the last 3 seconds
        float currentTime = Game.TotalElapsedGameTime;
        if (timpaHitTargets.ContainsKey(player.UniqueID))
        {
            float hitTime = timpaHitTargets[player.UniqueID];
            if (currentTime - hitTime <= TIMPA_KILL_WINDOW)
            {
                // Timpa gets credit for this kill - give strength boost
                timpaBot.SetStrengthBoostTime(10000); // 15 seconds                
                // Remove the hit record since it's been processed
                timpaHitTargets.Remove(player.UniqueID);
            }
        }
    }
}

public void OnUpdate(float elapsed)
{
    float currentTime = Game.TotalElapsedGameTime;
    
    // Clean up old Timpa hit records (older than 3 seconds)
    CleanupTimpaHitRecords(currentTime);
    
    // Handle Timpa's low HP strength boost
    HandleTimpaLowHP();
    
    // Handle Bichi's drone spawning
    HandleBichiDrones(currentTime);
    
    // Handle Kokola's bot spawning
    HandleKokolaBots(currentTime);
    
    // Handle Edur's size/speed changes
    HandleEdurSizeSpeed();
    
    // Handle Xray's laser pointer
    HandleXrayLaser(currentTime);
    
    // Handle Pakhi's HP regeneration
    HandlePakhiRegen(currentTime);
}

private void CleanupTimpaHitRecords(float currentTime)
{
    // Remove hit records older than 3 seconds
    List<int> keysToRemove = new List<int>();
    foreach (var kvp in timpaHitTargets)
    {
        if (currentTime - kvp.Value > TIMPA_KILL_WINDOW)
        {
            keysToRemove.Add(kvp.Key);
        }
    }
    
    foreach (int key in keysToRemove)
    {
        timpaHitTargets.Remove(key);
    }
}

private void HandleTimpaLowHP()
{
    if (timpaBot == null || timpaBot.IsDead) return;
    
    PlayerModifiers timpaMods = timpaBot.GetModifiers();
    float hpPercentage = (float)timpaMods.CurrentHealth / timpaMods.MaxHealth;
    
    // If HP is 30% or below, give strength boost
    if (hpPercentage <= 0.3f)
    {
        timpaBot.SetStrengthBoostTime(10000); // 10 seconds
        Game.PlayEffect(EffectName.Sparks, timpaBot.GetWorldPosition());
    }
}

private void HandleBichiDrones(float currentTime)
{
    if (bichiBot == null || bichiBot.IsDead) return;
    
    // Check if it's time to spawn drones
    if (currentTime - bichiLastSpawnTime >= BICHI_SPAWN_INTERVAL)
    {
        Vector2 bichiPosition = bichiBot.GetWorldPosition();
        
        // Spawn bullet drone
        IObjectStreetsweeper bulletDrone = Game.CreateObject("streetsweeper", bichiPosition) as IObjectStreetsweeper;
        bulletDrone.SetShowNamePlate(false);
        bulletDrone.SetCameraSecondaryFocusMode(CameraFocusMode.Ignore);
        bulletDrone.SetOwnerPlayer(bichiBot);
        bulletDrone.SetOwnerTeam(bichiBot.GetTeam());
        bulletDrone.SetWeaponType(StreetsweeperWeaponType.MachineGun);
       
        // Spawn flamethrower drone
        IObjectStreetsweeper flameDrone = Game.CreateObject("streetsweeper", bichiPosition) as IObjectStreetsweeper;
        flameDrone.SetShowNamePlate(false);
        flameDrone.SetCameraSecondaryFocusMode(CameraFocusMode.Ignore);
        flameDrone.SetOwnerPlayer(bichiBot);
        flameDrone.SetOwnerTeam(bichiBot.GetTeam());
        flameDrone.SetWeaponType(StreetsweeperWeaponType.Flamethrower);
        
        bichiLastSpawnTime = currentTime;
        Game.PlayEffect(EffectName.Sparks, bichiPosition);
    }
}

private void HandleKokolaBots(float currentTime)
{
    if (kokolaBot == null || kokolaBot.IsDead) return;
    
    // Check if it's time to spawn a bot
    if (currentTime - kokolaLastSpawnTime >= KOKOLA_SPAWN_INTERVAL)
    {
        Vector2 kokolaPosition = kokolaBot.GetWorldPosition();
        
        // Spawn a random bot at Kokola's position
        IPlayer spawnedBot = Game.CreatePlayer(kokolaPosition);
        if (spawnedBot != null)
        {
            spawnedBot.SetNametagVisible(false);
            spawnedBot.SetStatusBarsVisible(false);
            spawnedBot.SetCameraSecondaryFocusMode(CameraFocusMode.Ignore);
            spawnedBot.SetTeam(kokolaBot.GetTeam());
            spawnedBot.SetBotBehaviorActive(true);
            spawnedBot.SetBotBehavior(new BotBehavior(true, PredefinedAIType.BotA));
            

            PlayerModifiers mods = spawnedBot.GetModifiers();
            mods.SizeModifier = 0.88f;
            mods.MaxHealth = 15; // 3x rookie health
            mods.CurrentHealth = 15;
            mods.MeleeDamageDealtModifier = 0.3f; // Very low melee damage (30%)
            mods.ProjectileDamageDealtModifier = 0.3f; // Very low projectile damage (30%)
            spawnedBot.SetModifiers(mods);
            
            // Give random weapon
            WeaponItem[] weapons = { WeaponItem.BAT, WeaponItem.BOTTLE, WeaponItem.PIPE, WeaponItem.CHAIR };
            WeaponItem randomWeapon = weapons[random.Next(0, weapons.Length)];
            spawnedBot.GiveWeaponItem(randomWeapon);
        }
        
        kokolaLastSpawnTime = currentTime;
        Game.PlayEffect(EffectName.Sparks, kokolaPosition);
    }
}

private void HandleEdurSizeSpeed()
{
    if (edurBot == null || edurBot.IsDead) return;
    
    PlayerModifiers edurMods = edurBot.GetModifiers();
    float hpPercentage = (float)edurMods.CurrentHealth / edurMods.MaxHealth;
    
    // Calculate size and speed based on HP loss
    // As HP decreases, size decreases and speed increases
    float sizeFactor = 0.5f + (hpPercentage * 0.5f); // Size ranges from 0.5x to 1.0x
    float speedFactor = 1.0f + ((1.0f - hpPercentage) * 1.5f); // Speed ranges from 1.0x to 2.5x
    
    edurMods.SizeModifier = edurOriginalSize * sizeFactor;
    edurMods.RunSpeedModifier = speedFactor;
    edurMods.SprintSpeedModifier = speedFactor;
    
    edurBot.SetModifiers(edurMods);
}

private void HandleXrayLaser(float currentTime)
{
    if (xrayBot == null || xrayBot.IsDead) return;
    
    // Give laser every 10 seconds to prevent popup spam
    if (currentTime - xrayLastLaserTime >= XRAY_LASER_INTERVAL)
    {
        xrayBot.GiveWeaponItem(WeaponItem.LAZER);
        xrayLastLaserTime = currentTime;
    }
}

private void HandlePakhiRegen(float currentTime)
{
    if (pakhiBot == null || pakhiBot.IsDead) return;
    
    // Check if it's time to regenerate HP
    if (currentTime - pakhiLastRegenTime >= PAKHI_REGEN_INTERVAL)
    {
        PlayerModifiers pakhiMods = pakhiBot.GetModifiers();
        
        // Regenerate 1 HP per second (no combat or energy requirements)
        if (pakhiMods.CurrentHealth < pakhiMods.MaxHealth)
        {
            pakhiMods.CurrentHealth = Math.Min(pakhiMods.MaxHealth, pakhiMods.CurrentHealth + 1);
            pakhiBot.SetModifiers(pakhiMods);
        }
        
        pakhiLastRegenTime = currentTime;
    }
}