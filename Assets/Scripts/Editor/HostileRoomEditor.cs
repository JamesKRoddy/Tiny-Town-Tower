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
        
        // Enemy spawn points info
        var spawnPoints = hostileRoom.GetEnemySpawnPoints();
        EditorGUILayout.LabelField($"Enemy Spawn Points: {spawnPoints.Length}");
        
        if (spawnPoints.Length == 0)
        {
            EditorGUILayout.HelpBox("No enemy spawn points found. Add EnemySpawnPoint components as children.", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.LabelField($"Has Enemy Spawn Points: ✓", EditorStyles.helpBox);
        }
    }
} 