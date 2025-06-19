# Inventory Popup System Setup Guide

## Overview
The inventory popup system shows notifications when items are added to either the player's inventory or the possessed NPC's inventory. Popups appear in the bottom-right corner of the screen and stack vertically.

## Setup Steps

### 1. Create the Inventory Popup Item Prefab

1. Create a new UI GameObject in your scene
2. Add the following hierarchy:
   ```
   InventoryPopupItem (GameObject)
   ├── RectTransform (set to bottom-right anchor)
   ├── CanvasRenderer
   ├── Image (background - set to stretch)
   ├── InventoryPopupItem script
   └── Children:
       ├── ItemIcon (Image for item sprite)
       ├── ItemName (TextMeshPro - Text)
       ├── CountText (TextMeshPro - Text)
       └── InventoryType (TextMeshPro - Text)
   ```

3. Configure the RectTransform:
   - Anchor: Bottom-right corner
   - Pivot: (1, 0)
   - Size: ~200x60 pixels
   - Position: (0, 0) relative to anchor

4. Configure the UI elements:
   - **ItemIcon**: 40x40 pixels, left-aligned
   - **ItemName**: 16pt font, white text, center-right
   - **CountText**: 14pt font, white text, bold, top-right
   - **InventoryType**: 12pt font, bottom-right

5. Assign the UI elements to the InventoryPopupItem script fields

6. Create a prefab from this GameObject

### 2. Set up the AddedToInventoryPopup Component

1. Find or create the PlayerUIManager GameObject
2. Add the `AddedToInventoryPopup` component
3. Assign the popup item prefab to the `popupItemPrefab` field
4. Set the `popupContainer` to a Transform where popups should be instantiated
5. Configure the settings:
   - `popupDuration`: How long each popup stays visible (default: 3 seconds)
   - `popupSpacing`: Space between popups (default: 10 pixels)
   - `maxVisiblePopups`: Maximum number of popups shown at once (default: 5)
   - `slideInDuration`: Animation duration for sliding in (default: 0.3 seconds)
   - `slideOutDuration`: Animation duration for sliding out (default: 0.3 seconds)

### 3. Connect to PlayerUIManager

1. In the PlayerUIManager component, assign the `AddedToInventoryPopup` component to the `inventoryPopup` field

### 4. Test the System

The popup system will automatically trigger when:
- Items are picked up from chests/interactions
- Resources are gathered by NPCs
- Research tasks complete
- Items are transferred from NPC to player inventory
- Weapons are equipped
- Genetic mutations are added

## Visual Customization

### Colors
- Player inventory: Green background with white text
- NPC inventory: Orange background with white text

### Animation
- Popups slide in from the right
- Multiple popups stack from bottom to top
- Oldest popups are removed when limit is reached
- Popups slide out to the right when timer expires

## Troubleshooting

1. **Popups not appearing**: Check that the `inventoryPopup` field is assigned in PlayerUIManager
2. **UI elements not showing**: Verify that all UI element references are assigned in the InventoryPopupItem script
3. **Wrong positioning**: Ensure the RectTransform anchor and pivot are set correctly
4. **Performance issues**: Reduce `maxVisiblePopups` or increase `popupSpacing`

## Code Integration

The system is automatically integrated with:
- `PlayerInventory.AddToPlayerInventory()`
- `CharacterInventory.AddItem()`
- `RogueLiteManager.ReturnToCamp()`
- Various work tasks (Research, Gather, etc.)

No additional code changes are needed for basic functionality. 