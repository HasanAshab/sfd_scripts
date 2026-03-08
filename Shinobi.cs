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
            mods.SizeModifier *= 0.9f;
            mods.MaxHealth = 500;
            mods.CurrentHealth = 500;
            player.SetModifiers(mods);
            player.SetSpeedBoostTime(999999);
        }
    }
}