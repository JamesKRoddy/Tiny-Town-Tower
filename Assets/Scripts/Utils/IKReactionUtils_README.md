# IK Reaction Utils

## Overview

`IKReactionUtils` is a **stateless utility class** for applying procedural hit reactions to humanoid characters using Unity's IK system. It follows the same architecture pattern as `DamageUtils` and `RootMotionUtils` - simple, performant, and easy to integrate.

## Usage

### Automatic Integration

The system is **already integrated** into:
- ✅ `EnemyBase` - All humanoid enemies
- ✅ `HumanCharacterController` - All humanoid NPCs

When these characters take damage, they automatically:
1. Store the hit origin position
2. Record the time of hit
3. Apply IK reactions in `OnAnimatorIK()`

### How It Works

```csharp
// In TakeDamage:
lastHitOrigin = damageSource.position;
lastHitTime = Time.time;

// In OnAnimatorIK:
if (animator.isHuman && lastHitOrigin != Vector3.zero)
{
    IKReactionUtils.ApplyHitReactionIK(animator, transform, lastHitOrigin, lastHitTime);
}
```

That's it! The utility calculates everything based on time elapsed since hit.

## What It Does

When a character is hit, the system:

1. **Analyzes Hit Context**:
   - Hit direction (front/back/left/right)
   - Hit height (head/chest/low)
   - Distance from body parts to hit origin

2. **Applies IK Reactions**:
   - **Head**: Recoils away from hit + upward snap
   - **Hands**: Move defensively toward hit source
   - **Weights**: Smooth sine curve over reaction duration

3. **Auto-Expires**:
   - Reactions automatically fade after 0.3 seconds
   - No state to manage, no coroutines, no cleanup

## API

### Main Method

```csharp
IKReactionUtils.ApplyHitReactionIK(
    Animator animator,              // Character's humanoid animator
    Transform characterTransform,   // Character's transform
    Vector3 lastHitOrigin,         // Where hit came from
    float lastHitTime,             // When hit occurred
    float reactionDuration = 0.3f, // How long reaction lasts
    float reactionIntensity = 0.5f // Strength 0-1
)
```

### Simple Overload

```csharp
// Use defaults (0.3s duration, 0.5 intensity)
IKReactionUtils.ApplyHitReactionIK(animator, transform, lastHitOrigin, lastHitTime);
```

## Configuration

### Default Constants
```csharp
DEFAULT_REACTION_DURATION = 0.3f    // Reaction lasts 0.3 seconds
DEFAULT_REACTION_INTENSITY = 0.5f   // Moderate reaction strength
DEFAULT_MAX_IK_OFFSET = 0.15f       // Max 15cm body part movement
HEAD_IK_WEIGHT = 0.7f               // Strong head reactions
HAND_IK_WEIGHT = 0.5f               // Moderate hand reactions
HIT_DETECTION_RADIUS = 0.8f         // 80cm proximity detection
```

To customize, just pass different values to the method:
```csharp
// Stronger, longer reaction
IKReactionUtils.ApplyHitReactionIK(
    animator, transform, lastHitOrigin, lastHitTime,
    reactionDuration: 0.5f,
    reactionIntensity: 0.8f
);
```

## Integration Example

### Custom Character Class

```csharp
public class MyCharacter : MonoBehaviour, IDamageable
{
    private Animator animator;
    
    // IDamageable provides these automatically:
    public Vector3 LastHitOrigin { get; set; } = Vector3.zero;
    public float LastHitTime { get; set; } = -999f;
    
    public void TakeDamage(float amount, Transform damageSource)
    {
        // ... apply damage ...
        
        // Track hit for reactions
        if (damageSource != null)
        {
            LastHitOrigin = damageSource.position;  // Part of IDamageable
            LastHitTime = Time.time;                // Part of IDamageable
        }
    }
    
    private void OnAnimatorIK(int layerIndex)
    {
        // Apply hit reactions
        if (animator.isHuman && LastHitOrigin != Vector3.zero)
        {
            IKReactionUtils.ApplyHitReactionIK(animator, transform, LastHitOrigin, LastHitTime);
        }
    }
}
```

## Visual Behavior

### When Character is Hit:

**Frame 0 (Hit Occurs)**:
- `lastHitOrigin` and `lastHitTime` are set
- No IK applied yet (happens next frame)

**Frames 1-18 (0.3s @ 60fps)**:
- IK weights smoothly ramp up then down (sine curve)
- Head recoils away from hit
- Hands move defensively toward hit
- Reaction automatically fades out

**Frame 19+ (After 0.3s)**:
- `timeSinceHit > reactionDuration`
- Function returns early, no IK applied
- Character back to normal animations

## Performance

- **Stateless**: Zero heap allocations
- **Fast**: Simple math, no state management
- **Scalable**: Can handle many characters reacting simultaneously
- **Zero overhead**: When not reacting (beyond duration), function exits immediately

## Advantages Over Component Approach

| Feature | Component | Static Utility |
|---------|-----------|----------------|
| Memory per character | ~2KB + coroutines | 2 Vector3 + 1 float (~28 bytes) |
| Update overhead | Every frame when reacting | Only in OnAnimatorIK |
| State management | Complex (coroutines, enums) | None (time-based) |
| Integration | Requires component | Just 2 lines of code |
| Consistency | New pattern | Matches DamageUtils, etc. |

## Requirements

- Humanoid Animator with IK Pass enabled
- `OnAnimatorIK()` callback implemented
- Implement `IDamageable` interface (which provides `LastHitOrigin` and `LastHitTime`)

## Limitations

- **Humanoid Only**: Requires humanoid rig
- **One Reaction at a Time**: New hits during reaction don't stack (by design)
- **Fixed Duration**: Reactions always follow the time curve (but duration is customizable)
- **No Foot IK**: Feet aren't modified to maintain character stability

## Troubleshooting

**No reactions visible**:
- Ensure animator is humanoid (`animator.isHuman == true`)
- Check IK Pass is enabled in Animator component
- Verify `lastHitOrigin != Vector3.zero` in OnAnimatorIK
- Make sure `OnAnimatorIK()` is being called (it won't be for Generic rigs)

**Reactions too subtle**:
- Increase `reactionIntensity` parameter (0.5 → 0.8)
- Increase reaction duration (0.3s → 0.5s)

**Reactions too strong**:
- Decrease `reactionIntensity` parameter (0.5 → 0.3)
- Reduce `DEFAULT_MAX_IK_OFFSET` in the utility class

## Architecture Benefits

This stateless approach:
- ✅ Matches your codebase patterns (DamageUtils, RootMotionUtils)
- ✅ No GameObject overhead
- ✅ No component lifecycle management
- ✅ Simple integration (just 2 variables + 1 function call)
- ✅ Easy to customize per-character
- ✅ Predictable behavior (time-based, not state-based)

---

**Simple, stateless, and consistent with your architecture!** 🎮✨

