# Bed Assignment System

This system allows NPCs to be assigned to specific beds for sleeping. Each bed can be assigned to one NPC, and NPCs will navigate to their assigned bed when they need to sleep.

## Components

### SleepTask
- Inherits from `WorkTask`
- Handles bed assignments and sleeping behavior
- Each bed has one `SleepTask` component
- Manages which NPC is assigned to the bed

### BedBuilding
- Inherits from `Building`
- Automatically adds a `SleepTask` component
- Provides bed-specific functionality and UI

## Setup Instructions

### 1. Create a Bed Prefab
1. Create a new GameObject in your scene
2. Add the `BedBuilding` component
3. Add a `Transform` child object to represent the exact sleeping location
4. Assign this transform to the `bedLocation` field in the `BedBuilding` component
5. Add any visual bed models as children

### 2. Configure the Bed
- Set the `bedLocation` transform to the exact position where NPCs should sleep
- The `SleepTask` will automatically be added and configured
- No additional configuration needed

### 3. Access Bed Information
```csharp
// Get the SleepTask component
SleepTask sleepTask = bedBuilding.GetSleepTask();

// Check if bed is assigned
bool isAssigned = sleepTask.IsBedAssigned;

// Get the assigned settler
SettlerNPC assignedSettler = sleepTask.AssignedSettler;

// Check if a specific settler can use this bed
bool canUse = sleepTask.CanSettlerUseBed(settler);
```

### 4. Bed Assignment
- Select a bed building in the game
- Choose "Bed Assignment" from the selection popup
- Select "Assign Settler to Bed" to assign a new settler
- Choose a settler from the settler menu
- The settler will now be assigned to that bed

### 5. Programmatic Bed Assignment
```csharp
// Get the SleepTask component
SleepTask sleepTask = bedBuilding.GetSleepTask();

// Assign a settler to the bed
bool success = sleepTask.AssignSettlerToBed(settler);

// Unassign a settler from the bed
sleepTask.UnassignSettlerFromBed();
```

## How It Works

### Bed Assignment
- Each bed can only be assigned to one settler
- When a settler is assigned, they "own" that bed
- Other settlers cannot use an assigned bed
- Beds can be unassigned to make them available again

### Sleep Behavior
- NPCs will automatically find their assigned bed when tired
- If no bed is assigned, they will sleep in place or find an unassigned bed
- The `SleepState` component handles finding and navigating to beds
- Sleeping provides rest and stamina recovery

### UI Integration
- Bed selection shows current assignment status
- Building stats display bed assignment information
- Assignment/unassignment options in selection popup
- Tooltips show bed availability and current assignments

## Technical Details

### Reflection Usage
The system uses reflection to avoid compilation issues during development:
- `SleepTask` properties are accessed via reflection
- This allows the system to work even if `SleepTask` hasn't been compiled yet
- All bed operations use reflection for maximum compatibility

### Task Integration
- `SleepTask` integrates with the existing `WorkTask` system
- Beds appear in the work manager and can be assigned like other tasks
- Sleep tasks don't consume electricity or generate dirt
- Sleep tasks are single-worker tasks (one NPC per bed)

### Backward Compatibility
- The system falls back to the old sleep location finding if no beds are found
- Existing sleep behavior is preserved for NPCs without assigned beds
- No changes needed to existing NPCs or sleep states

## Example Usage

```csharp
// Check if a bed is assigned
bool isAssigned = bedBuilding.IsBedAssigned();

// Get the assigned settler
SettlerNPC assignedSettler = bedBuilding.GetAssignedSettler();

// Check if a settler can use a specific bed
bool canUse = sleepTask.CanSettlerUseBed(settler);
```

## Troubleshooting

### Bed Not Found
- Ensure the `BedBuilding` component is added to the bed GameObject
- Check that the `bedLocation` transform is assigned
- Verify the `SleepTask` component is present

### Assignment Issues
- Only one settler can be assigned to each bed
- Unassign the current settler before assigning a new one
- Check that the settler is a `SettlerNPC` (not a robot)

### Sleep Navigation Issues
- Ensure the bed location is accessible via NavMesh
- Check that the `SleepState` component is working correctly
- Verify bed assignments are properly set
