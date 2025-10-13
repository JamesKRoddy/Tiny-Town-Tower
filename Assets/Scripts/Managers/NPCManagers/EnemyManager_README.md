# Enemy Group Coordination System

## Overview

The **EnemyManager** system provides intelligent group behavior coordination for enemies, making combat more challenging and engaging. It manages attack timing, positioning, and aggro to create a more tactical combat experience.

## Features

### 1. **Attack Staggering**
- Prevents all enemies from attacking simultaneously
- Configurable maximum simultaneous attackers (default: 2)
- Queue system for waiting enemies
- Stagger delay between attack initiations

**Benefits:**
- Players have a window to react and counter
- Combat feels more fair and skill-based
- Prevents overwhelming burst damage

### 2. **Flanking & Positioning**
- Enemies dynamically coordinate to surround the player
- Maintains minimum angles between enemies
- Assigns tactical positions around targets
- Enemies circle around when waiting to attack
- Personal space system prevents clustering
- Updates positioning periodically

**Benefits:**
- Forces player to be aware of surroundings
- Creates fluid, organic-feeling combat
- Enemies approach from multiple angles
- No awkward standing still or bunching up
- Feels like enemies are actively strategizing

### 3. **Aggro/Threat System**
- Tracks threat levels based on damage dealt
- Threat decays over time
- Can be used for target prioritization
- Threat increases when dealing damage to targets

**Benefits:**
- Enables more strategic targeting
- Can create "tank" gameplay mechanics
- Adds depth to multi-enemy encounters

## Setup Instructions

### 1. Add EnemyManager to Scene

1. Create an empty GameObject in your scene
2. Name it "EnemyManager"
3. Add the `EnemyManager` component to it
4. The singleton will initialize automatically

### 2. Configure Settings

Configure all settings directly in the EnemyManager Inspector:
- Settings are organized into clear sections (Attack, Positioning, Aggro, Debug)
- All changes work in real-time during Play mode
- Values persist when you stop Play mode

#### Key Settings:

**Attack Coordination:**
- `Max Simultaneous Attackers`: How many enemies can attack at once (default: 2)
- `Attack Stagger Delay`: Time between attack starts (default: 0.5s)
- `Post Attack Cooldown`: Delay before next enemy can attack (default: 1.0s)

**Positioning:**
- `Enable Flanking Behavior`: Toggle flanking on/off
- `Min Angle Between Enemies`: Minimum separation angle (default: 45°)
- `Preferred Attack Distance`: Distance to maintain from target (default: 3m)
- `Position Update Interval`: How often to recalculate positions (default: 2s)
- `Enable Circling Behavior`: Toggle circling while waiting to attack
- `Circling Speed`: How fast enemies circle (default: 30°/s)
- `Personal Space Radius`: Minimum distance between enemies (default: 1.5m)

**Aggro/Threat:**
- `Enable Aggro System`: Toggle threat tracking on/off
- `Threat Decay Rate`: How fast threat decreases per second (default: 5)
- `Threat Per Damage`: Threat gained per damage point (default: 10)

### 3. Enable Debug Mode

Set `Show Debug` to true in the Inspector to see coordination logs in the console.

## How It Works

### Attack Coordination Flow

1. Enemy wants to attack (via Zombie.ExecuteAttack())
2. Requests permission from EnemyManager
3. If slots available and stagger time passed → **Granted**
4. If slots full → Added to **queue**
5. When attack completes → Notifies manager → Processes queue

### Flanking System Flow

1. Every `positionUpdateInterval` seconds
2. Groups enemies by their target
3. Calculates circular positions around target
4. Assigns positions with minimum angle separation
5. Enemies move toward assigned positions when not attacking

### Threat System Flow

1. Enemy deals damage via AttackBase
2. Adds threat to target (damage × threatPerDamage)
3. Threat decays over time (threatDecayRate per second)
4. Can query highest threat target

## Integration

The system automatically integrates with existing enemies:

- **EnemyBase**: Auto-registers/unregisters with EnemyManager
- **Zombie**: Requests attack permission before attacking
- **AttackBase**: Adds threat when dealing damage

No changes needed to individual enemy prefabs!

## Performance Considerations

- Position updates are throttled by `positionUpdateInterval`
- Queue processing is coroutine-based
- Automatic cleanup of null references
- O(n) operations for most coordination tasks

## Tuning Tips

### For Easier Combat
- Increase `maxSimultaneousAttackers` to 1
- Increase `attackStaggerDelay` to 1.0s
- Disable flanking behavior

### For Harder Combat
- Increase `maxSimultaneousAttackers` to 3-4
- Decrease `attackStaggerDelay` to 0.2s
- Enable flanking with smaller `minAngleBetweenEnemies`

### For Strategic Combat
- Enable aggro system
- Use threat to prioritize dangerous targets
- Medium values for attack coordination

## API Reference

### Public Methods

```csharp
// Registration
void RegisterEnemy(EnemyBase enemy)
void UnregisterEnemy(EnemyBase enemy)

// Attack Coordination
bool RequestAttackPermission(EnemyBase enemy)
void NotifyAttackComplete(EnemyBase enemy)
int GetAttackingEnemyCount()

// Positioning
Vector3 GetAssignedPosition(EnemyBase enemy)
bool ShouldRepositionForFlanking(EnemyBase enemy)

// Threat System
void AddThreat(Transform target, float amount)
float GetThreatLevel(Transform target)
Transform GetHighestThreatTarget()

// Queries
List<EnemyBase> GetActiveEnemies()
List<EnemyBase> GetEnemiesTargeting(Transform target)

// Configuration
void SetAttackCoordinationSettings(int maxAttackers, float staggerDelay, float cooldown)
void SetFlankingEnabled(bool enabled)
void SetAggroEnabled(bool enabled)
```

## Example Use Cases

### Example 1: Boss Phase Changes
```csharp
// Make boss phase more aggressive
void EnterPhase2()
{
    EnemyManager.Instance.SetAttackCoordinationSettings(4, 0.2f, 0.5f);
}
```

### Example 2: Difficulty Scaling
```csharp
void SetDifficulty(int level)
{
    int maxAttackers = 1 + level; // 1 at easy, 4 at hard
    EnemyManager.Instance.SetAttackCoordinationSettings(maxAttackers, 0.5f, 1.0f);
}
```

### Example 3: Check Threat
```csharp
void UpdateEnemyTarget()
{
    Transform highestThreat = EnemyManager.Instance.GetHighestThreatTarget();
    if (highestThreat != null)
    {
        // Prioritize high-threat target
        SetTarget(highestThreat);
    }
}
```

## Troubleshooting

**Enemies not coordinating:**
- Ensure EnemyManager is in the scene
- Check that enemies are being registered (enable debug mode)
- Verify configuration settings are reasonable

**Too many/few attackers:**
- Adjust `maxSimultaneousAttackers`
- Check `attackStaggerDelay` isn't too high
- Ensure enemies have valid attack components

**Flanking not working:**
- Enable `enableFlankingBehavior`
- Check NavMesh is properly baked
- Ensure `preferredAttackDistance` is valid for your scene

**Performance issues:**
- Increase `positionUpdateInterval`
- Reduce number of active enemies
- Disable flanking if not needed

## Future Enhancements

Potential additions:
- Formation-based positioning
- Retreat coordination when low health
- Synchronized special attacks
- Leader/follower hierarchies
- Cover-seeking behavior

