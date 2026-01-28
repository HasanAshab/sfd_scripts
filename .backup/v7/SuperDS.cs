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

public void OnPlayerDamage(IPlayer player, PlayerDamageArgs args)
{
    // Track when player takes damage
    UpdateCombatTime(player);
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
