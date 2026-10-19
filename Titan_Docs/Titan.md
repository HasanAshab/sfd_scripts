# Titan Transformation System - Complete Documentation

## Overview
The Titan Transformation System is a comprehensive game modification that allows players to transform into powerful Titans with unique abilities. When players pick up a strength boost item, they undergo a dramatic transformation with enhanced combat capabilities.

## Table of Contents
- [Core Concepts](#core-concepts)
- [System Architecture](#system-architecture)
- [Power Distribution System](#power-distribution-system)
- [Transformation Mechanics](#transformation-mechanics)
- [Titan Types](#titan-types)
- [Shared Systems](#shared-systems)
- [Constants Reference](#constants-reference)
- [API Reference](#api-reference)

---

## Core Concepts

### Titan Types
The system includes 4 distinct titan types:
1. **Pure Titan** - Raw power with grab damage ability
2. **Attack Titan** - Balanced fighter with hardening modes and slowmo
3. **Jaw Titan** - Agile assassin with claw and hunt abilities
4. **Armored Titan** - Tank with armor modes, rush, and super punch

### Transformation Trigger
- Players transform by picking up **Strength Boost** powerup
- Transformation is permanent for the match duration
- Each player is assigned a titan type at match start (persists daily)

### Power Distribution Philosophy
- **Daily Reset**: Titan powers reset every 24 hours
- **Player Group Based**: Each unique combination of players gets a unique distribution
- **Persistent Storage**: Uses SharedStorage to remember assignments
- **Temporary Players**: Players named "com" are excluded from storage

---

## System Architecture

### File Structure
```
Titan.cs (main implementation)
├── Constants (lines 1-140)
├── Data Structures (lines 141-250)
├── Initialization (lines 251-400)
├── Power Distribution (lines 401-700)
├── Transformation Logic (lines 701-1200)
├── Titan-Specific Updates (lines 1201-2900)
└── Helper Functions (lines 2901-end)
```

### Key Data Structures

#### Player Tracking Dictionaries
```csharp
titanPlayers                  // int -> TitanType: Currently transformed titans
assignedTitanPowers          // int -> TitanType: Assigned powers (not transformed yet)
originalProfiles             // int -> IProfile: Pre-transformation appearance
originalModifiers            // int -> PlayerModifiers: Pre-transformation stats
titanSizes                   // int -> float: Titan size for stat calculations
```

#### Titan-Specific Tracking
```csharp
// Attack Titan
attackTitanHardeningMode     // int -> HardeningMode: Current hardening state
attackTitanSlowmoEndTime     // int -> float: When slowmo ends

// Jaw Titan
jawTitanPunchCount           // int -> int: Punch counter for sharp claw
jawTitanHuntVictim           // int -> IPlayer: Current hunt victim

// Armored Titan
armoredTitanArmorOn          // int -> bool: Armor state (on/off)
armoredTitanPunchCount       // int -> int: Punch counter for super punch
armoredTitanRushHitTimes     // int -> Dictionary<int, float>: Rush cooldown tracking
```

---

## Power Distribution System

### Daily Distribution Flow
```
1. OnStartup() → Schedule InitializeTitanDistribution (500ms delay)
2. InitializeTitanDistribution()
   ├── Get today's date (YYYY-MM-DD)
   ├── Calculate player group key (sorted names joined with '_')
   ├── Check if distribution exists for this group + date
   ├── IF NEW DAY: DistributeTitanPowers()
   └── IF SAME DAY: RestoreAssignedPowersFromStorage()
3. CreateTitanTypeLabels() - Show [ATK]/[JAW]/[ARM] above players
```

### Player Group Key Generation
```csharp
// Example: Players "Alice", "Bob", "Charlie" → Key: "Alice_Bob_Charlie"
// Excludes temporary players (name == "com")
// Sorted alphabetically for consistency
```

### Distribution Algorithms

#### Scenario 1: More Players than Humanoid Titans (>3 players)
```
1. 50% chance each player gets Pure Titan
2. 50% chance each player gets Attack/Jaw/Armored (random)
3. Ensure at least one of each humanoid type exists
```

#### Scenario 2: Players ≤ Humanoid Titans (≤3 players)
```
1. Half get humanoid titans (evenly distributed)
2. Half get Pure Titan
3. Shuffle for randomness
```

### Storage Format
```
SharedStorage Key: "TitanPowerStorage"
├── "DATE_<playerGroupKey>" → "2024-10-19"
└── "POWERS_<playerGroupKey>" → "Alice:1;Bob:2;Charlie:0"
                                 (0=Pure, 1=Attack, 2=Jaw, 3=Armored)
```

---

## Transformation Mechanics

### Transformation Sequence
```
1. Detect strength boost pickup (OnUpdate)
2. Trigger TransformToTitan()
   ├── Explosion effect + camera shake
   ├── Spawn titan at player position
   ├── Apply titan-specific profile
   ├── Calculate size (0.8 to 1.25, with fixed sizes for special titans)
   ├── Apply stat modifiers based on size + type
   ├── Store original profile/modifiers for detransformation
   ├── Fall impact (shake + knock nearby players down)
   └── Update shared storage for ODM script sync
```

### Size-Based Stat Scaling
```csharp
// For every 0.01 size above 0.8:
Health += 0.3%
Melee Damage += 0.3%
Melee Force += 0.3%
Speed -= 2%

// Example: Size 1.0 (0.2 above 0.8 = 20 increments)
Health: +6%
Damage: +6%
Force: +6%
Speed: -40%
```

### Detransformation
Only Pure Titans can detransform (via power extraction):
1. Smoke/steam effect
2. Restore original profile/modifiers/bot behavior
3. Force fall (weakness effect)
4. Clean up all tracking data

---

## Titan Types

See detailed documentation in `Titan_Types/` folder:
- [Pure Titan](Titan_Types/Pure.md) - Grab damage, power extraction
- [Attack Titan](Titan_Types/Attack.md) - Hardening modes, slowmo
- [Jaw Titan](Titan_Types/Jaw.md) - Claw attacks, hunt ability
- [Armored Titan](Titan_Types/Armored.md) - Armor modes, rush, super punch

---

## Shared Systems

### Exhaustion System
**Applies to**: All titans

When energy reaches 0:
1. Force death kneel animation
2. Disable player input
3. Set recovery timer (6 seconds)
4. After 6s: Restore full energy + re-enable input

```csharp
CheckTitanExhaustion(player, playerId)
// Called in OnUpdate for all titan types
```

### Grab Damage System
**Applies to**: Pure Titan, Jaw Titan

When titan grabs a victim:
1. Deal damage every 50ms (7 base × melee modifier × 0.15)
2. Play blood effect every 1 second
3. Track victim for power extraction (Pure only)
4. Disable bot input during grab

```csharp
CheckTitanGrabDamage(titan, titanId)
// Verifies titan type before applying
```

### Fall Detection System
**Applies to**: All titans

Triggers camera shake + knockdown effect when titan falls:
1. Detect transition from air to ground
2. Play CameraShaker effect at titan position
3. Make nearby non-titans fall (23 unit radius)

### Facing Direction Tracking
**Purpose**: Jaw Titan backstab detection

Continuously updates `playerFacingDirections` dictionary:
- Updated every OnUpdate tick
- Used to determine if claw attack is from behind
- Prevents instant turn-around exploitation

---

## Constants Reference

### Global Constants
```csharp
TEMPORARY_PLAYER_NAME = "com"           // Excluded from storage
TRANSFORM_EXPLOSION_RADIUS = 30f        // Transformation explosion range
FALL_IMPACT_RADIUS = 23f                // Fall knockdown range
TITAN_MIN_SIZE = 0.8f                   // Minimum titan size
TITAN_MAX_SIZE = 1.25f                  // Maximum titan size
TITAN_EXHAUSTION_DURATION = 6000f       // Exhaustion recovery time (ms)
GRAB_DAMAGE_BASE = 7f                   // Base grab damage
GRAB_DAMAGE_INTERVAL = 50f              // Damage tick rate (ms)
GRAB_DAMAGE_MULTIPLIER = 0.15f          // Damage scaling
SIZE_STAT_BONUS_PER_0_01 = 0.003f      // 0.3% per 0.01 size
SIZE_SPEED_PENALTY_PER_0_01 = 0.02f    // 2% per 0.01 size
```

### Pure Titan Constants
```csharp
PURE_TITAN_HP_BONUS = 0.15f             // +15% HP
PURE_TITAN_MELEE_DAMAGE_BONUS = 0.10f   // +10% damage
PURE_TITAN_MELEE_FORCE_BONUS = 0.25f    // +25% force
PURE_TITAN_SPEED_PENALTY = 0.30f        // -30% speed
```

### Attack Titan Constants
```csharp
ATTACK_TITAN_HP_BONUS = 0.40f           // +40% HP
ATTACK_TITAN_MELEE_DAMAGE_BONUS = 0.32f // +32% damage
ATTACK_TITAN_MELEE_FORCE_BONUS = 0.01f  // +1% force
ATTACK_TITAN_SPEED_PENALTY = 0.20f      // -20% speed
ATTACK_TITAN_ENERGY_BONUS = 0.30f       // +30% energy

// Hardening
HARDENING_ENERGY_COST_PER_SECOND = 0.14f        // 14% energy/sec
HARDENING_OFFENSIVE_DAMAGE_BONUS = 0.25f        // +25% damage
HARDENING_OFFENSIVE_STAGGER_CHANCE = 0.70f      // 70% stagger
HARDENING_DEFENSIVE_SPEED_PENALTY = 0.20f       // -20% speed
HARDENING_DEFENSIVE_DEFENSE_BONUS = 0.75f       // 75% damage reduction

// Slowmo
SLOWMO_ENERGY_COST = 0.30f              // 30% energy cost
SLOWMO_ACTIVATION_DELAY = 1000f         // 1 second duration
```

### Jaw Titan Constants
```csharp
JAW_TITAN_HP_BONUS = 0.0f               // 0% HP
JAW_TITAN_MELEE_DAMAGE_BONUS = 0.25f    // +25% damage
JAW_TITAN_SPEED_PENALTY = 0.0f          // 0% speed penalty
JAW_TITAN_ENERGY_BONUS = 0.25f          // +25% energy
JAW_TITAN_SIZE = 0.880f                 // Fixed size

// Claw
JAW_CLAW_BASE_DAMAGE = 7f               // Base claw damage
JAW_CLAW_FRONT_MULTIPLIER = 2f          // 2x front attack
JAW_CLAW_BACK_MULTIPLIER = 4f           // 4x backstab
JAW_CLAW_GIB_THRESHOLD = 0.30f          // 30% overkill = gib

// Sharp Claw
JAW_SHARP_CLAW_PUNCH_COUNT = 3          // 3rd punch breaks objects
JAW_SHARP_CLAW_PUNCH_WINDOW = 600       // 600ms combo window

// Hunt
JAW_HUNT_VICTIM_STUN_DURATION = 1000f   // 1 second stun after hunt
```

### Armored Titan Constants
```csharp
ARMORED_TITAN_HP_BONUS = 0.32f          // +32% HP
ARMORED_TITAN_MELEE_DAMAGE_BONUS = 0.25f // +25% damage
ARMORED_TITAN_MELEE_FORCE_BONUS = 0.50f  // +50% force
ARMORED_TITAN_SPEED_PENALTY = 0.50f      // -50% speed
ARMORED_TITAN_ENERGY_BONUS = 0.15f       // +15% energy
ARMORED_TITAN_SIZE = 1.300f              // Fixed size

// Armor Off Bonuses
ARMORED_ARMOR_OFF_SPEED_BONUS = 0.30f    // +30% speed
ARMORED_ARMOR_OFF_ENERGY_REGEN_BONUS = 0.30f // +30% energy regen

// Rush
ARMORED_RUSH_DAMAGE_BASE = 7f            // Base rush damage
ARMORED_RUSH_RECOIL_RATIO = 0.5f         // 50% recoil
ARMORED_RUSH_TARGET_FORCE_WEIGHT = 1.4f  // Target force multiplier
ARMORED_RUSH_THROW_SCALE = 300f          // Velocity scaling
ARMORED_RUSH_THROW_UP_RATIO = 0.6f       // 60% upward component
ARMORED_RUSH_HIT_COOLDOWN = 800f         // Per-target cooldown (ms)

// Super Punch
ARMORED_SUPER_PUNCH_COUNT = 3            // 3rd punch is super
ARMORED_SUPER_PUNCH_WINDOW = 600         // 600ms combo window

// Armor Reflection
ARMORED_MELEE_RANGE = 30f                // Melee threat detection
ARMORED_MELEE_Y_TOLERANCE = 24f          // Vertical tolerance
ARMORED_PROJ_LOOKAHEAD_SEC = 0.06f       // Projectile prediction
ARMORED_REFLECT_OFFSET = 6f              // Reflected spawn offset
```

---

## API Reference

### Core Callbacks
```csharp
OnStartup()
// Initialize system, set up callbacks, modify weapon spawn rates

InitializeTitanDistribution(TriggerArgs)
// 500ms delayed init, handles daily distribution/restoration

OnUpdate(float elapsed)
// Main update loop: detect pickups, update titans, check abilities

OnPlayerKeyInput(IPlayer, VirtualKeyInfo[])
// Handle ability activation (hardening, armor toggle, slowmo)

OnPlayerMeleeAction(IPlayer, PlayerMeleeHitArg[])
// Handle melee abilities (hardening stagger, claw, super punch)

OnPlayerDeath(IPlayer)
// Handle power extraction

OnProjectileHit(IProjectile, ProjectileHitArgs)
// Handle projectile reflection (Armored)

OnPlayerDamage(IPlayer, PlayerDamageArgs)
// Safety net for armor immunity
```

### Power Distribution Functions
```csharp
DistributeTitanPowers(TriggerArgs)
// Main distribution logic, calls algorithm based on player count

DistributeWithOriginalAlgorithm(IPlayer[])
// For >3 players: random 50/50 with guarantees

DistributeWithHalfSplit(IPlayer[])
// For ≤3 players: half humanoid, half pure

SaveAssignedPowersToStorage()
// Serialize to "playerName:titanType;..."

RestoreAssignedPowersFromStorage()
// Deserialize and restore assignments

GetPlayerGroupKey()
// Generate sorted player name key
```

### Transformation Functions
```csharp
TransformToTitan(IPlayer)
// Complete transformation sequence

ApplyTitanStats(IPlayer, int, TitanType, float)
// Calculate and apply all stat modifiers

GetTitanSize(TitanType)
// Get size (random or fixed based on type)

UpdateTitanPlayersStorage()
// Sync with ODM script via shared storage
```

### Titan Update Functions
```csharp
// Generic
CheckTitanExhaustion(IPlayer, int)
CheckTitanGrabDamage(IPlayer, int)

// Attack Titan
CheckAttackTitanSlowmo(IPlayer, int, float)
CheckAttackTitanHardeningEnergy(IPlayer, int, float)
CheckAttackTitanBotHardening(IPlayer, int, float)
ApplyAttackTitanHardening(IPlayer, int, HardeningMode)

// Jaw Titan
CheckJawTitanHuntDamage(IPlayer, int, float)
HandleJawTitanMelee(IPlayer, int, PlayerMeleeHitArg[], float)
HandleJawTitanClawAttack(IPlayer, int, IPlayer)

// Armored Titan
CheckArmoredTitanArmor(IPlayer, int, float)
CheckArmoredTitanBotArmor(IPlayer, int, float)
CheckArmoredTitanRush(IPlayer, int, float)
CheckArmoredTitanBotRush(IPlayer, int, float)
ApplyArmoredTitanArmor(IPlayer, int, bool)
HandleArmoredTitanMelee(IPlayer, int, PlayerMeleeHitArg[], float)
```

### Power Extraction Functions
```csharp
ExtractTitanPower(IPlayer pureTitan, int pureTitanId, 
                  IPlayer victim, int victimId, TitanType extractedPower)
// Main extraction logic with temporary player handling

GetRandomPureTitanTeammate(IPlayer)
// Find random Pure Titan teammate for transfer

UpdateLabelsAfterPowerSwap(int, int)
// Update floating labels after extraction
```

### Helper Functions
```csharp
// Player Stats
GetPlayerForce(IPlayer) → float
GetPlayerHeight(IPlayer) → float

// Geometry
IsArmoredInFront(IPlayer, Vector2) → bool
IsArmoredFrontMeleeThreat(IPlayer, int) → bool
IsArmoredFrontProjectileThreat(IPlayer, int) → bool

// Bot AI
GetAimedPlayer(IPlayer) → IPlayer
GetPlayerAimingAtBot(IPlayer, int) → IPlayer

// UI
CreateTitanTypeLabels(IPlayer[])
UpdateTitanTypeLabels()
CreateLabelForPlayer(IPlayer, int, TitanType)

// Input Management
QuickCommand(IPlayer, PlayerCommand)
DisableTitanInput(IPlayer)
EnableTitanInput(IPlayer)

// Utility
GetTodayDateString() → string
MakeNearbyPlayersFall(Vector2, float, int)
```

---

## Integration with Other Scripts

### ODM Script Synchronization
```csharp
// TitanODMSync shared storage
IScriptStorage storage = Game.GetSharedStorage("TitanODMSync");
storage.SetItem("TITAN_PLAYERS", serializedTitanList);

// Format: "playerId1,playerId2,playerId3"
// ODM script reads this to prevent equipment on titans
```

### Weapon Spawn Modification
```csharp
// Increases strength boost spawn rate by 2x (100 weight)
// Reduces all other weapons by 50% (OTHERS_SCALE = 0.5)
Game.UpdateWeaponSpawnChances(chances);
```

---

## Performance Considerations

### Update Frequency
- Main OnUpdate: 50ms intervals
- Bot armor checks: 1000ms intervals
- Hardening defensive checks: 2000ms intervals

### Memory Management
- Dictionaries cleaned up on titan death/detransformation
- Labels removed when players die
- Grab data removed when grab ends

### Optimization Tips
1. Use `GetObjectsByArea()` instead of `GetObjects()` for local checks
2. Cache player lists when iterating multiple times in same frame
3. Only update labels for alive players
4. Use cooldown dictionaries to prevent spam checks

---

## Debugging Tips

### Enable Debug Messages
Current debug messages (can be removed for production):
```csharp
Game.ShowChatMessage("Player Group Key: " + playerGroupKey, Color.Yellow);
Game.ShowChatMessage("Distributing new powers...", Color.Green);
Game.ShowChatMessage("OnPlayerDeath called for: " + player.Name, Color.Cyan);
```

### Common Issues
1. **Powers reset every match**: Check date storage keys
2. **Temporary player gets stored**: Verify name comparison (case-insensitive)
3. **Bot behavior broken**: Check if original behavior was stored before transformation
4. **Grab damage on wrong titan**: Verify titan type check in CheckTitanGrabDamage
5. **Armor not reflecting**: Check IsArmoredInFront() calculation

### Testing Checklist
- [ ] Test with 2, 3, 4, 6 players
- [ ] Test with temporary player ("com")
- [ ] Test daily reset (change GetTodayDateString)
- [ ] Test power extraction (Pure vs humanoid)
- [ ] Test each titan's abilities
- [ ] Test bot AI triggers
- [ ] Test exhaustion recovery
- [ ] Test ODM integration

---

## Version History

### Current Version (v1.0)
- 4 Titan types implemented
- Daily power distribution with player group tracking
- Power extraction system
- All titan abilities functional
- Bot AI triggers for all abilities
- ODM script integration

### Future Enhancements
- Additional titan types
- Customizable distribution algorithms
- In-game configuration menu
- Replay/highlight system for power extractions
- Titan vs Titan combat statistics

---

## Credits & License
Part of Superfighters Deluxe custom script collection.
Requires SFD Script API v1.3.7d or higher.

**Author**: [Your Name]
**Last Updated**: 2024-10-19
