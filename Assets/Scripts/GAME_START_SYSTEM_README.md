# Game Start/Restart System

## Overview
This system handles the game initialization sequence when players start a new game or restart after losing all NPCs. It features a computer boot sequence UI and automatically manages game state reset.

## Components

### 1. GameStartManager
**Location:** `Assets/Scripts/Managers/GameManagers/GameStartManager.cs`

**Purpose:** Central manager for handling game start and restart logic.

**Key Features:**
- Detects whether it's a new game or restart
- Spawns 2-4 random NPCs for the computer boot sequence
- Resets camp state on restart (damages buildings, clears queues, etc.)
- Coordinates with other managers for proper initialization
- Pauses camp systems during initialization (time, cleanliness, electricity)
- Resumes camp systems when player presses continue button

**Inspector Settings:**
- `Starting NPC Range`: Number of NPCs to spawn (default: 2-4)
- `NPC Spawn Points`: Optional spawn transforms (auto-generates if not set)
- `Building Damage Percent`: How much buildings are damaged on restart (default: 40%)
- `Research Progress Loss Percent`: Research progress reset amount (default: 100%)

### 2. GameStartMenu
**Location:** `Assets/Scripts/UI/MiscMenus/GameStartMenu.cs`

**Purpose:** Displays the computer boot sequence UI with fade in/out animations.

**Key Features:**
- Shows different text for new game vs restart
- Updates description dynamically during sequence
- Shows "Continue" button when ready
- Waits for player input before proceeding
- Fires `OnContinuePressed` event when button is clicked
- Independent fade in/out (doesn't conflict with scene transition fade)
- Sets player controls to IN_MENU when active

**Inspector Settings:**
- `Title Text`: Text component for the title
- `Description Text`: Text component for the description
- `Continue Button`: Button that player presses to continue
- `Canvas Group`: For fade animations (auto-detects if not set)
- `Fade In Duration`: How long to fade in (default: 1s)
- `Fade Out Duration`: How long to fade out (default: 1s)

### 3. Updated Managers

#### NPCManager
**New Features:**
- `OnAllNPCsLost` event: Fires when all NPCs die
- Automatically triggers game restart when no NPCs remain
- Tracks NPC count changes

#### ResearchManager
**New Methods:**
- `ClearCurrentlyResearching()`: Clears in-progress research

#### WorkManager
**New Methods:**
- `ClearAllTasks()`: Removes all tasks from work queue

#### CleanlinessManager
**New Methods:**
- `ResetCleanliness()`: Sets cleanliness to abandoned state (30-50%) and spawns 3-5 dirt piles
- `SetPaused(bool paused)`: Pauses/resumes dirt generation and health effects

#### TimeManager
**New Methods:**
- `PauseTime()`: Pauses the time cycle
- `ResumeTime()`: Resumes the time cycle

#### ElectricityManager
**New Methods:**
- `SetPaused(bool paused)`: Pauses/resumes electricity system (for consistency)

#### WorkTask (Base Class)
**New Methods:**
- `ResetProgress()`: Resets work progress to 0
- `StopAllWork()`: Stops all workers and resets task

#### QueuedWorkTask
**New Methods:**
- `ClearQueue()`: Clears task queue and resets progress

## How It Works

### New Game Flow
1. Player clicks "New Game" in main menu
2. `SaveLoadManager` deletes existing save
3. `SceneTransitionManager` loads camp scene
4. `CampManager.Start()` waits 0.5s for save loading
5. `GameStartManager.InitializeGame()` detects no save file
6. **Pauses camp systems** (time, cleanliness, electricity)
7. Shows `GameStartMenu` with "SYSTEM INITIALIZATION"
8. Spawns 2-4 random NPCs around origin
9. Waits 2 seconds for dramatic effect
10. Updates UI: "System initialized. Survivors ready."
11. Shows "Continue" button
12. Waits for player to press button
13. **Player presses continue - fires `OnContinuePressed` event**
14. **Resumes camp systems**
15. Fades out menu and begins gameplay

### Restart After Losing All NPCs Flow
1. Last NPC dies
2. `NPCManager.UnregisterNPC()` detects NPC count = 0
3. Fires `OnAllNPCsLost` event
4. Calls `GameStartManager.TriggerGameRestart()`
5. **Pauses camp systems** (time, cleanliness, electricity)
6. Shows `GameStartMenu` with "SYSTEM REBOOT"
7. Resets camp state:
   - Damages all buildings by 40%
   - Clears research progress on active buildings
   - Clears all cooking/research queues
   - Removes all farm crops
   - Clears work tasks from queue
   - Sets cleanliness to abandoned state (30-50% with dirt piles)
8. Spawns 2-4 new random NPCs
9. Waits 2 seconds
10. Updates UI: "System restored. New survivors ready."
11. Shows "Continue" button
12. Waits for player to press button
13. **Player presses continue - fires `OnContinuePressed` event**
14. **Resumes camp systems**
15. Fades out menu and resumes gameplay

### Restart Reset Details

When restarting after losing all NPCs, the following changes occur:

**Buildings:**
- Take 40% damage (configurable)
- Do NOT get destroyed
- Can be repaired by new NPCs

**Research:**
- In-progress research is cancelled
- Completed research remains unlocked
- Research buildings have empty queues

**Farms:**
- All crops are removed
- Farm plots are cleared
- Ready for new planting

**Work Tasks:**
- All queued tasks are removed
- In-progress work is cancelled
- Buildings remain operational (if not destroyed)

**Cleanliness:**
- Set to 30-50% (abandoned state)
- 3-5 new dirt piles spawn
- Represents camp neglect during abandonment
- New NPCs will need to clean up

**Preserved:**
- Completed research
- Building placements
- Resource stockpiles in PlayerInventory
- Camp layout and structure

## Camp System Pause During Initialization

While the `GameStartMenu` is active, all camp systems are paused to prevent unintended progression:

**Paused Systems:**
- **Time Manager**: Day/night cycle is frozen
- **Cleanliness Manager**: No dirt accumulation or health effects
- **Electricity Manager**: No power consumption (work tasks aren't running anyway)

**Resume Trigger:**
When the player presses the "Continue" button:
1. `GameStartMenu` fires `OnContinuePressed` event
2. `GameStartManager` subscribes to this event
3. Camp systems are resumed via `ResumeCampSystems()`
4. Normal gameplay begins

This ensures that time doesn't pass and conditions don't deteriorate during the boot sequence.

## Setup Instructions

### In Unity Editor:

1. **Add GameStartManager to Camp Scene:**
   - Create new GameObject: "GameStartManager"
   - Add `GameStartManager` component
   - Configure spawn points (optional)
   - Set building damage and other parameters

2. **Setup GameStartMenu UI:**
   - Create UI Canvas if not exists
   - Add `GameStartMenu` component to a panel
   - Connect UI text elements (title, description)
   - Add CanvasGroup for fade animations
   - Set text for new game and restart scenarios

3. **Connect to PlayerUIManager:**
   - Select PlayerUIManager in scene
   - Drag GameStartMenu to the `gameStartMenu` field
   - Ensure it's in the Overlay Menu References section

4. **Configure NPCManager:**
   - Ensure NPC spawn settings are configured
   - Set settler prefab and name/description files

### Testing:

**New Game:**
1. Start from Main Menu
2. Click "New Game"
3. Watch boot sequence with 2-4 NPCs spawning
4. Verify gameplay begins after sequence

**Restart:**
1. Start a game with NPCs
2. Kill all NPCs (debug or gameplay)
3. Watch system reboot sequence
4. Verify 2-4 new NPCs spawn
5. Check that buildings are damaged
6. Verify research/crops/queues are cleared

## Configuration

### Spawn Points
If you want specific spawn locations:
1. Create empty GameObjects as spawn points
2. Position them around your camp spawn area
3. Add them to `GameStartManager.npcSpawnPoints` array
4. NPCs will cycle through these positions

If no spawn points are set, NPCs spawn in a circle around origin.

### Restart Difficulty
Adjust these in GameStartManager inspector:
- **Building Damage Percent**: Higher = more building damage (0.0 - 1.0)
- **Research Progress Loss**: Set to 1.0 to reset all progress
- **Clear All Building Queues**: Toggle to preserve/clear queues
- **Remove All Crops**: Toggle to preserve/clear farms

## Events

**GameStartManager Events:**
- `OnGameStartComplete`: Fires after new game sequence finishes
- `OnGameRestartComplete`: Fires after restart sequence finishes

**NPCManager Events:**
- `OnAllNPCsLost`: Fires when last NPC dies

Subscribe to these events if you need to trigger custom logic during game start/restart.

## Troubleshooting

**Menu doesn't show:**
- Check PlayerUIManager has gameStartMenu reference
- Verify CanvasGroup is on GameStartMenu object
- Check that GameStartMenu is inactive by default

**NPCs don't spawn:**
- Verify NPCManager has settler prefab configured
- Check name/description text files are assigned
- Ensure NavMesh is baked in camp scene

**Restart doesn't trigger:**
- Verify GameStartManager is in scene
- Check NPCManager.UnregisterNPC() is being called
- Look for "[NPCManager] All NPCs have been lost!" in console

**Buildings not damaged:**
- Check Building Damage Percent > 0
- Verify buildings have health component
- Ensure buildings are operational (not under construction)

## Future Enhancements

Potential additions:
- Customizable restart dialogue/story
- Different NPC spawn counts based on progression
- Save restart statistics (times restarted, etc.)
- Unlock bonuses after multiple restarts
- Optional permadeath mode (no restart)
- Rescue mission for original NPCs

