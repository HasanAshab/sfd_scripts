public void OnStartup()
{
    MakeEveryoneSmaller();
}


public void MakeEveryoneSmaller()
{
    IPlayer[] allPlayers = Game.GetPlayers();
    
    foreach (IPlayer player in allPlayers)
    {
        if (!player.IsDead)
        {
            PlayerModifiers mods = player.GetModifiers();
            mods.SizeModifier *= 0.85f;
            player.SetModifiers(mods);
            player.SetSpeedBoostTime(999999);
        }
    }
}