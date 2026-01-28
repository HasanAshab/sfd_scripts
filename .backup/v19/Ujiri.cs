// Bot special ability tracking
private IPlayer timpaBot = null;
private IPlayer bichiBot = null;
private IPlayer kokolaBot = null;
private IPlayer edurBot = null;
private IPlayer xrayBot = null;
private IPlayer pakhiBot = null;
private IPlayer psythicBot = null; // Kid boss - child of Xray

// Psythic hunger system
private bool psythicIsHungry = false; // Start full, not hungry
private float psythicLastFeedTime = -1;
private int psythicOriginalMaxHealth = 100; // Store original health for reverting

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
private float psythicLastTargetUpdateTime = -1;
private const float BICHI_SPAWN_INTERVAL = 13000; // 13 seconds
private const float KOKOLA_SPAWN_INTERVAL = 7000; // 7 seconds
private const float PAKHI_REGEN_INTERVAL = 500; // 0.5 second
private const float XRAY_LASER_INTERVAL = 10000; // 10 seconds
private const float PSYTHIC_TARGET_UPDATE_INTERVAL = 1000; // 1 second - update target frequently
private const float PSYTHIC_HUNGER_DURATION = 10000; // 10 seconds - how long Psythic stays full

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
    Events.PlayerMeleeActionCallback.Start(OnPlayerMeleeAction);
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
    
    Vector2[] spawnPositions = new Vector2[7]; // 7 bots now (added Psythic)
    
    if (pathNodes.Length >= 7)
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
        
        for (int i = 0; i < 7; i++)
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
                team1SpawnPos = p2.GetWorldPosition(); // Team1 bots spawn at P1
                team2SpawnPos = p1.GetWorldPosition(); // Team2 bots spawn at P2
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
        
        // Assign spawn positions: Team1 bots (Timpa, Bichi, Kokola) and Team2 bots (Edur, Xray, Pakhi, Psythic)
        spawnPositions[0] = team1SpawnPos; // Timpa
        spawnPositions[1] = team1SpawnPos; // Bichi
        spawnPositions[2] = team1SpawnPos; // Kokola
        spawnPositions[3] = team2SpawnPos; // Edur
        spawnPositions[4] = team2SpawnPos; // Xray
        spawnPositions[5] = team2SpawnPos; // Pakhi
        spawnPositions[6] = team2SpawnPos; // Psythic
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
        edurBot.SetBotBehavior(new BotBehavior(true, PredefinedAIType.CompanionA));
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
        xrayBot.SetBotBehavior(new BotBehavior(true, PredefinedAIType.CompanionA));
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
        pakhiBot.SetBotBehavior(new BotBehavior(true, PredefinedAIType.CompanionA));
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

    // Create Psythic - Kid Boss (child of Xray)
    psythicBot = Game.CreatePlayer(spawnPositions[6]);
    if (psythicBot != null)
    {
        psythicBot.SetTeam(PlayerTeam.Team2);
        psythicBot.SetBotName("Psythic");
        psythicBot.SetBotBehavior(new BotBehavior(true, PredefinedAIType.ZombieB));
        psythicBot.SetCameraSecondaryFocusMode(CameraFocusMode.Ignore);

        // Set full profile (male profile)
        SetPsythicFullProfile();

        // Set Psythic's enhanced speed (2x normal)
        PlayerModifiers psythicMods = psythicBot.GetModifiers();
        psythicMods.SizeModifier = 0.7f; // Smaller size (kid)
        psythicMods.RunSpeedModifier = 2.5f;
        psythicMods.SprintSpeedModifier = 4f;
        
        // Store original health and set 4x health for full mode
        psythicOriginalMaxHealth = psythicMods.MaxHealth;
        psythicMods.MaxHealth = psythicOriginalMaxHealth * 4; // 4x health when full
        psythicMods.CurrentHealth = psythicMods.MaxHealth; // Full health

        psythicBot.SetModifiers(psythicMods);

        // Start the hunger timer (will become hungry after 10 seconds)
        psythicLastFeedTime = Game.TotalElapsedGameTime;
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
        
        // Make Xray guard Psythic initially (since Psythic starts full)
        if (psythicBot != null)
        {
            xrayBot.SetGuardTarget(psythicBot);
        }
    }
    
    // Initialize timers
    float currentTime = Game.TotalElapsedGameTime;
    bichiLastSpawnTime = currentTime;
    kokolaLastSpawnTime = currentTime;
    pakhiLastRegenTime = currentTime;
    xrayLastLaserTime = currentTime;
    psythicLastTargetUpdateTime = currentTime;
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

public void OnPlayerMeleeAction(IPlayer player, PlayerMeleeHitArg[] args)
{
    // Handle Psythic's special melee abilities
    if (psythicBot != null && player.UniqueID == psythicBot.UniqueID && args.Length > 0)
    {
        foreach (PlayerMeleeHitArg hitArg in args)
        {
            if (hitArg.HitObject != null && hitArg.IsPlayer)
            {
                IPlayer hitPlayer = Game.GetPlayer(hitArg.HitObject.UniqueID);
                if (hitPlayer != null && !hitPlayer.IsDead && hitPlayer.GetTeam() != psythicBot.GetTeam())
                {
                    if (psythicIsHungry)
                    {
                        // HUNGRY MODE: Can gib and stun
                        // Store victim's health before gibbing
                        PlayerModifiers victimMods = hitPlayer.GetModifiers();
                        float victimHealth = victimMods.CurrentHealth;
                        
                        // 40% chance to gib enemy
                        int gibRoll = random.Next(0, 100);
                        if (gibRoll < 40)
                        {
                            hitPlayer.Gib();
                            
                            // Psythic feeds and gains HP
                            PlayerModifiers psythicMods = psythicBot.GetModifiers();
                            psythicMods.CurrentHealth = Math.Min(psythicMods.MaxHealth, psythicMods.CurrentHealth + victimHealth);
                            psythicBot.SetModifiers(psythicMods);
                            
                            // Psythic becomes full
                            SetPsythicFull();
                        }
                        else
                        {
                            // 85% chance to stun enemy
                            int stunRoll = random.Next(0, 100);
                            if (stunRoll < 85)
                            {
                                StunPlayer(hitPlayer);
                            }
                        }
                    }
                    else
                    {
                        // FULL MODE: Can attack and stun but cannot gib
                        // 85% chance to stun enemy (same as hungry mode but no gib)
                        int stunRoll = random.Next(0, 100);
                        if (stunRoll < 85)
                        {
                            StunPlayer(hitPlayer);
                        }
                    }
                }
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
    
    // Handle Psythic's target selection (lowest HP enemy)
    // HandlePsythicTargeting(currentTime);
    
    // Handle Psythic's hunger system
    HandlePsythicHunger(currentTime);
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

private void HandlePsythicTargeting(float currentTime)
{
    if (psythicBot == null || psythicBot.IsDead) return;
    
    // Update target every second
    if (currentTime - psythicLastTargetUpdateTime >= PSYTHIC_TARGET_UPDATE_INTERVAL)
    {
        // Find enemy player with lowest HP
        IPlayer[] allPlayers = Game.GetPlayers();
        IPlayer lowestHpEnemy = null;
        float lowestHp = int.MaxValue;
        
        foreach (IPlayer player in allPlayers)
        {
            if (!player.IsDead && player.GetTeam() != psythicBot.GetTeam())
            {
                PlayerModifiers mods = player.GetModifiers();
                if (mods.CurrentHealth < lowestHp)
                {
                    lowestHp = mods.CurrentHealth;
                    lowestHpEnemy = player;
                }
            }
        }
        
        // Set target to lowest HP enemy
        if (lowestHpEnemy != null)
        {
            psythicBot.SetForcedBotTarget(lowestHpEnemy);
        }
        
        psythicLastTargetUpdateTime = currentTime;
    }
}

private void HandlePsythicHunger(float currentTime)
{
    if (psythicBot == null || psythicBot.IsDead) return;
    
    // Check if Psythic should become hungry again (after 10 seconds of being full)
    if (!psythicIsHungry && psythicLastFeedTime >= 0 && currentTime - psythicLastFeedTime >= PSYTHIC_HUNGER_DURATION)
    {
        SetPsythicHungry();
        // Debug message to confirm hunger transition
        // Game.ShowPopupMessage("Psythic is now HUNGRY!", Color.Red);
    }
}

private void SetPsythicFull()
{
    if (psythicBot == null || psythicBot.IsDead) return;
    
    psythicIsHungry = false;
    psythicLastFeedTime = Game.TotalElapsedGameTime;
    
    // Debug message to confirm full transition
    // Game.ShowPopupMessage("Psythic is now FULL!", Color.Green);
    
    // Change to full profile (male)
    SetPsythicFullProfile();
    
    // Set 4x health when full
    PlayerModifiers psythicMods = psythicBot.GetModifiers();
    float healthPercentage = (float)psythicMods.CurrentHealth / psythicMods.MaxHealth; // Preserve health percentage
    // psythicMods.MaxHealth = psythicOriginalMaxHealth * 3; // 3x max health
    // psythicMods.CurrentHealth = (int)(psythicMods.MaxHealth * healthPercentage); // Scale current health
    psythicBot.SetModifiers(psythicMods);
    
    // Make Psythic guard Xray when full
    if (xrayBot != null && !xrayBot.IsDead)
    {
        psythicBot.SetGuardTarget(xrayBot);
    }
}

private void SetPsythicHungry()
{
    if (psythicBot == null || psythicBot.IsDead) return;
    
    psythicIsHungry = true;
    
    // Set hungry profile (current profile - female)
    SetPsythicHungryProfile();
    
    // Revert to normal health when hungry
    PlayerModifiers psythicMods = psythicBot.GetModifiers();
    float healthPercentage = (float)psythicMods.CurrentHealth / psythicMods.MaxHealth; // Preserve health percentage
    psythicMods.MaxHealth = psythicOriginalMaxHealth; // Normal max health
    psythicMods.CurrentHealth = (int)(psythicMods.MaxHealth * healthPercentage); // Scale current health down
    psythicBot.SetModifiers(psythicMods);
    
    // Remove guard target when hungry (become independent)
    psythicBot.SetGuardTarget(null);
}

private void SetPsythicFullProfile()
{
    if (psythicBot == null || psythicBot.IsDead) return;
    
    psythicBot.SetProfile(new IProfile()
    {
        Name = "Psythic_full",
        Gender = Gender.Male,
        Skin = new IProfileClothingItem("Normal", "Skin1", "ClothingBrown"),
        Head = new IProfileClothingItem("AviatorHat2", "ClothingDarkGray", "ClothingLightBrown"),
    });
}

private void SetPsythicHungryProfile()
{
    if (psythicBot == null || psythicBot.IsDead) return;
    
    psythicBot.SetProfile(new IProfile()
    {
        Name = "Psythic",
        Gender = Gender.Female,
        Skin = new IProfileClothingItem("Normal_fem", "Skin1", "ClothingBrown"),
        Head = new IProfileClothingItem("AviatorHat2_fem", "ClothingDarkGray", "ClothingLightBrown")
    });
}

private void StunPlayer(IPlayer player)
{
    // Disable player input for stun duration
    player.SetInputEnabled(false);
    player.AddCommand(new PlayerCommand(PlayerCommandType.Fall));

    // Create a timer to restore movement after stun duration
    IObjectTimerTrigger stunTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    stunTimer.SetIntervalTime(500);
    stunTimer.SetRepeatCount(1);
    stunTimer.SetScriptMethod("RestorePlayerMovement");
    stunTimer.Trigger();
}

public void RestorePlayerMovement(TriggerArgs args)
{
    // Restore movement for all players (since we can't target specific players in timer callbacks)
    IPlayer[] allPlayers = Game.GetPlayers();
    foreach (IPlayer player in allPlayers)
    {
        if (!player.IsDead)
        {
            player.SetInputEnabled(true);
        }
    }
}