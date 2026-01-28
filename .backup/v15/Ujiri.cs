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
    
    Vector2[] spawnPositions = new Vector2[6];
    
    if (pathNodes.Length >= 6)
    {
        // Enough path nodes - use random unique positions
        List<IObjectPathNode> shuffledNodes = pathNodes.ToList();
        for (int i = shuffledNodes.Count - 1; i > 0; i--)
        {
            int j = random.Next(0, i + 1);
            IObjectPathNode temp = shuffledNodes[i];
            shuffledNodes[i] = shuffledNodes[j];
            shuffledNodes[j] = temp;
        }
        
        for (int i = 0; i < 6; i++)
        {
            spawnPositions[i] = shuffledNodes[i].GetWorldPosition();
        }
    }
    else
    {
        // Not enough path nodes - spawn at human player positions based on teams
        IPlayer[] players = Game.GetPlayers();
        Vector2 team1SpawnPos = Vector2.Zero;
        Vector2 team2SpawnPos = Vector2.Zero;
        
        // Find human players and their team positions
        IPlayer p1 = players.Length >= 1 ? players[0] : null;
        IPlayer p2 = players.Length >= 2 ? players[1] : null;
        
        // Determine spawn positions based on team assignments
        if (p1 != null && p2 != null)
        {
            PlayerTeam p1Team = p1.GetTeam();
            PlayerTeam p2Team = p2.GetTeam();
            
            if (p1Team == PlayerTeam.Team1)
            {
                team1SpawnPos = p1.GetWorldPosition();
            }
            else if (p1Team == PlayerTeam.Team2)
            {
                team2SpawnPos = p1.GetWorldPosition();
            }
            
            if (p2Team == PlayerTeam.Team1)
            {
                team1SpawnPos = p2.GetWorldPosition();
            }
            else if (p2Team == PlayerTeam.Team2)
            {
                team2SpawnPos = p2.GetWorldPosition();
            }
            
            // If neither player is on Team1 or Team2, use default assignment
            if (team1SpawnPos == Vector2.Zero && team2SpawnPos == Vector2.Zero)
            {
                team1SpawnPos = p1.GetWorldPosition(); // Team1 bots spawn at P1
                team2SpawnPos = p2.GetWorldPosition(); // Team2 bots spawn at P2
            }
            else if (team1SpawnPos == Vector2.Zero)
            {
                // Only Team2 position found, use the other player for Team1
                team1SpawnPos = (p1Team != PlayerTeam.Team2) ? p1.GetWorldPosition() : p2.GetWorldPosition();
            }
            else if (team2SpawnPos == Vector2.Zero)
            {
                // Only Team1 position found, use the other player for Team2
                team2SpawnPos = (p1Team != PlayerTeam.Team1) ? p1.GetWorldPosition() : p2.GetWorldPosition();
            }
        }
        else if (p1 != null)
        {
            // Only one player - use their position for both teams
            team1SpawnPos = p1.GetWorldPosition();
            team2SpawnPos = p1.GetWorldPosition();
        }
        else
        {
            // No players found - use default positions (0,0)
            team1SpawnPos = Vector2.Zero;
            team2SpawnPos = Vector2.Zero;
        }
        
        // Assign spawn positions: Team1 bots (Timpa, Bichi, Kokola) and Team2 bots (Edur, Xray, Pakhi)
        spawnPositions[0] = team1SpawnPos; // Timpa
        spawnPositions[1] = team1SpawnPos; // Bichi
        spawnPositions[2] = team1SpawnPos; // Kokola
        spawnPositions[3] = team2SpawnPos; // Edur
        spawnPositions[4] = team2SpawnPos; // Xray
        spawnPositions[5] = team2SpawnPos; // Pakhi
    }
    
    // Create Team1 bots (Normal AI): Timpa, Bichi, Kokola
    timpaBot = Game.CreatePlayer(spawnPositions[0]);
    if (timpaBot != null)
    {
        timpaBot.SetTeam(PlayerTeam.Team1);
        timpaBot.SetBotName("Timpa");
        timpaBot.SetBotBehavior(new BotBehavior(true, PredefinedAIType.BotD));
        timpaBot.SetCameraSecondaryFocusMode(CameraFocusMode.Ignore);
        timpaBot.SetProfile(new IProfile()
        {
            Name = "Timpa",
            Gender = Gender.Male,
            Skin = new IProfileClothingItem("Normal", "Skin1", "ClothingLightGray"),
            Head = new IProfileClothingItem("Buzzcut", "ClothingDarkGray"),
            ChestUnder = new IProfileClothingItem("SleevelessShirt", "ClothingGreen"),
            Legs = new IProfileClothingItem("Skirt", "ClothingBlue"),
            Feet = new IProfileClothingItem("ShoesBlack", "ClothingBrown"),
        });
    }
    
    bichiBot = Game.CreatePlayer(spawnPositions[1]);
    if (bichiBot != null)
    {
        bichiBot.SetTeam(PlayerTeam.Team1);
        bichiBot.SetBotName("Bichi");
        bichiBot.SetBotBehavior(new BotBehavior(true, PredefinedAIType.BotD));
        bichiBot.SetCameraSecondaryFocusMode(CameraFocusMode.Ignore);
        bichiBot.SetProfile(new IProfile()
{
    Name = "Bichi",
    Gender = Gender.Male,
    Skin = new IProfileClothingItem("Normal", "Skin3", "ClothingLightGreen"),
    ChestUnder = new IProfileClothingItem("Sweater", "ClothingGreen"),
    Hands = new IProfileClothingItem("GlovesBlack", "ClothingLightGray"),
    Legs = new IProfileClothingItem("PantsBlack", "ClothingBlue"),
    Feet = new IProfileClothingItem("ShoesBlack", "ClothingBrown"),
});
    }
    
    kokolaBot = Game.CreatePlayer(spawnPositions[2]);
    if (kokolaBot != null)
    {
        kokolaBot.SetTeam(PlayerTeam.Team1);
        kokolaBot.SetBotName("Kokola");
        kokolaBot.SetBotBehavior(new BotBehavior(true, PredefinedAIType.BotD));
        kokolaBot.SetCameraSecondaryFocusMode(CameraFocusMode.Ignore);
        kokolaBot.SetProfile(new IProfile()
{
    Name = "Kokola",
    Gender = Gender.Female,
    Skin = new IProfileClothingItem("Normal_fem", "Skin4", "ClothingLightGreen"),
    Head = new IProfileClothingItem("Buzzcut", "ClothingDarkGray"),
    ChestUnder = new IProfileClothingItem("SleevelessShirt_fem", "ClothingLightGray"),
    Legs = new IProfileClothingItem("Skirt_fem", "ClothingBlue"),
    Feet = new IProfileClothingItem("ShoesBlack", "ClothingBrown"),
});
    }
    
    // Create Team2 bots (Expert AI): Edur, Xray, Pakhi
    edurBot = Game.CreatePlayer(spawnPositions[3]);
    if (edurBot != null)
    {
        edurBot.SetTeam(PlayerTeam.Team2);
        edurBot.SetBotName("Edur");
        edurBot.SetBotBehavior(new BotBehavior(true, PredefinedAIType.BotA));
        edurBot.SetCameraSecondaryFocusMode(CameraFocusMode.Ignore);
        edurBot.SetProfile(new IProfile()
         {
    Name = "Edur",
    Gender = Gender.Male,
    Skin = new IProfileClothingItem("BearSkin", ""),
    ChestUnder = new IProfileClothingItem("Shirt", "ClothingLightGray"),
    Legs = new IProfileClothingItem("PantsBlack", "ClothingDarkGray"),
    Feet = new IProfileClothingItem("ShoesBlack", "ClothingBrown"),
});
    }

    xrayBot = Game.CreatePlayer(spawnPositions[4]);
    if (xrayBot != null)
    {
        xrayBot.SetTeam(PlayerTeam.Team2);
        xrayBot.SetBotName("Xray");
        xrayBot.SetBotBehavior(new BotBehavior(true, PredefinedAIType.BotA));
        xrayBot.SetCameraSecondaryFocusMode(CameraFocusMode.Ignore);
        xrayBot.SetProfile(new IProfile(){
    Name = "Xray",
    Gender = Gender.Female,
    Skin = new IProfileClothingItem("Normal_fem", "Skin1", "ClothingLightGreen"),
    Head = new IProfileClothingItem("Buzzcut", "ClothingDarkGray"),
    ChestUnder = new IProfileClothingItem("SleevelessShirt_fem", "ClothingLightGray"),
    Legs = new IProfileClothingItem("Skirt_fem", "ClothingBlue"),
    Feet = new IProfileClothingItem("ShoesBlack", "ClothingBrown"),
    Accesory = new IProfileClothingItem("Glasses", "ClothingLightGray", "ClothingLightGray"),
});
    }
    
    pakhiBot = Game.CreatePlayer(spawnPositions[5]);
    if (pakhiBot != null)
    {
        pakhiBot.SetTeam(PlayerTeam.Team2);
        pakhiBot.SetBotName("Pakhi");
        pakhiBot.SetBotBehavior(new BotBehavior(true, PredefinedAIType.BotA));
        pakhiBot.SetCameraSecondaryFocusMode(CameraFocusMode.Ignore);
        pakhiBot.SetProfile(new IProfile()
{
    Name = "Pakhi",
    Gender = Gender.Female,
    Skin = new IProfileClothingItem("Normal_fem", "Skin3", "ClothingLightGreen"),
    Head = new IProfileClothingItem("SantaHat", "ClothingLightGray"),
    ChestOver = new IProfileClothingItem("Coat_fem", "ClothingLightGray", "ClothingLightGray"),
    ChestUnder = new IProfileClothingItem("SleevelessShirt_fem", "ClothingLightGray"),
    Legs = new IProfileClothingItem("Skirt_fem", "ClothingBlue"),
    Feet = new IProfileClothingItem("ShoesBlack", "ClothingBrown"),
});
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
            spawnedBot.SetBotBehavior(new BotBehavior(true, PredefinedAIType.BotD));
            

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