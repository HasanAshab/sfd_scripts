// Configuration Variables
private const float SUSANO_ACTIVATE_THRESHOLD = 20f;
private const float SUSANO_BREAK_THRESHOLD = 10f;
private const float SUSANO_SIZE_MULTIPLIER = 2f;
private const float SUSANO_HEALTH_MULTIPLIER = 3f;
private const float SUSANO_MELEE_DAMAGE_MULTIPLIER = 1.7f;
private const float SUSANO_PROJECTILE_DAMAGE_MULTIPLIER = 1f;

private const float UCHIHA_EYE_CONTACT_RANGE = 160f;
private const int UCHIHA_EYE_CONTACT_BURN_CHANCE = 75;
private const int UCHIHA_EYE_CONTACT_CHECK_INTERVAL = 500;

private const int UCHIHA_SECOND_PUNCH_COUNT = 2;
private const int UCHIHA_THIRD_PUNCH_COUNT = 3;
private const int UCHIHA_PUNCH_WINDOW = 600;
private const int UCHIHA_SECOND_PUNCH_STUN_DURATION = 1000;
private const int UCHIHA_SECOND_PUNCH_SHOCK_DAMAGE = 30;

private const float SENJU_MAX_ENERGY_MULTIPLIER = 3f;
private const float SENJU_ENERGY_RECHARGE_MULTIPLIER = 1.3f;
private const int SENJU_HEAL_INTERVAL = 3000;
private const float SENJU_HEAL_PERCENTAGE = 0.01f;
private const int SENJU_SLOWMO_INTERVAL = 20000;

private const int SENJU_SECOND_PUNCH_COUNT = 1;
private const int SENJU_THIRD_PUNCH_COUNT = 2;
private const int SENJU_PUNCH_WINDOW = 1000;
private const float SENJU_THIRD_PUNCH_FORCE = 10f;
private const float SENJU_THIRD_PUNCH_IMPACT_RESISTANCE = 0.1f;
private const float SENJU_THIRD_PUNCH_DAMAGE_MULTIPLIER = 3f;
private const int SENJU_THIRD_PUNCH_DAMAGE_DURATION = 500;
private const int SENJU_SECOND_PUNCH_STUN_DURATION = 1000;

private const float SENJU_JUMP_ATTACK_RANGE = 40f;
private const float SENJU_JUMP_ATTACK_SLOW_SPEED = 0.005f;
private const int SENJU_JUMP_ATTACK_SLOW_DURATION = 5000;
private const float SENJU_JUMP_ATTACK_ENERGY_COST = 200f;
private const int SENJU_JUMP_ATTACK_DAMAGE = 40;

private const int SENJU_BLOCKS_REQUIRED = 2;
private const float GOLEM_SUMMON_ENERGY_COST = 250f;
private const float GOLEM_ENERGY_DRAIN_PER_SECOND = 20f;
private const int GOLEM_ENERGY_DRAIN_INTERVAL = 500;
private const float GOLEM_MAX_HEALTH = 200f;
private const float GOLEM_SIZE_MULTIPLIER = 2f;
private const float GOLEM_SPEED_MULTIPLIER = 0.5f;
private const float GOLEM_MELEE_DAMAGE_MULTIPLIER = 1.5f;
private const float GOLEM_DAMAGE_RESISTANCE = 0.6f;
private const float GOLEM_FIRE_DAMAGE_MOD = 1.5f;

private const int HEALTH_MONITOR_INTERVAL = 100;
private const int FACING_TRACKING_INTERVAL = 30; // 30ms

private IPlayer uchihaPlayer = null;
private bool susanoActive = false;
private bool susanoUsed = false; // Track if Susano has been used once
private int originalMaxHealth = 100;
private float originalSizeModifier = 1f;
private float originalMeleeForceModifier = 1f;
private IProfile originalProfile = null;
private int uchihaPunchCount = 0;
private float lastUchihaPunchTime = 0f;
private List<int> shockVictimIDs = new List<int>();
private IPlayer senjuPlayer = null;
private List<IPlayer> woodenGolems = new List<IPlayer>();
private int senjuBlockCount = 0;
private int senjuPunchCount = 0;
private float lastSenjuPunchTime = 0f;
private float senjuOriginalMeleeForce = 1f;
private float senjuOriginalMeleeDamage = 1f;
private List<int> thirdPunchVictimIDs = new List<int>();
private List<int> secondPunchStunnedIDs = new List<int>();
private Dictionary<int, float> slowedPlayersOriginalRunSpeed = new Dictionary<int, float>();
private Dictionary<int, float> slowedPlayersOriginalSprintSpeed = new Dictionary<int, float>();



public void OnStartup()
{
    IPlayer[] players = Game.GetPlayers();
    GiveUchihaAbility(players[0]);
    GiveSenjuAbility(players[1]);
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
    // Give initial SLOWMO_5 removed - moved to Senju
    
    // Set up health monitoring timer for Susano transformation
    // IObjectTimerTrigger healthMonitorTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    // healthMonitorTimer.SetIntervalTime(HEALTH_MONITOR_INTERVAL);
    // healthMonitorTimer.SetRepeatCount(0); // Infinite repeats
    // healthMonitorTimer.SetScriptMethod("MonitorUchihaHealth");
    // healthMonitorTimer.Trigger();
    
    // Set up eye contact burning ability timer
    // IObjectTimerTrigger eyeContactTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    // eyeContactTimer.SetIntervalTime(UCHIHA_EYE_CONTACT_CHECK_INTERVAL);
    // eyeContactTimer.SetRepeatCount(0);
    // eyeContactTimer.SetScriptMethod("CheckUchihaEyeContact");
    // eyeContactTimer.Trigger();
    
    // Set up melee action callback for combat punch abilities
    Events.PlayerMeleeActionCallback.Start(OnUchihaMeleeAction);
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
    if (!susanoActive && !susanoUsed && healthPercentage <= SUSANO_ACTIVATE_THRESHOLD)
    {
        ActivateSusano();
    }
    
    // Break Susano when health drops to 10% while Susano is active
    if (susanoActive && healthPercentage <= SUSANO_BREAK_THRESHOLD)
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
    mods.SizeModifier = SUSANO_SIZE_MULTIPLIER;
    mods.MaxHealth = (int)(originalMaxHealth * SUSANO_HEALTH_MULTIPLIER);
    mods.CurrentHealth = (int)(originalMaxHealth * SUSANO_HEALTH_MULTIPLIER);
    mods.MeleeForceModifier = (int)(originalMeleeForceModifier * SUSANO_MELEE_DAMAGE_MULTIPLIER);
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
        
        // Check range - player must be within configured range of Uchiha
        Vector2 playerPos = player.GetWorldPosition();
        float distance = Vector2.Distance(uchihaPos, playerPos);
        if (distance > UCHIHA_EYE_CONTACT_RANGE) continue;
        
        // Check if they are facing each other (different facing directions)
        int playerFacing = player.FacingDirection;
        if (playerFacing == uchihaFacing) continue; // Same direction = not facing each other
        
        // Configured chance to burn the player
        int randomChance = (int)(Game.TotalElapsedGameTime * 1000) + player.UniqueID;
        if ((randomChance % 100) < UCHIHA_EYE_CONTACT_BURN_CHANCE)
        {
            // Burn the player
            player.SetMaxFire();
            
            // Show eye contact message
            Game.ShowChatMessage("SHARINGAN! " + player.GetProfile().Name + " burned by eye contact!", Color.Red);
        }
    }
}

public void OnUchihaMeleeAction(IPlayer attacker, PlayerMeleeHitArg[] args)
{
    if (attacker == null || uchihaPlayer == null || attacker.UniqueID != uchihaPlayer.UniqueID) return;
    
    // Don't use combat punch abilities when Susano is active
    if (susanoActive) return;
    
    float currentTime = Game.TotalElapsedGameTime;
    
    // Check if any hit was blocked by a player (blocked hits deal 0 damage)
    bool wasBlocked = false;
    bool hitSomething = false;
    
    foreach (PlayerMeleeHitArg arg in args)
    {
        if (arg.HitObject != null)
        {
            hitSomething = true;
            // Check if this was a blocked hit (blocked hits deal 0 damage on players)
            if (arg.IsPlayer && arg.HitDamage == 0)
            {
                wasBlocked = true;
                break;
            }
        }
    }
    
    // Don't count the punch if it was blocked by a player
    if (wasBlocked)
    {
        return;
    }
    
    // Only count if we actually hit something
    if (!hitSomething)
    {
        return;
    }
    
    // Check if this punch is within the time window
    if (currentTime - lastUchihaPunchTime <= UCHIHA_PUNCH_WINDOW)
    {
        uchihaPunchCount++;
    }
    else
    {
        uchihaPunchCount = 1;
    }
    
    lastUchihaPunchTime = currentTime;
    
    // Check if this is the second punch (shock and stun)
    if (uchihaPunchCount == UCHIHA_SECOND_PUNCH_COUNT)
    {
        foreach (PlayerMeleeHitArg arg in args)
        {
            if (arg.HitObject != null)
            {
                // Create electric effect at hit location
                Game.PlayEffect(EffectName.Electric, arg.HitObject.GetWorldPosition());
                
                if (arg.IsPlayer)
                {
                    IPlayer hitPlayer = arg.HitObject as IPlayer;
                    if (hitPlayer != null && !hitPlayer.IsDead)
                    {
                        // Apply shock damage
                        PlayerModifiers hitMods = hitPlayer.GetModifiers();
                        hitMods.CurrentHealth = hitMods.CurrentHealth - UCHIHA_SECOND_PUNCH_SHOCK_DAMAGE;
                        
                        if (hitMods.CurrentHealth <= 0)
                        {
                            hitMods.CurrentHealth = 0;
                            hitPlayer.SetModifiers(hitMods);
                            hitPlayer.Kill();
                        }
                        else
                        {
                            hitPlayer.SetModifiers(hitMods);
                        }
                        
                        // Store victim ID for stun
                        shockVictimIDs.Add(hitPlayer.UniqueID);
                        
                        // Stun the player
                        hitPlayer.SetInputEnabled(false);
                    }
                }
                else
                {
                    // Hit an object - apply shock effect
                    IObject hitObject = arg.HitObject;
                    
                    // Destroy destructible objects
                    if (hitObject.DestructionInitiated == false)
                    {
                        hitObject.Destroy();
                    }
                }
            }
        }
        
        // Show message
        Game.ShowChatMessage("UCHIHA SECOND PUNCH! SHOCK!", Color.Red);
        
        // Set up timer to restore movement
        IObjectTimerTrigger stunTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
        stunTimer.SetIntervalTime(UCHIHA_SECOND_PUNCH_STUN_DURATION);
        stunTimer.SetRepeatCount(1);
        stunTimer.SetScriptMethod("RestoreShockVictims");
        stunTimer.Trigger();
    }
    // Check if this is the third punch (burn)
    else if (uchihaPunchCount >= UCHIHA_THIRD_PUNCH_COUNT)
    {
        foreach (PlayerMeleeHitArg arg in args)
        {
            if (arg.HitObject != null)
            {
                // Create fire effect at hit location
                Game.PlayEffect(EffectName.Fire, arg.HitObject.GetWorldPosition());
                
                if (arg.IsPlayer)
                {
                    IPlayer hitPlayer = arg.HitObject as IPlayer;
                    if (hitPlayer != null && !hitPlayer.IsDead)
                    {
                        // Burn the player
                        hitPlayer.SetMaxFire();
                    }
                }
                else
                {
                    // Hit an object - set it on fire
                    IObject hitObject = arg.HitObject;
                    hitObject.SetMaxFire();
                }
            }
        }
        
        // Show message
        Game.ShowChatMessage("UCHIHA THIRD PUNCH! BURN!", Color.Red);
        
        // Reset punch counter
        uchihaPunchCount = 0;
    }
}

public void RestoreShockVictims(TriggerArgs args)
{
    if (shockVictimIDs.Count == 0) return;
    
    IPlayer[] allPlayers = Game.GetPlayers();
    foreach (IPlayer p in allPlayers)
    {
        if (shockVictimIDs.Contains(p.UniqueID) && !p.IsDead)
        {
            p.SetInputEnabled(true);
        }
    }
    
    shockVictimIDs.Clear();
}


public void GiveSenjuAbility(IPlayer player)
{
    // Store the player reference
    senjuPlayer = player;
    senjuPlayer.SetProfile(new IProfile()
    {
        Name = "kushi saul",
        Gender = Gender.Male,
        Skin = new IProfileClothingItem("Normal", "Skin5", "ClothingLightGray"),
        Head = new IProfileClothingItem("AviatorHat2", "ClothingDarkGray", "ClothingLightRed"),
        ChestOver = new IProfileClothingItem("Coat", "ClothingPurple", "ClothingPurple"),
        Hands = new IProfileClothingItem("FingerlessGlovesBlack", "ClothingGray"),
        Legs = new IProfileClothingItem("Pants", "ClothingPurple"),
        Feet = new IProfileClothingItem("ShoesBlack", "ClothingDarkGray"),
        Accesory = new IProfileClothingItem("GasMask", "ClothingDarkGray", "ClothingLightRed"),
    });

    // Get current modifiers
    PlayerModifiers mods = player.GetModifiers();

    // Set max energy to configured multiplier (default is 100)
    mods.MaxEnergy = (int)(mods.MaxEnergy * SENJU_MAX_ENERGY_MULTIPLIER);
    mods.CurrentEnergy = (int)(mods.CurrentEnergy * SENJU_MAX_ENERGY_MULTIPLIER);
    mods.EnergyRechargeModifier = SENJU_ENERGY_RECHARGE_MULTIPLIER;

    // Apply modifiers
    player.SetModifiers(mods);
    
    // Give initial SLOWMO_5
    if (senjuPlayer != null && !senjuPlayer.IsDead)
    {   
        senjuPlayer.RemoveWeaponItemType(WeaponItemType.Powerup);
        senjuPlayer.GiveWeaponItem(WeaponItem.SLOWMO_5);
    }

    // Set up timer to heal configured percentage of max HP every configured interval
    IObjectTimerTrigger senjuHealTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    senjuHealTimer.SetIntervalTime(SENJU_HEAL_INTERVAL);
    senjuHealTimer.SetRepeatCount(0); // Infinite repeats
    senjuHealTimer.SetScriptMethod("HealSenjuPlayer");
    senjuHealTimer.Trigger();
    
    // Set up timer to give SLOWMO_5 every 20 seconds
    IObjectTimerTrigger senjuSlowmoTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    senjuSlowmoTimer.SetIntervalTime(SENJU_SLOWMO_INTERVAL);
    senjuSlowmoTimer.SetRepeatCount(0); // Infinite repeats
    senjuSlowmoTimer.SetScriptMethod("GiveSenjuSlowmo");
    senjuSlowmoTimer.Trigger();
    
    // Set up golem energy drain timer
    IObjectTimerTrigger golemEnergyTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    golemEnergyTimer.SetIntervalTime(GOLEM_ENERGY_DRAIN_INTERVAL);
    golemEnergyTimer.SetRepeatCount(0); // Infinite repeats
    golemEnergyTimer.SetScriptMethod("DrainGolemEnergy");
    golemEnergyTimer.Trigger();
    
    // Set up player key input callback for block detection
    Events.PlayerKeyInputCallback.Start(OnSenjuKeyInput);
    
    // Set up melee action callback for third punch ability
    Events.PlayerMeleeActionCallback.Start(OnSenjuMeleeAction);
    
    // Set up player death callback for golem cleanup
    Events.PlayerDeathCallback.Start(OnSenjuPlayerDeath);
    
    // Show ability granted message
    Game.ShowChatMessage("SENJU ABILITY GRANTED! " + (int)SENJU_MAX_ENERGY_MULTIPLIER + "x Energy + " + (int)SENJU_ENERGY_RECHARGE_MULTIPLIER + "x Recharge + Regeneration + Golem Summon", Color.Green);
}


public void HealSenjuPlayer(TriggerArgs args)
{
    // Heal Senju player every configured interval
    if (senjuPlayer == null || senjuPlayer.IsDead) return;
    
    PlayerModifiers mods = senjuPlayer.GetModifiers();
    float maxHealth = mods.MaxHealth;
    float currentHealth = mods.CurrentHealth;
    
    // Heal configured percentage of max HP
    float healAmount = maxHealth * SENJU_HEAL_PERCENTAGE;
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

public void GiveSenjuSlowmo(TriggerArgs args)
{
    // Give SLOWMO_5 to the Senju player every 20 seconds
    if (senjuPlayer != null && !senjuPlayer.IsDead)
    {
        senjuPlayer.RemoveWeaponItemType(WeaponItemType.Powerup);
        senjuPlayer.GiveWeaponItem(WeaponItem.SLOWMO_5);
    }
}

public void OnSenjuKeyInput(IPlayer player, VirtualKeyInfo[] keyInfos)
{
    // Only track Senju player
    if (player == null || senjuPlayer == null || player.UniqueID != senjuPlayer.UniqueID) return;
    
    foreach (VirtualKeyInfo keyInfo in keyInfos)
    {
        // Check for ATTACK key press while in mid-air (jump attack)
        if (keyInfo.Event == VirtualKeyEvent.Pressed && keyInfo.Key == VirtualKey.ATTACK)
        {
            // Check if player is in mid-air (not on ground)
            if (!player.IsOnGround)
            {
                PerformSenjuJumpAttack(player);
            }
        }
        
        // Check for block key press while sitting
        if (keyInfo.Event == VirtualKeyEvent.Pressed && keyInfo.Key == VirtualKey.BLOCK)
        {
            // Check if player is sitting (crouching)
            if (player.IsCrouching)
            {
                senjuBlockCount++;
                Game.ShowChatMessage("Senju Block Count: " + senjuBlockCount + "/" + SENJU_BLOCKS_REQUIRED, Color.Green);
                
                // Summon golem after configured blocks required
                if (senjuBlockCount >= SENJU_BLOCKS_REQUIRED)
                {
                    // disabled for now
                    // SummonWoodenGolem();
                    senjuBlockCount = 0; // Reset counter
                }
            }
        }
    }
}

private void PerformSenjuJumpAttack(IPlayer senju)
{
    // Check if Senju has enough energy
    PlayerModifiers senjuMods = senju.GetModifiers();
    if (senjuMods.CurrentEnergy < SENJU_JUMP_ATTACK_ENERGY_COST)
    {
        Game.ShowChatMessage("Not enough energy for jump attack! (" + SENJU_JUMP_ATTACK_ENERGY_COST + " required)", Color.Red);
        return;
    }
    
    // Deduct energy cost
    senjuMods.CurrentEnergy -= SENJU_JUMP_ATTACK_ENERGY_COST;
    senju.SetModifiers(senjuMods);
    
    Vector2 senjuPosition = senju.GetWorldPosition();
    PlayerTeam senjuTeam = senju.GetTeam();
    
    // Find all players within range
    IPlayer[] allPlayers = Game.GetPlayers();
    foreach (IPlayer target in allPlayers)
    {
        if (target.UniqueID != senju.UniqueID && !target.IsDead)
        {
            Vector2 targetPosition = target.GetWorldPosition();
            float distance = Vector2.Distance(senjuPosition, targetPosition);
            
            if (distance <= SENJU_JUMP_ATTACK_RANGE)
            {
                // Skip teammates - only affect enemies
                if (target.GetTeam() != senjuTeam)
                {
                    // Deal damage to the target
                    PlayerModifiers targetMods = target.GetModifiers();
                    targetMods.CurrentHealth -= SENJU_JUMP_ATTACK_DAMAGE;

                    if (targetMods.CurrentHealth <= 0)
                    {
                        targetMods.CurrentHealth = 0;
                        target.SetModifiers(targetMods);
                        target.Kill();
                    }
                    else
                    {
                        target.SetModifiers(targetMods);
                    }
                    
                    // Make the player fall
                    target.SetInputEnabled(false);
                    target.AddCommand(new PlayerCommand(PlayerCommandType.Fall));

                    // Create a timer to restore movement after stun duration
                    IObjectTimerTrigger stunTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
                    stunTimer.SetIntervalTime(5000);
                    stunTimer.SetRepeatCount(1);
                    stunTimer.SetScriptMethod("RestorePlayerMovement");
                    stunTimer.Trigger();

                    // Store original speeds
                    PlayerModifiers targetMods2 = target.GetModifiers();
                    slowedPlayersOriginalRunSpeed[target.UniqueID] = targetMods2.RunSpeedModifier;
                    slowedPlayersOriginalSprintSpeed[target.UniqueID] = targetMods2.SprintSpeedModifier;
                    
                    // Set slow speed
                    targetMods2.RunSpeedModifier = SENJU_JUMP_ATTACK_SLOW_SPEED;
                    targetMods2.SprintSpeedModifier = SENJU_JUMP_ATTACK_SLOW_SPEED;
                    target.SetModifiers(targetMods2);
                    
                    // Create visual effect at target location
                    Game.PlayEffect(EffectName.Dig, targetPosition);
                }
            }
        }
    }
    
    // Destroy all objects within range using area-based search for better performance
    Area attackArea = new Area(
        senjuPosition.Y + SENJU_JUMP_ATTACK_RANGE,
        senjuPosition.X - SENJU_JUMP_ATTACK_RANGE,
        senjuPosition.Y - SENJU_JUMP_ATTACK_RANGE,
        senjuPosition.X + SENJU_JUMP_ATTACK_RANGE
    );
    
    IObject[] objectsInArea = Game.GetObjectsByArea(attackArea);
    foreach (IObject obj in objectsInArea)
    {
        if (obj != null && !(obj is IPlayer) && obj.GetBodyType() == BodyType.Dynamic && !obj.DestructionInitiated)
        {
            obj.Destroy();
        }
    }
    
    // Create main effect at Senju's location
    Game.PlayEffect(EffectName.CameraShaker, senjuPosition, 10.0f, 700.5f, false);
    
    // Show message
    Game.ShowChatMessage("SENJU JUMP ATTACK! GROUND SLAM!", Color.Green);
    
    // Set up timer to restore speeds after duration
    IObjectTimerTrigger restoreSpeedTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    restoreSpeedTimer.SetIntervalTime(SENJU_JUMP_ATTACK_SLOW_DURATION);
    restoreSpeedTimer.SetRepeatCount(1);
    restoreSpeedTimer.SetScriptMethod("RestoreSlowedPlayersSpeeds");
    restoreSpeedTimer.Trigger();
}

public void RestoreSlowedPlayersSpeeds(TriggerArgs args)
{
    if (slowedPlayersOriginalRunSpeed.Count == 0) return;
    
    IPlayer[] allPlayers = Game.GetPlayers();
    foreach (IPlayer p in allPlayers)
    {
        if (slowedPlayersOriginalRunSpeed.ContainsKey(p.UniqueID) && !p.IsDead)
        {
            PlayerModifiers mods = p.GetModifiers();
            mods.RunSpeedModifier = slowedPlayersOriginalRunSpeed[p.UniqueID];
            mods.SprintSpeedModifier = slowedPlayersOriginalSprintSpeed[p.UniqueID];
            p.SetModifiers(mods);
        }
    }
    
    // Clear the dictionaries
    slowedPlayersOriginalRunSpeed.Clear();
    slowedPlayersOriginalSprintSpeed.Clear();
}

public void OnSenjuMeleeAction(IPlayer attacker, PlayerMeleeHitArg[] args)
{
    // Only track Senju player
    if (attacker == null || senjuPlayer == null || attacker.UniqueID != senjuPlayer.UniqueID) return;
    
    float currentTime = Game.TotalElapsedGameTime;
    
    // Check if any hit was blocked by a player (blocked hits deal 0 damage)
    bool wasBlocked = false;
    bool hitSomething = false;
    
    foreach (PlayerMeleeHitArg arg in args)
    {
        if (arg.HitObject != null)
        {
            hitSomething = true;
            // Check if this was a blocked hit (blocked hits deal 0 damage on players)
            if (arg.IsPlayer && arg.HitDamage == 0)
            {
                wasBlocked = true;
                break;
            }
        }
    }
    
    // Don't count the punch if it was blocked by a player
    // if (wasBlocked)
    // {
    //     return;
    // }
    
    // Only count if we actually hit something
    if (!hitSomething)
    {
        return;
    }

    // Check if this punch is within the time window
    if (currentTime - lastSenjuPunchTime <= SENJU_PUNCH_WINDOW)
    {
        senjuPunchCount++;
    }
    else
    {
        // Reset counter if too much time has passed
        senjuPunchCount = 1;
    }
    
    lastSenjuPunchTime = currentTime;
    Game.ShowChatMessage("SENJU PUNCH COUNT: " + senjuPunchCount, Color.Green);
    
    // Check if this is the second punch (stun)
    if (senjuPunchCount == SENJU_SECOND_PUNCH_COUNT)
    {
        foreach (PlayerMeleeHitArg arg in args)
        {
            if (arg.IsPlayer && arg.HitObject != null)
            {
                IPlayer hitPlayer = arg.HitObject as IPlayer;
                if (hitPlayer != null && !hitPlayer.IsDead)
                {
                    // Store victim ID for stun
                    secondPunchStunnedIDs.Add(hitPlayer.UniqueID);
                    
                    // Stun the player
                    hitPlayer.SetInputEnabled(false);
                }
            }
        }
        
        // Show message
        Game.ShowChatMessage("SENJU SECOND PUNCH! STUN!", Color.Cyan);
        
        // Set up timer to restore movement
        IObjectTimerTrigger stunTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
        stunTimer.SetIntervalTime(SENJU_SECOND_PUNCH_STUN_DURATION);
        stunTimer.SetRepeatCount(1);
        stunTimer.SetScriptMethod("RestoreSecondPunchStunnedPlayers");
        stunTimer.Trigger();
    }
    // Check if this is the third punch
    else if (senjuPunchCount >= SENJU_THIRD_PUNCH_COUNT)
    {
        // Apply super force and damage to Senju player
        PlayerModifiers senjuMods = senjuPlayer.GetModifiers();
        float originalMeleeForce = senjuMods.MeleeForceModifier;
        float originalMeleeDamage = senjuMods.MeleeDamageDealtModifier;
        senjuMods.MeleeForceModifier = SENJU_THIRD_PUNCH_FORCE;
        senjuMods.MeleeDamageDealtModifier = SENJU_THIRD_PUNCH_DAMAGE_MULTIPLIER;
        senjuPlayer.SetModifiers(senjuMods);
        
        // Apply impact resistance to all hit targets
        foreach (PlayerMeleeHitArg arg in args)
        {
            if (arg.IsPlayer && arg.HitObject != null)
            {
                IPlayer hitPlayer = arg.HitObject as IPlayer;
                if (hitPlayer != null && !hitPlayer.IsDead)
                {
                    PlayerModifiers hitMods = hitPlayer.GetModifiers();
                    hitMods.ImpactDamageTakenModifier = SENJU_THIRD_PUNCH_IMPACT_RESISTANCE;
                    hitPlayer.SetModifiers(hitMods);
                    
                    // Store victim ID for reset
                    thirdPunchVictimIDs.Add(hitPlayer.UniqueID);
                }
            }
        }
        
        // Show third punch message
        Game.ShowChatMessage("SENJU THIRD PUNCH! MASSIVE KNOCKBACK!", Color.Cyan);
        
        // Reset punch counter
        senjuPunchCount = 0;
        
        // Store original melee force and damage
        senjuOriginalMeleeForce = originalMeleeForce;
        senjuOriginalMeleeDamage = originalMeleeDamage;
        
        // Reset Senju's melee force and damage after a tiny delay
        IObjectTimerTrigger resetForceTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
        resetForceTimer.SetIntervalTime(SENJU_THIRD_PUNCH_DAMAGE_DURATION);
        resetForceTimer.SetRepeatCount(1);
        resetForceTimer.SetScriptMethod("ResetSenjuMeleeForce");
        resetForceTimer.Trigger();
        
        // Reset impact resistance after a short delay (1s)
        IObjectTimerTrigger resetImpactTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
        resetImpactTimer.SetIntervalTime(1000);
        resetImpactTimer.SetRepeatCount(1);
        resetImpactTimer.SetScriptMethod("ResetImpactResistance");
        resetImpactTimer.Trigger();
    }
}

public void RestoreSecondPunchStunnedPlayers(TriggerArgs args)
{
    if (secondPunchStunnedIDs.Count == 0) return;
    
    IPlayer[] allPlayers = Game.GetPlayers();
    foreach (IPlayer p in allPlayers)
    {
        if (secondPunchStunnedIDs.Contains(p.UniqueID) && !p.IsDead)
        {
            p.SetInputEnabled(true);
        }
    }
    
    secondPunchStunnedIDs.Clear();
}

public void ResetSenjuMeleeForce(TriggerArgs args)
{
    if (senjuPlayer == null || senjuPlayer.IsDead) return;
    
    PlayerModifiers mods = senjuPlayer.GetModifiers();
    mods.MeleeForceModifier = senjuOriginalMeleeForce;
    mods.MeleeDamageDealtModifier = senjuOriginalMeleeDamage;
    senjuPlayer.SetModifiers(mods);
}

public void ResetImpactResistance(TriggerArgs args)
{
    if (thirdPunchVictimIDs.Count == 0) return;
    
    IPlayer[] allPlayers = Game.GetPlayers();
    foreach (IPlayer p in allPlayers)
    {
        if (thirdPunchVictimIDs.Contains(p.UniqueID) && !p.IsDead)
        {
            PlayerModifiers mods = p.GetModifiers();
            mods.ImpactDamageTakenModifier = 1f; // Reset to normal
            p.SetModifiers(mods);
        }
    }
    
    // Clear the victim list
    thirdPunchVictimIDs.Clear();
}

private void SummonWoodenGolem()
{
    if (senjuPlayer == null || senjuPlayer.IsDead) return;
    
    // Check if player has enough energy (configured energy cost required)
    PlayerModifiers mods = senjuPlayer.GetModifiers();
    Game.ShowChatMessage("Current Energy: " + mods.CurrentEnergy, Color.Green);
    if (mods.CurrentEnergy < GOLEM_SUMMON_ENERGY_COST)
    {
        Game.ShowChatMessage("Not enough energy to summon golem! (has " + (mods.CurrentEnergy) + ")", Color.Red);
        return;
    }
    
    // Deduct initial energy cost
    mods.CurrentEnergy -= GOLEM_SUMMON_ENERGY_COST;
    senjuPlayer.SetModifiers(mods);
    
    // Create wooden golem at Senju's position
    Vector2 senjuPos = senjuPlayer.GetWorldPosition();
    IPlayer woodenGolem = Game.CreatePlayer(senjuPos);
    
    if (woodenGolem != null)
    {
        // Add to golem list
        woodenGolems.Add(woodenGolem);
        
        // Set golem properties
        woodenGolem.SetTeam(senjuPlayer.GetTeam());
        woodenGolem.SetBotName("WOOD GOLEM");
        
        // Set as bot with defensive behavior
        BotBehavior golemBehavior = new BotBehavior(true, PredefinedAIType.CompanionD);
        woodenGolem.SetBotBehavior(golemBehavior);
        
        // Set golem to guard the Senju player
        woodenGolem.SetGuardTarget(senjuPlayer);
        
        // Set golem properties
        woodenGolem.SetNametagVisible(false);
        woodenGolem.SetStatusBarsVisible(false);
        woodenGolem.SetCameraSecondaryFocusMode(CameraFocusMode.Ignore);
        
        // Give golem enhanced stats (tanky guardian)
        PlayerModifiers golemMods = new PlayerModifiers();
        golemMods.MaxHealth = (int)GOLEM_MAX_HEALTH;
        golemMods.CurrentHealth = (int)GOLEM_MAX_HEALTH;
        golemMods.SizeModifier = GOLEM_SIZE_MULTIPLIER;
        golemMods.RunSpeedModifier = GOLEM_SPEED_MULTIPLIER;
        golemMods.SprintSpeedModifier = GOLEM_SPEED_MULTIPLIER;
        golemMods.MeleeDamageDealtModifier = GOLEM_MELEE_DAMAGE_MULTIPLIER;
        golemMods.ProjectileDamageTakenModifier = GOLEM_DAMAGE_RESISTANCE;
        golemMods.MeleeDamageTakenModifier = GOLEM_DAMAGE_RESISTANCE;
        golemMods.FireDamageTakenModifier = GOLEM_FIRE_DAMAGE_MOD;
        golemMods.MeleeStunImmunity = 1;
        woodenGolem.SetModifiers(golemMods);
        
        // Give golem weapons
        woodenGolem.GiveWeaponItem(WeaponItem.HAMMER);

        BotBehaviorSet golemBehaviorSet = woodenGolem.GetBotBehaviorSet();
        golemBehaviorSet.SetMeleeActionsToExpert();
        golemBehaviorSet.SearchItems = 0;
        woodenGolem.SetBotBehaviorSet(golemBehaviorSet);

        woodenGolem.SetStrengthBoostTime(9999999);
        
        // Set golem profile
        woodenGolem.SetProfile(GetWoodenGolemProfile());
        
        Game.ShowChatMessage("WOODEN GOLEM SUMMONED! (Total: " + woodenGolems.Count + ", -" + (GOLEM_SUMMON_ENERGY_COST * 100) + " energy, -" + (woodenGolems.Count * (GOLEM_ENERGY_DRAIN_PER_SECOND * 100)) + " energy/sec)", Color.Green);
    }
}

public void DrainGolemEnergy(TriggerArgs args)
{
    // Clean up dead golems first
    CleanupDeadGolems();
    
    // Only drain energy if golems are active
    if (woodenGolems.Count == 0 || senjuPlayer == null || senjuPlayer.IsDead) return;
    
    PlayerModifiers mods = senjuPlayer.GetModifiers();
    
    // Drain configured energy per second per golem
    float energyDrain = GOLEM_ENERGY_DRAIN_PER_SECOND * woodenGolems.Count;
    mods.CurrentEnergy -= energyDrain;
    
    // Check if out of energy
    if (mods.CurrentEnergy <= 0f)
    {
        mods.CurrentEnergy = 0f;
        senjuPlayer.SetModifiers(mods);
        
        // Destroy all golems when out of energy
        DestroyAllWoodenGolems();
        Game.ShowChatMessage("Out of energy! All Wooden Golems destroyed!", Color.Red);
    }
    else
    {
        senjuPlayer.SetModifiers(mods);
    }
}

private void DestroyAllWoodenGolems()
{
    foreach (IPlayer golem in woodenGolems)
    {
        if (golem != null && !golem.IsDead)
        {
            // Gib the golem
            golem.Gib();
        }
    }
    
    woodenGolems.Clear();
    senjuBlockCount = 0; // Reset block counter
}

private void CleanupDeadGolems()
{
    // Remove dead golems from the list
    woodenGolems.RemoveAll(golem => golem == null || golem.IsDead);
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
    // Clean up golems if Senju player dies
    if (player != null && senjuPlayer != null && player.UniqueID == senjuPlayer.UniqueID)
    {
        if (woodenGolems.Count > 0)
        {
            DestroyAllWoodenGolems();
            Game.ShowChatMessage("Senju died! All Wooden Golems destroyed!", Color.Yellow);
        }
    }
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