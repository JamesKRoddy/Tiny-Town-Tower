# WaveUI Prefab Setup Guide

This guide shows how to set up the WaveUI prefab structure in Unity.

## Prefab Structure

```
WaveUI (GameObject with WaveUI script)
├── WaveWarningPanel (GameObject)
│   ├── WarningBackground (Image)
│   ├── WarningText (TextMeshProUGUI)
│   └── CountdownText (TextMeshProUGUI)
├── WaveTimerPanel (GameObject)
│   ├── TimerBackground (Image)
│   ├── TimerText (TextMeshProUGUI)
│   ├── TimerSlider (Slider)
│   │   ├── Background (Image)
│   │   ├── Fill Area (GameObject)
│   │   │   └── Fill (Image) - This is the timerFillImage
│   │   └── Handle Slide Area (GameObject)
│   │       └── Handle (Image)
│   └── TimerLabel (TextMeshProUGUI)
└── WaveStatusPanel (GameObject)
    ├── StatusBackground (Image)
    ├── WaveStatusText (TextMeshProUGUI)
    └── EnemyCountText (TextMeshProUGUI)
```

## Component Settings

### WaveUI Script Settings
- **Warning Duration**: 3.0
- **Fade In Duration**: 0.5
- **Fade Out Duration**: 0.5
- **Warning Blink Interval**: 0.5
- **Timer Normal Color**: Green (0, 1, 0, 1)
- **Timer Warning Color**: Yellow (1, 1, 0, 1)
- **Timer Danger Color**: Red (1, 0, 0, 1)
- **Warning Color**: Red (1, 0, 0, 1)
- **Normal Color**: White (1, 1, 1, 1)

### Panel Settings
All panels should have:
- **Canvas Group** component for fade effects
- **RectTransform** with appropriate anchors
- **Image** component with semi-transparent background

### Text Settings
- **Font**: LiberationSans (or your preferred font)
- **Font Size**: 24-36 depending on importance
- **Color**: White or contrasting color
- **Alignment**: Center for warnings, Left for status

### Slider Settings
- **Min Value**: 0
- **Max Value**: 1
- **Value**: 0
- **Interactable**: false (this is display only)
- **Transition**: None
- **Navigation**: None

## Positioning

### WaveWarningPanel
- **Anchor**: Center
- **Position**: Center of screen
- **Size**: 800x200
- **Z-Order**: High (should be on top)

### WaveTimerPanel
- **Anchor**: Top Right
- **Position**: Top right corner with margin
- **Size**: 300x100
- **Z-Order**: Medium

### WaveStatusPanel
- **Anchor**: Top Left
- **Position**: Top left corner with margin
- **Size**: 300x100
- **Z-Order**: Medium

## Animation Setup

### Canvas Group Setup
Each panel should have a CanvasGroup component:
- **Alpha**: 0 (start invisible)
- **Interactable**: false
- **Blocks Raycasts**: false

### Image Setup for Blinking
The warning background should:
- **Color**: White (1, 1, 1, 1)
- **Material**: Default UI material
- **Raycast Target**: false (for performance)

## Integration Steps

1. **Create the prefab structure** as shown above
2. **Assign all references** in the WaveUI script inspector
3. **Add to PlayerUIManager** in the scene
4. **Test the wave system** using the debug menu
5. **Adjust positioning** and styling as needed

## Testing

Use the WaveTestUI component to test:
- Start wave button
- End wave button
- Clear enemies button
- Clear enemies with fade button

## Troubleshooting

### Common Issues
1. **Panels not showing**: Check CanvasGroup alpha values
2. **Text not updating**: Verify TextMeshProUGUI references
3. **Slider not working**: Check fill image reference
4. **Fade not working**: Ensure CanvasGroup components are present

### Debug Tips
- Use the Scene view to verify panel positions
- Check the Console for error messages
- Use the Inspector to verify all references are assigned
- Test with the debug menu (F1) for quick access 