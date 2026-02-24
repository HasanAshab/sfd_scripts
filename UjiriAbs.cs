private IPlayer p1 = null;
private IPlayer p2 = null;


// Bot special ability tracking
private IPlayer timpaBot = null;
private IPlayer bichiBot = null;
private IPlayer kokolaBot = null;
private IPlayer edurBot = null;
private IPlayer xrayBot = null;
private IPlayer pakhiBot = null;


// Random number generator
private Random random = new Random();


// Team assignments (fixed teams)
private bool botsCreated = false;

public void OnStartup()
{
    SetupBots();

    IPlayer[] players = Game.GetPlayers();
    p1 = players.Length >= 1 ? players[0] : null;
    p2 = players.Length >= 2 ? players[1] : null;

    PlayerModifiers p1Mods = p1.GetModifiers();

    p1Mods.RunSpeedModifier = 1.15f;
    p1Mods.SprintSpeedModifier = 1.3f;

    p1Mods.MaxEnergy = (int)(p1Mods.MaxEnergy * 1.2f);
    p1Mods.CurrentEnergy = (int)(p1Mods.CurrentEnergy * 1.2f);
    p1Mods.EnergyRechargeModifier *= 1.2f;
    p1.SetModifiers(p1Mods);


    PlayerModifiers p2Mods = p2.GetModifiers();

    p2Mods.SizeModifier = 1.12f;
    p2Mods.RunSpeedModifier = 0.8f;
    p2Mods.SprintSpeedModifier = 0.95f;
    p2Mods.MeleeForceModifier *= 1.2f;
    p2Mods.MeleeDamageDealtModifier *= 1.4f;

    p2.SetModifiers(p2Mods);

    TransformPlayersToUjiri();
}


public void SetupBots()
{
    // Only create bots once
    if (botsCreated) return;
    
    // Get available spawn positions from path nodes
    IObjectPathNode[] pathNodes = Game.GetObjects<IObjectPathNode>().Where(path => 
        path.GetNodeEnabled() && 
        !path.GetIsElevatorNode() &&
        (path.GetPathNodeType() == PathNodeType.Ground || path.GetPathNodeType() == PathNodeType.Platform)
    ).ToArray();
    
    Vector2[] spawnPositions = new Vector2[6];
    
    if (pathNodes.Length >= 6)
    {
        // Enough path nodes - use random unique positions
        List<IObjectPathNode> shuffledNodes = pathNodes.ToList();
        for (int i = shuffledNodes.Count - 1; i > 0; i--)
        {
            int j = random.Next(0, i + 1);
            IObjectPathNode temp = shuffledNodes[i];
            shuffledNodes[i] = shuffledNodes[j];
            shuffledNodes[j] = temp;
        }
        
        for (int i = 0; i < 6; i++)
        {
            spawnPositions[i] = shuffledNodes[i].GetWorldPosition();
        }
    }
    else
    {
        // Not enough path nodes - spawn at human player positions based on teams
        IPlayer[] players = Game.GetPlayers();
        Vector2 team1SpawnPos = Vector2.Zero;
        Vector2 team2SpawnPos = Vector2.Zero;
        
        // Find human players and their team positions
        IPlayer p1 = players.Length >= 1 ? players[0] : null;
        IPlayer p2 = players.Length >= 2 ? players[1] : null;
        
        // Determine spawn positions based on team assignments
        if (p1 != null && p2 != null)
        {
            PlayerTeam p1Team = p1.GetTeam();
            PlayerTeam p2Team = p2.GetTeam();
            
            if (p1Team == PlayerTeam.Team1)
            {
                team1SpawnPos = p1.GetWorldPosition();
            }
            else if (p1Team == PlayerTeam.Team2)
            {
                team2SpawnPos = p1.GetWorldPosition();
            }
            
            if (p2Team == PlayerTeam.Team1)
            {
                team1SpawnPos = p2.GetWorldPosition();
            }
            else if (p2Team == PlayerTeam.Team2)
            {
                team2SpawnPos = p2.GetWorldPosition();
            }
            
            // If neither player is on Team1 or Team2, use default assignment
            if (team1SpawnPos == Vector2.Zero && team2SpawnPos == Vector2.Zero)
            {
                team1SpawnPos = p2.GetWorldPosition(); // Team1 bots spawn at P1
                team2SpawnPos = p1.GetWorldPosition(); // Team2 bots spawn at P2
            }
            else if (team1SpawnPos == Vector2.Zero)
            {
                // Only Team2 position found, use the other player for Team1
                team1SpawnPos = (p1Team != PlayerTeam.Team2) ? p1.GetWorldPosition() : p2.GetWorldPosition();
            }
            else if (team2SpawnPos == Vector2.Zero)
            {
                // Only Team1 position found, use the other player for Team2
                team2SpawnPos = (p1Team != PlayerTeam.Team1) ? p1.GetWorldPosition() : p2.GetWorldPosition();
            }
        }
        else if (p1 != null)
        {
            // Only one player - use their position for both teams
            team1SpawnPos = p1.GetWorldPosition();
            team2SpawnPos = p1.GetWorldPosition();
        }
        else
        {
            // No players found - use default positions (0,0)
            team1SpawnPos = Vector2.Zero;
            team2SpawnPos = Vector2.Zero;
        }
        
        // Assign spawn positions: Team1 bots (Timpa, Bichi, Kokola) and Team2 bots (Edur, Xray, Pakhi, Psythic)
        spawnPositions[0] = team1SpawnPos; // Timpa
        spawnPositions[1] = team1SpawnPos; // Bichi
        spawnPositions[2] = team1SpawnPos; // Kokola
        spawnPositions[3] = team2SpawnPos; // Edur
        spawnPositions[4] = team2SpawnPos; // Xray
        spawnPositions[5] = team2SpawnPos; // Pakhi
    }
    
    // Create Team1 bots (Normal AI): Timpa, Bichi, Kokola
    timpaBot = Game.CreatePlayer(spawnPositions[0]);
    if (timpaBot != null)
    {
        timpaBot.SetTeam(PlayerTeam.Team1);
        timpaBot.SetBotName("Timpa");
        timpaBot.SetBotBehavior(new BotBehavior(true, PredefinedAIType.BotD));
        timpaBot.SetCameraSecondaryFocusMode(CameraFocusMode.Ignore);
        timpaBot.SetProfile(new IProfile()
        {
            Name = "Timpa",
            Gender = Gender.Male,
            Skin = new IProfileClothingItem("Normal", "Skin1", "ClothingLightGray"),
            Head = new IProfileClothingItem("Buzzcut", "ClothingDarkGray"),
            ChestUnder = new IProfileClothingItem("SleevelessShirt", "ClothingGreen"),
            Legs = new IProfileClothingItem("Skirt", "ClothingBlue"),
            Feet = new IProfileClothingItem("ShoesBlack", "ClothingBrown"),
        });
    }
    
    bichiBot = Game.CreatePlayer(spawnPositions[1]);
    if (bichiBot != null)
    {
        bichiBot.SetTeam(PlayerTeam.Team1);
        bichiBot.SetBotName("Bichi");
        bichiBot.SetBotBehavior(new BotBehavior(true, PredefinedAIType.BotD));
        bichiBot.SetCameraSecondaryFocusMode(CameraFocusMode.Ignore);
        bichiBot.SetProfile(new IProfile()
{
    Name = "Bichi",
    Gender = Gender.Male,
    Skin = new IProfileClothingItem("Normal", "Skin3", "ClothingLightGreen"),
    ChestUnder = new IProfileClothingItem("Sweater", "ClothingGreen"),
    Hands = new IProfileClothingItem("GlovesBlack", "ClothingLightGray"),
    Legs = new IProfileClothingItem("PantsBlack", "ClothingBlue"),
    Feet = new IProfileClothingItem("ShoesBlack", "ClothingBrown"),
});
    }
    
    kokolaBot = Game.CreatePlayer(spawnPositions[2]);
    if (kokolaBot != null)
    {
        kokolaBot.SetTeam(PlayerTeam.Team1);
        kokolaBot.SetBotName("Kokola");
        kokolaBot.SetBotBehavior(new BotBehavior(true, PredefinedAIType.BotC));
        kokolaBot.SetCameraSecondaryFocusMode(CameraFocusMode.Ignore);
        kokolaBot.SetProfile(new IProfile()
{
    Name = "Kokola",
    Gender = Gender.Female,
    Skin = new IProfileClothingItem("Normal_fem", "Skin4", "ClothingLightGreen"),
    Head = new IProfileClothingItem("Buzzcut", "ClothingDarkGray"),
    ChestUnder = new IProfileClothingItem("SleevelessShirt_fem", "ClothingLightGray"),
    Legs = new IProfileClothingItem("Skirt_fem", "ClothingBlue"),
    Feet = new IProfileClothingItem("ShoesBlack", "ClothingBrown"),
});
    }
    
    // Create Team2 bots (Expert AI): Edur, Xray, Pakhi
    edurBot = Game.CreatePlayer(spawnPositions[3]);
    if (edurBot != null)
    {
        edurBot.SetTeam(PlayerTeam.Team2);
        edurBot.SetBotName("Edur");
        edurBot.SetBotBehavior(new BotBehavior(true, PredefinedAIType.CompanionC));
        edurBot.SetCameraSecondaryFocusMode(CameraFocusMode.Ignore);
        edurBot.SetProfile(new IProfile()
         {
    Name = "Edur",
    Gender = Gender.Male,
    Skin = new IProfileClothingItem("BearSkin", ""),
    ChestUnder = new IProfileClothingItem("Shirt", "ClothingLightGray"),
    Legs = new IProfileClothingItem("PantsBlack", "ClothingDarkGray"),
    Feet = new IProfileClothingItem("ShoesBlack", "ClothingBrown"),
});
    }

    xrayBot = Game.CreatePlayer(spawnPositions[4]);
    if (xrayBot != null)
    {
        xrayBot.SetTeam(PlayerTeam.Team2);
        xrayBot.SetBotName("Xray");
        xrayBot.SetBotBehavior(new BotBehavior(true, PredefinedAIType.CompanionD));
        xrayBot.SetCameraSecondaryFocusMode(CameraFocusMode.Ignore);
        xrayBot.SetProfile(new IProfile(){
    Name = "Xray",
    Gender = Gender.Female,
    Skin = new IProfileClothingItem("Normal_fem", "Skin1", "ClothingLightGreen"),
    Head = new IProfileClothingItem("Buzzcut", "ClothingDarkGray"),
    ChestUnder = new IProfileClothingItem("SleevelessShirt_fem", "ClothingLightGray"),
    Legs = new IProfileClothingItem("Skirt_fem", "ClothingBlue"),
    Feet = new IProfileClothingItem("ShoesBlack", "ClothingBrown"),
    Accesory = new IProfileClothingItem("Glasses", "ClothingLightGray", "ClothingLightGray"),
});
    }
    
    pakhiBot = Game.CreatePlayer(spawnPositions[5]);
    if (pakhiBot != null)
    {
        pakhiBot.SetTeam(PlayerTeam.Team2);
        pakhiBot.SetBotName("Pakhi");
        pakhiBot.SetBotBehavior(new BotBehavior(true, PredefinedAIType.CompanionD));
        pakhiBot.SetCameraSecondaryFocusMode(CameraFocusMode.Ignore);
        pakhiBot.SetProfile(new IProfile()
{
    Name = "Pakhi",
    Gender = Gender.Female,
    Skin = new IProfileClothingItem("Normal_fem", "Skin3", "ClothingLightGreen"),
    Head = new IProfileClothingItem("SantaHat", "ClothingLightGray"),
    ChestOver = new IProfileClothingItem("Coat_fem", "ClothingLightGray", "ClothingLightGray"),
    ChestUnder = new IProfileClothingItem("SleevelessShirt_fem", "ClothingLightGray"),
    Legs = new IProfileClothingItem("Skirt_fem", "ClothingBlue"),
    Feet = new IProfileClothingItem("ShoesBlack", "ClothingBrown"),
});
    }
    botsCreated = true;
}


public void TransformPlayersToUjiri()
{
    IPlayer[] players = Game.GetPlayers();
    foreach (IPlayer player in players)
    {
        PlayerModifiers mods = player.GetModifiers();
        mods.RunSpeedModifier *= 1.35f;
        mods.SprintSpeedModifier *= 1.45f;
        player.SetModifiers(mods);

        player.GiveWeaponItem(WeaponItem.GRENADES);
        player.GiveWeaponItem(WeaponItem.PISTOL);
    }
}