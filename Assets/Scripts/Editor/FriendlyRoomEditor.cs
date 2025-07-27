using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FriendlyRoom))]
public class FriendlyRoomEditor : RogueLiteRoomEditorBase
{
    public override void OnInspectorGUI()
    {
        DrawCommonInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Friendly Room Specific", EditorStyles.boldLabel);
        
        FriendlyRoom friendlyRoom = (FriendlyRoom)target;
        
        // NPC spawn points info
        var npcSpawnPoints = friendlyRoom.GetNPCSpawnPoints();
        EditorGUILayout.LabelField($"NPC Spawn Points: {npcSpawnPoints.Length}");
        
        if (npcSpawnPoints.Length > 0)
        {
            EditorGUILayout.LabelField($"Has NPC Spawn Points: ✓", EditorStyles.helpBox);
            
            // List all NPC spawn points
            EditorGUI.indentLevel++;
            for (int i = 0; i < npcSpawnPoints.Length; i++)
            {
                if (npcSpawnPoints[i] != null)
                {
                    EditorGUILayout.LabelField($"NPC Spawn {i + 1}: {npcSpawnPoints[i].name}");
                }
                else
                {
                    EditorGUILayout.LabelField($"NPC Spawn {i + 1}: NULL", EditorStyles.helpBox);
                }
            }
            EditorGUI.indentLevel--;
        }
        else
        {
            EditorGUILayout.LabelField("⚠️ No NPC spawn points found!", EditorStyles.helpBox);
            EditorGUILayout.LabelField("Add child objects with 'NPCSpawn' in the name", EditorStyles.miniLabel);
        }
        
        // Spawned NPCs info
        var spawnedNPCs = friendlyRoom.GetSpawnedNPCs();
        int activeNPCs = 0;
        foreach (var npc in spawnedNPCs)
        {
            if (npc != null) activeNPCs++;
        }
        EditorGUILayout.LabelField($"Currently Spawned NPCs: {activeNPCs}");
        
        EditorGUILayout.Space();
        
        // Friendly room specific buttons
        if (GUILayout.Button("Find NPC Spawn Points"))
        {
            FindNPCSpawnPoints(friendlyRoom);
        }
        
        if (GUILayout.Button("Log NPC Spawn Info"))
        {
            LogNPCSpawnInfo(friendlyRoom);
        }
        
        if (npcSpawnPoints.Length > 0)
        {
            if (GUILayout.Button("Select All NPC Spawn Points"))
            {
                SelectAllNPCSpawnPoints(friendlyRoom);
            }
        }
        
        EditorGUILayout.Space();
        
        // NPC management buttons (only in play mode)
        EditorGUI.BeginDisabledGroup(!Application.isPlaying);
        
        if (GUILayout.Button("Spawn NPCs"))
        {
            friendlyRoom.SpawnNPCs();
            Debug.Log($"[FriendlyRoomEditor] Spawned NPCs in {friendlyRoom.gameObject.name}");
        }
        
        if (GUILayout.Button("Clear NPCs"))
        {
            friendlyRoom.ClearNPCs();
            Debug.Log($"[FriendlyRoomEditor] Cleared NPCs from {friendlyRoom.gameObject.name}");
        }
        
        EditorGUI.EndDisabledGroup();
        
        if (!Application.isPlaying)
        {
            EditorGUILayout.LabelField("NPC spawn/clear buttons only work in play mode", EditorStyles.miniLabel);
        }
        
        // Warning if no spawn points
        if (npcSpawnPoints.Length == 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("This friendly room has no NPC spawn points. NPCs won't be able to spawn here. " +
                                  "Add child GameObjects with 'NPCSpawn' in the name.", MessageType.Warning);
            
            if (GUILayout.Button("Create NPC Spawn Point"))
            {
                CreateNPCSpawnPoint(friendlyRoom);
            }
        }
        
        // Ambient effects info
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Ambient Effects", EditorStyles.boldLabel);
        
        var particleSystems = friendlyRoom.GetComponentsInChildren<ParticleSystem>();
        EditorGUILayout.LabelField($"Particle Systems Found: {particleSystems.Length}");
        
        if (particleSystems.Length > 0)
        {
            if (GUILayout.Button("Select All Particle Systems"))
            {
                SelectAllParticleSystems(friendlyRoom);
            }
        }
    }
    
    private void FindNPCSpawnPoints(FriendlyRoom friendlyRoom)
    {
        var spawnPoints = friendlyRoom.GetNPCSpawnPoints();
        Debug.Log($"[FriendlyRoomEditor] Found {spawnPoints.Length} NPC spawn points in {friendlyRoom.gameObject.name}");
        
        foreach (var spawnPoint in spawnPoints)
        {
            if (spawnPoint != null)
            {
                Debug.Log($"  - {spawnPoint.name} at {spawnPoint.position}");
            }
        }
    }
    
    private void LogNPCSpawnInfo(FriendlyRoom friendlyRoom)
    {
        Debug.Log($"[FriendlyRoomEditor] === NPC Spawn Info for {friendlyRoom.gameObject.name} ===");
        Debug.Log($"Room Type: {friendlyRoom.RoomType}");
        Debug.Log($"Has NPC Spawn Points: {friendlyRoom.HasNPCSpawnPoints()}");
        Debug.Log($"NPC Spawn Points Count: {friendlyRoom.GetNPCSpawnPoints().Length}");
        
        var spawnPoints = friendlyRoom.GetNPCSpawnPoints();
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
            {
                Debug.Log($"  NPC Spawn Point {i + 1}: {spawnPoints[i].name} at {spawnPoints[i].position}");
            }
        }
        
        var spawnedNPCs = friendlyRoom.GetSpawnedNPCs();
        int activeNPCs = 0;
        for (int i = 0; i < spawnedNPCs.Length; i++)
        {
            if (spawnedNPCs[i] != null)
            {
                activeNPCs++;
                Debug.Log($"  Spawned NPC {activeNPCs}: {spawnedNPCs[i].name}");
            }
        }
        Debug.Log($"Total Active NPCs: {activeNPCs}");
    }
    
    private void SelectAllNPCSpawnPoints(FriendlyRoom friendlyRoom)
    {
        var spawnPoints = friendlyRoom.GetNPCSpawnPoints();
        GameObject[] spawnPointObjects = new GameObject[spawnPoints.Length];
        
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
            {
                spawnPointObjects[i] = spawnPoints[i].gameObject;
            }
        }
        
        Selection.objects = spawnPointObjects;
        Debug.Log($"[FriendlyRoomEditor] Selected {spawnPoints.Length} NPC spawn points");
    }
    
    private void SelectAllParticleSystems(FriendlyRoom friendlyRoom)
    {
        var particleSystems = friendlyRoom.GetComponentsInChildren<ParticleSystem>();
        GameObject[] particleObjects = new GameObject[particleSystems.Length];
        
        for (int i = 0; i < particleSystems.Length; i++)
        {
            if (particleSystems[i] != null)
            {
                particleObjects[i] = particleSystems[i].gameObject;
            }
        }
        
        Selection.objects = particleObjects;
        Debug.Log($"[FriendlyRoomEditor] Selected {particleSystems.Length} particle systems");
    }
    
    private void CreateNPCSpawnPoint(FriendlyRoom friendlyRoom)
    {
        GameObject spawnPointObj = new GameObject("NPCSpawnPoint");
        spawnPointObj.transform.SetParent(friendlyRoom.transform);
        spawnPointObj.transform.localPosition = Vector3.zero;
        
        // Add a simple marker component or tag
        spawnPointObj.tag = "NPCSpawn"; // If you have this tag defined
        
        // Select the new spawn point
        Selection.activeGameObject = spawnPointObj;
        
        // Mark scene dirty
        EditorUtility.SetDirty(friendlyRoom.gameObject);
        
        Debug.Log($"[FriendlyRoomEditor] Created new NPC spawn point in {friendlyRoom.gameObject.name}");
    }
} 