// Track when players reach max energy for regeneration delay
private float p1MaxEnergyTime = -1;
private float p2MaxEnergyTime = -1;
private const float REGEN_DELAY = 1500; // 1.5 seconds in milliseconds

// Track combat activity to prevent regeneration
private float p1LastCombatTime = -1;
private float p2LastCombatTime = -1;
private const float COMBAT_COOLDOWN = 1500; // 1.5 seconds after combat before regen can start

// Bot regeneration tracking (p3 to p8)
private float[] botMaxEnergyTimes = new float[6] { -1, -1, -1, -1, -1, -1 }; // For p3-p8
private float[] botLastCombatTimes = new float[6] { -1, -1, -1, -1, -1, -1 }; // For p3-p8
private const float BOT_REGEN_DELAY = 500; // 0.5 seconds for bots
private const float BOT_COMBAT_COOLDOWN = 500; // 0.5 seconds after combat for bots

// P1 shock ability tracking
private int p1HitCounter = 0;
private const int SHOCK_HIT_THRESHOLD = 3; // Shock on 3rd hit

// P1 jump attack ability tracking
private float p1VulnerableUntil = -1;
private const float VULNERABILITY_DURATION = 3000; // 3 seconds vulnerable after jump attack
private const float JUMP_SHOCK_RANGE = 100; // Range for jump shock effect

public void OnStartup()
{
    IPlayer p1 = Game.GetPlayers()[0];

    PlayerModifiers p1Mods = p1.GetModifiers();

    p1Mods.RunSpeedModifier = 1.1f;
    p1Mods.SprintSpeedModifier = 1.25f;

    p1Mods.ProjectileDamageTakenModifier *= 0.4f;
    p1Mods.ExplosionDamageTakenModifier *= 0.3f;
    p1Mods.FireDamageTakenModifier *= 0.3f;

    p1Mods.ProjectileCritChanceDealtModifier *= 3f;

    p1Mods.MaxEnergy = (int)(p1Mods.MaxEnergy * 1.2f);
    p1Mods.CurrentEnergy = (int)(p1Mods.CurrentEnergy * 1.2f);
    p1Mods.EnergyRechargeModifier *= 1.5f;
    
    p1.SetModifiers(p1Mods);
    
    // Give p1 initial fire ammo
    p1.GiveWeaponItem(WeaponItem.FIREAMMO);
    
    
    
    IPlayer p2 = Game.GetPlayers()[1];

    PlayerModifiers p2Mods = p2.GetModifiers();

    p2Mods.SizeModifier = 2;
    p2Mods.RunSpeedModifier = 0.8f;
    p2Mods.SprintSpeedModifier = 0.8f;

    p2Mods.MaxEnergy = (int)(p1Mods.MaxEnergy * 1.3f);
    p2Mods.CurrentEnergy = (int)(p1Mods.CurrentEnergy * 1.3f);

    p2Mods.MaxHealth = (int)(p2Mods.MaxHealth * 1.4f);
    p2Mods.CurrentHealth = (int)(p2Mods.CurrentHealth * 1.4f);

    p2Mods.MeleeDamageTakenModifier *= 0.4f;
    p2Mods.ProjectileDamageTakenModifier *= 0.3f;

    p2Mods.MeleeForceModifier *= 3.2f;
    p2Mods.MeleeDamageDealtModifier *= 3.2f;

    p2.SetModifiers(p2Mods);
    
    // Set up combat detection events
    Events.PlayerDamageCallback.Start(OnPlayerDamage);
    Events.PlayerMeleeActionCallback.Start(OnPlayerMeleeAction);
    Events.PlayerKeyInputCallback.Start(OnPlayerKeyInput);
    
    // Set up fire ammo timer for p1 (every 30 seconds)
    IObjectTimerTrigger fireAmmoTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    fireAmmoTimer.SetIntervalTime(30000); // 30 seconds
    fireAmmoTimer.SetRepeatCount(0); // Infinite repeats
    fireAmmoTimer.SetScriptMethod("GiveP1FireAmmo");
    fireAmmoTimer.Trigger();
    
    // Set up update event for HP regeneration
    Events.UpdateCallback.Start(OnUpdate, 1000); // Check every second 
}

public void GiveP1FireAmmo(TriggerArgs args)
{
    // Give p1 fire ammo every 30 seconds
    IPlayer[] players = Game.GetPlayers();
    if (players.Length >= 1 && !players[0].IsDead)
    {
        players[0].GiveWeaponItem(WeaponItem.FIREAMMO);
    }
}

public void OnPlayerKeyInput(IPlayer player, VirtualKeyInfo[] keyInfos)
{
    // Check if p1 pressed ATTACK while in mid-air
    IPlayer[] players = Game.GetPlayers();
    if (players.Length >= 1 && player.UniqueID == players[0].UniqueID)
    {
        foreach (VirtualKeyInfo keyInfo in keyInfos)
        {
            if (keyInfo.Event == VirtualKeyEvent.Pressed && keyInfo.Key == VirtualKey.ATTACK)
            {
                // Check if p1 is in mid-air (not on ground) and has at least half energy
                if (!player.IsOnGround)
                {
                    PlayerModifiers p1Mods = player.GetModifiers();
                    if (p1Mods.CurrentEnergy >= p1Mods.MaxEnergy / 2)
                    {
                        PerformJumpShockAttack(player);
                    }
                }
            }
        }
    }
}

private void PerformJumpShockAttack(IPlayer p1)
{
    Vector2 p1Position = p1.GetWorldPosition();
    float currentTime = Game.TotalElapsedGameTime;
    PlayerTeam p1Team = p1.GetTeam();
    
    // Find all players within range
    IPlayer[] allPlayers = Game.GetPlayers();
    foreach (IPlayer target in allPlayers)
    {
        if (target.UniqueID != p1.UniqueID && !target.IsDead)
        {
            Vector2 targetPosition = target.GetWorldPosition();
            float distance = Vector2.Distance(p1Position, targetPosition);
            
            if (distance <= JUMP_SHOCK_RANGE)
            {
                // Skip teammates - only affect enemies
                if (target.GetTeam() != p1Team)
                {
                    // Apply shock effect to target
                    PlayerModifiers targetMods = target.GetModifiers();
                    targetMods.CurrentHealth = Math.Max(1, targetMods.CurrentHealth - 50); // Jump shock damage
                    target.SetModifiers(targetMods);
                    
                    // Disarm the target - drop their current weapon
                    DisarmPlayer(target);
                    
                    // Stun the target for 500ms
                    StunPlayer(target, 500);
                    
                    // Create visual effect at target location
                    Game.PlayEffect(EffectName.Electric, targetPosition);
                }
            }
        }
    }
    
    // Create main shock effect at p1's location
    Game.PlayEffect(EffectName.Electric, p1Position);
    
    // Make p1 vulnerable for 3 seconds and drain energy
    p1VulnerableUntil = currentTime + VULNERABILITY_DURATION;
    
    // Drain p1's energy to 0 during vulnerability
    PlayerModifiers p1Mods = p1.GetModifiers();
    p1Mods.CurrentEnergy = 0;
    p1.SetModifiers(p1Mods);
}

public void OnPlayerDamage(IPlayer player, PlayerDamageArgs args)
{
    // Track when player takes damage
    UpdateCombatTime(player);
    
    // Check if p1 is taking damage while vulnerable (increase damage)
    IPlayer[] players = Game.GetPlayers();
    if (players.Length >= 1 && player.UniqueID == players[0].UniqueID)
    {
        float currentTime = Game.TotalElapsedGameTime;
        if (p1VulnerableUntil > 0 && currentTime < p1VulnerableUntil)
        {
            // P1 is vulnerable - take extra damage
            PlayerModifiers p1Mods = player.GetModifiers();
            p1Mods.CurrentHealth = Math.Max(1, p1Mods.CurrentHealth - 10); // Extra vulnerability damage
            player.SetModifiers(p1Mods);
        }
    }
}

public void OnPlayerMeleeAction(IPlayer player, PlayerMeleeHitArg[] args)
{
    // Track when player performs melee action (hitting)
    UpdateCombatTime(player);
    
    // Check if p1 is performing melee action
    IPlayer[] players = Game.GetPlayers();
    if (players.Length >= 1 && player.UniqueID == players[0].UniqueID && args.Length > 0)
    {
        // Increment p1's hit counter for each successful hit
        p1HitCounter++;
        
        // Check if this is the 3rd hit (shock hit)
        if (p1HitCounter >= SHOCK_HIT_THRESHOLD)
        {
            // P1's shock ability - stun all hit targets
            foreach (PlayerMeleeHitArg hitArg in args)
            {
                if (hitArg.HitObject != null && hitArg.IsPlayer)
                {
                    IPlayer hitPlayer = Game.GetPlayer(hitArg.HitObject.UniqueID);
                    if (hitPlayer != null && !hitPlayer.IsDead)
                    {
                        // Apply shock effect (stun + extra damage)
                        PlayerModifiers hitMods = hitPlayer.GetModifiers();
                        hitMods.CurrentHealth = Math.Max(1, hitMods.CurrentHealth - 15); // Shock damage
                        hitPlayer.SetModifiers(hitMods);
                        
                        // Create visual effect at hit location
                        Game.PlayEffect(EffectName.Electric, hitArg.HitObject.GetWorldPosition());
                    }
                }
            }
            
            // Reset hit counter after shock
            p1HitCounter = 0;
        }
    }
}

private void UpdateCombatTime(IPlayer player)
{
    IPlayer[] players = Game.GetPlayers();
    if (players.Length >= 2)
    {
        float currentTime = Game.TotalElapsedGameTime;
        
        if (player.UniqueID == players[0].UniqueID)
        {
            p1LastCombatTime = currentTime;
            p1MaxEnergyTime = -1; // Reset regen timer
            // Don't reset hit counter here - only reset when out of combat
        }
        else if (player.UniqueID == players[1].UniqueID)
        {
            p2LastCombatTime = currentTime;
            p2MaxEnergyTime = -1; // Reset regen timer
        }
        else
        {
            // Handle bots (p3 to p8)
            for (int i = 2; i < players.Length && i < 8; i++)
            {
                if (player.UniqueID == players[i].UniqueID)
                {
                    int botIndex = i - 2; // Convert to bot array index (0-5)
                    botLastCombatTimes[botIndex] = currentTime;
                    botMaxEnergyTimes[botIndex] = -1; // Reset regen timer
                    break;
                }
            }
        }
    }
}

public void OnUpdate(float elapsed)
{
    IPlayer[] players = Game.GetPlayers();
    
    // Check if we have at least 2 players
    if (players.Length >= 2)
    {
        // Process main players (p1 and p2) with normal timers and energy requirement
        ProcessPlayerRegeneration(players[0], ref p1MaxEnergyTime, p1LastCombatTime, REGEN_DELAY, COMBAT_COOLDOWN);
        ProcessPlayerRegeneration(players[1], ref p2MaxEnergyTime, p2LastCombatTime, REGEN_DELAY, COMBAT_COOLDOWN);
        
        // Process bots (p3 to p8) with faster timers and no energy requirement
        for (int i = 2; i < players.Length && i < 8; i++)
        {
            int botIndex = i - 2; // Convert to bot array index (0-5)
            ProcessBotRegeneration(players[i], ref botMaxEnergyTimes[botIndex], botLastCombatTimes[botIndex]);
        }
    }
}

private void ProcessPlayerRegeneration(IPlayer player, ref float maxEnergyTime, float lastCombatTime, float regenDelay, float combatCooldown)
{
    if (player.IsDead) return;
    
    PlayerModifiers mods = player.GetModifiers();
    float currentTime = Game.TotalElapsedGameTime;
    
    // Check if player is in combat cooldown
    if (lastCombatTime >= 0 && currentTime - lastCombatTime < combatCooldown)
    {
        maxEnergyTime = -1; // Reset regen timer during combat cooldown
        return;
    }
    else
    {
        // If this is p1 and they're out of combat, reset hit counter
        IPlayer[] players = Game.GetPlayers();
        if (players.Length >= 1 && player.UniqueID == players[0].UniqueID)
        {
            p1HitCounter = 0; // Reset hit counter when out of combat
        }
    }
    
    // Check if player has max energy
    if (mods.CurrentEnergy >= mods.MaxEnergy)
    {
        // Start tracking max energy time if not already tracking
        if (maxEnergyTime < 0)
        {
            maxEnergyTime = currentTime;
        }
        
        // Check if enough time has passed since reaching max energy
        if (currentTime - maxEnergyTime >= regenDelay)
        {
            RegenerateHealth(player, mods, 0.05f);
        }
    }
    else
    {
        // Reset timer if energy is not at max
        maxEnergyTime = -1;
    }
}

private void ProcessBotRegeneration(IPlayer bot, ref float regenTimer, float lastCombatTime)
{
    if (bot.IsDead) return;
    PlayerModifiers mods = bot.GetModifiers();
    float currentTime = Game.TotalElapsedGameTime;
    
    // Check if bot is in combat cooldown
    if (lastCombatTime >= 0 && currentTime - lastCombatTime < BOT_COMBAT_COOLDOWN)
    {
        regenTimer = -1; // Reset regen timer during combat cooldown
        return;
    }
    
    // Bots don't need max energy - just start regeneration timer after combat cooldown
    if (regenTimer < 0)
    {
        regenTimer = currentTime; // Start regen timer immediately after combat cooldown
    }
    
    // Check if enough time has passed for regeneration
    if (currentTime - regenTimer >= BOT_REGEN_DELAY)
    {
        RegenerateHealth(bot, mods, 0.15f);
    }
}

private void RegenerateHealth(IPlayer player, PlayerModifiers mods, float regenPercentage)
{
    if (mods.CurrentHealth < mods.MaxHealth)
    {
        // Regenerate 5% of max health
        int regenAmount = (int)(mods.MaxHealth * regenPercentage);
        if (regenAmount < 1) regenAmount = 1; // Ensure at least 1 HP regen
        
        mods.CurrentHealth = Math.Min(mods.MaxHealth, mods.CurrentHealth + regenAmount);
        player.SetModifiers(mods);
    }
}

private void DisarmPlayer(IPlayer player)
{
    // Drop the player's current weapon by removing it from their inventory
    // This will cause the weapon to drop to the ground
    WeaponItemType currentWeaponType = player.CurrentWeaponDrawn;
    if (currentWeaponType != WeaponItemType.NONE)
    {
        // Use the weapon type directly - no conversion needed
        player.RemoveWeaponItemType(currentWeaponType);
    }
}

private void StunPlayer(IPlayer player, int durationMs)
{
    // Create a timer to stun the player for the specified duration
    // We'll use player modifiers to severely reduce movement speed
    PlayerModifiers stunMods = player.GetModifiers();
    
    // Store original speed values (we'll assume normal values)
    float originalRunSpeed = stunMods.RunSpeedModifier;
    float originalSprintSpeed = stunMods.SprintSpeedModifier;
    
    // Set movement to nearly zero
    stunMods.RunSpeedModifier = 0.01f;
    stunMods.SprintSpeedModifier = 0.01f;
    player.SetModifiers(stunMods);
    
    // Create a timer to restore movement after stun duration
    IObjectTimerTrigger stunTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    stunTimer.SetIntervalTime(durationMs);
    stunTimer.SetRepeatCount(1);
    stunTimer.SetScriptMethod("RestorePlayerMovement");
    
    // Store player ID for the timer (we'll need a way to identify which player to restore)
    // Since we can't pass parameters to timer methods, we'll restore all players
    // This is a limitation, but the stun duration is short (500ms)
    stunTimer.Trigger();
}

public void RestorePlayerMovement(TriggerArgs args)
{
    // Restore movement for all players (since we can't target specific players in timer callbacks)
    // This is called after stun duration expires
    IPlayer[] allPlayers = Game.GetPlayers();
    foreach (IPlayer player in allPlayers)
    {
        if (!player.IsDead)
        {
            PlayerModifiers mods = player.GetModifiers();
            
            // Only restore if movement is severely reduced (indicating stun)
            if (mods.RunSpeedModifier <= 0.02f)
            {
                // Restore to normal movement speeds based on player type
                if (player.UniqueID == Game.GetPlayers()[0].UniqueID) // P1
                {
                    mods.RunSpeedModifier = 1.1f;
                    mods.SprintSpeedModifier = 1.25f;
                }
                else if (Game.GetPlayers().Length >= 2 && player.UniqueID == Game.GetPlayers()[1].UniqueID) // P2
                {
                    mods.RunSpeedModifier = 0.8f;
                    mods.SprintSpeedModifier = 0.8f;
                }
                else // Other players (normal speed)
                {
                    mods.RunSpeedModifier = 1.0f;
                    mods.SprintSpeedModifier = 1.0f;
                }
                
                player.SetModifiers(mods);
            }
        }
    }
}
