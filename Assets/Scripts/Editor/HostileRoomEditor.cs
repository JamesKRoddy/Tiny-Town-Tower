using UnityEngine;
using UnityEditor;
using Enemies;

[CustomEditor(typeof(HostileRoom))]
public class HostileRoomEditor : RogueLiteRoomEditorBase
{
    public override void OnInspectorGUI()
    {
        DrawCommonInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Hostile Room Specific", EditorStyles.boldLabel);
        
        HostileRoom hostileRoom = (HostileRoom)target;
        
        // Enemy spawn points info - check both cached and direct search
        var cachedSpawnPoints = hostileRoom.GetEnemySpawnPoints();
        var directSpawnPoints = hostileRoom.GetComponentsInChildren<EnemySpawnPoint>();
        
        EditorGUILayout.LabelField($"Cached Enemy Spawn Points: {cachedSpawnPoints.Length}");
        EditorGUILayout.LabelField($"Direct Search Found: {directSpawnPoints.Length}");
        
        // Show warning if there's a mismatch
        if (cachedSpawnPoints.Length != directSpawnPoints.Length)
        {
            EditorGUILayout.HelpBox($"Mismatch! Cached: {cachedSpawnPoints.Length}, Direct: {directSpawnPoints.Length}. " +
                                  "The HostileRoom may need to refresh its cache. Try 'Refresh Spawn Points Cache' button.", 
                                  MessageType.Warning);
        }
        
        // Use the direct search result for display (more reliable)
        var spawnPoints = directSpawnPoints;
        
        if (spawnPoints.Length > 0)
        {
            EditorGUILayout.LabelField($"Has Spawn Points: ✓", EditorStyles.helpBox);
            
            // List all spawn points
            EditorGUI.indentLevel++;
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null)
                {
                    EditorGUILayout.LabelField($"Spawn Point {i + 1}: {spawnPoints[i].name}");
                }
                else
                {
                    EditorGUILayout.LabelField($"Spawn Point {i + 1}: NULL", EditorStyles.helpBox);
                }
            }
            EditorGUI.indentLevel--;
        }
        else
        {
            EditorGUILayout.LabelField("⚠️ No enemy spawn points found!", EditorStyles.helpBox);
            EditorGUILayout.LabelField("Add EnemySpawnPoint components as children", EditorStyles.miniLabel);
        }
        
        EditorGUILayout.Space();
        
        // Hostile room specific buttons
        if (GUILayout.Button("Refresh Spawn Points Cache"))
        {
            RefreshSpawnPointsCache(hostileRoom);
        }
        
        if (GUILayout.Button("Find Enemy Spawn Points"))
        {
            FindEnemySpawnPoints(hostileRoom);
        }
        
        if (GUILayout.Button("Log Enemy Spawn Info"))
        {
            LogEnemySpawnInfo(hostileRoom);
        }
        
        if (spawnPoints.Length > 0)
        {
            if (GUILayout.Button("Select All Spawn Points"))
            {
                SelectAllSpawnPoints(hostileRoom);
            }
        }
        
        // Warning if no spawn points
        if (spawnPoints.Length == 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("This hostile room has no enemy spawn points. Enemies won't be able to spawn here. " +
                                  "Add child GameObjects with EnemySpawnPoint components.", MessageType.Warning);
            
            if (GUILayout.Button("Create Enemy Spawn Point"))
            {
                CreateEnemySpawnPoint(hostileRoom);
            }
        }
    }
    
    private void RefreshSpawnPointsCache(HostileRoom hostileRoom)
    {
        // Call the public refresh method on HostileRoom
        hostileRoom.RefreshEnemySpawnPoints();
        
        var cachedSpawnPoints = hostileRoom.GetEnemySpawnPoints();
        Debug.Log($"[HostileRoomEditor] Refreshed spawn points cache for {hostileRoom.gameObject.name}");
        Debug.Log($"[HostileRoomEditor] Cache now contains {cachedSpawnPoints.Length} enemy spawn points");
        
        // Force Unity to repaint the inspector
        EditorUtility.SetDirty(hostileRoom);
        Repaint();
    }
    
    private void FindEnemySpawnPoints(HostileRoom hostileRoom)
    {
        Debug.Log($"[HostileRoomEditor] === Searching for Enemy Spawn Points in {hostileRoom.gameObject.name} ===");
        
        // Direct search
        var directSpawnPoints = hostileRoom.GetComponentsInChildren<EnemySpawnPoint>();
        Debug.Log($"Direct search found: {directSpawnPoints.Length} enemy spawn points");
        
        // Cached search
        var cachedSpawnPoints = hostileRoom.GetEnemySpawnPoints();
        Debug.Log($"Cached search returned: {cachedSpawnPoints.Length} enemy spawn points");
        
        // List all direct spawn points
        for (int i = 0; i < directSpawnPoints.Length; i++)
        {
            var spawnPoint = directSpawnPoints[i];
            Debug.Log($"  Direct Spawn Point {i + 1}: {spawnPoint.name} at {spawnPoint.transform.position}");
            Debug.Log($"    - Parent: {spawnPoint.transform.parent?.name ?? "None"}");
            Debug.Log($"    - Active: {spawnPoint.gameObject.activeInHierarchy}");
        }
        
        // Check if any child objects look like spawn points but don't have the component
        var allChildren = hostileRoom.GetComponentsInChildren<Transform>();
        var potentialSpawnPoints = 0;
        foreach (var child in allChildren)
        {
            if (child != hostileRoom.transform && 
                (child.name.ToLower().Contains("spawn") || child.name.ToLower().Contains("enemy")))
            {
                potentialSpawnPoints++;
                var hasComponent = child.GetComponent<EnemySpawnPoint>() != null;
                Debug.Log($"  Potential spawn point: {child.name} (Has EnemySpawnPoint: {hasComponent})");
            }
        }
        
        if (potentialSpawnPoints > directSpawnPoints.Length)
        {
            Debug.LogWarning($"Found {potentialSpawnPoints} potential spawn points but only {directSpawnPoints.Length} have EnemySpawnPoint components!");
        }
    }
    
    private void LogEnemySpawnInfo(HostileRoom hostileRoom)
    {
        Debug.Log($"[HostileRoomEditor] === Enemy Spawn Info for {hostileRoom.gameObject.name} ===");
        Debug.Log($"Room Type: {hostileRoom.RoomType}");
        Debug.Log($"Has Enemy Spawn Points: {hostileRoom.HasEnemySpawnPoints()}");
        Debug.Log($"Enemy Spawn Points Count: {hostileRoom.GetEnemySpawnPoints().Length}");
        
        var spawnPoints = hostileRoom.GetEnemySpawnPoints();
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
            {
                Debug.Log($"  Spawn Point {i + 1}: {spawnPoints[i].name} at {spawnPoints[i].transform.position}");
            }
        }
    }
    
    private void SelectAllSpawnPoints(HostileRoom hostileRoom)
    {
        var spawnPoints = hostileRoom.GetEnemySpawnPoints();
        GameObject[] spawnPointObjects = new GameObject[spawnPoints.Length];
        
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
            {
                spawnPointObjects[i] = spawnPoints[i].gameObject;
            }
        }
        
        Selection.objects = spawnPointObjects;
        Debug.Log($"[HostileRoomEditor] Selected {spawnPoints.Length} enemy spawn points");
    }
    
    private void CreateEnemySpawnPoint(HostileRoom hostileRoom)
    {
        GameObject spawnPointObj = new GameObject("EnemySpawnPoint");
        spawnPointObj.transform.SetParent(hostileRoom.transform);
        spawnPointObj.transform.localPosition = Vector3.zero;
        
        // Add EnemySpawnPoint component (assuming it exists)
        var spawnPoint = spawnPointObj.AddComponent<EnemySpawnPoint>();
        
        // Select the new spawn point
        Selection.activeGameObject = spawnPointObj;
        
        // Mark scene dirty
        EditorUtility.SetDirty(hostileRoom.gameObject);
        
        Debug.Log($"[HostileRoomEditor] Created new enemy spawn point in {hostileRoom.gameObject.name}");
    }
} 