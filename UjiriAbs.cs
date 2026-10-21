// Bot special ability tracking
private IPlayer timpaBot = null;
private IPlayer bichiBot = null;
private IPlayer kokolaBot = null;
private IPlayer heitBot = null;
private IPlayer edurBot = null;
private IPlayer xrayBot = null;
private IPlayer pakhiBot = null;
private IPlayer hakkuBot = null;


// Random number generator
private Random random = new Random();


// Team assignments (fixed teams)
private bool botsCreated = false;

// Total number of bots to spawn
private const int BOT_COUNT = 8;

public void OnStartup()
{
    SetupBots();
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
    
    Vector2[] spawnPositions = new Vector2[BOT_COUNT];
    
    if (pathNodes.Length >= BOT_COUNT)
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
        
        for (int i = 0; i < BOT_COUNT; i++)
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
                team1SpawnPos = p2.GetWorldPosition(); // Team1 bots spawn at P2
                team2SpawnPos = p1.GetWorldPosition(); // Team2 bots spawn at P1
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
        
        // Assign spawn positions:
        // Team1 bots (Timpa, Bichi, Kokola, Heit) and Team2 bots (Edur, Xray, Pakhi, Hakku)
        spawnPositions[0] = team1SpawnPos; // Timpa
        spawnPositions[1] = team1SpawnPos; // Bichi
        spawnPositions[2] = team1SpawnPos; // Kokola
        spawnPositions[3] = team2SpawnPos; // Edur
        spawnPositions[4] = team2SpawnPos; // Xray
        spawnPositions[5] = team2SpawnPos; // Pakhi
        spawnPositions[6] = team1SpawnPos; // Heit
        spawnPositions[7] = team2SpawnPos; // Hakku
    }
    
    // Create Team1 bots (Normal AI): Timpa, Bichi, Kokola, Heit
    timpaBot = Game.CreatePlayer(spawnPositions[0]);
    if (timpaBot != null)
    {
        timpaBot.SetTeam(PlayerTeam.Team1);
        timpaBot.SetBotName("Timpa");
        timpaBot.SetBotBehavior(new BotBehavior(true, PredefinedAIType.BotD));
        timpaBot.SetCameraSecondaryFocusMode(CameraFocusMode.Ignore);
        PlayerModifiers mods = timpaBot.GetModifiers();
        mods.SizeModifier = 0.98f;
        timpaBot.SetModifiers(mods);
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
        PlayerModifiers mods = bichiBot.GetModifiers();
        mods.SizeModifier = 0.92f;
        bichiBot.SetModifiers(mods);
        bichiBot.SetProfile(new IProfile()
        {
            Name = "Bichi",
            Gender = Gender.Male,
            Skin = new IProfileClothingItem("Normal", "Skin3", "ClothingLightGreen"),
            ChestUnder = new IProfileClothingItem("Sweater", "ClothingGreen"),
            Hands = new IProfileClothingItem("GlovesBlack", "ClothingLightGray"),
            Legs = new IProfileClothingItem("Skirt", "ClothingBlue"),
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
        PlayerModifiers mods = kokolaBot.GetModifiers();
        mods.SizeModifier = 0.95f;
        kokolaBot.SetModifiers(mods);
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

    heitBot = Game.CreatePlayer(spawnPositions[6]);
    if (heitBot != null)
    {
        heitBot.SetTeam(PlayerTeam.Team1);
        heitBot.SetBotName("Heit");
        heitBot.SetBotBehavior(new BotBehavior(true, PredefinedAIType.BotD));
        heitBot.SetCameraSecondaryFocusMode(CameraFocusMode.Ignore);
        PlayerModifiers mods = heitBot.GetModifiers();
        mods.SizeModifier = 1.01f;
        mods.MeleeForceModifier = 0.7f;
        heitBot.SetModifiers(mods);
        heitBot.SetProfile(new IProfile()
        {
            Name = "Heit",
            Gender = Gender.Female,
            Skin = new IProfileClothingItem("Normal_fem", "Skin1", "ClothingBrown"),
            Head = new IProfileClothingItem("Buzzcut", "ClothingDarkGray"),
            ChestUnder = new IProfileClothingItem("TShirt_fem", "ClothingLightGray"),
            Legs = new IProfileClothingItem("Skirt_fem", "ClothingBlue"),
            Feet = new IProfileClothingItem("HighHeels", "ClothingDarkGray"),
            Accesory = new IProfileClothingItem("SantaMask", ""),
        });
    }
    
    // Create Team2 bots (Expert AI): Edur, Xray, Pakhi, Hakku
    edurBot = Game.CreatePlayer(spawnPositions[3]);
    if (edurBot != null)
    {
        edurBot.SetTeam(PlayerTeam.Team2);
        edurBot.SetBotName("Edur");
        edurBot.SetBotBehavior(new BotBehavior(true, PredefinedAIType.CompanionC));
        edurBot.SetCameraSecondaryFocusMode(CameraFocusMode.Ignore);
        PlayerModifiers mods = edurBot.GetModifiers();
        mods.SizeModifier = 0.99f;
        edurBot.SetModifiers(mods);
        edurBot.SetProfile(new IProfile()
        {
            Name = "Edur",
            Gender = Gender.Male,
            Skin = new IProfileClothingItem("Normal", "Skin3", "ClothingLightGreen"),
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
        PlayerModifiers mods = xrayBot.GetModifiers();
        mods.SizeModifier = 0.98f;
        xrayBot.SetModifiers(mods);
        xrayBot.SetProfile(new IProfile()
        {
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
        
        PlayerModifiers mods = pakhiBot.GetModifiers();
        mods.SizeModifier = 0.92f;
        pakhiBot.SetModifiers(mods);

        pakhiBot.SetProfile(new IProfile()
        {
            Name = "pakhi",
            Gender = Gender.Female,
            Skin = new IProfileClothingItem("Normal_fem", "Skin2", "ClothingLightGreen"),
            Head = new IProfileClothingItem("Buzzcut", "ClothingDarkGray"),
            ChestOver = new IProfileClothingItem("Coat_fem", "ClothingLightGray", "ClothingLightGray"),
            Hands = new IProfileClothingItem("Gloves", "ClothingLightPink"),
            Legs = new IProfileClothingItem("Pants_fem", "ClothingDarkGray"),
            Feet = new IProfileClothingItem("ShoesBlack", "ClothingBrown"),
        });
    }

    hakkuBot = Game.CreatePlayer(spawnPositions[7]);
    if (hakkuBot != null)
    {
        hakkuBot.SetTeam(PlayerTeam.Team2);
        hakkuBot.SetBotName("Hakku");
        hakkuBot.SetBotBehavior(new BotBehavior(true, PredefinedAIType.CompanionD));
        hakkuBot.SetCameraSecondaryFocusMode(CameraFocusMode.Ignore);
        PlayerModifiers mods = hakkuBot.GetModifiers();
        mods.SizeModifier = 0.86f;
        mods.MeleeForceModifier = 0.7f;
        hakkuBot.SetModifiers(mods);
        hakkuBot.SetProfile(new IProfile()
        {
            Name = "Hakku",
            Gender = Gender.Male,
            Skin = new IProfileClothingItem("Normal", "Skin3", "ClothingLightGreen"),
            ChestUnder = new IProfileClothingItem("TShirt", "ClothingLightGray"),
            Legs = new IProfileClothingItem("Skirt", "ClothingLightGray"),
            Feet = new IProfileClothingItem("ShoesBlack", "ClothingBrown"),
            Accesory = new IProfileClothingItem("SantaMask", ""),
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