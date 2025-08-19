# 2D Blend Tree Damage Animation System

This system uses Unity's 2D blend tree functionality to create smooth, directional damage animations based on hit direction vectors. This provides natural and flexible animation blending for realistic character reactions.

## Overview

The system uses a 2D blend tree approach for optimal animation quality:

- **`Damaged` trigger** - Activates the damage animation
- **`HitDirectionX` float parameter** - Left/Right component (-1 to 1)
- **`HitDirectionY` float parameter** - Forward/Back component (-1 to 1)
- **Vector2 hit direction** - Smooth blending between any hit direction

## 2D Blend Tree Setup

### 1. Required Parameters

Add these parameters to your character's Animator Controller:

- **`Damaged`** - Trigger parameter
- **`HitDirectionX`** - Float parameter (range: -1 to 1)
- **`HitDirectionY`** - Float parameter (range: -1 to 1)

### 2. 2D Blend Tree Configuration

Create a 2D blend tree for damage animations:

1. **Right-click in Animator → Create Motion → Blend Tree**
2. **Name it**: "DamageBlendTree2D"
3. **Blend Type**: 2D
4. **Parameters**: 
   - **X**: HitDirectionX
   - **Y**: HitDirectionY
5. **Motion**: Add your damage animations at key positions

### 3. Animation Mapping

Map your animations to the 2D blend tree:

- **HitDirectionX = 1, HitDirectionY = 1**: Front-Right damage
- **HitDirectionX = 0, HitDirectionY = 1**: Front damage
- **HitDirectionX = -1, HitDirectionY = 1**: Front-Left damage
- **HitDirectionX = 1, HitDirectionY = 0**: Right damage
- **HitDirectionX = 0, HitDirectionY = 0**: Side damage
- **HitDirectionX = -1, HitDirectionY = 0**: Left damage
- **HitDirectionX = 1, HitDirectionY = -1**: Back-Right damage
- **HitDirectionX = 0, HitDirectionY = -1**: Back damage
- **HitDirectionX = -1, HitDirectionY = -1**: Back-Left damage

### 4. State Machine Setup

1. **Create a `Damaged` state**
2. **Assign the 2D blend tree** to this state
3. **Add transitions** from your base states (Idle, Walking, etc.) to `Damaged`
4. **Set transition conditions** to use the `Damaged` trigger

## Animation Values

The system automatically calculates hit direction as a Vector2:

- **X Component (Left/Right)**:
  - **1.0**: Damage from the right
  - **0.0**: Damage from center (front/back)
  - **-1.0**: Damage from the left

- **Y Component (Forward/Back)**:
  - **1.0**: Damage from the front
  - **0.0**: Damage from the side
  - **-1.0**: Damage from the back

## Usage Examples

### Automatic Direction Detection
```csharp
// Damage direction automatically calculated as Vector2
Vector2 hitDirection = DamageUtils.ApplyDamage(character, 10f, enemyTransform, animator, transform);
```

### Explicit Direction Control
```csharp
// Force specific 2D hit direction
Vector2 customDirection = new Vector2(0.5f, -0.8f); // Right-Back hit
DamageUtils.ApplyDamageWithDirection(character, 10f, customDirection, null, animator, transform);
```

### Custom Direction Calculation
```csharp
// Calculate 2D direction manually if needed
Vector2 hitDirection = DamageUtils.CalculateHitDirection(transform, damageSource);
DamageUtils.TriggerDamagedAnimation(animator, hitDirection);
```

## Benefits of 2D Blend Tree Approach

1. **Natural Blending**: Animations blend smoothly between any hit direction
2. **8-Direction Support**: Can handle diagonal hits naturally
3. **Smooth Interpolation**: No discrete jumps between animation states
4. **More Realistic**: Characters react naturally to hits from any angle
5. **Flexible Animation**: Easy to add new hit directions without code changes
6. **Better Performance**: Single blend tree handles all directions

## Advanced Configuration

### Custom Blend Tree Shapes

You can create different blend tree shapes for different animation styles:

- **Square Grid**: 9 animations in a 3x3 grid (most flexible)
- **Diamond**: 5 animations in a diamond pattern (good balance)
- **Triangle**: 3 animations for front/back/side (simplest)

### Animation Weight Curves

Customize how animations blend:

- **Linear**: Smooth, predictable blending
- **Curved**: More natural, organic transitions
- **Custom**: Fine-tune blending for specific animations

### Multiple Animation Layers

For complex setups:
- **Base Layer**: Movement and idle animations
- **Damage Layer**: Override damage animations with 2D blend tree
- **Upper Body Layer**: Upper body damage reactions

## Troubleshooting

### 2D Blend Tree Issues

1. **Animations Not Blending**: Check that both X and Y parameters are set
2. **Wrong Animation Direction**: Verify parameter ranges match blend tree setup
3. **Poor Blending**: Ensure animations are positioned correctly in 2D space

### General Issues

1. **Animations Not Playing**: Check that `Damaged` trigger exists
2. **Wrong Direction**: Verify character's forward/right directions are correct
3. **Poor Performance**: Consider reducing blend tree complexity

## Migration from Old System

### From HitFront/HitBack System
1. Remove old triggers (`HitFront`, `HitBack`)
2. Add new parameters (`Damaged`, `HitDirectionX`, `HitDirectionY`)
3. Set up 2D blend tree with existing animation clips
4. Update code to use new 2D methods

### From 1D Blend Tree
1. Add `HitDirectionX` parameter
2. Convert 1D blend tree to 2D
3. Position animations in 2D space
4. Update code to use new 2D methods

## Performance Considerations

- **2D Blend Trees**: Optimal balance of flexibility and performance
- **Animation Count**: More animations = more memory but better quality
- **Blend Complexity**: Complex curves may impact performance
- **Parameter Updates**: Only two float parameters to update per frame

The 2D system provides the best balance of flexibility and performance for most use cases, with smooth animation blending and natural character reactions.
