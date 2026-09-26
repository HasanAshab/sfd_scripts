// AoT-style regeneration - all players regenerate like bots
// Fast regeneration without energy requirement

// Track combat activity to prevent regeneration
private float p1LastCombatTime = -1;
private float p2LastCombatTime = -1;
private const float COMBAT_COOLDOWN = 500; // 0.5 seconds after combat before regen can start

// Regeneration tracking for all players
private float p1RegenTimer = -1;
private float p2RegenTimer = -1;
private const float REGEN_DELAY = 500; // 0.5 seconds delay before regeneration starts

// Bot regeneration tracking (p3 to p8)
private float[] botLastCombatTimes = new float[6] { -1, -1, -1, -1, -1, -1 }; // For p3-p8
private float[] botRegenTimers = new float[6] { -1, -1, -1, -1, -1, -1 }; // For p3-p8

public void OnStartup()
{
    // Set up combat detection events for regeneration tracking
    Events.PlayerDamageCallback.Start(OnPlayerDamage);
    Events.PlayerMeleeActionCallback.Start(OnPlayerMeleeAction);
    
    // Set up update event for HP regeneration
    Events.UpdateCallback.Start(OnUpdate, 500); // Check every 500ms for smooth regeneration
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
            p1RegenTimer = -1; // Reset regen timer
        }
        else if (player.UniqueID == players[1].UniqueID)
        {
            p2LastCombatTime = currentTime;
            p2RegenTimer = -1; // Reset regen timer
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
                    botRegenTimers[botIndex] = -1; // Reset regen timer
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
        // Process all players (p1 and p2) with bot-style regeneration
        ProcessPlayerRegeneration(players[0], ref p1RegenTimer, p1LastCombatTime);
        ProcessPlayerRegeneration(players[1], ref p2RegenTimer, p2LastCombatTime);
        
        // Process bots (p3 to p8) with same timers
        for (int i = 2; i < players.Length && i < 8; i++)
        {
            int botIndex = i - 2; // Convert to bot array index (0-5)
            ProcessPlayerRegeneration(players[i], ref botRegenTimers[botIndex], botLastCombatTimes[botIndex]);
        }
    }
}

private void ProcessPlayerRegeneration(IPlayer player, ref float regenTimer, float lastCombatTime)
{
    if (player.IsDead) return;
    
    PlayerModifiers mods = player.GetModifiers();
    float currentTime = Game.TotalElapsedGameTime;
    
    // Check if player is in combat cooldown
    if (lastCombatTime >= 0 && currentTime - lastCombatTime < COMBAT_COOLDOWN)
    {
        regenTimer = -1; // Reset regen timer during combat cooldown
        return;
    }
    
    // Start regeneration timer immediately after combat cooldown (no energy requirement)
    if (regenTimer < 0)
    {
        regenTimer = currentTime; // Start regen timer
    }
    
    // Check if enough time has passed for regeneration
    if (currentTime - regenTimer >= REGEN_DELAY)
    {
        RegenerateHealth(player, mods, 0.03f);
    }
}

private void RegenerateHealth(IPlayer player, PlayerModifiers mods, float regenPercentage)
{
    if (mods.CurrentHealth < mods.MaxHealth)
    {
        // Regenerate health based on percentage of max health
        int regenAmount = (int)(mods.MaxHealth * regenPercentage);
        // Ensure at least 1 HP regen if needed
        // if (regenAmount < 1) regenAmount = 1;

        mods.CurrentHealth = Math.Min(mods.MaxHealth, mods.CurrentHealth + regenAmount);
        player.SetModifiers(mods);
    }
}
