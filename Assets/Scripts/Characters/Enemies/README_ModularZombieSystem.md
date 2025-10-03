# Modular Zombie Attack System

This document describes the new modular zombie attack system that replaces the previous inheritance-based approach.

## Overview

The new system uses a component-based architecture similar to the boss attack system, allowing for flexible and customizable zombie behaviors. Instead of inheriting from specific zombie classes, zombies now use attack components that can be mixed and matched.

## Key Components

### 1. ZombieAttackBase
Abstract base class for all zombie attack components. Provides common functionality for:
- Attack range validation
- Cooldown management
- Damage dealing
- Effect playing
- Animation coordination

### 2. Attack Components
- **MeleeZombieAttack**: Close-range physical attacks with area damage
- **RangedZombieAttack**: Projectile-based attacks with vomit projectiles
- **LaserZombieAttack**: Continuous laser beam attacks with precise aiming
- **BoomerZombieAttack**: Explosive attacks that deal area damage and kill the zombie

### 3. Zombie (Base Class)
Refactored to automatically manage attack components and select appropriate attacks based on:
- **DISTANCE_BASED**: Select attack based on distance to target (default)
- **PRIORITY**: Use attacks in order of priority (first in list)
- **RANDOM**: Randomly select from available attacks
- **ROTATION_BASED**: Select attack based on rotation requirements

## Usage

### Basic Setup (Prefab-based)

1. Create a GameObject with a `Zombie` component
2. Add desired attack components to the same GameObject:
   - Add `MeleeZombieAttack` for close-range attacks
   - Add `RangedZombieAttack` for projectile attacks
   - Add `LaserZombieAttack` for laser beam attacks
   - Add `BoomerZombieAttack` for explosive suicide attacks
3. Configure each attack component in the inspector (damage, range, cooldown, etc.)
4. The zombie will automatically select the best attack based on distance

**That's it!** No code required. The `Zombie` component automatically:
- Finds all attack components
- Initializes them
- Selects the appropriate attack based on distance to target

### Configuration in Inspector

On the `Zombie` component:
- **Use Modular Attacks**: Check this to enable the modular system (enabled by default)
- **Selection Strategy**: Choose how attacks are selected (DISTANCE_BASED is default)
- **Attack Switch Cooldown**: Minimum time between switching attacks

On each attack component:
- **Range**: Maximum attack distance
- **Cooldown**: Time between attacks
- **Damage**: Base damage amount
- **Poise Damage**: Poise damage amount
- **Elemental Type**: Damage type (Physical, Fire, Poison, etc.)

### Creating Custom Zombie Behaviors (Optional)

Only if you need special behavior, inherit from `Zombie`:

```csharp
public class SpecialBoomerZombie : Zombie
{
    public override void TakeDamage(float amount, Transform damageSource = null)
    {
        // Check if boomer should explode on damage
        var boomerAttack = GetAttackComponent<BoomerZombieAttack>();
        if (boomerAttack != null && boomerAttack.ShouldExplodeOnDamage(amount))
        {
            boomerAttack.ForceExplode();
            return;
        }
        
        base.TakeDamage(amount, damageSource);
    }
}
```

## Creating Different Zombie Types

All zombie types use the same `Zombie` component with different attack components:

### Melee Zombie
```
GameObject with:
- Zombie component
- MeleeZombieAttack component (attackType = 1)
```

### Ranged Zombie (Spitter)
```
GameObject with:
- Zombie component
- RangedZombieAttack component (attackType = 2)
```

### Laser Zombie
```
GameObject with:
- Zombie component
- LaserZombieAttack component (attackType = 3)
```

### Boomer Zombie (Explosive)
```
GameObject with:
- Zombie component
- BoomerZombieAttack component (attackType = 4)
```

### Hybrid Zombie (Multiple Attacks)
```
GameObject with:
- Zombie component
- MeleeZombieAttack component (attackType = 1)
- RangedZombieAttack component (attackType = 2)
(Automatically switches between attacks based on distance)
```

## Animation Setup

To set up animations for different attack types:

1. **Create Animator Parameter**: Add an integer parameter named `AttackType`
2. **Create Attack Animations**: Create separate animations for each attack type
3. **Set Up Transitions**: Create transitions from idle to attack animations based on `AttackType` values
4. **Configure Animator Events**: Add events to attack animations:
   - `AttackWarning`: Called before damage dealing
   - `Attack`: Called when damage should be dealt
   - `AttackEnd`: Called when attack animation ends

The system automatically sets `AttackType` to the appropriate value when an attack starts and resets it to 0 when the attack ends.

## Benefits

1. **Flexibility**: Mix and match attack types without complex inheritance
2. **Maintainability**: Attack logic is isolated in components
3. **Reusability**: Attack components can be shared across different zombie types
4. **Extensibility**: Easy to add new attack types
5. **Backward Compatibility**: Old zombie classes still work

## Attack Component Features

### Common Features (ZombieAttackBase)
- Range validation with NavMesh obstacle consideration
- Cooldown management
- Elemental damage support
- Effect system integration
- Animation parameter management

### MeleeZombieAttack
- Area damage in radius
- Building-specific attack ranges
- Min/max distance validation
- Angle threshold checking

### RangedZombieAttack
- Projectile spawning and management
- Target position prediction
- Min/max range validation
- Vomit projectile effects

### LaserZombieAttack
- Continuous damage application
- Head IK targeting
- Precise angle validation
- Laser beam visual management

### BoomerZombieAttack
- Explosion on attack or death
- Area damage with high poise damage
- Configurable explosion triggers
- Force explosion capability

## Configuration Options

### Zombie Settings
- **Use Modular Attacks**: Enable/disable the modular attack system
- **Selection Strategy**: How to choose between available attacks
- **Attack Switch Cooldown**: Minimum time between attack switches

### Attack Component Settings
- **Range**: Maximum attack distance
- **Cooldown**: Time between attacks
- **Damage**: Base damage amount
- **Poise Damage**: Poise damage amount
- **Attack Type**: Integer parameter for animation selection (0=default, 1=melee, 2=ranged, 3=laser, 4=boomer)
- **Elemental Type**: Damage type (Physical, Fire, Poison, etc.)
- **Elemental Bonus**: Additional elemental damage
- **Effects**: Start, attack, hit, and end effects
- **Animation**: Trigger names and parameters

## Debugging

### Gizmos
Attack components provide visual debugging:
- Attack ranges (colored spheres)
- Attack angles (colored rays)
- Special indicators (laser fire points, explosion centers)

### Logging
Comprehensive logging for:
- Attack initialization
- Attack selection
- Damage dealing
- State changes

## Performance Considerations

- Attack components are only updated when the zombie has a target
- Damage calculations are optimized with early returns
- Effect playing is managed efficiently
- NavMesh queries are cached where possible

## Future Enhancements

Potential additions to the system:
- Combo attacks (chaining multiple attack types)
- Conditional attacks (based on health, target type, etc.)
- Attack modifiers (damage multipliers, range extensions)
- AI behavior integration (fleeing, grouping, etc.)
- Animation blending between attack types
