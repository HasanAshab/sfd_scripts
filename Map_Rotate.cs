// ============================================================================
// MapRotation.cs  -  Superfighters Deluxe EXTENSION script
//
// Rotates through a list of maps. Each map is played for MATCHES_PER_MAP
// matches (rounds), then the server switches to the next map in the list and
// wraps back to the first one after the last.
//
// Must be run as an EXTENSION script (not a map script): extension scripts are
// allowed to run every command, which is needed to change the map.
// ============================================================================

// ---------------------------------------------------------------------------
// CONFIG - edit these
// ---------------------------------------------------------------------------

// Maps to rotate through, in order. Use exactly what you would type after /MAP.
private readonly string[] MAP_LIST = new string[]
{
    "Storage",
    "Trainyard",
    "Police Station"
};

// How many matches (rounds) to play on each map before switching.
private const int MATCHES_PER_MAP = 2;

// Delay after the final match ends before the map is changed (milliseconds),
// so players can still see the result screen.
private const int SWITCH_DELAY_MS = 4000;

// Command used to change the map. The map name is appended to this.
private const string CHANGE_MAP_COMMAND = "/MAP ";

// Key used in Game.LocalStorage to remember where we are in the list.
private const string STORAGE_KEY_INDEX = "MapRotationIndex";

// ---------------------------------------------------------------------------
// STATE
// ---------------------------------------------------------------------------

private int currentIndex = -1;          // index in MAP_LIST of the map being played (-1 = unknown)
private bool switchScheduled = false;   // true once a map switch has been queued for this round

// ---------------------------------------------------------------------------
// LIFECYCLE
// ---------------------------------------------------------------------------

public void OnStartup()
{
    // Don't rotate while testing in the map editor.
    if (Game.IsEditorTest) return;

    if (MAP_LIST == null || MAP_LIST.Length < 2)
    {
        Game.WriteToConsole("[MapRotation] MAP_LIST needs at least 2 maps. Rotation disabled.");
        return;
    }

    switchScheduled = false;

    // Where were we? Start from what was saved, then re-sync from the current
    // map's name if it is one of the maps in the list (handles manual map changes).
    currentIndex = LoadIndex();
    int foundIndex = FindMapIndex(Game.MapName);
    if (foundIndex >= 0) currentIndex = foundIndex;
    SaveIndex(currentIndex);

    Game.WriteToConsole("[MapRotation] Map '" + Game.MapName + "' - match " + Game.MapRound + "/" + MATCHES_PER_MAP);

    // Safety net: if the game restarted the map before our delayed switch fired
    // and we've already gone past the limit, switch right away.
    if (Game.MapRound > MATCHES_PER_MAP)
    {
        SwitchToNextMap();
        return;
    }

    // Watch for the end of the match.
    Events.UpdateCallback.Start(OnUpdate, 250);
}

public void OnUpdate(float elapsed)
{
    if (switchScheduled) return;
    if (!Game.IsGameOver) return;

    // Only switch once the last match for this map has finished.
    if (Game.MapRound < MATCHES_PER_MAP) return;

    switchScheduled = true;

    int nextIndex = (currentIndex + 1) % MAP_LIST.Length;
    Game.ShowChatMessage("Map rotation: next map is " + MAP_LIST[nextIndex] + " (changing in " + (SWITCH_DELAY_MS / 1000) + "s)");

    // Run once after the delay.
    Events.UpdateCallback.Start(OnSwitchTimer, SWITCH_DELAY_MS, 1);
}

public void OnSwitchTimer(float elapsed)
{
    SwitchToNextMap();
}

// ---------------------------------------------------------------------------
// HELPERS
// ---------------------------------------------------------------------------

private void SwitchToNextMap()
{
    // currentIndex of -1 (unknown) wraps to the first map in the list.
    int nextIndex = (currentIndex + 1) % MAP_LIST.Length;
    string nextMap = MAP_LIST[nextIndex];

    // Save BEFORE changing map; the script restarts on the new map.
    SaveIndex(nextIndex);

    Game.WriteToConsole("[MapRotation] Switching to '" + nextMap + "' (index " + nextIndex + ")");
    Game.RunCommand(CHANGE_MAP_COMMAND + nextMap);
}

private int FindMapIndex(string mapName)
{
    if (string.IsNullOrEmpty(mapName)) return -1;

    for (int i = 0; i < MAP_LIST.Length; i++)
    {
        if (string.Equals(MAP_LIST[i], mapName, StringComparison.OrdinalIgnoreCase))
        {
            return i;
        }
    }
    return -1;
}

private int LoadIndex()
{
    int value;
    if (Game.LocalStorage.TryGetItemInt(STORAGE_KEY_INDEX, out value))
    {
        if (value >= 0 && value < MAP_LIST.Length) return value;
    }
    return -1;
}

private void SaveIndex(int index)
{
    Game.LocalStorage.SetItem(STORAGE_KEY_INDEX, index);
}