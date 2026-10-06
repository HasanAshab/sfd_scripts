private IPlayer p1 = null;
private IPlayer p2 = null;

public void OnStartup()
{
    SetupPlayers();
}

public void SetupPlayers()
{
    IPlayer[] players = Game.GetPlayers();
    p1 = players.Length >= 1 ? players[0] : null;
    p2 = players.Length >= 2 ? players[1] : null;

    if (p1 != null)
    {
        PlayerModifiers p1Mods = p1.GetModifiers();

        // p1Mods.RunSpeedModifier = 1.15f;
        // p1Mods.SprintSpeedModifier = 1.3f;

        // p1Mods.MaxEnergy = (int)(p1Mods.MaxEnergy * 1.2f);
        // p1Mods.CurrentEnergy = (int)(p1Mods.CurrentEnergy * 1.2f);
        // p1Mods.EnergyRechargeModifier *= 1.2f;

        p1Mods.SizeModifier = 0.89f;
        p1Mods.MeleeForceModifier *= 0.8f;
        p1Mods.MeleeDamageDealtModifier *= 1.2f;
        p1Mods.RunSpeedModifier = 1.1f;
        p1Mods.SprintSpeedModifier = 1.15f;
        p1.SetModifiers(p1Mods);
        p1.SetProfile(new IProfile()
        {
            Name = "daul",
            Gender = Gender.Female,
            Skin = new IProfileClothingItem("Normal_fem", "Skin4", "ClothingLightGreen"),
            Head = new IProfileClothingItem("Cap", "ClothingGray"),
            ChestUnder = new IProfileClothingItem("LumberjackShirt2_fem", "ClothingBlue", "ClothingLightGray"),
            Legs = new IProfileClothingItem("Pants_fem", "ClothingDarkBlue"),
            Feet = new IProfileClothingItem("ShoesBlack", "ClothingBrown"),
        });
    }

    if (p2 != null)
    {
        PlayerModifiers p2Mods = p2.GetModifiers();

        // p2Mods.SizeModifier = 1.12f;
        // p2Mods.RunSpeedModifier = 0.8f;
        // p2Mods.SprintSpeedModifier = 0.95f;
        // p2Mods.MeleeForceModifier *= 1.2f;
        // p2Mods.MeleeDamageDealtModifier *= 1.4f;

        p2Mods.SizeModifier = 0.86f;
        p2Mods.MeleeForceModifier *= 0.8f;
        p2Mods.MeleeDamageDealtModifier *= 1.2f;
        p2Mods.RunSpeedModifier = 1.13f;
        p2Mods.SprintSpeedModifier = 1.19f;
        // p2Mods.RunSpeedModifier = 0.8f;
        // p2Mods.SprintSpeedModifier = 0.95f;

        p2.SetModifiers(p2Mods);
        p2.SetProfile(new IProfile()
        {
            Name = "saul",
            Gender = Gender.Female,
            Skin = new IProfileClothingItem("Normal_fem", "Skin4", "ClothingLightGreen"),
            Head = new IProfileClothingItem("Cap", "ClothingGray"),
            ChestUnder = new IProfileClothingItem("LumberjackShirt2_fem", "ClothingLightRed", "ClothingLightGray"),
            Legs = new IProfileClothingItem("Pants_fem", "ClothingRed"),
            Feet = new IProfileClothingItem("ShoesBlack", "ClothingBrown"),
        });
    }
}
