// Vinland - Norse warrior theme with enhanced combat abilities
// P1 (ThorsBonduk) - Speed and agility focused with slowmo power
// P2 (ThorsHateli) - Strength and durability focused melee fighter

private const int HIT_POINT = 28;
private const int BLOCKS_REQUIRED = 2; // Number of blocks required while crouching to spawn troops
private const int MAX_MIGHT_PER_SIDE = 200; // Maximum total might (energy cost) of deployed troops per side

private IPlayer p1 = null;
private IPlayer p2 = null;

// Super troops (one-time spawn)
private IPlayer bjorn = null;
private IPlayer thorsfin = null;

// Troop lists for each leader with might tracking
private Dictionary<int, int> p1TroopMight = new Dictionary<int, int>(); // UniqueID -> Might
private Dictionary<int, int> p2TroopMight = new Dictionary<int, int>(); // UniqueID -> Might
private List<IPlayer> p1Troops = new List<IPlayer>();
private List<IPlayer> p2Troops = new List<IPlayer>();

// Track guard mode for each leader
private bool p1GuardEnabled = false;
private bool p2GuardEnabled = false;

// Track player facing directions for P1 backstab detection
private Dictionary<int, int> playerFacingDirections = new Dictionary<int, int>();

// Track players killed by P2's jump attack for respawning
private class KilledPlayerData
{
    public int DeadPlayerID;
    public IProfile Profile;
    public PlayerTeam Team;
    public PlayerModifiers Modifiers;
    public IUser User; // For human players
    public float KillTime;
    public string BotName; // Bug #1 - store name
    public BotBehavior BotBehavior; // Store bot behavior
    public BotBehaviorSet BotBehaviorSet; // Bug #4 - store bot behavior set
    public bool NametagVisible; // Store nametag visibility
    public bool StatusBarsVisible; // Store status bars visibility
    public CameraFocusMode CameraFocusMode; // Store camera focus mode
    public bool IsP1; // Bug #5 - track if this is P1
    public bool IsBjorn; // Bug #5 - track if this is Bjorn
    public bool IsP2; // Bug #5 - track if this is P2
    public bool IsThorsFin; // Bug #5 - track if this is ThorsFin
}
private List<KilledPlayerData> p2JumpKilledPlayers = new List<KilledPlayerData>();

// Track players that need input re-enabled after fall (bug #6)
private List<int> playersToReEnableInput = new List<int>();

// Random generator
private Random rnd = new Random();

public void OnStartup()
{
    // Store player references at startup
    IPlayer[] players = Game.GetPlayers();
    p1 = players.Length >= 1 ? players[0] : null;
    p2 = players.Length >= 2 ? players[1] : null;
    
    if (p1 == null || p2 == null) return; // Need both players
    
    // Set up P1 (ThorsBonduk) - Speed warrior with slowmo
    SetupP1();
    
    // Set up P2 (ThorsHateli) - Strength warrior
    SetupP2();
    
    // Spawn super troops (one-time only)
    SpawnBjorn();
    SpawnThorsFin();
    
    // Spawn initial troops for both leaders
    // SpawnInitialTroops();
    
    // Set up slowmo timer for P1 (every 12 seconds)
    IObjectTimerTrigger slowmoTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    slowmoTimer.SetIntervalTime(12000); // 12 seconds
    slowmoTimer.SetRepeatCount(0); // Infinite repeats
    slowmoTimer.SetScriptMethod("GiveP1Slowmo");
    slowmoTimer.Trigger();
    
    // Set up ammo refill timer
    IObjectTimerTrigger ammoTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    ammoTimer.SetIntervalTime(20000); // 20 seconds
    ammoTimer.SetRepeatCount(0); // Infinite repeats
    ammoTimer.SetScriptMethod("RefillAmmo");
    ammoTimer.Trigger();
    
    // Set up player key input callback for guard toggle and troop spawning
    Events.PlayerKeyInputCallback.Start(OnPlayerKeyInput);
    
    // Set up player death callback for auto-gib and might cleanup
    // Events.PlayerDeathCallback.Start(OnTroopDeath);
    
    // Set up player facing direction tracking timer (every 30ms)
    IObjectTimerTrigger facingTrackingTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    facingTrackingTimer.SetIntervalTime(30); // 30ms for precise tracking
    facingTrackingTimer.SetRepeatCount(0); // Infinite repeats
    facingTrackingTimer.SetScriptMethod("UpdatePlayerFacingDirections");
    facingTrackingTimer.Trigger();
    
    // Set up update callback for Bjorn's low HP strength boost
    Events.UpdateCallback.Start(OnUpdate, 100); // Check every 100ms
    
    // Set up melee hit callback for P1 backstab mechanics
    Events.PlayerMeleeActionCallback.Start(OnPlayerMeleeAction);
    
    // Set up damage callback for P1 backstab projectile damage
    Events.PlayerDamageCallback.Start(OnPlayerDamage);
}

public void OnPlayerKeyInput(IPlayer player, VirtualKeyInfo[] keyInfos)
{
    if (player == null) return;
    
    foreach (VirtualKeyInfo keyInfo in keyInfos)
    {
        // Check for SHEATHE key to toggle guard mode
        if (keyInfo.Event == VirtualKeyEvent.Pressed && keyInfo.Key == VirtualKey.SHEATHE)
        {
            if (p1 != null && player.UniqueID == p1.UniqueID)
            {
                p1GuardEnabled = !p1GuardEnabled;
                UpdateTroopGuards(p1Troops, p1, p1GuardEnabled);
                Game.ShowChatMessage("P1 Guard Mode: " + (p1GuardEnabled ? "ON" : "OFF"), Color.Yellow);
            }
            else if (p2 != null && player.UniqueID == p2.UniqueID)
            {
                p2GuardEnabled = !p2GuardEnabled;
                UpdateTroopGuards(p2Troops, p2, p2GuardEnabled);
                Game.ShowChatMessage("P2 Guard Mode: " + (p2GuardEnabled ? "ON" : "OFF"), Color.Yellow);
            }
        }
        
        // Check for BLOCK key while crouching to spawn troops
        if (keyInfo.Event == VirtualKeyEvent.Pressed && keyInfo.Key == VirtualKey.RELOAD)
        {
            if (p1 != null && player.UniqueID == p1.UniqueID)
            {
                SpawnTroopsForLeader(p1, p1Troops);
            }
            else if (p2 != null && player.UniqueID == p2.UniqueID)
            {
                SpawnTroopsForLeader(p2, p2Troops);
            }
        }
    }
}

private void SetupP1()
{
    if (p1 == null) return;
    
    // Remove existing weapons
    p1.RemoveWeaponItemType(WeaponItemType.Rifle);
    p1.RemoveWeaponItemType(WeaponItemType.Handgun);
    p1.RemoveWeaponItemType(WeaponItemType.Melee);
    p1.RemoveWeaponItemType(WeaponItemType.Thrown);
    p1.RemoveWeaponItemType(WeaponItemType.Powerup);
    
    // Give P1 weapons: katana, bow, and initial slowmo
    p1.GiveWeaponItem(WeaponItem.KATANA);
    p1.GiveWeaponItem(WeaponItem.BOW);
    p1.GiveWeaponItem(WeaponItem.SLOWMO_5);
    
    // Set P1 modifiers - 2x speed, 2.5x energy, 1.1x energy regen, 1.2x size
    PlayerModifiers p1Mods = p1.GetModifiers();
    p1Mods.RunSpeedModifier *= 2.0f;
    p1Mods.SprintSpeedModifier *= 2.0f;
    p1Mods.MaxEnergy = (int)(p1Mods.MaxEnergy * 1.2f);
    p1Mods.CurrentEnergy = (int)(p1Mods.CurrentEnergy * 1.2f);
    // p1Mods.EnergyRechargeModifier *= 1.05f;
    // p1Mods.SizeModifier = 1.1f;
    p1.SetModifiers(p1Mods);
    
    // Set P1 profile - ThorsBonduk
    p1.SetProfile(new IProfile()
    {
        Name = "ThorsBonduk",
        Gender = Gender.Female,
        Skin = new IProfileClothingItem("Normal_fem", "Skin3", "ClothingLightGreen"),
        Head = new IProfileClothingItem("Buzzcut", "ClothingLightYellow"),
        ChestOver = new IProfileClothingItem("Poncho_fem", "ClothingDarkGray", "ClothingLightGray"),
        ChestUnder = new IProfileClothingItem("Sweater_fem", "ClothingOrange"),
        Legs = new IProfileClothingItem("CamoPants_fem", "ClothingOrange", "ClothingDarkGray"),
        Feet = new IProfileClothingItem("ShoesBlack", "ClothingBrown"),
        Accesory = new IProfileClothingItem("Moustache", "ClothingLightYellow"),
    });
}

private void SetupP2()
{
    if (p2 == null) return;
    
    // Remove existing weapons
    p2.RemoveWeaponItemType(WeaponItemType.Rifle);
    p2.RemoveWeaponItemType(WeaponItemType.Handgun);
    p2.RemoveWeaponItemType(WeaponItemType.Melee);
    p2.RemoveWeaponItemType(WeaponItemType.Thrown);
    p2.RemoveWeaponItemType(WeaponItemType.Powerup);
    
    // Give P2 weapons: katana and bow
    p2.GiveWeaponItem(WeaponItem.KATANA);
    p2.GiveWeaponItem(WeaponItem.BOW);
    
    // Set P2 modifiers - 2.5x health, 2x melee damage, 2x melee force, 1.4x size
    PlayerModifiers p2Mods = p2.GetModifiers();
    p2Mods.MaxHealth = (int)(p2Mods.MaxHealth * 2.0f);
    p2Mods.CurrentHealth = (int)(p2Mods.CurrentHealth * 2.0f);
    p2Mods.MeleeDamageDealtModifier *= 1.5f;
    p2Mods.MeleeForceModifier *= 1.8f;
    p2Mods.SizeModifier = 1.2f;
    p2.SetModifiers(p2Mods);
    
    // Set P2 profile - ThorsHateli
    p2.SetProfile(new IProfile()
    {
        Name = "ThorsHateli",
        Gender = Gender.Male,
        Skin = new IProfileClothingItem("Normal", "Skin5", "ClothingLightGray"),
        Head = new IProfileClothingItem("Hood", "ClothingDarkGray"),
        ChestOver = new IProfileClothingItem("Apron", "ClothingGray"),
        ChestUnder = new IProfileClothingItem("ShirtWithTie", "ClothingGray", "ClothingGray"),
        Waist = new IProfileClothingItem("SmallBelt", "ClothingLightGray", "ClothingLightGray"),
        Legs = new IProfileClothingItem("Skirt", "ClothingGray"),
        Feet = new IProfileClothingItem("RidingBoots", "ClothingGray"),
    });
}

private void SpawnBjorn()
{
    if (p1 == null) return;
    
    // Get spawn position - use random path node if available, otherwise use P1's position
    Vector2 spawnPos = p1.GetWorldPosition();
    
    IObjectPathNode[] pathNodes = Game.GetObjects<IObjectPathNode>().Where(path => 
        path.GetNodeEnabled() && 
        !path.GetIsElevatorNode() &&
        (path.GetPathNodeType() == PathNodeType.Ground || path.GetPathNodeType() == PathNodeType.Platform)
    ).ToArray();
    
    if (pathNodes.Length > 0)
    {
        // Use random path node
        int randomIndex = rnd.Next(0, pathNodes.Length);
        spawnPos = pathNodes[randomIndex].GetWorldPosition();
    }
    
    bjorn = Game.CreatePlayer(spawnPos);
    
    if (bjorn == null) return;
    
    // Set team same as P1
    bjorn.SetTeam(p1.GetTeam());
    bjorn.SetBotName("Bjorn");
    
    // Remove all default weapons
    bjorn.RemoveWeaponItemType(WeaponItemType.Rifle);
    bjorn.RemoveWeaponItemType(WeaponItemType.Handgun);
    bjorn.RemoveWeaponItemType(WeaponItemType.Melee);
    bjorn.RemoveWeaponItemType(WeaponItemType.Thrown);
    bjorn.RemoveWeaponItemType(WeaponItemType.Powerup);
    
    // Give weapons: katana and bow (same as leaders)
    bjorn.GiveWeaponItem(WeaponItem.AXE);
    
    // Set Bjorn modifiers
    PlayerModifiers bjornMods = bjorn.GetModifiers();
    bjornMods.MaxHealth = (int)(bjornMods.MaxHealth * 1.5f);
    bjornMods.CurrentHealth = (int)(bjornMods.CurrentHealth * 1.5f);
    bjornMods.MeleeDamageDealtModifier *= 1.3f;
    bjornMods.MeleeForceModifier *= 1.3f;
    bjornMods.SizeModifier = 1.13f;
    bjorn.SetModifiers(bjornMods);
    
    // Set bot behavior (good AI)
    bjorn.SetBotBehavior(new BotBehavior(true, PredefinedAIType.Hulk));
    BotBehaviorSet bS = bjorn.GetBotBehaviorSet();
    bS.SearchItems = 0;
    bjorn.SetBotBehaviorSet(bS);

    // Set Bjorn profile
    bjorn.SetProfile(new IProfile()
    {
        Name = "Bjorn",
        Gender = Gender.Male,
        Skin = new IProfileClothingItem("Normal", "Skin2", "ClothingLightGreen"),
        Head = new IProfileClothingItem("Headband", "ClothingGray"),
        ChestOver = new IProfileClothingItem("Poncho2", "ClothingBrown", "ClothingLightGray"),
        ChestUnder = new IProfileClothingItem("LeatherJacket", "ClothingDarkGray", "ClothingLightBrown"),
        Waist = new IProfileClothingItem("CombatBelt", "ClothingBrown"),
        Legs = new IProfileClothingItem("Skirt", "ClothingLightBrown"),
        Feet = new IProfileClothingItem("RidingBoots", "ClothingDarkRed"),
        Accesory = new IProfileClothingItem("Mask", "ClothingDarkOrange"),
    });
    
    // Show nametag and status bars
    bjorn.SetNametagVisible(true);
    bjorn.SetStatusBarsVisible(true);
}

private void SpawnThorsFin()
{
    if (p2 == null) return;
    
    // Get spawn position - use random path node if available, otherwise use P2's position
    Vector2 spawnPos = p2.GetWorldPosition();
    
    IObjectPathNode[] pathNodes = Game.GetObjects<IObjectPathNode>().Where(path => 
        path.GetNodeEnabled() && 
        !path.GetIsElevatorNode() &&
        (path.GetPathNodeType() == PathNodeType.Ground || path.GetPathNodeType() == PathNodeType.Platform)
    ).ToArray();
    
    if (pathNodes.Length > 0)
    {
        // Use random path node
        int randomIndex = rnd.Next(0, pathNodes.Length);
        spawnPos = pathNodes[randomIndex].GetWorldPosition();
    }
    
    thorsfin = Game.CreatePlayer(spawnPos);
    
    if (thorsfin == null) return;
    
    // Set team same as P2
    thorsfin.SetTeam(p2.GetTeam());
    thorsfin.SetBotName("Thors Fin");
    
    // Remove all default weapons
    thorsfin.RemoveWeaponItemType(WeaponItemType.Rifle);
    thorsfin.RemoveWeaponItemType(WeaponItemType.Handgun);
    thorsfin.RemoveWeaponItemType(WeaponItemType.Melee);
    thorsfin.RemoveWeaponItemType(WeaponItemType.Thrown);
    thorsfin.RemoveWeaponItemType(WeaponItemType.Powerup);
    
    thorsfin.GiveWeaponItem(WeaponItem.KNIFE);
    
    // Set ThorsFin modifiers
    PlayerModifiers thorsfinMods = thorsfin.GetModifiers();
    thorsfinMods.SizeModifier = 1.045f;
    thorsfinMods.RunSpeedModifier *= 2.0f;
    thorsfinMods.RunSpeedModifier *= 2.0f;
    thorsfinMods.SprintSpeedModifier *= 2.0f;
    thorsfinMods.MaxHealth = (int)(thorsfinMods.MaxHealth * 1.3f);
    thorsfinMods.CurrentHealth = (int)(thorsfinMods.CurrentHealth * 1.3f);
    thorsfinMods.MaxEnergy = (int)(thorsfinMods.MaxEnergy * 2f);
    thorsfinMods.CurrentEnergy = (int)(thorsfinMods.CurrentEnergy * 2f);
    thorsfinMods.EnergyRechargeModifier *= 1.5f;
    thorsfinMods.MeleeDamageDealtModifier *= 1.6f;
    thorsfinMods.ProjectileDamageDealtModifier *= 1.6f;
    thorsfinMods.MeleeForceModifier *= 1.15f;
    thorsfin.SetModifiers(thorsfinMods);
    
    // Set bot behavior (good AI)
    thorsfin.SetBotBehavior(new BotBehavior(true, PredefinedAIType.BotA));
    BotBehaviorSet bS = thorsfin.GetBotBehaviorSet();
    bS.SearchItems = 0;
    thorsfin.SetBotBehaviorSet(bS);
    
    // Set ThorsFin profile
    thorsfin.SetProfile(new IProfile()
    {
        Name = "ThorsFin",
        Gender = Gender.Female,
        Skin = new IProfileClothingItem("Normal_fem", "Skin4", "ClothingLightGray"),
        Head = new IProfileClothingItem("Buzzcut", "ClothingDarkGray"),
        ChestOver = new IProfileClothingItem("Poncho2_fem", "ClothingYellow", "ClothingGray"),
        ChestUnder = new IProfileClothingItem("StuddedLeatherSuit_fem", "ClothingYellow"),
        Waist = new IProfileClothingItem("Belt_fem", "ClothingYellow", "ClothingYellow"),
        Legs = new IProfileClothingItem("Skirt_fem", "ClothingYellow"),
        Feet = new IProfileClothingItem("RidingBoots", "ClothingDarkBrown"),
        Accesory = new IProfileClothingItem("Scarf", "ClothingLightGray"),
    });
    
    // Show nametag and status bars
    thorsfin.SetNametagVisible(true);
    thorsfin.SetStatusBarsVisible(true);

    thorsfin.SetSpeedBoostTime(999999);
}

public void GiveP1Slowmo(TriggerArgs args)
{
    // Give P1 slowmo powerup every 12 seconds if alive and doesn't have one
    if (p1 != null && !p1.IsDead)
    {
        // Check if P1 already has a powerup item in the powerup slot
        PowerupWeaponItem currentPowerup = p1.CurrentPowerupItem;
        if (currentPowerup.WeaponItem == WeaponItem.NONE)
        {
            p1.GiveWeaponItem(WeaponItem.SLOWMO_5);
        }
    }
}

private void SpawnInitialTroops()
{
    // Initial troops are spawned automatically on startup
    SpawnTroopsForLeader(p1, p1Troops);
    SpawnTroopsForLeader(p2, p2Troops);
}

private void SpawnTroop(string troopType, IPlayer leader, Vector2 position, List<IPlayer> troopList, Dictionary<int, int> troopMightDict, int mightCost)
{
    IPlayer troop = Game.CreatePlayer(position);
    if (troop == null) return;
    
    // Set team
    troop.SetTeam(leader.GetTeam());
    
    // Hide troop UI elements
    troop.SetNametagVisible(false);
    troop.SetStatusBarsVisible(false);
    troop.SetCameraSecondaryFocusMode(CameraFocusMode.Ignore);
    
    // Configure based on troop type
    ConfigureTroop(troop, troopType, leader);
    
    // Add to troop list and track might
    troopList.Add(troop);
    troopMightDict[troop.UniqueID] = mightCost;
    
    // Set guard behavior if enabled
    bool guardEnabled = (leader.UniqueID == p1.UniqueID) ? p1GuardEnabled : p2GuardEnabled;
    if (guardEnabled)
    {
        troop.SetGuardTarget(leader);
    }
}

private void ConfigureTroop(IPlayer troop, string troopType, IPlayer leader)
{
    // Remove all default weapons
    troop.RemoveWeaponItemType(WeaponItemType.Rifle);
    troop.RemoveWeaponItemType(WeaponItemType.Handgun);
    troop.RemoveWeaponItemType(WeaponItemType.Melee);
    troop.RemoveWeaponItemType(WeaponItemType.Thrown);
    troop.RemoveWeaponItemType(WeaponItemType.Powerup);
    
    PlayerModifiers mods = troop.GetModifiers();
    
    // Random skin selection (Skin1 to Skin4)
    string[] skins = { "Skin1", "Skin2", "Skin3", "Skin4" };
    string randomSkin = skins[rnd.Next(skins.Length)];
    
    // Team color for ChestOver
    string teamColor = (leader.UniqueID == p1.UniqueID) ? "ClothingDarkOrange" : "ClothingDarkBlue";
    
    switch (troopType)
    {
        case "Stickman":
            troop.GiveWeaponItem(WeaponItem.CUESTICK);
            mods.MaxHealth = (int)(1 * HIT_POINT);
            mods.CurrentHealth = (int)(1 * HIT_POINT);
            // mods.SizeModifier = 0.8f;
            troop.SetProfile(new IProfile()
            {
                Name = "Stickman",
                Gender = Gender.Female,
                Skin = new IProfileClothingItem("Normal_fem", randomSkin, "ClothingLightGreen"),
                Head = new IProfileClothingItem("WoolCap", "ClothingGray"),
                ChestOver = new IProfileClothingItem("Apron_fem", teamColor),
                Hands = new IProfileClothingItem("SafetyGloves_fem", "ClothingGray"),
                Legs = new IProfileClothingItem("PantsBlack_fem", "ClothingGray"),
                Feet = new IProfileClothingItem("ShoesBlack", "ClothingGray"),
            });
            SetBotBehavior(troop, PredefinedAIType.BotD);
            break;
            
        case "Knifeman":
            troop.GiveWeaponItem(WeaponItem.KNIFE);
            mods.MaxHealth = (int)(2 * HIT_POINT);
            mods.CurrentHealth = (int)(2 * HIT_POINT);
            // mods.SizeModifier = 0.8f;
            troop.SetProfile(new IProfile()
            {
                Name = "Knifeman",
                Gender = Gender.Female,
                Skin = new IProfileClothingItem("Normal_fem", randomSkin, "ClothingLightGreen"),
                Head = new IProfileClothingItem("SpikedHelmet", "ClothingGray"),
                ChestOver = new IProfileClothingItem("Apron_fem", teamColor),
                ChestUnder = new IProfileClothingItem("ShirtWithBowtie_fem", "ClothingGray", "ClothingLightGray"),
                Hands = new IProfileClothingItem("SafetyGloves_fem", "ClothingGray"),
                Legs = new IProfileClothingItem("PantsBlack_fem", "ClothingGray"),
                Feet = new IProfileClothingItem("ShoesBlack", "ClothingGray"),
                Accesory = new IProfileClothingItem("Scarf", "ClothingLightGray"),
            });
            SetBotBehavior(troop, PredefinedAIType.BotD);
            break;
            
        case "Bowman":
            troop.GiveWeaponItem(WeaponItem.BOW);
            mods.MaxHealth = (int)(1 * HIT_POINT);
            mods.CurrentHealth = (int)(1 * HIT_POINT);
            // mods.SizeModifier = 0.8f;
            mods.RunSpeedModifier *= 1.3f;
            mods.SprintSpeedModifier *= 1.3f;
            troop.SetProfile(new IProfile()
            {
                Name = "Bowman",
                Gender = Gender.Female,
                Skin = new IProfileClothingItem("Normal_fem", randomSkin, "ClothingLightGreen"),
                Head = new IProfileClothingItem("StylishHat", "ClothingGray", "ClothingLightGray"),
                ChestOver = new IProfileClothingItem("Apron_fem", teamColor),
                ChestUnder = new IProfileClothingItem("ShirtWithBowtie_fem", "ClothingGray", "ClothingLightGray"),
                Legs = new IProfileClothingItem("PantsBlack_fem", "ClothingGray"),
                Feet = new IProfileClothingItem("ShoesBlack", "ClothingBrown"),
            });
            BotBehaviorSet bsBowman = SetBotBehavior(troop, PredefinedAIType.BotC);
            bsBowman.MeleeUsage = false;
            troop.SetBotBehaviorSet(bsBowman);
            break;
            
        case "Knight":
            troop.GiveWeaponItem(WeaponItem.MACHETE);
            mods.MaxHealth = (int)(2 * HIT_POINT);
            mods.CurrentHealth = (int)(2 * HIT_POINT);
            mods.SizeModifier = (int)(1.05f);
            troop.SetProfile(new IProfile()
            {
                Name = "Knight",
                Gender = Gender.Male,
                Skin = new IProfileClothingItem("Normal", randomSkin, "ClothingLightGreen"),
                ChestOver = new IProfileClothingItem("Apron", teamColor),
                ChestUnder = new IProfileClothingItem("BodyArmor", "ClothingGray"),
                Hands = new IProfileClothingItem("Gloves", "ClothingGray"),
                Legs = new IProfileClothingItem("CamoPants", "ClothingGray", "ClothingDarkGray"),
                Feet = new IProfileClothingItem("ShoesBlack", "ClothingBrown"),
                Accesory = new IProfileClothingItem("Balaclava", "ClothingGray"),
            });
            SetBotBehavior(troop, PredefinedAIType.BotB);
            break;
            
        case "Axeman":
            troop.GiveWeaponItem(WeaponItem.AXE);
            mods.MaxHealth = (int)(4 * HIT_POINT);
            mods.CurrentHealth = (int)(4 * HIT_POINT);
            mods.RunSpeedModifier *= 0.7f;
            mods.SprintSpeedModifier *= 0.7f;
            mods.SizeModifier = 1.2f;
            troop.SetProfile(new IProfile()
            {
                Name = "Axeman",
                Gender = Gender.Male,
                Skin = new IProfileClothingItem("Normal", randomSkin, "ClothingLightGreen"),
                Head = new IProfileClothingItem("Afro", "ClothingDarkGray"),
                ChestOver = new IProfileClothingItem("KevlarVest", teamColor),
                Hands = new IProfileClothingItem("SafetyGloves", "ClothingGray"),
                Legs = new IProfileClothingItem("Skirt", "ClothingGray"),
                Feet = new IProfileClothingItem("RidingBoots", "ClothingGray"),
                Accesory = new IProfileClothingItem("ClownMakeup", "ClothingGray"),
            });
            SetBotBehavior(troop, PredefinedAIType.BotC);
            break;
            
        case "FireBowman":
            troop.GiveWeaponItem(WeaponItem.BOW);
            troop.GiveWeaponItem(WeaponItem.FIREAMMO);
            mods.MaxHealth = (int)(1 * HIT_POINT);
            mods.CurrentHealth = (int)(1 * HIT_POINT);
            mods.RunSpeedModifier *= 1.3f;
            mods.SprintSpeedModifier *= 1.3f;
            mods.ItemDropMode = 1;
            mods.CanBurn = 0;
            mods.FireDamageTakenModifier = 0.001f;
            troop.SetProfile(new IProfile()
            {
                Name = "firebowman",
                Gender = Gender.Male,
                Skin = new IProfileClothingItem("Normal", randomSkin, "ClothingLightGreen"),
                Head = new IProfileClothingItem("StylishHat", "ClothingLightOrange", "ClothingLightRed"),
                ChestOver = new IProfileClothingItem("Apron", teamColor),
                ChestUnder = new IProfileClothingItem("BodyArmor", "ClothingLightOrange"),
                Hands = new IProfileClothingItem("Gloves", "ClothingLightOrange"),
                Legs = new IProfileClothingItem("CamoPants", "ClothingGray", "ClothingDarkGray"),
                Feet = new IProfileClothingItem("RidingBoots", "ClothingLightOrange"),
                Accesory = new IProfileClothingItem("GasMask", "ClothingDarkGray", "ClothingLightGray"),
            });
            BotBehaviorSet bsFireBowman = SetBotBehavior(troop, PredefinedAIType.BotA);
            bsFireBowman.MeleeUsage = false;
            troop.SetBotBehaviorSet(bsFireBowman);
            break;
    }
    
    troop.SetModifiers(mods);
}

private BotBehaviorSet SetBotBehavior(IPlayer bot, PredefinedAIType aiType)
{
    // Set AI behavior
    bot.SetBotBehavior(new BotBehavior(true, aiType));
    
    // Disable item searching
    BotBehaviorSet bS = bot.GetBotBehaviorSet();
    bS.SearchItems = 0;
    bot.SetBotBehaviorSet(bS);
    
    return bS;
}

private void UpdateTroopGuards(List<IPlayer> troops, IPlayer leader, bool enableGuard)
{
    foreach (IPlayer troop in troops)
    {
        if (troop != null && !troop.IsDead)
        {
            if (enableGuard)
            {
                troop.SetGuardTarget(leader);
            }
            else
            {
                troop.SetGuardTarget(null);
            }
        }
    }
}

public void TrySpawnTroops(TriggerArgs args)
{
    // This method is no longer used - troop spawning is now triggered by block key while crouching
}

private void SpawnTroopsForLeader(IPlayer leader, List<IPlayer> troopList)
{
    PlayerModifiers mods = leader.GetModifiers();
    float availableEnergy = mods.CurrentEnergy;
    
    // Determine which might dictionary to use
    Dictionary<int, int> troopMightDict = (leader.UniqueID == p1.UniqueID) ? p1TroopMight : p2TroopMight;
    string leaderName = (leader.UniqueID == p1.UniqueID) ? "P1" : "P2";
    
    // Calculate current total might
    int currentMight = CalculateTotalMight(troopMightDict, troopList);
    int availableMight = MAX_MIGHT_PER_SIDE - currentMight;
    
    // Check if at max might
    if (availableMight <= 0)
    {
        Game.ShowChatMessage(leaderName + " has deployed the maximum troops! (Might: " + currentMight + "/" + MAX_MIGHT_PER_SIDE + ")", Color.Red);
        return;
    }
    
    // Check if leader has any energy
    if (availableEnergy <= 0)
    {
        Game.ShowChatMessage(leaderName + " has no energy to spawn troops!", Color.Red);
        return;
    }
    
    // Limit available energy by available might
    if (availableEnergy > availableMight)
    {
        availableEnergy = availableMight;
    }
    
    // Define troop types with their energy requirements (might = energy cost)
    List<TroopSpawnData> troopTypes = new List<TroopSpawnData>()
    {
        new TroopSpawnData("Stickman", 30),
        new TroopSpawnData("Knifeman", 60),
        new TroopSpawnData("Bowman", 60),
        new TroopSpawnData("Knight", 100),
        new TroopSpawnData("Axeman", 100),
        new TroopSpawnData("FireBowman", 100),
    };
    
    Vector2 spawnPos = leader.GetWorldPosition();
    int troopsSpawned = 0;
    float totalEnergyCost = 0;
    
    // Keep spawning until no eligible troops remain
    while (availableEnergy > 0)
    {
        // Get eligible troops (energy requirement <= available energy)
        List<TroopSpawnData> eligibleTroops = new List<TroopSpawnData>();
        foreach (TroopSpawnData data in troopTypes)
        {
            if (data.EnergyCost <= availableEnergy)
            {
                eligibleTroops.Add(data);
            }
        }
        
        // If no eligible troops, break
        if (eligibleTroops.Count == 0) break;
        
        // Select random eligible troop
        TroopSpawnData selectedTroop = eligibleTroops[rnd.Next(eligibleTroops.Count)];
        
        // Spawn the troop with might tracking
        SpawnTroop(selectedTroop.TroopType, leader, spawnPos, troopList, troopMightDict, selectedTroop.EnergyCost);
        
        // Deduct energy cost
        availableEnergy -= selectedTroop.EnergyCost;
        totalEnergyCost += selectedTroop.EnergyCost;
        troopsSpawned++;
    }
    
    // Actually deduct energy from the leader
    if (totalEnergyCost > 0)
    {
        mods.CurrentEnergy -= totalEnergyCost;
        leader.SetModifiers(mods);
        
        int newTotalMight = CalculateTotalMight(troopMightDict, troopList);
        Game.ShowChatMessage(leaderName + " spawned " + troopsSpawned + " troops! (-" + totalEnergyCost + " energy, Might: " + newTotalMight + "/" + MAX_MIGHT_PER_SIDE + ")", Color.Green);
    }
}

private int CalculateTotalMight(Dictionary<int, int> troopMightDict, List<IPlayer> troopList)
{
    int totalMight = 0;
    
    // Clean up dead troops from the dictionary
    List<int> deadTroopIds = new List<int>();
    foreach (var entry in troopMightDict)
    {
        int troopId = entry.Key;
        bool troopExists = false;
        
        foreach (IPlayer troop in troopList)
        {
            if (troop != null && troop.UniqueID == troopId && !troop.IsDead)
            {
                troopExists = true;
                totalMight += entry.Value;
                break;
            }
        }
        
        if (!troopExists)
        {
            deadTroopIds.Add(troopId);
        }
    }
    
    // Remove dead troops from dictionary
    foreach (int deadId in deadTroopIds)
    {
        troopMightDict.Remove(deadId);
    }
    
    return totalMight;
}

public void OnTroopDeath(IPlayer player, PlayerDeathArgs args)
{
    if (player == null) return;
    
    // Check if this is a P1 troop
    if (p1TroopMight.ContainsKey(player.UniqueID))
    {
        // Gib the troop to remove body
        player.Gib();
        
        // Remove from might tracking
        p1TroopMight.Remove(player.UniqueID);
        
        // Remove from troop list
        p1Troops.RemoveAll(t => t != null && t.UniqueID == player.UniqueID);
    }
    // Check if this is a P2 troop
    else if (p2TroopMight.ContainsKey(player.UniqueID))
    {
        // Gib the troop to remove body
        player.Gib();
        
        // Remove from might tracking
        p2TroopMight.Remove(player.UniqueID);
        
        // Remove from troop list
        p2Troops.RemoveAll(t => t != null && t.UniqueID == player.UniqueID);
    }
}

public void RefillAmmo(TriggerArgs args)
{
    // Refill ammo for P1
    if (p1 != null && !p1.IsDead)
    {
        RefillPlayerAmmo(p1, WeaponItem.KATANA, WeaponItem.BOW);
    }
    
    // Refill ammo for P2
    if (p2 != null && !p2.IsDead)
    {
        RefillPlayerAmmo(p2, WeaponItem.KATANA, WeaponItem.BOW);
    }
    
    // Refill ammo for super troops
    if (bjorn != null && !bjorn.IsDead)
    {
        RefillPlayerAmmo(bjorn, WeaponItem.AXE);
    }
    
    if (thorsfin != null && !thorsfin.IsDead)
    {
        RefillPlayerAmmo(thorsfin, WeaponItem.KNIFE);
    }
    
    // Refill ammo for all troops
    // RefillTroopAmmo(p1Troops);
    // RefillTroopAmmo(p2Troops);
}

private void RefillPlayerAmmo(
    IPlayer player,
    WeaponItem meleeWeapon,
    WeaponItem rangedWeapon = WeaponItem.NONE)
{
    // Check melee slot
    MeleeWeaponItem currentMelee = player.CurrentMeleeWeapon;
    if (currentMelee.WeaponItem == WeaponItem.NONE)
    {
        player.GiveWeaponItem(meleeWeapon);
    }

    // Check primary weapon slot (rifle)
    RifleWeaponItem currentPrimary = player.CurrentPrimaryWeapon;
    if (currentPrimary.WeaponItem == WeaponItem.NONE &&
        rangedWeapon != WeaponItem.NONE)
    {
        player.GiveWeaponItem(rangedWeapon);
    }
}

private void RefillTroopAmmo(List<IPlayer> troops)
{
    foreach (IPlayer troop in troops)
    {
        if (troop == null || troop.IsDead) continue;
        
        IProfile profile = troop.GetProfile();
        string troopName = profile.Name;
        
        // Determine weapon based on troop type
        WeaponItem weapon = WeaponItem.NONE;
        WeaponItemType slot = WeaponItemType.Melee;
        
        switch (troopName)
        {
            case "Stickman":
                weapon = WeaponItem.CUESTICK;
                slot = WeaponItemType.Melee;
                break;
            case "Knifeman":
                weapon = WeaponItem.KNIFE;
                slot = WeaponItemType.Melee;
                break;
            case "Bowman":
                weapon = WeaponItem.BOW;
                slot = WeaponItemType.Rifle;
                break;
            case "Knight":
                weapon = WeaponItem.MACHETE;
                slot = WeaponItemType.Melee;
                break;
            case "Axeman":
                weapon = WeaponItem.AXE;
                slot = WeaponItemType.Melee;
                break;
        }
        
        // Check if troop needs weapon refill
        if (weapon != WeaponItem.NONE)
        {
            if (slot == WeaponItemType.Melee)
            {
                MeleeWeaponItem currentMelee = troop.CurrentMeleeWeapon;
                if (currentMelee.WeaponItem == WeaponItem.NONE)
                {
                    troop.GiveWeaponItem(weapon);
                }
            }
            else if (slot == WeaponItemType.Rifle)
            {
                RifleWeaponItem currentPrimary = troop.CurrentPrimaryWeapon;
                if (currentPrimary.WeaponItem == WeaponItem.NONE)
                {
                    troop.GiveWeaponItem(weapon);
                }
            }
        }
    }
}

// Helper class for troop spawn data
private class TroopSpawnData
{
    public string TroopType { get; set; }
    public int EnergyCost { get; set; }
    
    public TroopSpawnData(string troopType, int energyCost)
    {
        TroopType = troopType;
        EnergyCost = energyCost;
    }
}

public void UpdatePlayerFacingDirections(TriggerArgs args)
{
    // Update facing directions for all alive players
    IPlayer[] allPlayers = Game.GetPlayers();
    foreach (IPlayer player in allPlayers)
    {
        if (!player.IsDead)
        {
            playerFacingDirections[player.UniqueID] = player.FacingDirection;
        }
    }
}

public void OnPlayerMeleeAction(IPlayer attacker, PlayerMeleeHitArg[] args)
{
    // Check P2's jump attack ability
    if (p2 != null && attacker.UniqueID == p2.UniqueID && attacker.IsJumpAttacking)
    {
        foreach (PlayerMeleeHitArg hitArg in args)
        {
            if (hitArg.IsPlayer)
            {
                IPlayer target = hitArg.HitObject as IPlayer;
                if (target != null && target.GetTeam() != p2.GetTeam())
                {
                    // Don't affect dead bodies (bug #7)
                    if (target.IsDead) continue;
                    
                    // Check if target blocked the hit (bug #6 - fix block scenario)
                    if (hitArg.HitDamage == 0)
                    {
                        // Blocked - disable input, make them fall, then re-enable
                        target.SetInputEnabled(false);
                        target.AddCommand(new PlayerCommand(PlayerCommandType.Fall));
                        
                        // Store player ID for re-enabling input
                        playersToReEnableInput.Add(target.UniqueID);
                        
                        // Re-enable input after 200ms
                        IObjectTimerTrigger inputTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
                        inputTimer.SetIntervalTime(200);
                        inputTimer.SetRepeatCount(1);
                        inputTimer.SetScriptMethod("ReEnablePlayersInput");
                        inputTimer.Trigger();
                    }
                    else
                    {
                        // Not blocked - kill and prepare for respawn
                        // Store player data before killing
                        KilledPlayerData killedData = new KilledPlayerData();
                        killedData.DeadPlayerID = target.UniqueID;
                        killedData.Profile = target.GetProfile();
                        killedData.Team = target.GetTeam();
                        killedData.Modifiers = target.GetModifiers();
                        killedData.User = target.GetUser();
                        killedData.KillTime = Game.TotalElapsedGameTime;
                        
                        // Bug #1 - Store bot name
                        killedData.BotName = target.Name;
                        
                        // Store bot behavior and bot behavior set
                        killedData.BotBehavior = target.GetBotBehavior();
                        killedData.BotBehaviorSet = target.GetBotBehaviorSet();
                        
                        // Store visibility and camera settings
                        killedData.NametagVisible = target.GetNametagVisible();
                        killedData.StatusBarsVisible = target.GetStatusBarsVisible();
                        killedData.CameraFocusMode = target.GetCameraSecondaryFocusMode();
                        
                        // Bug #5 - Track special players
                        if (p1 != null && target.UniqueID == p1.UniqueID)
                        {
                            killedData.IsP1 = true;
                        }
                        else if (bjorn != null && target.UniqueID == bjorn.UniqueID)
                        {
                            killedData.IsBjorn = true;
                        }
                        else if (p2 != null && target.UniqueID == p2.UniqueID)
                        {
                            killedData.IsP2 = true;
                        }
                        else if (thorsfin != null && target.UniqueID == thorsfin.UniqueID)
                        {
                            killedData.IsThorsFin = true;
                        }
                        
                        // Bug #3 - Don't copy weapons (removed weapon storage)
                        
                        p2JumpKilledPlayers.Add(killedData);
                        
                        // Bug #6 - Signal to CoopGameOver.cs
                        IScriptStorage storage = Game.GetSharedStorage("SuperDSCoopSync");
                        storage.SetItem("P2_SPLITTING", true);
                        storage.SetItem("P2_SPLIT_TIME", Game.TotalElapsedGameTime + 3000); // 3s buffer
                        
                        // Kill the player
                        target.Kill();
                        
                        // Schedule respawn after 3 seconds
                        IObjectTimerTrigger respawnTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
                        respawnTimer.SetIntervalTime(3000); // 3 seconds
                        respawnTimer.SetRepeatCount(1);
                        respawnTimer.SetScriptMethod("RespawnP2JumpKilledPlayer");
                        respawnTimer.Trigger();
                    }
                }
            }
        }
    }
    
    // Check P1's block disarm ability
    if (p1 != null)
    {
        foreach (PlayerMeleeHitArg hitArg in args)
        {
            // Check if P1 blocked this hit (hit P1 with 0 damage = blocked)
            if (hitArg.IsPlayer && hitArg.HitDamage == 0)
            {
                IPlayer target = hitArg.HitObject as IPlayer;
                if (target != null && target.UniqueID == p1.UniqueID)
                {
                    // P1 successfully blocked! 70% chance to disarm the attacker
                    if (rnd.NextDouble() < 0.7)
                    {
                        DisarmPlayer(attacker);
                    }
                    // Don't process backstab for this hit
                    continue;
                }
            }
        }
    }
    
    // Only P1 has backstab ability
    if (p1 == null || attacker.UniqueID != p1.UniqueID) return;
    
    // Check each target hit by P1
    foreach (PlayerMeleeHitArg hitArg in args)
    {
        // Check if the hit object is a player
        if (!hitArg.IsPlayer) continue;
        
        IPlayer target = hitArg.HitObject as IPlayer;
        if (target == null || target.IsDead) continue;
        
        // Don't backstab self
        if (target.UniqueID == p1.UniqueID) continue;
        
        // Don't backstab teammates (same team)
        if (target.GetTeam() == p1.GetTeam()) continue;
        
        // Check if both players are facing the same direction (backstab condition)
        // Use stored facing direction from before the hit
        int attackerFacing = attacker.FacingDirection;
        int targetFacing = playerFacingDirections.ContainsKey(target.UniqueID) ? 
                          playerFacingDirections[target.UniqueID] : 
                          target.FacingDirection;
        
        if (attackerFacing == targetFacing)
        {
            // Backstab! Apply 2x damage
            // Calculate damage dealt by getting the target's current health before and after
            float currentHealth = target.GetHealth();
            float meleeBaseDamage = 15f; // Approximate base melee damage
            float extraDamage = meleeBaseDamage * 3; // 2x damage = base + extra base

            
            // Deal extra damage to simulate 4x total damage
            target.DealDamage(extraDamage);
            Game.PlayEffect(EffectName.Gib, target.GetWorldPosition());
        }
    }
}

public void OnPlayerDamage(IPlayer player, PlayerDamageArgs args)
{
    // P1's damage negation ability (40% chance to negate melee/projectile damage)
    if (p1 != null && player.UniqueID == p1.UniqueID)
    {
        // Only for melee and projectile damage
        if (args.DamageType == PlayerDamageEventType.Melee || args.DamageType == PlayerDamageEventType.Projectile)
        {
            // 40% chance to negate damage
            if (rnd.NextDouble() < 0.4)
            {
                p1.SetInputEnabled(false);
                p1.AddCommand(new PlayerCommand(PlayerCommandType.Block));

                // Re-enable input after 400ms
                IObjectTimerTrigger inputTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
                inputTimer.SetIntervalTime(200);
                inputTimer.SetRepeatCount(1);
                inputTimer.SetScriptMethod("ReEnableP1Input");
                inputTimer.Trigger();

                // Heal back the damage
                PlayerModifiers p1Mods = p1.GetModifiers();
                p1Mods.CurrentHealth = Math.Min(p1Mods.MaxHealth, p1Mods.CurrentHealth + args.Damage);
                p1.SetModifiers(p1Mods);
                
                // Play Block effect at P1's position
                Game.PlayEffect(EffectName.Sparks, p1.GetWorldPosition());
                Game.PlaySound("MeleeBlockMetal", p1.GetWorldPosition());
            }
        }
    }
    
    // P1's backstab ability for projectiles
    if (p1 == null || args.SourceID == 0) return;
    
    // Check if source is P1
    if (args.SourceID != p1.UniqueID) return;
    
    // Only apply to projectile damage
    if (args.DamageType != PlayerDamageEventType.Projectile) return;
    
    IPlayer target = player;
    if (target == null || target.IsDead) return;
    
    // Don't backstab self
    if (target.UniqueID == p1.UniqueID) return;
    
    // Don't backstab teammates (same team)
    if (target.GetTeam() == p1.GetTeam()) return;
    
    // Check if both players are facing the same direction (backstab condition)
    int attackerFacing = p1.FacingDirection;
    int targetFacing = playerFacingDirections.ContainsKey(target.UniqueID) ? 
                      playerFacingDirections[target.UniqueID] : 
                      target.FacingDirection;
    
    if (attackerFacing == targetFacing)
    {
        // Backstab! Apply extra damage to simulate 4x total damage
        float extraDamage = args.Damage * 3;
        target.DealDamage(extraDamage);
        Game.PlayEffect(EffectName.Gib, target.GetWorldPosition());
    }
}

private void DisarmPlayer(IPlayer player)
{
    // Drop the player's melee weapon by removing it from their inventory
    // This will cause the weapon to drop to the ground
    MeleeWeaponItem currentMelee = player.CurrentMeleeWeapon;
    if (currentMelee.WeaponItem != WeaponItem.NONE)
    {
        player.Disarm(WeaponItemType.Melee);
    }
}

public void ReEnableP1Input(TriggerArgs args)
{
    if (p1 != null && !p1.IsDead)
    {
        p1.SetInputEnabled(true);
    }
}

public void OnUpdate(float elapsed)
{
    // Handle Bjorn's low HP strength boost
    HandleBjornLowHP();
}

private void HandleBjornLowHP()
{
    if (bjorn == null || bjorn.IsDead) return;
    
    PlayerModifiers bjornMods = bjorn.GetModifiers();
    float hpPercentage = (float)bjornMods.CurrentHealth / bjornMods.MaxHealth;
    
    // If HP is 30% or below, give strength boost
    if (hpPercentage <= 0.3f)
    {
        bjorn.SetStrengthBoostTime(6000); // 6 seconds
    }
}

public void RespawnP2JumpKilledPlayer(TriggerArgs args)
{
    // Check if there are any killed players to respawn
    if (p2JumpKilledPlayers.Count == 0) return;
    
    float currentTime = Game.TotalElapsedGameTime;
    List<KilledPlayerData> playersToRespawn = new List<KilledPlayerData>();
    
    // Find all players that should be respawned (killed 3+ seconds ago)
    foreach (KilledPlayerData data in p2JumpKilledPlayers)
    {
        if (currentTime - data.KillTime >= 3000)
        {
            playersToRespawn.Add(data);
        }
    }
    
    // Respawn each eligible player
    foreach (KilledPlayerData data in playersToRespawn)
    {
        // Find the dead body by ID (bug #5 - get current body position, not stored position)
        IPlayer deadBody = null;
        Vector2 respawnPosition = Vector2.Zero;
        bool bodyGibbed = true;
        
        IPlayer[] allPlayers = Game.GetPlayers();
        foreach (IPlayer player in allPlayers)
        {
            if (player.UniqueID == data.DeadPlayerID && player.IsDead)
            {
                deadBody = player;
                respawnPosition = player.GetWorldPosition(); // Bug #5 - use current body position
                bodyGibbed = false;
                break;
            }
        }
        
        // Bug #5 - only respawn if body is not gibbed
        if (!bodyGibbed && deadBody != null)
        {
            // Remove the dead body
            deadBody.Remove();
            
            // Create new player at the body's current position
            IPlayer respawnedPlayer = Game.CreatePlayer(respawnPosition);
            if (respawnedPlayer != null)
            {
                // Restore team
                respawnedPlayer.SetTeam(data.Team);
                
                // Restore profile
                respawnedPlayer.SetProfile(data.Profile);
                
                // Bug #1 - Restore name
                respawnedPlayer.SetBotName(data.BotName);
                
                // Bug #2 - Fix max health before restoring modifiers
                PlayerModifiers mods = data.Modifiers;
                // The MaxHealth in stored modifiers is correct, just apply it
                respawnedPlayer.SetModifiers(mods);
                
                // Bug #3 - Restore controller for human players
                if (data.User != null)
                {
                    respawnedPlayer.SetUser(data.User);
                }
                
                // Restore bot behavior and bot behavior set
                respawnedPlayer.SetBotBehavior(data.BotBehavior);
                respawnedPlayer.SetBotBehaviorSet(data.BotBehaviorSet);
                
                // Restore visibility and camera settings
                respawnedPlayer.SetNametagVisible(data.NametagVisible);
                respawnedPlayer.SetStatusBarsVisible(data.StatusBarsVisible);
                respawnedPlayer.SetCameraSecondaryFocusMode(data.CameraFocusMode);
                
                // Bug #5 - Restore special player references and abilities
                if (data.IsP1)
                {
                    p1 = respawnedPlayer;
                }
                else if (data.IsBjorn)
                {
                    bjorn = respawnedPlayer;
                }
                else if (data.IsP2)
                {
                    p2 = respawnedPlayer;
                }
                else if (data.IsThorsFin)
                {
                    thorsfin = respawnedPlayer;
                    // Restore ThorsFin's permanent speed boost
                    thorsfin.SetSpeedBoostTime(999999);
                }
                
                // Bug #3 - Don't restore weapons (as requested)
            }
        }
        
        // Bug #6 - Clear the splitting flag
        IScriptStorage storage = Game.GetSharedStorage("SuperDSCoopSync");
        storage.SetItem("P2_SPLITTING", false);
        
        // Remove from tracking list
        p2JumpKilledPlayers.Remove(data);
    }
}

public void ReEnablePlayersInput(TriggerArgs args)
{
    // Re-enable input for all players that were knocked down by P2's blocked jump attack
    IPlayer[] allPlayers = Game.GetPlayers();
    foreach (IPlayer player in allPlayers)
    {
        if (playersToReEnableInput.Contains(player.UniqueID))
        {
            player.SetInputEnabled(true);
        }
    }
    
    // Clear the list
    playersToReEnableInput.Clear();
}