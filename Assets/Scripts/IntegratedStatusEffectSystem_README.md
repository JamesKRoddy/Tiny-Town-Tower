# Integrated Status Effect System

A comprehensive status effect system that integrates with the existing EffectManager for unified visual and gameplay effects.

## Overview

This system extends the existing `EffectManager` to handle status effects like being on fire, sick, tired, hungry, etc. It leverages the current effect architecture (`EffectDefinition`, `CharacterEffects`) to provide:

- **Unified Effect Pipeline**: All effects (combat, environmental, status) use the same system
- **Performance Optimized**: Shared object pooling and resource management
- **Designer Friendly**: Uses existing ScriptableObject workflow
- **Extensible**: Easy to add new status effects and behaviors

## Architecture Integration

### Existing Components (Extended)
- **`EffectManager`**: Now handles status effects alongside combat effects
- **`CharacterEffects`**: Extended with `StatusEffectDefinition[]` for character-specific status effects
- **`EffectDefinition`**: Used for visual effects of status conditions

### New Components
- **`StatusEffectTypes.cs`**: Defines all status effect types and interfaces
- **`StatusEffectDefinition.cs`**: Configuration for individual status effects
- **`IStatusEffectTarget`**: Interface for characters that can receive status effects

## Status Effect Types

### Health States
- `HEALTHY`, `HUNGRY`, `STARVING`
- `TIRED`, `EXHAUSTED`, `SICK`
- `POISONED`, `BLEEDING`, `HEALING`

### Environmental Effects
- `ON_FIRE`, `FROZEN`, `ELECTROCUTED`
- `WET`, `BURNING`, `SHOCKED`

### Task States
- `SLEEPING`, `WORKING`, `EATING`
- `FIGHTING`, `FLEEING`, `RESTING`

### Buff/Debuff Effects
- `BUFFED`, `DEBUFFED`, `SLOWED`, `HASTENED`
- `STRENGTHENED`, `WEAKENED`, `PROTECTED`

### Special Effects
- `INVISIBLE`, `SHIELDED`, `STUNNED`
- `CONFUSED`, `MIND_CONTROLLED`, `CHARMED`

## Setup Instructions

### Step 1: Configure Character Effects
1. Open your existing `CharacterEffects` ScriptableObject (or create new one)
2. In the "Status Effects" section, configure status effect definitions
3. For each status effect:
   - Set the `Status Type`
   - Assign a `Visual Effect` (EffectDefinition)
   - Configure appearance settings (icon, tints, etc.)
   - Set gameplay effects (movement speed, damage over time, etc.)

### Step 2: Create Visual Effect Definitions
For each status effect, create `EffectDefinition` assets:
1. Right-click → Create → Scriptable Objects → Effects → Effect Definition
2. Assign particle prefabs and sounds
3. Configure duration and playback settings

### Step 3: Character Integration
Characters implementing `IStatusEffectTarget` automatically work with the system:
- `SettlerNPC` already implements this interface
- For enemies, add the interface to enemy classes

## Usage Examples

### Applying Status Effects
```csharp
// Apply temporary fire effect
EffectManager.Instance.ApplyStatusEffect(target, StatusEffectType.ON_FIRE, 10f);

// Apply permanent sick status (until cured)
EffectManager.Instance.ApplyStatusEffect(target, StatusEffectType.SICK);

// Using convenience methods on SettlerNPC
settlernpc.ApplyStatusEffect(StatusEffectType.FROZEN, 5f);
```

### Checking Status Effects
```csharp
// Check if character has effect
bool isOnFire = EffectManager.Instance.HasStatusEffect(target, StatusEffectType.ON_FIRE);

// Using convenience method
bool isSick = settlernpc.HasStatusEffect(StatusEffectType.SICK);
```

### Removing Status Effects
```csharp
// Remove specific effect
EffectManager.Instance.RemoveStatusEffect(target, StatusEffectType.FROZEN);

// Remove all effects from character
EffectManager.Instance.RemoveAllStatusEffects(target);
```

## Configuration Options

### StatusEffectDefinition Properties

#### Visual Configuration
- **Visual Effect**: EffectDefinition for particles/sounds
- **Icon Sprite**: UI icon displayed above character
- **Character Tint**: Color tint applied to character materials
- **Height Offset**: Position offset for effects/icons
- **Floating Text**: Text displayed when effect is applied

#### Gameplay Effects
- **Movement Speed Multiplier**: Affects character movement speed
- **Animation Speed Multiplier**: Affects animation playback speed
- **Prevent Actions**: Blocks character actions (stun effect)
- **Damage Per Second**: Continuous damage while effect is active
- **Healing Per Second**: Continuous healing while effect is active

#### Behavior Settings
- **Priority**: Display priority when multiple effects are active
- **Behavior**: How effect behaves when applied to character that already has it
  - `REPLACE_EXISTING`: Remove old, apply new
  - `STACK`: Allow multiple instances
  - `REFRESH_DURATION`: Reset timer of existing effect
  - `IGNORE_IF_EXISTS`: Don't apply if already active
- **Default Duration**: How long effect lasts (0 = permanent)

## Automatic Status Updates

### SettlerNPC Integration
The system automatically applies status effects based on character state:

- **Hunger**: `HUNGRY` → `STARVING` based on hunger levels
- **Stamina**: `TIRED` → `EXHAUSTED` based on stamina levels
- **Health**: `SICK` when character becomes ill
- **Tasks**: `WORKING`, `SLEEPING`, `EATING` based on current activity
- **Medical**: `RECEIVING_MEDICAL_TREATMENT` when getting treated

### Manual Status Application
External systems can apply status effects:
```csharp
// Environmental damage
if (inFireZone)
{
    EffectManager.Instance.ApplyStatusEffect(character, StatusEffectType.ON_FIRE, 5f);
}

// Spell effects
if (castFreezeSpell)
{
    EffectManager.Instance.ApplyStatusEffect(target, StatusEffectType.FROZEN, spellDuration);
}
```

## Performance Features

### Shared Object Pooling
- Status effect visuals use the same pooling system as combat effects
- Automatic cleanup when effects expire
- Memory efficient for large numbers of NPCs

### Smart Updates
- Status effects only update when character state changes
- Visual effects follow characters automatically
- Material changes are cached and restored

### Priority System
- Higher priority effects display more prominently
- Emergency effects (fire) override low-priority effects (working)
- Configurable priority levels for all effects

## Extending the System

### Adding New Status Effects
1. Add new enum value to `StatusEffectType`
2. Create `StatusEffectDefinition` configuration
3. Add to appropriate `CharacterEffects` asset
4. Optionally add custom logic in `IStatusEffectTarget.OnStatusEffectApplied`

### Custom Character Integration
```csharp
public class EnemyController : MonoBehaviour, IStatusEffectTarget
{
    public CharacterType GetCharacterType() => CharacterType.ENEMY;
    
    public void OnStatusEffectApplied(StatusEffectType effectType, float duration)
    {
        switch (effectType)
        {
            case StatusEffectType.FROZEN:
                // Stop enemy AI
                GetComponent<EnemyAI>().enabled = false;
                break;
            case StatusEffectType.ON_FIRE:
                // Enemy-specific fire behavior
                StartFirePanic();
                break;
        }
    }
    
    public void OnStatusEffectRemoved(StatusEffectType effectType)
    {
        // Cleanup logic
    }
}
```

### Creating Effect Definitions
1. Create particle systems for visual effects
2. Create `EffectDefinition` ScriptableObjects
3. Configure in `StatusEffectDefinition`
4. Assign to `CharacterEffects`

## Troubleshooting

### Effects Not Appearing
1. Check if `CharacterEffects` asset is assigned to `EffectManager`
2. Verify status effect definition exists for character type
3. Ensure visual effect prefabs are properly configured
4. Check console for missing references

### Performance Issues
1. Reduce particle effect complexity
2. Limit number of simultaneous status effects
3. Use lower priority for background effects
4. Optimize effect durations

### Status Effects Not Applying
1. Verify character implements `IStatusEffectTarget`
2. Check if `EffectManager.Instance` is available
3. Ensure character type matches configuration
4. Check effect behavior settings (might be set to ignore)

## Best Practices

### Effect Design
- Use **particles** for dramatic effects (fire, electricity)
- Use **icons** for status information (hungry, tired)
- Use **tints** for subtle state changes (sick, buffed)
- Combine multiple visual types for important effects

### Performance
- Set appropriate durations for temporary effects
- Use priority system to limit simultaneous effects
- Pool particle effects when possible
- Avoid complex material modifications

### Gameplay Balance
- Consider effect stacking and interactions
- Test movement speed modifications carefully
- Balance damage-over-time effects
- Provide clear visual feedback for all effects

This integrated system provides a robust foundation for all character status effects while maintaining consistency with the existing effect architecture.
