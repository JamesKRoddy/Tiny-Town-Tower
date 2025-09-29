# Weapon Elemental Integration System

This document describes how the elemental damage system has been integrated with the existing weapon system in Tiny Town Tower.

## Overview

The weapon system now fully supports elemental damage, resistance calculations, and status effects. All weapon types (Melee, Ranged, Throwable) automatically handle elemental damage based on their configuration.

## Updated Components

### 1. WeaponScriptableObj (Enhanced)

**New Properties:**
```csharp
[Header("Elemental Properties")]
public WeaponElement weaponElement = WeaponElement.NONE;
public int elementalDamageBonus = 0;
public bool isPureElemental = false;
public StatusEffectType[] possibleStatusEffects = new StatusEffectType[0];
public float statusEffectChance = 0.1f;
public float statusEffectDuration = 5f;
```

**Key Features:**
- **Elemental Damage Bonus**: Additional damage added to base damage for elemental weapons
- **Pure Elemental**: Option to make weapons deal only elemental damage (ignores physical resistances)
- **Status Effects**: Weapons can apply status effects on hit
- **Status Effect Chance**: Probability of applying status effects (0-1)
- **Status Effect Duration**: How long status effects last

### 2. WeaponBase (Enhanced)

**New Methods:**
```csharp
// Elemental damage dealing
public virtual void DealDamage(IDamageable target, Transform damageSource)

// Status effect application
protected virtual void ApplyStatusEffects(IDamageable target, Transform damageSource)
protected virtual void ApplyStatusEffect(IDamageable target, StatusEffectType effectType)

// Damage calculation
public int GetTotalDamage() // Includes elemental bonus
public int GetCurrentElementalDamageBonus()
```

**New Properties:**
```csharp
public WeaponElement WeaponElement => weaponData?.weaponElement ?? WeaponElement.NONE;
public bool IsPureElemental => weaponData?.isPureElemental ?? false;
public StatusEffectType[] PossibleStatusEffects => weaponData?.possibleStatusEffects ?? new StatusEffectType[0];
public float StatusEffectChance => weaponData?.statusEffectChance ?? 0f;
public float StatusEffectDuration => weaponData?.statusEffectDuration ?? 0f;
```

## Weapon Type Updates

### MeleeWeapon
- Now uses `DealDamage()` method instead of direct `TakeDamage()` calls
- Automatically handles elemental damage and status effects

### RangedWeapon
- Updated to use elemental damage system
- Fire point used as damage source for proper VFX positioning

### ThrowableWeapon
- Enhanced collision handler to support elemental damage
- Passes weapon data to collision handler for full elemental support

## How It Works

### 1. Damage Calculation
```csharp
// Total damage = base damage + elemental bonus
int totalDamage = GetCurrentDamage() + GetCurrentElementalDamageBonus();

// Damage type determined by weapon element
if (elementType == WeaponElement.NONE || elementType == WeaponElement.PHYSICAL)
{
    target.TakeDamage(totalDamage, poiseDamage, damageSource); // Physical
}
else
{
    target.TakeDamage(totalDamage, poiseDamage, elementType, damageSource); // Elemental
}
```

### 2. Resistance Application
- Elemental damage automatically applies target resistances
- Physical damage uses normal resistance calculations
- Pure elemental weapons bypass physical resistances

### 3. Status Effect Application
- Random chance based on `statusEffectChance`
- Random selection from `possibleStatusEffects` array
- Customizable duration via `statusEffectDuration`

## Usage Examples

### Creating Elemental Weapons

**Fire Sword:**
```csharp
// In WeaponScriptableObj
weaponElement = WeaponElement.FIRE;
elementalDamageBonus = 15;
possibleStatusEffects = new StatusEffectType[] { StatusEffectType.BURNING };
statusEffectChance = 0.3f;
statusEffectDuration = 5f;
```

**Ice Staff:**
```csharp
// In WeaponScriptableObj
weaponElement = WeaponElement.ICE;
elementalDamageBonus = 10;
isPureElemental = true; // Deals only ice damage
possibleStatusEffects = new StatusEffectType[] { StatusEffectType.FROZEN };
statusEffectChance = 0.2f;
statusEffectDuration = 3f;
```

**Lightning Bow:**
```csharp
// In WeaponScriptableObj
weaponElement = WeaponElement.ELECTRIC;
elementalDamageBonus = 20;
possibleStatusEffects = new StatusEffectType[] { 
    StatusEffectType.ELECTROCUTED, 
    StatusEffectType.SHOCKED 
};
statusEffectChance = 0.4f;
statusEffectDuration = 4f;
```

### Using Weapons in Code

```csharp
// Get weapon component
WeaponBase weapon = GetComponent<WeaponBase>();

// Deal damage (automatically handles elemental damage)
weapon.DealDamage(target, transform);

// Check weapon properties
if (weapon.WeaponElement == WeaponElement.FIRE)
{
    Debug.Log("This is a fire weapon!");
}

// Get total damage including elemental bonus
int totalDamage = weapon.GetTotalDamage();
```

## Integration with Existing Systems

### Character Resistances
- All characters automatically have resistance values for each element
- Damage is modified based on target's resistance to weapon's element
- Buildings have normal resistance to all elements

### Animation System
- `DamageType` parameter automatically set based on weapon element
- Different animations can be triggered for different damage types

### VFX System
- Elemental effects automatically played based on weapon element
- Hit effects vary by damage type

### Status Effect System
- Weapons can apply status effects on hit
- Integration point ready for status effect manager

## Migration Guide

### For Existing Weapons
1. **No Breaking Changes**: Existing weapons continue to work
2. **Add Elemental Properties**: Set `weaponElement` and `elementalDamageBonus` in ScriptableObject
3. **Configure Status Effects**: Add desired status effects and chances
4. **Test Resistance Interactions**: Verify damage calculations with different targets

### For New Weapons
1. **Choose Element Type**: Select appropriate `WeaponElement`
2. **Set Damage Bonus**: Configure `elementalDamageBonus` for additional damage
3. **Configure Status Effects**: Add status effects that make sense for the element
4. **Test Thoroughly**: Verify against different character types

## Best Practices

### Elemental Design
- **Fire Weapons**: High damage, burning effects, good against organic targets
- **Ice Weapons**: Slowing effects, good against fast enemies
- **Electric Weapons**: Chain damage potential, good against machines
- **Poison Weapons**: Damage over time, good against high-health targets
- **Holy Weapons**: Good against undead/shadow creatures
- **Shadow Weapons**: Good against holy/light creatures

### Balance Considerations
- **Elemental Bonus**: Should be 20-50% of base damage for balance
- **Status Effect Chance**: 10-40% is usually good for gameplay
- **Status Effect Duration**: 3-8 seconds provides meaningful impact
- **Pure Elemental**: Use sparingly, only for special weapons

### Performance
- Status effect checks only happen on successful hits
- Resistance calculations are cached per character
- Elemental damage uses the same damage pipeline as physical damage

## Testing

Use the `ElementalWeaponIntegrationExample` script to test weapon configurations:

1. **Assign Weapon**: Set `testWeapon` in inspector
2. **Set Target**: Assign a target with `IDamageable` component
3. **Test Damage**: Use context menu "Test Weapon Damage"
4. **View Info**: Check console for detailed weapon information
5. **Test All**: Use "Test All Weapon Types" to test multiple weapons

## Future Enhancements

1. **Elemental Combinations**: Weapons with multiple elements
2. **Environmental Interactions**: Elemental weapons affecting environment
3. **Weapon Upgrades**: Elemental damage increases with weapon level
4. **Elemental Synergies**: Certain element combinations deal bonus damage
5. **Dynamic Resistances**: Resistances that change based on conditions
