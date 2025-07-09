# Wave System Implementation

This document describes the full wave loop system implementation for the camp attack mode.

## Overview

The wave system provides:
- **UI overlay** to display that an attack is coming to the camp
- **UI timer** to display time until the wave is over
- **Wave completion** either by killing all zombies or time out
- **Enemy clearing** with fade out effect
- **Automatic wave looping** with configurable delays

## Components

### 1. WaveUI (Assets/Scripts/UI/GameUI/WaveUI.cs)
The main UI component that handles wave display and timing.

**Features:**
- Attack warning overlay with blinking effect
- Countdown timer with color-coded progress
- Enemy count display
- Smooth fade in/out animations

**Required UI Elements:**
- `waveWarningPanel` - Panel for attack warning
- `warningText` - Text for "ATTACK INCOMING!"
- `countdownText` - Text for countdown
- `warningBackground` - Background image for blinking effect
- `waveTimerPanel` - Panel for timer display
- `timerText` - Text for time remaining
- `timerSlider` - Progress bar for time
- `timerFillImage` - Fill image for color changes
- `waveStatusPanel` - Panel for status information
- `waveStatusText` - Text for wave status
- `enemyCountText` - Text for enemy count

### 2. Enhanced CampManager (Assets/Scripts/Managers/CampManager.cs)
Enhanced with wave timing and looping capabilities.

**New Features:**
- Wave timing with configurable duration
- Automatic wave looping
- Wave completion detection (time or enemies cleared)
- Enemy fade-out clearing
- Wave loop events

**Configuration:**
- `waveLoopDelay` - Delay between waves (default: 5s)
- `enableWaveLooping` - Whether waves should loop (default: true)
- `maxWavesPerLoop` - Maximum waves before longer break (default: 3)

### 3. Enhanced EnemySpawnManager (Assets/Scripts/Managers/EnemySpawnManager.cs)
Modified to support camp wave completion logic.

**Changes:**
- Camp waves don't automatically start next wave
- Wave completion is handled by CampManager timer
- Supports both time-based and enemy-clear completion

### 4. WaveTestUI (Assets/Scripts/UI/GameUI/WaveTestUI.cs)
Test component for debugging and testing the wave system.

**Features:**
- Start/end wave buttons
- Clear enemies (instant or fade)
- Wave loop toggle
- Wave delay adjustment
- Real-time status display

## Setup Instructions

### 1. UI Setup
1. Add the `WaveUI` component to your UI canvas
2. Create the required UI panels and elements
3. Assign all the serialized fields in the inspector
4. Add the `WaveUI` reference to `PlayerUIManager`

### 2. Wave Configuration
1. Create a `CampEnemyWaveConfig` ScriptableObject
2. Set the wave duration and other parameters
3. Assign enemy prefabs
4. Assign the config to the `CampManager`

### 3. Testing
1. Add the `WaveTestUI` component to a test panel
2. Set up the test controls
3. Use the debug menu (F1) to test wave functionality

## Usage

### Starting a Wave
```csharp
CampManager.Instance.StartCampWave();
```

### Ending a Wave
```csharp
CampManager.Instance.ForceEndCampWave();
```

### Clearing Enemies
```csharp
// Instant clear
CampManager.Instance.ClearAllEnemies();

// Fade out clear
CampManager.Instance.ClearAllEnemiesWithFade();
```

### Wave Events
```csharp
// Subscribe to wave events
CampManager.Instance.OnCampWaveStarted += OnWaveStarted;
CampManager.Instance.OnCampWaveEnded += OnWaveEnded;
CampManager.Instance.OnWaveLoopComplete += OnWaveLoopComplete;
```

## Wave Flow

1. **Wave Start**: `StartCampWave()` is called
2. **Warning Phase**: UI shows "ATTACK INCOMING!" with countdown
3. **Wave Active**: Enemies spawn, timer runs, UI shows progress
4. **Wave End**: Either all enemies defeated or time expired
5. **Loop**: If enabled, waits for delay then starts next wave

## Configuration Options

### Wave Timing
- **Warning Duration**: How long the warning phase lasts
- **Wave Duration**: How long each wave lasts
- **Loop Delay**: Delay between waves in a loop
- **Max Waves Per Loop**: How many waves before a longer break

### UI Animation
- **Fade Duration**: How long fade in/out takes
- **Blink Interval**: How fast the warning blinks
- **Timer Colors**: Color coding for time remaining

### Enemy Management
- **Spawn Points**: Where enemies spawn from
- **Enemy Types**: Which enemies spawn in each wave
- **Spawn Count**: Min/max enemies per wave

## Troubleshooting

### Common Issues
1. **Wave not starting**: Check if `CampManager` is in scene
2. **UI not showing**: Verify `WaveUI` is assigned to `PlayerUIManager`
3. **Enemies not spawning**: Check spawn points and wave config
4. **Timer not working**: Ensure wave config has duration set

### Debug Tools
- Use `WaveTestUI` for testing
- Check console for debug messages
- Use F1 debug menu for quick access
- Monitor `CampManager.IsWaveActive` for wave state

## Future Enhancements

- **Difficulty scaling**: Increase difficulty over time
- **Wave rewards**: Give rewards for completing waves
- **Wave types**: Different wave patterns (boss waves, etc.)
- **Player progression**: Unlock new wave types
- **Wave persistence**: Save wave progress between sessions 