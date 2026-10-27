Now make bot usage for cart titan. 

# when a bot gets cart titan:

## Mode Switch:
    - Rescue: when an ally with hp less than 20% remaining
    - Offensive: If no ally with hp less than 20% remaining

## Mode Usage:
    - Rescue: 
        - choose a ally to rescue among all, whos hp is less than 20%.
        - the choose algo is: The closest one (4 Points) + Is p1 or p2 (2 Points) + Is titan owner (3 Points) + is Armored titan (2 Points).
            Note that a player can have points from multiple criteria. (e.g, a player is p1 and also a Armored titan and we know armored titan is also a titan = 2 + 3 + 2 = 7 Points).
        
        - run away from enemies (read ./Examples/Bot_Avoid_Explosives.cs, learn from it, and instead of explosives, avoid enemy player) until the ally rescuing has >80% hp (you dont have to heal him, as he will be healed naturally) then release him at the spot.
        