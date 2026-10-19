# Attack Titan - Complete Documentation

## Overview
The Attack Titan is a versatile humanoid titan with three distinct hardening modes and a slowmotion ability. It excels at both offense and defense through intelligent mode switching.

---

## Statistics

### Base Stats
```csharp
Health:        +40% (ATTACK_TITAN_HP_BONUS = 0.40f)
Melee Damage:  +32% (ATTACK_TITAN_MELEE_DAMAGE_BONUS = 0.32f)
Melee Force:   +1%  (ATTACK_TITAN_MELEE_FORCE_BONUS = 0.01f)
Speed:         -20% (ATTACK_TITAN_SPEED_PENALTY = 0.20f)
Energy:        +30% (ATTACK_TITAN_ENERGY_BONUS = 0.30f)
Size:          Random (0.8 to 1.25)
```

### Size-Based Scaling
Attack Titan has random size with additional scaling:
```
For every 0.01 size above 0.8:
- Health:        +0.3%
- Melee Damage:  +0.3%
- Melee Force:   +0.3%
- Speed:         -2.0%
```

---

## Appearance

### Profile
```csharp
Gender: Male (random)
Skin:   "Normal", "Skin4", "ClothingDarkRed"
Head:   "Normal", "Skin4", "ClothingDarkRed"
Eyes:   "EyesDarkGreen"
ChestOver: "SatchelBelt", "ClothingDarkGreen"
Legs:   "Pants", "ClothingDarkRed"
Feet:   "BootsBlack"
```

### Visual Characteristics
- **Color Theme**: Dark red/green military appearance
- **Identifying Features**: Green eyes, satchel belt
- **Label**: Red `[ATK]` floating above head
- **Size Variation**: Random (0.8-1.25 scale)

---

## Abilities

### 1. Hardening Modes (Toggle with SHEATHE key)
**Description**: Three distinct combat modes that drastically change playstyle.

#### Mode: NEUTRAL (Default)
```
Stats:   Base stats (no modifiers)
Purpose: Balanced state, energy conservation
Visual:  Standard appearance
```

#### Mode: OFFENSIVE
```csharp
Activation:  Automatic when enemy within 25f (HARDENING_OFFENSIVE_TRIGGER_DISTANCE)
Duration:    Until no melee for 2s (HARDENING_OFFENSIVE_MELEE_TIMEOUT)
Energy Cost: 14% max energy per second (HARDENING_ENERGY_COST_PER_SECOND)

Bonuses:
- Melee Damage:    +25% (HARDENING_OFFENSIVE_DAMAGE_BONUS)
- Stagger Chance:  70% on hit (HARDENING_OFFENSIVE_STAGGER_CHANCE)

Profile Change:
ChestUnder: "BodyArmor", "ClothingDarkRed"
Hands:      "SafetyGlovesBlack"
```

**Stagger Mechanic**:
```csharp
On each melee hit:
- 70% chance to apply Stagger command to victim
- Implemented in OnPlayerMeleeAction
- Works on all hit players
```

#### Mode: DEFENSIVE
```csharp
Activation:  Automatic when enemy aiming at titan (raycast detection)
Duration:    While being aimed at (checked every 2s)
Energy Cost: 14% max energy per second

Bonuses:
- Damage Reduction:  75% (HARDENING_DEFENSIVE_DEFENSE_BONUS)
- Speed Penalty:     -20% additional (HARDENING_DEFENSIVE_SPEED_PENALTY)

Profile Change:
ChestUnder: "BodyArmor", "ClothingDarkRed"
ChestOver:  "KevlarVest", "ClothingDarkGreen"
Hands:      "SafetyGlovesBlack"
```

**Damage Reduction**:
```csharp
Modifiers.ProjectileDamageTakenModifier = 0.25f  // 75% reduction
Modifiers.MeleeDamageTakenModifier = 0.25f       // 75% reduction
```

### Hardening Implementation

#### Energy Drain
```csharp
Function: CheckAttackTitanHardeningEnergy(player, playerId, currentTime)
Called:   Every OnUpdate tick
Logic:    Drain 14% per second when in Offensive/Defensive mode

Calculation:
energyDrain = maxEnergy × 0.14 × (elapsed / 1000)
```

#### Mode Switching (Players)
```csharp
Key:      VirtualKey.SHEATHE
Action:   Cycle through Neutral → Offensive → Defensive → Neutral
Function: OnPlayerKeyInput → ApplyAttackTitanHardening()
```

#### Bot AI Triggers

##### Offensive Mode
```csharp
Function: CheckAttackTitanBotHardening()
Trigger:  Bot target within 25f
Check:    Every 2 seconds (HARDENING_DEFENSIVE_CHECK_INTERVAL)

Additional Logic:
- If energy < 30%: 80% chance to skip activation
- If already offensive: 60% chance to return to neutral when low energy
- Update last melee time for timeout tracking
```

##### Defensive Mode
```csharp
Trigger: Enemy player aiming at bot (raycast detection)
Check:   Every 2 seconds
Range:   800f (AIM_DETECTION_RANGE)

Aim Detection:
- Raycast from enemy in facing direction
- Check if bot is hit within range
- Activate defensive if detected
```

##### Offensive Timeout
```csharp
Condition: No melee action for 2 seconds
Action:    Switch back to Neutral mode
Tracking:  attackTitanLastMeleeTime dictionary
```

---

### 2. Slowmotion Ability (AIM key)
**Description**: Activates game slowmotion for 1 second, then ends automatically.

#### Mechanics
```csharp
Key:         VirtualKey.AIM (hold)
Energy Cost: 30% of max energy (SLOWMO_ENERGY_COST)
Duration:    1 second (SLOWMO_ACTIVATION_DELAY)
Cooldown:    Energy dependent (must have 30% available)
```

#### Implementation
```csharp
Activation (OnPlayerKeyInput):
1. Check if AIM key pressed
2. Verify energy >= 30% max
3. Deduct 30% energy
4. Set Game.SlowmotionModifier = 0.5f (slow)
5. Set attackTitanSlowmoEndTime = currentTime + 1000ms

Deactivation (CheckAttackTitanSlowmo):
1. Called every OnUpdate tick
2. Check if currentTime >= endTime
3. Set Game.SlowmotionModifier = 1.0f (normal)
4. Clear attackTitanSlowmoEndTime
```

#### Usage Strategy
- **Offense**: Slow time during combo attacks
- **Defense**: Dodge projectiles, react to threats
- **Positioning**: Line up perfect attacks
- **Escape**: Create space when overwhelmed

---

## Bot AI Behavior

### Combat
```csharp
Attack:       25% (standard)
AttackCombo:  25% (standard)
Block:        25% (standard)
Grab:         25% (standard)
```

### Hardening Strategy

#### Offensive Triggers
```
1. Target within 25f
2. Energy > 30%
3. No low energy panic (80% chance check)
```

#### Defensive Triggers
```
1. Enemy aiming at bot (raycast detection)
2. No close target (prioritize offense over defense)
3. Energy sufficient
```

#### Energy Management
```
- Monitor energy percentage
- Switch to neutral when energy < 30% (60% chance)
- Avoid hardening when energy < 30% (80% chance)
```

### Slowmo Usage
Bots do NOT use slowmo ability (player exclusive).

---

## Exhaustion System
Attack Titans are affected by exhaustion like all titans.

### Exhaustion Trigger
```
When CurrentEnergy <= 0:
1. Force death kneel (DeathKneelInfinite)
2. Disable input
3. Set recovery timer (6 seconds)
4. Hardening automatically deactivates
```

### Recovery
```
After 6 seconds:
1. Stop death kneel
2. Re-enable input
3. Restore full energy
4. Hardening can be reactivated
```

---

## Stat Calculation Examples

### Example 1: Size 1.0, Neutral Mode
```
Base Multipliers:
- HP:     1.40 (40% bonus)
- Damage: 1.32 (32% bonus)
- Force:  1.01 (1% bonus)
- Speed:  0.80 (20% penalty)
- Energy: 1.30 (30% bonus)

Size Bonus (0.2 above 0.8 = 20 increments):
- HP:     +6% → 1.40 × 1.06 = 1.484
- Damage: +6% → 1.32 × 1.06 = 1.3992
- Force:  +6% → 1.01 × 1.06 = 1.0706
- Speed:  -40% → 0.80 × 0.60 = 0.48

Final Stats (base 100 HP, 100 energy):
- Health:  148.4 HP
- Energy:  130 energy
- Damage:  Base × 1.3992
- Force:   Base × 1.0706
- Speed:   Base × 0.48
```

### Example 2: Size 1.0, Offensive Mode
```
Additional Modifiers:
- Damage: × 1.25 (25% bonus)

Final Damage:
- 1.3992 × 1.25 = 1.749
```

### Example 3: Size 1.0, Defensive Mode
```
Additional Modifiers:
- Speed:        × 0.80 (20% penalty)
- Damage Taken: × 0.25 (75% reduction)

Final Stats:
- Speed: 0.48 × 0.80 = 0.384 (very slow)
- Effective HP: 148.4 ÷ 0.25 = 593.6 (taking 75% less damage)
```

---

## Energy Calculations

### Example: Max Energy with Size 1.0
```
Base Energy: 100 (standard player)
Bonus:       +30%
Final:       130 energy

Hardening Cost:
- Per second: 130 × 0.14 = 18.2 energy/sec
- Duration:   130 ÷ 18.2 = 7.14 seconds max

Slowmo Cost:
- Per use: 130 × 0.30 = 39 energy
- Uses:    3 uses from full energy (before exhaustion)
```

---

## Combat Scenarios

### Scenario 1: Close Range Brawl
```
1. Enemy approaches (< 25f)
2. Bot activates Offensive hardening
3. Engage with melee combo
4. 70% chance per hit to stagger enemy
5. Continue until enemy dead or out of energy
6. If energy < 30%: Return to Neutral
```

### Scenario 2: Long Range Threat
```
1. Enemy sniper aims at bot
2. Bot detects aim (raycast)
3. Bot activates Defensive hardening
4. Take 75% reduced damage
5. Move slower but safely approach
6. Once close: Switch to Offensive
```

### Scenario 3: Player Combo
```
1. Player holds SHEATHE → Activate Offensive
2. Close distance to enemy
3. Land melee hits (70% stagger chance)
4. Tap AIM → Activate slowmo
5. Land perfect combo in slow time
6. Enemy dies before slowmo ends
```

### Scenario 4: Emergency Escape
```
1. Health low, surrounded
2. Press AIM → Slowmo active
3. Navigate through enemies in slow time
4. Reach safe distance
5. Hold SHEATHE → Defensive mode
6. Retreat with damage reduction
```

---

## Technical Implementation

### Dictionary Entries Created
```csharp
titanPlayers[playerId] = TitanType.Attack
assignedTitanPowers[playerId] = TitanType.Attack
originalProfiles[playerId] = preTransformProfile
originalModifiers[playerId] = preTransformModifiers
titanSizes[playerId] = randomSize

// Attack-specific
attackTitanHardeningMode[playerId] = HardeningMode.Neutral
attackTitanHardeningEnergyCost[playerId] = energyDrainPerSecond
attackTitanLastMeleeTime[playerId] = lastMeleeTimestamp
attackTitanLastDefensiveCheck[playerId] = lastCheckTimestamp
attackTitanSlowmoEndTime[playerId] = slowmoEndTimestamp
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

// Attack-specific
attackTitanHardeningMode.Remove(playerId)
attackTitanHardeningEnergyCost.Remove(playerId)
attackTitanLastMeleeTime.Remove(playerId)
attackTitanLastDefensiveCheck.Remove(playerId)
attackTitanSlowmoEndTime.Remove(playerId)
```

---

## Hardening Mode State Machine

### State Diagram
```
       SHEATHE key
  ┌───────────────────────┐
  │                       │
  ▼                       │
NEUTRAL ◄────────────────┐│
  │ SHEATHE or Auto      ││
  │                      ││
  ▼                      ││
OFFENSIVE ───────────────┤│ SHEATHE or Timeout
  │ SHEATHE              ││ (2s no melee)
  │                      ││
  ▼                      ││
DEFENSIVE ───────────────┘│
           SHEATHE         │
           or Aim lost     │
                           │
           └───────────────┘
```

### Auto-Activation Priority
```
1. Check Offensive trigger (target close)
2. If not Offensive, check Defensive trigger (being aimed at)
3. If neither, stay in current mode or return to Neutral
```

---

## Raycast Aim Detection

### How It Works
```csharp
Function: GetAimedPlayer(IPlayer aimer)

1. Get aimer position and facing direction
2. Calculate endpoint: position + (facing × 800f)
3. Perform raycast from position to endpoint
4. Check if any player is hit
5. Return hit player or null

Usage:
- Defensive mode activation for bots
- Checks all enemies to see if any are aiming at bot
```

---

## Testing Checklist

### Basic Functionality
- [ ] Transformation works
- [ ] Stats apply correctly
- [ ] Label appears ([ATK] in red)
- [ ] Size variation works

### Hardening
- [ ] SHEATHE key cycles modes (player)
- [ ] Offensive activates on target close (bot)
- [ ] Defensive activates on being aimed at (bot)
- [ ] Offensive grants damage bonus
- [ ] Offensive staggers 70% of hits
- [ ] Defensive reduces damage 75%
- [ ] Defensive slows movement
- [ ] Energy drains correctly
- [ ] Profile changes per mode
- [ ] Timeout returns to neutral (2s no melee)

### Slowmo
- [ ] AIM key activates slowmo (player)
- [ ] Costs 30% energy
- [ ] Lasts 1 second
- [ ] Auto-deactivates after 1s
- [ ] Cannot activate without energy

### Energy Management
- [ ] Low energy prevents hardening (bots)
- [ ] Low energy switches to neutral (bots)
- [ ] Exhaustion at 0 energy
- [ ] Hardening drains continuously

### Integration
- [ ] Power extraction works
- [ ] Daily distribution includes Attack
- [ ] ODM recognizes Attack Titan

---

## Debugging

### Common Issues
1. **Hardening not activating**
   - Check attackTitanHardeningMode dictionary
   - Verify energy sufficient
   - Check trigger conditions (distance/aim)

2. **Stagger not working**
   - Verify Offensive mode active
   - Check random roll (70% chance)
   - Ensure victim is player

3. **Slowmo not ending**
   - Check attackTitanSlowmoEndTime
   - Verify CheckAttackTitanSlowmo called
   - Check SlowmotionModifier reset

4. **Bot not using defensive**
   - Verify raycast working
   - Check enemy is aiming
   - Ensure no close target (offense priority)

### Debug Commands
```csharp
// Add to CheckAttackTitanBotHardening
Game.ShowChatMessage("Mode: " + mode + " Energy: " + energyPercent);

// Add to GetAimedPlayer
Game.ShowChatMessage("Aimed at: " + (result?.Name ?? "none"));
```

---

## Related Systems
- [Exhaustion System](../Titan.md#exhaustion-system)
- [Power Distribution](../Titan.md#power-distribution-system)
- [Size-Based Scaling](../Titan.md#size-based-stat-scaling)

---

**Last Updated**: 2024-10-19
