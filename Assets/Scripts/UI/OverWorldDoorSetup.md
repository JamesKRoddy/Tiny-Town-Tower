# OverWorldDoor Setup Guide

## Overview
The OverWorldDoor system allows you to set building difficulty directly on doors in the overworld, with built-in Gizmos display of difficulty information in the Unity Scene view.

## Components

### OverWorldDoor
- **Location**: `Assets/Scripts/RogueLite/RoomLevels/OverWorldDoor.cs`
- **Purpose**: Door component for overworld buildings with difficulty control
- **Inherits**: From `RogueLiteDoor`
- **Features**: Gizmos display in Scene view for easy difficulty visualization

## Setup Instructions

### 1. Creating OverWorldDoor Prefabs

1. Create a new GameObject in your overworld scene
2. Add the `OverWorldDoor` component
3. Configure the settings:
   - **Building Difficulty**: Set the base difficulty for this building
   - **Building Type**: Select the type of building (House, Factory, etc.)
   - **Building Name**: Give it a descriptive name
   - **Gizmo Display**: Configure how difficulty is shown in Scene view

### 2. OverWorldDoor Settings

```csharp
[Header("Overworld Door Settings")]
public int buildingDifficulty = 10;        // Base difficulty for this building
public BuildingType buildingType;          // Type of building
public string buildingName = "Unknown Building"; // Display name

[Header("Gizmo Display")]
public bool showDifficultyGizmos = true;   // Show difficulty gizmos in Scene view
public float gizmoHeight = 2f;             // Height of the gizmo above the door
public float gizmoRadius = 0.5f;           // Size of the difficulty sphere
public bool showDifficultyText = true;     // Show difficulty text in Scene view
```

## How It Works

### 1. Building Entry
When a player interacts with an OverWorldDoor:
1. `OnDoorEntered()` is called
2. DifficultyManager is initialized with the building's difficulty
3. Base door entry logic is executed

### 2. Gizmos Display
- **Wire Sphere**: Shows above each door in Scene view
- **Color Coding**: Different colors for different difficulty levels
- **Connection Line**: Visual line connecting door to difficulty indicator
- **Text Labels**: Building name and difficulty when selected
- **Filled Sphere**: When door is selected in Scene view

### 3. Difficulty Progression
- Building difficulty is set by the OverWorldDoor
- Room difficulty is calculated automatically based on room number
- Wave difficulty combines building and room difficulty

## Visual Features

### Scene View Gizmos
- **Wire Sphere**: Always visible, shows difficulty level
- **Color Coding**: 
  - Green (0-19): Easy
  - Yellow (20-39): Medium  
  - Red (40-59): Hard
  - Magenta (60+): Very Hard
- **Connection Line**: White line from door to difficulty sphere
- **Text Labels**: Building name and difficulty when selected
- **Filled Sphere**: When door is selected for better visibility

### Difficulty Colors
```csharp
private Color GetDifficultyColor(int difficulty)
{
    if (difficulty < 20) return Color.green;      // Easy
    if (difficulty < 40) return Color.yellow;     // Medium
    if (difficulty < 60) return Color.red;        // Hard
    return Color.magenta;                         // Very Hard
}
```

## Example Usage

### Creating a Difficult Building
```csharp
// In the inspector for OverWorldDoor:
buildingDifficulty = 50;
buildingType = BuildingType.FACTORY;
buildingName = "Abandoned Factory";
showDifficultyGizmos = true;
gizmoHeight = 2f;
gizmoRadius = 0.5f;
showDifficultyText = true;
```

### Creating an Easy Building
```csharp
// In the inspector for OverWorldDoor:
buildingDifficulty = 5;
buildingType = BuildingType.HOUSE;
buildingName = "Small House";
showDifficultyGizmos = true;
gizmoHeight = 2f;
gizmoRadius = 0.5f;
showDifficultyText = true;
```

## Benefits

1. **Visual Feedback**: Developers can see difficulty at a glance in Scene view
2. **Easy Configuration**: Set difficulty directly on doors
3. **Color Coding**: Quick visual identification of difficulty levels
4. **Consistent System**: All difficulty logic centralized
5. **Developer Friendly**: Clear visual indicators in editor
6. **Performance**: No runtime GUI overhead

## Tips

1. **Balance Difficulty**: Start with low difficulties and increase gradually
2. **Use Descriptive Names**: Help identify buildings in Scene view
3. **Adjust Gizmo Height**: Set appropriate height for your scene scale
4. **Color Consistency**: Use consistent colors across your game
5. **Scene Organization**: Use gizmos to quickly identify building types and difficulties

## Gizmo Features

### OnDrawGizmos()
- Always visible wire sphere
- Color-coded by difficulty
- Connection line from door
- No text labels (for performance)

### OnDrawGizmosSelected()
- Filled sphere when selected
- Text labels with building name and difficulty
- Same color coding as wire sphere
- More detailed visual feedback

### Configuration Options
- **showDifficultyGizmos**: Enable/disable all gizmo display
- **gizmoHeight**: Control how high above the door the gizmo appears
- **gizmoRadius**: Control the size of the difficulty sphere
- **showDifficultyText**: Enable/disable text labels when selected 