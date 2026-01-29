// Militia - Team-based loadout with guaranteed specialist item distribution and continuous bot spawning
// Safe spawn system using path nodes
private static readonly Random RNG = new Random();

// Track spawned bots to exclude from winner calculation
private HashSet<int> spawnedRookieIds = new HashSet<int>();
private HashSet<int> spawnedCaptainIds = new HashSet<int>();
private HashSet<int> spawnedArtilleryIds = new HashSet<int>();
private HashSet<int> spawnedDroneIds = new HashSet<int>();

// Track player respawn timers
private Dictionary<int, IObjectTimerTrigger> playerRespawnTimers = new Dictionary<int, IObjectTimerTrigger>();

// Track dead players for respawning (stores player info even after they're gibbed)
private Dictionary<int, DeadPlayerInfo> deadPlayersAwaitingRespawn = new Dictionary<int, DeadPlayerInfo>();

// Simple respawn queue - much cleaner approach
private List<DeadPlayerInfo> respawnQueue = new List<DeadPlayerInfo>();

// Class to store dead player information for respawning
public class DeadPlayerInfo
{
    public int OriginalUniqueID { get; set; }
    public IUser User { get; set; }
    public PlayerTeam Team { get; set; }
    public IProfile Profile { get; set; }
    public bool NametagVisible { get; set; }
    public bool StatusBarsVisible { get; set; }
    public CameraFocusMode CameraFocusMode { get; set; }
    public PlayerModifiers Modifiers { get; set; }
    public BotBehavior BotBehavior { get; set; }
    public IObject GuardTarget { get; set; }
}

// Winner announcement tracking
private bool gameEnded = false;

// Track colonels for each team (now supports all teams)
private Dictionary<PlayerTeam, IPlayer> colonels = new Dictionary<PlayerTeam, IPlayer>();

private IPlayer team1Colonel = null;
private IPlayer team2Colonel = null;
private IPlayer team3Colonel = null;
private IPlayer team4Colonel = null;

// Track specialist item assignments per team
private int team1AssignedCount = 0;
private int team2AssignedCount = 0;
private int team3AssignedCount = 0;
private int team4AssignedCount = 0;

// Track which items have been assigned per team (for guaranteed coverage)
private bool[] team1Items = new bool[4]; // [SHOTGUN, Knife, Sniper, SMG]
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
    
    IObjectTimerTrigger artilleryTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    artilleryTimer.SetIntervalTime(20000); // 20 seconds
    artilleryTimer.SetRepeatCount(0); // Infinite repeats
    artilleryTimer.SetScriptMethod("SpawnArtillerys");
    artilleryTimer.Trigger();
    
    IObjectTimerTrigger droneTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    droneTimer.SetIntervalTime(16000); // 16 seconds
    droneTimer.SetRepeatCount(0); // Infinite repeats
    droneTimer.SetScriptMethod("SpawnDrones");
    droneTimer.Trigger();
    
    // Set up single respawn timer (processes queue every 5 seconds)
    IObjectTimerTrigger respawnTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    respawnTimer.SetIntervalTime(5000); // 5 seconds
    respawnTimer.SetRepeatCount(0); // Infinite repeats
    respawnTimer.SetScriptMethod("ProcessRespawnQueue");
    respawnTimer.Trigger();
    
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
    
    // Set up player death callback for respawning
    Events.PlayerDeathCallback.Start(OnPlayerDeath);
    
    // Equip all players with militia loadout
    foreach (IPlayer player in Game.GetPlayers())
    {
        EquipMilitiaLoadout(player);

        if (player.IsBot) {
            player.SetNametagVisible(false);
            player.SetStatusBarsVisible(false);
        }
    }

    PrepareP1();
    PrepareP2();
}

public void PrepareP1()
{
    IPlayer p1 = Game.GetPlayers()[0];
    PlayerModifiers mods = p1.GetModifiers();
    mods.ProjectileDamageTakenModifier *= 0.8f;
    p1.SetModifiers(mods);
}

public void PrepareP2()
{
    IPlayer p2 = Game.GetPlayers()[1];
    PlayerModifiers mods = p2.GetModifiers();
    mods.MeleeDamageTakenModifier *= 0.8f;
    p2.SetModifiers(mods);
}

public void SpawnRookies(TriggerArgs args)
{
    if (!gameEnded)
    {
        // Only spawn if colonels are alive
        foreach (var colonelEntry in colonels)
        {
            PlayerTeam team = colonelEntry.Key;
            IPlayer colonel = colonelEntry.Value;
            
            if (colonel != null && !colonel.IsDead)
            {
                SpawnRookie(team);
            }
        }
    }
}

public void SpawnCaptains(TriggerArgs args)
{
    if (!gameEnded)
    {
        // Only spawn if colonels are alive
        foreach (var colonelEntry in colonels)
        {
            PlayerTeam team = colonelEntry.Key;
            IPlayer colonel = colonelEntry.Value;
            
            if (colonel != null && !colonel.IsDead)
            {
                SpawnCaptain(team);
            }
        }
    }
}

public void SpawnArtillerys(TriggerArgs args)
{
    if (!gameEnded)
    {
        // Only spawn if colonels are alive
        foreach (var colonelEntry in colonels)
        {
            PlayerTeam team = colonelEntry.Key;
            IPlayer colonel = colonelEntry.Value;
            
            if (colonel != null && !colonel.IsDead)
            {
                SpawnArtillery(team);
            }
        }
    }
}

public void SpawnDrones(TriggerArgs args)
{
    if (!gameEnded)
    {
        // Only spawn if colonels are alive
        foreach (var colonelEntry in colonels)
        {
            PlayerTeam team = colonelEntry.Key;
            IPlayer colonel = colonelEntry.Value;
            
            if (colonel != null && !colonel.IsDead)
            {
                SpawnDrone(team);
            }
        }
    }
}

public void CheckForWinner(TriggerArgs args)
{
    if (gameEnded) return;
    
    // Get all main players/bots (exclude spawned rookies and captains)
    IPlayer[] allPlayers = Game.GetPlayers();
    IPlayer[] mainPlayers = allPlayers.Where(p => !spawnedRookieIds.Contains(p.UniqueID) && 
                                                  !spawnedCaptainIds.Contains(p.UniqueID) &&
                                                  !spawnedArtilleryIds.Contains(p.UniqueID) &&
                                                  !spawnedDroneIds.Contains(p.UniqueID)).ToArray();
    
    // Check if all colonels are dead
    bool allColonelsDead = true;
    foreach (var colonelEntry in colonels)
    {
        IPlayer colonel = colonelEntry.Value;
        if (colonel != null && !colonel.IsDead)
        {
            allColonelsDead = false;
            break;
        }
    }
    
    // Check if all human players are dead
    bool allHumansDead = true;
    foreach (IPlayer player in mainPlayers)
    {
        if (!player.IsBot && !player.IsDead)
        {
            allHumansDead = false;
            break;
        }
    }
    
    // If no colonels left AND all humans are dead, end the game
    if (allColonelsDead && allHumansDead)
    {
        gameEnded = true;
        
        // Clean up respawn queue when game ends
        respawnQueue.Clear();
        
        Game.ShowPopupMessage("GAME OVER - No colonels or humans left!");
        Game.RunCommand("gameover");
        return;
    }
    
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
        
        // Clean up respawn queue when game ends
        respawnQueue.Clear();
        
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
    // Get all current players and find unique teams
    IPlayer[] allPlayers = Game.GetPlayers();
    HashSet<PlayerTeam> existingTeams = new HashSet<PlayerTeam>();
    
    foreach (IPlayer player in allPlayers)
    {
        PlayerTeam team = player.GetTeam();
        if (team != PlayerTeam.Independent)
        {
            existingTeams.Add(team);
        }
    }
    
    // Create colonels for each existing team
    foreach (PlayerTeam team in existingTeams)
    {
        CreateColonel(team);
    }
}

private void CreateColonel(PlayerTeam team)
{
    // Find any player from this team to get spawn position
    IPlayer[] allPlayers = Game.GetPlayers();
    Vector2 spawnPos = Vector2.Zero;
    
    foreach (IPlayer player in allPlayers)
    {
        if (player.GetTeam() == team)
        {
            spawnPos = player.GetWorldPosition();
            break; // Use the first player found from this team
        }
    }
    
    // Fallback to center if no team player found
    if (spawnPos == Vector2.Zero)
    {
        spawnPos = Vector2.Zero;
    }
    
    // Create colonel bot
    IPlayer colonel = Game.CreatePlayer(spawnPos);
    if (colonel != null)
    {
        // Set team
        colonel.SetTeam(team);
        colonel.SetBotName("COLONEL");
        
        // Set as bot with very bad behavior (BotD)
        BotBehavior colonelBehavior = new BotBehavior(true, PredefinedAIType.BotD);
        colonel.SetBotBehavior(colonelBehavior);
        
        // Set colonel properties
        colonel.SetNametagVisible(true);
        colonel.SetStatusBarsVisible(true);
        colonel.SetCameraSecondaryFocusMode(CameraFocusMode.Focus);
        
        // Give colonel enhanced stats (they're important but bad at fighting)
        PlayerModifiers colonelModifiers = colonel.GetModifiers();
        colonelModifiers.MaxHealth = 600; // High health to survive
        colonelModifiers.CurrentHealth = 600;
        colonel.SetModifiers(colonelModifiers);
        
        // Give colonel basic weapons
        colonel.GiveWeaponItem(WeaponItem.MAGNUM);
        colonel.GiveWeaponItem(WeaponItem.KATANA);
        colonel.GiveWeaponItem(WeaponItem.GRENADES);
        
        // Set colonel profile
        colonel.SetProfile(GetColonelProfile(team));
        
        // Store colonel reference
        colonels[team] = colonel;
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
            !spawnedCaptainIds.Contains(player.UniqueID) &&
            !spawnedArtilleryIds.Contains(player.UniqueID) &&
            !spawnedDroneIds.Contains(player.UniqueID))
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

public void OnPlayerDeath(IPlayer player, PlayerDeathArgs args)
{
    // Only handle respawn for main players (P1-P8), not spawned bots
    if (
        !spawnedRookieIds.Contains(player.UniqueID) && 
        !spawnedCaptainIds.Contains(player.UniqueID) && 
        !spawnedArtilleryIds.Contains(player.UniqueID) &&
        !spawnedDroneIds.Contains(player.UniqueID))
    {
        // Check if player's colonel is alive
        PlayerTeam playerTeam = player.GetTeam();
        IPlayer colonel = null;

        if (colonels.ContainsKey(playerTeam))
        {
            colonel = colonels[playerTeam];
        }
        
        // Only queue for respawn if colonel is alive and game hasn't ended
        if (colonel != null && !colonel.IsDead && !gameEnded)
        {
            // Remove any existing entry for this player from the queue
            respawnQueue.RemoveAll(deadPlayer => deadPlayer.OriginalUniqueID == player.UniqueID);
            
            // Store player information and add to respawn queue
            DeadPlayerInfo deadPlayerInfo = new DeadPlayerInfo
            {
                OriginalUniqueID = player.UniqueID,
                User = player.GetUser(),
                Team = player.GetTeam(),
                Profile = player.GetProfile(),
                NametagVisible = player.GetNametagVisible(),
                StatusBarsVisible = player.GetStatusBarsVisible(),
                CameraFocusMode = player.GetCameraSecondaryFocusMode(),
                Modifiers = player.GetModifiers(),
                BotBehavior = player.GetBotBehavior(),
                GuardTarget = player.GetGuardTarget()
            };
            
            // Add to respawn queue
            respawnQueue.Add(deadPlayerInfo);
        }
    }
}

public void ProcessRespawnQueue(TriggerArgs args)
{
    if (gameEnded || respawnQueue.Count == 0) return;
    
    // Process all players in the respawn queue
    List<DeadPlayerInfo> playersToRemove = new List<DeadPlayerInfo>();
    
    foreach (DeadPlayerInfo deadPlayerInfo in respawnQueue)
    {
        // Check if player's colonel is still alive
        IPlayer colonel = null;
        
        if (colonels.ContainsKey(deadPlayerInfo.Team))
        {
            colonel = colonels[deadPlayerInfo.Team];
        }
        
        // Respawn at colonel's position if colonel is alive
        if (colonel != null && !colonel.IsDead)
        {
            Vector2 respawnPos = colonel.GetWorldPosition();
            IPlayer newPlayer = Game.CreatePlayer(respawnPos);
            
            if (newPlayer != null)
            {
                // Restore all the original player properties
                newPlayer.SetProfile(deadPlayerInfo.Profile);
                newPlayer.SetNametagVisible(deadPlayerInfo.NametagVisible);
                newPlayer.SetStatusBarsVisible(deadPlayerInfo.StatusBarsVisible);
                newPlayer.SetTeam(deadPlayerInfo.Team);
                newPlayer.SetCameraSecondaryFocusMode(deadPlayerInfo.CameraFocusMode);
                
                // Set health to full
                PlayerModifiers mods = deadPlayerInfo.Modifiers;
                mods.CurrentHealth = mods.MaxHealth;
                newPlayer.SetModifiers(mods);
                
                newPlayer.SetUser(deadPlayerInfo.User);
                newPlayer.SetBotBehavior(deadPlayerInfo.BotBehavior);
                
                if (deadPlayerInfo.GuardTarget != null)
                {
                    newPlayer.SetGuardTarget(deadPlayerInfo.GuardTarget);
                }
                
                // Re-equip the player after respawn
                EquipMilitiaLoadout(newPlayer);
            }
        }

        // Mark this player for removal from queue (whether respawned or not)
        playersToRemove.Add(deadPlayerInfo);
    }
    
    // Remove all processed players from the queue
    foreach (DeadPlayerInfo deadPlayer in playersToRemove)
    {
        respawnQueue.Remove(deadPlayer);
    }
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

private void EquipMilitiaLoadout(IPlayer player, bool assignProfile = true)
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
    if (player.IsBot && assignProfile)
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
            player.GiveWeaponItem(WeaponItem.SHOTGUN);
            break;
        case 1:
            player.GiveWeaponItem(WeaponItem.KNIFE);
            break;
        case 2:
            // 50% chance for BAZOOKA, 50% chance for SNIPER
            Vector2 playerPos = player.GetWorldPosition();
            int weaponSeed = (int)(Game.TotalElapsedGameTime * 1000) + 
                           (int)(playerPos.X * 100) + 
                           (int)(playerPos.Y * 100) + 
                           player.UniqueID;
            
            bool giveBazooka = (weaponSeed % 2) == 0;
            
            if (giveBazooka)
            {
                player.GiveWeaponItem(WeaponItem.BAZOOKA);
            }
            else
            {
                player.GiveWeaponItem(WeaponItem.SNIPER);
            }
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
    string primeColor = GetPrimeColor(team);
    return new IProfile[]
        {
            new IProfile()
            {
                Name = "Soldier",
                Gender = Gender.Male,
                Skin = new IProfileClothingItem("Tattoos", "Skin4", "ClothingLightYellow"),
                Head = new IProfileClothingItem("Helmet", primeColor),
                ChestUnder = new IProfileClothingItem("MilitaryShirt", primeColor, "ClothingLightBlue"),
                Waist = new IProfileClothingItem("SatchelBelt", primeColor),
                Legs = new IProfileClothingItem("CamoPants", primeColor, primeColor),
                Feet = new IProfileClothingItem("BootsBlack", primeColor),
            },
            new IProfile()
            {
                Name = "Sniper",
                Gender = Gender.Male,
                Skin = new IProfileClothingItem("Normal", "Skin2", "ClothingLightGray"),
                ChestOver = new IProfileClothingItem("AmmoBelt", primeColor),
                ChestUnder = new IProfileClothingItem("TShirt", primeColor),
                Hands = new IProfileClothingItem("Gloves", "ClothingGray"),
                Waist = new IProfileClothingItem("AmmoBeltWaist", "ClothingGray"),
                Legs = new IProfileClothingItem("CamoPants", "ClothingDarkGreen", primeColor),
                Feet = new IProfileClothingItem("BootsBlack", primeColor),
                Accesory = new IProfileClothingItem("Vizor", primeColor, "ClothingLightRed"),
            },
            new IProfile()
            {
                Name = "Sniper",
                Gender = Gender.Male,
                Skin = new IProfileClothingItem("Normal", "Skin1", "ClothingLightGray"),
                ChestOver = new IProfileClothingItem("AmmoBelt", primeColor),
                ChestUnder = new IProfileClothingItem("TShirt", primeColor),
                Hands = new IProfileClothingItem("Gloves", "ClothingGray"),
                Waist = new IProfileClothingItem("AmmoBeltWaist", "ClothingGray"),
                Legs = new IProfileClothingItem("CamoPants", "ClothingDarkGreen", primeColor),
                Feet = new IProfileClothingItem("BootsBlack", primeColor),
                Accesory = new IProfileClothingItem("Vizor", primeColor, "ClothingLightRed"),
            }
        };

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
        BotBehavior rookieBehavior = new BotBehavior(true, PredefinedAIType.BotD);
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
        BotBehavior captainBehavior = new BotBehavior(true, PredefinedAIType.BotC);
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

private void SpawnArtillery(PlayerTeam team)
{
    // Get spawn position near colonel
    Vector2 spawnPos = GetColonelSpawnPosition(team);
    
    // Create artillery bot
    IPlayer artillery = Game.CreatePlayer(spawnPos);
    artillery.SetNametagVisible(false);
    artillery.SetStatusBarsVisible(false);
    // Track this as a spawned artillery (exclude from winner calculation)
    spawnedArtilleryIds.Add(artillery.UniqueID);
    
    artillery.SetCameraSecondaryFocusMode(CameraFocusMode.Ignore);
    
    // Set team
    artillery.SetTeam(team);
    
    // Set as bot with normal behavior
    BotBehavior artilleryBehavior = new BotBehavior(true, PredefinedAIType.BotA);
    artillery.SetBotBehavior(artilleryBehavior);

    BotBehaviorSet artilleryBehaviorSet = artillery.GetBotBehaviorSet();
    artilleryBehaviorSet.SearchItems = 0;
    artilleryBehaviorSet.MeleeUsage = false;
    artillery.SetBotBehaviorSet(artilleryBehaviorSet);


    // Guard Own Colonel
    // SetColonelGuard(artillery, team);

    // Set high HP and slow speed
    PlayerModifiers artilleryModifiers = new PlayerModifiers();
    artilleryModifiers.MaxHealth = 100;
    artilleryModifiers.CurrentHealth = 100;
    artilleryModifiers.RunSpeedModifier = 0.35f;
    artilleryModifiers.SprintSpeedModifier = 0.35f;

    artilleryModifiers.SizeModifier = 1.5f;
    artilleryModifiers.ProjectileDamageDealtModifier *= 0.2f;

    artilleryModifiers.FireDamageTakenModifier *= 1.3f;
    artilleryModifiers.ProjectileDamageTakenModifier *= 0.4f;
    artilleryModifiers.MeleeDamageTakenModifier *= 0.2f;
    artilleryModifiers.ImpactDamageTakenModifier *= 0.2f;
    artilleryModifiers.ExplosionDamageTakenModifier *= 0.05f;

    artilleryModifiers.MeleeStunImmunity = 1;
    artilleryModifiers.InfiniteAmmo = 1;
    artilleryModifiers.ItemDropMode = 1;


    artillery.SetModifiers(artilleryModifiers);

    // Give weapons
    artillery.GiveWeaponItem(WeaponItem.GRENADE_LAUNCHER);
    artillery.GiveWeaponItem(WeaponItem.FIREAMMO);

    // Set artillery profile
    artillery.SetProfile(GetArtilleryProfile(team));
}


private void SpawnDrone(PlayerTeam team)
{
    Vector2 spawnPos = GetColonelSpawnPosition(team);

    IObjectStreetsweeper bulletDrone = Game.CreateObject("streetsweeper", spawnPos) as IObjectStreetsweeper;
    bulletDrone.SetCameraSecondaryFocusMode(CameraFocusMode.Ignore);
    bulletDrone.SetOwnerTeam(team);
    bulletDrone.SetWeaponType(StreetsweeperWeaponType.MachineGun);
    
    // Track the drone (Streetsweeper objects have UniqueID too)
    spawnedDroneIds.Add(bulletDrone.UniqueID);
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
    
    // Different spawn strategies for different teams
    switch (team)
    {
        case PlayerTeam.Team1:
            // Team1: rightest + topest position
            IObject rightestTopest = allSpawns
                .OrderByDescending(spawn => spawn.GetWorldPosition().X) // Rightest first
                .ThenBy(spawn => spawn.GetWorldPosition().Y)            // Then topest (lowest Y)
                .FirstOrDefault();
            return rightestTopest != null ? rightestTopest.GetWorldPosition() : Vector2.Zero;
            
        case PlayerTeam.Team2:
            // Team2: leftest + bottomest position
            IObject leftestBottomest = allSpawns
                .OrderBy(spawn => spawn.GetWorldPosition().X)           // Leftest first
                .ThenByDescending(spawn => spawn.GetWorldPosition().Y) // Then bottomest (highest Y)
                .FirstOrDefault();
            return leftestBottomest != null ? leftestBottomest.GetWorldPosition() : Vector2.Zero;
            
        case PlayerTeam.Team3:
            // Team3: leftest + topest position
            IObject leftestTopest = allSpawns
                .OrderBy(spawn => spawn.GetWorldPosition().X)          // Leftest first
                .ThenBy(spawn => spawn.GetWorldPosition().Y)           // Then topest (lowest Y)
                .FirstOrDefault();
            return leftestTopest != null ? leftestTopest.GetWorldPosition() : Vector2.Zero;
            
        case PlayerTeam.Team4:
            // Team4: rightest + bottomest position
            IObject rightestBottomest = allSpawns
                .OrderByDescending(spawn => spawn.GetWorldPosition().X) // Rightest first
                .ThenByDescending(spawn => spawn.GetWorldPosition().Y)  // Then bottomest (highest Y)
                .FirstOrDefault();
            return rightestBottomest != null ? rightestBottomest.GetWorldPosition() : Vector2.Zero;
            
        default:
            // Random spawn for other teams
            return allSpawns.ElementAt(RNG.Next(allSpawns.Count())).GetWorldPosition();
    }
}

private Vector2 GetColonelSpawnPosition(PlayerTeam team)
{
    if (colonels.ContainsKey(team))
    {
        IPlayer colonel = colonels[team];
        if (colonel != null && !colonel.IsDead)
        {
            // Spawn at exact same position as colonel
            return colonel.GetWorldPosition();
        }
    }
    
    // Fallback to team spawn position if colonel is dead/missing
    return GetTeamSpawnPosition(team);
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
    if (colonels.ContainsKey(team))
    {
        IPlayer ownColonel = colonels[team];
        if (ownColonel != null && !ownColonel.IsDead)
        {
            player.SetGuardTarget(ownColonel);
        }
    }
}

private string GetPrimeColor(PlayerTeam team)
{
    if (team == PlayerTeam.Team1)
    {
        return "ClothingDarkGray";
    }
    else if (team == PlayerTeam.Team2)
    {
        return "ClothingDarkYellow";
    }
    else if (team == PlayerTeam.Team3)
    {
        return "ClothingLightOrange";
    }
    else if (team == PlayerTeam.Team4)
    {
        return "ClothingLightGray";
    }
    
    return "";
}

private IProfile GetRookieProfile(PlayerTeam team)
{
    string primeColor = GetPrimeColor(team);
    
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
    string primeColor = GetPrimeColor(team);
    
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
    string primeColor = GetPrimeColor(team);
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

private IProfile GetArtilleryProfile(PlayerTeam team)
{
    string primeColor = GetPrimeColor(team);
    
    return new IProfile()
    {
        Name = "Artillery",
        Gender = Gender.Male,
        Skin = new IProfileClothingItem("MechSkin", primeColor, "ClothingLightGray"),
    };
}