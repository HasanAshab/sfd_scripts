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
private const float BICHI_SPAWN_INTERVAL = 10000; // 10 seconds
private const float KOKOLA_SPAWN_INTERVAL = 5000; // 5 seconds
private const float PAKHI_REGEN_INTERVAL = 1000; // 1 second

// Edur size tracking
private float edurOriginalSize = 1.0f;
private int edurOriginalHealth = 100;

// Team assignments (fixed after first assignment)
private PlayerTeam[] botTeams = new PlayerTeam[6];
private bool teamsAssigned = false;

public void OnStartup()
{
    // Wait a moment for all players to spawn, then assign bots
    IObjectTimerTrigger setupTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    setupTimer.SetIntervalTime(1000); // 1 second delay
    setupTimer.SetRepeatCount(1);
    setupTimer.SetScriptMethod("SetupBots");
    setupTimer.Trigger();
    
    // Set up events
    Events.PlayerDamageCallback.Start(OnPlayerDamage);
    Events.PlayerDeathCallback.Start(OnPlayerDeath);
    Events.UpdateCallback.Start(OnUpdate, 100); // Check every 100ms
}


public void SetupBots(TriggerArgs args)
{
    IPlayer[] players = Game.GetPlayers();
    
    // Ensure we have at least 6 players for the bots
    if (players.Length < 6) return;
    
    // Assign bot identities (players 3-8, indices 2-7)
    timpaBot = players[2];
    bichiBot = players[3];
    kokolaBot = players[4];
    edurBot = players[5];
    xrayBot = players[6];
    pakhiBot = players[7];
    
    // Randomly assign teams only on first setup
    if (!teamsAssigned)
    {
        // Create array of available teams
        PlayerTeam[] availableTeams = { PlayerTeam.Team1, PlayerTeam.Team1, PlayerTeam.Team1, 
                                       PlayerTeam.Team2, PlayerTeam.Team2, PlayerTeam.Team2 };
        
        // Shuffle the teams randomly
        for (int i = 0; i < 6; i++)
        {
            int randomIndex = random.Next(0, 6);
            PlayerTeam temp = availableTeams[i];
            availableTeams[i] = availableTeams[randomIndex];
            availableTeams[randomIndex] = temp;
        }
        
        // Assign teams to bots
        botTeams[0] = availableTeams[0]; // Timpa
        botTeams[1] = availableTeams[1]; // Bichi
        botTeams[2] = availableTeams[2]; // Kokola
        botTeams[3] = availableTeams[3]; // Edur
        botTeams[4] = availableTeams[4]; // Xray
        botTeams[5] = availableTeams[5]; // Pakhi
        
        teamsAssigned = true;
    }

    // Set names
    timpaBot.SetBotName("Timpa");
    bichiBot.SetBotName("Bichi");
    kokolaBot.SetBotName("Kokola");
    edurBot.SetBotName("Edur");
    xrayBot.SetBotName("Xray");
    pakhiBot.SetBotName("Pakhi");
    
    // Apply team assignments
    timpaBot.SetTeam(botTeams[0]);
    bichiBot.SetTeam(botTeams[1]);
    kokolaBot.SetTeam(botTeams[2]);
    edurBot.SetTeam(botTeams[3]);
    xrayBot.SetTeam(botTeams[4]);
    pakhiBot.SetTeam(botTeams[5]);

    // Set profiles


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
                timpaBot.SetStrengthBoostTime(15000); // 15 seconds                
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
    HandleXrayLaser();
    
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
        timpaBot.SetStrengthBoostTime(15000); // 15 seconds
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
        
        // Spawn a random bot
        IPlayer spawnedBot = Game.CreatePlayer(kokolaPosition);
        spawnedBot.SetNametagVisible(false);
        spawnedBot.SetStatusBarsVisible(false);
        spawnedBot.SetWorldPosition(kokolaPosition);
        
        // Random bot profile
        IProfile[] botProfiles = { 
            new IProfile()
            {
                Name = "BOT",
                Gender = Gender.Male,
                Skin = new IProfileClothingItem("Normal", "Skin3", "ClothingLightGreen"),
                ChestUnder = new IProfileClothingItem("SleevelessShirt", "ClothingCyan"),
                Legs = new IProfileClothingItem("PantsBlack", "ClothingBlue"),
                Feet = new IProfileClothingItem("ShoesBlack", "ClothingBrown"),
            }
        };
        IProfile randomProfile = botProfiles[random.Next(0, botProfiles.Length)];
        spawnedBot.SetProfile(randomProfile);

        // Give random weapon
        WeaponItem[] weapons = { WeaponItem.BAT, WeaponItem.BOTTLE, WeaponItem.PIPE, WeaponItem.CHAIR };
        WeaponItem randomWeapon = weapons[random.Next(0, weapons.Length)];
        spawnedBot.GiveWeaponItem(randomWeapon);
        
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

private void HandleXrayLaser()
{
    if (xrayBot == null || xrayBot.IsDead) return;
    xrayBot.GiveWeaponItem(WeaponItem.LAZER);
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