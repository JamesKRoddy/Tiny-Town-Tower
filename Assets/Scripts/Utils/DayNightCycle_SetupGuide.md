# Day/Night Cycle System - Setup Guide

## Overview

The Day/Night Cycle system adds realistic time progression to your camp with the following features:

- **Dynamic lighting** changes between day/night
- **NPC behavior changes** - NPCs may sleep at night instead of working
- **Work efficiency modifiers** - reduced productivity during night hours
- **Visual transitions** with customizable colors and intensities
- **UI display** showing current time and progress

## Components

### 1. TimeManager
**Location**: `Assets/Scripts/Managers/CampManagers/TimeManager.cs`

Central controller for the day/night cycle. Manages time progression, lighting, and integrates with other camp systems.

### 2. SleepState
**Location**: `Assets/Scripts/Characters/NPCs/CampTasks/SleepState.cs`

New NPC task state that handles sleeping behavior during night time.

### 3. TimeDisplayUI
**Location**: `Assets/Scripts/UI/MiscMenus/TimeDisplayUI.cs`

UI component to display current time of day and progress to players.

## Setup Instructions

### Step 1: Add TimeManager to Camp Scene

1. In your camp scene, create a child GameObject under your CampManager
2. Name it "TimeManager"
3. Add the `TimeManager` component to this GameObject
4. Configure the TimeManager settings:

#### Time Settings
- **Day Duration**: How long daylight lasts (default: 300 seconds = 5 minutes)
- **Night Duration**: How long night lasts (default: 180 seconds = 3 minutes)
- **Transition Duration**: How long dawn/dusk transitions take (default: 30 seconds)
- **Auto Start Cycle**: Whether to start automatically when scene loads
- **Starting Time Of Day**: What time to start at (Dawn, Day, Dusk, Night)

#### Visual Settings
- **Sun Light**: Reference to your main directional light (sun)
- **Moon Light**: Reference to a secondary light for night (optional)
- **Sun Color Gradient**: Color transition from day to night
- **Sun Intensity Curve**: Brightness changes throughout the cycle
- **Moon Intensity Curve**: Moon brightness changes
- **Fog Color Day/Night**: Fog color transitions

#### Camp Effects
- **Night Work Efficiency Multiplier**: How much slower work is at night (default: 0.7)
- **Night Movement Speed Multiplier**: How much slower NPCs move at night (default: 0.8)
- **Enable Night Sleep Behavior**: Whether NPCs should sleep at night
- **Sleep Chance**: Probability that NPCs will sleep instead of work (default: 0.6)

### Step 2: Configure Lighting

1. **Sun Light Setup**:
   - Create or assign your main directional light as the Sun Light
   - Set up a color gradient from warm day colors to cool night colors
   - Configure intensity curve: bright during day, dim at night

2. **Moon Light Setup** (Optional):
   - Create a second directional light for moonlight
   - Position it opposite to the sun
   - Set a cool blue/white color
   - Initially disable it (TimeManager will control it)

### Step 3: Add SleepState to NPCs

1. Open your SettlerNPC prefab
2. Add the `SleepState` component to the prefab
3. Configure sleep settings:
   - **Sleep Search Radius**: How far NPCs look for sleep locations
   - **Sleep Location Check Radius**: Area around sleep spots to check for availability
   - **Sleep Animation Trigger**: Animation trigger name for sleeping

### Step 4: Set Up Sleep Locations

NPCs will automatically find sleep locations, but you can optimize this:

1. **Method 1 - Tags**: Tag objects with "SleepLocation" for beds, sleeping bags, etc.
2. **Method 2 - Buildings**: NPCs will use houses, barracks, or shelters automatically
3. **Method 3 - Fallback**: NPCs will sleep in place if no locations found

### Step 5: Add Time Display UI

1. In your camp UI canvas, create a new GameObject for the time display
2. Add the `TimeDisplayUI` component
3. Set up UI references:
   - **Time Of Day Text**: TextMeshPro component for current time
   - **Time Remaining Text**: TextMeshPro component for countdown
   - **Time Progress Bar**: Image component with "Filled" type
   - **Time Of Day Icon**: Image component for visual time indicator
   - **Canvas Group**: For fade effects (optional)

4. Configure time of day icons:
   - **Dawn Icon**: Orange/yellow icon
   - **Day Icon**: Bright sun icon
   - **Dusk Icon**: Orange/red icon
   - **Night Icon**: Moon/stars icon

5. Set display options:
   - **Show Time Remaining**: Display countdown timer
   - **Show Progress Bar**: Visual progress indicator
   - **Auto Fade In Out**: Fade effect on time changes

## Integration with Existing Systems

### CampManager Integration
The TimeManager is automatically integrated with CampManager and will be found as a child component.

### WorkManager Integration
- Work assignment now checks for night time and may assign sleep instead
- Work efficiency is reduced during night hours
- NPCs may prioritize sleep over work during night

### NPC Behavior Changes
- NPCs can now transition to SLEEP task type during night
- Sleep behavior includes finding sleep locations and rest animation
- NPCs wake up automatically when day starts
- Stamina restoration is faster while sleeping

## Events and Scripting

The TimeManager provides several events you can subscribe to:

```csharp
// Subscribe to time events
TimeManager.OnTimeOfDayChanged += OnTimeChanged;
TimeManager.OnDayStarted += OnDayStarted;
TimeManager.OnNightStarted += OnNightStarted;
TimeManager.OnNightWorkEfficiencyChanged += OnEfficiencyChanged;

private void OnTimeChanged(TimeOfDay newTime)
{
    Debug.Log($"Time changed to: {newTime}");
}
```

## Customization Options

### Custom Work Tasks
You can create work tasks that behave differently during night:

```csharp
public override bool CanPerformTask()
{
    // Some tasks might only work during day
    if (requiresDaylight && CampManager.Instance.TimeManager.IsNight)
        return false;
        
    return base.CanPerformTask();
}
```

### Custom Sleep Locations
Tag any GameObject with "SleepLocation" to make it a valid sleep spot for NPCs.

### Visual Customization
- Adjust color gradients and intensity curves for different atmospheres
- Add particle effects that react to time changes
- Customize fog colors for different weather conditions

## Troubleshooting

### NPCs Not Sleeping
- Check that SleepState component is added to NPC prefabs
- Verify that "Enable Night Sleep Behavior" is enabled on TimeManager
- Ensure Sleep Chance is greater than 0

### Lighting Not Changing
- Verify Sun Light reference is assigned
- Check that color gradient and intensity curves are configured
- Make sure the light isn't controlled by other scripts

### Performance Issues
- Reduce the number of sleep location checks by using fewer tagged objects
- Adjust cache refresh intervals in SleepState
- Consider using object pooling for large numbers of NPCs

### Time Not Progressing
- Check that "Auto Start Cycle" is enabled
- Verify TimeManager is active and not paused
- Look for error messages in the console

## Tips for Best Results

1. **Balance day/night durations** based on your gameplay needs
2. **Use transition periods** to create smooth lighting changes
3. **Place sleep locations strategically** near work areas
4. **Test with different NPC counts** to ensure good performance
5. **Consider seasonal variations** by adjusting color gradients
6. **Implement weather effects** that interact with time of day

This system integrates seamlessly with your existing camp management architecture and provides a foundation for more advanced time-based gameplay mechanics.
