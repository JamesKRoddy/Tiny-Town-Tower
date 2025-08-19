using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(HostileRoom))]
public class HostileRoomEditor : RogueLiteRoomEditorBase
{
    public override void OnInspectorGUI()
    {
        DrawCommonInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Hostile Room Specific", EditorStyles.boldLabel);
        
        HostileRoom hostileRoom = (HostileRoom)target;
        
        // Enemy spawning info
        EditorGUILayout.LabelField("Enemy Spawning System", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("This hostile room uses random NavMesh-based enemy spawning. Enemies will spawn at random positions on the NavMesh that are at least 10 units away from the player.", MessageType.Info);
        
        EditorGUILayout.LabelField("Spawn System: Random NavMesh Positions ✓", EditorStyles.helpBox);
    }
} 