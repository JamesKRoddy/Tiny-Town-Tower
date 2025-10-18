using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RogueLikeBuildingDataScriptableObj))]
public class RogueLikeBuildingDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Building Summary", EditorStyles.boldLabel);
        
        RogueLikeBuildingDataScriptableObj buildingData = (RogueLikeBuildingDataScriptableObj)target;
        
        // Building info
        EditorGUILayout.LabelField($"Building Type: {buildingData.buildingType}");
        EditorGUILayout.LabelField($"Hostile Rooms: {buildingData.buildingRooms.Count}");
        EditorGUILayout.LabelField($"Friendly Rooms: {buildingData.friendlyRooms.Count}");
        EditorGUILayout.LabelField($"Friendly Room Spawn Chance: {buildingData.GetFriendlyRoomSpawnChance()}%");
        
        // Settler NPCs info
        var settlerNPCs = buildingData.GetBuildingNPCs();
        EditorGUILayout.LabelField($"Settler NPCs: {settlerNPCs.Length}");
        EditorGUILayout.LabelField($"Auto Spawn NPCs: {(buildingData.GetAutoSpawnNPCs() ? "Yes" : "No")}");
        EditorGUILayout.LabelField($"NPC Spawn Chance: {buildingData.GetNPCSpawnChance()}%");
        
        if (settlerNPCs.Length == 0)
        {
            EditorGUILayout.HelpBox("No settler NPCs assigned. Friendly rooms in this building won't spawn NPCs.", MessageType.Warning);
        }
        
    }
} 