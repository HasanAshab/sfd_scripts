// Militia - Team-based loadout with guaranteed specialist item distribution and continuous bot spawning

// Safe spawn system using path nodes
private static readonly Random RNG = new Random();

// Track spawned bots to exclude from winner calculation
private HashSet<int> spawnedRookieIds = new HashSet<int>();
private HashSet<int> spawnedCaptainIds = new HashSet<int>();

// Winner announcement tracking
private bool gameEnded = false;

// Track colonels for each team
private IPlayer team1Colonel = null;
private IPlayer team2Colonel = null;

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

public IObjectPathNode[] AvailablePathNodes
{
    get
    {
        return Game.GetObjects<IObjectPathNode>().Where(path => path.GetNodeEnabled() && !path.GetIsElevatorNode() &&
            (path.GetPathNodeType() == PathNodeType.Ground ||
             path.GetPathNodeType() == PathNodeType.Platform)).ToArray();
    }
}

public Vector2 RandomSpawnPos
{
    get
    {
        IObject[] spawnPlayers = Game.GetObjectsByName("SpawnPlayer");
        IObjectPathNode[] availablePathNodes = AvailablePathNodes;
        IEnumerable<IObject> spawns = spawnPlayers.Concat(availablePathNodes);
        return spawns.Any() ? spawns.ElementAt(RNG.Next(spawns.Count())).GetWorldPosition() : Vector2.Zero;
    }
}

public void OnStartup()
{
    // Reset specialist tracking
    ResetSpecialistTracking();
    
    // Set up timer triggers for bot spawning
    IObjectTimerTrigger rookieTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    rookieTimer.SetIntervalTime(6000); // 6 seconds
    rookieTimer.SetRepeatCount(0); // Infinite repeats
    rookieTimer.SetScriptMethod("SpawnRookies");
    rookieTimer.Trigger();
    
    IObjectTimerTrigger captainTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    captainTimer.SetIntervalTime(12000); // 12 seconds
    captainTimer.SetRepeatCount(0); // Infinite repeats
    captainTimer.SetScriptMethod("SpawnCaptains");
    captainTimer.Trigger();
    
    // Set up winner check timer
    IObjectTimerTrigger winnerTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    winnerTimer.SetIntervalTime(2000); // Check every 2 seconds
    winnerTimer.SetRepeatCount(0); // Infinite repeats
    winnerTimer.SetScriptMethod("CheckForWinner");
    winnerTimer.Trigger();
    
    // Set up colonel identification timer (after players are equipped)
    IObjectTimerTrigger colonelTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    colonelTimer.SetIntervalTime(1000); // 1 second delay to let players spawn
    colonelTimer.SetRepeatCount(1); // Run once
    colonelTimer.SetScriptMethod("IdentifyColonels");
    colonelTimer.Trigger();
    
    // Equip all players with militia loadout
    foreach (IPlayer player in Game.GetPlayers())
    {
        EquipMilitiaLoadout(player);
    }

    PrepareP1();
    PrepareP2();
}

public void PrepareP1()
{
    IPlayer p1 = Game.GetPlayers()[0];
    PlayerModifiers mods = p1.GetModifiers();
    mods.ProjectileDamageTakenModifier *= 1.2f;
    p1.SetModifiers(mods);
}

public void PrepareP2()
{
    IPlayer p2 = Game.GetPlayers()[1];
    PlayerModifiers mods = p2.GetModifiers();
    mods.MeleeDamageTakenModifier *= 1.2f;
    p2.SetModifiers(mods);
}

public void SpawnRookies(TriggerArgs args)
{
    if (!gameEnded)
    {
        // Only spawn if colonels are alive
        if (team1Colonel != null && !team1Colonel.IsDead)
        {
            SpawnRookie(PlayerTeam.Team1);
        }
        if (team2Colonel != null && !team2Colonel.IsDead)
        {
            SpawnRookie(PlayerTeam.Team2);
        }
    }
}

public void SpawnCaptains(TriggerArgs args)
{
    if (!gameEnded)
    {
        // Only spawn if colonels are alive
        if (team1Colonel != null && !team1Colonel.IsDead)
        {
            SpawnCaptain(PlayerTeam.Team1);
        }
        if (team2Colonel != null && !team2Colonel.IsDead)
        {
            SpawnCaptain(PlayerTeam.Team2);
        }
    }
}

public void CheckForWinner(TriggerArgs args)
{
    if (gameEnded) return;
    
    // Get all main players/bots (exclude spawned rookies and captains)
    IPlayer[] allPlayers = Game.GetPlayers();
    IPlayer[] mainPlayers = allPlayers.Where(p => !spawnedRookieIds.Contains(p.UniqueID) && 
                                                  !spawnedCaptainIds.Contains(p.UniqueID)).ToArray();
    
    // Count teams with alive main players
    bool team1HasAlive = false;
    bool team2HasAlive = false;
    bool team3HasAlive = false;
    bool team4HasAlive = false;
    
    foreach (IPlayer player in mainPlayers)
    {
        if (!player.IsDead)
        {
            switch (player.GetTeam())
            {
                case PlayerTeam.Team1: team1HasAlive = true; break;
                case PlayerTeam.Team2: team2HasAlive = true; break;
                case PlayerTeam.Team3: team3HasAlive = true; break;
                case PlayerTeam.Team4: team4HasAlive = true; break;
            }
        }
    }
    
    // Count how many teams have alive players
    int aliveTeamCount = 0;
    PlayerTeam winningTeam = PlayerTeam.Independent;
    
    if (team1HasAlive) { aliveTeamCount++; winningTeam = PlayerTeam.Team1; }
    if (team2HasAlive) { aliveTeamCount++; winningTeam = PlayerTeam.Team2; }
    if (team3HasAlive) { aliveTeamCount++; winningTeam = PlayerTeam.Team3; }
    if (team4HasAlive) { aliveTeamCount++; winningTeam = PlayerTeam.Team4; }
    
    // Announce winner if only one team remains
    if (aliveTeamCount <= 1)
    {
        gameEnded = true;
        
        if (aliveTeamCount == 1)
        {
            string teamName = GetTeamName(winningTeam);
            Game.ShowPopupMessage(teamName + " WINS!");
            Game.RunCommand("gameover");
        }
        else
        {
            // No teams left - draw
            Game.ShowPopupMessage("DRAW - No survivors!");
            Game.RunCommand("gameover");
        }
    }
}

public void IdentifyColonels(TriggerArgs args)
{
    // Find the highest AI behavior bot for each team to be the colonel
    IPlayer[] allPlayers = Game.GetPlayers();
    
    IPlayer bestTeam1Bot = null;
    IPlayer bestTeam2Bot = null;
    PredefinedAIType highestTeam1AI = PredefinedAIType.BotA;
    PredefinedAIType highestTeam2AI = PredefinedAIType.BotA;
    
    // Define AI hierarchy (higher index = better AI)
    PredefinedAIType[] aiHierarchy = {
        PredefinedAIType.BotA,
        PredefinedAIType.BotB,
        PredefinedAIType.BotC,
        PredefinedAIType.BotD,
        PredefinedAIType.Grunt,
        PredefinedAIType.Hulk,
        PredefinedAIType.Meatgrinder
    };
    
    foreach (IPlayer player in allPlayers)
    {
        if (player.IsBot && !spawnedRookieIds.Contains(player.UniqueID) && !spawnedCaptainIds.Contains(player.UniqueID))
        {
            BotBehavior behavior = player.GetBotBehavior();
            PredefinedAIType playerAI = behavior.PredefinedAI;
            
            // Get AI level (higher = better)
            int aiLevel = GetAILevel(playerAI, aiHierarchy);
            
            if (player.GetTeam() == PlayerTeam.Team1)
            {
                int currentLevel = GetAILevel(highestTeam1AI, aiHierarchy);
                if (aiLevel > currentLevel)
                {
                    highestTeam1AI = playerAI;
                    bestTeam1Bot = player;
                }
            }
            else if (player.GetTeam() == PlayerTeam.Team2)
            {
                int currentLevel = GetAILevel(highestTeam2AI, aiHierarchy);
                if (aiLevel > currentLevel)
                {
                    highestTeam2AI = playerAI;
                    bestTeam2Bot = player;
                }
            }
        }
    }
    
    // Assign colonels and set up guard relationships
    if (bestTeam1Bot != null)
    {
        team1Colonel = bestTeam1Bot;
        SetupColonel(team1Colonel, PlayerTeam.Team1);
        // SetupGuards(PlayerTeam.Team1);
    }
    
    if (bestTeam2Bot != null)
    {
        team2Colonel = bestTeam2Bot;
        SetupColonel(team2Colonel, PlayerTeam.Team2);
        // SetupGuards(PlayerTeam.Team2);
    }
}

private int GetAILevel(PredefinedAIType aiType, PredefinedAIType[] hierarchy)
{
    for (int i = 0; i < hierarchy.Length; i++)
    {
        if (hierarchy[i] == aiType)
            return i;
    }
    return 0; // Default to lowest level
}

private void SetupColonel(IPlayer colonel, PlayerTeam team)
{
    // Give colonel the General profile with team colors
    colonel.SetProfile(GetColonelProfile(team));
    
    // Show colonel announcement
    string teamName = GetTeamName(team);
}

private void SetupGuards(PlayerTeam team)
{
    IPlayer colonel = (team == PlayerTeam.Team1) ? team1Colonel : team2Colonel;
    if (colonel == null) return;
    
    // Set all main bots on the team to guard the colonel
    IPlayer[] allPlayers = Game.GetPlayers();
    foreach (IPlayer player in allPlayers)
    {
        if (player.IsBot && 
            player.GetTeam() == team && 
            player.UniqueID != colonel.UniqueID &&
            !spawnedRookieIds.Contains(player.UniqueID) && 
            !spawnedCaptainIds.Contains(player.UniqueID))
        {
            // Set this bot to guard the colonel
            player.SetGuardTarget(colonel);
        }
    }
}

private string GetTeamName(PlayerTeam team)
{
    switch (team)
    {
        case PlayerTeam.Team1: return "TEAM 1";
        case PlayerTeam.Team2: return "TEAM 2";
        case PlayerTeam.Team3: return "TEAM 3";
        case PlayerTeam.Team4: return "TEAM 4";
        default: return "UNKNOWN TEAM";
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

private void SpawnRookie(PlayerTeam team)
{
    // Get spawn position near colonel
    Vector2 spawnPos = GetColonelSpawnPosition(team);
    
    // Create rookie bot
    IPlayer rookie = Game.CreatePlayer(spawnPos);
    rookie.SetNametagVisible(false);
    rookie.SetStatusBarsVisible(false);
    if (rookie != null)
    {
        // Track this as a spawned rookie (exclude from winner calculation)
        spawnedRookieIds.Add(rookie.UniqueID);
        
        rookie.SetCameraSecondaryFocusMode(CameraFocusMode.Ignore);
        
        // Set team
        rookie.SetTeam(team);
        
        // Set as bot with easy behavior
        BotBehavior rookieBehavior = new BotBehavior(true, PredefinedAIType.BotA);
        rookie.SetBotBehavior(rookieBehavior);
        
        // Target enemy colonel
        // SetColonelTarget(rookie, team);

        // Guard Own Colonel
        SetColonelGuard(rookie, team);
        
        // Set very low HP (1 hit life) and very low damage
        PlayerModifiers rookieModifiers = new PlayerModifiers();
        rookieModifiers.SizeModifier = 0.88f;
        rookieModifiers.MaxHealth = 5; // Very low health
        rookieModifiers.CurrentHealth = 5;
        rookieModifiers.MeleeDamageDealtModifier = 0.1f; // Very low melee damage (10%)
        rookieModifiers.ProjectileDamageDealtModifier = 0.1f; // Very low projectile damage (10%)
        rookie.SetModifiers(rookieModifiers);
        
        // Give pistol weapon
        rookie.GiveWeaponItem(WeaponItem.PISTOL);
        rookie.GiveWeaponItem(WeaponItem.GRENADES);

        // Set rookie profile
        rookie.SetProfile(GetRookieProfile(team));
    }
}

private void SpawnCaptain(PlayerTeam team)
{
    // Get spawn position near colonel
    Vector2 spawnPos = GetColonelSpawnPosition(team);
    
    // Create captain bot
    IPlayer captain = Game.CreatePlayer(spawnPos);
    captain.SetNametagVisible(false);
    captain.SetStatusBarsVisible(false);
    if (captain != null)
    {
        // Track this as a spawned captain (exclude from winner calculation)
        spawnedCaptainIds.Add(captain.UniqueID);
        
        // Set team
        captain.SetTeam(team);
        
        // Set as bot with normal behavior
        BotBehavior captainBehavior = new BotBehavior(true, PredefinedAIType.BotB);
        captain.SetBotBehavior(captainBehavior);
        
        // Target enemy colonel
        // SetColonelTarget(captain, team);

        // Guard Own Colonel
        SetColonelGuard(captain, team);
        
        // Set moderate HP (3x rookie health) and very low damage
        PlayerModifiers captainModifiers = new PlayerModifiers();
        captainModifiers.MaxHealth = 15; // 3x rookie health
        captainModifiers.CurrentHealth = 15;
        captainModifiers.MeleeDamageDealtModifier = 0.3f; // Very low melee damage (30%)
        captainModifiers.ProjectileDamageDealtModifier = 0.3f; // Very low projectile damage (30%)
        captain.SetModifiers(captainModifiers);

        // Give pistol and knife weapons
        captain.GiveWeaponItem(WeaponItem.PISTOL);
        captain.GiveWeaponItem(WeaponItem.KNIFE);
        captain.GiveWeaponItem(WeaponItem.GRENADES);

        // Set captain profile
        captain.SetProfile(GetCaptainProfile(team));
    }
}

private Vector2 GetTeamSpawnPosition(PlayerTeam team)
{
    // Get available path nodes and spawn players
    IObject[] spawnPlayers = Game.GetObjectsByName("SpawnPlayer");
    IObjectPathNode[] availablePathNodes = AvailablePathNodes;
    IEnumerable<IObject> allSpawns = spawnPlayers.Concat(availablePathNodes);
    
    if (!allSpawns.Any())
    {
        // Fallback to center if no spawns available
        return Vector2.Zero;
    }
    
    if (team == PlayerTeam.Team1)
    {
        // Team1: rightest + topest position
        IObject rightestTopest = allSpawns
            .OrderByDescending(spawn => spawn.GetWorldPosition().X) // Rightest first
            .ThenBy(spawn => spawn.GetWorldPosition().Y)            // Then topest (lowest Y)
            .FirstOrDefault();
        
        return rightestTopest != null ? rightestTopest.GetWorldPosition() : Vector2.Zero;
    }
    else
    {
        // Team2: leftest + bottomest position
        IObject leftestBottomest = allSpawns
            .OrderBy(spawn => spawn.GetWorldPosition().X)           // Leftest first
            .ThenByDescending(spawn => spawn.GetWorldPosition().Y) // Then bottomest (highest Y)
            .FirstOrDefault();
        
        return leftestBottomest != null ? leftestBottomest.GetWorldPosition() : Vector2.Zero;
    }
}

private Vector2 GetColonelSpawnPosition(PlayerTeam team)
{
    IPlayer colonel = (team == PlayerTeam.Team1) ? team1Colonel : team2Colonel;
    
    if (colonel != null && !colonel.IsDead)
    {
        // Spawn at exact same position as colonel
        return colonel.GetWorldPosition();
    }
    else
    {
        // Fallback to team spawn position if colonel is dead/missing
        return GetTeamSpawnPosition(team);
    }
}

private void SetColonelTarget(IPlayer player, PlayerTeam team)
{
    // Target the enemy colonel
    IPlayer enemyColonel = null;
    
    if (team == PlayerTeam.Team1)
    {
        enemyColonel = team2Colonel;
    }
    else if (team == PlayerTeam.Team2)
    {
        enemyColonel = team1Colonel;
    }
    
    if (enemyColonel != null && !enemyColonel.IsDead)
    {
        player.SetForcedBotTarget(enemyColonel);
    }
}

private void SetColonelGuard(IPlayer player, PlayerTeam team)
{
    IPlayer ownColonel = null;
    
    if (team == PlayerTeam.Team1)
    {
        ownColonel = team1Colonel;
    }
    else if (team == PlayerTeam.Team2)
    {
        ownColonel = team2Colonel;
    }
    
    if (ownColonel != null && !ownColonel.IsDead)
    {
        player.SetGuardTarget(ownColonel);
    }
   
}

private IProfile GetRookieProfile(PlayerTeam team)
{
    string primeColor = (team == PlayerTeam.Team1) ? "ClothingDarkGray" : "ClothingDarkYellow";
    
    return new IProfile()
    {
        Name = "Rookie",
        Gender = Gender.Female,
        Skin = new IProfileClothingItem("Tattoos_fem", "Skin3", "ClothingLightYellow"),
        Head = new IProfileClothingItem("PithHelmet", primeColor, "ClothingLightGray"),
        ChestUnder = new IProfileClothingItem("ShirtWithBowtie_fem", primeColor, "ClothingLightGray"),
        Waist = new IProfileClothingItem("SatchelBelt_fem", primeColor),
        Legs = new IProfileClothingItem("Shorts_fem", primeColor),
        Feet = new IProfileClothingItem("BootsBlack", "ClothingGray"),
    };
}

private IProfile GetCaptainProfile(PlayerTeam team)
{
    string primeColor = (team == PlayerTeam.Team1) ? "ClothingDarkGray" : "ClothingDarkYellow";
    
    return new IProfile()
    {
        Name = "Captain",
        Gender = Gender.Male,
        Skin = new IProfileClothingItem("Tattoos", "Skin3", "ClothingLightYellow"),
        Head = new IProfileClothingItem("StylishHat", primeColor, "ClothingLightGray"),
        ChestUnder = new IProfileClothingItem("ShirtWithBowtie", primeColor, "ClothingLightGray"),
        Waist = new IProfileClothingItem("SatchelBelt", primeColor),
        Legs = new IProfileClothingItem("Pants", primeColor),
        Feet = new IProfileClothingItem("BootsBlack", "ClothingGray"),
    };
}

private IProfile GetColonelProfile(PlayerTeam team)
{
    string primeColor = (team == PlayerTeam.Team1) ? "ClothingDarkGray" : "ClothingDarkYellow";
    return new IProfile()
    {
        Name = "General",
        Gender = Gender.Male,
        Skin = new IProfileClothingItem("Tattoos", "Skin3", "ClothingLightYellow"),
        Head = new IProfileClothingItem("GeneralHat", primeColor),
        ChestUnder = new IProfileClothingItem("LeatherJacket", primeColor, "ClothingLightYellow"),
        Waist = new IProfileClothingItem("Sash", primeColor),
        Legs = new IProfileClothingItem("CamoPants", primeColor, primeColor),
        Feet = new IProfileClothingItem("BootsBlack", "ClothingGray"),
        Accesory = new IProfileClothingItem("Armband", "ClothingLightYellow"),
    };
}