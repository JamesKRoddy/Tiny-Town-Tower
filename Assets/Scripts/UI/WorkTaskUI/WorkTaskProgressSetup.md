# Work Task Progress Bar Setup Guide

This guide shows how to set up the work task progress bar system in Unity.

## Overview

The system consists of:
- `WorkTaskProgressBar.cs` - The individual progress bar component
- Progress bar management integrated into `WorkManager.cs`
- Integration into `WorkTask.cs` and `QueuedWorkTask.cs`

## Setup Instructions

### 1. Create Progress Bar Prefab

Create a new GameObject with the following structure:

```
WorkTaskProgressBar (GameObject with WorkTaskProgressBar script)
├── Background (Image)
├── ProgressSlider (Slider)
│   ├── Background (Image)
│   ├── Fill Area (GameObject)
│   │   └── Fill (Image) - Assign to progressFill
│   └── Handle Slide Area (GameObject) - Disable if not needed
│       └── Handle (Image) - Disable if not needed
├── TaskNameText (TextMeshProUGUI)
└── ProgressText (TextMeshProUGUI)

Note: CanvasGroup component will be added automatically at runtime
```

### 2. Configure WorkTaskProgressBar Component

- **Progress Slider**: Assign the Slider component
- **Progress Fill**: Assign the Fill Image from the slider
- **Task Name Text**: Assign the TextMeshProUGUI for task name
- **Progress Text**: Assign the TextMeshProUGUI for percentage
- **Canvas Group**: Will be created automatically at runtime (no need to assign)

#### Visual Settings:
- **Normal Progress Color**: Green (0, 1, 0, 1)
- **Paused Progress Color**: Yellow (1, 1, 0, 1)
- **Error Progress Color**: Red (1, 0, 0, 1)
- **Fade In Duration**: 0.3 seconds
- **Fade Out Duration**: 0.3 seconds
- **Offset From Task**: (0, 2, 0) - Positions the bar 2 units above the task

### 3. Configure PlayerUIManager

The progress bars will be automatically parented to the PlayerUIManager's Canvas.

1. **Find PlayerUIManager**: Look for the PlayerUIManager GameObject in your scene
2. **Progress Bar Parent**: The system will automatically create a "ProgressBars" child transform
3. **No Manual Setup Required**: Everything is handled automatically

### 4. Configure WorkManager

In the WorkManager component, configure the "Progress Bar Settings" section:
- **Progress Bar Prefab**: Assign the prefab created in step 1
- **Max Active Progress Bars**: 10 (adjustable based on performance needs)

**Note**: The system will automatically use PlayerUIManager's Canvas for progress bars.

### 5. Enable Progress Bars on Work Tasks

Each WorkTask now has a "Progress Display" section in the inspector:
- **Show Progress Bar**: Check this to enable progress bars for the task

## How It Works

1. When a worker starts working on a task with `showProgressBar = true`, a progress bar appears above the task
2. The progress bar updates in real-time as work progresses
3. Different colors indicate different states:
   - **Green**: Normal working
   - **Yellow**: Paused (worker can't work efficiently)
   - **Red**: Error state
4. The progress bar automatically hides when work is complete or interrupted
5. For QueuedWorkTasks, the progress bar resets to 0% when starting a new queued item

## Performance Considerations

- Progress bars use object pooling to reduce memory allocations
- Maximum number of active progress bars is limited (default: 10)
- Progress bars automatically hide when the task is too far from the camera
- Uses world-to-screen positioning for efficient rendering

## Customization

### Task Name Display
The system automatically generates user-friendly names:
- ResourceUpgradeTask → "Upgrading [Resource Name]"
- CookingTask → "Cooking [Recipe Name]"  
- ResearchTask → "Researching [Research Name]"
- Other tasks → Converts class name to readable format

### Visual Customization
- Adjust colors in the WorkTaskProgressBar component
- Modify fade durations for different animation speeds
- Change offset values to position bars differently relative to tasks
- Customize the prefab design for different visual styles

## Troubleshooting

### Progress Bar Not Showing
1. Check that `showProgressBar` is enabled on the WorkTask
2. Verify WorkTaskProgressManager exists in the scene
3. Ensure the progress bar prefab is assigned to the manager
4. Check that the Canvas is properly configured

### Performance Issues
1. Reduce `maxActiveProgressBars` in WorkTaskProgressManager
2. Increase fade out duration to hide bars sooner
3. Reduce the distance threshold for hiding distant progress bars

### Progress Not Updating
1. Check that the WorkTask is properly calling `DoWork()`
2. Verify that `baseWorkTime` is set correctly on the task
3. Ensure the task is operational and has electricity if required
