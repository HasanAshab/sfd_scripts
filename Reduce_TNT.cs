public void OnStartup()
{
    IObject[] barrels = Game.GetObjectsByName("BarrelExplosive"); // check exact name, see note below

    for (int i = 0; i < barrels.Length; i++)
    {
        // Remove every 2nd barrel (halves the count)
        if (i % 2 == 0)
            barrels[i].Remove();
    }
}