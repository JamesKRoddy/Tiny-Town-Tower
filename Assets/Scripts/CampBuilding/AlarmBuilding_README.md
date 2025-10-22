# Alarm Building System

## Overview
The Alarm Building system provides two key features for defending your camp against zombie attacks:

1. **Automatic Wake-Up on Attack**: Settlers who are sleeping will automatically wake up when they take damage from zombies
2. **Wave Alert System**: When operational, the Alarm Building alerts all sleeping NPCs when a wave starts, giving them time to prepare

## Features

### 1. Wake-Up on Attack (SettlerNPC.cs)
When a sleeping settler is attacked by a zombie, they will:
- Immediately wake up from sleep
- Stop their sleep animation
- Transition to FLEE state to escape the threat
- React to the attacker's position

This works for all damage types:
- Basic damage
- Damage with poise
- Elemental damage
- Combined poise and elemental damage

### 2. Alarm Building (AlarmBuilding.cs)
The Alarm Building is a defensive structure that:
- Automatically triggers when enemy waves start (via `CampManager.OnCampWaveStarted` event)
- Wakes up all sleeping NPCs in range
- Only functions when operational and not under construction
- Can be manually triggered for testing via context menu

#### Configuration Options
- **Alarm Range**: Set to 0 for unlimited range, or specify a radius in meters
- **Alarm Effect Prefab**: Optional visual/audio effect to play when alarm triggers
- **Alarm Effect Duration**: How long the alarm effect lasts (default: 3 seconds)

## Setup Instructions

### Creating an Alarm Building in Unity

1. **Create Building GameObject**:
   - Create a new GameObject in your scene
   - Add the `AlarmBuilding` component
   - Add necessary colliders for building placement

2. **Create Building ScriptableObject**:
   - Create a new `BuildingScriptableObj` asset
   - Configure building properties (health, construction time, repair time, etc.)
   - Assign this ScriptableObject to the AlarmBuilding

3. **Configure Alarm Settings**:
   - Set `alarmRange` (0 = unlimited, or specify radius)
   - Optionally assign an `alarmEffectPrefab` for visual/audio feedback
   - Set `alarmEffectDuration` for how long effects last

4. **Register with Building System**:
   - Ensure the building is properly registered with the camp's building system
   - The alarm will automatically subscribe to wave events when constructed

### Example Configuration
```
Alarm Range: 0 (alerts all NPCs regardless of distance)
Alarm Effect Prefab: SirenEffect (with AudioSource + ParticleSystem)
Alarm Effect Duration: 3 seconds
```

## How It Works

### Wake-Up Flow on Attack
```
1. Zombie attacks sleeping settler
2. SettlerNPC.TakeDamage() detects sleep state
3. WakeUpFromAttack() is called
4. Settler changes to FLEE state
5. SleepState.OnExitState() handles cleanup
6. Base damage handling proceeds normally
```

### Alarm Flow on Wave Start
```
1. CampManager starts new wave
2. CampManager.OnCampWaveStarted event fires
3. AlarmBuilding.OnWaveStarted() receives event
4. Checks if building is operational
5. TriggerAlarm() wakes all sleeping NPCs
6. Each sleeping NPC transitions to WANDER state
7. NPCs detect threats and respond appropriately
```

## Technical Details

### Event Subscription
The AlarmBuilding subscribes to wave events in:
- `Start()` - when first loaded
- `CompleteConstruction()` - when construction finishes
- `OnDestroy()` - unsubscribes to prevent memory leaks

### NPC Wake-Up Logic
When waking NPCs, the alarm:
1. Gets all settlers from `NPCManager.Instance.GetAllNPCs()`
2. Checks each settler's current task
3. If sleeping, checks if within range
4. Changes task to `TaskType.WANDER`
5. Logs how many NPCs were woken

### Range Visualization
When selected in editor, the alarm shows:
- **Limited Range**: Red wire sphere showing alarm radius
- **Unlimited Range**: Expanding circles to indicate global coverage

## Debug Features

### Manual Alarm Trigger
Right-click the AlarmBuilding component in inspector and select "Trigger Alarm Manually" to test the system.

### Console Logging
The system provides detailed logging:
- When alarm subscribes to wave events
- When alarm triggers
- How many NPCs were woken
- When individual NPCs wake up
- When alarm deactivates

## Integration with Existing Systems

### Compatible Systems
- **Wave System**: Uses `CampManager.OnCampWaveStarted` event
- **NPC System**: Uses `NPCManager.GetAllNPCs()`
- **Task System**: Uses existing `TaskType.SLEEP`, `TaskType.FLEE`, `TaskType.WANDER`
- **Building System**: Extends `Building` base class

### No Breaking Changes
The implementation:
- Uses the `new` keyword to extend `TakeDamage` methods (no base class changes)
- Doesn't modify any existing enums or interfaces
- Follows existing code patterns and conventions
- Respects building operational state

## Usage Tips

1. **Placement**: Place the alarm centrally if using limited range
2. **Power**: Consider connecting to electricity system if implemented
3. **Redundancy**: Multiple alarms can provide overlapping coverage
4. **Defense Strategy**: Combine with bunkers for evacuation + alert system
5. **Testing**: Use context menu to test alarm during peaceful times

## Future Enhancements

Possible additions:
- Power requirement (electricity system integration)
- Upgrade levels for increased range
- Different alarm types (evacuate vs. defend)
- Visual indicators on sleeping NPCs showing they're alerted
- Cooldown period to prevent spam
- Integration with building health (damaged alarms are less effective)

## Troubleshooting

### Alarm Not Triggering
- Check if building is operational (`isOperational = true`)
- Check if building is under construction (`isUnderConstruction = false`)
- Verify subscription to wave events in console logs

### NPCs Not Waking Up
- Ensure NPCs are actually sleeping (`TaskType.SLEEP`)
- Check alarm range settings
- Verify `NPCManager.Instance` is available
- Check console logs for wake-up count

### Wave Events Not Firing
- Verify `CampManager.Instance` exists
- Check that wave system is properly initialized
- Look for wave start event logs in console


