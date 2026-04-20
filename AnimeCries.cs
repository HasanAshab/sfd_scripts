
// Bot references (must match Ujiri.cs bot names)
private IPlayer timpaBot = null;
private IPlayer bichiBot = null;
private IPlayer kokolaBot = null;
private IPlayer edurBot = null;
private IPlayer xrayBot = null;
private IPlayer pakhiBot = null;
private IPlayer psythicBot = null;

// Non-bot player references
private IPlayer bondukPlayer = null;
private IPlayer hateliPlayer = null;

// Tracking dictionaries
private Dictionary<int, bool> playerOnFire = new Dictionary<int, bool>();
private Dictionary<int, float> playerLastFireCryTime = new Dictionary<int, float>();
private Dictionary<int, bool> playerStrengthBoosted = new Dictionary<int, bool>();
private Dictionary<int, float> playerLastAmmoCheckTime = new Dictionary<int, float>();
private Dictionary<int, bool> playerWasGrabbed = new Dictionary<int, bool>();
private Dictionary<int, bool> playerWasDriven = new Dictionary<int, bool>();
private Dictionary<int, int> kokolaLastTargetedPlayer = new Dictionary<int, int>();
private Dictionary<int, float> kokolaLastTargetCryTime = new Dictionary<int, float>();
private Dictionary<int, bool> playerLastAliveEventFired = new Dictionary<int, bool>();
private Dictionary<int, WeaponItemType> playerLastWeapon = new Dictionary<int, WeaponItemType>();
private Dictionary<int, float> playerLastWeaponCryTime = new Dictionary<int, float>();
private Dictionary<int, float> playerLastFallingCryTime = new Dictionary<int, float>();
private Dictionary<int, float> playerLastFriendlyFireCryTime = new Dictionary<int, float>();

// Constants
private const float AMMO_CHECK_INTERVAL = 1000; // Check ammo every 1 second
private const float FIRE_CRY_COOLDOWN = 10000; // Fire cry cooldown: 10 seconds
private const float WEAPON_CRY_COOLDOWN = 10000; // Weapon cry cooldown: 10 seconds
private const float FALLING_CRY_COOLDOWN = 1500; // Falling cry cooldown: 1.5 seconds
private const float TARGET_CRY_COOLDOWN = 10000; // Target cry cooldown: 10 seconds
private const float FRIENDLY_FIRE_CRY_COOLDOWN = 5000; // Friendly fire cry cooldown: 5 seconds

// Edur special weapons list (by weapon item name)
private string[] edurSpecialWeapons = {
    "PISTOL",
    "CHAINSAW"
};

public void OnStartup()
{
    // Set up event callbacks
    Events.PlayerDamageCallback.Start(OnPlayerDamage);
    Events.PlayerDeathCallback.Start(OnPlayerDeath);
    Events.PlayerMeleeActionCallback.Start(OnPlayerMeleeAction);
    Events.UpdateCallback.Start(OnUpdate, 100); // Check every 100ms
    Events.ExplosionHitCallback.Start(OnExplosionHit);
    Events.ObjectCreatedCallback.Start(OnObjectCreated);
    Events.PlayerWeaponAddedActionCallback.Start(OnPlayerWeaponAdded);
    
    // Find bot references after 1 second delay (to ensure bots are created)
    IObjectTimerTrigger botRefTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    botRefTimer.SetIntervalTime(1000); // 1 second delay
    botRefTimer.SetRepeatCount(1); // Run once
    botRefTimer.SetScriptMethod("FindBotReferences");
    botRefTimer.Trigger();
}

public void FindBotReferences(TriggerArgs args)
{
    IPlayer[] allPlayers = Game.GetPlayers();
    foreach (IPlayer player in allPlayers)
    {
        string playerName = player.GetProfile().Name;
        
        // Find bots
        if (player.IsBot)
        {
            switch (playerName)
            {
                case "Timpa": timpaBot = player; break;
                case "Bichi": bichiBot = player; break;
                case "Kokola": kokolaBot = player; break;
                case "Edur": edurBot = player; break;
                case "Xray": xrayBot = player; break;
                case "Pakhi": pakhiBot = player; break;
                case "Psythic": psythicBot = player; break;
            }
        }
        else
        {
            // Find non-bot players
            switch (playerName)
            {
                case "Bonduk": bondukPlayer = player; break;
                case "Hateli": hateliPlayer = player; break;
            }
        }
    }
}

public void OnPlayerDamage(IPlayer player, PlayerDamageArgs args)
{
    if (player == null || player.IsDead || args.DamageType != PlayerDamageEventType.Projectile) return;

    string botName = GetBotName(player);
    if (botName == null) return;

    // Check for friendly fire (hit by own team member)
    if (args.SourceID != 0)
    {
        IProjectile projectile = Game.GetProjectile(args.SourceID);
        IPlayer attacker = Game.GetPlayer(projectile.InitialOwnerPlayerID);
      
        if (attacker != null && attacker.GetTeam() == player.GetTeam() && attacker.UniqueID != player.UniqueID)
        {
            float currentTime = Game.TotalElapsedGameTime;
            
            // Check cooldown before firing cry event
            bool canFireFriendlyFireCry = !playerLastFriendlyFireCryTime.ContainsKey(player.UniqueID) || 
                                          (currentTime - playerLastFriendlyFireCryTime[player.UniqueID] >= FRIENDLY_FIRE_CRY_COOLDOWN);
            
            if (canFireFriendlyFireCry)
            {
                playerLastFriendlyFireCryTime[player.UniqueID] = currentTime;
                
                // Friendly fire detected
                if (botName == "Pakhi")
                {
                    Crie("Pakhi", "friendly_fire");
                }
                else if (botName == "Timpa")
                {
                    Crie("Timpa", "friendly_fire");
                }
                else if (botName == "Bichi")
                {
                    Crie("Bichi", "friendly_fire");
                }
            }
        }
    }

    // Check for fire damage
    if (args.DamageType == PlayerDamageEventType.Fire)
    {
        float currentTime = Game.TotalElapsedGameTime;
        
        if (!playerOnFire.ContainsKey(player.UniqueID) || !playerOnFire[player.UniqueID])
        {
            playerOnFire[player.UniqueID] = true;
        }
        
        // Check cooldown before firing cry event
        bool canFireCry = !playerLastFireCryTime.ContainsKey(player.UniqueID) || 
                          (currentTime - playerLastFireCryTime[player.UniqueID] >= FIRE_CRY_COOLDOWN);
        
        if (canFireCry)
        {
            playerLastFireCryTime[player.UniqueID] = currentTime;
            
            if (botName == "Pakhi")
            {
                Crie("Pakhi", "on_fire");
            }
            else if (botName == "Timpa")
            {
                Crie("Timpa", "on_fire");
            }
        }
    }
}

public void OnPlayerDeath(IPlayer player, PlayerDeathArgs args)
{
    if (player == null) return;
    
    string playerName = GetPlayerName(player);
    if (playerName == null) return;

    // Check if player was gibbed - fire for all tracked players
    if (args.Removed)
    {
        Crie(playerName, "gibbed");
    }
}

public void OnPlayerMeleeAction(IPlayer player, PlayerMeleeHitArg[] args)
{
    if (player == null || player.IsDead) return;
    
    string botName = GetBotName(player);
    
    // Check if Edur is doing hand-to-hand combat (no weapon)
    if (botName == "Edur" && player.CurrentWeaponDrawn == WeaponItemType.NONE)
    {
        Crie("Edur", "hand_to_hand");
    }

    // Check if Kokola is targeting any player
    if (botName == "Kokola")
    {
        foreach (PlayerMeleeHitArg hitArg in args)
        {
            if (hitArg.IsPlayer && hitArg.HitObject != null)
            {
                IPlayer target = Game.GetPlayer(hitArg.HitObject.UniqueID);
                if (target != null && target.UniqueID != player.UniqueID)
                {
                    float currentTime = Game.TotalElapsedGameTime;
                    
                    // Check if this is a different player than last targeted
                    int lastTargetID = kokolaLastTargetedPlayer.ContainsKey(player.UniqueID) ? kokolaLastTargetedPlayer[player.UniqueID] : 0;
                    
                    // Check cooldown
                    bool canFireCry = !kokolaLastTargetCryTime.ContainsKey(player.UniqueID) || 
                                      (currentTime - kokolaLastTargetCryTime[player.UniqueID] >= TARGET_CRY_COOLDOWN);
                    
                    // Fire if target changed and cooldown passed
                    if (lastTargetID != target.UniqueID && canFireCry)
                    {
                        kokolaLastTargetedPlayer[player.UniqueID] = target.UniqueID;
                        kokolaLastTargetCryTime[player.UniqueID] = currentTime;
                        Crie("Kokola", "targeted_a_player");
                    }
                    else if (lastTargetID != target.UniqueID)
                    {
                        // Update last target even if cooldown hasn't passed
                        kokolaLastTargetedPlayer[player.UniqueID] = target.UniqueID;
                    }
                }
            }
        }
    }
    
    // Check if Pakhi is targeting any player
    if (botName == "Pakhi")
    {
        foreach (PlayerMeleeHitArg hitArg in args)
        {
            if (hitArg.IsPlayer && hitArg.HitObject != null)
            {
                IPlayer target = Game.GetPlayer(hitArg.HitObject.UniqueID);
                if (target != null && target.UniqueID != player.UniqueID)
                {
                    // Check if target has strength boost
                    if (target.GetStrengthBoostTime() > 0)
                    {
                        Crie("Pakhi", "opponent_strength_boost");
                    }
                }
            }
        }
    }
}

public void OnExplosionHit(ExplosionData explosionData, ExplosionHitArg[] args)
{
    foreach (ExplosionHitArg hitArg in args)
    {
        if (hitArg.IsPlayer && hitArg.HitObject != null)
        {
            IPlayer player = Game.GetPlayer(hitArg.HitObject.UniqueID);
            if (player != null && !player.IsDead)
            {
                string botName = GetBotName(player);
                if (botName == "Kokola")
                {
                    Crie("Kokola", "explodes");
                }
                else if (botName == "Pakhi")
                {
                    Crie("Pakhi", "explodes");
                }
            }
        }
    }
}

public void OnObjectCreated(IObject[] objects)
{
    // Placeholder for future use
}

public void OnPlayerWeaponAdded(IPlayer player, PlayerWeaponAddedArg arg)
{
    if (player == null || player.IsDead) return;
    
    string botName = GetBotName(player);
    if (botName != "Edur") return;
    
    // Check if the picked up weapon is in the special weapons list
    string weaponItem = arg.WeaponItem.ToString();
    foreach (string specialWeapon in edurSpecialWeapons)
    {
        if (weaponItem == specialWeapon)
        {
            Crie("Edur", "gets_weapon");
            break;
        }
    }
}

public void OnUpdate(float elapsed)
{
    float currentTime = Game.TotalElapsedGameTime;
    IPlayer[] allPlayers = Game.GetPlayers();

    foreach (IPlayer player in allPlayers)
    {
        if (player == null || player.IsDead) continue;
        
        string botName = GetBotName(player);
        if (botName == null) continue;

        // Check for falling
        if (player.IsInMidAir && player.GetLinearVelocity().Y < -5)
        {
            bool canFireFallingCry = !playerLastFallingCryTime.ContainsKey(player.UniqueID) || 
                                      (currentTime - playerLastFallingCryTime[player.UniqueID] >= FALLING_CRY_COOLDOWN);
            
            if (canFireFallingCry)
            {
                playerLastFallingCryTime[player.UniqueID] = currentTime;
                
                if (botName == "Kokola")
                {
                    Crie("Kokola", "falling");
                }
                else if (botName == "Bichi")
                {
                    Crie("Bichi", "falling");
                }
                else if (botName == "Xray")
                {
                    Crie("Xray", "falling");
                }
            }
        }

        // Check for strength boost
        bool hasStrengthBoost = player.GetStrengthBoostTime() > 0;
        bool wasStrengthBoosted = playerStrengthBoosted.ContainsKey(player.UniqueID) && playerStrengthBoosted[player.UniqueID];
        
        if (hasStrengthBoost && !wasStrengthBoosted)
        {
            playerStrengthBoosted[player.UniqueID] = true;
            
            if (botName == "Timpa")
            {
                Crie("Timpa", "strength_boost");
            }
            else if (botName == "Xray")
            {
                Crie("Xray", "strength_boost");
            }
            else if (botName == "Pakhi")
            {
                Crie("Pakhi", "strength_boost");
            }
        }
        else if (!hasStrengthBoost && wasStrengthBoosted)
        {
            playerStrengthBoosted[player.UniqueID] = false;
        }

        // Check for out of ammo (Bichi)
        if (botName == "Bichi")
        {
            if (!playerLastAmmoCheckTime.ContainsKey(player.UniqueID) || 
                currentTime - playerLastAmmoCheckTime[player.UniqueID] >= AMMO_CHECK_INTERVAL)
            {
                playerLastAmmoCheckTime[player.UniqueID] = currentTime;
                
                WeaponItemType currentWeapon = player.CurrentWeaponDrawn;
                if (currentWeapon == WeaponItemType.Rifle || 
                    currentWeapon == WeaponItemType.Handgun ||
                    currentWeapon == WeaponItemType.Thrown)
                {
                    // Check if weapon needs reload (this is approximate)
                    PlayerModifiers mods = player.GetModifiers();
                    if (mods.CurrentEnergy < 10) // Low energy might indicate need to reload
                    {
                        Crie("Bichi", "out_of_ammo");
                    }
                }
            }
        }

        // Check for grabbed state
        bool isGrabbed = player.IsCaughtByPlayerInGrab;
        bool wasGrabbed = playerWasGrabbed.ContainsKey(player.UniqueID) && playerWasGrabbed[player.UniqueID];
        
        if (isGrabbed && !wasGrabbed && botName == "Bichi")
        {
            playerWasGrabbed[player.UniqueID] = true;
            Crie("Bichi", "grabbed");
        }
        else if (!isGrabbed && wasGrabbed)
        {
            playerWasGrabbed[player.UniqueID] = false;
        }

        // Check for being caught in dive
        bool isCaughtInDive = player.IsCaughtByPlayerInDive;
        bool wasCaughtInDive = playerWasDriven.ContainsKey(player.UniqueID) && playerWasDriven[player.UniqueID];
        
        if (isCaughtInDive && !wasCaughtInDive && botName == "Pakhi")
        {
            playerWasDriven[player.UniqueID] = true;
            Crie("Pakhi", "dive_caught");
        }
        else if (!isCaughtInDive && wasCaughtInDive)
        {
            playerWasDriven[player.UniqueID] = false;
        }

        // Check for last alive vs multiple opponents
        if (botName == "Bichi" || botName == "Pakhi" || botName == "Kokola" || botName == "Timpa")
        {
            CheckLastAliveVsMultiple(player, botName);
        }

        // Check for weapon pickups
        if (botName == "Edur" || botName == "Bichi" || botName == "Xray")
        {
            CheckWeaponPickup(player, botName);
        }
        
        // Check for nearby enemies with strength boost (Kokola and Pakhi)
        if (botName == "Kokola" || botName == "Pakhi")
        {
            CheckNearbyEnemyStrengthBoost(player, botName);
        }

        // Clear fire status when no longer on fire
        PlayerModifiers playerMods = player.GetModifiers();
        if (playerOnFire.ContainsKey(player.UniqueID) && playerOnFire[player.UniqueID])
        {
            // Check if player is still burning (approximate check)
            if (playerMods.CurrentHealth > 0)
            {
                // Fire status will be reset when player stops taking fire damage
                playerOnFire[player.UniqueID] = false;
            }
        }
    }
}

private void CheckLastAliveVsMultiple(IPlayer player, string botName)
{
    if (player.IsDead) return;
    
    // Check if event already fired for this player
    if (playerLastAliveEventFired.ContainsKey(player.UniqueID) && playerLastAliveEventFired[player.UniqueID])
    {
        return;
    }
    
    PlayerTeam playerTeam = player.GetTeam();
    int aliveTeammates = 0;
    int aliveEnemies = 0;
    
    IPlayer[] allPlayers = Game.GetPlayers();
    foreach (IPlayer p in allPlayers)
    {
        if (p.IsDead) continue;
        
        if (p.GetTeam() == playerTeam && p.UniqueID != player.UniqueID)
        {
            aliveTeammates++;
        }
        else if (p.GetTeam() != playerTeam && p.GetTeam() != PlayerTeam.Independent)
        {
            aliveEnemies++;
        }
    }
    
    // Last alive on team vs 2+ enemies
    if (aliveTeammates == 0 && aliveEnemies >= 2)
    {
        playerLastAliveEventFired[player.UniqueID] = true;
        Crie(botName, "last_alive_vs_multiple");
    }
}

private void CheckWeaponPickup(IPlayer player, string botName)
{
    if (player.IsDead) return;
    
    WeaponItemType currentWeapon = player.CurrentWeaponDrawn;
    float currentTime = Game.TotalElapsedGameTime;
    
    // List of special weapons to track
    WeaponItemType[] specialWeapons = {
        WeaponItemType.Rifle,
        WeaponItemType.Thrown,
        WeaponItemType.Powerup
    };
    
    // Check if current weapon is a special weapon
    bool isSpecialWeapon = false;
    foreach (WeaponItemType weaponType in specialWeapons)
    {
        if (currentWeapon == weaponType)
        {
            isSpecialWeapon = true;
            break;
        }
    }
    
    if (!isSpecialWeapon) return;
    
    // Check if this is a different weapon than last time
    WeaponItemType lastWeapon = playerLastWeapon.ContainsKey(player.UniqueID) ? playerLastWeapon[player.UniqueID] : WeaponItemType.NONE;
    
    // Check cooldown
    bool canFireCry = !playerLastWeaponCryTime.ContainsKey(player.UniqueID) || 
                      (currentTime - playerLastWeaponCryTime[player.UniqueID] >= WEAPON_CRY_COOLDOWN);
    
    // Fire event if weapon changed and cooldown passed
    if (currentWeapon != lastWeapon && canFireCry)
    {
        playerLastWeapon[player.UniqueID] = currentWeapon;
        playerLastWeaponCryTime[player.UniqueID] = currentTime;
        Crie(botName, "gets_weapon");
    }
    else if (currentWeapon != lastWeapon)
    {
        // Update last weapon even if cooldown hasn't passed
        playerLastWeapon[player.UniqueID] = currentWeapon;
    }
}

private void CheckNearbyEnemyStrengthBoost(IPlayer player, string botName)
{
    if (player.IsDead) return;
    
    Vector2 playerPosition = player.GetWorldPosition();
    PlayerTeam playerTeam = player.GetTeam();
    const float STRENGTH_BOOST_CHECK_RADIUS = 50f;
    
    // Check all players for nearby enemies with strength boost
    IPlayer[] allPlayers = Game.GetPlayers();
    foreach (IPlayer otherPlayer in allPlayers)
    {
        if (otherPlayer.IsDead || otherPlayer.UniqueID == player.UniqueID) continue;
        
        // Check if other player is on a different team
        if (otherPlayer.GetTeam() != playerTeam && otherPlayer.GetTeam() != PlayerTeam.Independent)
        {
            // Check if other player has strength boost
            if (otherPlayer.GetStrengthBoostTime() > 0)
            {
                // Check distance
                Vector2 otherPosition = otherPlayer.GetWorldPosition();
                float distance = Vector2.Distance(playerPosition, otherPosition);
                
                if (distance <= STRENGTH_BOOST_CHECK_RADIUS)
                {
                    Crie(botName, "opponent_strength_boost");
                    return; // Fire once per update cycle
                }
            }
        }
    }
}

private string GetBotName(IPlayer player)
{
    if (player == null || !player.IsBot) return null;
    
    if (timpaBot != null && player.UniqueID == timpaBot.UniqueID) return "Timpa";
    if (bichiBot != null && player.UniqueID == bichiBot.UniqueID) return "Bichi";
    if (kokolaBot != null && player.UniqueID == kokolaBot.UniqueID) return "Kokola";
    if (edurBot != null && player.UniqueID == edurBot.UniqueID) return "Edur";
    if (xrayBot != null && player.UniqueID == xrayBot.UniqueID) return "Xray";
    if (pakhiBot != null && player.UniqueID == pakhiBot.UniqueID) return "Pakhi";
    if (psythicBot != null && player.UniqueID == psythicBot.UniqueID) return "Psythic";
    
    return null;
}

private string GetPlayerName(IPlayer player)
{
    if (player == null) return null;
    
    // Check bots
    if (player.IsBot)
    {
        if (timpaBot != null && player.UniqueID == timpaBot.UniqueID) return "Timpa";
        if (bichiBot != null && player.UniqueID == bichiBot.UniqueID) return "Bichi";
        if (kokolaBot != null && player.UniqueID == kokolaBot.UniqueID) return "Kokola";
        if (edurBot != null && player.UniqueID == edurBot.UniqueID) return "Edur";
        if (xrayBot != null && player.UniqueID == xrayBot.UniqueID) return "Xray";
        if (pakhiBot != null && player.UniqueID == pakhiBot.UniqueID) return "Pakhi";
        if (psythicBot != null && player.UniqueID == psythicBot.UniqueID) return "Psythic";
    }
    else
    {
        // Check non-bot players
        if (bondukPlayer != null && player.UniqueID == bondukPlayer.UniqueID) return "Bonduk";
        if (hateliPlayer != null && player.UniqueID == hateliPlayer.UniqueID) return "Hateli";
    }
    
    return null;
}

// Placeholder method - will be implemented later with actual sound playback
private void Crie(string botName, string tag)
{
    // TODO: Implement actual sound effect playback
    // For now, just log the event
    Game.ShowChatMessage(botName + " - " + tag);
}
