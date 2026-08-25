// Vinland - Norse warrior theme with enhanced combat abilities
// P1 (ThorsBonduk) - Speed and agility focused with slowmo power
// P2 (ThorsHateli) - Strength and durability focused melee fighter

private const int HIT_POINT = 14;

private IPlayer p1 = null;
private IPlayer p2 = null;

// Troop lists for each leader
private List<IPlayer> p1Troops = new List<IPlayer>();
private List<IPlayer> p2Troops = new List<IPlayer>();

// Track guard mode for each leader
private bool p1GuardEnabled = true;
private bool p2GuardEnabled = true;

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
    
    // Spawn initial troops for both leaders
    SpawnInitialTroops();
    
    // Set up slowmo timer for P1 (every 12 seconds)
    IObjectTimerTrigger slowmoTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    slowmoTimer.SetIntervalTime(12000); // 12 seconds
    slowmoTimer.SetRepeatCount(0); // Infinite repeats
    slowmoTimer.SetScriptMethod("GiveP1Slowmo");
    slowmoTimer.Trigger();
    
    // Set up troop spawn timer
    IObjectTimerTrigger troopTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    troopTimer.SetIntervalTime(10000); // 10 seconds
    troopTimer.SetRepeatCount(0); // Infinite repeats
    troopTimer.SetScriptMethod("TrySpawnTroops");
    troopTimer.Trigger();
    
    // Set up ammo refill timer
    IObjectTimerTrigger ammoTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    ammoTimer.SetIntervalTime(20000); // 20 seconds
    ammoTimer.SetRepeatCount(0); // Infinite repeats
    ammoTimer.SetScriptMethod("RefillAmmo");
    ammoTimer.Trigger();
    
    // Set up player key input callback for guard toggle
    Events.PlayerKeyInputCallback.Start(OnPlayerKeyInput);
}

public void OnPlayerKeyInput(IPlayer player, VirtualKeyInfo[] keyInfos)
{
    // Check for sheathe weapon toggle to enable/disable guard mode
    if (player == null) return;
    
    foreach (VirtualKeyInfo keyInfo in keyInfos)
    {
        if (keyInfo.Event == VirtualKeyEvent.Pressed && keyInfo.Key == VirtualKey.SHEATHE)
        {
            if (p1 != null && player.UniqueID == p1.UniqueID)
            {
                p1GuardEnabled = !p1GuardEnabled;
                UpdateTroopGuards(p1Troops, p1, p1GuardEnabled);
            }
            else if (p2 != null && player.UniqueID == p2.UniqueID)
            {
                p2GuardEnabled = !p2GuardEnabled;
                UpdateTroopGuards(p2Troops, p2, p2GuardEnabled);
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
    p1Mods.MaxEnergy = (int)(p1Mods.MaxEnergy * 2.5f);
    p1Mods.CurrentEnergy = (int)(p1Mods.CurrentEnergy * 2.5f);
    p1Mods.EnergyRechargeModifier *= 1.1f;
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
    p2Mods.MaxHealth = (int)(p2Mods.MaxHealth * 2.5f);
    p2Mods.CurrentHealth = (int)(p2Mods.CurrentHealth * 2.5f);
    p2Mods.MeleeDamageDealtModifier *= 2.0f;
    p2Mods.MeleeForceModifier *= 2.0f;
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
    // Spawn initial troops for P1: 4 Stickman, 2 Knife, 2 Bowman, 1 Knight, 1 Axeman
    for (int i = 0; i < 4; i++) SpawnTroop("Stickman", p1, p1.GetWorldPosition(), p1Troops);
    for (int i = 0; i < 2; i++) SpawnTroop("Knifeman", p1, p1.GetWorldPosition(), p1Troops);
    for (int i = 0; i < 2; i++) SpawnTroop("Bowman", p1, p1.GetWorldPosition(), p1Troops);
    SpawnTroop("Knight", p1, p1.GetWorldPosition(), p1Troops);
    SpawnTroop("Axeman", p1, p1.GetWorldPosition(), p1Troops);
    
    // Spawn initial troops for P2: 4 Stickman, 2 Knife, 2 Bowman, 1 Knight, 1 Axeman
    for (int i = 0; i < 4; i++) SpawnTroop("Stickman", p2, p2.GetWorldPosition(), p2Troops);
    for (int i = 0; i < 2; i++) SpawnTroop("Knifeman", p2, p2.GetWorldPosition(), p2Troops);
    for (int i = 0; i < 2; i++) SpawnTroop("Bowman", p2, p2.GetWorldPosition(), p2Troops);
    SpawnTroop("Knight", p2, p2.GetWorldPosition(), p2Troops);
    SpawnTroop("Axeman", p2, p2.GetWorldPosition(), p2Troops);
}

private void SpawnTroop(string troopType, IPlayer leader, Vector2 position, List<IPlayer> troopList)
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
    
    // Add to troop list
    troopList.Add(troop);
    
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
            mods.SizeModifier = 0.8f;
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
            mods.SizeModifier = 0.8f;
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
            mods.SizeModifier = 0.8f;
            mods.RunSpeedModifier *= 1.3f;
            mods.SprintSpeedModifier *= 1.3f;
            troop.SetProfile(new IProfile()
            {
                Name = "Bowman",
                Gender = Gender.Female,
                Skin = new IProfileClothingItem("Normal_fem", randomSkin, "ClothingLightGreen"),
                Head = new IProfileClothingItem("StylishHat_fem", "ClothingGray", "ClothingLightGray"),
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
    // Try to spawn troops for P1
    if (p1 != null && !p1.IsDead)
    {
        SpawnTroopsForLeader(p1, p1Troops);
    }
    
    // Try to spawn troops for P2
    if (p2 != null && !p2.IsDead)
    {
        SpawnTroopsForLeader(p2, p2Troops);
    }
}

private void SpawnTroopsForLeader(IPlayer leader, List<IPlayer> troopList)
{
    PlayerModifiers mods = leader.GetModifiers();
    float availableEnergy = mods.CurrentEnergy;
    
    // Define troop types with their energy requirements
    List<TroopSpawnData> troopTypes = new List<TroopSpawnData>()
    {
        new TroopSpawnData("Stickman", 15),
        new TroopSpawnData("Knifeman", 30),
        new TroopSpawnData("Bowman", 30),
        new TroopSpawnData("Knight", 50),
        new TroopSpawnData("Axeman", 50),
    };
    
    Vector2 spawnPos = leader.GetWorldPosition();
    
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
        
        // Spawn the troop
        SpawnTroop(selectedTroop.TroopType, leader, spawnPos, troopList);
        
        // Deduct energy cost (but don't actually modify player energy)
        availableEnergy -= selectedTroop.EnergyCost;
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
    
    // Refill ammo for all troops
    RefillTroopAmmo(p1Troops);
    RefillTroopAmmo(p2Troops);
}

private void RefillPlayerAmmo(IPlayer player, WeaponItem meleeWeapon, WeaponItem rangedWeapon)
{
    // Check melee slot
    MeleeWeaponItem currentMelee = player.CurrentMeleeWeapon;
    if (currentMelee.WeaponItem == WeaponItem.NONE)
    {
        player.GiveWeaponItem(meleeWeapon);
    }
    
    // Check primary weapon slot (rifle)
    RifleWeaponItem currentPrimary = player.CurrentPrimaryWeapon;
    if (currentPrimary.WeaponItem == WeaponItem.NONE)
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
