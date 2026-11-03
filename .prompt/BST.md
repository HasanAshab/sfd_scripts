since last you had worked on the Titan.cs script last time, it got a very major update. so before working on my new tasks, note that dont revert the new updates. respect them instead. then implement the new titan. and you dont have to write documentation. 





## now lets add "Beast Titan" label: BST:

- Stats:
Health +10%
Speed +1%
Energy +1%
Melee damage +0%
Force +0%
Size = 1.15f

- Profile:
set profile new IProfile()
{
    Gender = Gender.Male,
    Skin = new IProfileClothingItem("BearSkin", ""),
}

- Abilities:
    - Rock Throw:
        - when ever throws shuriken, remove that thrown and replace with a large text '.' simulating a large stone
        - read the ./TEMP.cs script for psudo code
        - when he picks up fireammo, the next 3 rock throws will be of fire powered (also on the psudo)
        - when picks up a WeaponItemType of { Handgun, Rifle, Melee }, remove that and give 2 shurikin.
        - Rock Mode [ Switch on SHEATHE virtual key toggle ]
            * Single Large Rock (Default)
                - psudo code have its implementation
                - hits on any player, including ally
                - On Hit:
                    - play sound of explosive
                    - effect = knock back a bit
                    - damage = 7 * Titan Melee dam dealt mod * 9

            * Multiple Small Rocks:
                - 9 small rocks, each size is (Large Rock / 9)
                - hits only on enemy player
                - On Hit each:
                    - play sound of BulletHitMetal
                    - effect = 11% chance to fall (use quickcommand helper)
                    - damage = 7 * Titan Melee dam dealt mod