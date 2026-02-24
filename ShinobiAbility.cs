// ShinobiAbility - Special abilities for shinobi players
// Provides Uchiha ability that grants SLOWMO_5 every 20 seconds
// Susano transformation at 20% HP and breaks at 10% HP
// Provides Senju ability with 3x max energy and health regeneration

private IPlayer uchihaPlayer = null;
private bool susanoActive = false;
private bool susanoUsed = false; // Track if Susano has been used once
private int originalMaxHealth = 100;
private float originalSizeModifier = 1f;

private IPlayer senjuPlayer = null;



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
    
    // Give initial SLOWMO_5
    if (uchihaPlayer != null && !uchihaPlayer.IsDead)
    {
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
}

public void GiveUchihaSlowmo(TriggerArgs args)
{
    // Give SLOWMO_5 to the Uchiha player every 20 seconds
    if (uchihaPlayer != null && !uchihaPlayer.IsDead)
    {
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
    mods.MaxHealth = (int)(originalMaxHealth * 3f); // 3x health
    mods.CurrentHealth = (int)(originalMaxHealth * 3f); // Full heal to 3x health

    // Strength boost
    mods.MeleeDamageDealtModifier = 2f; // 2x melee damage
    mods.ProjectileDamageDealtModifier = 1.5f; // 1.5x projectile damage

    // Apply modifiers
    uchihaPlayer.SetModifiers(mods);
    
    // Give Katana
    uchihaPlayer.GiveWeaponItem(WeaponItem.KATANA);
    
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
    
    // Remove strength boost
    mods.MeleeDamageDealtModifier = 1f; // Normal melee damage
    mods.ProjectileDamageDealtModifier = 1f; // Normal projectile damage
    
    // Apply modifiers
    uchihaPlayer.SetModifiers(mods);
    
    // Remove Katana
    uchihaPlayer.RemoveWeaponItemType(WeaponItemType.Melee);
    
    // Show break message
    Game.ShowChatMessage("SUSANO BROKEN!", Color.Blue);
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

    // Apply modifiers
    player.SetModifiers(mods);

    // Set up timer to heal 2% of max HP every 2 seconds
    IObjectTimerTrigger senjuHealTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    senjuHealTimer.SetIntervalTime(2000); // 2 seconds
    senjuHealTimer.SetRepeatCount(0); // Infinite repeats
    senjuHealTimer.SetScriptMethod("HealSenjuPlayer");
    senjuHealTimer.Trigger();
    
    // Show ability granted message
    Game.ShowChatMessage("SENJU ABILITY GRANTED! 3x Energy + Regeneration", Color.Green);
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
