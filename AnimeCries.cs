
// Bot references (must match Ujiri.cs bot names)
private IPlayer timpaBot = null;
private IPlayer bichiBot = null;
private IPlayer kokolaBot = null;
private IPlayer edurBot = null;
private IPlayer xrayBot = null;
private IPlayer pakhiBot = null;
private IPlayer psythicBot = null;

// Tracking dictionaries
private Dictionary<int, bool> playerOnFire = new Dictionary<int, bool>();
private Dictionary<int, bool> playerStrengthBoosted = new Dictionary<int, bool>();
private Dictionary<int, float> playerLastAmmoCheckTime = new Dictionary<int, float>();
private Dictionary<int, bool> playerWasGrabbed = new Dictionary<int, bool>();
private Dictionary<int, bool> playerWasDriven = new Dictionary<int, bool>();

// Constants
private const float AMMO_CHECK_INTERVAL = 1000; // Check ammo every 1 second

public void OnStartup()
{
    // Find bot references by name
    FindBotReferences();
    
    // Set up event callbacks
    Events.PlayerDamageCallback.Start(OnPlayerDamage);
    Events.PlayerDeathCallback.Start(OnPlayerDeath);
    Events.PlayerMeleeActionCallback.Start(OnPlayerMeleeAction);
    Events.UpdateCallback.Start(OnUpdate, 100); // Check every 100ms
    Events.ExplosionHitCallback.Start(OnExplosionHit);
    Events.ObjectCreatedCallback.Start(OnObjectCreated);
}

private void FindBotReferences()
{
    IPlayer[] allPlayers = Game.GetPlayers();
    foreach (IPlayer player in allPlayers)
    {
        if (!player.IsBot) continue;
        
        string botName = player.GetProfile().Name;
        switch (botName)
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
}

public void OnPlayerDamage(IPlayer player, PlayerDamageArgs args)
{
    if (player == null || player.IsDead) return;
    
    string botName = GetBotName(player);
    if (botName == null) return;

    // Check for friendly fire (hit by own team member)
    if (args.SourceID != 0)
    {
        IPlayer attacker = Game.GetPlayer(args.SourceID);
        if (attacker != null && attacker.GetTeam() == player.GetTeam() && attacker.UniqueID != player.UniqueID)
        {
            // Friendly fire detected
            if (botName == "Pakhi")
            {
                Crie("Pakhi", "friendly_fire");
            }
            else if (botName == "Timpa")
            {
                Crie("Timpa", "hit_by_teammate");
            }
        }
    }

    // Check for fire damage
    if (args.DamageType == PlayerDamageEventType.Fire)
    {
        if (!playerOnFire.ContainsKey(player.UniqueID) || !playerOnFire[player.UniqueID])
        {
            playerOnFire[player.UniqueID] = true;
            
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
    
    string botName = GetBotName(player);
    if (botName == null) return;

    // Check if player was gibbed
    if (args.Removed)
    {
        Crie(botName, "gibbed");
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

    // Check if Kokola is being targeted by other bots
    foreach (PlayerMeleeHitArg hitArg in args)
    {
        if (hitArg.IsPlayer && hitArg.HitObject != null)
        {
            IPlayer target = Game.GetPlayer(hitArg.HitObject.UniqueID);
            if (target != null)
            {
                string targetBotName = GetBotName(target);
                if (targetBotName == "Kokola" && player.IsBot && player.UniqueID != target.UniqueID)
                {
                    Crie("Kokola", "targeted_by_bot");
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
    // Check for weapon pickups by Edur
    foreach (IObject obj in objects)
    {
        if (obj is IObjectWeaponItem)
        {
            // This will be checked in OnUpdate when Edur picks up weapons
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
            
            // Check if opponent got strength boost (for Kokola and Pakhi)
            if (player.GetTeam() != PlayerTeam.Team1)
            {
                if (kokolaBot != null && !kokolaBot.IsDead)
                {
                    Crie("Kokola", "opponent_strength_boost");
                }
            }
            if (player.GetTeam() != PlayerTeam.Team2)
            {
                if (pakhiBot != null && !pakhiBot.IsDead)
                {
                    Crie("Pakhi", "opponent_strength_boost");
                }
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
        if (botName == "Bichi" || botName == "Pakhi")
        {
            CheckLastAliveVsMultiple(player, botName);
        }

        // Check for Edur getting listed weapons
        if (botName == "Edur")
        {
            CheckEdurWeaponPickup(player);
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
        Crie(botName, "last_alive_vs_multiple");
    }
}

private void CheckEdurWeaponPickup(IPlayer player)
{
    if (player.IsDead) return;
    
    WeaponItemType currentWeapon = player.CurrentWeaponDrawn;
    
    // List of special weapons Edur might pick up
    WeaponItemType[] specialWeapons = {
        WeaponItemType.Rifle,
        WeaponItemType.Thrown,
        WeaponItemType.Powerup
    };
    
    foreach (WeaponItemType weaponType in specialWeapons)
    {
        if (currentWeapon == weaponType)
        {
            Crie("Edur", "gets_weapon");
            break;
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

// Placeholder method - will be implemented later with actual sound playback
private void Crie(string botName, string tag)
{
    // TODO: Implement actual sound effect playback
    // For now, just log the event
    Game.WriteToConsole(botName + " - " + tag);
}
