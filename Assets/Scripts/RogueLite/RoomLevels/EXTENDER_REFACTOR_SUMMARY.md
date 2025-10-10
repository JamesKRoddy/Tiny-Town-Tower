# Room Extender System - Architecture Refactor

## What Changed

### Before (Original Design):
- Extender settings were in `RogueLiteRoomParent.cs`
- Two inspector fields: `extenderSpawnChance` and `maxExtenders`
- Settings applied globally to all building types
- Less flexible configuration

### After (Refactored Design):
- Extender settings moved to `RogueLikeBuildingDataScriptableObj.cs`
- Extenders are now a separate room type alongside hostile and friendly rooms
- Settings are per-building-type, allowing different configurations
- More consistent with existing room system architecture

## Changes Made

### 1. `RogueLikeBuildingDataScriptableObj.cs`
**Added:**
```csharp
[Header("Room Extenders")]
public List<BuildingRooms> extenderRooms = new List<BuildingRooms>();
[SerializeField, Range(0f, 100f)] private float extenderSpawnChance = 30f;
[SerializeField, Range(0, 5)] private int maxExtendersPerBuilding = 2;
```

**Updated Methods:**
- `GetBuildingRoom(int difficulty, int currentExtenderCount = 0, bool allowExtenders = true)`
  - Now checks extender spawn chance first
  - Considers current extender count vs max extenders
  - Returns extender if roll succeeds, otherwise proceeds to friendly/hostile selection
  
- **New:** `GetExtenderRoom(int difficulty)`
  - Selects a random extender room based on difficulty
  - Returns null if no suitable extenders available
  
- `GetAllRooms(int difficulty, bool includeExtenders = true)`
  - Added `includeExtenders` parameter
  - Allows excluding extenders for emergency placement
  - Prevents infinite recursion

- **New Getter/Setter Methods:**
  - `GetMaxExtendersPerBuilding()`
  - `GetExtenderSpawnChance()`
  - `SetExtenderSpawnChance(float chance)`

### 2. `RogueLiteRoomParent.cs`
**Removed:**
```csharp
// These fields were removed from inspector
[SerializeField] private float extenderSpawnChance = 0.3f;
[SerializeField] private int maxExtenders = 2;
```

**Kept:**
```csharp
private int extendersPlaced = 0; // Still tracks count internally
```

**Updated Method Calls:**
- `GetBuildingRoom()` → `GetBuildingRoom(currentDifficulty, extendersPlaced, allowExtenders: true)`
  - Passes current extender count to ScriptableObject
  - ScriptableObject makes the spawn decision
  
- `GetAllRooms()` → `GetAllRooms(currentDifficulty, includeExtenders: false)`
  - Emergency placement excludes extenders
  - Prevents recursive extender spawning

**Enhanced Tracking:**
- Increments `extendersPlaced` when an extender is placed
- Logs extender count in debug messages

## Benefits of Refactoring

### 1. **Centralized Configuration**
- All room data (hostile, friendly, extenders) in one place
- Easier to manage and understand
- Consistent with existing architecture

### 2. **Per-Building Flexibility**
- Different building types can have different extender settings
- Hospital might have high extender chance (complex layout)
- Shop might have low extender chance (simple layout)
- More design control

### 3. **Better Separation of Concerns**
- ScriptableObject: Room selection logic and configuration
- RoomParent: Room placement and collision management
- Cleaner code organization

### 4. **Consistent API**
- Extenders use same system as friendly/hostile rooms
- Easier to understand for new developers
- Less duplication of logic

### 5. **Prevents Recursion More Explicitly**
- `includeExtenders: false` parameter is clear and obvious
- No need to check for RoomExtender component in multiple places
- Emergency fill explicitly excludes extenders

## Migration Guide

### For Existing Projects:
1. Open each `RogueLikeBuildingDataScriptableObj` in Inspector
2. Find the new **"Room Extenders"** section
3. Add your extender prefabs to the `Extender Rooms` list
4. Set **Extender Spawn Chance** (default: 30%)
5. Set **Max Extenders Per Building** (default: 2)
6. Remove any custom extender settings from scene objects

### For New Prefabs:
1. Create your room extender prefab (see ROOM_EXTENDER_GUIDE.md)
2. Add it to the extender rooms list in your building data
3. Configure spawn chance and max extenders
4. Test in-game

## Testing Checklist

- [ ] Extenders spawn at expected frequency
- [ ] Max extender limit is respected
- [ ] Extender spawn points are filled
- [ ] No infinite recursion in emergency placement
- [ ] Different building types can have different settings
- [ ] Console logs show correct extender count
- [ ] Visual gizmos display correctly

## Backward Compatibility

**Breaking Changes:**
- Inspector fields removed from `RogueLiteRoomParent`
  - You'll need to reconfigure settings in ScriptableObjects
  
**Non-Breaking:**
- All existing functionality preserved
- Same room placement behavior
- Same collision detection
- Same debug tools

## Future Enhancements

Possible improvements:
- **Difficulty Scaling**: Increase max extenders at higher difficulties
- **Room Type Preferences**: Certain extenders only in friendly/hostile rooms
- **Conditional Spawning**: Spawn extenders based on room count/size
- **Weighted Selection**: Some extenders more common than others
- **Dynamic Spawn Points**: Vary number of spawn points per extender

## Summary

This refactor improves the architecture by:
- ✅ Centralizing configuration in ScriptableObjects
- ✅ Providing per-building-type flexibility
- ✅ Following existing patterns (friendly/hostile rooms)
- ✅ Explicitly preventing recursion
- ✅ Maintaining all existing functionality

The system is now more maintainable, flexible, and easier to understand!

