its already done, and also i have updated the height prop of each players, so re read it before modifying.

Now add some troops.

Note:
- add a global var HIT_POINT = 14
- and they all will not search for items  
    BotBehaviorSet bS = bot.GetBotBehaviorSet();
    bS.SearchItems = 0;
    bot.SetBotBehaviorSet(bS)
- all the troops and main players will get refill their ammo (the dedicated ones, like p1 and p2 will get katana, bow. stickman will cue stick ..etc) if they dont have a item on that slot
- on 7 sec interval, check p1 and p2 energy, based on it, summon troops but dont cut the energy of the player. assume p1 has 150 current energy, then select all the troop types which are elegible, then do a random, then assume we got Bowman, so 150 - 30 = 120 then again select all the elegible troops and do the same algo until there are no more eligible troops. summon them on the same location of their leader
- all troops will guard their leader (p1/p2). if p1/p2 toogles Sheathe weapon key, then remove the guard and if again toggle, re set the guard to leader.
- initially summon: 4 Stickman, 2 Knife, 2 Bowman, 1 Knight, 1 Axeman on each side (p1 and p2)
- hide troops team and name + disable camera focus on them. 
- For easy team recognition set ChestOver color to 

Stick Man:
    energy requirement: 15
    health = 1 * HIT_POINT
    behavior = easy
    weapon = cue stick
    profile: new IProfile()
        {
            Name = "Stickman",
            Gender = Gender.Female,
            Skin = new IProfileClothingItem("Normal_fem", "Skin3", "ClothingLightGreen"),
            Head = new IProfileClothingItem("WoolCap", "ClothingGray"),
            ChestOver = new IProfileClothingItem("Apron_fem", "ClothingGray"),
            Hands = new IProfileClothingItem("SafetyGloves_fem", "ClothingGray"),
            Legs = new IProfileClothingItem("PantsBlack_fem", "ClothingGray"),
            Feet = new IProfileClothingItem("ShoesBlack", "ClothingGray"),
        }


