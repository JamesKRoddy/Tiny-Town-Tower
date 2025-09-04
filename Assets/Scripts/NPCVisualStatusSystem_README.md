# NPC Visual Status System

A comprehensive visual status indicator system for NPCs and enemies that provides flexible, extensible visual feedback for various character states.

## Overview

This system separates visual status management from core NPC logic, making it easy to:
- Add new visual effects for different status types
- Configure visual appearances through ScriptableObjects
- Extend to enemies and other character types
- Handle complex effects like being on fire, electrocuted, sick, tired, etc.

## Core Components

### 1. NPCStatusEffects.cs
Defines all status effect types, visual configuration classes, and interfaces.

**Key Elements:**
- `NPCStatusEffectType` enum: Defines all possible status effects
- `StatusEffectVisualConfig` class: Configuration for each effect's appearance
- `IStatusEffectTarget` interface: For objects that receive status effect notifications

### 2. NPCVisualStatusManager.cs
The main manager that handles applying and removing visual effects.

**Features:**
- Object pooling for performance
- Multiple display types (particles, icons, color tints, etc.)
- Automatic cleanup and state management
- Extensible effect system

### 3. NPCVisualStatusConfig.cs
ScriptableObject for configuring visual appearances of all status effects.

**Features:**
- Designer-friendly configuration
- Default effect creation
- Runtime effect management
- Validation and debugging tools

## Setup Instructions

### Step 1: Create Visual Status Configuration
1. Right-click in Project window
2. Go to Create > Scriptable Objects > NPCs > Visual Status Config
3. Name it "NPCVisualStatusConfig"
4. In the inspector, click "Create Default Configurations" to set up basic effects

### Step 2: Configure Visual Effects
For each status effect in the configuration:
1. Set the `Effect Type`
2. Choose `Display Type`:
   - **PARTICLE_EFFECT**: Spawn particle systems
   - **ICON_OVERLAY**: Show UI icons above character
   - **COLOR_TINT**: Tint character materials
   - **ANIMATION_MODIFIER**: Modify character animations
   - **SOUND_EFFECT**: Play audio cues
   - **FLOATING_TEXT**: Display floating text
   - **COMBINATION**: Multiple effects combined
3. Configure visual elements (prefabs, sprites, colors, sounds)
4. Set priority for effect ordering

### Step 3: Add to NPC Prefabs
The system automatically integrates with SettlerNPC, but for other characters:
1. Add `NPCVisualStatusManager` component to character prefab
2. Assign the Visual Status Config
3. Optionally configure icon/text prefabs and settings

### Step 4: Create Effect Prefabs
Create prefabs for particle effects, icons, and floating text:

#### Particle Effect Prefab
- Create particle system with desired effect
- Set to loop if needed
- Save as prefab in project

#### Icon Prefab
- Create UI Image with Canvas Renderer
- Add to prefab for reuse
- Will be automatically configured by system

#### Floating Text Prefab
- Create TextMeshPro component
- Configure font and basic styling
- Save as prefab

## Usage Examples

### Applying Status Effects
```csharp
// Get the visual status manager
var statusManager = npc.GetVisualStatusManager();

// Apply a temporary effect
statusManager.ApplyStatusEffect(NPCStatusEffectType.ON_FIRE, 10f); // 10 seconds

// Apply permanent effect (until manually removed)
statusManager.ApplyStatusEffect(NPCStatusEffectType.SICK);

// Remove effect
statusManager.RemoveStatusEffect(NPCStatusEffectType.SICK);
```

### Custom Status Effects
```csharp
// Apply custom environmental effect
npc.ApplyVisualStatusEffect(NPCStatusEffectType.ELECTROCUTED, 5f);

// Check if effect is active
bool isOnFire = statusManager.HasStatusEffect(NPCStatusEffectType.ON_FIRE);

// Get all active effects
var activeEffects = statusManager.GetActiveStatusEffects();
```

### Automatic Status Updates
The system automatically updates visual status for SettlerNPC based on:
- Hunger levels (hungry → starving)
- Stamina levels (tired → exhausted)
- Health status (sick, healthy)
- Current tasks (working, sleeping, eating, etc.)

## Status Effect Categories

### Health-Related
- `HEALTHY`, `HUNGRY`, `STARVING`
- `TIRED`, `EXHAUSTED`, `SICK`
- `POISONED`, `BLEEDING`, `HEALING`

### Physical States
- `SLEEPING`, `WORKING`, `EATING`
- `FIGHTING`, `FLEEING`

### Environmental Effects
- `ON_FIRE`, `FROZEN`, `ELECTROCUTED`
- `WET`, `BURNING`, `SHOCKED`

### Buff/Debuff Effects
- `BUFFED`, `DEBUFFED`
- `SLOWED`, `HASTENED`
- `STRENGTHENED`, `WEAKENED`

### Special Effects
- `INVISIBLE`, `SHIELDED`, `STUNNED`
- `CONFUSED`, `MIND_CONTROLLED`

### Medical/Treatment
- `RECEIVING_MEDICAL_TREATMENT`, `QUARANTINED`

## Extending the System

### Adding New Status Effects
1. Add new enum value to `NPCStatusEffectType`
2. Create configuration in NPCVisualStatusConfig
3. Optionally add logic in `IStatusEffectTarget.OnStatusEffectApplied/Removed`

### Creating Custom Display Types
1. Add new enum value to `StatusDisplayType`
2. Implement handling in `NPCVisualStatusManager.ApplyVisualEffect()`
3. Add configuration options to `StatusEffectVisualConfig`

### Using with Enemies
```csharp
public class EnemyController : MonoBehaviour, IStatusEffectTarget
{
    private NPCVisualStatusManager visualStatusManager;
    
    private void Start()
    {
        visualStatusManager = GetComponent<NPCVisualStatusManager>();
        // Set enemy-specific configuration
    }
    
    public void OnStatusEffectApplied(NPCStatusEffectType effectType)
    {
        // Handle enemy-specific status effects
        switch (effectType)
        {
            case NPCStatusEffectType.FROZEN:
                // Slow enemy movement
                break;
            case NPCStatusEffectType.STUNNED:
                // Stop enemy attacks
                break;
        }
    }
    
    public void OnStatusEffectRemoved(NPCStatusEffectType effectType)
    {
        // Handle cleanup
    }
}
```

## Performance Considerations

- **Object Pooling**: Icons and text objects are pooled for performance
- **Material Caching**: Original materials are cached and restored
- **Selective Updates**: Visual updates only occur when status changes
- **Priority System**: Higher priority effects are displayed prominently

## Troubleshooting

### Visual Effects Not Appearing
1. Check if NPCVisualStatusConfig is assigned
2. Verify effect configuration exists for the status type
3. Ensure particle/icon prefabs are assigned
4. Check console for warnings/errors

### Performance Issues
1. Reduce max visible icons if too many status effects
2. Optimize particle effects (lower particle count, simpler shaders)
3. Use ICON_OVERLAY instead of PARTICLE_EFFECT for simple indicators

### Status Effects Not Updating
1. Verify IStatusEffectTarget interface is implemented
2. Check if status changes are calling update methods
3. Ensure NPCVisualStatusManager is active and enabled

## Best Practices

1. **Use Appropriate Display Types**: Icons for simple status, particles for dramatic effects
2. **Set Correct Priorities**: Emergency effects (fire) should have higher priority than low-level effects (working)
3. **Configure Durations**: Use durations for temporary effects, permanent for ongoing states
4. **Test Performance**: Monitor performance with many NPCs and active effects
5. **Use Pooling**: Always use provided object pools for runtime-created effects

## Future Enhancements

Potential areas for expansion:
- Custom shader effects for glow/outline
- Screen-space UI integration
- Sound effect pooling
- Animation state modifications
- Networked multiplayer support
- VR/AR specific indicators
