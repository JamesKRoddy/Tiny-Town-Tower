using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RogueLikeBuildingDataScriptableObj))]
public class RogueLikeBuildingDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        RogueLikeBuildingDataScriptableObj buildingData = (RogueLikeBuildingDataScriptableObj)target;
        
        // Basic Settings
        DrawColoredSection("🏢 Basic Settings", () => {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("buildingType"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("buildingEntrance"));
        }, new Color(0.8f, 0.9f, 1f, 0.3f)); // Light blue
        
        // Building Parents
        DrawColoredSection("🏗️ Building Parents", () => {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("buildingParents"));
            EditorGUILayout.HelpBox("Parent structures that define the overall building layout and spawn points.", MessageType.Info);
        }, new Color(0.9f, 0.8f, 1f, 0.3f)); // Light purple
        
        // Hostile Rooms
        DrawColoredSection("⚔️ Hostile Rooms", () => {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("buildingRooms"));
            EditorGUILayout.LabelField($"Total Hostile Rooms: {buildingData.buildingRooms.Count}", EditorStyles.miniLabel);
        }, new Color(1f, 0.8f, 0.8f, 0.3f)); // Light red
        
        // Friendly Rooms
        DrawColoredSection("🏠 Friendly Rooms", () => {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("friendlyRooms"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("friendlyRoomSpawnChance"));
            EditorGUILayout.LabelField($"Total Friendly Rooms: {buildingData.friendlyRooms.Count}", EditorStyles.miniLabel);
            
            if (buildingData.friendlyRooms.Count > 0)
            {
                EditorGUILayout.HelpBox($"Friendly rooms have a {buildingData.GetFriendlyRoomSpawnChance()}% chance to spawn instead of hostile rooms.", MessageType.Info);
            }
        }, new Color(0.8f, 1f, 0.8f, 0.3f)); // Light green
        
        // Room Extenders
        DrawColoredSection("📐 Room Extenders", () => {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("extenderRooms"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("extenderSpawnChance"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxExtendersPerBuilding"));
            EditorGUILayout.LabelField($"Total Extender Rooms: {buildingData.extenderRooms.Count}", EditorStyles.miniLabel);
            
            if (buildingData.extenderRooms.Count > 0)
            {
                EditorGUILayout.HelpBox("Room extenders add additional spawn points to expand the building. They have a chance to spawn up to the maximum limit per building.", MessageType.Info);
            }
        }, new Color(0.9f, 1f, 0.8f, 0.3f)); // Light lime
        
        // NPC Configuration
        DrawColoredSection("👤 NPC Configuration", () => {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("buildingNPCs"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoSpawnNPCs"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("npcSpawnChance"));
            
            var settlerNPCs = buildingData.GetBuildingNPCs();
            if (settlerNPCs.Length == 0)
            {
                EditorGUILayout.HelpBox("No settler NPCs assigned. Friendly rooms in this building won't spawn NPCs.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField($"Total NPCs: {settlerNPCs.Length}", EditorStyles.miniLabel);
            }
        }, new Color(0.9f, 0.9f, 0.8f, 0.3f)); // Light yellow
        
        // Boss Configuration
        DrawColoredSection("👹 Boss Configuration", () => {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("possibleBosses"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bossSpawnChance"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("guaranteeBossAsEndRoom"));
            
            var possibleBosses = buildingData.GetPossibleBosses();
            if (possibleBosses.Length == 0)
            {
                EditorGUILayout.HelpBox("No bosses configured. This building will not spawn boss encounters.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField($"Total Bosses: {possibleBosses.Length}", EditorStyles.miniLabel);
                EditorGUILayout.HelpBox($"Bosses have a {buildingData.GetBossSpawnChance()}% chance to spawn at the end of the building.", MessageType.Info);
            }
        }, new Color(1f, 0.8f, 0.9f, 0.3f)); // Light pink
        
        // Room Settings
        DrawColoredSection("🔢 Room Settings", () => {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("minRoomCount"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxRoomCount"));
            EditorGUILayout.HelpBox("Defines the range of rooms that can spawn in this building. Actual count scales with difficulty.", MessageType.Info);
        }, new Color(0.8f, 0.9f, 1f, 0.3f)); // Light cyan
        
        // Draw separator
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space(10);
        
        // Summary Section
        DrawColoredSection("📊 Building Summary", () => {
            EditorGUILayout.LabelField($"Building Type: {buildingData.buildingType}", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);
            
            // Room counts
            EditorGUILayout.LabelField("Room Configuration:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"  • Hostile Rooms: {buildingData.buildingRooms.Count}");
            EditorGUILayout.LabelField($"  • Friendly Rooms: {buildingData.friendlyRooms.Count} ({buildingData.GetFriendlyRoomSpawnChance()}% spawn chance)");
            EditorGUILayout.LabelField($"  • Extender Rooms: {buildingData.extenderRooms.Count} ({buildingData.GetExtenderSpawnChance()}% spawn chance, max {buildingData.GetMaxExtendersPerBuilding()})");
            EditorGUILayout.LabelField($"  • Room Count Range: {buildingData.minRoomCount} - {buildingData.maxRoomCount}");
            
            EditorGUILayout.Space(3);
            
            // NPC info
            var settlerNPCs = buildingData.GetBuildingNPCs();
            EditorGUILayout.LabelField("NPC Configuration:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"  • Total NPCs: {settlerNPCs.Length}");
            EditorGUILayout.LabelField($"  • Auto Spawn: {(buildingData.GetAutoSpawnNPCs() ? "Yes" : "No")}");
            EditorGUILayout.LabelField($"  • Spawn Chance: {buildingData.GetNPCSpawnChance()}%");
            
            EditorGUILayout.Space(3);
            
            // Boss info
            var possibleBosses = buildingData.GetPossibleBosses();
            EditorGUILayout.LabelField("Boss Configuration:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"  • Total Bosses: {possibleBosses.Length}");
            EditorGUILayout.LabelField($"  • Spawn Chance: {buildingData.GetBossSpawnChance()}%");
            EditorGUILayout.LabelField($"  • Guarantee as End Room: {(buildingData.ShouldGuaranteeBossAsEndRoom() ? "Yes" : "No")}");
        }, new Color(0.9f, 0.9f, 1f, 0.3f)); // Light lavender
        
        serializedObject.ApplyModifiedProperties();
    }
    
    /// <summary>
    /// Draw a section with a colored background
    /// </summary>
    private void DrawColoredSection(string title, System.Action content, Color backgroundColor)
    {
        EditorGUILayout.Space(5);
        
        // Store original background color
        Color originalColor = GUI.backgroundColor;
        
        // Set background color
        GUI.backgroundColor = backgroundColor;
        
        // Draw section with colored background
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        content?.Invoke();
        EditorGUILayout.EndVertical();
        
        // Restore original background color
        GUI.backgroundColor = originalColor;
    }
} 