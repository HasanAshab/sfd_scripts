# Jaw Titan - Complete Documentation

## Overview
The Jaw Titan is the fastest and most agile humanoid titan, specializing in assassination and hit-and-run tactics. Features devastating claw attacks with backstab multipliers, dive hunt ability, and object-breaking sharp claws.

---

## Statistics

### Base Stats
```csharp
Health:        +0%  (JAW_TITAN_HP_BONUS = 0.0f)
Melee Damage:  +25% (JAW_TITAN_MELEE_DAMAGE_BONUS = 0.25f)
Melee Force:   +0%  (JAW_TITAN_MELEE_FORCE_BONUS = 0.0f)
Speed:         -0%  (JAW_TITAN_SPEED_PENALTY = 0.0f) - FASTEST TITAN
Energy:        +25% (JAW_TITAN_ENERGY_BONUS = 0.25f)
Size:          0.880 (JAW_TITAN_SIZE) - FIXED, SMALLEST TITAN
```

### Size-Based Scaling
Jaw Titan has FIXED size (0.88), so size scaling is predetermined:
```
Size 0.880 is 0.08 above 0.8 (8 increments):
- Health:        +2.4% (8 × 0.3%)
- Melee Damage:  +2.4% (8 × 0.3%)
- Melee Force:   +2.4% (8 × 0.3%)
- Speed:         -16%  (8 × 2%)

Final Multipliers:
- Health:        1.024  (base 1.0 + 2.4% size)
- Melee Damage:  1.28   (base 1.25 + 2.4% size)
- Melee Force:   1.024  (base 1.0 + 2.4% size)
- Speed:         0.84   (base 1.0 - 16% size)
- Energy:        1.25   (base 1.25, no size scaling)
```

---

## Appearance

### Profile
```csharp
Gender: Male (random)
Skin:   "Normal", "Skin4", "ClothingYellow"
Head:   "Normal", "Skin4", "ClothingYellow"
Legs:   "Pants", "ClothingYellow"
```

### Visual Characteristics
- **Color Theme**: Yellow/blonde appearance
- **Identifying Features**: Small size (0.88), shirtless, blonde
- **Label**: Green `[JAW]` floating above head
- **Size**: Fixed small size (0.88), consistent across all players

---

## Abilities

### 1. Claw Attack (Passive on Jump Attacks)
**Description**: Aerial melee attacks deal massive damage with backstab multipliers.

#### Mechanics
```csharp
Trigger:      Jump attack (IsInMidAir during melee)
Base Damage:  7 (JAW_CLAW_BASE_DAMAGE)
Multipliers:
  - Front:    ×2 (JAW_CLAW_FRONT_MULTIPLIER)
  - Backstab: ×4 (JAW_CLAW_BACK_MULTIPLIER)
```

#### Damage Formula
```
Total Damage = Base × TitanMeleeMod × DirectionMultiplier

Front Attack:
7 × 1.28 × 2 = 17.92 damage

Backstab:
7 × 1.28 × 4 = 35.84 damage
```

#### Backstab Detection
```csharp
Logic: Compare attacker and victim facing directions

Backstab Condition:
attackerFacing == victimFacing

Example:
- Attacker facing RIGHT (+1)
- Victim facing RIGHT (+1)
- Result: BACKSTAB (both facing same direction)

This means attacker is BEHIND victim
```

**Why This Works**:
- If both face same direction → attacker is behind
- If facing opposite directions → attacker is in front

#### Gib Mechanic
```csharp
Condition: Overkill damage > 30% of victim max HP
Threshold: MaxHP × 0.30 (JAW_CLAW_GIB_THRESHOLD)

Example:
- Victim MaxHP: 100
- Threshold: 30
- Backstab deals 35.84 damage
- If victim has < 35.84 HP: Instant gib

Gib Effect: victim.Gib() - full body destruction
```

#### Sound Effects
```csharp
Front Attack:  "MacheteDraw"  (slash sound)
Backstab:      "KatanaDraw"   (sharp slice sound)
```

#### Implementation
```csharp
Function: HandleJawTitanClawAttack(jawTitan, jawTitanId, victim)
Called:   From HandleJawTitanMelee when isJumpAttack = true
Tracking: playerFacingDirections for backstab detection
```

---

### 2. Hunt Ability (Passive on Dive Catch)
**Description**: When Jaw Titan catches enemy in dive, continuously damages them until dive ends.

#### Mechanics
```csharp
Trigger:      victim.CaughtByPlayerInDiveID == jawTitanId
Damage:       Same as grab damage system
  - Base:     7 (GRAB_DAMAGE_BASE)
  - Scaling:  × MeleeMod × 0.15 (GRAB_DAMAGE_MULTIPLIER)
  - Tick:     Every 50ms
Blood:        Every 1 second
Stun:         1 second after hunt ends (JAW_HUNT_VICTIM_STUN_DURATION)
```

#### Hunt Flow
```
1. Jaw Titan performs dive (special move)
2. Catches enemy player mid-dive
3. CheckJawTitanHuntDamage detects catch
4. Initialize jawTitanHuntVictim tracking
5. Deal damage every 50ms while caught
6. Play blood effect every 1 second
7. On hunt end (victim released):
   - Disable victim input
   - Set jawHuntVictimReenableTime = currentTime + 1000ms
8. After 1 second: Re-enable victim input
```

#### Damage Calculation
```
Damage per tick = 7 × 1.28 × 0.15 = 1.344
DPS = 1.344 × 20 = 26.88 damage per second

Time to kill 100 HP victim:
100 ÷ 26.88 = 3.72 seconds
```

#### Stun Mechanic
```csharp
Purpose: Victim is temporarily defenseless after hunt
Duration: 1 second (1000ms)
Implementation:
- victim.SetInputEnabled(false) on hunt end
- Track in jawHuntVictimReenableTime
- victim.SetInputEnabled(true) after 1s
```

---

### 3. Sharp Claw (Combo System)
**Description**: Every 3rd punch within 600ms window breaks objects.

#### Mechanics
```csharp
Punch Count:  3 (JAW_SHARP_CLAW_PUNCH_COUNT)
Window:       600ms (JAW_SHARP_CLAW_PUNCH_WINDOW)
Effect:       Instant object destruction
```

#### Combo System
```
Punch 1 (time 0):
- Initialize count = 1
- Record jawTitanLastPunchTime

Punch 2 (time < 600ms):
- Increment count = 2
- Update jawTitanLastPunchTime

Punch 3 (time < 600ms):
- Increment count = 3
- BREAK OBJECTS IN HIT
- Reset count = 0
- Update jawTitanLastPunchTime

Punch 4 (time > 600ms from Punch 3):
- Window expired
- Reset count = 1
- Restart combo
```

#### Object Breaking
```csharp
Condition: Hit object in 3rd punch
Effect:    hitObject.Destroy()
Visual:    EffectName.DestroyMetal + sound
```

**Valid Objects**:
- Destructible objects (crates, barrels, etc.)
- Includes dynamic objects
- Works on any object that can be damaged

**Does NOT Break**:
- Players (uses claw damage instead)
- Indestructible objects

#### Implementation
```csharp
Function: HandleJawTitanMelee(player, playerId, args, currentTime)
Called:   OnPlayerMeleeAction for Jaw Titans
Tracking:
- jawTitanPunchCount[playerId]
- jawTitanLastPunchTime[playerId]
```

---

### 4. Grab Damage (Passive)
**Description**: Same as Pure Titan - damages grabbed victims continuously.

#### Mechanics
```csharp
Shared with Pure Titan:
- Base Damage: 7
- Tick Rate:   Every 50ms
- Scaling:     × MeleeMod × 0.15
- Blood:       Every 1 second

DPS: 7 × 1.28 × 0.15 × 20 = 26.88
```

**Note**: Unlike Pure Titan, Jaw Titan does NOT extract powers on kill.

---

## Bot AI Behavior

### Combat
```csharp
Attack:       25% (standard)
AttackCombo:  25% (standard)
Block:        25% (standard)
Grab:         25% (standard)
```

### Claw Usage
- Bots will naturally perform jump attacks
- Automatic claw damage on jump melee
- No special bot logic needed

### Hunt Usage
- Bots will attempt dives naturally
- Damage automatically applied when catching
- No manual activation required

### Sharp Claw Usage
- Bots punch objects naturally
- Combo counter tracks automatically
- 3rd punch breaks objects

---

## Exhaustion System
Jaw Titans are affected by exhaustion like all titans.

### Exhaustion Trigger
```
When CurrentEnergy <= 0:
1. Force death kneel (DeathKneelInfinite)
2. Disable input
3. Set recovery timer (6 seconds)
```

### Recovery
```
After 6 seconds:
1. Stop death kneel
2. Re-enable input
3. Restore full energy
```

---

## Combat Strategies

### Assassin Playstyle
```
1. Use speed advantage to maneuver
2. Position behind enemy
3. Jump attack for backstab
4. Instant kill or massive damage
5. Retreat with speed if needed
```

### Hunt Strategy
```
1. Dive toward enemy
2. Catch mid-dive
3. Deal continuous damage
4. Enemy stunned after hunt
5. Follow up with finishing attack
```

### Object Destruction
```
1. Punch object twice
2. 3rd punch breaks it
3. Create openings in walls
4. Destroy cover
5. Control map
```

### Combo Example
```
Turn 1: Punch × 2 on wall
Turn 2: 3rd punch breaks wall
Turn 3: Enemy exposed behind wall
Turn 4: Jump attack backstab
Turn 5: Enemy eliminated
```

---

## Stat Calculation Example

### Jaw Titan (Size 0.88)
```
Base Stats:
- HP:     1.00 (no bonus)
- Damage: 1.25 (25% bonus)
- Force:  1.00 (no bonus)
- Speed:  1.00 (no penalty)
- Energy: 1.25 (25% bonus)

Size Bonuses (0.08 above 0.8):
- HP:     +2.4% → 1.00 × 1.024 = 1.024
- Damage: +2.4% → 1.25 × 1.024 = 1.280
- Force:  +2.4% → 1.00 × 1.024 = 1.024
- Speed:  -16%  → 1.00 × 0.84  = 0.84

Final Stats (base 100 HP, 100 energy):
- Health:  102.4 HP (slightly above base)
- Energy:  125 energy
- Damage:  Base × 1.280
- Force:   Base × 1.024
- Speed:   Base × 0.84 (FAST!)
```

---

## Damage Calculation Examples

### Example 1: Front Jump Attack
```
Victim HP: 100

Damage Calculation:
7 (base) × 1.28 (melee mod) × 2 (front multiplier) = 17.92

Result:
- Victim takes 17.92 damage
- Victim HP: 82.08
- "MacheteDraw" sound plays
```

### Example 2: Backstab Jump Attack
```
Victim HP: 100

Damage Calculation:
7 × 1.28 × 4 (backstab multiplier) = 35.84

Result:
- Victim takes 35.84 damage
- Victim HP: 64.16
- "KatanaDraw" sound plays
```

### Example 3: Backstab with Gib
```
Victim HP: 30

Damage Calculation:
7 × 1.28 × 4 = 35.84

Overkill Check:
- Damage: 35.84
- Health: 30
- Overkill: 5.84
- Threshold: 100 × 0.30 = 30
- Result: 5.84 < 30, NO GIB

If victim had 5 HP:
- Damage: 35.84
- Health: 5
- Overkill: 30.84
- Threshold: 30
- Result: 30.84 > 30, GIB!
```

### Example 4: Hunt DPS
```
Hunt duration: 4 seconds

Tick Damage: 7 × 1.28 × 0.15 = 1.344
Ticks per second: 20
DPS: 1.344 × 20 = 26.88

Total Damage: 26.88 × 4 = 107.52

Result: Victim dies (if had 100 HP)
```

---

## Technical Implementation

### Dictionary Entries Created
```csharp
titanPlayers[playerId] = TitanType.Jaw
assignedTitanPowers[playerId] = TitanType.Jaw
originalProfiles[playerId] = preTransformProfile
originalModifiers[playerId] = preTransformModifiers
titanSizes[playerId] = 0.880 (fixed)

// Jaw-specific
jawTitanPunchCount[playerId] = currentComboCount
jawTitanLastPunchTime[playerId] = lastPunchTimestamp
jawTitanHuntVictim[playerId] = currentHuntVictim
jawTitanHuntDamageTime[playerId] = nextDamageTimestamp
jawTitanHuntBloodEffectTime[playerId] = nextBloodTimestamp
jawHuntVictimReenableTime[playerId] = reenableTimestamp

// Shared
titanGrabDamage[playerId] = grabDamageData (for grab ability)
playerFacingDirections[playerId] = facingDirection
```

### Cleanup
```csharp
// Generic
titanPlayers.Remove(playerId)
originalProfiles.Remove(playerId)
originalModifiers.Remove(playerId)
titanSizes.Remove(playerId)
titanGrabDamage.Remove(playerId)
titanExhausted.Remove(playerId)
titanExhaustionRecoverTime.Remove(playerId)

// Jaw-specific
jawTitanPunchCount.Remove(playerId)
jawTitanLastPunchTime.Remove(playerId)
jawTitanHuntVictim.Remove(playerId)
jawTitanHuntDamageTime.Remove(playerId)
jawTitanHuntBloodEffectTime.Remove(playerId)
jawHuntVictimReenableTime.Remove(playerId)
```

---

## Combo State Machine

### Sharp Claw States
```
IDLE (count = 0)
  │ Punch
  ▼
PUNCH_1 (count = 1)
  │ Punch within 600ms
  ▼
PUNCH_2 (count = 2)
  │ Punch within 600ms
  ▼
PUNCH_3 (count = 3)
  │ BREAK OBJECTS
  │ Reset
  ▼
IDLE (count = 0)

Timeout anywhere:
  Wait > 600ms → Reset to IDLE
```

---

## Testing Checklist

### Basic Functionality
- [ ] Transformation works
- [ ] Stats apply correctly (especially speed)
- [ ] Label appears ([JAW] in green)
- [ ] Fixed size (0.88) applies

### Claw Attack
- [ ] Jump attacks deal 2x damage (front)
- [ ] Jump attacks deal 4x damage (backstab)
- [ ] Backstab detection works correctly
- [ ] Gib occurs on 30%+ overkill
- [ ] Sounds play correctly (Machete vs Katana)

### Hunt Ability
- [ ] Dive catch deals continuous damage
- [ ] Blood effects appear
- [ ] Victim stunned 1s after hunt
- [ ] Input re-enabled after stun
- [ ] Damage stops when hunt ends

### Sharp Claw
- [ ] Punch counter tracks correctly
- [ ] 600ms window enforced
- [ ] 3rd punch breaks objects
- [ ] Counter resets after 3rd punch
- [ ] Timeout resets counter

### Grab Damage
- [ ] Grab deals continuous damage
- [ ] Blood effects appear
- [ ] Does NOT extract power (unlike Pure)

### Integration
- [ ] Power extraction works (when killed by Pure)
- [ ] Daily distribution includes Jaw
- [ ] ODM recognizes Jaw Titan

---

## Debugging

### Common Issues
1. **Backstab not detecting**
   - Verify playerFacingDirections updated
   - Check timing (face direction before hit)
   - Test with stationary targets

2. **Combo not resetting**
   - Check jawTitanLastPunchTime
   - Verify 600ms window calculation
   - Add debug message for count

3. **Hunt damage not working**
   - Verify CaughtByPlayerInDiveID
   - Check jawTitanHuntVictim tracking
   - Ensure dive is active

4. **Gib not triggering**
   - Check overkill calculation
   - Verify 30% threshold
   - Test with low HP victims

### Debug Commands
```csharp
// Add to HandleJawTitanMelee
Game.ShowChatMessage("Combo: " + currentPunchCount);

// Add to HandleJawTitanClawAttack
Game.ShowChatMessage("Backstab: " + isBackstab + " Dmg: " + totalDamage);

// Add to CheckJawTitanHuntDamage
Game.ShowChatMessage("Hunt victim: " + victim.Name);
```

---

## Advanced Tactics

### Backstab Setup
```
1. Approach from side/back
2. Jump over enemy
3. Attack while airborne and behind
4. Land killing blow
```

### Hunt Chain
```
1. Dive catch enemy
2. Deal hunt damage
3. Enemy stunned on release
4. Jump attack (enemy can't dodge)
5. Likely backstab (still facing away)
```

### Speed Advantage
```
Jaw Titan speed: 0.84
Pure Titan speed: 0.48 (size 1.0)
Advantage: 75% faster!

Use for:
- Hit and run
- Kiting
- Repositioning
- Escape
```

---

## Related Systems
- [Exhaustion System](../Titan.md#exhaustion-system)
- [Grab Damage System](../Titan.md#grab-damage-system)
- [Power Distribution](../Titan.md#power-distribution-system)
- [Facing Direction Tracking](../Titan.md#facing-direction-tracking)

---

**Last Updated**: 2024-10-19
