// Player references (stored at startup to avoid index shifting when players die)
private IPlayer p1 = null;
private IPlayer p2 = null;

// P1 shock ability tracking
private int p1HitCounter = 0;
private const int SHOCK_HIT_THRESHOLD = 3; // Shock on 3rd hit

// P1 jump attack charge system
private bool p1HasJumpCharge = false;
private const int JUMP_CHARGE_INTERVAL = 15000; // 15 seconds to get a charge

// P1 jump attack ability tracking
private float p1VulnerableUntil = -1;
private const float VULNERABILITY_DURATION = 3000; // 3 seconds vulnerable after jump attack
private const float JUMP_SHOCK_RANGE = 40; // Range for jump shock effect

// P2 split ability tracking
private bool p2HasDied = false;
private bool p2HasSplit = false;
private int p2SplitLevel = 0; // 0 = no split, 1 = first split, 2 = second split
private List<IPlayer> p2SplitPlayers = new List<IPlayer>(); // Track all P2 split players

public void OnStartup()
{
    // Store player references at startup to avoid index shifting issues
    IPlayer[] players = Game.GetPlayers();
    p1 = players.Length >= 1 ? players[0] : null;
    p2 = players.Length >= 2 ? players[1] : null;
    
    if (p1 == null || p2 == null) return; // Need both players
    
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
    
    // Set up superpower events
    Events.PlayerDamageCallback.Start(OnPlayerDamage);
    Events.PlayerMeleeActionCallback.Start(OnPlayerMeleeAction);
    Events.PlayerKeyInputCallback.Start(OnPlayerKeyInput);
    Events.PlayerDeathCallback.Start(OnPlayerDeath);
    
    // Set up fire ammo timer for p1 (every 30 seconds)
    IObjectTimerTrigger fireAmmoTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    fireAmmoTimer.SetIntervalTime(30000); // 30 seconds
    fireAmmoTimer.SetRepeatCount(0); // Infinite repeats
    fireAmmoTimer.SetScriptMethod("GiveP1FireAmmo");
    fireAmmoTimer.Trigger();
    
    // Set up jump charge timer for p1 (every 15 seconds)
    IObjectTimerTrigger jumpChargeTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    jumpChargeTimer.SetIntervalTime(JUMP_CHARGE_INTERVAL); // 15 seconds
    jumpChargeTimer.SetRepeatCount(0); // Infinite repeats
    jumpChargeTimer.SetScriptMethod("GiveP1JumpCharge");
    jumpChargeTimer.Trigger();
    
    // Set up update event for charge effects
    Events.UpdateCallback.Start(OnUpdate, 1000); // Check every 1 second for charge effects 
}

public void GiveP1FireAmmo(TriggerArgs args)
{
    // Give p1 fire ammo every 30 seconds
    if (p1 != null && !p1.IsDead)
    {
        p1.GiveWeaponItem(WeaponItem.FIREAMMO);
    }
}

public void GiveP1JumpCharge(TriggerArgs args)
{
    // Give p1 a jump charge every 15 seconds
    p1HasJumpCharge = true;
}

public void OnPlayerKeyInput(IPlayer player, VirtualKeyInfo[] keyInfos)
{
    // Check if p1 pressed ATTACK while in mid-air
    if (p1 != null && player.UniqueID == p1.UniqueID)
    {
        foreach (VirtualKeyInfo keyInfo in keyInfos)
        {
            if (keyInfo.Event == VirtualKeyEvent.Pressed && keyInfo.Key == VirtualKey.ATTACK)
            {
                // Check if p1 is in mid-air (not on ground) and has a jump charge
                if (!player.IsOnGround && p1HasJumpCharge)
                {
                    PerformJumpShockAttack(player);
                    p1HasJumpCharge = false; // Consume the charge
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
                    // Apply shock effect to target - disable regen temporarily and deal heavy damage
                    PlayerModifiers targetMods = target.GetModifiers();
                    int shockDamage = 45; // Increased damage to ensure lethality
                    targetMods.CurrentHealth = targetMods.CurrentHealth - shockDamage;
                    
                    // Allow death if health drops to 0 or below
                    if (targetMods.CurrentHealth <= 0)
                    {
                        targetMods.CurrentHealth = 0;
                        target.SetModifiers(targetMods);
                        target.Kill(); // Force kill the player
                    }
                    else
                    {
                        target.SetModifiers(targetMods);
                    }
                    
                    // Disable shock regeneration temporarily
                    // Note: Regeneration is handled by separate Regen.cs script
                    UpdateCombatTime(target);
                    
                    // Disarm the target - drop their current weapon
                    DisarmPlayer(target);
                    
                    // Stun the target for 500ms
                    StunPlayer(target, 2000);
                    
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

public void OnPlayerDeath(IPlayer player, PlayerDeathArgs args)
{
    // Check if P2 or any P2 split player died
    if (p2 != null && (player.UniqueID == p2.UniqueID || p2SplitPlayers.Contains(player)))
    {
        Game.AutoVictoryConditionEnabled = false;
        
        // Determine split level based on current state
        if (player.UniqueID == p2.UniqueID && !p2HasDied)
        {
            // Original P2 died for the first time - first split
            p2HasDied = true;
            p2SplitLevel = 1;
            TriggerP2Split(player, 1);
        }
        else if (p2SplitPlayers.Contains(player) && p2SplitLevel == 1)
        {
            // A first-split player died - second split
            p2SplitLevel = 2;
            TriggerP2Split(player, 2);
        }
    }
}

private void TriggerP2Split(IPlayer deadPlayer, int splitLevel)
{
    // Trigger P2 split after a short delay to allow death animation
    IObjectTimerTrigger splitTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    splitTimer.SetIntervalTime(2); // 2ms delay
    splitTimer.SetRepeatCount(1);
    
    if (splitLevel == 1)
    {
        splitTimer.SetScriptMethod("SplitP2First");
    }
    else if (splitLevel == 2)
    {
        splitTimer.SetScriptMethod("SplitP2Second");
    }
    
    splitTimer.Trigger();
}

public void SplitP2First(TriggerArgs args)
{
    if (p2HasSplit || p2 == null) return; // Prevent multiple splits
    
    // First split: 1 human + 2 bots
    IPlayer oldP2 = p2;
    Vector2 spawnPos = oldP2.GetWorldPosition();
    PlayerTeam team = oldP2.GetTeam();
    IUser user = oldP2.GetUser();
    IProfile profile = oldP2.GetProfile();
    
    // Create new P2 (human)
    IPlayer newP2 = Game.CreatePlayer(spawnPos);
    newP2.SetTeam(team);
    newP2.SetUser(user);
    newP2.SetProfile(profile);
    
    // Update p2 reference and add to split players list
    p2 = newP2;
    p2SplitPlayers.Add(newP2);
    
    // Create 2 bot copies
    for (int i = 0; i < 2; i++)
    {
        IPlayer botPlayer = Game.CreatePlayer(spawnPos);
        botPlayer.SetTeam(team);
        botPlayer.SetGuardTarget(newP2);
        botPlayer.SetBotBehavior(new BotBehavior(true, PredefinedAIType.BotD));
        botPlayer.SetCameraSecondaryFocusMode(CameraFocusMode.Ignore);
        botPlayer.SetProfile(profile);
        botPlayer.SetNametagVisible(false);
        botPlayer.SetStatusBarsVisible(false);
        
        // Add bot to split players list for tracking
        p2SplitPlayers.Add(botPlayer);
    }
    
    p2HasSplit = true;
    Game.AutoVictoryConditionEnabled = true;
}

public void SplitP2Second(TriggerArgs args)
{
    if (p2SplitLevel != 2) return; // Only allow second split
    
    // Second split: Each remaining P2 split player creates 3 more
    // Result: 1 human + 8 bots (if 3 players remain from first split)
    
    List<IPlayer> playersToSplit = new List<IPlayer>(p2SplitPlayers);
    p2SplitPlayers.Clear(); // Clear the list to rebuild it
    
    foreach (IPlayer splitPlayer in playersToSplit)
    {
        if (splitPlayer != null && !splitPlayer.IsDead)
        {
            Vector2 spawnPos = splitPlayer.GetWorldPosition();
            PlayerTeam team = splitPlayer.GetTeam();
            IProfile profile = splitPlayer.GetProfile();
            
            // Apply 0.7x size to the original split player too
            PlayerModifiers originalMods = splitPlayer.GetModifiers();
            originalMods.SizeModifier = 0.7f;
            splitPlayer.SetModifiers(originalMods);
            
            // Keep the original split player
            p2SplitPlayers.Add(splitPlayer);
            
            // Create 2 additional copies (so each becomes 3 total)
            for (int i = 0; i < 2; i++)
            {
                IPlayer newPlayer;
                
                // If this is the human P2, create another human copy
                if (splitPlayer.UniqueID == p2.UniqueID)
                {
                    newPlayer = Game.CreatePlayer(spawnPos);
                    newPlayer.SetTeam(team);
                    newPlayer.SetUser(splitPlayer.GetUser());
                    newPlayer.SetProfile(profile);
                }
                else
                {
                    // Create bot copy
                    newPlayer = Game.CreatePlayer(spawnPos);
                    newPlayer.SetTeam(team);
                    newPlayer.SetGuardTarget(p2);
                    newPlayer.SetBotBehavior(new BotBehavior(true, PredefinedAIType.BotD));
                    newPlayer.SetCameraSecondaryFocusMode(CameraFocusMode.Ignore);
                    newPlayer.SetProfile(profile);
                    newPlayer.SetNametagVisible(false);
                    newPlayer.SetStatusBarsVisible(false);
                }
                
                // Set 0.7x size for all second split players
                PlayerModifiers newPlayerMods = newPlayer.GetModifiers();
                newPlayerMods.SizeModifier = 0.7f;
                newPlayer.SetModifiers(newPlayerMods);
                
                p2SplitPlayers.Add(newPlayer);
            }
        }
    }
    
    Game.AutoVictoryConditionEnabled = true;
}

public void OnPlayerDamage(IPlayer player, PlayerDamageArgs args)
{
    // Track combat for regeneration system (handled by Regen.cs)
    UpdateCombatTime(player);
    
    // Check if p1 is taking damage while vulnerable (increase damage)
    if (p1 != null && player.UniqueID == p1.UniqueID)
    {
        float currentTime = Game.TotalElapsedGameTime;
        if (p1VulnerableUntil > 0 && currentTime < p1VulnerableUntil)
        {
            // P1 is vulnerable - take extra damage and disable regen
            PlayerModifiers p1Mods = player.GetModifiers();
            int vulnerabilityDamage = 25; // Increased damage
            p1Mods.CurrentHealth = p1Mods.CurrentHealth - vulnerabilityDamage;
            
            // Allow death if health drops to 0 or below
            if (p1Mods.CurrentHealth <= 0)
            {
                p1Mods.CurrentHealth = 0;
                player.SetModifiers(p1Mods);
                player.Kill(); // Force kill the player
            }
            else
            {
                player.SetModifiers(p1Mods);
            }
            
            // Update combat time to prevent regeneration
            UpdateCombatTime(player);
        }
    }
}

public void OnPlayerMeleeAction(IPlayer player, PlayerMeleeHitArg[] args)
{
    // Track combat for regeneration system (handled by Regen.cs)
    UpdateCombatTime(player);
    
    // Check if p1 is performing melee action
    if (p1 != null && player.UniqueID == p1.UniqueID && args.Length > 0)
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
                        // Apply shock effect (stun + heavy damage) and disable regen
                        PlayerModifiers hitMods = hitPlayer.GetModifiers();
                        int shockDamage = 20; // Increased damage for 3-hit combo
                        hitMods.CurrentHealth = hitMods.CurrentHealth - shockDamage;
                        
                        // Allow death if health drops to 0 or below
                        if (hitMods.CurrentHealth <= 0)
                        {
                            hitMods.CurrentHealth = 0;
                            hitPlayer.SetModifiers(hitMods);
                            hitPlayer.Kill(); // Force kill the player
                        }
                        else
                        {
                            hitPlayer.SetModifiers(hitMods);
                        }
                        
                        // Disable regeneration temporarily (handled by Regen.cs)
                        UpdateCombatTime(hitPlayer);
                        
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

// Simple combat time tracking for regeneration system integration
private void UpdateCombatTime(IPlayer player)
{
    // This method is kept for integration with Regen.cs script
    // The actual regeneration logic is handled by the separate Regen.cs script
    
    // Reset P1 hit counter when out of combat (handled in OnUpdate)
    if (p1 != null && player.UniqueID == p1.UniqueID)
    {
        // P1 hit counter will be reset in OnUpdate when out of combat
    }
}

public void OnUpdate(float elapsed)
{
    // Check if we have valid player references
    if (p1 == null) return;
    
    float currentTime = Game.TotalElapsedGameTime;
    
    // Show continuous charge effect for P1 when jump charge is ready
    if (p1HasJumpCharge && !p1.IsDead)
    {
        // Show electric effect around P1 to indicate charge is ready
        Vector2 p1Position = p1.GetWorldPosition();
        Game.PlayEffect(EffectName.Sparks, p1Position);
    }
    
    // Show vulnerability effect for P1 when vulnerable after jump attack
    if (p1VulnerableUntil > 0 && currentTime < p1VulnerableUntil && !p1.IsDead)
    {
        // Show smoke trail effect around P1 to indicate vulnerability
        Vector2 p1Position = p1.GetWorldPosition();
        Game.PlayEffect(EffectName.Steam, p1Position);
        Game.PlayEffect(EffectName.Steam, p1Position);
    }
    
    // Reset P1 hit counter when not in active combat
    // This is a simplified version - full combat tracking is in Regen.cs
    if (!p1.IsDead)
    {
        // Reset hit counter periodically when not actively fighting
        // This ensures the combo doesn't persist indefinitely
        p1HitCounter = Math.Max(0, p1HitCounter - 1);
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
   
    player.SetInputEnabled(false);
    
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
            player.SetInputEnabled(true);                       
        }
    }
}
