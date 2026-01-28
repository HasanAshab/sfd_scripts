// CoopGameOver - Automatically restarts the game when no human players remain

private bool gameOverTriggered = false;
private float gameStartTime = 0;
private const float EARLY_DEATH_WINDOW = 10000; // 10 seconds in milliseconds

public void OnStartup()
{
    // Record game start time
    gameStartTime = Game.TotalElapsedGameTime;
    
    // Set up player death callback to check for early deaths
    Events.PlayerDeathCallback.Start(OnPlayerDeath);
    
    // Create a timer that checks every 500ms for human players
    IObjectTimerTrigger checkTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    checkTimer.SetIntervalTime(500);
    checkTimer.SetRepeatCount(0); // Infinite repeat
    checkTimer.SetScriptMethod("CheckForHumanPlayers");
    checkTimer.Trigger();
}

public void OnPlayerDeath(IPlayer player, PlayerDeathArgs args)
{
    // Don't check if game over is already triggered
    if (gameOverTriggered)
        return;

    // Check if this is a human player dying within the first 10 seconds
    if (!player.IsBot)
    {
        float currentTime = Game.TotalElapsedGameTime;
        float timeSinceStart = currentTime - gameStartTime;
        
        if (timeSinceStart <= EARLY_DEATH_WINDOW)
        {
            gameOverTriggered = true;
            
            // Show a message before restarting
            Game.ShowPopupMessage("Human player died too early. Restarting game...");
            Game.RunCommand("slowmo 0");
            Game.RunCommand("gameover");
        }
    }
}

public void CheckForHumanPlayers(TriggerArgs args)
{
    // Don't check if game over is already triggered
    if (gameOverTriggered)
        return;
        
    bool hasHumanPlayers = false;
    
    // Get all players and check if any are human (not bots)
    IPlayer[] players = Game.GetPlayers();
    
    foreach (IPlayer player in players)
    {
        // Check if player is not a bot and is alive
        if (!player.IsBot && !player.IsDead)
        {
            hasHumanPlayers = true;
            break;
        }
    }

    // If no human players exist, restart the game
    if (!hasHumanPlayers && players.Length > 0)
    {
        gameOverTriggered = true;
        
        // Show a message before restarting
        Game.ShowPopupMessage("No human players remaining. Restarting game...");
        Game.RunCommand("slowmo 0");
        Game.RunCommand("gameover");
       
    }
}

public void OnPlayerSpawned(IPlayer player)
{
    // Reset the flag when a new game starts and update game start time
    gameOverTriggered = false;
    gameStartTime = Game.TotalElapsedGameTime;
}