// Bedwars - Teams must protect their own objects to win

// Track team objects
private Dictionary<PlayerTeam, IObject> teamObjects = new Dictionary<PlayerTeam, IObject>();
private bool gameEnded = false;

public IObjectPathNode[] AvailablePathNodes
{
    get
    {
        return Game.GetObjects<IObjectPathNode>().Where(path => path.GetNodeEnabled() && !path.GetIsElevatorNode() &&
            (path.GetPathNodeType() == PathNodeType.Ground ||
             path.GetPathNodeType() == PathNodeType.Platform)).ToArray();
    }
}

public void OnStartup()
{
    // Spawn team objects at top corner positions
    SpawnTeamObjects();
    
    // Set up winner check timer
    IObjectTimerTrigger winnerTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    winnerTimer.SetIntervalTime(2000); // Check every 2 seconds
    winnerTimer.SetRepeatCount(0); // Infinite repeats
    winnerTimer.SetScriptMethod("CheckForWinner");
    winnerTimer.Trigger();
    
    // Show game instructions
    Game.ShowPopupMessage("BEDWARS: Protect your team's TNT!");
}

public void CheckForWinner(TriggerArgs args)
{
    if (gameEnded) return;
    
    // Check which team objects are still alive
    List<PlayerTeam> aliveTeams = new List<PlayerTeam>();
    
    foreach (var kvp in teamObjects)
    {
        PlayerTeam team = kvp.Key;
        IObject teamObject = kvp.Value;
        
        // Check if object still exists and is not destroyed
        if (teamObject != null && !teamObject.IsRemoved)
        {
            aliveTeams.Add(team);
        }
    }
    
    // Announce winner if only one team's object remains
    if (aliveTeams.Count <= 1)
    {
        gameEnded = true;
        
        if (aliveTeams.Count == 1)
        {
            string teamName = GetTeamName(aliveTeams[0]);
            Game.ShowPopupMessage(teamName + " WINS! Their TNT survived!");
        }
        else
        {
            // All objects destroyed - draw
            Game.ShowPopupMessage("DRAW - All TNT destroyed!");
        }
        
        // Trigger game over after 3 seconds
        IObjectTimerTrigger gameOverTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
        gameOverTimer.SetIntervalTime(3000);
        gameOverTimer.SetRepeatCount(1);
        gameOverTimer.SetScriptMethod("TriggerGameOver");
        gameOverTimer.Trigger();
    }
}

public void TriggerGameOver(TriggerArgs args)
{
    IObjectGameOverTrigger gameOverTrigger = (IObjectGameOverTrigger)Game.CreateObject("GameOverTrigger");
    gameOverTrigger.Trigger();
}

private void SpawnTeamObjects()
{
    IObjectPathNode[] availableNodes = AvailablePathNodes;
    
    if (availableNodes.Length == 0)
    {
        Game.ShowPopupMessage("No suitable spawn points found!");
        return;
    }
    
    // Get teams that have players
    List<PlayerTeam> activeTeams = GetActiveTeams();
    
    if (activeTeams.Count < 2)
    {
        Game.ShowPopupMessage("Need at least 2 teams to play Bedwars!");
        return;
    }
    
    // Find top corner positions for each team
    foreach (PlayerTeam team in activeTeams)
    {
        Vector2 spawnPos = GetTeamObjectPosition(team, availableNodes);
        SpawnTeamObject(team, spawnPos);
    }
}

private Vector2 GetTeamObjectPosition(PlayerTeam team, IObjectPathNode[] nodes)
{
    // Sort nodes by Y position (top = lowest Y values)
    var topNodes = nodes.OrderBy(node => node.GetWorldPosition().Y).Take(nodes.Length / 2).ToArray();
    
    if (team == PlayerTeam.Team1)
    {
        // Team1: Top-left corner (lowest X among top nodes)
        var topLeftNode = topNodes.OrderBy(node => node.GetWorldPosition().X).FirstOrDefault();
        return topLeftNode != null ? topLeftNode.GetWorldPosition() : Vector2.Zero;
    }
    else if (team == PlayerTeam.Team2)
    {
        // Team2: Top-right corner (highest X among top nodes)
        var topRightNode = topNodes.OrderByDescending(node => node.GetWorldPosition().X).FirstOrDefault();
        return topRightNode != null ? topRightNode.GetWorldPosition() : Vector2.Zero;
    }
    else if (team == PlayerTeam.Team3)
    {
        // Team3: Second from left among top nodes
        var team3Node = topNodes.OrderBy(node => node.GetWorldPosition().X).Skip(1).FirstOrDefault();
        if (team3Node == null) 
        {
            team3Node = topNodes.OrderBy(node => node.GetWorldPosition().X).FirstOrDefault();
        }
        return team3Node != null ? team3Node.GetWorldPosition() : Vector2.Zero;
    }
    else if (team == PlayerTeam.Team4)
    {
        // Team4: Second from right among top nodes
        var team4Node = topNodes.OrderByDescending(node => node.GetWorldPosition().X).Skip(1).FirstOrDefault();
        if (team4Node == null) 
        {
            team4Node = topNodes.OrderByDescending(node => node.GetWorldPosition().X).FirstOrDefault();
        }
        return team4Node != null ? team4Node.GetWorldPosition() : Vector2.Zero;
    }
    
    // Fallback to center of top nodes
    if (topNodes.Length > 0)
    {
        return topNodes[0].GetWorldPosition();
    }
    return Vector2.Zero;
}

private void SpawnTeamObject(PlayerTeam team, Vector2 position)
{
    // Create TNT with high health
    IObject tnt = Game.CreateObject("WpnGrenadesThrown", position);
    
    if (tnt != null)
    {
        // Set high health to make it durable
        tnt.SetHealth(200); // High health so it takes effort to destroy
        
        // Store reference to track this team's object
        teamObjects[team] = tnt;
        
        // Show team color indicator
        string teamName = GetTeamName(team);
        Game.ShowPopupMessage(teamName + " TNT spawned!");
    }
}

private List<PlayerTeam> GetActiveTeams()
{
    List<PlayerTeam> activeTeams = new List<PlayerTeam>();
    IPlayer[] players = Game.GetPlayers();
    
    foreach (IPlayer player in players)
    {
        PlayerTeam team = player.GetTeam();
        if (team != PlayerTeam.Independent && !activeTeams.Contains(team))
        {
            activeTeams.Add(team);
        }
    }
    
    return activeTeams;
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
