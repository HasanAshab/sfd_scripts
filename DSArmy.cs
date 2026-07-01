// DSArmy - Two-player commanders with loyal commando squads
// P1 and P2 are commanders with enhanced abilities
// Each commander has 3 commandos that follow and protect them

private IPlayer p1 = null;
private IPlayer p2 = null;

// Track commandos for each commander
private List<IPlayer> p1Commandos = new List<IPlayer>();
private List<IPlayer> p2Commandos = new List<IPlayer>();

// Track all spawned commandos to exclude from victory calculation
private HashSet<int> spawnedCommandoIds = new HashSet<int>();

public void OnStartup()
{
    // Store player references at startup
    IPlayer[] players = Game.GetPlayers();
    p1 = players.Length >= 1 ? players[0] : null;
    p2 = players.Length >= 2 ? players[1] : null;
    
    if (p1 == null || p2 == null) return; // Need both players
    
    // Set up P1 (Commander 1) - Focus on speed and projectile damage
    SetupP1();
    
    // Set up P2 (Commander 2) - Focus on melee combat
    SetupP2();
    
    // Spawn commandos after a short delay to ensure commanders are ready
    IObjectTimerTrigger commandoSpawnTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    commandoSpawnTimer.SetIntervalTime(500); // 0.5 second delay
    commandoSpawnTimer.SetRepeatCount(1); // Run once
    commandoSpawnTimer.SetScriptMethod("SpawnCommandos");
    commandoSpawnTimer.Trigger();
}

private void SetupP1()
{
    if (p1 == null) return;
    
    // Remove existing weapons
    p1.RemoveWeaponItemType(WeaponItemType.Rifle);
    p1.RemoveWeaponItemType(WeaponItemType.Handgun);
    p1.RemoveWeaponItemType(WeaponItemType.Melee);
    p1.RemoveWeaponItemType(WeaponItemType.Thrown);
    
    // Give P1 weapons: knife, MP50, PISTOL45, bomb
    p1.GiveWeaponItem(WeaponItem.KNIFE);
    p1.GiveWeaponItem(WeaponItem.MP50);
    p1.GiveWeaponItem(WeaponItem.PISTOL45);
    p1.GiveWeaponItem(WeaponItem.GRENADES);
    
    // Set P1 modifiers - 30% more speed, energy recharge, and crit chance
    PlayerModifiers p1Mods = p1.GetModifiers();
    p1Mods.RunSpeedModifier *= 1.3f;
    p1Mods.SprintSpeedModifier *= 1.3f;
    p1Mods.EnergyRechargeModifier *= 1.3f;
    p1Mods.ProjectileCritChanceDealtModifier *= 1.3f;
    p1.SetModifiers(p1Mods);
    
    // Set P1 profile - Bonduk
    string primeColor = GetPrimeColor(p1.GetTeam());
    p1.SetProfile(new IProfile()
    {
        Name = "Bonduk",
        Gender = Gender.Female,
        Skin = new IProfileClothingItem("Normal_fem", "Skin4", "ClothingLightGreen"),
        ChestOver = new IProfileClothingItem("KevlarVest_fem", primeColor),
        ChestUnder = new IProfileClothingItem("LumberjackShirt2_fem", primeColor, "ClothingLightYellow"),
        Hands = new IProfileClothingItem("GlovesBlack", "ClothingLightGray"),
        Legs = new IProfileClothingItem("Pants_fem", primeColor),
        Feet = new IProfileClothingItem("ShoesBlack", "ClothingBrown"),
        Accesory = new IProfileClothingItem("SunGlasses", "", "ClothingLightGray"),
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
    
    // Give P2 weapons: knife, MP50, PISTOL45, bomb
    p2.GiveWeaponItem(WeaponItem.KNIFE);
    p2.GiveWeaponItem(WeaponItem.MP50);
    p2.GiveWeaponItem(WeaponItem.PISTOL45);
    p2.GiveWeaponItem(WeaponItem.GRENADES);
    
    // Set P2 modifiers - 30% more melee damage and force
    PlayerModifiers p2Mods = p2.GetModifiers();
    p2Mods.MeleeDamageDealtModifier *= 1.3f;
    p2Mods.MeleeForceModifier *= 1.3f;
    p2.SetModifiers(p2Mods);
    
    // Set P2 profile - Hateli
    string primeColor = GetPrimeColor(p2.GetTeam());
    p2.SetProfile(new IProfile()
    {
        Name = "Hateli",
        Gender = Gender.Male,
        Skin = new IProfileClothingItem("Normal", "Skin5", "ClothingLightGray"),
        ChestOver = new IProfileClothingItem("MilitaryJacket", primeColor, "ClothingLightGray"),
        ChestUnder = new IProfileClothingItem("SleevelessShirt", "ClothingLightGray"),
        Hands = new IProfileClothingItem("GlovesBlack", "ClothingLightGray"),
        Legs = new IProfileClothingItem("Pants", primeColor),
        Feet = new IProfileClothingItem("ShoesBlack", "ClothingBrown"),
        Accesory = new IProfileClothingItem("Cigar", "ClothingDarkGray"),
    });
}

public void SpawnCommandos(TriggerArgs args)
{
    // Spawn 3 commandos for P1
    if (p1 != null && !p1.IsDead)
    {
        for (int i = 0; i < 3; i++)
        {
            SpawnCommando(p1, ref p1Commandos);
        }
    }
    
    // Spawn 3 commandos for P2
    if (p2 != null && !p2.IsDead)
    {
        for (int i = 0; i < 3; i++)
        {
            SpawnCommando(p2, ref p2Commandos);
        }
    }
}

private void SpawnCommando(IPlayer commander, ref List<IPlayer> commandoList)
{
    if (commander == null) return;
    
    Vector2 commanderPos = commander.GetWorldPosition();
    PlayerTeam commanderTeam = commander.GetTeam();
    
    // Create commando bot
    IPlayer commando = Game.CreatePlayer(commanderPos);
    if (commando != null)
    {
        // Track as spawned commando
        spawnedCommandoIds.Add(commando.UniqueID);
        commandoList.Add(commando);
        
        // Set commando properties
        commando.SetTeam(commanderTeam); // Inherit commander's team
        commando.SetNametagVisible(false);
        commando.SetStatusBarsVisible(false);
        commando.SetCameraSecondaryFocusMode(CameraFocusMode.Ignore);
        
        // Set as bot with CompanionC behavior
        BotBehavior commandoBehavior = new BotBehavior(true, PredefinedAIType.CompanionC);
        commando.SetBotBehavior(commandoBehavior);
        
        // Set commando to guard the commander
        commando.SetGuardTarget(commander);
        
        // Remove existing weapons
        commando.RemoveWeaponItemType(WeaponItemType.Rifle);
        commando.RemoveWeaponItemType(WeaponItemType.Handgun);
        commando.RemoveWeaponItemType(WeaponItemType.Melee);
        commando.RemoveWeaponItemType(WeaponItemType.Thrown);
        
        // Give commando weapons: knife, PISTOL45, SMG
        commando.GiveWeaponItem(WeaponItem.KNIFE);
        commando.GiveWeaponItem(WeaponItem.PISTOL45);
        commando.GiveWeaponItem(WeaponItem.SMG);
        
        // Set commando profile
        string primeColor = GetPrimeColor(commanderTeam);
        commando.SetProfile(GetCommandoProfile(commanderTeam, primeColor));
    }
}

private IProfile GetCommandoProfile(PlayerTeam team, string primeColor)
{
    // Create variation in commando profiles (3 different looks)
    int profileVariant = spawnedCommandoIds.Count % 3;
    
    if (profileVariant == 0)
    {
        return new IProfile()
        {
            Name = "Commando",
            Gender = Gender.Male,
            Skin = new IProfileClothingItem("Tattoos", "Skin1", "ClothingLightYellow"),
            Head = new IProfileClothingItem("Helmet", primeColor),
            ChestUnder = new IProfileClothingItem("MilitaryShirt", primeColor, "ClothingLightBlue"),
            Hands = new IProfileClothingItem("GlovesBlack", "ClothingDarkGray"),
            Waist = new IProfileClothingItem("SatchelBelt", primeColor),
            Legs = new IProfileClothingItem("CamoPants", primeColor, primeColor),
            Feet = new IProfileClothingItem("BootsBlack", "ClothingDarkGray"),
        };
    }
    else if (profileVariant == 1)
    {
        return new IProfile()
        {
            Name = "Commando",
            Gender = Gender.Male,
            Skin = new IProfileClothingItem("Normal", "Skin3", "ClothingLightGray"),
            ChestOver = new IProfileClothingItem("AmmoBelt", primeColor),
            ChestUnder = new IProfileClothingItem("TShirt", primeColor),
            Hands = new IProfileClothingItem("Gloves", "ClothingGray"),
            Waist = new IProfileClothingItem("AmmoBeltWaist", "ClothingGray"),
            Legs = new IProfileClothingItem("CamoPants", "ClothingDarkGreen", primeColor),
            Feet = new IProfileClothingItem("BootsBlack", "ClothingDarkGray"),
        };
    }
    else
    {
        return new IProfile()
        {
            Name = "Commando",
            Gender = Gender.Female,
            Skin = new IProfileClothingItem("Normal_fem", "Skin2", "ClothingLightGray"),
            Head = new IProfileClothingItem("Helmet", primeColor),
            ChestUnder = new IProfileClothingItem("MilitaryShirt_fem", primeColor, "ClothingLightBlue"),
            Hands = new IProfileClothingItem("GlovesBlack", "ClothingDarkGray"),
            Waist = new IProfileClothingItem("SatchelBelt_fem", primeColor),
            Legs = new IProfileClothingItem("CamoPants_fem", primeColor, primeColor),
            Feet = new IProfileClothingItem("BootsBlack", "ClothingDarkGray"),
        };
    }
}

private string GetPrimeColor(PlayerTeam team)
{
    switch (team)
    {
        case PlayerTeam.Team1:
            return "ClothingDarkGray";
        case PlayerTeam.Team2:
            return "ClothingDarkYellow";
        case PlayerTeam.Team3:
            return "ClothingOrange";
        case PlayerTeam.Team4:
            return "ClothingLightGray";
        default:
            return "ClothingDarkGray";
    }
}
