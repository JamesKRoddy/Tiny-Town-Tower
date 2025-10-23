# AttackBase EffectSpawnData Migration

## Overview
Successfully migrated `AttackBase` and all inheriting attack classes from using `EffectDefinition` to the new `EffectSpawnData` system. This provides much more flexibility in positioning and configuring attack effects.

## Changes Made

### 1. AttackBase.cs - Core Changes

#### Field Updates
**Before:**
```csharp
public EffectDefinition startEffect;
public float startEffectDelay = 0f;
public EffectDefinition attackEffect;
public float attackEffectDelay = 0f;
public EffectDefinition hitEffect;
public float hitEffectDelay = 0f;
public EffectDefinition endEffect;
public float endEffectDelay = 0f;
```

**After:**
```csharp
public EffectSpawnData startEffect;
public float startEffectDelay = 0f;
public EffectSpawnData attackEffect;
public float attackEffectDelay = 0f;
public EffectSpawnData hitEffect;
public float hitEffectDelay = 0f;
public EffectSpawnData endEffect;
public float endEffectDelay = 0f;
```

#### Method Updates

**New Helper Methods:**
```csharp
protected virtual void PlayEffectSpawnData(EffectSpawnData effectData, float delay, Transform fallbackTransform)
private IEnumerator PlayEffectSpawnDataDelayed(EffectSpawnData effectData, float delay, Transform fallbackTransform)
```

**Updated Play Methods:**
All `PlayStartEffect()`, `PlayAttackEffect()`, `PlayHitEffect()`, and `PlayEndEffect()` methods now:
- Check `effectData.IsValid()` before spawning
- Use `effectData.SpawnEffect(fallbackTransform)` with `attackOrigin` as fallback
- Handle delays properly with coroutines

### 2. Inheriting Attack Classes Updated

#### ProjectileAttack.cs
- Updated validation check: `if (attackEffect == null || !attackEffect.IsValid())`
- Updated `CalculateDamageRadius()` to access `hitEffect.effectDefinition.prefabs`

#### ExplosionAttack.cs
- Updated validation check: `if (attackEffect == null || !attackEffect.IsValid())`

## Benefits

### 1. Flexible Effect Positioning
Attack effects can now:
- Spawn at custom Transform locations (e.g., weapon tip, hand, mouth)
- Use position/rotation offsets without code changes
- Be parented to specific transforms (follow the attacker)

### 2. Per-Attack Customization
Each attack instance can have unique effect spawn configurations:
```csharp
// Example: Projectile from mouth
projectileAttack.attackEffect.spawnPoint = enemyMouthTransform;
projectileAttack.attackEffect.positionOffset = Vector3.forward * 0.5f;

// Example: Ground slam effect
slamAttack.hitEffect.spawnPoint = null; // Use attackOrigin
slamAttack.hitEffect.positionOffset = Vector3.down * 0.5f;
slamAttack.hitEffect.parentToTransform = false;
```

### 3. Artist-Friendly Workflow
VFX artists can:
- Create child transforms to position effects visually
- Adjust offsets in the inspector without touching code
- Preview effect placement with scene gizmos
- Test different spawn configurations easily

### 4. Backward Compatible
The migration maintains compatibility:
- Delay system still works the same way
- All existing attack behaviors preserved
- No breaking changes to attack logic
- Falls back to `attackOrigin` if no spawn point assigned

## Inspector Setup Guide

### Basic Setup (No Custom Spawn Point)
1. Assign an `EffectDefinition` to `effectDefinition` field
2. Leave `spawnPoint` null (will use `attackOrigin`)
3. Effect spawns at attack origin position

### Advanced Setup (Custom Spawn Point)
1. Create child transform on enemy (e.g., "MuzzlePoint", "WeaponTip")
2. Position it where you want the effect to spawn
3. Assign this transform to `spawnPoint` in the effect data
4. Set `positionOffset` for fine-tuning (local space)
5. Set `rotationOffset` if needed (euler angles)
6. Toggle `parentToTransform` if effect should follow the enemy

### Duration Override
- Leave at 0 to use effect definition's duration
- Set to custom value to override per-attack

## Migration Checklist for Custom Attacks

If you have custom attack scripts that inherit from `AttackBase`:

- [ ] Update validation checks from `effect == null` to `effect == null || !effect.IsValid()`
- [ ] Update direct effect property access from `effect.prefabs` to `effect.effectDefinition.prefabs`
- [ ] Update direct effect property access from `effect.duration` to `effect.effectDefinition.duration`
- [ ] Test all effect spawning in-game
- [ ] Verify effect positioning is correct
- [ ] Check that delays still work as expected

## Attack Classes Verified

✅ **AttackBase.cs** - Base class updated
✅ **ProjectileAttack.cs** - Validation and prefab access updated
✅ **ExplosionAttack.cs** - Validation updated
✅ **BeamAttack.cs** - No changes needed (uses base methods)
✅ **ShockwaveAttack.cs** - No changes needed (uses base methods)
✅ **JumpAttack.cs** - No changes needed (uses base methods)
✅ **AreaOfEffectAttack.cs** - No changes needed (uses base methods)
✅ **AnimationAttack.cs** - No changes needed (uses base methods)

## Common Patterns

### Pattern 1: Weapon Tip Effect
```csharp
// In inspector:
// spawnPoint = WeaponTip (child transform)
// positionOffset = (0, 0, 0.2) // Slightly in front
// rotationOffset = (0, 0, 0)
// parentToTransform = false
```

### Pattern 2: Mouth/Head Effect
```csharp
// In inspector:
// spawnPoint = HeadBone (child transform)
// positionOffset = (0, 0, 0.5) // In front of mouth
// rotationOffset = (0, 0, 0)
// parentToTransform = false
```

### Pattern 3: Ground Impact
```csharp
// In inspector:
// spawnPoint = null (use attackOrigin)
// positionOffset = (0, -0.5, 0) // Below ground
// rotationOffset = (0, 0, 0)
// parentToTransform = false
```

### Pattern 4: Continuous Effect (Follows Enemy)
```csharp
// In inspector:
// spawnPoint = EffectAnchor (child transform)
// positionOffset = (0, 1, 0) // Above enemy
// rotationOffset = (0, 0, 0)
// parentToTransform = true // Follows the enemy
```

## Testing Recommendations

1. **Visual Verification**: Check that all attack effects spawn at correct positions
2. **Rotation Testing**: Verify effects are oriented correctly
3. **Parenting Testing**: Confirm parented effects follow the enemy properly
4. **Fallback Testing**: Test that null spawn points fall back to attackOrigin
5. **Delay Testing**: Ensure effect delays still work as expected
6. **Multi-Attack Testing**: Test enemies with multiple attack types

## Performance Notes

- No performance impact - still uses EffectManager's pooling system
- Same number of GameObject instantiations
- Minimal overhead from position/rotation calculations
- Spawn point transforms are cached references

## Troubleshooting

### Effect spawns at wrong location
- Check if `spawnPoint` is assigned correctly
- Verify `positionOffset` is in local space (not world space)
- Ensure `attackOrigin` is set properly on AttackBase

### Effect not spawning
- Check `effectDefinition` is assigned in EffectSpawnData
- Call `IsValid()` to verify configuration
- Check EffectManager is in the scene

### Effect has wrong rotation
- Verify `rotationOffset` values (euler angles)
- Check parent transform's rotation
- Ensure spawn point transform is oriented correctly

### Effect doesn't follow enemy
- Set `parentToTransform = true`
- Assign a child transform as spawn point
- Verify the parent transform exists during effect lifetime

## Future Enhancements

Potential additions to EffectSpawnData for attacks:
- Scale override per attack
- Multiple spawn points (spawn at all points)
- Random offset within radius
- Surface alignment (raycast to ground)
- Dynamic target tracking (effect follows target)

## Files Modified

1. `Assets/Scripts/ScriptableObjects/EffectSpawnData.cs` - New class (already created)
2. `Assets/Scripts/Characters/Enemies/AttackBase.cs` - Updated to use EffectSpawnData
3. `Assets/Scripts/Characters/Enemies/Attacks/ProjectileAttack.cs` - Updated validation and access
4. `Assets/Scripts/Characters/Enemies/Attacks/ExplosionAttack.cs` - Updated validation

## Related Documentation

- `EffectSpawnData_README.md` - Complete EffectSpawnData documentation
- `AlarmBuilding_README.md` - Example implementation in AlarmBuilding
- `AttackBase.cs` - Full attack system documentation (inline comments)


