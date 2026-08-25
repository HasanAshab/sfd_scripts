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
    bot.SetNametagVisible(false);
    bot.SetStatusBarsVisible(false);    
    bot.SetCameraSecondaryFocusMode(CameraFocusMode.Ignore);
- For easy team recognition set ChestOver color to ClothingDarkOrange for P1 troops and ClothingDarkBlue for P2 troops
- all troops profile's skinN should be skin1 to skin4 randomly (
    Skin = new IProfileClothingItem("Normal_fem", "SkinN", "ClothingLightGreen")
)

StickMan:
    energy requirement: 15
    health = 1 * HIT_POINT
    behavior = PredefinedAIType.BotD
    weapon = cue stick
    size = * 0.8 (-20%)
    profile: new IProfile()
        {
            Name = "Stickman",
            Gender = Gender.Female,
            Skin = new IProfileClothingItem("Normal_fem", "SkinN", "ClothingLightGreen"),
            Head = new IProfileClothingItem("WoolCap", "ClothingGray"),
            ChestOver = new IProfileClothingItem("Apron_fem", "ClothingGray"),
            Hands = new IProfileClothingItem("SafetyGloves_fem", "ClothingGray"),
            Legs = new IProfileClothingItem("PantsBlack_fem", "ClothingGray"),
            Feet = new IProfileClothingItem("ShoesBlack", "ClothingGray"),
        }
    
knifeMan:
    energy requirement: 30
    health = 2 * HIT_POINT
    behavior = PredefinedAIType.BotD
    weapon = knife
    size = * 0.8 (-20%)
    profile: new IProfile()
        {
            Name = "Knifeman",
            Gender = Gender.Female,
            Skin = new IProfileClothingItem("Normal_fem", "Skin3", "ClothingLightGreen"),
            Head = new IProfileClothingItem("SpikedHelmet", "ClothingGray"),
            ChestOver = new IProfileClothingItem("Apron_fem", "ClothingGray"),
            ChestUnder = new IProfileClothingItem("ShirtWithBowtie_fem", "ClothingGray", "ClothingLightGray"),
            Hands = new IProfileClothingItem("SafetyGloves_fem", "ClothingGray"),
            Legs = new IProfileClothingItem("PantsBlack_fem", "ClothingGray"),
            Feet = new IProfileClothingItem("ShoesBlack", "ClothingGray"),
            Accesory = new IProfileClothingItem("Scarf", "ClothingLightGray"),
        }
    
BowMan:
    energy requirement: 30
    health = 1 * HIT_POINT
    behavior = PredefinedAIType.BotC
    weapon = bow
    size = * 0.8 (-20%)
    speed = * 1.3 (+30%)
    Extra:
        bS.MeleeUsage = false;
    profile: new IProfile()
        {
            Name = "Bowman",
            Gender = Gender.Female,
            Skin = new IProfileClothingItem("Normal_fem", "Skin3", "ClothingLightGreen"),
            Head = new IProfileClothingItem("StylishHat_fem", "ClothingGray", "ClothingLightGray"),
            ChestOver = new IProfileClothingItem("Apron_fem", "ClothingGray"),
            ChestUnder = new IProfileClothingItem("ShirtWithBowtie_fem", "ClothingGray", "ClothingLightGray"),
            Legs = new IProfileClothingItem("PantsBlack_fem", "ClothingGray"),
            Feet = new IProfileClothingItem("ShoesBlack", "ClothingBrown"),
        }


Knight:
    energy requirement: 50
    health = 2 * HIT_POINT
    behavior = PredefinedAIType.BotB
    weapon = machete
    profile: new IProfile()
        {
            Name = "Knight",
            Gender = Gender.Male,
            Skin = new IProfileClothingItem("Normal", "Skin3", "ClothingLightGreen"),
            ChestOver = new IProfileClothingItem("Apron", "ClothingGray"),
            ChestUnder = new IProfileClothingItem("BodyArmor", "ClothingGray"),
            Hands = new IProfileClothingItem("Gloves", "ClothingGray"),
            Legs = new IProfileClothingItem("CamoPants", "ClothingGray", "ClothingDarkGray"),
            Feet = new IProfileClothingItem("ShoesBlack", "ClothingBrown"),
            Accesory = new IProfileClothingItem("Balaclava", "ClothingGray"),
        }


Axeman:
    energy requirement: 50
    health = 4 * HIT_POINT
    behavior = PredefinedAIType.BotC
    weapon = axe
    speed = * 0.7 (-30%)
    profile: new IProfile()
        {
            Name = "Axeman",
            Gender = Gender.Male,
            Skin = new IProfileClothingItem("Normal", "Skin1", "ClothingLightGreen"),
            Head = new IProfileClothingItem("Afro", "ClothingDarkGray"),
            ChestOver = new IProfileClothingItem("KevlarVest", "ClothingGray"),
            Hands = new IProfileClothingItem("SafetyGloves", "ClothingGray"),
            Legs = new IProfileClothingItem("Skirt", "ClothingGray"),
            Feet = new IProfileClothingItem("RidingBoots", "ClothingGray"),
            Accesory = new IProfileClothingItem("ClownMakeup", "ClothingGray"),
        }



