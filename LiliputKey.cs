// Character team setup script
// Team1: Undir, Mew
// Team2: Udoy, Poshu, 3Tala, ChipaBhoot, Jhumur

private const int BOT_COUNT = 7;
private bool botsCreated = false;
private Random random = new Random();

public void OnStartup()
{
    SetupBots();
}

public void SetupBots()
{
    if (botsCreated) return;

    Vector2[] spawnPositions = GetBotSpawnPositions();

    // Team1
    CreateCharacter(
        "Undir",
        spawnPositions[0],
        PlayerTeam.Team1,
        PredefinedAIType.BotD,
        0.8f,
        new IProfile()
        {
            Name = "Undir",
            Gender = Gender.Female,
            Skin = new IProfileClothingItem("Normal_fem", "Skin4", "ClothingLightGreen"),
            Head = new IProfileClothingItem("Hood", "ClothingLightBlue"),
            ChestOver = new IProfileClothingItem("Apron_fem", "ClothingLightBlue"),
            ChestUnder = new IProfileClothingItem("HawaiiShirt_fem", "ClothingLightBlue", "ClothingLightGray"),
            Legs = new IProfileClothingItem("PantsBlack_fem", "ClothingDarkGray"),
            Feet = new IProfileClothingItem("RidingBoots", "ClothingDarkGray"),
        }
    );

    CreateCharacter(
        "Mew",
        spawnPositions[1],
        PlayerTeam.Team1,
        PredefinedAIType.BotC,
        0.8f,
        new IProfile()
        {
            Name = "Mew",
            Gender = Gender.Female,
            Skin = new IProfileClothingItem("Normal_fem", "Skin2", "ClothingLightGreen"),
            Head = new IProfileClothingItem("Buzzcut", "ClothingDarkGray"),
            ChestUnder = new IProfileClothingItem("ShirtWithBowtie_fem", "ClothingDarkPurple", "ClothingLightPink"),
            Legs = new IProfileClothingItem("PantsBlack_fem", "ClothingBlue"),
            Feet = new IProfileClothingItem("ShoesBlack", "ClothingBrown"),
        }
    );

    // Team2
    CreateCharacter(
        "Udoy",
        spawnPositions[2],
        PlayerTeam.Team2,
        PredefinedAIType.BotA,
        0.9f,
        new IProfile()
        {
            Name = "Udoy",
            Gender = Gender.Male,
            Skin = new IProfileClothingItem("Normal", "Skin2", "ClothingLightGreen"),
            Head = new IProfileClothingItem("Buzzcut", "ClothingDarkGray"),
            ChestOver = new IProfileClothingItem("Jacket", "ClothingCyan", "ClothingCyan"),
            ChestUnder = new IProfileClothingItem("Shirt", "ClothingLightGray"),
            Legs = new IProfileClothingItem("PantsBlack", "ClothingDarkGray"),
            Feet = new IProfileClothingItem("ShoesBlack", "ClothingBrown"),
            Accesory = new IProfileClothingItem("Glasses", "ClothingLightGray", "ClothingLightGray"),
        }
    );

    CreateCharacter(
        "Poshu",
        spawnPositions[3],
        PlayerTeam.Team2,
        PredefinedAIType.BotA,
        0.9f,
        new IProfile()
        {
            Name = "Poshu",
            Gender = Gender.Male,
            Skin = new IProfileClothingItem("Normal", "Skin3", "ClothingLightGreen"),
            Head = new IProfileClothingItem("Buzzcut", "ClothingDarkGray"),
            ChestUnder = new IProfileClothingItem("TShirt", "ClothingLightGray"),
            Legs = new IProfileClothingItem("Shorts", "ClothingGray"),
            Feet = new IProfileClothingItem("ShoesBlack", "ClothingBrown"),
        }
    );

    CreateCharacter(
        "3Tala",
        spawnPositions[4],
        PlayerTeam.Team2,
        PredefinedAIType.BotA,
        0.9f,
        new IProfile()
        {
            Name = "3Tala",
            Gender = Gender.Female,
            Skin = new IProfileClothingItem("Normal_fem", "Skin1", "ClothingLightGreen"),
            Head = new IProfileClothingItem("Helmet2", "ClothingDarkGray"),
            ChestOver = new IProfileClothingItem("Robe_fem", "ClothingGray"),
            Legs = new IProfileClothingItem("Pants_fem", "ClothingLightGray"),
            Feet = new IProfileClothingItem("HighHeels", "ClothingDarkGray"),
        }
    );

    CreateCharacter(
        "ChipaBhoot",
        spawnPositions[5],
        PlayerTeam.Team2,
        PredefinedAIType.BotA,
        0.9f,
        new IProfile()
        {
            Name = "ChipaBhoot",
            Gender = Gender.Female,
            Skin = new IProfileClothingItem("Normal_fem", "Skin2", "ClothingLightGreen"),
            Head = new IProfileClothingItem("Buzzcut", "ClothingDarkGray"),
            ChestOver = new IProfileClothingItem("Robe_fem", "ClothingDarkGreen"),
            Legs = new IProfileClothingItem("PantsBlack_fem", "ClothingDarkGray"),
            Feet = new IProfileClothingItem("ShoesBlack", "ClothingBrown"),
        }
    );

    CreateCharacter(
        "Jhumur",
        spawnPositions[6],
        PlayerTeam.Team2,
        PredefinedAIType.BotA,
        0.9f,
        new IProfile()
        {
            Name = "Jhumur",
            Gender = Gender.Female,
            Skin = new IProfileClothingItem("Normal_fem", "Skin1", "ClothingLightGray"),
            Head = new IProfileClothingItem("Buzzcut", "ClothingDarkGray"),
            ChestOver = new IProfileClothingItem("SuitJacketBlack_fem", "ClothingDarkGray"),
            ChestUnder = new IProfileClothingItem("ShirtWithTie_fem", "ClothingLightGray", "ClothingDarkGray"),
            Legs = new IProfileClothingItem("PantsBlack_fem", "ClothingDarkGray"),
            Feet = new IProfileClothingItem("ShoesBlack", "ClothingDarkBrown"),
        }
    );

    botsCreated = true;
}

private void CreateCharacter(
    string name,
    Vector2 spawnPosition,
    PlayerTeam team,
    PredefinedAIType aiType,
    float sizeModifier,
    IProfile profile)
{
    IPlayer bot = Game.CreatePlayer(spawnPosition);
    if (bot == null) return;

    bot.SetTeam(team);
    bot.SetBotName(name);
    bot.SetBotBehavior(new BotBehavior(true, aiType));
    bot.SetCameraSecondaryFocusMode(CameraFocusMode.Ignore);
    bot.SetProfile(profile);

    PlayerModifiers mods = bot.GetModifiers();
    mods.SizeModifier = sizeModifier;
    bot.SetModifiers(mods);
}

private Vector2[] GetBotSpawnPositions()
{
    IObjectPathNode[] pathNodes = Game.GetObjects<IObjectPathNode>().Where(path =>
        path.GetNodeEnabled() &&
        !path.GetIsElevatorNode() &&
        (path.GetPathNodeType() == PathNodeType.Ground ||
         path.GetPathNodeType() == PathNodeType.Platform)
    ).ToArray();

    Vector2[] spawnPositions = new Vector2[BOT_COUNT];

    if (pathNodes.Length >= BOT_COUNT)
    {
        // Shuffle nodes, then use seven unique spawn positions.
        List<IObjectPathNode> shuffledNodes = pathNodes.ToList();
        for (int i = shuffledNodes.Count - 1; i > 0; i--)
        {
            int j = random.Next(0, i + 1);
            IObjectPathNode temp = shuffledNodes[i];
            shuffledNodes[i] = shuffledNodes[j];
            shuffledNodes[j] = temp;
        }

        for (int i = 0; i < BOT_COUNT; i++)
            spawnPositions[i] = shuffledNodes[i].GetWorldPosition();

        return spawnPositions;
    }

    // Not enough usable path nodes: use existing player positions as team spawn points.
    IPlayer[] players = Game.GetPlayers();
    Vector2 fallbackPos = players.Length > 0
        ? players[0].GetWorldPosition()
        : Vector2.Zero;

    Vector2 team1SpawnPos = fallbackPos;
    Vector2 team2SpawnPos = players.Length > 1
        ? players[1].GetWorldPosition()
        : fallbackPos;

    bool foundTeam1 = false;
    bool foundTeam2 = false;

    foreach (IPlayer player in players)
    {
        if (!foundTeam1 && player.GetTeam() == PlayerTeam.Team1)
        {
            team1SpawnPos = player.GetWorldPosition();
            foundTeam1 = true;
        }
        else if (!foundTeam2 && player.GetTeam() == PlayerTeam.Team2)
        {
            team2SpawnPos = player.GetWorldPosition();
            foundTeam2 = true;
        }
    }

    spawnPositions[0] = team1SpawnPos; // Undir
    spawnPositions[1] = team1SpawnPos; // Mew
    spawnPositions[2] = team2SpawnPos; // Udoy
    spawnPositions[3] = team2SpawnPos; // Poshu
    spawnPositions[4] = team2SpawnPos; // 3Tala
    spawnPositions[5] = team2SpawnPos; // ChipaBhoot
    spawnPositions[6] = team2SpawnPos; // Jhumur

    return spawnPositions;
}
