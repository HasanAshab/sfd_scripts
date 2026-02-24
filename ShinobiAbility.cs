// ShinobiAbility - Special abilities for shinobi players
// Provides Uchiha ability that grants SLOWMO_5 every 20 seconds

private IPlayer uchihaPlayer = null;

public void GiveUchihaAbility(IPlayer player)
{
    // Store the player reference
    uchihaPlayer = player;
    
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
}

public void GiveUchihaSlowmo(TriggerArgs args)
{
    // Give SLOWMO_5 to the Uchiha player every 20 seconds
    if (uchihaPlayer != null && !uchihaPlayer.IsDead)
    {
        uchihaPlayer.GiveWeaponItem(WeaponItem.SLOWMO_5);
    }
}
