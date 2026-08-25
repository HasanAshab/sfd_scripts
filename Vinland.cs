// Vinland - Norse warrior theme with enhanced combat abilities
// P1 (ThorsBonduk) - Speed and agility focused with slowmo power
// P2 (ThorsHateli) - Strength and durability focused melee fighter

private IPlayer p1 = null;
private IPlayer p2 = null;

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
    
    // Set up slowmo timer for P1 (every 12 seconds)
    IObjectTimerTrigger slowmoTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
    slowmoTimer.SetIntervalTime(12000); // 12 seconds
    slowmoTimer.SetRepeatCount(0); // Infinite repeats
    slowmoTimer.SetScriptMethod("GiveP1Slowmo");
    slowmoTimer.Trigger();
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
    p1Mods.SizeModifier = 1.2f;
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
    p2Mods.SizeModifier = 1.4f;
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
