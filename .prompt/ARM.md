## Now lets add "Armored Titan"

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
        OwnTeamTotalHP = sum of current health of all players (including self) on same team (e.g Own team Team1 or only self if team is None)
        OpponentTeamTotalHP = ....  on other teams (e.g Opponent team is Team2,3,4 if own team is 1, or if own team is NONE then every single other players hp)
        Differ = OpponentTeamTotalHP / OwnTeamTotalHP
        SizeMod = Make a formula based on the Differ we got, I give you 3 flag points, your formula should scale automatically to respect those flags:
            Differ      Size
          1 or less    0.920f
            1.7         1.250f
            2.5        1.600f
          3 or more    1.600f + strength boost infinite

        and the hight stab will be same as pure titan

    - Slow motion: 
        - on virtual key ACTIVATE, cut 20% of max energy as cost and give SLOW_MO_5, then use PlayerCommandType.DrawPowerup command, and after 2 seconds, Game.RunCommand("/settime 1") 
        - Bot Usage: unlike human players, bots cant use this ability wisely, also i dont want, instead just give them better bot for fighting, like BotA


    - Hardening: on virtual key SHEATHE toggle, cut 7% of max energy as cost per second and switch the titan to hardening forms:
        - Available forms:
            - Offensive: 
                - melee damage dealth mod +20%, 
                - other players getting his melee hit will have a 20% chance to Stagger (PlayerCommandType.Stagger)
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
            - Neutral (default): Set profile to inital attack titan and everything as before hardening

        - Bot Usage: unlike human players, bots cant use this ability wisely
            - Triggers:
                - Offensive: target (IPlayer.GetBotTarget()) is close (25f), turn on this mode. then listen to own melee hit events, if he stops melee attack for 2s, then turn to nutral mode again
                - defensive: if any enemy (player with different team) aims and their point is at him and if no target is close, then use this mode. and after 2 seconds check these conditions again, if no longer true then switch back to neutral
            - Avoid triggers: sometimes the bot will avoid triggers:
                - if current energy is less than 30% of max, then there will be a 80% chance that he will not switch to any mode
                - when he is already on a mode and current energy is less than 30% of max, then there will be a 60% chance that he will switch back to neutral mode

- Disablities:
    - Grab damage: this titan not have the Grab damage power, but note that, he can get the damage from others grab ability