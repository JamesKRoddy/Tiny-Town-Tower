using UnityEngine;
using UnityEditor;
using Enemies;

/// <summary>
/// Custom inspector for HumanoidEnemy to add appearance system controls
/// </summary>
[CustomEditor(typeof(HumanoidEnemy), true)] // true allows derived classes to use this editor
public class HumanoidEnemyEditor : Editor
{
    public override void OnInspectorGUI()
    {
        HumanoidEnemy enemy = (HumanoidEnemy)target;
        
        // Draw the default inspector first
        DrawDefaultInspector();
        
        // Add appearance controls section
        EditorGUILayout.Space(10);
        DrawAppearanceControls(enemy);
    }
    
    /// <summary>
    /// Draw appearance system controls
    /// </summary>
    private void DrawAppearanceControls(HumanoidEnemy enemy)
    {
        // Only show in play mode when appearance system is available
        if (!Application.isPlaying) return;
        
        var appearanceSystem = enemy.GetAppearanceSystem();
        if (appearanceSystem == null) return;
        
        // Draw a colored section for appearance controls
        Color originalBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.8f, 0.9f, 1f, 0.3f);
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("🎨 Appearance Controls", EditorStyles.boldLabel);
        
        EditorGUILayout.Space(5);
        
        // Randomize Appearance button
        GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
        if (GUILayout.Button("🎲 Randomize Appearance", GUILayout.Height(25)))
        {
            appearanceSystem.ClearCurrentAppearance();
            appearanceSystem.RandomizeAppearance();
            Debug.Log($"[HumanoidEnemyEditor] Randomized appearance for {enemy.name}");
        }
        GUI.backgroundColor = originalBg;
        
        EditorGUILayout.Space(3);
        EditorGUILayout.HelpBox("Click to clear and regenerate a random appearance", MessageType.Info);
        
        EditorGUILayout.EndVertical();
    }
}

