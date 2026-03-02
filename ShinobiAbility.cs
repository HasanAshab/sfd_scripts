// ShinobiAbility - Special abilities for shinobi players
// Provides Uchiha ability that grants SLOWMO_5 every 20 seconds
// Susano transformation at 20% HP and breaks at 10% HP
// Provides Senju ability with 3x max energy and health regeneration

private IPlayer uchihaPlayer = null;
private bool susanoActive = false;
private bool susanoUsed = false; // Track if Susano has been used once
private int originalMaxHealth = 100;
private float originalSizeModifier = 1f;
private float originalMeleeForceModifier = 1f;
private IProfile originalProfile = null;
private IPlayer senjuPlayer = null;
private IPlayer woodenGolem = null;
private int senjuBlockCount = 0;
private bool isGolemActive = false;



public void OnStartup()
{
    IPlayer[] players = Game.GetPlayers();
    GiveSenjuAbility(players[0]);
    GiveUchihaAbility(players[1]);
}



public void GiveUchihaAbility(IPlayer player)
{
    // Store the player reference
    uchihaPlayer = player;
    
    // Store original stats
    PlayerModifiers originalMods = player.GetModifiers();
    originalMaxHealth = originalMods.MaxHealth;
    originalSizeModifier = originalMods.SizeModifier;
    originalMeleeForceModifier = originalMods.MeleeForceModifier;
    originalProfile = player.GetProfile();
    
    // Give initial SLOWMO_5
    if (uchihaPlayer != null && !uchihaPlayer.IsDead)
    {   
        uchihaPlayer.RemoveWeaponItemType(WeaponItemType.Powerup);
        uchihaPlayer.GiveWeaponItem(WeaponItem.SLOWMO_5);
    }
    
    // Set up timer to give SLOWMO_5 every 20 seconds
    IObjectTimerTrigger uchihaAbilityTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    uchihaAbilityTimer.SetIntervalTime(20000); // 20 seconds
    uchihaAbilityTimer.SetRepeatCount(0); // Infinite repeats
    uchihaAbilityTimer.SetScriptMethod("GiveUchihaSlowmo");
    uchihaAbilityTimer.Trigger();
    
    // Set up health monitoring timer for Susano transformation
    IObjectTimerTrigger healthMonitorTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    healthMonitorTimer.SetIntervalTime(100); // Check every 100ms
    healthMonitorTimer.SetRepeatCount(0); // Infinite repeats
    healthMonitorTimer.SetScriptMethod("MonitorUchihaHealth");
    healthMonitorTimer.Trigger();
    
    // Set up eye contact burning ability timer
    IObjectTimerTrigger eyeContactTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    eyeContactTimer.SetIntervalTime(500); // Check every 500ms
    eyeContactTimer.SetRepeatCount(0); // Infinite repeats
    eyeContactTimer.SetScriptMethod("CheckUchihaEyeContact");
    eyeContactTimer.Trigger();
}

public void GiveUchihaSlowmo(TriggerArgs args)
{
    // Give SLOWMO_5 to the Uchiha player every 20 seconds
    // But not when Susano is active
    if (uchihaPlayer != null && !uchihaPlayer.IsDead && !susanoActive)
    {
        uchihaPlayer.RemoveWeaponItemType(WeaponItemType.Powerup);
        uchihaPlayer.GiveWeaponItem(WeaponItem.SLOWMO_5);
    }
}

public void MonitorUchihaHealth(TriggerArgs args)
{
    if (uchihaPlayer == null || uchihaPlayer.IsDead) return;
    
    PlayerModifiers mods = uchihaPlayer.GetModifiers();
    float currentHealth = mods.CurrentHealth;
    float maxHealth = mods.MaxHealth;
    float healthPercentage = (currentHealth / maxHealth) * 100f;
    
    // Activate Susano when health drops to 20% (first time only)
    if (!susanoActive && !susanoUsed && healthPercentage <= 20f)
    {
        ActivateSusano();
    }
    
    // Break Susano when health drops to 10% while Susano is active
    if (susanoActive && healthPercentage <= 10f)
    {
        BreakSusano();
    }
}

private void ActivateSusano()
{
    if (uchihaPlayer == null || uchihaPlayer.IsDead) return;
    
    susanoActive = true;
    susanoUsed = true; // Mark that Susano has been used
    
    // Get current modifiers
    PlayerModifiers mods = uchihaPlayer.GetModifiers();
    
    // Transform to Susano form
    mods.SizeModifier = 2f; // 2x size
    mods.MaxHealth = (int)(originalMaxHealth * 2f); // 3x health
    mods.CurrentHealth = (int)(originalMaxHealth * 2f); // Full heal to 3x health
    mods.MeleeForceModifier = (int)(originalMeleeForceModifier * 3f);
    mods.MeleeStunImmunity = 1;
    mods.CanBurn = 0;

    // Apply modifiers
    uchihaPlayer.SetModifiers(mods);
    
    uchihaPlayer.RemoveWeaponItemType(WeaponItemType.Powerup);
    // Give Katana
    uchihaPlayer.GiveWeaponItem(WeaponItem.KATANA);

    uchihaPlayer.SetProfile(
        new IProfile()
        {
            Name = "Assassin",
            Gender = Gender.Male,
            Skin = new IProfileClothingItem("Normal", "Skin5", "ClothingLightBlue"),
            Head = new IProfileClothingItem("SpikedHelmet", "ClothingLightBlue"),
            ChestOver = new IProfileClothingItem("Poncho", "ClothingLightBlue", "ClothingLightBlue"),
            ChestUnder = new IProfileClothingItem("LumberjackShirt", "ClothingLightBlue", "ClothingLightGray"),
            Hands = new IProfileClothingItem("SafetyGloves", "ClothingLightBlue"),
            Legs = new IProfileClothingItem("PantsBlack", "ClothingBlue"),
            Feet = new IProfileClothingItem("Boots", "ClothingLightBlue"),
            Accesory = new IProfileClothingItem("ClownMakeup", "ClothingLightBlue"),
        }
    );

    uchihaPlayer.SetStrengthBoostTime(9999999);
    
    // Show transformation message
    Game.ShowChatMessage("SUSANO ACTIVATED!", Color.Red);
}

private void BreakSusano()
{
    if (uchihaPlayer == null || uchihaPlayer.IsDead) return;

    susanoActive = false;
    
    // Get current modifiers
    PlayerModifiers mods = uchihaPlayer.GetModifiers();
    
    // Revert to original form
    mods.SizeModifier = originalSizeModifier; // Original size
    mods.MaxHealth = originalMaxHealth; // Original max health
    mods.CurrentHealth = originalMaxHealth * 0.2f; // Set to 20% of original health
    mods.MeleeForceModifier = originalMeleeForceModifier;
    mods.MeleeStunImmunity = 0;
    mods.CanBurn = 1;
    
    // Apply modifiers
    uchihaPlayer.SetModifiers(mods);
    
    // Remove strength boost
    uchihaPlayer.SetStrengthBoostTime(0);

    // Remove Katana
    uchihaPlayer.RemoveWeaponItemType(WeaponItemType.Melee);

    // Remove Susano profile
    uchihaPlayer.SetProfile(originalProfile);

    // Show break message
    Game.ShowChatMessage("SUSANO BROKEN!", Color.Blue);
}

public void CheckUchihaEyeContact(TriggerArgs args)
{
    if (uchihaPlayer == null || uchihaPlayer.IsDead) return;
    
    // Only work when Susano is not active
    if (susanoActive) return;
    
    // Get all players to check for targeting
    IPlayer[] allPlayers = Game.GetPlayers();
    int uchihaFacing = uchihaPlayer.FacingDirection;
    Vector2 uchihaPos = uchihaPlayer.GetWorldPosition();
    
    foreach (IPlayer player in allPlayers)
    {
        // Skip if same player, dead, or not a bot
        if (player.UniqueID == uchihaPlayer.UniqueID || player.IsDead || !player.IsBot) continue;
        
        // Skip if player is already burning
        if (player.IsBurning) continue;
        
        // Check if this player is targeting the Uchiha
        IObject target = player.GetBotTarget();
        if (target == null) continue;
        
        // Check if target is the Uchiha player
        IPlayer targetPlayer = target as IPlayer;
        if (targetPlayer == null || targetPlayer.UniqueID != uchihaPlayer.UniqueID) continue;
        
        // Check range - player must be within 5 tiles (160 pixels) of Uchiha
        Vector2 playerPos = player.GetWorldPosition();
        float distance = Vector2.Distance(uchihaPos, playerPos);
        if (distance > 120f) continue; // 5 tiles * 32 pixels per tile
        
        // Check if they are facing each other (different facing directions)
        int playerFacing = player.FacingDirection;
        if (playerFacing == uchihaFacing) continue; // Same direction = not facing each other
        
        // 50% chance to burn the player
        int randomChance = (int)(Game.TotalElapsedGameTime * 1000) + player.UniqueID;
        if ((randomChance % 100) < 50) // 50% chance
        {
            // Burn the player
            player.SetMaxFire();
            
            // Show eye contact message
            Game.ShowChatMessage("SHARINGAN! " + player.GetProfile().Name + " burned by eye contact!", Color.Red);
        }
    }
}


public void GiveSenjuAbility(IPlayer player)
{
    // Store the player reference
    senjuPlayer = player;

    // Get current modifiers
    PlayerModifiers mods = player.GetModifiers();

    // Set max energy to 3x (default is 100)
    mods.MaxEnergy = (int)(mods.MaxEnergy * 3f); // 3x max energy
    mods.CurrentEnergy = (int)(mods.CurrentEnergy * 3f); // Start with full energy
    mods.EnergyRechargeModifier = 2f; // 2x energy recharge rate

    // Apply modifiers
    player.SetModifiers(mods);

    // Set up timer to heal 2% of max HP every 2 seconds
    IObjectTimerTrigger senjuHealTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    senjuHealTimer.SetIntervalTime(2000); // 2 seconds
    senjuHealTimer.SetRepeatCount(0); // Infinite repeats
    senjuHealTimer.SetScriptMethod("HealSenjuPlayer");
    senjuHealTimer.Trigger();
    
    // Set up golem energy drain timer (every 1 second)
    IObjectTimerTrigger golemEnergyTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    golemEnergyTimer.SetIntervalTime(1000); // 1 second
    golemEnergyTimer.SetRepeatCount(0); // Infinite repeats
    golemEnergyTimer.SetScriptMethod("DrainGolemEnergy");
    golemEnergyTimer.Trigger();
    
    // Set up player key input callback for block detection
    Events.PlayerKeyInputCallback.Start(OnSenjuKeyInput);
    
    // Set up player death callback for golem cleanup
    Events.PlayerDeathCallback.Start(OnSenjuPlayerDeath);
    
    // Show ability granted message
    Game.ShowChatMessage("SENJU ABILITY GRANTED! 3x Energy + 2x Recharge + Regeneration + Golem Summon", Color.Green);
}

public void HealSenjuPlayer(TriggerArgs args)
{
    // Heal Senju player every 2 seconds
    if (senjuPlayer == null || senjuPlayer.IsDead) return;
    
    PlayerModifiers mods = senjuPlayer.GetModifiers();
    float maxHealth = mods.MaxHealth;
    float currentHealth = mods.CurrentHealth;
    
    // Heal 5% of max HP
    float healAmount = maxHealth * 0.05f;
    float newHealth = currentHealth + healAmount;
    
    // Don't exceed max health
    if (newHealth > maxHealth)
    {
        newHealth = maxHealth;
    }
    
    // Apply healing
    mods.CurrentHealth = newHealth;
    senjuPlayer.SetModifiers(mods);
}

public void OnSenjuKeyInput(IPlayer player, VirtualKeyInfo[] keyInfos)
{
    // Only track Senju player
    if (player == null || senjuPlayer == null || player.UniqueID != senjuPlayer.UniqueID) return;
    
    foreach (VirtualKeyInfo keyInfo in keyInfos)
    {
        // Check for block key press while sitting
        if (keyInfo.Event == VirtualKeyEvent.Pressed && keyInfo.Key == VirtualKey.BLOCK)
        {
            // Check if player is sitting (crouching)
            if (player.IsCrouching)
            {
                senjuBlockCount++;
                Game.ShowChatMessage("Senju Block Count: " + senjuBlockCount + "/2", Color.Green);
                
                // Summon golem after 2 blocks
                if (senjuBlockCount >= 2 && !isGolemActive)
                {
                    SummonWoodenGolem();
                    senjuBlockCount = 0; // Reset counter
                }
            }
        }
    }
}

private void SummonWoodenGolem()
{
    if (senjuPlayer == null || senjuPlayer.IsDead) return;
    
    // Check if player has enough energy (30 energy required)
    PlayerModifiers mods = senjuPlayer.GetModifiers();
    if (mods.CurrentEnergy < 0.3f) // 30% of max energy (which is 3.0, so 0.3 = 30 energy)
    {
        Game.ShowChatMessage("Not enough energy to summon golem! (30 energy required)", Color.Red);
        return;
    }
    
    // Deduct initial energy cost
    mods.CurrentEnergy -= 0.3f; // 30 energy
    senjuPlayer.SetModifiers(mods);
    
    // Create wooden golem at Senju's position
    Vector2 senjuPos = senjuPlayer.GetWorldPosition();
    woodenGolem = Game.CreatePlayer(senjuPos);
    
    if (woodenGolem != null)
    {
        isGolemActive = true;
        
        // Set golem properties
        woodenGolem.SetTeam(senjuPlayer.GetTeam());
        woodenGolem.SetBotName("WOOD GOLEM");
        
        // Set as bot with defensive behavior
        BotBehavior golemBehavior = new BotBehavior(true, PredefinedAIType.BotA);
        woodenGolem.SetBotBehavior(golemBehavior);
        
        // Set golem to guard the Senju player
        woodenGolem.SetGuardTarget(senjuPlayer);
        
        // Set golem properties
        woodenGolem.SetNametagVisible(true);
        woodenGolem.SetStatusBarsVisible(true);
        woodenGolem.SetCameraSecondaryFocusMode(CameraFocusMode.Ignore);
        
        // Give golem enhanced stats (tanky guardian)
        PlayerModifiers golemMods = new PlayerModifiers();
        golemMods.MaxHealth = 200; // High health
        golemMods.CurrentHealth = 200;
        golemMods.SizeModifier = 1.3f; // Larger size
        golemMods.RunSpeedModifier = 0.7f; // Slower movement
        golemMods.SprintSpeedModifier = 0.7f;
        golemMods.MeleeDamageDealtModifier = 1.5f; // Strong melee
        golemMods.ProjectileDamageTakenModifier = 0.6f; // Damage resistance
        golemMods.MeleeDamageTakenModifier = 0.6f;
        woodenGolem.SetModifiers(golemMods);
        
        // Give golem weapons
        woodenGolem.GiveWeaponItem(WeaponItem.HAMMER);
        woodenGolem.GiveWeaponItem(WeaponItem.GRENADES);
        
        // Set golem profile
        woodenGolem.SetProfile(GetWoodenGolemProfile());
        
        Game.ShowChatMessage("WOODEN GOLEM SUMMONED! (-30 energy, -5 energy/sec)", Color.Green);
    }
}

public void DrainGolemEnergy(TriggerArgs args)
{
    // Only drain energy if golem is active
    if (!isGolemActive || senjuPlayer == null || senjuPlayer.IsDead) return;
    
    PlayerModifiers mods = senjuPlayer.GetModifiers();
    
    // Drain 5 energy per second (5% of max energy which is 3.0, so 0.05)
    mods.CurrentEnergy -= 0.05f; // 5 energy per second
    
    // Check if out of energy
    if (mods.CurrentEnergy <= 0f)
    {
        mods.CurrentEnergy = 0f;
        senjuPlayer.SetModifiers(mods);
        
        // Destroy golem when out of energy
        DestroyWoodenGolem();
        Game.ShowChatMessage("Out of energy! Wooden Golem destroyed!", Color.Red);
    }
    else
    {
        senjuPlayer.SetModifiers(mods);
    }
}

private void DestroyWoodenGolem()
{
    if (woodenGolem != null && !woodenGolem.IsDead)
    {
        // Gib the golem
        woodenGolem.Gib();
    }
    
    woodenGolem = null;
    isGolemActive = false;
    senjuBlockCount = 0; // Reset block counter
}

private IProfile GetWoodenGolemProfile()
{
    return new IProfile()
    {
        Name = "Wood Golem",
        Gender = Gender.Male,
        Skin = new IProfileClothingItem("MechSkin", "ClothingBrown", "ClothingDarkBrown"),
        Head = new IProfileClothingItem("Helmet", "ClothingBrown"),
        ChestOver = new IProfileClothingItem("KevlarVest", "ClothingBrown"),
        ChestUnder = new IProfileClothingItem("MilitaryShirt", "ClothingBrown", "ClothingDarkBrown"),
        Hands = new IProfileClothingItem("Gloves", "ClothingBrown"),
        Waist = new IProfileClothingItem("SatchelBelt", "ClothingBrown"),
        Legs = new IProfileClothingItem("Pants", "ClothingBrown"),
        Feet = new IProfileClothingItem("BootsBlack", "ClothingBrown"),
    };
}

public void OnSenjuPlayerDeath(IPlayer player, PlayerDeathArgs args)
{
    // Clean up golem if Senju player dies
    if (player != null && senjuPlayer != null && player.UniqueID == senjuPlayer.UniqueID)
    {
        if (isGolemActive)
        {
            DestroyWoodenGolem();
            Game.ShowChatMessage("Senju died! Wooden Golem destroyed!", Color.Yellow);
        }
    }
}
