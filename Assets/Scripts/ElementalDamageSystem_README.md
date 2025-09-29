# Elemental Damage System

This document describes the new elemental damage system implemented for Tiny Town Tower.

## Overview

The elemental damage system allows weapons and attacks to deal different types of elemental damage (Fire, Ice, Electric, Poison, etc.) with character-specific resistances that modify the final damage amount.

## Key Components

### 1. WeaponElement Enum
Located in `Assets/Scripts/Managers/Enumerates.cs`

```csharp
public enum WeaponElement
{
    NONE,
    BASIC,
    FIRE,
    ICE,
    ELECTRIC,
    POISON,
    BLEED,
    HOLY,
    SHADOW,
    PHYSICAL
}
```

### 2. DamageResistance Enum
Located in `Assets/Scripts/Managers/Enumerates.cs`

```csharp
public enum DamageResistance
{
    IMMUNE = 0,      // 0% damage taken (0x multiplier)
    RESISTANT = 1,   // 50% damage taken (0.5x multiplier)
    NORMAL = 2,      // 100% damage taken (1x multiplier)
    WEAK = 3,        // 150% damage taken (1.5x multiplier)
    VULNERABLE = 4   // 200% damage taken (2x multiplier)
}
```

### 3. Extended IDamageable Interface
The interface now includes elemental damage methods:

```csharp
void TakeDamage(float amount, WeaponElement damageType, Transform damageSource = null);
void TakeDamage(float amount, float poiseDamage, WeaponElement damageType, Transform damageSource = null);
DamageResistance GetResistance(WeaponElement damageType);
```

### 4. Enhanced DamageUtils
New methods for elemental damage calculation:

- `ApplyElementalDamage()` - Applies elemental damage with resistance calculation
- `ApplyElementalDamageWithPoise()` - Includes poise damage
- `GetDamageMultiplier()` - Converts resistance enum to damage multiplier
- `TriggerElementalDamagedAnimation()` - Sets DamageType parameter for animations

## Character Resistance System

### EnemyBase Class
- Default resistances: All elements = NORMAL
- Override `GetDamageResistances()` in derived classes for specific resistances

### HumanCharacterController Class
- Fire, Ice, Electric, Poison: WEAK (1.5x damage)
- Physical, Bleed, Holy, Shadow: NORMAL (1x damage)

## Animation Integration

The system sets the `DamageType` parameter in animators, allowing for different animations based on damage type:

```csharp
animator.SetInteger("DamageType", (int)damageType);
```

## VFX Integration

The `EffectManager` now supports elemental effects:

```csharp
EffectManager.Instance.PlayElementalHitEffect(hitPoint, hitNormal, character, damageType);
```

## Usage Examples

### Basic Elemental Attack
```csharp
// Deal fire damage to a target
target.TakeDamage(25f, WeaponElement.FIRE, attackerTransform);
```

### Attack with Poise Damage
```csharp
// Deal electric damage with poise damage
target.TakeDamage(25f, 15f, WeaponElement.ELECTRIC, attackerTransform);
```

### Check Resistance
```csharp
// Check target's resistance to fire
DamageResistance resistance = target.GetResistance(WeaponElement.FIRE);
float damageMultiplier = DamageUtils.GetDamageMultiplier(resistance);
```

### Weapon Implementation
See `Assets/Scripts/Weapons/ElementalWeaponExample.cs` for a complete example of how to implement elemental weapons.

## Damage Multipliers

| Resistance | Damage Multiplier | Description |
|------------|-------------------|-------------|
| IMMUNE | 0.0x | No damage taken |
| RESISTANT | 0.5x | Half damage |
| NORMAL | 1.0x | Normal damage |
| WEAK | 1.5x | 50% more damage |
| VULNERABLE | 2.0x | Double damage |

## Customizing Resistances

### For New Enemy Types
Override the `GetDamageResistances()` method:

```csharp
protected override Dictionary<WeaponElement, DamageResistance> GetDamageResistances()
{
    return new Dictionary<WeaponElement, DamageResistance>
    {
        { WeaponElement.FIRE, DamageResistance.RESISTANT },
        { WeaponElement.ICE, DamageResistance.WEAK },
        { WeaponElement.ELECTRIC, DamageResistance.IMMUNE }
    };
}
```

### For NPCs
Modify the resistance dictionary in `HumanCharacterController.GetDamageResistances()` or create derived classes.

## Future Enhancements

1. **Status Effects**: Elemental damage could trigger status effects (burning, frozen, shocked, etc.)
2. **Elemental Combinations**: Some elements could interact (fire + ice = steam damage)
3. **Environmental Interactions**: Elemental attacks could affect the environment
4. **Equipment Resistances**: Armor and equipment could modify resistances
5. **Dynamic Resistances**: Resistances could change based on conditions or buffs

## Integration with Existing Systems

The elemental damage system is fully backward compatible:
- Existing `TakeDamage(float, Transform)` calls still work
- Existing `TakeDamage(float, float, Transform)` calls still work
- New elemental methods are additive, not replacing

## Testing

Use the `ElementalWeaponExample` script to test the system:
1. Attach to a weapon GameObject
2. Set the weapon element in the inspector
3. Call `PerformElementalAttack()` or `TestResistances()` methods
4. Check console output for resistance calculations

