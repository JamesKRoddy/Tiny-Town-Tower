using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool to snap selected GameObjects to the grid based on Unity's snap settings.
/// Use Ctrl+Alt+S (or Edit > Snap to Grid) to snap all selected objects.
/// </summary>
public class SnapToGrid : EditorWindow
{
    [MenuItem("Edit/Snap to Grid %&S")] // Ctrl+Alt+S shortcut
    static void Snap()
    {
        if (Selection.transforms.Length == 0)
        {
            Debug.LogWarning("No GameObjects selected. Please select objects to snap to grid.");
            return;
        }

        int snappedCount = 0;
        
        foreach (Transform transform in Selection.transforms)
        {
            Vector3 pos = transform.position;
            Vector3 newPos = new Vector3(
                Mathf.Round(pos.x / EditorSnapSettings.move.x) * EditorSnapSettings.move.x,
                Mathf.Round(pos.y / EditorSnapSettings.move.y) * EditorSnapSettings.move.y,
                Mathf.Round(pos.z / EditorSnapSettings.move.z) * EditorSnapSettings.move.z
            );
            
            if (pos != newPos)
            {
                Undo.RecordObject(transform, "Snap to Grid");
                transform.position = newPos;
                snappedCount++;
            }
        }

        if (snappedCount > 0)
        {
            Debug.Log($"Snapped {snappedCount} GameObject(s) to grid with snap settings: {EditorSnapSettings.move}");
        }
        else
        {
            Debug.Log("All selected objects are already aligned to grid.");
        }
    }

    [MenuItem("Edit/Snap to Grid %&S", true)]
    static bool SnapValidation()
    {
        // Only enable the menu item if we have transforms selected
        return Selection.transforms.Length > 0;
    }
}
