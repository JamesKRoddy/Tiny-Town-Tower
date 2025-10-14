# Hit Reaction System - Quick Start Guide

## What You Just Added

A **stateless utility** for procedural hit reactions using Unity's IK system. When a character is hit:
- Their head recoils away from the impact
- Their hands move defensively toward the hit
- All reactions are smooth and natural-looking
- No components, no state management - just simple utility calls!

## Already Integrated!

The system is **automatically active** for:
- ✅ All humanoid enemies (`EnemyBase` and derived classes)
- ✅ All humanoid NPCs (`HumanCharacterController` and derived classes)

**You don't need to do anything** - it just works! Hit reactions will trigger automatically when these characters take damage.

## Testing It Out

### In Editor:
1. Start a game session with enemies
2. Attack a humanoid enemy with a weapon
3. Watch for subtle IK reactions when they're hit
4. The enemy's hands and head will react based on where the hit came from

## How It Works

Even simpler - it's **built into `IDamageable`**! The interface provides:

```csharp
public interface IDamageable
{
    // ... existing properties ...
    
    // Hit reaction tracking (for procedural IK reactions)
    Vector3 LastHitOrigin { get; set; }
    float LastHitTime { get; set; }
}
```

So any `IDamageable` character automatically has:

```csharp
// When taking damage:
public void TakeDamage(float amount, Transform damageSource)
{
    // ... apply damage ...
    
    if (damageSource != null)
    {
        LastHitOrigin = damageSource.position;  // From IDamageable
        LastHitTime = Time.time;                // From IDamageable
    }
}

// In OnAnimatorIK:
private void OnAnimatorIK(int layerIndex)
{
    if (animator.isHuman && LastHitOrigin != Vector3.zero)
    {
        IKReactionUtils.ApplyHitReactionIK(animator, transform, LastHitOrigin, LastHitTime);
    }
}
```

That's it! The utility automatically handles:
- Time-based reaction curves
- IK weight calculations
- Body part selection based on hit location
- Automatic fade-out after 0.3 seconds

## Adjusting Reactions

### In Code:
```csharp
// Stronger reaction (0.8 intensity, 0.5s duration)
IKReactionUtils.ApplyHitReactionIK(
    animator, transform, lastHitOrigin, lastHitTime,
    reactionDuration: 0.5f,
    reactionIntensity: 0.8f
);

// Subtle reaction
IKReactionUtils.ApplyHitReactionIK(
    animator, transform, lastHitOrigin, lastHitTime,
    reactionDuration: 0.2f,
    reactionIntensity: 0.3f
);
```

### Constants (Edit in `IKReactionUtils.cs`):
```csharp
DEFAULT_REACTION_DURATION = 0.3f    // How long reactions last
DEFAULT_REACTION_INTENSITY = 0.5f   // Strength (0-1)
DEFAULT_MAX_IK_OFFSET = 0.15f       // Max body part movement
HEAD_IK_WEIGHT = 0.7f               // Head reaction strength
HAND_IK_WEIGHT = 0.5f               // Hand reaction strength
```

## When Reactions DON'T Trigger

Reactions are automatically skipped when:
- Character is dead
- Character's poise is broken (implemented in character classes)
- More than 0.3s since last hit (automatic timeout)
- Character is not humanoid (no IK available)

## Common Issues

**"I don't see any reactions"**
- Check that your character has a Humanoid animator (not Generic)
- Make sure IK Pass is enabled in the Animator component
- Verify damage source is not null when calling TakeDamage
- Enable Debug Gizmos to see if reactions are triggering

**"Reactions are too subtle"**
- Increase "Reaction Intensity" to 0.7-0.8
- Increase "Max IK Offset" to 0.2-0.25
- Make sure you're hitting the character from the side/front (back hits may be less visible)

**"Character position drifts"**
- Reduce "Body Shift Amount" to 0.02 or lower
- This is the backward "push" from hits

## Files Added

- `Assets/Scripts/Utils/HitReactionController.cs` - Main system
- `Assets/Scripts/Utils/HitReactionSystem_README.md` - Detailed documentation
- `Assets/Scripts/Utils/HitReactionExample.cs` - Example/test script
- `Assets/Scripts/HitReactionQuickStart.md` - This file

## Next Steps

1. **Test it**: Hit some enemies and see the reactions
2. **Tune it**: Adjust intensity/offsets to match your game's feel
3. **Extend it**: Use the API to create custom reactions for special situations

For detailed technical documentation, see: `Assets/Scripts/Utils/HitReactionSystem_README.md`

Enjoy more responsive combat! 🎮✨

