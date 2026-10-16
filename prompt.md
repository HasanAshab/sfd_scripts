## now the titan we have now will be called "Pure Titan". We will add more types of titans like AOT has. starting with attack titan:

- Transformation:
when a player gots strength boost and you transform them to titan, there will be 50% of chance to be Pure or Attack titan.

- Stats:
Health +40%
Melee damage +32%
Speed -20%
Energy +30%

- Profile:
ply.SetProfile(
    new IProfile()
    {
        Gender = Gender.Male,
        Skin = new IProfileClothingItem("Normal", "Skin2", "ClothingDarkGray"),
        Head = new IProfileClothingItem("AviatorHat2", "ClothingDarkGray", "ClothingLightGreen"),
        Accesory = new IProfileClothingItem("Glasses", "ClothingGreen", "ClothingGreen"),
    }
);

- Abilities:
    - Height: its not random, but with a algo, here is psudo code
        OwnTeamTotalHP = sum of current health of all players on same team (e.g Own team Team1)
        OpponentTeamTotalHP = ....  on other teams (e.g Opponent team is Team2,3,4 if own team is 1)
        Differ = OpponentTeamTotalHP / OwnTeamTotalHP
        SizeMod = Make a formula based on the Differ we got, I give you 3 flag points, your formula should scale automatically to respect those flags:
            Differ      Size
          1 or less    0.920f
            1.7         1.250f
            2.5        1.600f
          3 or more    1.600f + strength boost infinite

        and the hight stab will be same as pure titan

    - Slow motion: on virtual key ACTIVATE, cut 20% of max energy as cost and 

    - Hardening: on virtual key SHEATHE toggle, cut 7% of max energy as cost and switch the titan to these forms:
        - Offensive: 
            - melee damage dealth mod +20%, 
            - set profile: new IProfile()
                {
                    Gender = Gender.Male,
                    Skin = new IProfileClothingItem("Normal", "Skin2", "ClothingDarkGray"),
                    Head = new IProfileClothingItem("AviatorHat2", "ClothingDarkGray", "ClothingLightGreen"),
                    Hands = new IProfileClothingItem("SafetyGloves", "ClothingLightGray"),
                    Accesory = new IProfileClothingItem("Glasses", "ClothingGreen", "ClothingGreen"),
                }
        - Defensive:
            - speed -20%
            - defence +45% (both projectile and melee damage taken mod decrease by 45%)
            - set profile: new IProfile()
                {
                    Gender = Gender.Male,
                    Skin = new IProfileClothingItem("Normal", "Skin2", "ClothingDarkGray"),
                    Head = new IProfileClothingItem("AviatorHat2", "ClothingDarkGray", "ClothingLightGreen"),
                    ChestUnder = new IProfileClothingItem("SleevelessShirt", "ClothingLightGray"),
                    Accesory = new IProfileClothingItem("Glasses", "ClothingGreen", "ClothingGreen"),
                }
        - Neutral: Set profile to inital attack titan and everything as before hardening
