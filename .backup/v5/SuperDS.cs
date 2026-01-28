// Track when players reach max energy for regeneration delay
private float p1MaxEnergyTime = -1;
private float p2MaxEnergyTime = -1;
private const float REGEN_DELAY = 1500; // 1.5 seconds in milliseconds

// Track combat activity to prevent regeneration
private float p1LastCombatTime = -1;
private float p2LastCombatTime = -1;
private const float COMBAT_COOLDOWN = 1500; // 1.5 seconds after combat before regen can start

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
    
    
    
    IPlayer p2 = Game.GetPlayers()[1];

    PlayerModifiers p2Mods = p2.GetModifiers();

    p2Mods.SizeModifier = 2;
    p2Mods.RunSpeedModifier = 0.8f;
    p2Mods.SprintSpeedModifier = 0.8f;

    p2Mods.MaxHealth = (int)(p2Mods.MaxHealth * 1.4f);
    p2Mods.CurrentHealth = (int)(p2Mods.CurrentHealth * 1.4f);

    p2Mods.MeleeDamageTakenModifier *= 0.4f;
    p2Mods.ProjectileDamageTakenModifier *= 0.3f;

    p2Mods.MeleeStunImmunity = 1;
    p2Mods.MeleeForceModifier *= 1.6f;
    p2Mods.MeleeDamageDealtModifier *= 1.4f;

    p2.SetModifiers(p2Mods);
    
    // Set up combat detection events
    Events.PlayerDamageCallback.Start(OnPlayerDamage);
    Events.PlayerMeleeActionCallback.Start(OnPlayerMeleeAction);
    
    // Set up update event for HP regeneration
    Events.UpdateCallback.Start(OnUpdate, 1000); // Check every 100ms for better timing precision
}

public void OnPlayerDamage(IPlayer player, PlayerDamageArgs args)
{
    // Track when player takes damage
    UpdateCombatTime(player);
}

public void OnPlayerMeleeAction(IPlayer player, PlayerMeleeHitArg[] args)
{
    // Track when player performs melee action (hitting)
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
    }
}

public void OnUpdate(float elapsed)
{
    IPlayer[] players = Game.GetPlayers();
    
    // Check if we have at least 2 players
    if (players.Length >= 2)
    {
        ProcessPlayerRegeneration(players[0], ref p1MaxEnergyTime, p1LastCombatTime);
        ProcessPlayerRegeneration(players[1], ref p2MaxEnergyTime, p2LastCombatTime);
    }
}

private void ProcessPlayerRegeneration(IPlayer player, ref float maxEnergyTime, float lastCombatTime)
{
    if (player.IsDead) return;
    
    PlayerModifiers mods = player.GetModifiers();
    float currentTime = Game.TotalElapsedGameTime;
    
    // Check if player is in combat cooldown
    if (lastCombatTime >= 0 && currentTime - lastCombatTime < COMBAT_COOLDOWN)
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
        if (currentTime - maxEnergyTime >= REGEN_DELAY)
        {
            RegenerateHealth(player, mods);
        }
    }
    else
    {
        // Reset timer if energy is not at max
        maxEnergyTime = -1;
    }
}

private void RegenerateHealth(IPlayer player, PlayerModifiers mods)
{
    if (mods.CurrentHealth < mods.MaxHealth)
    {
        // Regenerate 5% of max health
        int regenAmount = (int)(mods.MaxHealth * 0.05f);
        if (regenAmount < 1) regenAmount = 1; // Ensure at least 1 HP regen
        
        mods.CurrentHealth = Math.Min(mods.MaxHealth, mods.CurrentHealth + regenAmount);
        player.SetModifiers(mods);
    }
}
