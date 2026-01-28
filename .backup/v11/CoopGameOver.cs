// CoopGameOver - Automatically restarts the game when no human players remain

private bool gameOverTriggered = false;

public void OnStartup()
{
    // Create a timer that checks every 1000ms (1 second) for human players
    IObjectTimerTrigger checkTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    checkTimer.SetIntervalTime(500);
    checkTimer.SetRepeatCount(0); // Infinite repeat
    checkTimer.SetScriptMethod("CheckForHumanPlayers");
    checkTimer.Trigger();
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
        Game.ShowPopupMessage("No human players remaining. Restarting game...", Color.Red);
        Game.RunCommand("slowmo 0");
        Game.RunCommand("gameover");
       
    }
}

public void OnPlayerSpawned(IPlayer player)
{
    // Reset the flag when a new game starts
    gameOverTriggered = false;
}