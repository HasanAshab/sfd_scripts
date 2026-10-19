// ---- Health regeneration system ----
// Tracks regen state for ANY number of players (including ones added after
// startup), keyed by each player's UniqueID instead of fixed array slots.

private class RegenState
{
    public float MaxEnergyTime = -1f;   // Time regen eligibility started (-1 = not tracking)
    public float LastCombatTime = -1f;  // Time of last combat activity (-1 = none recent)
}

// Regen rules per player "type" - human-controlled vs bot.
private struct RegenProfile
{
    public float RegenDelay;        // Delay after becoming eligible before regen starts
    public float CombatCooldown;    // Delay after combat before regen can start
    public float RegenPercent;      // % of max health regenerated per tick
    public bool RequiresMaxEnergy;  // Humans must be at max energy; bots don't

    public RegenProfile(float regenDelay, float combatCooldown, float regenPercent, bool requiresMaxEnergy)
    {
        RegenDelay = regenDelay;
        CombatCooldown = combatCooldown;
        RegenPercent = regenPercent;
        RequiresMaxEnergy = requiresMaxEnergy;
    }
}

private static readonly RegenProfile HumanRegenProfile = new RegenProfile(1500f, 1500f, 0.05f, true);
private static readonly RegenProfile BotRegenProfile = new RegenProfile(500f, 500f, 0.03f, false);

// Keyed by UniqueID so it works for any number of players, in any slot order,
// including players/bots that join after OnStartup.
private readonly Dictionary<int, RegenState> regenStates = new Dictionary<int, RegenState>();

// Reused every tick instead of reallocated, to keep GC pressure down.
private readonly HashSet<int> activePlayerIds = new HashSet<int>();
private readonly List<int> staleIdBuffer = new List<int>();

private int updateTickCount = 0;
private const int PRUNE_INTERVAL_TICKS = 50; // Sweep stale entries roughly every 5s at a 100ms tick

public void OnStartup()
{
    // Set up combat detection events for regeneration tracking
    Events.PlayerDamageCallback.Start(OnPlayerDamage);
    Events.PlayerMeleeActionCallback.Start(OnPlayerMeleeAction);

    // Drive regen at 100ms for smooth, responsive ticking
    Events.UpdateCallback.Start(OnUpdate, 250);
}

public void OnPlayerDamage(IPlayer player, PlayerDamageArgs args)
{
    RegisterCombatActivity(player);
}

public void OnPlayerMeleeAction(IPlayer player, PlayerMeleeHitArg[] args)
{
    RegisterCombatActivity(player);
}

private void RegisterCombatActivity(IPlayer player)
{
    if (player == null) return;

    // O(1) lookup by UniqueID - no need to scan Game.GetPlayers() to find "who this is"
    RegenState state = GetOrCreateState(player.UniqueID);
    state.LastCombatTime = Game.TotalElapsedGameTime;
    state.MaxEnergyTime = -1f; // Reset regen timer
}

public void OnUpdate(float elapsed)
{
    IPlayer[] players = Game.GetPlayers();
    activePlayerIds.Clear();

    for (int i = 0; i < players.Length; i++)
    {
        IPlayer player = players[i];
        activePlayerIds.Add(player.UniqueID);

        if (player.IsDead) continue;

        ProcessRegeneration(player, GetOrCreateState(player.UniqueID));
    }

    // Periodically forget players who are no longer around, so the dictionary
    // doesn't grow forever on long-running or high-churn servers.
    updateTickCount++;
    if (updateTickCount >= PRUNE_INTERVAL_TICKS)
    {
        updateTickCount = 0;
        PruneStaleStates();
    }
}

private RegenState GetOrCreateState(int uniqueId)
{
    RegenState state;
    if (!regenStates.TryGetValue(uniqueId, out state))
    {
        state = new RegenState();
        regenStates[uniqueId] = state;
    }
    return state;
}

private void PruneStaleStates()
{
    staleIdBuffer.Clear();
    foreach (KeyValuePair<int, RegenState> entry in regenStates)
    {
        if (!activePlayerIds.Contains(entry.Key))
            staleIdBuffer.Add(entry.Key);
    }
    for (int i = 0; i < staleIdBuffer.Count; i++)
        regenStates.Remove(staleIdBuffer[i]);
}

private void ProcessRegeneration(IPlayer player, RegenState state)
{
    // Bots regen faster and don't need max energy; humans do.
    RegenProfile profile = player.IsBot ? BotRegenProfile : HumanRegenProfile;
    float currentTime = Game.TotalElapsedGameTime;

    // Still in combat cooldown - skip the modifiers read entirely
    if (state.LastCombatTime >= 0 && currentTime - state.LastCombatTime < profile.CombatCooldown)
    {
        state.MaxEnergyTime = -1f;
        return;
    }

    PlayerModifiers mods = player.GetModifiers();
    bool regenEligible = !profile.RequiresMaxEnergy || mods.CurrentEnergy >= mods.MaxEnergy;

    if (!regenEligible)
    {
        state.MaxEnergyTime = -1f;
        return;
    }

    if (state.MaxEnergyTime < 0)
    {
        state.MaxEnergyTime = currentTime;
    }

    if (currentTime - state.MaxEnergyTime >= profile.RegenDelay)
    {
        RegenerateHealth(player, mods, profile.RegenPercent);
    }
}

private void RegenerateHealth(IPlayer player, PlayerModifiers mods, float regenPercentage)
{
    if (mods.CurrentHealth >= mods.MaxHealth) return;

    int regenAmount = (int)(mods.MaxHealth * regenPercentage);
    mods.CurrentHealth = Math.Min(mods.MaxHealth, mods.CurrentHealth + regenAmount);
    player.SetModifiers(mods);
}