# Armored Titan - Complete Documentation

## Overview
The Armored Titan is the ultimate tank, featuring defensive armor that blocks and reflects attacks, a devastating rush ability, and a super punch combo. Trades speed for durability and crowd control.

---

## Statistics

### Base Stats
```csharp
Health:        +32% (ARMORED_TITAN_HP_BONUS = 0.32f)
Melee Damage:  +25% (ARMORED_TITAN_MELEE_DAMAGE_BONUS = 0.25f)
Melee Force:   +50% (ARMORED_TITAN_MELEE_FORCE_BONUS = 0.50f)
Speed:         -50% (ARMORED_TITAN_SPEED_PENALTY = 0.50f) - SLOWEST TITAN
Energy:        +15% (ARMORED_TITAN_ENERGY_BONUS = 0.15f)
Size:          1.300 (ARMORED_TITAN_SIZE) - FIXED, LARGEST TITAN
```

### Size-Based Scaling
Armored Titan has FIXED size (1.3), so size scaling is predetermined:
```
Size 1.3 is 0.5 above 0.8 (50 increments):
- Health:        +15%  (50 × 0.3%)
- Melee Damage:  +15%  (50 × 0.3%)
- Melee Force:   +15%  (50 × 0.3%)
- Speed:         -100% (50 × 2%) - CAPPED at minimum

Final Multipliers:
- Health:        1.518  (base 1.32 × 1.15)
- Melee Damage:  1.4375 (base 1.25 × 1.15)
- Melee Force:   1.725  (base 1.50 × 1.15)
- Speed:         0      (50% base penalty + 100% size = total 150%, capped at ~0.1 minimum)
- Energy:        1.15   (no size scaling)
```

---

## Appearance

### Armor ON (Default)
```csharp
Gender: Male (random)
Skin:   "Normal", "Skin5", "ClothingLightGreen"
Head:   "Buzzcut", "ClothingLightGray"
ChestUnder: "BodyArmor", "ClothingLightGray"
Hands:  "Gloves", "ClothingLightGray"
Legs:   "Pants", "ClothingLightGray"
Feet:   "Boots", "ClothingLightGray"
Accesory: "ClownMakeup", "ClothingLightGray"
```

### Armor OFF
```csharp
Same as above but WITHOUT:
- ChestUnder: (removed, shows shirtless)
```

### Visual Characteristics
- **Color Theme**: Gray armor plating
- **Identifying Features**: Largest size (1.3), heavily armored
- **Label**: Blue `[ARM]` floating above head
- **Size**: Fixed large size (1.3), consistent across all players

---

## Abilities

### 1. Armor Toggle (SHEATHE key)
**Description**: Switch between defensive armor and mobility mode.

#### Armor ON (Default)
```csharp
Bonuses:
- Front Immunity:    Melee attacks from front
- Front Immunity:    Projectile attacks from front
- Front Immunity:    Thrown/explosion damage from front
- Reflection:        Bullets reflected back at attacker

Visual:
- BodyArmor visible
- Sparks effect on front hits
- Block effect on reflections

Profile: Full armor (ChestUnder + all items)
```

**Immunity System**:
```csharp
Implementation: 3-layer protection

Layer 1: Predictive Damage Reduction (OnUpdate)
- Check for front melee threats (distance < 30f, attacking)
- Check for front projectile threats (lookahead 0.06s)
- Set MeleeDamageTakenModifier = 0 (immune)
- Set ProjectileDamageTakenModifier = 0 (immune)

Layer 2: Projectile Reflection (OnProjectileHit)
- Check if hit from front
- Mirror reflect projectile (flip X direction)
- Spawn new projectile with same damage
- Remove original
- Play Sparks + Block effects

Layer 3: Safety Net (OnPlayerDamage)
- Catch any damage that slipped through
- Check if source is from front
- Refund damage (restore health)
- Play Sparks effect
- Handles melee, projectile, and explosion types
```

**Front Detection**:
```csharp
Function: IsArmoredInFront(player, sourcePosition)

Logic:
dx = sourcePosition.X - player.X
return Sign(dx) == player.FacingDirection

Example:
- Player at X=100, facing RIGHT (+1)
- Source at X=120
- dx = +20, Sign = +1
- Result: TRUE (source is in front)
```

#### Armor OFF
```csharp
Bonuses:
- Speed:          +30% (ARMORED_ARMOR_OFF_SPEED_BONUS)
- Energy Regen:   +30% (ARMORED_ARMOR_OFF_ENERGY_REGEN_BONUS)

Penalties:
- No front immunity
- No reflection
- Take full damage
- Recoil from rush (50%)

Profile: No ChestUnder (shirtless for mobility)
```

**Speed Comparison**:
```
Armor ON:  0.1× speed (extremely slow, nearly immobile)
Armor OFF: 0.13× speed (still slow, but 30% faster)
```

#### Toggle Implementation
```csharp
Key:      VirtualKey.SHEATHE
Function: ToggleArmoredTitanArmor → ApplyArmoredTitanArmor
Tracking: armoredTitanArmorOn[playerId]
```

---

### 2. Rush Ability (Sprint)
**Description**: While sprinting, throw enemies and objects in path.

#### Mechanics
```csharp
Trigger:       player.IsSprinting
Base Damage:   7 (ARMORED_RUSH_DAMAGE_BASE)
Recoil:        50% of damage dealt (ARMORED_RUSH_RECOIL_RATIO)
Cooldown:      800ms per target (ARMORED_RUSH_HIT_COOLDOWN)
```

#### Throw Power Formula
```
throwPower = (TitanForce × TitanHeight) - (TargetForce × TargetHeight × 1.4)

Example:
Armored Titan: Force 1.725, Height 1.3
  → TitanPower = 1.725 × 1.3 = 2.2425

Target: Force 1.0, Height 1.0
  → TargetPower = 1.0 × 1.0 × 1.4 = 1.4

Throw Power = 2.2425 - 1.4 = 0.8425
Velocity = 0.8425 × 300 = 252.75
Direction: Horizontal + 60% upward
```

#### Throw Velocity
```csharp
velocity = throwPower × 300 (ARMORED_RUSH_THROW_SCALE)
direction = new Vector2(facing × velocity, velocity × 0.6)

Example:
- throwPower = 0.8425
- velocity = 252.75
- X velocity = +252.75 (or -252.75 if facing left)
- Y velocity = +151.65 (60% of 252.75, always upward)
```

#### Damage Calculation
```csharp
damage = 7 × TitanMeleeDamageMod

Example:
7 × 1.4375 = 10.0625 damage per hit

Recoil (if armor OFF):
10.0625 × 0.5 = 5.03 damage to titan
```

#### Target Restrictions
```csharp
Valid Targets:
- Enemy players
- Enemy team players
- Independent players (if titan not independent)

Skip:
- Self
- Teammates (same team, non-independent)
- Dead players
- Players hit recently (800ms cooldown)
```

#### Object Throwing
```csharp
Valid Objects:
- Destructible objects (obj.Destructable == true)
- Within collision area (GetObjectsByArea)

Throw Power:
- Half of player throw power (× 0.5)
- Same direction (horizontal + upward)
```

#### Player Fall Effect
```csharp
On hit:
- QuickCommand(target, Fall) - force fall animation
- SetLinearVelocity() - apply throw velocity
- Both happen simultaneously
```

---

### 3. Super Punch (3rd Punch Combo)
**Description**: Every 3rd punch within 600ms launches enemy with massive force.

#### Mechanics
```csharp
Punch Count:  3 (ARMORED_SUPER_PUNCH_COUNT)
Window:       600ms (ARMORED_SUPER_PUNCH_WINDOW)
```

#### Throw Power Formula
```
Same as Rush ability:
throwPower = (TitanForce × TitanHeight) - (TargetForce × TargetHeight)

Note: No 1.4× multiplier (unlike rush)

Example:
Armored: 1.725 × 1.3 = 2.2425
Target:  1.0 × 1.0 = 1.0
Power:   2.2425 - 1.0 = 1.2425
Velocity: 1.2425 × 300 = 372.75
```

**Why Different from Rush?**
- Rush: More power because continuous sprinting
- Punch: Single hit, so no target weight multiplier

#### Combo System
```
Punch 1 (time 0):
- Initialize count = 1
- Record armoredTitanLastPunchTime

Punch 2 (time < 600ms):
- Increment count = 2
- Update armoredTitanLastPunchTime

Punch 3 (time < 600ms):
- Increment count = 3
- SUPER PUNCH all hit players
- Reset count = 0
- Update armoredTitanLastPunchTime

Punch 4 (time > 600ms):
- Window expired
- Reset count = 1
- Restart combo
```

#### Super Punch Effects
```csharp
On all hit players:
1. QuickCommand(victim, Fall)
2. SetLinearVelocity(throw velocity)
3. CameraShaker effect
4. DestroyMetal sound
```

#### Implementation
```csharp
Function: HandleArmoredTitanMelee(player, playerId, args, currentTime)
Called:   OnPlayerMeleeAction for Armored Titans
Tracking:
- armoredTitanPunchCount[playerId]
- armoredTitanLastPunchTime[playerId]
```

---

## Bot AI Behavior

### Combat
```csharp
Attack:       23% (ARMORED_TITAN_BOT_MELEE_ATTACK)
AttackCombo:  23% (ARMORED_TITAN_BOT_MELEE_ATTACK_COMBO)
Block:        23% (ARMORED_TITAN_BOT_MELEE_BLOCK)
Grab:         23% (ARMORED_TITAN_BOT_MELEE_GRAB)
Wait:         8%  (reduced for aggression)
```

### Armor Toggle Strategy

#### Turn Armor ON
```csharp
Triggers:
1. Enemy within 35f (ARMORED_ARMOR_ON_TRIGGER_DISTANCE)
2. Enemy aiming at bot (raycast detection, 800f range)

Check Interval: Every 1 second
```

#### Turn Armor OFF
```
Trigger: No enemies close AND no one aiming
Purpose: Mobility when safe
```

**Logic Priority**:
```
1. Check if target is close → Armor ON
2. If not, check if being aimed at → Armor ON
3. If neither → Armor OFF
```

### Rush Strategy

#### Activation Conditions
```csharp
Requirements:
1. Energy > 80% (ARMORED_BOT_RUSH_ENERGY_THRESHOLD)
2. Multiple enemies nearby (ARMORED_BOT_RUSH_MIN_ENEMIES = 2)
3. Enemies within 40f (ARMORED_BOT_RUSH_CHECK_DISTANCE)
4. Enemies in facing direction
```

#### Abort Conditions
```csharp
Once rushing:
- Abort if energy < 20% (ARMORED_BOT_RUSH_ABORT_ENERGY)
- Otherwise continue rush
```

**Bot Rush Behavior**:
```
1. Check conditions (80% energy, 2+ enemies within 40f)
2. Set armoredTitanIsRushing = true
3. Bot naturally sprints toward targets
4. CheckArmoredTitanRush handles damage/throws
5. Abort if energy < 20%
```

---

## Exhaustion System
Armored Titans are affected by exhaustion like all titans.

### Exhaustion Trigger
```
When CurrentEnergy <= 0:
1. Force death kneel (DeathKneelInfinite)
2. Disable input
3. Set recovery timer (6 seconds)
4. Armor state maintained (no change)
```

### Recovery
```
After 6 seconds:
1. Stop death kneel
2. Re-enable input
3. Restore full energy
4. Can resume rush/armor toggle
```

---

## Stat Calculation Example

### Armored Titan (Size 1.3)
```
Base Stats:
- HP:     1.32  (32% bonus)
- Damage: 1.25  (25% bonus)
- Force:  1.50  (50% bonus)
- Speed:  0.50  (50% penalty)
- Energy: 1.15  (15% bonus)

Size Bonuses (0.5 above 0.8):
- HP:     +15% → 1.32 × 1.15 = 1.518
- Damage: +15% → 1.25 × 1.15 = 1.4375
- Force:  +15% → 1.50 × 1.15 = 1.725
- Speed:  -100% → 0.50 × 0  ≈ 0.1 (capped minimum)

Armor OFF Speed:
- Base: 0.1
- Bonus: × 1.30
- Final: 0.13 (still very slow)

Final Stats (base 100 HP, 100 energy):
- Health:  151.8 HP (highest of all titans!)
- Energy:  115 energy
- Damage:  Base × 1.4375
- Force:   Base × 1.725 (highest!)
- Speed:   Base × 0.1 (slowest!)
```

---

## Damage Calculation Examples

### Example 1: Rush Hit
```
Armored Stats: Damage 1.4375, Force 1.725, Height 1.3
Target Stats:  Damage 1.0, Force 1.0, Height 1.0

Throw Power:
(1.725 × 1.3) - (1.0 × 1.0 × 1.4) = 2.2425 - 1.4 = 0.8425

Velocity:
0.8425 × 300 = 252.75

Direction:
X: +252.75 (forward)
Y: +151.65 (upward 60%)

Damage:
7 × 1.4375 = 10.0625

Recoil (armor OFF):
10.0625 × 0.5 = 5.03
```

### Example 2: Super Punch
```
Throw Power (no 1.4× multiplier):
(1.725 × 1.3) - (1.0 × 1.0) = 2.2425 - 1.0 = 1.2425

Velocity:
1.2425 × 300 = 372.75

Direction:
X: +372.75 (forward)
Y: +223.65 (upward 60%)

Note: 48% stronger than rush (372.75 vs 252.75)
```

### Example 3: Rush vs Large Target
```
Target Stats: Force 1.5, Height 1.2
Target Power: 1.5 × 1.2 × 1.4 = 2.52

Throw Power:
2.2425 - 2.52 = -0.2775

Capped at 0 (cannot throw)
Still deals damage: 10.0625
```

---

## Combat Scenarios

### Scenario 1: Defensive Hold
```
Setup: Armored in narrow corridor, armor ON

1. Enemies approach from front
2. All attacks blocked/reflected
3. Bullets bounce back at attackers
4. Tank position indefinitely
5. Team pushes from behind/sides
```

### Scenario 2: Rush Crowd Control
```
Setup: 3 enemies clustered, 80%+ energy

1. Bot detects 2+ enemies within 40f
2. Activate rush (sprint)
3. Hit all 3 enemies
4. All thrown in same direction
5. Deal 10 damage each + displace
6. Total: 30 damage + repositioning
```

### Scenario 3: Super Punch Combo
```
Setup: 1v1 combat

1. Land punch 1
2. Within 600ms, land punch 2
3. Within 600ms, land punch 3 (SUPER)
4. Enemy launched 372 units
5. Falls during flight
6. Large repositioning advantage
```

### Scenario 4: Armor Toggle Tactics
```
Safe Phase:
- Armor OFF for mobility
- Energy regenerates 30% faster
- Move between positions

Combat Phase:
- Enemy appears
- Toggle armor ON (instant)
- Tank damage
- Reflect bullets
```

---

## Technical Implementation

### Dictionary Entries Created
```csharp
titanPlayers[playerId] = TitanType.Armored
assignedTitanPowers[playerId] = TitanType.Armored
originalProfiles[playerId] = preTransformProfile
originalModifiers[playerId] = preTransformModifiers
titanSizes[playerId] = 1.300 (fixed)

// Armored-specific
armoredTitanArmorOn[playerId] = true (default)
armoredTitanLastArmorCheck[playerId] = lastCheckTime
armoredTitanRushHitTimes[playerId] = Dictionary<targetId, lastHitTime>
armoredTitanIsRushing[playerId] = false
armoredTitanPunchCount[playerId] = currentCombo
armoredTitanLastPunchTime[playerId] = lastPunchTime
```

### Cleanup
```csharp
// Generic
titanPlayers.Remove(playerId)
originalProfiles.Remove(playerId)
originalModifiers.Remove(playerId)
titanSizes.Remove(playerId)
titanExhausted.Remove(playerId)
titanExhaustionRecoverTime.Remove(playerId)

// Armored-specific
armoredTitanArmorOn.Remove(playerId)
armoredTitanLastArmorCheck.Remove(playerId)
armoredTitanRushHitTimes.Remove(playerId)
armoredTitanIsRushing.Remove(playerId)
armoredTitanPunchCount.Remove(playerId)
armoredTitanLastPunchTime.Remove(playerId)
```

---

## Armor State Machine

### States
```
ARMOR_ON (default)
  │ SHEATHE toggle
  │ OR safe conditions
  ▼
ARMOR_OFF
  │ SHEATHE toggle
  │ OR threat detected
  ▼
ARMOR_ON
```

### Bot Decision Tree
```
Check Armor State:
  ├─ Target close (< 35f)?
  │  └─ YES → ARMOR_ON
  │  └─ NO → Check next
  ├─ Being aimed at?
  │  └─ YES → ARMOR_ON
  │  └─ NO → Check next
  └─ No threats?
     └─ YES → ARMOR_OFF
```

---

## Testing Checklist

### Basic Functionality
- [ ] Transformation works
- [ ] Stats apply correctly (highest HP)
- [ ] Label appears ([ARM] in blue)
- [ ] Fixed size (1.3) applies
- [ ] Slowest movement confirmed

### Armor System
- [ ] SHEATHE toggles armor
- [ ] Armor ON: ChestUnder visible
- [ ] Armor OFF: ChestUnder removed
- [ ] Front melee blocked
- [ ] Front projectiles blocked
- [ ] Front explosions blocked
- [ ] Bullets reflected correctly
- [ ] Sparks effect on hits
- [ ] Back hits still damage
- [ ] Speed +30% when armor OFF
- [ ] Energy regen +30% when armor OFF

### Rush Ability
- [ ] Sprint triggers rush
- [ ] Enemies thrown correctly
- [ ] Objects thrown correctly
- [ ] Teammates NOT thrown
- [ ] Thrown players fall mid-air
- [ ] Damage dealt correctly
- [ ] Recoil when armor OFF
- [ ] No recoil when armor ON
- [ ] 800ms cooldown per target
- [ ] Bot activates at 80% energy
- [ ] Bot aborts at 20% energy

### Super Punch
- [ ] Punch counter tracks correctly
- [ ] 600ms window enforced
- [ ] 3rd punch throws enemy
- [ ] Throw power calculated correctly
- [ ] Falls during throw
- [ ] Effects/sounds play
- [ ] Counter resets after 3rd

### Bot AI
- [ ] Armor ON when target close
- [ ] Armor ON when aimed at
- [ ] Armor OFF when safe
- [ ] Rush activates correctly
- [ ] Rush aborts on low energy

### Integration
- [ ] Power extraction works
- [ ] Daily distribution includes Armored
- [ ] ODM recognizes Armored Titan

---

## Debugging

### Common Issues
1. **Armor not blocking**
   - Check armoredTitanArmorOn dictionary
   - Verify IsArmoredInFront() calculation
   - Test with stationary attacker

2. **Reflection not working**
   - Check OnProjectileHit callback
   - Verify projectile spawning
   - Check reflection direction (flip X)

3. **Rush not throwing**
   - Verify IsSprinting state
   - Check throw power calculation
   - Ensure targets not on cooldown

4. **Super punch combo broken**
   - Check armoredTitanLastPunchTime
   - Verify 600ms window
   - Add debug for count

5. **Bot not rushing**
   - Check energy level (need 80%)
   - Verify enemy count (need 2+)
   - Check distance (40f max)

### Debug Commands
```csharp
// Add to CheckArmoredTitanArmor
Game.ShowChatMessage("Armor: " + armoredTitanArmorOn[playerId]);

// Add to CheckArmoredTitanRush
Game.ShowChatMessage("Rush hit: " + target.Name + " Power: " + power);

// Add to HandleArmoredTitanMelee
Game.ShowChatMessage("Super Punch combo: " + currentPunchCount);
```

---

## Related Systems
- [Exhaustion System](../Titan.md#exhaustion-system)
- [Power Distribution](../Titan.md#power-distribution-system)
- [Size-Based Scaling](../Titan.md#size-based-stat-scaling)

---

**Last Updated**: 2024-10-19
