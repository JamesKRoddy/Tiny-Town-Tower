# Game Start System - Unity Setup Checklist

## Quick Setup Guide

Follow these steps to set up the game start/restart system in your Unity project.

### Step 1: Create GameStartManager GameObject
- [ ] Open your Camp Scene
- [ ] Create new GameObject: `GameStartManager`
- [ ] Add Component: `GameStartManager` script
- [ ] Configure settings:
  - [ ] Starting NPC Range: Min=2, Max=4
  - [ ] Building Damage Percent: 0.4 (40%)
  - [ ] Research Progress Loss Percent: 1.0 (100%)
  - [ ] Clear All Building Queues: ✓ (checked)
  - [ ] Remove All Crops: ✓ (checked)
  - [ ] Debug Logging: ✓ (optional, for testing)

### Step 2: Create NPC Spawn Points (Optional)
- [ ] Create empty GameObject: `NPC Spawn Points`
- [ ] Create 4 child empty GameObjects
- [ ] Position them around your camp spawn area (recommended: circle ~5 units radius)
- [ ] Name them: `Spawn Point 1`, `Spawn Point 2`, etc.
- [ ] Drag all 4 spawn points to `GameStartManager.npcSpawnPoints` array

> **Note:** If you skip this, NPCs will automatically spawn in a circle around (0,0,0)

### Step 3: Create GameStartMenu UI
- [ ] Find or create your UI Canvas in the scene
- [ ] Create new UI Panel: `GameStartMenu`
  - [ ] Set anchors: Stretch Full (fill screen)
  - [ ] Set color: Black or dark with some transparency (e.g., rgba(0,0,0,200))
  
- [ ] Add child: Text (TextMeshPro): `TitleText`
  - [ ] Position: Center-top of screen
  - [ ] Font Size: 48-72
  - [ ] Alignment: Center
  - [ ] Default Text: "SYSTEM INITIALIZATION"
  
- [ ] Add child: Text (TextMeshPro): `DescriptionText`
  - [ ] Position: Center of screen
  - [ ] Font Size: 24-32
  - [ ] Alignment: Center
  - [ ] Default Text: "Booting up... Survivors detected."

- [ ] Add child: Button (TextMeshPro): `ContinueButton`
  - [ ] Position: Bottom-center of screen
  - [ ] Add child Text (TextMeshPro) with text: "Continue" or "Press Any Key"
  - [ ] Style: Optional - can add hover effects, colors, etc.

- [ ] Add Component to `GameStartMenu` panel:
  - [ ] `CanvasGroup` (for fade animations)
  - [ ] `GameStartMenu` script
  
- [ ] Configure `GameStartMenu` script:
  - [ ] Drag `TitleText` to titleText field
  - [ ] Drag `DescriptionText` to descriptionText field
  - [ ] Drag `ContinueButton` to continueButton field
  - [ ] CanvasGroup will auto-detect (or drag manually if needed)
  - [ ] Set Fade In Duration: 1.0 (or customize)
  - [ ] Set Fade Out Duration: 1.0 (or customize)
  
- [ ] Set GameStartMenu panel to **inactive** (uncheck in inspector)

### Step 4: Connect to PlayerUIManager
- [ ] Find `PlayerUIManager` in your scene hierarchy
- [ ] Drag `GameStartMenu` to the `gameStartMenu` field in the inspector
  - It should be in the "Overlay Menu References" section

### Step 5: Verify NPCManager Configuration
- [ ] Find `NPCManager` in your scene
- [ ] Verify these are set:
  - [ ] Settler Prefab (your procedural NPC prefab)
  - [ ] Settler Names File (text file with names)
  - [ ] Settler Descriptions File (text file with descriptions)
  - [ ] Age Range: 18-65 (or your preference)

### Step 6: Test New Game Flow
- [ ] Save your scene
- [ ] Open Main Menu scene
- [ ] Enter Play Mode
- [ ] Click "New Game"
- [ ] **Expected:**
  - Loading screen appears
  - Camp scene loads
  - GameStartMenu fades in with "SYSTEM INITIALIZATION"
  - **Time is frozen (day/night cycle paused)**
  - **No dirt accumulation during sequence**
  - 2-4 NPCs spawn
  - Description updates: "System initialized. Survivors ready."
  - **Continue button appears**
  - Press the button
  - **Time resumes (day/night cycle continues)**
  - Menu fades out
  - NPCs are active and ready to work

### Step 7: Test Restart Flow
- [ ] Start a game with NPCs
- [ ] Kill all NPCs (you can use debug commands or let enemies kill them)
- [ ] **Expected:**
  - Console shows: "[NPCManager] All NPCs have been lost!"
  - GameStartMenu fades in with "SYSTEM REBOOT"
  - Description: "Rebooting system... Survivors detected nearby."
  - **Time is frozen during restart**
  - **No dirt spawns or health effects during sequence**
  - Buildings show damage (health bars reduced)
  - 2-4 new NPCs spawn
  - Description updates: "System restored. New survivors ready."
  - **Continue button appears**
  - Press the button
  - **Time resumes**
  - Menu fades out
  - New NPCs are active

### Step 8: Verify Reset Functionality
After restart, check:
- [ ] Buildings have reduced health (40% damage)
- [ ] Research buildings have empty queues
- [ ] Farm plots are cleared (no crops)
- [ ] Cleanliness is low (30-50%) with 3-5 dirt piles visible
- [ ] Completed research is still unlocked
- [ ] Building positions unchanged

## Common Issues & Solutions

### GameStartMenu doesn't appear
**Problem:** Menu never shows during game start
**Solutions:**
- Ensure GameStartMenu has a CanvasGroup component
- Check that GameStartMenu is referenced in PlayerUIManager
- Verify GameStartMenu starts as inactive
- Check Canvas is set to Screen Space - Overlay

### NPCs spawn but don't move/work
**Problem:** NPCs appear but stand still
**Solutions:**
- Ensure NavMesh is baked in camp scene
- Check that NPCManager.Initialize() is being called
- Verify CampManager is in the scene
- Check that WorkManager is set up properly

### Restart doesn't trigger
**Problem:** When last NPC dies, nothing happens
**Solutions:**
- Verify GameStartManager is in the scene
- Check console for "[NPCManager] All NPCs have been lost!" message
- Ensure NPCManager.UnregisterNPC() is called when NPCs die
- Verify that SettlerNPC properly unregisters on death

### Buildings not damaged on restart
**Problem:** After restart, buildings have full health
**Solutions:**
- Check Building Damage Percent is > 0
- Ensure buildings are not under construction (only operational buildings are damaged)
- Verify buildings have IDamageable interface
- Check for errors in console during ResetCampState()

### UI text doesn't update
**Problem:** Menu shows but text is blank or wrong
**Solutions:**
- Check TitleText and DescriptionText are properly assigned
- Verify TextMeshPro is imported in project
- Ensure text objects are active
- Check font asset is assigned to text components

## Debug Commands (Optional)

You can add these to a debug menu for testing:

```csharp
// Trigger restart manually
if (GameStartManager.Instance != null)
{
    GameStartManager.Instance.TriggerGameRestart();
}

// Kill all NPCs for testing
foreach (var npc in NPCManager.Instance.GetAllNPCs())
{
    npc.Die();
}

// Check NPC count
Debug.Log($"Current NPC Count: {NPCManager.Instance.TotalNPCs}");
```

## Performance Notes

- Restart sequence takes approximately 5-7 seconds
- New game sequence takes approximately 4-6 seconds
- No significant performance impact during normal gameplay
- Fade animations use Lerp, very lightweight

## Customization Tips

**Change number of starting NPCs:**
- Adjust `Starting NPC Range` in GameStartManager

**Modify restart difficulty:**
- Increase `Building Damage Percent` for harder restart
- Set to 0 for no building damage
- Adjust `Research Progress Loss Percent` (0-1)

**Custom UI styling:**
- Modify GameStartMenu panel appearance
- Change font sizes and colors
- Add additional UI elements (icons, progress bars)
- Customize fade duration for faster/slower transitions

**Add narrative elements:**
- Edit text strings in GameStartMenu inspector
- Add more description updates in GameStartManager.StartNewGameSequence()
- Create custom UI elements that appear during sequence

## Final Checklist
- [ ] GameStartManager in scene with settings configured
- [ ] GameStartMenu UI created and styled
- [ ] PlayerUIManager references GameStartMenu
- [ ] NPCManager fully configured
- [ ] New game tested successfully
- [ ] Restart tested successfully
- [ ] All reset functions verified

## Need Help?

Refer to `GAME_START_SYSTEM_README.md` for detailed documentation on:
- System architecture
- How each component works
- Event system details
- Advanced configuration options

