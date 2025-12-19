# Material Randomization System

A comprehensive system for automatically randomizing materials on room objects (walls, floors, props, etc.) with minimal component overhead.

## Overview

This system provides an efficient way to add material variety to your roguelite rooms without adding components to every single object. Instead of having dozens of `MaterialRandomizer` components, you use one `RoomMaterialManager` per room.

## Components

### 1. RoomMaterialManager (Main Component)
**Location**: `Assets/Scripts/RogueLite/RoomLevels/RoomMaterialManager.cs`

Central manager that handles all material randomization for a room. Add ONE of these to your room root GameObject.

**Features**:
- Multiple material groups (Walls, Floors, Props, etc.)
- Individual randomization (each object gets different material)
- Synchronized randomization (all objects get same material)
- Support for multi-material models
- Automatic integration with room setup

**Usage**:
```csharp
// Called automatically during room setup, or manually:
roomMaterialManager.RandomizeAllMaterials();
```

### 2. MaterialRandomizer (Legacy/Single Object)
**Location**: `Assets/Scripts/RogueLite/RoomLevels/MaterialRandomizer.cs`

Use this if you need to randomize a single GameObject independently. For room-wide randomization, use `RoomMaterialManager` instead.

### 3. Room Material Setup Tool (Editor Tool)
**Location**: `Assets/Scripts/RogueLite/RoomLevels/Editor/RoomMaterialSetupTool.cs`

**Access**: `Tools > Room Material Setup Tool`

Automated editor tool that:
- Automatically finds wall/floor/prop parents
- Scans for material variants based on naming patterns
- Creates and configures material groups
- Sets up the entire room in seconds

## Quick Start Guide

### Setting Up a Room (Automated Method)

1. **Open the Tool**
   - Go to `Tools > Room Material Setup Tool`

2. **Select Your Room**
   - Drag your room root GameObject into "Room Root GameObject"

3. **Find Parents**
   - Click "Find Parents Automatically" to auto-detect Walls, Floors, Props
   - Or manually assign parent GameObjects

4. **Configure Options**
   - **Create Separate Groups**: Creates individual groups per material type
   - **Log Progress**: Shows detailed setup information in console

5. **Run Setup**
   - Click "Setup Material Randomization"
   - Done! The system automatically:
     - Finds all renderers
     - Detects material variants
     - Creates appropriate groups
     - Configures synchronization

### Manual Setup (For More Control)

1. **Add Component**
   ```
   Select room root → Add Component → RoomMaterialManager
   ```

2. **Create Groups**
   - Click "Add Empty Group"
   - Name it (e.g., "Walls", "Bedroom Furniture")
   - Set "Synchronize Group" for walls/floors
   - Uncheck for individual props

3. **Add Renderers**
   - Drag renderers that should share material variants

4. **Add Material Variants**
   - Add materials to the variants list
   - Set material index (0 for first material slot, 1 for second, etc.)

5. **Test**
   - Click "Randomize Now" to preview

## Material Variant Detection

The setup tool automatically finds material variants based on naming patterns:

### Supported Patterns

| Pattern | Example | Base Name |
|---------|---------|-----------|
| `_1`, `_2`, `_3` | `Wall_Material_1`, `Wall_Material_2` | `Wall_Material` |
| `_a`, `_b`, `_c` | `Floor_Wood_a`, `Floor_Wood_b` | `Floor_Wood` |
| Final digit | `WallTexture1`, `WallTexture2` | `WallTexture` |
| Final letter | `Floora`, `Floorb` | `Floor` |

### Requirements
- Materials must be in the same folder
- Materials must share the same base name
- At least 2 variants required for auto-detection

## Usage Examples

### Example 1: Apartment Room with Varying Walls/Floors

```
Apartment_Room/
├── RoomMaterialManager ← One component here
├── Walls/
│   ├── Wall_01 (uses Wall_Material_1/2/3)
│   ├── Wall_02 (uses Wall_Material_1/2/3)
│   └── Wall_03 (uses Wall_Material_1/2/3)
├── Floors/
│   └── Floor_Mesh (uses Floor_Wood_a/b/c)
└── Props/
    ├── PropRandomizer ← Handles enable/disable
    ├── Bed (uses Bed_Mat_1/2/3)
    ├── Bookshelf (uses Bookshelf_Mat_a/b)
    └── Rug (uses Rug_Material_1/2/3/4)
```

**Setup Result**:
- Group "Walls": Synchronized (all walls same color)
- Group "Floors": Synchronized (consistent floor)
- Group "Bed": Individual (each bed can be different)
- Group "Bookshelf": Individual
- Group "Rug": Individual

### Example 2: Multi-Material Object

For a bookshelf with separate materials for wood and books:

```
Material Variants:
├── Bookshelf_Wood_1 (Index 0)
├── Bookshelf_Wood_2 (Index 0)
├── Bookshelf_Books_a (Index 1)
└── Bookshelf_Books_b (Index 1)
```

The tool will create:
- One group for wood variants (material index 0)
- One group for book variants (material index 1)

## Integration with Existing Systems

### PropRandomizer Integration

The system works alongside `PropRandomizer`:

```
Props/
├── PropRandomizer ← Enables/disables props randomly
├── Prop_01/
│   └── Renderer ← Material randomized by RoomMaterialManager
├── Prop_02/
│   └── Renderer ← Material randomized by RoomMaterialManager
```

**Execution Order**:
1. PropRandomizer enables/disables props
2. RoomMaterialManager randomizes materials on enabled props
3. Both happen automatically during `RogueLiteRoom.Setup()`

### RogueLiteRoom Integration

Material randomization is automatically called during room setup:

```csharp
public virtual void Setup()
{
    // ... door and chest setup ...
    
    // Randomize props (enable/disable)
    var propRandomizers = GetComponentsInChildren<PropRandomizer>();
    foreach (var propRandomizer in propRandomizers)
        propRandomizer.RandomizeProps();
    
    // Randomize materials
    var materialManager = GetComponent<RoomMaterialManager>();
    if (materialManager != null)
        materialManager.RandomizeAllMaterials();
}
```

## Best Practices

### Organization

1. **Parent Structure**: Organize your room hierarchy:
   ```
   Room_Root/
   ├── Walls/
   ├── Floors/
   ├── Ceiling/
   └── Props/
   ```

2. **Naming Conventions**: Use consistent suffixes for material variants
   - `_1`, `_2`, `_3` for numbered variants
   - `_a`, `_b`, `_c` for lettered variants

3. **Material Organization**: Keep variant materials in the same folder
   ```
   Materials/Apartment/
   ├── Wall_Blue_1.mat
   ├── Wall_Blue_2.mat
   └── Wall_Blue_3.mat
   ```

### Performance

1. **Single Manager**: Use ONE `RoomMaterialManager` per room
2. **Group Wisely**: Combine objects that share variants into groups
3. **Material Instances**: The system creates material instances at runtime automatically

### Workflow

1. **Initial Setup**: Use the automated tool for first-time setup
2. **Refinement**: Adjust groups in the inspector as needed
3. **Testing**: Use "Randomize Now" button for instant preview
4. **Iteration**: Add/remove variants without changing code

## Troubleshooting

### No Material Variants Found

**Problem**: Setup tool doesn't find any variants

**Solutions**:
- Check material naming follows supported patterns
- Ensure materials are in same folder
- Verify at least 2 variants exist
- Check materials are named similarly (e.g., `Wall_1` and `Wall_2`)

### Wrong Materials Applied

**Problem**: Materials appear on wrong objects

**Solutions**:
- Check material index in variant options (0, 1, 2, etc.)
- Verify renderer has correct number of material slots
- Use "Log Material Info" context menu to debug

### Materials Not Randomizing

**Problem**: Materials stay the same

**Solutions**:
- Ensure "Randomize On Start" is checked
- Check that Setup() is being called on the room
- Verify material variants list is not empty
- Check renderers are assigned to groups

### Groups Not Working

**Problem**: Groups behave unexpectedly

**Solutions**:
- Verify renderers are assigned
- Check synchronize setting matches intent
- Use "Log Randomization" to see what's happening
- Ensure material variants are not null

## API Reference

### RoomMaterialManager

```csharp
// Randomize all groups
public void RandomizeAllMaterials()

// Add a new group
public MaterialGroup AddGroup(string groupName, bool synchronized = false)

// Get a group by name
public MaterialGroup GetGroup(string groupName)

// Clear all groups
public void ClearAllGroups()
```

### MaterialRandomizer (Legacy)

```csharp
// Randomize materials
public void RandomizeMaterials()

// Add material option
public void AddMaterialOption(Material material, int index = 0)

// Set synchronization
public void SetSynchronizeAll(bool synchronize)
```

## Editor Context Menus

### RoomMaterialManager
- "Randomize Now (Editor)": Test randomization in edit mode
- "Clear All Groups": Remove all configured groups

### MaterialRandomizer
- "Randomize Now (Editor)": Test randomization
- "Find All Renderers": Auto-populate renderer list
- "Log Material Info": Debug current setup

## Performance Notes

- **Runtime Cost**: Minimal - only runs during room setup
- **Memory**: Material instances created per renderer
- **Build Size**: No impact - editor tools excluded from build

## Future Enhancements

Potential additions:
- Color tinting support
- Texture variation support
- Weighted random selection
- Material property randomization
- Save/load material configurations
- Batch setup for multiple rooms

## Support

For issues or questions:
1. Check "Log Randomization" for debug info
2. Use "Log Material Info" context menu
3. Verify material variant naming
4. Check Unity console for warnings/errors


