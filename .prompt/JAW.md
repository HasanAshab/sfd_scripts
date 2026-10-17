## Now lets add "Jaw Titan"

- Stats:
Health +10%
Melee damage +25%
Force +0%
Speed -0%
Energy +25%

- Profile:
ply.SetProfile(
    new IProfile()
    {
        Name = "jawT",
        Gender = Gender.Female,
        Skin = new IProfileClothingItem("Zombie_fem", ""),
        Accesory = new IProfileClothingItem("GoalieMask", ""),
    }
);

- Abilities:
    - Grab damage: same as pure titan
    - Claw: When jump attacks:
        - when opponent is facing toward the jaw titan:
            - damage = 7 * Melee dam dealth mod * 2
            - play MacheteDraw sound.
        - when jaw titan attacked on victims back:
            - damage = 7 * Melee dam dealth mod * 4
            - play KatanaDraw sound.
