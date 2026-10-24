// ============================================================================
//  MARLEY vs PARADIS  -  WAR TRACKER
//  Superfighters Deluxe map script (paste into the map editor's script window)
//
//  - Every finished match (battle) is credited to Team 1 or Team 2 (nobody else
//    is tracked; draws are not counted, and neither is a match ended by a
//    GameOverTrigger whose text is NOBODY_WIN_TEXT, "NOBODY WIN!").
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
private const int    WINS_TO_TAKE_WAR = 2;                 // the first team to win this many battles wins the war
private const string TEAM1_NAME      = "Marley";           // PlayerTeam.Team1
private const string TEAM2_NAME      = "Paradis";          // PlayerTeam.Team2

// A match ended by a GameOverTrigger with this text is never counted (case-insensitive).
private const string NOBODY_WIN_TEXT = "NOBODY WIN!";

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

private bool   m_roundCounted;        // this match already credited (or must never be credited)
private bool   m_nobodyWin;           // a "NOBODY WIN!" GameOverTrigger fired this match
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

    m_nobodyWin = false;
    HookNobodyWinTriggers();

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
}

public void OnUpdate(float elapsed)
{
    if (m_warDecided)
    {
        UpdateDeclaration(elapsed);
    }
    else if (!m_roundCounted && Game.IsGameOver)
    {
        m_roundCounted = true;
        CountRound();
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
    if (m_nobodyWin) return;                        // ended by a "NOBODY WIN!" trigger: not counted

    PlayerTeam winner = FindRoundWinner();
    if (winner == PlayerTeam.Independent) return;   // draw / not a Team1-Team2 result

    int battles = GetBattleWins(winner) + 1;
    if (battles >= WINS_TO_TAKE_WAR)
    {
        SetWarWins(winner, GetWarWins(winner) + 1);
        SetBattleWins(PlayerTeam.Team1, 0);
        SetBattleWins(PlayerTeam.Team2, 0);
        SetPendingWinner(winner);
    }
    else
    {
        SetBattleWins(winner, battles);
    }

    m_popupTimer = 0f;
    ShowStats();
}

// The API has no "who won" value, so the winner is the tracked team that still
// has living players when the game is over. A draw (nobody alive, or both teams
// alive, or any other team still alive) returns PlayerTeam.Independent.
private PlayerTeam FindRoundWinner()
{
    bool team1Alive = false;
    bool team2Alive = false;
    bool otherAlive = false;

    foreach (IPlayer player in Game.GetPlayers())
    {
        if (player == null || player.IsRemoved || player.IsDead) continue;

        PlayerTeam team = player.GetTeam();
        if (team == PlayerTeam.Team1) team1Alive = true;
        else if (team == PlayerTeam.Team2) team2Alive = true;
        else otherAlive = true;
    }

    if (otherAlive) return PlayerTeam.Independent;
    if (team1Alive && !team2Alive) return PlayerTeam.Team1;
    if (team2Alive && !team1Alive) return PlayerTeam.Team2;
    return PlayerTeam.Independent;
}

// ====================== "NOBODY WIN!" GAME OVER =============================

// The API can't tell which GameOverTrigger ended the match, but a trigger runs
// its script method when it fires. So every GameOverTrigger whose text is
// NOBODY_WIN_TEXT is pointed at OnNobodyWin(), which marks the match as not
// countable. You can also set that trigger's "Script method" to OnNobodyWin in
// the editor yourself.
private void HookNobodyWinTriggers()
{
    foreach (IObjectGameOverTrigger trigger in Game.GetObjects<IObjectGameOverTrigger>())
    {
        if (trigger == null || !IsNobodyWinText(trigger.GetGameOverText())) continue;

        string method = trigger.GetScriptMethod();
        if (string.IsNullOrEmpty(method))
        {
            trigger.SetScriptMethod("OnNobodyWin");
        }
        else if (method != "OnNobodyWin")
        {
            // Don't override a method the map already uses; it has to call OnNobodyWin(args) itself.
            Game.WriteToConsole("War script: the '" + NOBODY_WIN_TEXT + "' trigger already runs '" + method
                + "'. Call OnNobodyWin(args) from that method so the match is not counted.");
        }
    }
}

public void OnNobodyWin(TriggerArgs args)
{
    IObjectGameOverTrigger trigger = (args == null) ? null : args.Caller as IObjectGameOverTrigger;
    if (trigger != null && !IsNobodyWinText(trigger.GetGameOverText())) return;   // some other game over trigger

    m_nobodyWin = true;
}

private bool IsNobodyWinText(string text)
{
    return text != null && string.Equals(text.Trim(), NOBODY_WIN_TEXT, System.StringComparison.OrdinalIgnoreCase);
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