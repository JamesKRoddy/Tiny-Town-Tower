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
        
        if (npcSpawnPoints.Length == 0)
        {
            EditorGUILayout.HelpBox("No NPC spawn points found. Add child objects with 'NPCSpawn' in the name.", MessageType.Warning);
        }
        
        // Settler NPCs info (from building data)
        var settlerNPCs = friendlyRoom.GetBuildingNPCs();
        EditorGUILayout.LabelField($"Settler NPCs (from building): {settlerNPCs.Length}");
        
        if (settlerNPCs.Length == 0)
        {
            EditorGUILayout.HelpBox("No settler NPCs available from building data. Configure settler NPCs in the RogueLikeBuildingDataScriptableObj.", MessageType.Warning);
        }
        
        // Spawned NPCs info (runtime only)
        if (Application.isPlaying)
        {
            var spawnedNPCs = friendlyRoom.GetSpawnedNPCs();
            int activeNPCs = 0;
            foreach (var npc in spawnedNPCs)
            {
                if (npc != null) activeNPCs++;
            }
            EditorGUILayout.LabelField($"Currently Spawned NPCs: {activeNPCs}");
        }
    }
} 