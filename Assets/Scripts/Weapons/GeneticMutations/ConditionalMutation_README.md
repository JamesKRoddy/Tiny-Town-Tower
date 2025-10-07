# Conditional Mutation System

## Overview

The `ConditionalMutation` class is a powerful wrapper mutation that applies another mutation only when specific conditions are met. This allows you to create complex, dynamic mutation effects without writing new code.

## Key Design Feature: No Enum Required!

Unlike typical conditional systems, **there's no condition type enum**. Instead, you simply configure the fields you want to check:
- Leave fields at their default values (0, NONE, false, empty) to **ignore** that condition
- Set fields to specific values to **enable** that condition
- **Multiple configured conditions = AND logic** (all must be true)

This design is more intuitive, flexible, and allows complex multi-condition setups!

## How It Works

1. **Create a mutation prefab** with the mutation you want to trigger (e.g., CombatMutation, ElementalMutation, etc.)
2. **Configure that mutation's values** in the Inspector (damage multipliers, thresholds, etc.)
3. **Create your ConditionalMutation** on a GameObject
4. **Configure the conditions** you want to check (set thresholds, select elements, check status effects, etc.)
5. **Leave unused conditions at default** (0 for thresholds, NONE for enums, false for bools)
6. **Reference the mutation prefab** in the `mutationPrefab` field
7. **When ALL conditions are met**, the ConditionalMutation instantiates and activates the mutation prefab automatically

## Available Configuration Options

### Health Conditions

| Field | Type | Description |
|-------|------|-------------|
| `healthBelowThreshold` | 0.0-1.0 | Triggers when health % is below this value (0 = disabled) |
| `healthAboveThreshold` | 0.0-1.0 | Triggers when health % is above this value (0 = disabled) |
| `requireFullHealth` | bool | Requires character to be at maximum health |

**Example Configurations:**
- **Low Health**: Set `healthBelowThreshold = 0.3` (triggers below 30% health)
- **High Health**: Set `healthAboveThreshold = 0.8` (triggers above 80% health)  
- **Full Health**: Check `requireFullHealth` checkbox
- **Medium Health Range**: Set `healthAboveThreshold = 0.4` AND `healthBelowThreshold = 0.7` (40-70% health)

---

### Poise Conditions

| Field | Type | Description |
|-------|------|-------------|
| `poiseBelowThreshold` | 0.0-1.0 | Triggers when poise % is below this value (0 = disabled) |
| `poiseAboveThreshold` | 0.0-1.0 | Triggers when poise % is above this value (0 = disabled) |
| `requirePoiseBroken` | bool | Requires poise to be at zero (broken) |

**Example Configurations:**
- **Low Poise**: Set `poiseBelowThreshold = 0.3`
- **High Poise**: Set `poiseAboveThreshold = 0.8`
- **Poise Broken**: Check `requirePoiseBroken` checkbox

---

### Weapon Conditions

| Field | Type | Description |
|-------|------|-------------|
| `requiredWeaponElement` | AttackElement | Requires specific weapon element (NONE = any) |
| `requiredWeaponType` | WeaponAnimationType | Requires specific weapon type (NONE = any) |

**Available Elements:** NONE, BASIC, FIRE, ICE, ELECTRIC, POISON, BLEED, HOLY, SHADOW, PHYSICAL

**Available Types:** NONE, ONE_HANDED, TWO_HANDED

**Example Configurations:**
- **Fire Weapon Only**: Set `requiredWeaponElement = FIRE`
- **Two-Handed Only**: Set `requiredWeaponType = TWO_HANDED`
- **Fire Two-Handed**: Set both fields above (AND logic!)

---

### Status Effect Conditions

| Field | Type | Description |
|-------|------|-------------|
| `requiredStatusEffects` | StatusEffectType[] | Array of status effects that ALL must be active (empty = none required) |

**Example Configurations:**
- **On Fire**: Add `ON_FIRE` to array
- **Frozen or Poisoned**: Add both `FROZEN` and `POISONED` (must have BOTH)
- **Any Status Effect**: Leave empty array

---

### Combat State Conditions

| Field | Type | Description |
|-------|------|-------------|
| `requireInCombat` | bool | Requires character to be in combat |
| `requireOutOfCombat` | bool | Requires character to be out of combat |
| `combatTimeout` | float | Seconds without damage to be considered "out of combat" |

**Example Configurations:**
- **In Combat**: Check `requireInCombat`, set `combatTimeout = 5.0` (5 seconds)
- **Out of Combat**: Check `requireOutOfCombat`

---

## Complex Multi-Condition Examples

The real power comes from combining multiple conditions!

---

## Configuration Examples

### Example 1: Simple Berserker (Low Health → Damage Boost)

**Step 1: Create the mutation prefab**
```
Prefab: "BerserkerCombatMutation"
└─ CombatMutation (component)
   ├─ Damage Multiplier: 2.0 (double damage)
   └─ Attack Speed Multiplier: 1.5 (50% faster)
```

**Step 2: Create the conditional mutation**
```
GameObject: "ConditionalBerserker"
└─ ConditionalMutation
   ├─ Health Below Threshold: 0.30 (30%)
   └─ Mutation Prefab: → BerserkerCombatMutation (from step 1)
```

### Example 2: Fire Weapon Synergy (Fire Weapon → Fire Boost)

**Step 1: Create the mutation prefab**
```
Prefab: "FireBoostMutation"
└─ ElementalMutation (component)
   ├─ Affected Element: FIRE
   ├─ Elemental Damage Multiplier: 1.5
   └─ Flat Elemental Damage Bonus: 10
```

**Step 2: Create the conditional mutation**
```
GameObject: "ConditionalFireSynergy"
└─ ConditionalMutation
   ├─ Required Weapon Element: FIRE
   └─ Mutation Prefab: → FireBoostMutation
```

### Example 3: Desperate Fire Master (Low Health AND Fire Weapon!)

**Step 1: Create the mutation prefab**
```
Prefab: "MassiveFireBoost"
└─ ElementalMutation
   ├─ Affected Element: FIRE
   ├─ Elemental Damage Multiplier: 3.0
   └─ Flat Elemental Damage Bonus: 25
```

**Step 2: Create the conditional mutation**
```
GameObject: "ConditionalDesperateFireMaster"
└─ ConditionalMutation
   ├─ Health Below Threshold: 0.30 (30%)
   ├─ Required Weapon Element: FIRE  ← Multiple conditions!
   └─ Mutation Prefab: → MassiveFireBoost
```
**Description:** Triggers ONLY when BOTH health is low AND using a fire weapon!

### Example 4: Combat Medic (Out of Combat → Health Regen)

```
GameObject: "CombatMedicMutation"
├─ ConditionalMutation
│  ├─ Require Out Of Combat: ✓
│  ├─ Combat Timeout: 5.0 seconds
│  └─ Conditional Mutation Effect: → SurvivalMutation (below)
└─ SurvivalMutation
   ├─ Enable Health Regen: ✓
   └─ Health Regen Per Second: 5.0
```

### Example 5: Spite (On Fire → Damage Boost)

```
GameObject: "SpiteMutation"
├─ ConditionalMutation
│  ├─ Required Status Effects: [ON_FIRE]
│  └─ Conditional Mutation Effect: → CombatMutation (below)
└─ CombatMutation
   ├─ Damage Multiplier: 1.5
   └─ Elemental Damage Multiplier: 2.0
```

### Example 6: Perfect Storm (Multiple Status Effects!)

```
GameObject: "PerfectStormMutation"
├─ ConditionalMutation
│  ├─ Required Status Effects: [ON_FIRE, ELECTROCUTED, POISONED]  ← ALL required!
│  └─ Conditional Mutation Effect: → CombatMutation
└─ CombatMutation
   └─ Damage Multiplier: 5.0 (massive boost for surviving 3 status effects!)
```

### Example 7: Glass Cannon (Full Health + Two-Handed)

```
GameObject: "GlassCannonMutation"
├─ ConditionalMutation
│  ├─ Require Full Health: ✓
│  ├─ Required Weapon Type: TWO_HANDED
│  └─ Conditional Mutation Effect: → CombatMutation
└─ CombatMutation
   └─ Damage Multiplier: 3.0
```

### Example 8: Danger Zone (Low Health + Poise Broken + In Combat!)

```
GameObject: "DangerZoneMutation"
├─ ConditionalMutation
│  ├─ Health Below Threshold: 0.25
│  ├─ Require Poise Broken: ✓
│  ├─ Require In Combat: ✓
│  └─ Conditional Mutation Effect: → SurvivalMutation
└─ SurvivalMutation
   ├─ Enable Damage Reduction: ✓
   └─ Damage Reduction: 0.5 (50% - you're in trouble, need protection!)
```

---

## Advanced Techniques

### Built-In AND Logic
The system automatically uses AND logic when you configure multiple conditions:
- **Example**: `healthBelowThreshold = 0.3` + `requiredWeaponElement = FIRE` means "low health AND fire weapon"
- This is much more powerful than single-condition systems!

### OR Logic (Multiple ConditionalMutations)
Create multiple ConditionalMutation GameObjects that apply the same mutation for OR logic:
```
GameObject 1: "BerserkerLowHealth"
└─ ConditionalMutation (health < 30%) → CombatMutation

GameObject 2: "BerserkerOnFire"  
└─ ConditionalMutation (has ON_FIRE) → Same CombatMutation reference

Result: Damage boost when EITHER low health OR on fire!
```

### Nested Conditions
Create a conditional mutation that triggers another conditional mutation:
```
GameObject: "SuperBerserker"
├─ ConditionalMutation #1 (in combat) → ConditionalMutation #2
└─ ConditionalMutation #2 (health < 30%) → CombatMutation

Result: Only applies berserker when in combat AND low health!
```

### Stacking
Enable `allowStacking` to let multiple instances stack:
- If 3 mutations all trigger the same effect, it can stack 3x

---

## Technical Notes

### Event Subscriptions
The system automatically subscribes to relevant events based on the condition type:
- **Health conditions**: Subscribes to `OnDamageTaken` and `OnHeal`
- **Poise conditions**: Subscribes to `OnPoiseBroken`
- **Weapon conditions**: Subscribes to `OnWeaponEquipped`
- **Status effects**: Checks periodically (every 0.5s)
- **Combat state**: Checks periodically (every 1s) and on damage

### Performance
- Periodic checks (status effects, combat state) run coroutines that check every 0.5-1 second
- Event-based checks (health, poise, weapons) have minimal overhead
- Conditions are only evaluated when relevant events occur

### Mutation Lifecycle
1. **Condition Met**: `OnEquip()` is called on the referenced mutation
2. **Condition No Longer Met**: `OnUnequip()` is called on the referenced mutation
3. **Threshold Crossing**: Mutations only toggle when crossing the threshold (prevents flickering)

---

## Integration with Other Systems

### Compatible Mutation Types
- ✅ `CombatMutation` - Damage, attack speed, poise, elemental bonuses
- ✅ `ElementalMutation` - Elemental damage, resistances, conversions
- ✅ `SurvivalMutation` - Health regen, max health, damage reduction
- ✅ `ConditionalMutation` - Yes, you can nest them for complex logic!

### Future Expansion
To add new condition types:
1. Add the condition to the `ConditionType` enum
2. Add the evaluation logic in `EvaluateCondition()`
3. Add event subscriptions in `ApplyEffect()` if needed
4. Add the description text in `GetStatsDescription()`

---

## Tips & Best Practices

1. **Start Simple**: Test basic conditions before creating complex nested setups
2. **Balance Carefully**: Conditional mutations can be very powerful when conditions are easy to meet
3. **Clear Naming**: Name your mutation GameObjects clearly (e.g., "LowHealthDamageBoost")
4. **Test Thresholds**: Different thresholds (20% vs 30% health) can dramatically change gameplay feel
5. **Visual Feedback**: Consider adding VFX or UI indicators when conditional mutations activate
6. **Performance**: Avoid too many periodic checks; prefer event-based conditions when possible

---

## Troubleshooting

**Mutation not triggering?**
- Check that the condition is actually being met (use Debug.Log in CheckCondition)
- Ensure the referenced mutation is properly configured
- Verify event subscriptions are working (check component references)

**Mutation triggering too often?**
- The threshold-crossing logic prevents flickering, but very low thresholds might toggle frequently
- Consider adding a cooldown or delay between toggles if needed

**Status effect conditions not working?**
- Ensure the EffectManager is properly set up
- Check that the character implements IStatusEffectTarget
- Verify status effects are actually being applied to the character

