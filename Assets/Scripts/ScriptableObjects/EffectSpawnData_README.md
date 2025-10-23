# EffectSpawnData System

## Overview
`EffectSpawnData` is a flexible wrapper class that combines an `EffectDefinition` with spawn configuration (position, rotation, parenting, and offsets). This class can be used throughout the codebase to provide precise control over where and how effects are spawned.

## Purpose
Before `EffectSpawnData`, effects could only be spawned at the position of the calling object. This new system allows:
- Custom spawn points via Transform references
- Local position and rotation offsets
- Optional parenting to specific transforms
- Duration overrides per instance
- Fallback to calling object's transform if no spawn point specified

## Class Structure

### Fields

```csharp
public EffectDefinition effectDefinition;  // The effect to spawn
public Transform spawnPoint;                // Optional spawn location
public Vector3 positionOffset;              // Local position offset
public Vector3 rotationOffset;              // Local rotation offset (euler angles)
public bool parentToTransform;              // Should effect be parented?
public float durationOverride;              // Optional duration override
```

### Key Methods

#### `bool IsValid()`
Returns true if the effect definition is assigned.

#### `Vector3 GetSpawnPosition(Transform fallbackTransform)`
Returns the world position where the effect should spawn.
- Uses `spawnPoint` if assigned, otherwise uses `fallbackTransform`
- Applies `positionOffset` in local space

#### `Quaternion GetSpawnRotation(Transform fallbackTransform)`
Returns the world rotation for the effect.
- Uses `spawnPoint` if assigned, otherwise uses `fallbackTransform`
- Applies `rotationOffset` as local euler angles

#### `Transform GetParentTransform(Transform fallbackTransform)`
Returns the parent transform for the effect (or null if not parenting).
- Returns null if `parentToTransform` is false
- Uses `spawnPoint` if assigned and parenting, otherwise uses `fallbackTransform`

#### `float GetEffectDuration()`
Returns the effective duration (override if set, otherwise effect definition's duration).

#### `GameObject SpawnEffect(Transform fallbackTransform)`
Spawns the effect using EffectManager with all configured settings.
- Returns the spawned GameObject, or null if failed
- Handles all the configuration automatically

## Usage Examples

### Example 1: Basic Usage (No Spawn Point)
```csharp
[SerializeField] private EffectSpawnData explosionEffect;

void Explode()
{
    // Spawns at this GameObject's position with default rotation
    explosionEffect.SpawnEffect(transform);
}
```

### Example 2: With Custom Spawn Point
```csharp
[SerializeField] private EffectSpawnData muzzleFlashEffect;

void FireWeapon()
{
    // Set up in inspector:
    // - effectDefinition = MuzzleFlash
    // - spawnPoint = WeaponTip transform
    // - parentToTransform = true
    
    muzzleFlashEffect.SpawnEffect(transform);
    // Effect spawns at WeaponTip's position/rotation and is parented to it
}
```

### Example 3: With Offsets
```csharp
[SerializeField] private EffectSpawnData footstepEffect;

void OnFootstep()
{
    // Set up in inspector:
    // - effectDefinition = Dust
    // - positionOffset = (0, -0.1, 0) // Slightly below feet
    // - rotationOffset = (0, 0, 0)
    
    footstepEffect.SpawnEffect(transform);
}
```

### Example 4: AlarmBuilding Implementation
```csharp
public class AlarmBuilding : Building
{
    [SerializeField] private EffectSpawnData alarmEffect;
    
    private void PlayAlarmEffect()
    {
        if (alarmEffect != null && alarmEffect.IsValid())
        {
            // Spawns at configured spawn point, or defaults to building transform
            GameObject effect = alarmEffect.SpawnEffect(transform);
        }
    }
}
```

## Inspector Setup

1. **Assign Effect Definition**: Drag an EffectDefinition ScriptableObject to the `effectDefinition` field

2. **Configure Spawn Point** (Optional):
   - Leave null to use the calling object's transform
   - Assign a child transform for precise positioning
   - Common use: create an empty GameObject child named "EffectSpawnPoint"

3. **Set Position Offset**: Local space offset from the spawn point
   - Example: (0, 2, 0) spawns 2 meters above the spawn point

4. **Set Rotation Offset**: Local euler angle offset
   - Example: (0, 90, 0) rotates 90 degrees around Y axis

5. **Parent to Transform**: Check to make effect follow the transform
   - Useful for effects that should move with the object (e.g., status effects)
   - Uncheck for world-space effects (e.g., explosions, ground impacts)

6. **Duration Override**: Set to override the effect definition's duration
   - Leave at 0 to use the effect definition's duration

## Benefits

### 1. Flexibility
- One effect definition can be used in multiple contexts with different spawn configurations
- Easy to adjust effect placement without changing code

### 2. Artist-Friendly
- Artists/designers can position effects visually in the scene
- No need to ask programmers to adjust hardcoded offsets

### 3. Reusability
- Same effect definition can spawn at different locations
- Configuration is per-instance, not per-effect

### 4. Consistency
- Standard way to handle effect spawning across the entire codebase
- All effects benefit from EffectManager's pooling system

### 5. Debug Visualization
- AlarmBuilding example shows how to visualize spawn points with Gizmos
- Easy to see exactly where effects will spawn in the editor

## Migration Guide

### Old Pattern
```csharp
[SerializeField] private EffectDefinition effect;
[SerializeField] private Transform spawnPoint;

void PlayEffect()
{
    Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
    EffectManager.Instance.PlayEffect(pos, Vector3.up, Quaternion.identity, null, effect);
}
```

### New Pattern
```csharp
[SerializeField] private EffectSpawnData effect;

void PlayEffect()
{
    effect.SpawnEffect(transform);
}
```

## Best Practices

1. **Always check IsValid()** before spawning if the effect might not be assigned

2. **Use descriptive spawn point names** in the hierarchy:
   - "MuzzleFlashPoint"
   - "FootstepEffectPoint"
   - "StatusEffectAnchor"

3. **Leverage local offsets** instead of moving spawn point transforms
   - Makes it easier to adjust effects without affecting other systems

4. **Use parenting wisely**:
   - Parent for persistent effects (burning, sleeping icons)
   - Don't parent for one-shot effects (explosions, impacts)

5. **Consider duration overrides** when the same effect needs different durations in different contexts

## Integration with Existing Systems

### EffectManager
`EffectSpawnData` uses `EffectManager.Instance.PlayEffect()` internally, so all pooling and cleanup is handled automatically.

### Gizmos
You can visualize spawn points in your own components:
```csharp
#if UNITY_EDITOR
private void OnDrawGizmosSelected()
{
    if (myEffect != null)
    {
        Vector3 pos = myEffect.GetSpawnPosition(transform);
        Quaternion rot = myEffect.GetSpawnRotation(transform);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos, 0.5f);
        Gizmos.DrawLine(pos, pos + rot * Vector3.forward);
    }
}
#endif
```

## Future Enhancements

Potential additions:
- Scale override
- Layer mask for raycast-based ground positioning
- Multiple spawn points (spawn at all configured points)
- Random position offset (within a radius)
- Delay before spawn
- Auto-rotation to face camera/target

## Files Modified

- `Assets/Scripts/ScriptableObjects/EffectSpawnData.cs` - New class
- `Assets/Scripts/CampBuilding/AlarmBuilding.cs` - First implementation
- `Assets/Scripts/CampBuilding/AlarmBuilding_README.md` - Updated documentation

## Compatibility

This system is **backward compatible**. Existing code using `EffectDefinition` directly continues to work. `EffectSpawnData` is purely additive and optional.


