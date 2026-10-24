// ============================================================================
//  MARLEY vs PARADIS  -  WAR TRACKER
//  Superfighters Deluxe map script (paste into the map editor's script window)
//
//  - Every finished match (battle) is credited to Team 1 or Team 2 (players on
//    any other team are ignored). Not counted: draws, and matches that the
//    co-op script (COOPGAMEOVER.cs) restarted with the /gameover command.
//  - One chat line after every match says whether it was counted and why
//    (ANNOUNCE_RESULTS).
//  - The first team to win WINS_TO_TAKE_WAR battles (5) wins the WAR. That adds
//    one to its WAR WIN COUNT and the battle tally starts again from 0.
//  - War win counts (and the running battle tally) persist in a shared storage,
//    so they survive round restarts, map changes and server restarts, and can
//    be shared between maps that contain this script and use the same
//    STORAGE_NAME.
//  - The war win count of both teams is always shown with a popup message.
//  - On the first match AFTER a war was won (this happens once per war):
//      1. input is disabled for every player
//      2. every living winner aims at the sky and fires 3 shots
//         (a pistol is given first if the player has no handgun)
//      3. when that is finished every living loser starts crouching
//      4. input is never re-enabled for the rest of that match
//
//  Host/moderator chat command:   /war reset     (sets war wins and battle tally to 0)
// ============================================================================

// ------------------------------- CONFIG -------------------------------------
private const int    WINS_TO_TAKE_WAR = 4;                 // the first team to win this many battles wins the war
private const string TEAM1_NAME      = "Marley";           // PlayerTeam.Team1
private const string TEAM2_NAME      = "Paradis";          // PlayerTeam.Team2

// Skip matches that COOPGAMEOVER.cs ended with /gameover. That script's commands can't be
// seen from here, so its two conditions are mirrored instead (see CoopScriptWouldRestart):
//   - no living human (non-bot) player exists, or
//   - a human player died within COOP_EARLY_DEATH_WINDOW_MS of the match start.
private const bool   SKIP_COOP_GAMEOVER         = true;
private const string COOP_STORAGE_NAME         = "SuperDSCoopSync";   // the storage COOPGAMEOVER.cs reads
private const float  COOP_CHECK_INTERVAL_MS    = 500f;                // how often COOPGAMEOVER.cs checks
private const float  COOP_EARLY_DEATH_WINDOW_MS = 10000f;             // its "died too early" window

// One chat line after every match: counted or not, and why.
private const bool   ANNOUNCE_RESULTS          = true;

// Shared storage name. API rule: letters only, no spaces, 3-40 characters.
private const string STORAGE_NAME    = "MarleyParadisWar";

private const int    SALUTE_SHOTS            = 3;          // shots fired into the sky by each winner
private const int    SALUTE_SHOT_INTERVAL_MS = 700;        // pause between those shots
private const int    SALUTE_DRAW_DELAY_MS    = 600;        // after drawing the handgun, before aiming
private const int    SALUTE_AIM_DELAY_MS     = 600;        // after starting to aim, before the first shot
private const int    SALUTE_STOP_DELAY_MS    = 300;        // after lowering the aim, before the sequence ends
private const float  DECLARATION_START_DELAY_MS = 3000f;   // wait after match start so the intro/spawn is over
private const float  POPUP_REFRESH_MS = 10000f;            // re-show the stat popup this often (0 = only on change)

private static readonly Color POPUP_COLOR = new Color(255, 220, 90);
// ----------------------------------------------------------------------------

private const string KEY_TEAM1_WAR_WINS = "Team1WarWins";      // wars won  (shown in the popup)
private const string KEY_TEAM2_WAR_WINS = "Team2WarWins";
private const string KEY_TEAM1_BATTLES  = "Team1Battles";      // battles won in the current war
private const string KEY_TEAM2_BATTLES  = "Team2Battles";
private const string KEY_PENDING_WINNER = "PendingWinner";     // team (1 or 2) whose war win is still to be declared, 0 = none

private const int PHASE_WAIT   = 0;   // waiting for the match to settle / players to exist
private const int PHASE_SALUTE = 1;   // winners are firing into the sky
private const int PHASE_DONE   = 2;   // losers are crouching, everything stays locked

private IScriptStorage m_storage;
private Events.UpdateCallback m_updateCallback;
private Events.UserMessageCallback m_messageCallback;
private Events.PlayerDeathCallback m_deathCallback;
private IScriptStorage m_coopStorage;

private bool   m_roundCounted;        // this match already credited (or must never be credited)
private bool   m_coopLatched;         // COOPGAMEOVER.cs has restarted (or is about to restart) this match
private float  m_coopHoldMs;          // how long its "no living human" condition has held
private float  m_startTime;           // Game.TotalElapsedGameTime when this match started
private bool   m_warDecided;          // a war was won last match, so this match plays the declaration
private PlayerTeam m_warWinner = PlayerTeam.Independent;
private PlayerTeam m_warLoser  = PlayerTeam.Independent;

private int    m_phase;
private float  m_phaseTime;
private float  m_saluteMinMs;
private float  m_saluteTimeoutMs;
private float  m_popupTimer;
private System.Collections.Generic.List<int> m_saluteIds   = new System.Collections.Generic.List<int>();
private System.Collections.Generic.List<int> m_crouchedIds = new System.Collections.Generic.List<int>();

// ============================== GAME HOOKS ==================================

public void OnStartup()
{
    // Open the shared storage right away: the first access reads a file and may
    // stall for a moment, which is best done at startup (see IGame.GetSharedStorage).
    m_storage = Game.GetSharedStorage(STORAGE_NAME);

    // Never credit a match that was already over before this script began.
    m_roundCounted = Game.IsGameOver;

    m_startTime = Game.TotalElapsedGameTime;
    m_coopLatched = false;
    m_coopHoldMs = 0f;
    if (SKIP_COOP_GAMEOVER)
    {
        m_coopStorage = Game.GetSharedStorage(COOP_STORAGE_NAME);   // open it now, see GetSharedStorage docs
        m_deathCallback = Events.PlayerDeathCallback.Start(OnPlayerDeath);
    }

    // A war won in the previous match is declared in this one. The marker is
    // cleared right away so the declaration only ever happens once per war.
    m_warWinner = GetPendingWinner();
    m_warDecided = (m_warWinner != PlayerTeam.Independent);
    if (m_warDecided)
    {
        SetPendingWinner(PlayerTeam.Independent);
        m_warLoser = (m_warWinner == PlayerTeam.Team1) ? PlayerTeam.Team2 : PlayerTeam.Team1;
        m_phase = PHASE_WAIT;
        m_phaseTime = 0f;
    }

    ShowStats();

    m_updateCallback  = Events.UpdateCallback.Start(OnUpdate, 0);
    m_messageCallback = Events.UserMessageCallback.Start(OnUserMessage);
}

public void OnShutdown()
{
    // Safety net: if the round ended and the update loop never got to see it.
    if (!m_warDecided && !m_roundCounted && Game.IsGameOver)
    {
        m_roundCounted = true;
        CountRound();
    }

    if (m_updateCallback != null)  { m_updateCallback.Stop();  m_updateCallback = null; }
    if (m_messageCallback != null) { m_messageCallback.Stop(); m_messageCallback = null; }
    if (m_deathCallback != null)   { m_deathCallback.Stop();   m_deathCallback = null; }
}

public void OnUpdate(float elapsed)
{
    if (m_warDecided)
    {
        UpdateDeclaration(elapsed);
    }
    else if (!m_roundCounted)
    {
        if (Game.IsGameOver)
        {
            m_roundCounted = true;
            CountRound();
        }
        else
        {
            TrackCoopRestart(elapsed);
        }
    }

    if (POPUP_REFRESH_MS > 0f)
    {
        m_popupTimer += elapsed;
        if (m_popupTimer >= POPUP_REFRESH_MS)
        {
            m_popupTimer = 0f;
            ShowStats();
        }
    }
}

public void OnUserMessage(UserMessageCallbackArgs args)
{
    if (!args.IsCommand || args.Command != "WAR") return;

    IUser user = args.User;
    if (user == null || !(user.IsHost || user.IsModerator)) return;

    string argument = (args.CommandArguments == null) ? "" : args.CommandArguments.Trim().ToUpper();
    if (argument == "RESET")
    {
        SetWarWins(PlayerTeam.Team1, 0);
        SetWarWins(PlayerTeam.Team2, 0);
        SetBattleWins(PlayerTeam.Team1, 0);
        SetBattleWins(PlayerTeam.Team2, 0);
        SetPendingWinner(PlayerTeam.Independent);
        ShowStats();
        Game.ShowChatMessage("War stats reset: " + TEAM1_NAME + " 0 : 0 " + TEAM2_NAME);
    }
}

// ============================ WIN COUNTING ==================================

// Credits the finished battle to the team that was left standing. Reaching
// WINS_TO_TAKE_WAR battle wins wins the war: the war win count goes up, the
// battle tally restarts and the declaration is queued for the next match.
private void CountRound()
{
    // Matches that COOPGAMEOVER.cs restarted with /gameover are never counted.
    if (SKIP_COOP_GAMEOVER && (m_coopLatched || CoopScriptWouldRestart()))
    {
        Announce("WAR: match not counted (restarted by the co-op game over script)");
        return;
    }

    PlayerTeam winner = FindRoundWinner();
    if (winner == PlayerTeam.Independent)
    {
        Announce("WAR: match not counted (no single winning team)");
        return;
    }

    int battles = GetBattleWins(winner) + 1;
    if (battles >= WINS_TO_TAKE_WAR)
    {
        SetWarWins(winner, GetWarWins(winner) + 1);
        SetBattleWins(PlayerTeam.Team1, 0);
        SetBattleWins(PlayerTeam.Team2, 0);
        SetPendingWinner(winner);
        Announce("WAR: " + TeamName(winner) + " wins the battle and takes the WAR! (declaration next match)");
    }
    else
    {
        SetBattleWins(winner, battles);
        Announce("WAR: " + TeamName(winner) + " wins the battle  |  " + TEAM1_NAME + " " + GetBattleWins(PlayerTeam.Team1)
            + " : " + GetBattleWins(PlayerTeam.Team2) + " " + TEAM2_NAME + "  (first to " + WINS_TO_TAKE_WAR + " takes the war)");
    }

    m_popupTimer = 0f;
    ShowStats();
}

private void Announce(string message)
{
    if (ANNOUNCE_RESULTS) Game.ShowChatMessage(message);
}

// The API has no "who won" value, so the winner is the tracked team that still
// has living players when the game is over. Only Team1 and Team2 are tracked:
// living players on any other team are ignored. If both tracked teams (or
// neither) still have living players, there is no winner: PlayerTeam.Independent.
private PlayerTeam FindRoundWinner()
{
    bool team1Alive = false;
    bool team2Alive = false;

    foreach (IPlayer player in Game.GetPlayers())
    {
        if (player == null || player.IsRemoved || player.IsDead) continue;

        PlayerTeam team = player.GetTeam();
        if (team == PlayerTeam.Team1) team1Alive = true;
        else if (team == PlayerTeam.Team2) team2Alive = true;
    }

    if (team1Alive && !team2Alive) return PlayerTeam.Team1;
    if (team2Alive && !team1Alive) return PlayerTeam.Team2;
    return PlayerTeam.Independent;
}

// ================== MATCHES RESTARTED BY COOPGAMEOVER.cs ====================

// COOPGAMEOVER.cs ends the match with /gameover when
//   (A) no living human (non-bot) player exists - it checks every 500 ms and
//       skips the check while SuperDS.cs is splitting P2 (shared storage keys
//       P2_SPLITTING / P2_SPLIT_TIME), or
//   (B) a human player dies within 10 s of the match start.
// Scripts can't observe that command, so the same two conditions are mirrored
// here. m_coopLatched becomes true as soon as either one has held long enough
// for COOPGAMEOVER.cs to have acted on it.

// Condition (A) at this very moment. Same test as COOPGAMEOVER.cs.CheckForHumanPlayers.
private bool CoopScriptWouldRestart()
{
    bool splitting;
    float splitEndTime;
    if (m_coopStorage != null
        && m_coopStorage.TryGetItemBool("P2_SPLITTING", out splitting) && splitting
        && m_coopStorage.TryGetItemFloat("P2_SPLIT_TIME", out splitEndTime)
        && Game.TotalElapsedGameTime < splitEndTime)
        return false;                                // P2 is being split: that script waits

    IPlayer[] players = Game.GetPlayers();
    if (players.Length == 0) return false;

    foreach (IPlayer player in players)
    {
        if (player != null && !player.IsBot && !player.IsDead) return false;   // a living human exists
    }
    return true;
}

// Called every update while the match is still running. Condition (A) has to
// hold for a bit longer than that script's check interval, so a blink at the
// very start of a match (humans not spawned yet) can't flag a normal match.
private void TrackCoopRestart(float elapsed)
{
    if (!SKIP_COOP_GAMEOVER || m_coopLatched) return;

    if (CoopScriptWouldRestart())
    {
        m_coopHoldMs += elapsed;
        if (m_coopHoldMs >= COOP_CHECK_INTERVAL_MS + 100f) m_coopLatched = true;
    }
    else
    {
        m_coopHoldMs = 0f;
    }
}

// Condition (B): a human died (or was removed) shortly after the match started.
public void OnPlayerDeath(IPlayer player)
{
    if (!SKIP_COOP_GAMEOVER || m_coopLatched || player == null || player.IsBot) return;

    if (Game.TotalElapsedGameTime - m_startTime <= COOP_EARLY_DEATH_WINDOW_MS)
        m_coopLatched = true;
}

// ============================== STORAGE =====================================

private int ReadInt(string key)
{
    int value;
    if (m_storage != null && m_storage.TryGetItemInt(key, out value)) return value;
    return 0;
}

private void WriteInt(string key, int value)
{
    if (m_storage != null) m_storage.SetItem(key, value);
}

private int GetWarWins(PlayerTeam team)
{
    return ReadInt(team == PlayerTeam.Team1 ? KEY_TEAM1_WAR_WINS : KEY_TEAM2_WAR_WINS);
}

private void SetWarWins(PlayerTeam team, int wins)
{
    WriteInt(team == PlayerTeam.Team1 ? KEY_TEAM1_WAR_WINS : KEY_TEAM2_WAR_WINS, wins);
}

private int GetBattleWins(PlayerTeam team)
{
    return ReadInt(team == PlayerTeam.Team1 ? KEY_TEAM1_BATTLES : KEY_TEAM2_BATTLES);
}

private void SetBattleWins(PlayerTeam team, int wins)
{
    WriteInt(team == PlayerTeam.Team1 ? KEY_TEAM1_BATTLES : KEY_TEAM2_BATTLES, wins);
}

// The team whose war win has not been declared yet, or Independent if none.
private PlayerTeam GetPendingWinner()
{
    int value = ReadInt(KEY_PENDING_WINNER);
    if (value == 1) return PlayerTeam.Team1;
    if (value == 2) return PlayerTeam.Team2;
    return PlayerTeam.Independent;
}

private void SetPendingWinner(PlayerTeam team)
{
    WriteInt(KEY_PENDING_WINNER, team == PlayerTeam.Team1 ? 1 : (team == PlayerTeam.Team2 ? 2 : 0));
}

private string TeamName(PlayerTeam team)
{
    return (team == PlayerTeam.Team1) ? TEAM1_NAME : TEAM2_NAME;
}

// =============================== POPUP ======================================

private void ShowStats()
{
    int wars1 = GetWarWins(PlayerTeam.Team1);
    int wars2 = GetWarWins(PlayerTeam.Team2);
    string text = "WAR WINS  |  " + TEAM1_NAME + " " + wars1 + " : " + wars2 + " " + TEAM2_NAME;

    // In the declaration match, say who just took the war.
    if (m_warDecided)
        text = TeamName(m_warWinner).ToUpper() + " WINS THE WAR!  |  " + text;

    Game.ShowPopupMessage(text, POPUP_COLOR);
}

// ============================ DECLARATION ===================================

private void UpdateDeclaration(float elapsed)
{
    IPlayer[] players = Game.GetPlayers();

    // Input must stay off for the whole match (also catches respawns / late joiners).
    // PlayerCommands only run while neither a user nor an AI controls the player.
    LockInput(players);

    m_phaseTime += elapsed;

    switch (m_phase)
    {
        case PHASE_WAIT:
            if (m_phaseTime >= DECLARATION_START_DELAY_MS && HasLivingPlayerOnTeam(players, m_warWinner, m_warLoser))
                BeginSalute(players);
            break;

        case PHASE_SALUTE:
            if (m_phaseTime >= m_saluteTimeoutMs || (m_phaseTime >= m_saluteMinMs && SaluteFinished()))
                BeginCrouch(players);
            break;

        case PHASE_DONE:
            // Losers that show up after the sequence also end up crouching.
            CrouchLosers(players);
            break;
    }
}

private void LockInput(IPlayer[] players)
{
    foreach (IPlayer player in players)
    {
        if (player != null && !player.IsRemoved && player.IsInputEnabled)
            player.SetInputEnabled(false);
    }
}

private bool HasLivingPlayerOnTeam(IPlayer[] players, PlayerTeam teamA, PlayerTeam teamB)
{
    foreach (IPlayer player in players)
    {
        if (player == null || player.IsRemoved || player.IsDead) continue;
        PlayerTeam team = player.GetTeam();
        if (team == teamA || team == teamB) return true;
    }
    return false;
}

private bool IsLivingOnTeam(IPlayer player, PlayerTeam team)
{
    return player != null && !player.IsRemoved && !player.IsDead && player.GetTeam() == team;
}

// ---- step 1: winners aim at the sky and fire -------------------------------

private void BeginSalute(IPlayer[] players)
{
    m_saluteIds.Clear();

    foreach (IPlayer player in players)
    {
        if (!IsLivingOnTeam(player, m_warWinner)) continue;

        player.ClearCommandQueue();
        EnsureHandgunWithAmmo(player);
        QueueSalute(player);
        m_saluteIds.Add(player.UniqueID);
    }

    // The sequence can't be quicker than the delays queued in it. The timeout
    // keeps one stuck player from blocking the crouch step forever.
    m_saluteMinMs = SALUTE_DRAW_DELAY_MS + SALUTE_AIM_DELAY_MS + (SALUTE_SHOTS * SALUTE_SHOT_INTERVAL_MS) + SALUTE_STOP_DELAY_MS;
    m_saluteTimeoutMs = m_saluteMinMs + 5000f;

    m_phase = PHASE_SALUTE;
    m_phaseTime = 0f;
}

// Gives a pistol if the player has no handgun, and tops up the magazine if it
// can't cover the salute.
private void EnsureHandgunWithAmmo(IPlayer player)
{
    HandgunWeaponItem handgun = player.CurrentSecondaryWeapon;
    if (handgun.WeaponItemType != WeaponItemType.Handgun || handgun.WeaponItem == WeaponItem.NONE)
    {
        player.GiveWeaponItem(WeaponItem.PISTOL);
        return;
    }

    if (handgun.CurrentAmmo < SALUTE_SHOTS)
        player.SetCurrentSecondaryWeaponAmmo(System.Math.Max(SALUTE_SHOTS, handgun.MagSize), handgun.SpareMags);
}

private PlayerCommand Cmd(PlayerCommandType type, int delayMs)
{
    PlayerCommand command = new PlayerCommand(type);
    command.DelayTime = delayMs;
    return command;
}

private void QueueSalute(IPlayer player)
{
    Vector2 position = player.GetWorldPosition();
    int face = (player.FacingDirection < 0) ? -1 : 1;
    Vector2 sky = new Vector2(position.X + face * 40f, position.Y + 400f);   // world Y points up

    player.AddCommand(Cmd(PlayerCommandType.DrawHandgun, SALUTE_DRAW_DELAY_MS));
    player.AddCommand(new PlayerCommand(PlayerCommandType.StartAimAt, sky, SALUTE_AIM_DELAY_MS));
    for (int i = 0; i < SALUTE_SHOTS; i++)
        player.AddCommand(Cmd(PlayerCommandType.AttackOnce, SALUTE_SHOT_INTERVAL_MS));
    player.AddCommand(Cmd(PlayerCommandType.StopAim, SALUTE_STOP_DELAY_MS));
}

// True once every winner that took part has run through its whole command queue
// (or is dead / gone).
private bool SaluteFinished()
{
    foreach (int id in m_saluteIds)
    {
        IPlayer player = Game.GetPlayer(id);
        if (player == null || player.IsRemoved || player.IsDead) continue;
        if (player.PerformedCommandCount < player.TotalCommandCount) return false;
    }
    return true;
}

// ---- step 2: losers crouch, then everything stays as is --------------------

private void BeginCrouch(IPlayer[] players)
{
    m_crouchedIds.Clear();
    CrouchLosers(players);
    m_phase = PHASE_DONE;
}

private void CrouchLosers(IPlayer[] players)
{
    foreach (IPlayer player in players)
    {
        if (!IsLivingOnTeam(player, m_warLoser)) continue;
        if (m_crouchedIds.Contains(player.UniqueID)) continue;

        player.ClearCommandQueue();
        player.AddCommand(new PlayerCommand(PlayerCommandType.StartCrouch));
        m_crouchedIds.Add(player.UniqueID);
    }
}