# Redundant Bool Cleanup Summary

## Problem
Several mutation classes had redundant boolean fields that were used to enable/disable features when the related float/int values could naturally represent "disabled" state (0 = disabled).

## Changes Made

### 1. SurvivalMutation.cs

#### ✅ Removed `enableHealthRegen` bool
- **Before**: `enableHealthRegen = false` + `healthRegenPerSecond = 1f`
- **After**: `healthRegenPerSecond = 0f` (0 = disabled)
- **Logic**: `if (healthRegenPerSecond > 0f)` instead of `if (enableHealthRegen)`

#### ✅ Removed `modifyMaxHealth` bool  
- **Before**: `modifyMaxHealth = false` + `maxHealthMultiplier = 1.0f` + `flatMaxHealthBonus = 0`
- **After**: Just the multiplier and bonus fields
- **Logic**: `if (maxHealthMultiplier != 1.0f || flatMaxHealthBonus != 0)` instead of `if (modifyMaxHealth)`

#### ✅ Removed `enableDamageReduction` bool
- **Before**: `enableDamageReduction = false` + `damageReductionPercentage = 0.1f`
- **After**: `damageReductionPercentage = 0f` (0 = disabled)
- **Logic**: `if (damageReductionPercentage > 0f)` instead of `if (enableDamageReduction)`

### 2. ElementalMutation.cs

#### ✅ Removed `modifyElementalDamage` bool
- **Before**: `modifyElementalDamage = false` + `elementalDamageMultiplier = 1.0f` + `flatElementalDamageBonus = 0`
- **After**: Just the multiplier and bonus fields
- **Logic**: `if (elementalDamageMultiplier != 1.0f || flatElementalDamageBonus != 0)` instead of `if (modifyElementalDamage)`

#### ✅ Removed `enableConversion` bool
- **Before**: `enableConversion = false` + `conversionPercentage = 0.5f`
- **After**: `conversionPercentage = 0f` (0 = disabled)
- **Logic**: `if (conversionPercentage > 0f)` instead of `if (enableConversion)`

### 3. ConditionalMutation.cs

#### ❌ Kept `modifyResistance` bool (in ElementalMutation)
- **Reason**: This is a different type of condition - it's about whether to modify resistance at all, not about a threshold value
- **Alternative**: Could potentially be removed if we used a "NONE" resistance level, but the current approach is clearer

#### ❌ Kept all bools in ConditionalMutation
- **Reason**: These represent binary states that don't have natural "0 = disabled" equivalents:
  - `requireFullHealth` - Either require full health or don't (not a threshold)
  - `requirePoiseBroken` - Either require broken poise or don't (not a threshold)  
  - `requireInCombat` / `requireOutOfCombat` - Binary combat states

## Benefits

1. **Cleaner Inspector**: Fewer fields to configure
2. **More Intuitive**: 0 naturally means "disabled" for numeric values
3. **Less Redundancy**: No need to check both a bool AND a value
4. **Consistent Pattern**: All numeric thresholds use the same "0 = disabled" pattern

## Pattern for Future

When designing mutation fields, use this pattern:
- **Numeric values**: Use 0, 1.0, or NONE as "disabled" state
- **Binary states**: Use bools only when there's no natural "disabled" value
- **Arrays**: Use empty arrays as "disabled" state

## Examples

### ✅ Good (No redundant bool)
```csharp
[SerializeField] private float healthRegenPerSecond = 0f; // 0 = disabled
[SerializeField] private float damageMultiplier = 1.0f;   // 1.0 = no change
[SerializeField] private AttackElement element = AttackElement.NONE; // NONE = any
```

### ❌ Bad (Redundant bool)
```csharp
[SerializeField] private bool enableHealthRegen = false;
[SerializeField] private float healthRegenPerSecond = 1f; // Redundant!
```

### ✅ Good (Necessary bool)
```csharp
[SerializeField] private bool requireFullHealth = false; // Binary state
[SerializeField] private bool requireInCombat = false;   // Binary state
```
