## Now lets add "Armored Titan"

- Stats:
Health +32%
Melee damage +25%
Force +50%
Speed -50%
Energy +15%
Size = 1.300f

- Bot Melee actions:
Attack = 23%
AttackCombo = 23%
Block = 23%
Grab = 23%
Wait = 8%


- Profile:
ply.SetProfile(
    new IProfile()
    {
        Gender = Gender.Male,
        Skin = new IProfileClothingItem("Normal", "Skin5", "ClothingLightGreen"),
        Head = new IProfileClothingItem("Buzzcut", "ClothingLightGray"),
        ChestUnder = new IProfileClothingItem("BodyArmor", "ClothingLightGray"),
        Hands = new IProfileClothingItem("Gloves", "ClothingLightGray"),
        Legs = new IProfileClothingItem("Pants", "ClothingLightGray"),
        Feet = new IProfileClothingItem("Boots", "ClothingLightGray"),
        Accesory = new IProfileClothingItem("ClownMakeup", "ClothingLightGray"),
    }
);

- Abilities:
    - Armor: on virtual key SHEATHE toggle, switch the titan to armor on/off forms:
        - form details:
            - On (Default): 
                - melee hits from forward are immune
                - projectile hits from forward are immune and bullets are mirror reflected
                - play Spike effect on forward hit
                - set profile: new IProfile()
                    {
                        Gender = Gender.Male,
                        Skin = new IProfileClothingItem("Normal", "Skin5", "ClothingLightGreen"),
                        Head = new IProfileClothingItem("Buzzcut", "ClothingLightGray"),
                        ChestUnder = new IProfileClothingItem("BodyArmor", "ClothingLightGray"),
                        Hands = new IProfileClothingItem("Gloves", "ClothingLightGray"),
                        Legs = new IProfileClothingItem("Pants", "ClothingLightGray"),
                        Feet = new IProfileClothingItem("Boots", "ClothingLightGray"),
                        Accesory = new IProfileClothingItem("ClownMakeup", "ClothingLightGray"),
                    }
                - psudo code for this functionality:
                    ```cs
                    // ---------- CONFIG ----------
                    const float MELEE_RANGE        = 30f;    // how close an attacker must be to count as a melee threat
                    const float MELEE_Y_TOLERANCE  = 24f;
                    const float PROJ_LOOKAHEAD_SEC = 0.06f;  // look-ahead for fast bullets
                    const float REFLECT_OFFSET     = 6f;     // spawn reflected bullet this far from the shield player

                    IPlayer shieldPlayer;                    // the special player
                    bool    guardActive = false;

                    OnStartup():
                        shieldPlayer = Game.GetPlayers()[0];            // or however you pick the special player
                        Events.UpdateCallback.Start(OnUpdate, 0);       // every tick
                        Events.ProjectileHitCallback.Start(OnProjectileHit);
                        Events.PlayerDamageCallback.Start(OnPlayerDamage);   // safety net only


                    // ---------- HELPERS ----------
                    bool IsInFront(Vector2 point):
                        // player faces -1 (left) or +1 (right)
                        dx = point.X - shieldPlayer.GetWorldPosition().X
                        return Sign(dx) == shieldPlayer.FacingDirection

                    bool FrontMeleeThreat():
                        foreach enemy in Game.GetPlayers():
                            if enemy == shieldPlayer or enemy.IsDead: continue
                            if !enemy.IsMeleeAttacking: continue            // check IsKicking too if you want kicks
                            d = enemy.GetWorldPosition() - shieldPlayer.GetWorldPosition()
                            if Abs(d.X) <= MELEE_RANGE and Abs(d.Y) <= MELEE_Y_TOLERANCE and IsInFront(enemy.GetWorldPosition()):
                                return true
                        return false

                    bool FrontProjectileThreat():
                        foreach p in Game.GetProjectiles():
                            if p.IsRemoved: continue
                            if p.InitialOwnerPlayerID == shieldPlayer.UniqueID: continue   // ignore own bullets
                            next = p.Position + p.Velocity * PROJ_LOOKAHEAD_SEC
                            movingTowardPlayer = (Sign(p.Velocity.X) == -shieldPlayer.FacingDirection)
                            if movingTowardPlayer
                            and IsInFront(p.Position)
                            and next is inside / past shieldPlayer.GetAABB() on X:
                                return true
                        return false


                    // ---------- 1) PREDICTIVE ZERO DAMAGE ----------
                    OnUpdate(elapsed):
                        if shieldPlayer.IsDead: return

                        meleeThreat = FrontMeleeThreat()
                        projThreat  = FrontProjectileThreat()

                        mods = shieldPlayer.GetModifiers()        // keep the other modifiers intact
                        mods.MeleeDamageTakenModifier      = meleeThreat ? 0 : -2   // -2 = reset to default
                        mods.ProjectileDamageTakenModifier = projThreat  ? 0 : -2
                        shieldPlayer.SetModifiers(mods)           // only call if values changed (avoid spam)


                    // ---------- 2) REFLECT PROJECTILES ----------
                    OnProjectileHit(projectile, args):
                        if !args.IsPlayer or args.HitObjectID != shieldPlayer.UniqueID: return
                        if !IsInFront(args.HitPosition): return              // hits from behind hurt normally

                        // The shield is a vertical plane, so the normal is horizontal.
                        // Mirror reflection r = d - 2(d.n)n  simplifies to flipping X:
                        dir = projectile.Direction
                        reflected = new Vector2(-dir.X, dir.Y)
                        // Optional: use args.HitNormal instead for a general mirror:
                        //   n = args.HitNormal;  reflected = dir - 2 * Dot(dir, n) * n
                        // Optional: add a small random angle for a "fan scatter" feel

                        spawnPos = args.HitPosition + reflected * REFLECT_OFFSET   // avoid instantly re-hitting the shield

                        newProj = Game.SpawnProjectile(projectile.ProjectileItem, spawnPos, reflected)
                        if newProj != null:
                            newProj.DamageDealtModifier = projectile.DamageDealtModifier
                            // copy powerups (fire/bounce) here if you care

                        projectile.FlagForRemoval()                  // make sure the original is gone
                        Game.PlayEffect(EffectName.Block, args.HitPosition)    // or a spark/bullet-hit effect


                    // ---------- 3) SAFETY NET (damage that slipped through) ----------
                    OnPlayerDamage(player, args):
                        if player != shieldPlayer or args.OverkillDamage: return

                        if args.DamageType == PlayerDamageEventType.Projectile:
                            proj = Game.GetProjectile(args.SourceID)
                            srcPos = proj != null ? proj.Position : null
                        else if args.DamageType == PlayerDamageEventType.Melee:
                            attacker = Game.GetPlayer(args.SourceID)
                            srcPos = attacker != null ? attacker.GetWorldPosition() : null
                        else:
                            return                                    // explosions, fire, etc. are not blocked

                        if srcPos != null and IsInFront(srcPos):
                            shieldPlayer.SetHealth(shieldPlayer.GetHealth() + args.Damage)   // refund
                    ```
                    Note: follow current code standard instead of this psudo code
            - Off:
                - speed +30%
                - energy recharge mod +30%
                - set profile: new IProfile()
                    {
                        Gender = Gender.Male,
                        Skin = new IProfileClothingItem("Normal", "Skin5", "ClothingLightGreen"),
                        Head = new IProfileClothingItem("Buzzcut", "ClothingLightGray"),
                        Hands = new IProfileClothingItem("Gloves", "ClothingLightGray"),
                        Legs = new IProfileClothingItem("Pants", "ClothingLightGray"),
                        Feet = new IProfileClothingItem("Boots", "ClothingLightGray"),
                        Accesory = new IProfileClothingItem("ClownMakeup", "ClothingLightGray"),
                    }

        - Bot Usage: unlike real players, bots cant use this ability wisely, so here are triggers
            - on: 
                - target is closer 35f (like attack titans hardening offensive trigger)
                - enemy aim (like attack titans hardening defensive trigger)
            - off: when no need to on

    - Rush: Sprint:
        - when he sprints, enemy players overlaped will get a throw.  
        - the throw power will be based on: Armored Titans (Force * Height) - Target (Force * Height) * 1.4
        - victims will also get a damage of 7 * Armored Titans melee damage mod and the armored titan will get a 50% recoil if he not turned on armor
        - psudo code:
            ```cs
            // ---------- CONFIG ----------
            const float CHARGE_DAMAGE_BASE   = 7f;
            const float RECOIL_RATIO         = 0.5f;   // 50% of dealt damage comes back to the titan
            const float TARGET_FORCE_WEIGHT  = 1.4f;   // the "* 1.4" from your formula
            const float THROW_SCALE          = 300f;   // converts power -> velocity (TUNE IN-GAME)
            const float THROW_UP_RATIO       = 0.6f;   // upward share of the throw
            const float HIT_COOLDOWN_MS      = 800f;   // per-target, so one overlap isn't hit every tick

            IPlayer titan;                              // the special player
            bool    armorOn = false;                    // armor toggle state
            float   now = 0;                            // running clock from OnUpdate
            Dictionary<int, float> lastHitTime;         // targetUniqueID -> time of last charge hit

            OnStartup():
                titan = Game.GetPlayers()[0];
                Events.UpdateCallback.Start(OnUpdate, 0);
                Events.PlayerKeyInputCallback.Start(OnKeyInput);


            // ---------- ARMOR TOGGLE ----------
            OnKeyInput(player, keyEvents):
                if player != titan: return
                foreach k in keyEvents:
                    if k.Key == <YOUR_ARMOR_KEY> and k.Event == VirtualKeyEvent.Pressed:
                        armorOn = !armorOn


            // ---------- HELPERS ----------
            float GetForce(IPlayer p):
                f = p.GetModifiers().MeleeForceModifier       // range 0-10
                return (f < 0) ? 1 : f                        // -1/-2 = unset -> treat as default 1

            float GetHeight(IPlayer p):
                s = p.GetModifiers().SizeModifier             // range 0.5-2.0
                return (s < 0) ? 1 : s
                // alternative: p.GetAABB().Height (raw world units, then re-tune THROW_SCALE)


            // ---------- SPRINT CHARGE ----------
            OnUpdate(elapsed):
                now += elapsed
                if titan.IsDead or !titan.IsSprinting: return

                titanArea = titan.GetAABB()
                dir       = titan.FacingDirection             // -1 or +1

                foreach target in Game.GetPlayers():
                    if target == titan or target.IsDead: continue
                    // optional: if target.GetTeam() == titan.GetTeam(): continue
                    if !titanArea.Intersects(target.GetAABB()): continue
                    // optional: skip targets behind the titan:
                    //   if Sign(target.GetWorldPosition().X - titan.GetWorldPosition().X) != dir: continue
                    if lastHitTime.Contains(target.UniqueID) and now - lastHitTime[target.UniqueID] < HIT_COOLDOWN_MS: continue

                    lastHitTime[target.UniqueID] = now

                    // --- throw power ---
                    titanPower  = GetForce(titan)  * GetHeight(titan)
                    targetPower = GetForce(target) * GetHeight(target)
                    power = titanPower - targetPower * TARGET_FORCE_WEIGHT   // literal reading of your formula
                    power = Max(power, 0)                                    // never pull the target backwards

                    if power > 0:
                        v = power * THROW_SCALE
                        target.SetLinearVelocity(new Vector2(dir * v, v * THROW_UP_RATIO))

                    // --- damage ---
                    dmg = CHARGE_DAMAGE_BASE * titan.GetModifiers().MeleeDamageDealtModifier   // treat <0 as 1 here too
                    target.DealDamage(dmg, titan.UniqueID)

                    // --- recoil (only if armor is OFF) ---
                    if !armorOn:
                        titan.DealDamage(dmg * RECOIL_RATIO)
            ```

        - Bot Usage: unlike real players, bots cant use this ability wisely, 
            - so use this:
                - run move to position and sprint command using QuickCommand() helper
            - triggers:
                - current energy > 80% and was in a combat
                - multiple enemies on the same direction on max 40f distance
            - dont rush or abort if was in a rush, if current energy falls then 20%
