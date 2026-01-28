// Militia - Team-based loadout with guaranteed specialist item distribution

// Track specialist item assignments per team
private int team1AssignedCount = 0;
private int team2AssignedCount = 0;
private int team3AssignedCount = 0;
private int team4AssignedCount = 0;

// Track which items have been assigned per team (for guaranteed coverage)
private bool[] team1Items = new bool[4]; // [Pistol45, Knife, Sniper, SMG]
private bool[] team2Items = new bool[4];
private bool[] team3Items = new bool[4];
private bool[] team4Items = new bool[4];

public void OnStartup()
{
    // Reset specialist tracking
    ResetSpecialistTracking();
    
    // Equip all players with militia loadout
    foreach (IPlayer player in Game.GetPlayers())
    {
        EquipMilitiaLoadout(player);
    }
}

public void OnPlayerSpawned(IPlayer player)
{
    // Equip militia loadout when player spawns
    EquipMilitiaLoadout(player);
}

private void ResetSpecialistTracking()
{
    team1AssignedCount = 0;
    team2AssignedCount = 0;
    team3AssignedCount = 0;
    team4AssignedCount = 0;
    
    // Reset item tracking arrays
    for (int i = 0; i < 4; i++)
    {
        team1Items[i] = false;
        team2Items[i] = false;
        team3Items[i] = false;
        team4Items[i] = false;
    }
}

private void EquipMilitiaLoadout(IPlayer player)
{
    // Remove existing weapons
    player.RemoveWeaponItemType(WeaponItemType.Rifle);
    player.RemoveWeaponItemType(WeaponItemType.Handgun);
    player.RemoveWeaponItemType(WeaponItemType.Melee);
    player.RemoveWeaponItemType(WeaponItemType.Thrown);
    
    // Everyone gets grenades as general equipment
    player.GiveWeaponItem(WeaponItem.GRENADES);
    
    // Everyone gets a specialist item
    AssignSpecialistItem(player);
    
    // Assign random profile to bots
    if (player.IsBot)
    {
        AssignRandomProfile(player);
    }
}

private void AssignSpecialistItem(IPlayer player)
{
    PlayerTeam team = player.GetTeam();
    
    // Get team info
    int teamSize = GetTeamSize(team);
    int assignedCount = GetTeamAssignedCount(team);
    bool[] teamItems = GetTeamItemsArray(team);
    
    int itemToAssign = -1;
    
    // Create a more variable seed that changes between matches
    // Using game elapsed time and player position for more randomness
    Vector2 playerPos = player.GetWorldPosition();
    int variableSeed = (int)(Game.TotalElapsedGameTime * 1000) + 
                      (int)(playerPos.X * 100) + 
                      (int)(playerPos.Y * 100) + 
                      player.UniqueID;
    
    if (teamSize >= 4)
    {
        // Teams with 4+ members: guarantee all 4 items are covered first
        if (assignedCount < 4)
        {
            // Randomly find an unassigned item for guaranteed coverage
            int[] availableItems = new int[4];
            int availableCount = 0;
            
            // Build list of available items
            for (int i = 0; i < 4; i++)
            {
                if (!teamItems[i])
                {
                    availableItems[availableCount] = i;
                    availableCount++;
                }
            }
            
            // Randomly select from available items using variable seed
            if (availableCount > 0)
            {
                int randomIndex = (variableSeed * 17 + assignedCount * 23) % availableCount;
                if (randomIndex < 0) randomIndex = -randomIndex; // Handle negative modulo
                itemToAssign = availableItems[randomIndex];
            }
        }
        else
        {
            // After all 4 items are covered, assign randomly (can overlap)
            itemToAssign = (variableSeed * 13) % 4;
            if (itemToAssign < 0) itemToAssign = -itemToAssign;
        }
    }
    else
    {
        // Teams with 3 or fewer members: randomly assign from available items
        if (assignedCount < teamSize)
        {
            // Build list of available items
            int[] availableItems = new int[4];
            int availableCount = 0;
            
            for (int i = 0; i < 4; i++)
            {
                if (!teamItems[i])
                {
                    availableItems[availableCount] = i;
                    availableCount++;
                }
            }
            
            // Randomly select from available items using variable seed
            if (availableCount > 0)
            {
                int randomIndex = (variableSeed * 19 + assignedCount * 31) % availableCount;
                if (randomIndex < 0) randomIndex = -randomIndex; // Handle negative modulo
                itemToAssign = availableItems[randomIndex];
            }
        }
    }
    
    // Assign the specialist item
    if (itemToAssign >= 0)
    {
        GiveSpecialistItem(player, itemToAssign);
        MarkItemAssigned(team, itemToAssign);
        IncrementTeamAssignedCount(team);
    }
}

private void GiveSpecialistItem(IPlayer player, int itemIndex)
{
    switch (itemIndex)
    {
        case 0:
            player.GiveWeaponItem(WeaponItem.PISTOL45);
            break;
        case 1:
            player.GiveWeaponItem(WeaponItem.KNIFE);
            break;
        case 2:
            player.GiveWeaponItem(WeaponItem.SNIPER);
            break;
        case 3:
            player.GiveWeaponItem(WeaponItem.SMG);
            break;
    }
}

private int GetTeamSize(PlayerTeam team)
{
    int count = 0;
    IPlayer[] allPlayers = Game.GetPlayers();
    foreach (IPlayer p in allPlayers)
    {
        if (p.GetTeam() == team)
            count++;
    }
    return count;
}

private int GetTeamAssignedCount(PlayerTeam team)
{
    switch (team)
    {
        case PlayerTeam.Team1: return team1AssignedCount;
        case PlayerTeam.Team2: return team2AssignedCount;
        case PlayerTeam.Team3: return team3AssignedCount;
        case PlayerTeam.Team4: return team4AssignedCount;
        default: return 0;
    }
}

private bool[] GetTeamItemsArray(PlayerTeam team)
{
    switch (team)
    {
        case PlayerTeam.Team1: return team1Items;
        case PlayerTeam.Team2: return team2Items;
        case PlayerTeam.Team3: return team3Items;
        case PlayerTeam.Team4: return team4Items;
        default: return new bool[4];
    }
}

private void MarkItemAssigned(PlayerTeam team, int itemIndex)
{
    switch (team)
    {
        case PlayerTeam.Team1: team1Items[itemIndex] = true; break;
        case PlayerTeam.Team2: team2Items[itemIndex] = true; break;
        case PlayerTeam.Team3: team3Items[itemIndex] = true; break;
        case PlayerTeam.Team4: team4Items[itemIndex] = true; break;
    }
}

private void IncrementTeamAssignedCount(PlayerTeam team)
{
    switch (team)
    {
        case PlayerTeam.Team1: team1AssignedCount++; break;
        case PlayerTeam.Team2: team2AssignedCount++; break;
        case PlayerTeam.Team3: team3AssignedCount++; break;
        case PlayerTeam.Team4: team4AssignedCount++; break;
    }
}

private void AssignRandomProfile(IPlayer player)
{
    PlayerTeam team = player.GetTeam();
    
    // Create variable seed for profile randomization
    Vector2 playerPos = player.GetWorldPosition();
    int profileSeed = (int)(Game.TotalElapsedGameTime * 500) + 
                     (int)(playerPos.X * 50) + 
                     (int)(playerPos.Y * 50) + 
                     player.UniqueID * 7;
    
    // Get team-specific profiles
    IProfile[] profiles = GetTeamProfiles(team);
    
    // Randomly select a profile
    int profileIndex = profileSeed % profiles.Length;
    if (profileIndex < 0) profileIndex = -profileIndex;
    
    // Apply the profile
    player.SetProfile(profiles[profileIndex]);
}

private IProfile[] GetTeamProfiles(PlayerTeam team)
{
    if (team == PlayerTeam.Team1)
    {
        // Team 1 profiles with DarkGray primary color
        return new IProfile[]
        {
            new IProfile()
            {
                Name = "Soldier",
                Gender = Gender.Male,
                Skin = new IProfileClothingItem("Tattoos", "Skin4", "ClothingLightYellow"),
                Head = new IProfileClothingItem("Helmet", "ClothingDarkGray"),
                ChestUnder = new IProfileClothingItem("MilitaryShirt", "ClothingDarkGray", "ClothingLightBlue"),
                Waist = new IProfileClothingItem("SatchelBelt", "ClothingDarkGray"),
                Legs = new IProfileClothingItem("CamoPants", "ClothingDarkGray", "ClothingDarkGray"),
                Feet = new IProfileClothingItem("BootsBlack", "ClothingDarkGray"),
            },
            new IProfile()
            {
                Name = "Sniper",
                Gender = Gender.Male,
                Skin = new IProfileClothingItem("Normal", "Skin2", "ClothingLightGray"),
                ChestOver = new IProfileClothingItem("AmmoBelt", "ClothingDarkGray"),
                ChestUnder = new IProfileClothingItem("TShirt", "ClothingDarkGray"),
                Hands = new IProfileClothingItem("Gloves", "ClothingGray"),
                Waist = new IProfileClothingItem("AmmoBeltWaist", "ClothingGray"),
                Legs = new IProfileClothingItem("CamoPants", "ClothingDarkGreen", "ClothingDarkGray"),
                Feet = new IProfileClothingItem("BootsBlack", "ClothingDarkGray"),
                Accesory = new IProfileClothingItem("Vizor", "ClothingDarkGray", "ClothingLightRed"),
            },
            new IProfile()
            {
                Name = "Sniper",
                Gender = Gender.Male,
                Skin = new IProfileClothingItem("Normal", "Skin1", "ClothingLightGray"),
                ChestOver = new IProfileClothingItem("AmmoBelt", "ClothingDarkGray"),
                ChestUnder = new IProfileClothingItem("TShirt", "ClothingDarkGray"),
                Hands = new IProfileClothingItem("Gloves", "ClothingGray"),
                Waist = new IProfileClothingItem("AmmoBeltWaist", "ClothingGray"),
                Legs = new IProfileClothingItem("CamoPants", "ClothingDarkGreen", "ClothingDarkGray"),
                Feet = new IProfileClothingItem("BootsBlack", "ClothingDarkGray"),
                Accesory = new IProfileClothingItem("Vizor", "ClothingDarkGray", "ClothingLightRed"),
            }
        };
    }
    else if (team == PlayerTeam.Team2)
    {
        // Team 2 profiles with DarkYellow primary color
        return new IProfile[]
        {
            new IProfile()
            {
                Name = "Soldier",
                Gender = Gender.Male,
                Skin = new IProfileClothingItem("Tattoos", "Skin1", "ClothingLightYellow"),
                Head = new IProfileClothingItem("Helmet", "ClothingDarkYellow"),
                ChestUnder = new IProfileClothingItem("MilitaryShirt", "ClothingDarkYellow", "ClothingLightBlue"),
                Waist = new IProfileClothingItem("SatchelBelt", "ClothingDarkYellow"),
                Legs = new IProfileClothingItem("CamoPants", "ClothingDarkYellow", "ClothingDarkYellow"),
                Feet = new IProfileClothingItem("BootsBlack", "ClothingGray"),
            },
            new IProfile()
            {
                Name = "Soldier",
                Gender = Gender.Female,
                Skin = new IProfileClothingItem("Tattoos_fem", "Skin4", "ClothingLightYellow"),
                Head = new IProfileClothingItem("Helmet", "ClothingDarkYellow"),
                ChestUnder = new IProfileClothingItem("MilitaryShirt_fem", "ClothingDarkYellow", "ClothingLightBlue"),
                Waist = new IProfileClothingItem("SatchelBelt_fem", "ClothingDarkYellow"),
                Legs = new IProfileClothingItem("CamoPants_fem", "ClothingDarkYellow", "ClothingDarkYellow"),
                Feet = new IProfileClothingItem("BootsBlack", "ClothingGray"),
            },
            new IProfile()
            {
                Name = "Sniper",
                Gender = Gender.Male,
                Skin = new IProfileClothingItem("Normal", "Skin2", "ClothingLightGray"),
                ChestOver = new IProfileClothingItem("AmmoBelt", "ClothingDarkYellow"),
                ChestUnder = new IProfileClothingItem("TShirt", "ClothingDarkYellow"),
                Hands = new IProfileClothingItem("Gloves", "ClothingDarkGray"),
                Waist = new IProfileClothingItem("AmmoBeltWaist", "ClothingGray"),
                Legs = new IProfileClothingItem("CamoPants", "ClothingDarkGreen", "ClothingDarkYellow"),
                Feet = new IProfileClothingItem("BootsBlack", "ClothingDarkGray"),
                Accesory = new IProfileClothingItem("Vizor", "ClothingDarkGray", "ClothingLightRed"),
            }
        };
    }
    else
    {
        // Default profiles for Team 3 and 4 (using Team 1 style)
        return new IProfile[]
        {
            new IProfile()
            {
                Name = "Soldier",
                Gender = Gender.Male,
                Skin = new IProfileClothingItem("Tattoos", "Skin4", "ClothingLightYellow"),
                Head = new IProfileClothingItem("Helmet", "ClothingDarkGray"),
                ChestUnder = new IProfileClothingItem("MilitaryShirt", "ClothingDarkGray", "ClothingLightBlue"),
                Waist = new IProfileClothingItem("SatchelBelt", "ClothingDarkGray"),
                Legs = new IProfileClothingItem("CamoPants", "ClothingDarkGray", "ClothingDarkGray"),
                Feet = new IProfileClothingItem("BootsBlack", "ClothingDarkGray"),
            }
        };
    }
}