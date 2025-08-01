using UnityEngine;
using UnityEditor;

/// <summary>
/// Base editor for RogueLiteRoom - provides common functionality for all room types
/// Since RogueLiteRoom is abstract, this editor won't be used directly but provides shared methods
/// </summary>
public class RogueLiteRoomEditorBase : Editor
{
    protected virtual void DrawCommonInspector()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Room Bounds Debug", EditorStyles.boldLabel);
        
        RogueLiteRoom room = (RogueLiteRoom)target;
        
        // Room type display (read-only since it's determined by class)
        EditorGUILayout.LabelField($"Room Type: {room.RoomType}", EditorStyles.helpBox);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Recalculate Bounds"))
        {
            room.CalculateRoomBounds();
            Debug.Log($"[RogueLiteRoom] Bounds recalculated for {room.gameObject.name}");
        }
        
        if (GUILayout.Button("Log Room Info"))
        {
            LogRoomInfo(room);
        }
        
        if (GUILayout.Button("Test Bounds Calculation"))
        {
            TestBoundsCalculation(room);
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Current Bounds Info:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Bounds Calculated: {room.GetBoundsCalculated()}");
        EditorGUILayout.LabelField($"Bounds Center: {room.GetWorldBounds().center}");
        EditorGUILayout.LabelField($"Bounds Size: {room.GetWorldBounds().size}");
        
        if (room.GetRoomColliders() != null)
        {
            EditorGUILayout.LabelField($"Colliders Found: {room.GetRoomColliders().Length}");
        }
    }
    
    protected void LogRoomInfo(RogueLiteRoom room)
    {
        Debug.Log($"[RogueLiteRoom] === Room Info for {room.gameObject.name} ===");
        Debug.Log($"Room Type: {room.RoomType}");
        Debug.Log($"Bounds Calculated: {room.GetBoundsCalculated()}");
        Debug.Log($"Current Bounds: Center={room.GetWorldBounds().center}, Size={room.GetWorldBounds().size}");
        Debug.Log($"Colliders Found: {(room.GetRoomColliders() != null ? room.GetRoomColliders().Length : 0)}");
        
        if (room.GetRoomColliders() != null)
        {
            for (int i = 0; i < room.GetRoomColliders().Length; i++)
            {
                var collider = room.GetRoomColliders()[i];
                if (collider != null)
                {
                    Debug.Log($"  Collider {i}: {collider.name} (enabled: {collider.enabled}) bounds: {collider.bounds}");
                }
            }
        }
    }
    
    protected void TestBoundsCalculation(RogueLiteRoom room)
    {
        Debug.Log($"[RogueLiteRoom] === Testing Bounds Calculation for {room.gameObject.name} ===");
        
        var colliders = room.GetComponentsInChildren<Collider>();
        Debug.Log($"Found {colliders.Length} colliders using GetComponentsInChildren");
        
        foreach (var collider in colliders)
        {
            Debug.Log($"Collider: {collider.name} at {collider.transform.position}, bounds: {collider.bounds}");
        }
    }
} 