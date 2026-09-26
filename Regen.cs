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

public void OnStartup()
{
    // Set up combat detection events for regeneration tracking
    Events.PlayerDamageCallback.Start(OnPlayerDamage);
    Events.PlayerMeleeActionCallback.Start(OnPlayerMeleeAction);
    
    // Set up update event for HP regeneration
    Events.UpdateCallback.Start(OnUpdate, 500); // Check every 100ms for smooth regeneration
}

public void OnPlayerDamage(IPlayer player, PlayerDamageArgs args)
{
    // Track when player takes damage to prevent regeneration
    UpdateCombatTime(player);
}

public void OnPlayerMeleeAction(IPlayer player, PlayerMeleeHitArg[] args)
{
    // Track when player performs melee action (hitting) to prevent regeneration
    UpdateCombatTime(player);
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
        RegenerateHealth(bot, mods, 0.03f);
    }
}

private void RegenerateHealth(IPlayer player, PlayerModifiers mods, float regenPercentage)
{
    if (mods.CurrentHealth < mods.MaxHealth)
    {
        // Regenerate health based on percentage of max health
        int regenAmount = (int)(mods.MaxHealth * regenPercentage);
        // if (regenAmount < 1) regenAmount = 1; // Ensure at least 1 HP regen

        mods.CurrentHealth = Math.Min(mods.MaxHealth, mods.CurrentHealth + regenAmount);
        player.SetModifiers(mods);
    }
}
