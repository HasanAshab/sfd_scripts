# Titan System Documentation

Welcome to the complete documentation for the Titan Transformation System!

## Quick Navigation

### Main Documentation
📖 **[Titan.md](Titan.md)** - Complete system overview, architecture, and technical reference

### Titan Type Documentation
Each titan has comprehensive A-Z documentation including stats, abilities, mechanics, and strategies:

🔴 **[Attack Titan](Titan_Types/Attack.md)** - Hardening modes, slowmo ability, versatile fighter
- Base stats: +40% HP, +32% damage, +30% energy
- Three hardening modes: Neutral, Offensive, Defensive
- Slowmotion ability for tactical advantage
- Bot AI with intelligent mode switching

🟢 **[Jaw Titan](Titan_Types/Jaw.md)** - Speed demon, claw attacks, hunt ability
- Base stats: +25% damage, +25% energy, NO speed penalty
- Claw attacks: 2x front, 4x backstab damage
- Hunt ability with dive catch mechanics
- Sharp claw combo breaks objects on 3rd punch
- Smallest (0.88) and fastest titan

🔵 **[Armored Titan](Titan_Types/Armored.md)** - Tank, armor modes, rush, super punch
- Base stats: +32% HP, +25% damage, +50% force
- Armor ON: Front immunity + projectile reflection
- Armor OFF: +30% speed, +30% energy regen
- Rush ability throws enemies and objects
- Super punch combo on 3rd hit
- Largest (1.3) and slowest titan

⚪ **[Pure Titan](Titan_Types/Pure.md)** - Basic type, grab damage, power extraction
- Base stats: +15% HP, +10% damage, +25% force
- Continuous grab damage ability
- Power extraction from humanoid titans
- Can detransform and swap powers
- Random size variation (0.8-1.25)

---

## Documentation Structure

### Titan.md - Main Documentation
- **Core Concepts**: Titan types, transformation triggers, distribution philosophy
- **System Architecture**: File structure, data structures, key dictionaries
- **Power Distribution**: Daily reset, player group keys, storage format
- **Transformation Mechanics**: Size scaling, stat calculations, visual effects
- **Shared Systems**: Exhaustion, grab damage, fall detection
- **Constants Reference**: All configuration values
- **API Reference**: All functions and callbacks
- **Integration**: ODM script sync, weapon spawns
- **Performance**: Update frequencies, memory management
- **Debugging**: Common issues, testing checklist

### Titan Type Documentation (Attack.md, Jaw.md, Armored.md, Pure.md)
Each titan type document includes:
- **Overview**: High-level description
- **Statistics**: Base stats + size-based scaling
- **Appearance**: Profile details and visual characteristics
- **Abilities**: Complete mechanics with formulas and examples
- **Bot AI Behavior**: Combat patterns and ability triggers
- **Exhaustion System**: How exhaustion affects this titan
- **Combat Strategies**: Tactical examples and scenarios
- **Stat Calculations**: Detailed examples with numbers
- **Technical Implementation**: Dictionary entries and cleanup
- **Testing Checklist**: Comprehensive feature verification
- **Debugging**: Common issues and solutions
- **Related Systems**: Links to shared functionality

---

## Quick Reference

### Titan Comparison Table

| Titan | HP | Damage | Force | Speed | Energy | Size | Role |
|-------|-----|--------|-------|-------|--------|------|------|
| **Pure** | +15% | +10% | +25% | -30% | Base | Random | Bruiser |
| **Attack** | +40% | +32% | +1% | -20% | +30% | Random | Versatile |
| **Jaw** | Base | +25% | Base | Base | +25% | 0.88 | Assassin |
| **Armored** | +32% | +25% | +50% | -50% | +15% | 1.30 | Tank |

### Key Features by Titan

#### Pure Titan
- ✅ Grab damage (continuous)
- ✅ Power extraction (swap with victim)
- ✅ Random size variation
- ❌ No special abilities

#### Attack Titan
- ✅ Hardening modes (Neutral/Offensive/Defensive)
- ✅ Slowmotion ability
- ✅ Offensive stagger (70% chance)
- ✅ Defensive damage reduction (75%)
- ✅ Intelligent bot AI mode switching

#### Jaw Titan
- ✅ Claw attacks (2x front, 4x backstab)
- ✅ Hunt ability (dive damage)
- ✅ Sharp claw combo (3rd punch breaks objects)
- ✅ Grab damage
- ✅ Fastest movement speed

#### Armored Titan
- ✅ Armor toggle (ON: immunity+reflection, OFF: mobility)
- ✅ Rush ability (throw enemies/objects while sprinting)
- ✅ Super punch combo (3rd punch launches enemy)
- ✅ Highest HP and Force
- ✅ Slowest movement

---

## Getting Started

### For Players
1. Pick up **Strength Boost** powerup to transform
2. Check your floating label: `[ATK]` / `[JAW]` / `[ARM]` or none (Pure)
3. Learn your titan's abilities (see titan type docs)
4. Powers persist for the day (24 hours)

### For Developers
1. Read **[Titan.md](Titan.md)** for system architecture
2. Review **[Power Distribution](Titan.md#power-distribution-system)** for persistence logic
3. Study titan type docs for ability implementation details
4. Check **[API Reference](Titan.md#api-reference)** for function signatures
5. Use **[Debugging](Titan.md#debugging-tips)** section for troubleshooting

### For AI Agents
1. Start with **[Titan.md](Titan.md)** - Overview and Architecture sections
2. Understand **[Power Distribution System](Titan.md#power-distribution-system)** first
3. Review **[Data Structures](Titan.md#key-data-structures)** to understand tracking
4. Study one titan type at a time (start with Pure, simplest)
5. Check **[Constants Reference](Titan.md#constants-reference)** for all config values
6. Use **[API Reference](Titan.md#api-reference)** to find specific functions

---

## Common Tasks

### How to Add a New Titan Type
1. Add enum value to `TitanType`
2. Add constants for stats and abilities
3. Add tracking dictionaries (if needed)
4. Update `DistributeTitanPowers()` to include new type
5. Add transformation profile in `TransformToTitan()`
6. Add stat application in `ApplyTitanStats()`
7. Add ability logic in `OnUpdate()` / `OnPlayerMeleeAction()` / etc.
8. Update `CreateTitanTypeLabels()` for label
9. Create documentation in `Titan_Types/NewTitan.md`

### How to Modify Distribution Algorithm
1. Review `DistributeTitanPowers()` in **[Titan.md](Titan.md#power-distribution-system)**
2. Modify `DistributeWithOriginalAlgorithm()` or `DistributeWithHalfSplit()`
3. Test with different player counts
4. Update `HUMANOID_TITAN_COUNT` if adding new humanoid types

### How to Debug Power Extraction
1. Enable debug messages in `OnPlayerDeath()`
2. Check `gibbedByPureTitan` dictionary tracking
3. Verify victim has humanoid power in `assignedTitanPowers`
4. Confirm Pure Titan still exists and is Pure type
5. Review **[Pure Titan - Power Extraction](Titan_Types/Pure.md#2-power-extraction-active-on-kill)**

### How to Adjust Bot AI
1. Find relevant bot check function (e.g., `CheckAttackTitanBotHardening`)
2. Modify trigger distances, thresholds, or conditions
3. Test with bot players
4. Review Bot AI Behavior sections in titan type docs

---

## File Structure
```
Titan_Docs/
├── README.md (this file)
├── Titan.md (main documentation)
└── Titan_Types/
    ├── Pure.md
    ├── Attack.md
    ├── Jaw.md
    └── Armored.md
```

---

## Version Information
- **Script Version**: 1.0
- **SFD Version**: 1.3.7d or higher
- **Last Updated**: 2024-10-19
- **Total Lines of Code**: ~3200
- **Total Constants**: 50+
- **Total Functions**: 80+

---

## External Dependencies
- **ODM Script Integration**: Shared storage for titan player tracking
- **SFD Script API**: All game functionality via IGame interface
- **SharedStorage**: Persistent storage for daily distribution

---

## Credits
Custom script for Superfighters Deluxe by [Your Name]

For questions or issues, refer to the debugging sections in each document.

---

## Quick Links Summary
- 📖 [Main Documentation](Titan.md)
- 🔴 [Attack Titan](Titan_Types/Attack.md)
- 🟢 [Jaw Titan](Titan_Types/Jaw.md)
- 🔵 [Armored Titan](Titan_Types/Armored.md)
- ⚪ [Pure Titan](Titan_Types/Pure.md)
