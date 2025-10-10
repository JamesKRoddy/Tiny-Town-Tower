# Room Extender System Guide

## Overview
The Room Extender system allows you to dynamically expand buildings during procedural generation by adding additional spawn points for more rooms. This creates larger, more complex buildings without increasing the base spawn point count.

## How It Works

### 1. **RoomExtender Class**
- Inherits from `RogueLiteRoom`
- Contains additional spawn points that get added to the building's available spawn pool
- Can be either FRIENDLY or HOSTILE (inherits type from parent building)
- Only spawns basic rooms at its spawn points (no recursive extenders to avoid infinite loops)

### 2. **Integration with RogueLikeBuildingDataScriptableObj**
- Room extenders are configured in the ScriptableObject alongside other room types
- **Extender Spawn Chance** (0-100%): Probability of selecting an extender when choosing a room
- **Max Extenders Per Building**: Maximum number of room extenders allowed in one building
- Extenders are selected through the same `GetBuildingRoom()` system as other rooms

### 3. **Integration with RogueLiteRoomParent**
- Room extenders can be placed like any other room
- When placed, their spawn points are automatically added to the dynamic spawn point list
- These new spawn points are prioritized for immediate filling
- The system tracks extender count and passes it to the ScriptableObject for spawn decisions
- Emergency fill system excludes extenders to prevent infinite recursion

## Creating a Room Extender Prefab

### Step 1: Create the Room Structure
1. Create a new GameObject for your room extender
2. Add the basic room structure (walls, floor, colliders, etc.)
3. Ensure all physical geometry has colliders for proper bounds calculation

### Step 2: Add the RoomExtender Component
```csharp
// The room will automatically inherit from RogueLiteRoom
// Add the RoomExtender component in the Inspector
```

### Step 3: Set Up Spawn Points
1. Create a child GameObject called "AdditionalSpawnPoints" (or any name)
2. Assign this to the `Additional Spawn Points Parent` field in RoomExtender
3. Add child Transforms for each spawn point:
   ```
   RoomExtender
   └── AdditionalSpawnPoints
       ├── SpawnPoint1
       ├── SpawnPoint2
       └── SpawnPoint3
   ```
4. Position and rotate each spawn point to face the correct direction
5. The forward direction (blue arrow) shows where rooms will face

### Step 4: Configure Settings
- **Basic Rooms Only**: If true (recommended), only basic rooms can spawn at extender spawn points
- **Show Extender Spawn Points**: Displays magenta gizmos in the scene view for debugging
- **Spawn Point Gizmo Color**: Customize the debug color

### Step 5: Add to Building Data
Add your Room Extender prefab to the **Room Extenders** list in your `RogueLikeBuildingDataScriptableObj`:

1. Open your Building Data ScriptableObject in the Inspector
2. Find the **"Room Extenders"** section
3. Add your extender prefab to the `Extender Rooms` list
4. Set the difficulty level (0 = always available)
5. Configure the spawn settings:
   - **Extender Spawn Chance** (0-100%): Recommend starting at 30%
   - **Max Extenders Per Building**: Recommend 1-2 for balanced buildings

**Important**: Extenders are in a separate list from hostile and friendly rooms. This allows independent control over when and how many extenders spawn.

## How Placement Works

### Room Selection Flow:
1. `GetBuildingRoom()` is called for each spawn point
2. **First Check**: Should we spawn an extender?
   - Roll random number (0-100)
   - If roll < extender spawn chance AND current extender count < max extenders:
     - Attempt to spawn an extender from the extender rooms list
     - If successful, return the extender
3. **Second Check**: Should we spawn a friendly room?
   - Roll random number (0-100)
   - If roll < friendly room spawn chance:
     - Spawn friendly room
   - Otherwise:
     - Spawn hostile room

### When an Extender is Placed:
1. Extender room is instantiated at the spawn point
2. System detects it's an extender via `RoomExtender` component
3. Extender counter is incremented
4. Extender's room type is set to match parent building
5. Extender's spawn points are added to the dynamic spawn list
6. These new spawn points are prioritized for immediate filling
7. System continues until all spawns (original + extender) are filled

### Emergency Fill System:
- If extender spawn points can't be filled normally
- System calls `GetAllRooms(difficulty, includeExtenders: false)`
- This excludes extenders to prevent infinite recursion
- Chooses room with minimum overlap from basic rooms only

## Visual Debugging

### Scene View Gizmos:
- **Magenta Spheres**: Extender spawn points
- **Magenta Arrows**: Spawn point facing direction
- **Green Bounds**: Rooms placed with no overlap
- **Yellow Bounds**: Rooms with acceptable overlap
- **Orange Bounds**: Rooms that exceeded tolerance (but were best option)

### Console Logging:
Enable `ShowCollisionDebug` to see detailed logs:
```
[PlaceRoomAtSpawn] Room extender detected! Type: HOSTILE, Adding 3 new spawn points
[HierarchicalPlacement] ⚠️ Room extender added 3 new spawn points - MUST be filled!
[HierarchicalPlacement] Prioritizing extender spawn 5 at position (...)
```

## Best Practices

### 1. **Spawn Point Placement**
- Place spawn points at reasonable distances from the extender's main structure
- Ensure spawn points face away from the extender to avoid overlap
- Test with `maxAcceptableOverlapVolume` to find the right tolerance

### 2. **Room Design**
- Keep extenders relatively compact to avoid excessive overlap
- Ensure proper colliders for accurate bounds calculation
- Use the "Log Extender Info" context menu option to verify setup

### 3. **Balance Configuration**
- Configure in the Building Data ScriptableObject, not the parent
- Start with **Extender Spawn Chance = 30%**
- Set **Max Extenders Per Building = 1-2** initially
- Adjust based on desired building complexity
- Settings are per-building-type, allowing different complexity for different buildings

### 4. **Testing**
Use the debug context menu options:
- Right-click RoomExtender → "Log Extender Info"
- Right-click RogueLiteRoomParent → "Check All Room Overlaps"
- Right-click RogueLiteRoomParent → "Log Current Room Placement"

## Example Setup

### Simple Hallway Extender:
```
HallwayExtender (RoomExtender)
├── Hallway Model (with colliders)
├── AdditionalSpawnPoints
│   ├── LeftSpawn (rotated left)
│   └── RightSpawn (rotated right)
└── Props (optional)
```

### Complex Multi-Exit Extender:
```
HubExtender (RoomExtender)
├── Hub Room Model (with colliders)
├── AdditionalSpawnPoints
│   ├── NorthSpawn
│   ├── EastSpawn
│   ├── SouthSpawn
│   └── WestSpawn
└── Props
```

## Troubleshooting

### Problem: Extender spawn points not filling
- **Check**: Ensure `Basic Rooms Only` is enabled
- **Check**: Verify spawn points have correct forward direction
- **Check**: Increase `maxAcceptableOverlapVolume` if rooms can't fit

### Problem: Too much overlap
- **Solution**: Reduce `maxAcceptableOverlapVolume`
- **Solution**: Adjust spawn point positions to be farther apart
- **Solution**: Use smaller room prefabs

### Problem: Infinite recursion / too many spawns
- **Solution**: Ensure `maxExtenders` limit is set
- **Solution**: Verify "Basic Rooms Only" is enabled on extenders
- **Solution**: Check emergency fill system is working (skips extenders)

### Problem: Room type mismatch
- **Check**: Extenders automatically inherit FRIENDLY/HOSTILE from parent
- **Check**: Use "Log Extender Info" to verify room type is set correctly

## Technical Notes

### Bounds Calculation:
- Extenders use the same bounds calculation as regular rooms
- All child colliders are automatically included
- Padding from `RogueLiteRoom` settings is applied

### Overlap Detection:
- Uses cubic unit volume for overlap calculation
- Smaller values = less physical overlap
- Distance penalty applied for rooms too close together

### Placement Priority:
- Extender spawn points are added to the front of the queue
- Ensures they're filled before other spawns
- Iteration counter resets to allow proper processing

## Integration with Existing Systems

### Doors:
- Extender rooms have doors like any other room
- Door validation still applies (floor behind door check)
- Extender type determines door behavior (locked in HOSTILE, unlocked in FRIENDLY)

### Chests:
- Standard chest spawning works in extenders
- 75% chance to deactivate (same as regular rooms)
- Difficulty-based chest setup applies

### Enemy Spawning:
- HOSTILE extenders spawn enemies normally
- FRIENDLY extenders skip enemy spawning
- Room type is determined by parent building composition

### Navigation:
- NavMesh is baked after all rooms (including extenders) are placed
- Extender rooms are included in navigation calculations
- No special handling needed

## Future Enhancements

Possible improvements to consider:
- **Weighted extender selection**: Choose specific extenders based on context
- **Conditional spawning**: Only spawn extenders if certain criteria are met
- **Extender chains**: Allow one level of extender recursion with strict limits
- **Dynamic spawn count**: Vary number of extender spawn points based on difficulty
- **Themed extenders**: Match extender style to parent building theme

## Summary

The Room Extender system provides a powerful way to create dynamic, varied building layouts:
- ✅ Seamlessly integrates with existing room placement system
- ✅ Respects overlap detection and collision resolution
- ✅ Inherits room type from parent building
- ✅ Prioritizes filling extender spawn points
- ✅ Prevents infinite recursion
- ✅ Fully debuggable with comprehensive logging

Test thoroughly with different configurations to find the right balance for your game!

