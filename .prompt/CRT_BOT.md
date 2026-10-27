Now make bot usage for cart titan. 

# when a bot gets cart titan:

## Mode Switch:
    - Rescue: when an ally with hp less than 20% remaining
    - Offensive: If no ally with hp less than 20% remaining

## Mode Usage:
    - Rescue:
        - set predefined ai type to BotA
        - set bot beh set, 
            EliminateEnemies = false
            MeleeUsage = false
        - choose a ally to rescue among all, whos hp is less than 20%.
        - the choose algo is: The closest one (4 Points) + Is p1 or p2 (2 Points) + Is titan owner (3 Points) + is Armored titan (2 Points).
            Note that a player can have points from multiple criteria. (e.g, a player is p1 and also a Armored titan and we know armored titan is also a titan = 2 + 3 + 2 = 7 Points).
        
        - run away from enemies (read ./Examples/Bot_Avoid_Explosives.cs, learn from it, and instead of explosives, avoid enemy player) until the ally rescuing has >80% hp (you dont have to heal him, as he will be healed naturally) then release him at the spot.

     - Offensive:
        - set predefined ai type to BotA
        - set bot beh set, 
            MeleeUsage = false
        - choose a ally to rescue among all, whos hp is less than 20%.
        - He will fight like that:
            Fire bullets: 70% of times
            run away from enemies: 30% of times
            Melee Combat: 0%
        - Fire Bullet: fire the selected bullet type at the target with 80% accuracy.
        - Select Bullet Type: if the enemy is fast, choose M60, if slow, choose Sniper. but with a chance based random
            - Formula: 
                - Enemies Size modifier from 1.7f to 0.5f the chance of M60 = 5% and Sniper = 95%. scale for other sizes based on these data
            