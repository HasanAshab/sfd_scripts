// Titan Transformation Script
// When a player picks up strength boost, they transform into a Titan with enhanced abilities

// Configuration variables
private const float TRANSFORM_EXPLOSION_RADIUS = 30f;
private const float TITAN_HP_BONUS = 0.15f; // 15% HP increase
private const float TITAN_MELEE_DAMAGE_BONUS = 0.10f; // 10% melee damage increase
private const float FALL_IMPACT_RADIUS = 15f;

// Track titan players
private Dictionary<int, bool> titanPlayers = new Dictionary<int, bool>();
private Dictionary<int, IProfile> originalProfiles = new Dictionary<int, IProfile>();
private Dictionary<int, PlayerModifiers> originalModifiers = new Dictionary<int, PlayerModifiers>();

// Track player ground state for fall detection
private Dictionary<int, bool> playerWasOnGround = new Dictionary<int, bool>();

// Track player strength boost state to detect when they pick it up
private Dictionary<int, bool> playerHadStrengthBoost = new Dictionary<int, bool>();

// Track titans that are exhausted (out of energy)
private Dictionary<int, bool> titanExhausted = new Dictionary<int, bool>();

public void OnStartup()
{
    // Set up update callback to detect strength boost pickups and falls
    Events.UpdateCallback.Start(OnUpdate, 50); // Check every 50ms
}

private void TransformToTitan(IPlayer player)
{
    int playerId = player.UniqueID;
    
    // Don't transform if already a titan
    if (titanPlayers.ContainsKey(playerId) && titanPlayers[playerId])
    {
        return;
    }
    
    Vector2 playerPos = player.GetWorldPosition();
    
    // 1. Explosion effect + sound at player position
    Game.PlayEffect(EffectName.Electric, playerPos);
    Game.PlayEffect(EffectName.CameraShaker, playerPos);
    Game.PlaySound("StrengthBoostStart", playerPos);
    
    // Make nearby non-titan players fall
    MakeNearbyPlayersFall(playerPos, TRANSFORM_EXPLOSION_RADIUS, playerId);
    
    // 2. Remove strength boost powerup
    player.RemoveWeaponItemType(WeaponItemType.Powerup);
    
    // Store original profile and modifiers
    originalProfiles[playerId] = player.GetProfile();
    originalModifiers[playerId] = player.GetModifiers();
    
    // 3. Set titan profile
    IProfile titanProfile = new IProfile()
    {
        Gender = player.GetProfile().Gender,
        Skin = new IProfileClothingItem(
            player.GetProfile().Gender == Gender.Female ? "Normal_fem" : "Normal",
            "Skin4",
            "ClothingLightPink"
        )
    };
    player.SetProfile(titanProfile);
    
    // 4. Increase HP and melee damage
    PlayerModifiers mods = player.GetModifiers();
    
    // Increase max health by 15%
    int newMaxHealth = (int)(mods.MaxHealth * (1f + TITAN_HP_BONUS));
    int healthIncrease = newMaxHealth - mods.MaxHealth;
    mods.MaxHealth = newMaxHealth;
    mods.CurrentHealth = mods.CurrentHealth + healthIncrease; // Add the health increase
    
    // Increase melee damage by 10%
    mods.MeleeDamageDealtModifier *= (1f + TITAN_MELEE_DAMAGE_BONUS);
    
    // 5. Disable item pickup
    mods.ItemDropMode = 1; // Cannot pick up items
    
    player.SetModifiers(mods);
    
    // Mark as titan
    titanPlayers[playerId] = true;
    
    // Show transformation message
    Game.ShowChatMessage(player.Name + " has transformed into a TITAN!", Color.Red);
}

private void MakeNearbyPlayersFall(Vector2 explosionPos, float radius, int titanPlayerId)
{
    IPlayer[] allPlayers = Game.GetPlayers();
    
    foreach (IPlayer player in allPlayers)
    {
        if (player == null || player.IsDead) continue;
        if (player.UniqueID == titanPlayerId) continue;
        
        // Skip if already a titan
        if (titanPlayers.ContainsKey(player.UniqueID) && titanPlayers[player.UniqueID])
        {
            continue;
        }
        
        Vector2 playerPos = player.GetWorldPosition();
        float distance = Vector2.Distance(explosionPos, playerPos);
        
        if (distance <= radius)
        {
            // Make player fall
            QuickCommand(player, new PlayerCommand(PlayerCommandType.Fall));
        }
    }
}

public void OnUpdate(float elapsed)
{
    IPlayer[] allPlayers = Game.GetPlayers();
    
    foreach (IPlayer player in allPlayers)
    {
        if (player == null || player.IsDead) continue;
        
        int playerId = player.UniqueID;
        
        // Initialize tracking
        if (!playerWasOnGround.ContainsKey(playerId))
        {
            playerWasOnGround[playerId] = player.IsOnGround;
        }
        
        if (!playerHadStrengthBoost.ContainsKey(playerId))
        {
            playerHadStrengthBoost[playerId] = false;
        }
        
        // Check if player just picked up strength boost
        bool hasStrengthBoost = player.GetStrengthBoostTime() > 0;
        bool hadStrengthBoost = playerHadStrengthBoost[playerId];
        
        if (hasStrengthBoost && !hadStrengthBoost)
        {
            // Player just picked up strength boost - transform!
            TransformToTitan(player);
        }
        
        playerHadStrengthBoost[playerId] = hasStrengthBoost;
        
        // Check titan energy exhaustion
        if (titanPlayers.ContainsKey(playerId) && titanPlayers[playerId])
        {
            CheckTitanExhaustion(player, playerId);
        }
        
        // Check if titan and just landed
        if (titanPlayers.ContainsKey(playerId) && titanPlayers[playerId])
        {
            bool wasOnGround = playerWasOnGround[playerId];
            bool isOnGround = player.IsOnGround;
            
            // Detect landing (was in air, now on ground)
            if (!wasOnGround && isOnGround)
            {
                OnTitanLanded(player);
            }
            
            playerWasOnGround[playerId] = isOnGround;
        }
        else
        {
            playerWasOnGround[playerId] = player.IsOnGround;
        }
    }
}

private void OnTitanLanded(IPlayer titan)
{
    Vector2 titanPos = titan.GetWorldPosition();
    
    // Camera shake at landing position
    Game.PlayEffect(EffectName.CameraShaker, titanPos);
    Game.PlaySound("Explosion", titanPos);
    
    // Make nearby non-titan players fall
    MakeNearbyPlayersFall(titanPos, FALL_IMPACT_RADIUS, titan.UniqueID);
}

private void CheckTitanExhaustion(IPlayer titan, int titanId)
{
    PlayerModifiers mods = titan.GetModifiers();
    
    // Initialize exhaustion state if not tracked
    if (!titanExhausted.ContainsKey(titanId))
    {
        titanExhausted[titanId] = false;
    }
    
    bool isExhausted = titanExhausted[titanId];
    bool hasNoEnergy = mods.CurrentEnergy <= 0;
    bool hasMaxEnergy = mods.CurrentEnergy >= mods.MaxEnergy;
    
    // Titan just ran out of energy
    if (hasNoEnergy && !isExhausted)
    {
        // Disable input and make titan kneel
        titan.SetInputEnabled(false);
        QuickCommand(titan, new PlayerCommand(PlayerCommandType.DeathKneelInfinite));
        titanExhausted[titanId] = true;
    }
    // Titan has recovered to full energy
    else if (isExhausted && hasMaxEnergy)
    {
        // Stop kneeling and re-enable input
        titan.AddCommand(new PlayerCommand(PlayerCommandType.StopDeathKneel));
        titan.SetInputEnabled(true);
        titanExhausted[titanId] = false;
    }
}

private void QuickCommand(IPlayer player, PlayerCommand command)
{
    bool inputEnabled = player.IsInputEnabled;

    player.AddCommand(command);

    if (inputEnabled)
    {
        player.SetInputEnabled(false);

        Game.Events.StartUpdateCallback(dlt =>
        {
            player?.SetInputEnabled(true);
        }, 1, 1);
    }
}