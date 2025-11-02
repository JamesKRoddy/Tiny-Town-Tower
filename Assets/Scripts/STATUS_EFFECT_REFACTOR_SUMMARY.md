# Status Effect System - Slot-Based Multi-Effect Architecture

## Overview
The status effect system uses a **slot-based organization** that allows multiple effects to coexist naturally. An NPC can be **BURNING + EXHAUSTED + SICK + WORKING** all at once, with each effect in its appropriate display slot.

---

## Core Problem Solved

**Issue**: The old system treated status effects as if only ONE should be primary, but reality is:
- NPCs can be **BURNING + ELECTROCUTED** (multiple elementals)
- NPCs can be **SICK + TIRED + WORKING** (health + activity)  
- Effects should be organized by **context** not a single priority

**Solution**: **Slot-based architecture** where effects are categorized into display slots that can show multiple effects simultaneously.

---

## Architecture

### 1. **Category System** (Granular Organization)

Effects are grouped into **fine-grained categories** that define mutual exclusivity:

#### Health Categories (Mutually Exclusive Within Category)
- `HEALTH_HUNGER`: HUNGRY, STARVING (can't be both)
- `HEALTH_FATIGUE`: TIRED, EXHAUSTED (can't be both)
- `HEALTH_ILLNESS`: SICK (standalone)

#### Activity Category (Mutually Exclusive)
- `ACTIVITY`: WORKING, SLEEPING, EATING (only one at a time)

#### Combat Category (Mutually Exclusive)
- `COMBAT`: FIGHTING, FLEEING (can't do both)

#### Elemental Categories (CAN Stack Across Types!)
- `ELEMENTAL_FIRE`: ON_FIRE, BURNING
- `ELEMENTAL_COLD`: FROZEN
- `ELEMENTAL_ELECTRIC`: ELECTROCUTED

**Key Design**: Fire + Electric = ✅ Coexist  
**Key Design**: Fire + Cold = ❌ Conflict

#### Other Categories
- `MEDICAL`: RECEIVING_MEDICAL_TREATMENT
- `POSITIVE`: HEALTHY

---

### 2. **Display Slot System**

Effects are organized into **display slots** for UI presentation:

```csharp
public class DisplayOrganization
{
    List<StatusEffectType> PrimaryConditions;  // Combat, elementals (MOST URGENT)
    List<StatusEffectType> HealthStatus;       // Hunger, fatigue, sickness
    List<StatusEffectType> ActivityStatus;     // Working, sleeping, eating
    List<StatusEffectType> SpecialStatus;      // Medical, healthy
}
```

#### Slot Rules
1. **PRIMARY_CONDITION**: All critical effects (combat, elementals) - shows ALL active
2. **HEALTH_STATUS**: Most severe from each health category - shows ALL types
3. **ACTIVITY_STATUS**: Current activity - shows ONE (hidden if critical condition)
4. **SPECIAL_STATUS**: Medical treatment or healthy status

---

## Example Scenarios

### Scenario 1: Multiple Elementals
```csharp
// NPC is on fire AND electrocuted
activeEffects = { ON_FIRE, ELECTROCUTED }

organized = OrganizeForDisplay(activeEffects)
// PRIMARY_CONDITION: [ON_FIRE, ELECTROCUTED]  ← Both shown!
// HEALTH_STATUS: []
// ACTIVITY_STATUS: []
// SPECIAL_STATUS: []
```

### Scenario 2: Complex Multi-Effect
```csharp
// NPC is burning, exhausted, sick, and working
activeEffects = { BURNING, EXHAUSTED, SICK, WORKING }

organized = OrganizeForDisplay(activeEffects)
// PRIMARY_CONDITION: [BURNING]               ← Critical!
// HEALTH_STATUS: [EXHAUSTED, SICK]           ← Both health issues shown
// ACTIVITY_STATUS: []                        ← Hidden due to critical condition
// SPECIAL_STATUS: []
```

### Scenario 3: Health Issues Only
```csharp
// NPC is hungry, tired, and working
activeEffects = { HUNGRY, TIRED, WORKING }

organized = OrganizeForDisplay(activeEffects)
// PRIMARY_CONDITION: []
// HEALTH_STATUS: [HUNGRY, TIRED]             ← Both shown
// ACTIVITY_STATUS: [WORKING]                 ← Shown (no critical condition)
// SPECIAL_STATUS: []
```

### Scenario 4: Smart Filtering
```csharp
// NPC is both hungry and starving (system picks most severe)
activeEffects = { HUNGRY, STARVING, WORKING }

organized = OrganizeForDisplay(activeEffects)
// PRIMARY_CONDITION: []
// HEALTH_STATUS: [STARVING]                  ← HUNGRY hidden (redundant)
// ACTIVITY_STATUS: [WORKING]
// SPECIAL_STATUS: []
```

---

## Conflict Resolution

### Automatic Conflict Detection
```csharp
AreEffectsConflicting(ON_FIRE, FROZEN)      // ✅ true  - fire melts ice
AreEffectsConflicting(ON_FIRE, ELECTROCUTED) // ❌ false - can coexist
AreEffectsConflicting(HUNGRY, STARVING)     // ✅ true  - same category
AreEffectsConflicting(FIGHTING, FLEEING)    // ✅ true  - can't do both
```

### Conflict Rules
1. **Same category (mutually exclusive)**: HUNGRY vs STARVING → conflict
2. **Fire vs Cold elementals**: ON_FIRE vs FROZEN → conflict
3. **Combat vs Sleeping**: FIGHTING vs SLEEPING → conflict
4. **Different elemental types**: ON_FIRE + ELECTROCUTED → NO conflict!

---

## Key API Methods

### Organize for Display
```csharp
var organized = StatusEffectUtils.OrganizeForDisplay(npc.GetActiveStatusEffects());

// Access organized slots:
foreach (var effect in organized.PrimaryConditions)  // Combat, elementals
foreach (var effect in organized.HealthStatus)       // Health issues
foreach (var effect in organized.ActivityStatus)     // What they're doing
foreach (var effect in organized.SpecialStatus)      // Medical, healthy
```

### Legacy Flat List (Backward Compatibility)
```csharp
// Returns flattened list using new slot logic
var displayable = StatusEffectUtils.GetDisplayableEffects(activeEffects);
// Returns: [BURNING, SICK, EXHAUSTED, WORKING] in priority order
```

### Check Specific Conditions
```csharp
bool hasHealthIssues = StatusEffectUtils.HasNegativeHealthCondition(activeEffects);
bool isCritical = StatusEffectUtils.HasCriticalCondition(activeEffects);
var category = StatusEffectUtils.GetCategory(StatusEffectType.ON_FIRE);
var slot = StatusEffectUtils.GetDisplaySlot(StatusEffectType.SICK);
```

---

## Priority System (Within Slots)

Priority determines **order within each slot**, not which effects are shown:

### Critical (100+) - PRIMARY_CONDITION Slot
- **ON_FIRE/BURNING**: 110
- **FIGHTING**: 105
- **FLEEING**: 104
- **FROZEN/ELECTROCUTED**: 103

### High (80-99) - HEALTH_STATUS Slot
- **STARVING**: 90
- **SICK**: 85
- **EXHAUSTED**: 82

### Medium (50-79) - HEALTH_STATUS Slot
- **HUNGRY**: 70
- **TIRED**: 65
- **MEDICAL_TREATMENT**: 60

### Low (20-49) - ACTIVITY_STATUS Slot
- **SLEEPING**: 40
- **EATING**: 35
- **WORKING**: 30

### Baseline (0-19) - SPECIAL_STATUS Slot
- **HEALTHY**: 10

---

## Editor Display

### Before (Single Priority)
```
Active Status Effects (4):
  ★ Sick (P:85, HEALTH_CONDITION)
    • Tired (P:65, HEALTH_CONDITION)
    • Working (P:30, ACTIVITY)
  (1 effects hidden)
```

### After (Slot-Based)
```
Active Status Effects (5 total, organized by slot):

⚠ Critical:
  • On Fire
  • Electrocuted

❤ Health:
  • Sick
  • Exhausted

⚙ Activity:
  • Working
```

**Visual**: Effects are clearly organized by urgency and type!

---

## Benefits

### ✅ Natural Multi-Effect Support
- NPCs can have multiple elementals, health issues, and activities
- No artificial "pick one" limitation
- Reflects reality: you can be burning AND exhausted

### ✅ Smart Organization
- Critical effects (combat, fire) always visible
- Health issues grouped together
- Activities shown contextually

### ✅ Intelligent Filtering
- Shows STARVING, hides HUNGRY (redundant)
- Shows EXHAUSTED, hides TIRED (redundant)
- Hides activities during critical conditions

### ✅ Extensible
- Adding new effects: Update category in ONE place
- Conflict rules automatic based on category
- Display logic handles new effects automatically

### ✅ Performance
- O(1) lookups with HashSet
- Efficient LINQ-based organization
- No redundant tracking variables

---

## Files Modified

### Core System
- ✅ **`StatusEffectUtils.cs`**: Complete rewrite with slot-based architecture
- ✅ **`SettlerNPC.cs`**: Uses organized display for health status
- ✅ **`SettlerNPCEditor.cs`**: Shows effects organized by slot with icons
- ✅ **`Interfaces.cs`**: IStatusEffectTarget owns effect data
- ✅ **`EffectManager.cs`**: Handles VFX only, delegates to objects

---

## Design Principles

### 1. **Granular Categories**
Instead of broad "HEALTH" or "ELEMENTAL", use specific categories:
- `HEALTH_HUNGER` vs `HEALTH_FATIGUE` (can coexist!)
- `ELEMENTAL_FIRE` vs `ELEMENTAL_ELECTRIC` (can stack!)

### 2. **Slot-Based Display**
Organize by **context** not single priority:
- PRIMARY: Combat + all elementals
- HEALTH: All health issues
- ACTIVITY: What they're doing
- SPECIAL: Medical/healthy

### 3. **Smart Within-Category Filtering**
Show most severe within mutually-exclusive categories:
- STARVING over HUNGRY
- EXHAUSTED over TIRED

### 4. **Cross-Category Coexistence**
Different categories can coexist:
- SICK + TIRED + ON_FIRE = All shown (different categories)

---

## Real-World Examples

### NPC in Combat (Burning + Electrocuted + Sick)
```
⚠ Critical:
  • On Fire           ← Both elementals shown
  • Electrocuted

❤ Health:
  • Sick              ← Health issue shown

✓ Special:
  • Receiving Treatment
```

### NPC Recovering (Exhausted + Eating)
```
❤ Health:
  • Exhausted

⚙ Activity:
  • Eating

✓ Special:
  • Receiving Treatment
```

### Healthy Working NPC
```
⚙ Activity:
  • Working

✓ Special:
  • Healthy
```

---

## Summary

The status effect system now uses a **multi-slot architecture** that:
1. **Allows multiple effects** to coexist naturally
2. **Organizes by context** (critical/health/activity/special)
3. **Filters intelligently** within categories
4. **Scales effortlessly** for new effects

NPCs can now be **BURNING + ELECTROCUTED + SICK + WORKING** and the system handles it beautifully! 🔥⚡🤒⚙️
