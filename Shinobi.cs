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
            }


        }
    }
}