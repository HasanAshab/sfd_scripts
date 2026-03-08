public void OnStartup()
{
    MakeEveryoneSmaller();
}


public void MakeEveryoneSmaller()
{
    IPlayer[] allPlayers = Game.GetPlayers();
    
    // Track which profile index to use for each team
    int team1ProfileIndex = 0;
    int team2ProfileIndex = 0;
    
    // Define Team 2 profiles (zombies)
    IProfile[] team2Profiles = new IProfile[]
    {
        new IProfile()
        {
            Name = "Dead Cop",
            Gender = Gender.Male,
            Skin = new IProfileClothingItem("Zombie", ""),
            ChestUnder = new IProfileClothingItem("Sweater", "ClothingGreen"),
            Hands = new IProfileClothingItem("FingerlessGloves", "ClothingDarkGray"),
            Waist = new IProfileClothingItem("Belt", "ClothingDarkGray", "ClothingLightGray"),
            Legs = new IProfileClothingItem("PantsBlack", "ClothingDarkGray"),
            Feet = new IProfileClothingItem("BootsBlack", "ClothingDarkBrown"),
        },
        new IProfile()
        {
            Name = "Zombie Ninja",
            Gender = Gender.Female,
            Skin = new IProfileClothingItem("Zombie_fem", ""),
            ChestUnder = new IProfileClothingItem("TrainingShirt_fem", "ClothingDarkBlue"),
            Hands = new IProfileClothingItem("FingerlessGlovesBlack", "ClothingDarkBlue"),
            Waist = new IProfileClothingItem("Sash_fem", "ClothingDarkRed"),
            Legs = new IProfileClothingItem("Pants_fem", "ClothingDarkBlue"),
            Feet = new IProfileClothingItem("ShoesBlack", "ClothingDarkBlue"),
            Accesory = new IProfileClothingItem("Mask", "ClothingDarkRed"),
        },
        new IProfile()
        {
            Name = "Zombie Bruiser",
            Gender = Gender.Male,
            Skin = new IProfileClothingItem("Zombie", ""),
            ChestOver = new IProfileClothingItem("VestBlack", "ClothingBlue", "ClothingDarkBlue"),
            Legs = new IProfileClothingItem("TornPants", "ClothingDarkPurple"),
            Accesory = new IProfileClothingItem("RestraintMask", "ClothingGray"),
        }
    };
    
    // Define Team 1 profiles (assassins)
    IProfile[] team1Profiles = new IProfile[]
    {
        new IProfile()
        {
            Name = "Assassin",
            Gender = Gender.Male,
            Skin = new IProfileClothingItem("Normal", "Skin4", "ClothingLightGray"),
            ChestUnder = new IProfileClothingItem("SweaterBlack", "ClothingDarkBlue"),
            Legs = new IProfileClothingItem("PantsBlack", "ClothingDarkBlue"),
            Feet = new IProfileClothingItem("ShoesBlack", "ClothingGray"),
            Accesory = new IProfileClothingItem("Mask", "ClothingDarkBlue"),
        },
        new IProfile()
        {
            Name = "Assassin",
            Gender = Gender.Male,
            Skin = new IProfileClothingItem("Normal", "Skin4", "ClothingLightGray"),
            ChestOver = new IProfileClothingItem("Robe", "ClothingDarkBlue"),
            ChestUnder = new IProfileClothingItem("ShirtWithTie", "ClothingLightGray", "ClothingDarkGray"),
            Legs = new IProfileClothingItem("PantsBlack", "ClothingDarkBlue"),
            Feet = new IProfileClothingItem("ShoesBlack", "ClothingGray"),
            Accesory = new IProfileClothingItem("Mask", "ClothingDarkBlue"),
        },
        new IProfile()
        {
            Name = "Assassin",
            Gender = Gender.Female,
            Skin = new IProfileClothingItem("Normal_fem", "Skin4", "ClothingLightGray"),
            ChestUnder = new IProfileClothingItem("SweaterBlack_fem", "ClothingDarkBlue"),
            Legs = new IProfileClothingItem("PantsBlack_fem", "ClothingDarkBlue"),
            Feet = new IProfileClothingItem("ShoesBlack", "ClothingGray"),
            Accesory = new IProfileClothingItem("Balaclava", "ClothingDarkBlue"),
        }
    };
    
    foreach (IPlayer player in allPlayers)
    {
        if (!player.IsDead)
        {
            int hp = player.IsBot ? 200 : 500;
            PlayerModifiers mods = player.GetModifiers();
            mods.SizeModifier *= 0.8f;
            mods.MaxHealth = 500;
            mods.CurrentHealth = 500;
            if (player.IsBot) {
                mods.InfiniteAmmo = 1;
            }
            if (player.GetTeam() == PlayerTeam.Team2)
            {
                mods.SizeModifier *= 1.1f;
                mods.MeleeDamageDealtModifier = 1.3f;
                mods.RunSpeedModifier = 0.85f;
            }
            player.SetModifiers(mods);
            player.SetSpeedBoostTime(999999);

            if (player.GetTeam() == PlayerTeam.Team1)
            {
                player.GiveWeaponItem(WeaponItem.BOW);
                player.GiveWeaponItem(WeaponItem.BAT);
                
                // Assign unique profile to Team 1 bots
                if (player.IsBot)
                {
                    player.SetProfile(team1Profiles[team1ProfileIndex % team1Profiles.Length]);
                    team1ProfileIndex++;
                }
            }
            
            // Assign unique profile to Team 2 bots
            if (player.GetTeam() == PlayerTeam.Team2 && player.IsBot)
            {
                player.SetProfile(team2Profiles[team2ProfileIndex % team2Profiles.Length]);
                team2ProfileIndex++;
            }
        }
    }
}