// NinjaEra - Ninja-themed gameplay with shurikens, bows, knives, and katanas



public void OnStartup()
{
    SetupBots();

    IPlayer[] players = Game.GetPlayers();
    p1 = players.Length >= 1 ? players[0] : null;
    p2 = players.Length >= 2 ? players[1] : null;

    PlayerModifiers p1Mods = p1.GetModifiers();

    p1Mods.RunSpeedModifier = 1.15f;
    p1Mods.SprintSpeedModifier = 1.3f;

    p1Mods.MaxEnergy = (int)(p1Mods.MaxEnergy * 1.2f);
    p1Mods.CurrentEnergy = (int)(p1Mods.CurrentEnergy * 1.2f);
    p1Mods.EnergyRechargeModifier *= 1.2f;
    p1.SetModifiers(p1Mods);


    PlayerModifiers p2Mods = p2.GetModifiers();

    p2Mods.SizeModifier = 1.12f;
    p2Mods.RunSpeedModifier = 0.8f;
    p2Mods.SprintSpeedModifier = 0.95f;
    p2Mods.MeleeForceModifier *= 1.2f;
    p2Mods.MeleeDamageDealtModifier *= 1.4f;

    p2.SetModifiers(p2Mods);

    TransformPlayersToUjiri();
}

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
    // bool isEliteBot = IsHighestDifficultyBot(player);
    
    // Give basic ninja weapons to everyone
    player.GiveWeaponItem(WeaponItem.SHURIKEN);
    player.GiveWeaponItem(WeaponItem.BOW);
    player.GiveWeaponItem(WeaponItem.KNIFE);

    

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