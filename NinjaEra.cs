// NinjaEra - Ninja-themed gameplay with shurikens, bows, knives, and katanas

public void OnStartup()
{
    // Enable infinite ammo using console command
    Game.RunCommand("infinite_ammo 1");
    
    // Remove all existing weapon spawn areas to stop item spawning
    IObject[] weaponSpawns = Game.GetObjects("WpnSpawnTrigger");
    foreach (IObject spawn in weaponSpawns)
    {
        spawn.Remove();
    }
    
    // Remove supply crates and other item spawners
    IObject[] supplyCrates = Game.GetObjects("SupplyCrate00");
    foreach (IObject crate in supplyCrates)
    {
        crate.Remove();
    }
    
    // Equip all players with ninja weapons
    foreach (IPlayer player in Game.GetPlayers())
    {
        EquipNinjaWeapons(player);
    }
}

public void OnPlayerSpawned(IPlayer player)
{
    // Equip ninja weapons when player spawns
    EquipNinjaWeapons(player);
}

private void EquipNinjaWeapons(IPlayer player)
{
    // Remove existing weapons by type
    player.RemoveWeaponItemType(WeaponItemType.Rifle);
    player.RemoveWeaponItemType(WeaponItemType.Handgun);
    player.RemoveWeaponItemType(WeaponItemType.Melee);
    player.RemoveWeaponItemType(WeaponItemType.Thrown);
    
    // Check if this player is the highest difficulty bot on their team
    bool isEliteBot = IsHighestDifficultyBot(player);
    
    // Give basic ninja weapons to everyone
    player.GiveWeaponItem(WeaponItem.SHURIKEN);
    player.GiveWeaponItem(WeaponItem.BOW);
    
    // Give katana to elite bots, knife to everyone else
    if (isEliteBot)
    {
        player.GiveWeaponItem(WeaponItem.KATANA);
    }
    else
    {
        player.GiveWeaponItem(WeaponItem.KNIFE);
    }
}

private bool IsHighestDifficultyBot(IPlayer player)
{
    // Only check bots
    if (!player.IsBot)
        return false;
    
    // Get all players on the same team
    IPlayer[] allPlayers = Game.GetPlayers();
    PlayerTeam playerTeam = player.GetTeam();
    
    // Get this player's bot behavior
    BotBehavior playerBehavior = player.GetBotBehavior();
    PredefinedAIType playerAI = playerBehavior.PredefinedAI;
    
    // Define elite bot types (higher difficulty bots)
    PredefinedAIType[] eliteBotTypes = {
        PredefinedAIType.Hulk,
        PredefinedAIType.Grunt,
        PredefinedAIType.BotD,
        PredefinedAIType.BotC,
        PredefinedAIType.Meatgrinder
    };

    // Check if this player is an elite bot type
    bool isEliteBot = false;
    foreach (PredefinedAIType eliteType in eliteBotTypes)
    {
        if (playerAI == eliteType)
        {
            isEliteBot = true;
            break;
        }
    }
    
    return isEliteBot;
}