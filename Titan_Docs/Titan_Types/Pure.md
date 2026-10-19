# Pure Titan - Complete Documentation

## Overview
The Pure Titan is the basic titan type representing raw, uncontrolled power. It features moderate stat boosts and a devastating grab damage ability that can extract powers from humanoid titans.

---

## Statistics

### Base Stats
```csharp
Health:        +15% (PURE_TITAN_HP_BONUS = 0.15f)
Melee Damage:  +10% (PURE_TITAN_MELEE_DAMAGE_BONUS = 0.10f)
Melee Force:   +25% (PURE_TITAN_MELEE_FORCE_BONUS = 0.25f)
Speed:         -30% (PURE_TITAN_SPEED_PENALTY = 0.30f)
Energy:        Base (no bonus)
Size:          Random (0.8 to 1.25)
```

### Size-Based Scaling
Since Pure Titan has random size, additional bonuses apply:
```
For every 0.01 size above 0.8:
- Health:        +0.3%
- Melee Damage:  +0.3%
- Melee Force:   +0.3%
- Speed:         -2.0%

Example: Size 1.0 (0.2 above base)
- Health:        +15% base + 6% size = +21% total
- Melee Damage:  +10% base + 6% size = +16% total
- Melee Force:   +25% base + 6% size = +31% total
- Speed:         -30% base - 40% size = -70% total
```

---

## Appearance

### Profile
```csharp
Gender: Male (random)
Skin:   "Normal", "Skin3", "ClothingPink"
Head:   "Bald"
Legs:   "Torn"
```

### Visual Characteristics
- **Color Theme**: Pink/flesh toned skin
- **Identifying Features**: Bald head, torn pants, shirtless
- **Size Variation**: Varies per player (0.8-1.25 scale)
- **No Label**: Pure Titans don't get floating type labels

---

## Abilities

### 1. Grab Damage (Passive)
**Description**: Continuously damages grabbed victims until they die.

#### Mechanics
```csharp
Base Damage:    7 (GRAB_DAMAGE_BASE)
Damage Scaling: Base × TitanMeleeDamage × 0.15 (GRAB_DAMAGE_MULTIPLIER)
Tick Rate:      Every 50ms (GRAB_DAMAGE_INTERVAL)
Blood Effect:   Every 1 second (GRAB_BLOOD_EFFECT_INTERVAL)
```

#### Damage Formula
```
Damage per tick = 7 × MeleeDamageMod × 0.15
Example: If MeleeDamageMod = 1.31 (base 1.10 + 0.21 size):
         7 × 1.31 × 0.15 = 1.38 damage per tick
         1.38 × 20 ticks/sec = 27.6 DPS
```

#### Implementation Details
```csharp
Function: CheckTitanGrabDamage(IPlayer titan, int titanId)
Trigger:  Called every OnUpdate tick (50ms)
Tracking: titanGrabDamage dictionary stores GrabDamageData
```

**Flow**:
1. Check if titan is grabbing someone (IsCaughtByPlayerInGrab)
2. Verify titan is close enough (distance < 5f)
3. Initialize GrabDamageData if new grab
4. Apply damage every 50ms
5. Play blood effect every 1000ms
6. Track victim for power extraction (gibbedByPureTitan)
7. Clean up when grab ends

#### Bot Behavior
- Bot titans have input disabled during grab (prevents premature release)
- Input restored when grab ends

---

### 2. Power Extraction (Active on Kill)
**Description**: When Pure Titan kills a humanoid titan via grab damage, it extracts their power and swaps titan types.

#### Trigger Conditions
```
1. Victim must be gibbed (health < 0)
2. Victim must be tracked in gibbedByPureTitan dictionary
3. Victim must have humanoid power (Attack/Jaw/Armored)
4. Pure Titan must still exist and be Pure type
```

#### Extraction Process

##### Standard Extraction (Normal Player)
```
1. Pure Titan detransforms (smoke effect, fall)
2. Swap powers:
   - Pure Titan → Gets victim's humanoid power
   - Victim → Gets Pure Titan's Pure power
3. Update labels ([ATK]/[JAW]/[ARM])
4. Save to persistent storage
5. Show chat message
```

##### Special Extraction (Temporary Player "com")
```
1. Pure Titan detransforms (same as standard)
2. Special power distribution:
   - Random Pure Titan teammate → Gets victim's humanoid power
   - Victim → Gets teammate's old power
   - Temporary player → Keeps Pure Titan power (unchanged)
3. Update labels
4. Save to persistent storage
5. Show special chat message
```

#### Implementation Details
```csharp
Trigger:   OnPlayerDeath(IPlayer deadPlayer)
Function:  ExtractTitanPower(pureTitan, pureTitanId, victim, victimId, extractedPower)
Tracking:  gibbedByPureTitan[victimId] = pureTitanId
```

**Flow**:
1. OnPlayerDeath checks gibbedByPureTitan dictionary
2. Verify Pure Titan still exists
3. Verify victim had humanoid power
4. Call ExtractTitanPower()
5. Check if extractor is temporary player
6. Execute appropriate swap logic
7. Clean up tracking data
8. Show result message

#### Edge Cases
- **Pure kills Pure**: No extraction (victim has no humanoid power)
- **Non-grab death**: No extraction (victim not in gibbedByPureTitan)
- **Pure Titan died**: No extraction (extractor verification fails)
- **No teammate for temp player**: Power is lost, shows error message

---

## Bot AI Behavior

### Combat
Pure Titan bots use standard combat behavior:
```csharp
Attack:       25% (unchanged from default)
AttackCombo:  25% (unchanged from default)
Block:        25% (unchanged from default)
Grab:         25% (unchanged from default)
```

### Grab Strategy
- Bots will attempt grabs at same rate as normal
- Input disabled during grab to prevent premature release
- Automatically maintains grab until victim dies or escapes

### Power Extraction
- Happens automatically when conditions met
- No special bot logic needed (triggered by death event)

---

## Exhaustion System
Pure Titans are affected by exhaustion like all titans.

### Exhaustion Trigger
```
When CurrentEnergy <= 0:
1. Force death kneel animation (DeathKneelInfinite)
2. Disable input
3. Set recovery timer (6 seconds)
```

### Recovery
```
After 6 seconds:
1. Stop death kneel (StopDeathKneel)
2. Re-enable input
3. Restore full energy
```

### Implementation
```csharp
Function: CheckTitanExhaustion(IPlayer titan, int titanId)
Called:   Every OnUpdate tick
Tracking: titanExhausted[titanId], titanExhaustionRecoverTime[titanId]
```

---

## Transformation Visuals

### Transformation Effects
```csharp
1. Explosion at player position (30f radius)
2. Camera shake (CameraShaker)
3. Steam effect at spawn location
4. Destruction sound ("BarrelDestroyed")
5. Spawn animation
```

### Fall Impact
```csharp
When titan lands:
1. Camera shake at titan position
2. All non-titans within 23f fall
3. Visual feedback
```

---

## Stat Calculation Example

### Player with Size 1.1
```
Base Multipliers:
- HP:     1.15 (15% bonus)
- Damage: 1.10 (10% bonus)  
- Force:  1.25 (25% bonus)
- Speed:  0.70 (30% penalty)

Size Bonus (0.3 above 0.8 = 30 × 0.01 increments):
- HP:     +9% → 1.15 × 1.09 = 1.2535
- Damage: +9% → 1.10 × 1.09 = 1.199
- Force:  +9% → 1.25 × 1.09 = 1.3625
- Speed:  -60% → 0.70 × 0.40 = 0.28

Final Stats (assuming base 100 HP):
- Health:        100 × 1.2535 = 125.35
- Melee Damage:  Base × 1.199
- Melee Force:   Base × 1.3625
- Speed:         Base × 0.28 (very slow!)
```

---

## Grab Damage Calculation Example

### Scenario: Pure Titan (Size 1.1) grabs victim
```
Titan Stats:
- Base Melee Damage Mod: 1.199 (from above)

Grab Damage Per Tick:
- Formula: 7 × 1.199 × 0.15 = 1.259 damage

DPS (20 ticks per second):
- 1.259 × 20 = 25.18 damage per second

Time to Kill (victim has 100 HP):
- 100 ÷ 25.18 = 3.97 seconds
```

---

## Power Extraction Scenarios

### Scenario 1: Normal Players Only
```
Players: Alice (Pure), Bob (Attack), Charlie (Jaw)
Action:  Alice grabs and gibs Bob

Result:
- Alice:  Pure → Attack Titan
- Bob:    Attack → Pure Titan (next spawn)
- Charlie: Unchanged (Jaw)

Storage Update:
"Alice:1;Bob:0;Charlie:2" (0=Pure, 1=Attack, 2=Jaw)
```

### Scenario 2: With Temporary Player
```
Players: Alice (Pure), Bob (Attack), com (Pure)
Action:  com grabs and gibs Bob

Result:
- com:    Pure → Pure (unchanged)
- Alice:  Pure → Attack Titan (random teammate)
- Bob:    Attack → Pure Titan (gets Alice's old power)

Storage Update:
"Alice:1;Bob:0" (com not stored)

Chat: "com extracted the ATTACK TITAN power and transferred it to Alice!"
```

### Scenario 3: Pure Kills Pure
```
Players: Alice (Pure), Bob (Pure)
Action:  Alice grabs and gibs Bob

Result:
- No extraction (Bob has no humanoid power)
- Powers unchanged
```

### Scenario 4: No Teammates
```
Players: com (Pure, Team 1), Bob (Attack, Team 2)
Action:  com grabs and gibs Bob

Result:
- No Pure Titan teammate found
- Power is lost
- Chat: "com extracted a power but had no teammate to transfer it to!"
```

---

## Technical Implementation

### Dictionary Entries Created
```csharp
titanPlayers[playerId] = TitanType.Pure
originalProfiles[playerId] = preTransformProfile
originalModifiers[playerId] = preTransformModifiers
titanSizes[playerId] = randomSize (0.8 to 1.25)
titanGrabDamage[playerId] = new GrabDamageData() // when grabbing
assignedTitanPowers[playerId] = TitanType.Pure
```

### Cleanup on Detransformation
```csharp
titanPlayers.Remove(playerId)
originalProfiles.Remove(playerId)
originalModifiers.Remove(playerId)
titanSizes.Remove(playerId)
titanGrabDamage.Remove(playerId)
titanInputDisabled.Remove(playerId)
titanExhausted.Remove(playerId)
titanExhaustionRecoverTime.Remove(playerId)
// assignedTitanPowers kept for next transformation
```

---

## Testing Checklist

### Basic Functionality
- [ ] Transformation works on strength boost pickup
- [ ] Stats apply correctly for various sizes
- [ ] Profile/appearance correct
- [ ] Grab damage works
- [ ] Blood effects appear during grab

### Power Extraction
- [ ] Pure → Attack extraction works
- [ ] Pure → Jaw extraction works
- [ ] Pure → Armored extraction works
- [ ] Pure → Pure does NOT extract
- [ ] Temporary player transfers to teammate
- [ ] Labels update after extraction
- [ ] Powers persist after extraction

### Edge Cases
- [ ] Victim dies mid-grab
- [ ] Pure Titan dies before extraction
- [ ] Multiple Pure Titans active
- [ ] Bot Pure Titans maintain grab
- [ ] Exhaustion recovery works

### Integration
- [ ] ODM script recognizes Pure Titans
- [ ] Daily distribution includes Pure
- [ ] Storage/restore works correctly

---

## Debugging

### Common Issues
1. **Grab damage not working**
   - Check titanPlayers contains playerId with TitanType.Pure
   - Verify victim IsCaughtByPlayerInGrab
   - Check distance < 5f

2. **Power extraction not triggering**
   - Verify victim in gibbedByPureTitan dictionary
   - Check victim has humanoid assigned power
   - Ensure Pure Titan still exists

3. **Bot input not restoring**
   - Check EnableTitanInput called on grab end
   - Verify titanInputDisabled cleanup

### Debug Messages
```csharp
Game.ShowChatMessage("OnPlayerDeath called for: " + player.Name);
Game.ShowChatMessage("Found in gibbedByPureTitan!");
Game.ShowChatMessage("Extracting power!");
```

---

## Related Systems
- [Exhaustion System](../Titan.md#exhaustion-system)
- [Grab Damage System](../Titan.md#grab-damage-system)
- [Power Distribution](../Titan.md#power-distribution-system)
- [Size-Based Scaling](../Titan.md#size-based-stat-scaling)

---

**Last Updated**: 2024-10-19
