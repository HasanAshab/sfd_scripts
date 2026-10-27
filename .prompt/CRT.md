## now lets add "Cart Titan":

- Stats:
Health -40%
Melee damage +0%
Force +0%
Speed +30%
Energy +200%
Size = 0.920f



- Abilities:
    - Modes: on virtual key SHEATHE toggle, death kneel command the titan and play ShotgunReload sound and then switch the titan to these forms:
        - form details:
            - Rescue (Default): 
                - Look/skin:
                    - profile: new IProfile()
                        {
                            Gender = Gender.Female,
                            Skin = new IProfileClothingItem("Normal_fem", "Skin4", "ClothingLightPink"),
                            Accesory = new IProfileClothingItem("SunGlasses", "", "ClothingLightRed"),
                        }
                    - show BgFence01G object in his back (to look like a big backpack bag)
                - When Dives on ally:
                    - lift the ally on titans backback
                    - ally melee, projectile, explosive and impact damage taken will reduced by 50%
                    - only 1 ally lift at a time and per second cut 0.5% of max energy of the titan as lift cost
                    - on DRAW_SPECIAL virtual key press, release the ally and damage taken mod will be as before
            
            - Offensive: 
                - Look/skin:
                    - profile: new IProfile()
                        {
                            Gender = Gender.Female,
                            Skin = new IProfileClothingItem("Normal_fem", "Skin4", "ClothingLightPink"),
                            Head = new IProfileClothingItem("WeldingHelmet", "ClothingLightGray"),
                            Accesory = new IProfileClothingItem("SunGlasses", "", "ClothingLightRed"),
                        }
                    - add a drone of same team in his back attached (only moved with the cart titan).
                - stats: speed -40%, Health +40%
                - Uphead Gun:
                    - create a player on same team with transparent skin profile:
                        - new IProfile()
                            {
                                Gender = Gender.Female,
                                Skin = new IProfileClothingItem("__CANVAS__", ""),
                            }
                    - the player will attached to titans up head, so that the gun that player hold looks like a attached gun of the cart titan.
                    - hide player name and status bar
                    - the player will have infinite ammo
                    - give him M60 by default, and draw it using command
                    - on virtual key press:
                        - DRAW_RIFLE: switch the players gun by sniper
                        - DRAW_HANDGUN: switch the players gun by M60 again
                    - the player will have a predefined AI type of BotB if the cart titan is a bot otherwise set none and the player will aim at forward
                    - when ATTACK is pressed keep firing the gun of uphead player until released and cut energy 1.5% if M60 or 6% if sniper, per bullet.
                    - when block is pressed the titan will go readonly input mode and using up (JUMP) and down (CROUCH_ROLL_DIVE) virtual key, the titan can adjust the aim of uphead players aim, when released, set input mode enabled again and uphead bots aim will remain there same
                    - always show a '+' text object at the aim of uphead players aim


    - on death animation: create a crate of same team and place it on titan back
    
                    