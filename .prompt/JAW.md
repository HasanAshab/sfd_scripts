## Now lets add "Jaw Titan"

- Stats:
Health +0%
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
        - read Milita.cs script, assasin troops backstab for ref only if you cant
        - if victim got more than 30% of extra damage (e.g Victim max hp is 100 and current 10, Jaw claw caused 40 damage, then gib)
    - Hunt: When catch victims on dive:
        - 
