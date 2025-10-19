using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom inspector for HumanCharacterController to display modifiable stats clearly
/// </summary>
[CustomEditor(typeof(HumanCharacterController), true)]
public class HumanCharacterControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw default inspector
        DrawDefaultInspector();
        
        // Add spacing
        EditorGUILayout.Space(10);
        
        // Debug section to confirm custom inspector is working
        EditorGUILayout.LabelField("🎯 HumanCharacterController Custom Inspector", EditorStyles.boldLabel);
        
        // Draw modifiable stats comparison
        HumanCharacterController character = (HumanCharacterController)target;
        
        if (Application.isPlaying)
        {
            DrawModifiableStatsSection(character);
        }
        else
        {
            EditorGUILayout.HelpBox("Modifiable Stats will be displayed here during Play Mode", MessageType.Info);
        }
    }
    
    /// <summary>
    /// Draw only the modifiable stats section (for use by derived classes)
    /// </summary>
    public void DrawModifiableStatsOnly()
    {
        HumanCharacterController character = (HumanCharacterController)target;
        
        EditorGUILayout.LabelField("🎯 Modifiable Stats", EditorStyles.boldLabel);
        
        if (Application.isPlaying)
        {
            DrawModifiableStatsSection(character);
        }
        else
        {
            EditorGUILayout.HelpBox("Modifiable Stats will be displayed here during Play Mode", MessageType.Info);
        }
    }
    
    /// <summary>
    /// Draw the modifiable stats section showing base vs current stats
    /// </summary>
    private void DrawModifiableStatsSection(HumanCharacterController character)
    {
        EditorGUILayout.LabelField("Modifiable Stats (Runtime)", EditorStyles.boldLabel);
        
        var baseStats = character.GetBaseStats();
        var currentStats = character.GetCurrentStats();
        
        // Draw a box around the stats
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        // Health
        DrawStatComparison("Max Health", baseStats.maxHealth, currentStats.maxHealth);
        
        // Poise
        DrawStatComparison("Max Poise", baseStats.maxPoise, currentStats.maxPoise);
        
        // Movement Speed
        DrawStatComparison("Move Max Speed", baseStats.moveMaxSpeed, currentStats.moveMaxSpeed);
        
        // Rotation Speed
        DrawStatComparison("Rotation Speed", baseStats.rotationSpeed, currentStats.rotationSpeed);
        
        // Dash Speed
        DrawStatComparison("Dash Speed", baseStats.dashSpeed, currentStats.dashSpeed);
        
        // Dash Cooldown
        DrawStatComparison("Dash Cooldown", baseStats.dashCooldown, currentStats.dashCooldown);
        
        // If this is an NPC, show NPC-specific stats
        if (currentStats is NPCModifiableStats npcCurrent && baseStats is NPCModifiableStats npcBase)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("NPC-Specific Stats", EditorStyles.miniBoldLabel);
            
            DrawStatComparison("Max Stamina", npcBase.maxStamina, npcCurrent.maxStamina);
            DrawStatComparison("Additional Mutation Slots", npcBase.additionalMutationSlots, npcCurrent.additionalMutationSlots);
        }
        
        EditorGUILayout.EndVertical();
        
        // Show total modifiers summary
        bool hasModifiers = HasAnyModifiers(baseStats, currentStats);
        if (hasModifiers)
        {
            EditorGUILayout.HelpBox("Stats are currently modified by characteristics/mutations", MessageType.Info);
        }
    }
    
    /// <summary>
    /// Draw a single stat comparison (base vs current)
    /// </summary>
    private void DrawStatComparison(string label, float baseValue, float currentValue)
    {
        EditorGUILayout.BeginHorizontal();
        
        EditorGUILayout.LabelField(label, GUILayout.Width(150));
        
        // Base value
        EditorGUILayout.LabelField($"Base: {baseValue:F1}", GUILayout.Width(80));
        
        // Current value with color coding
        bool isModified = Mathf.Abs(currentValue - baseValue) > 0.001f;
        if (isModified)
        {
            GUI.color = currentValue > baseValue ? Color.green : Color.red;
            float difference = currentValue - baseValue;
            string sign = difference > 0 ? "+" : "";
            EditorGUILayout.LabelField($"Current: {currentValue:F1} ({sign}{difference:F1})", GUILayout.Width(150));
            GUI.color = Color.white;
        }
        else
        {
            EditorGUILayout.LabelField($"Current: {currentValue:F1}", GUILayout.Width(150));
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    /// <summary>
    /// Draw a single stat comparison for integers
    /// </summary>
    private void DrawStatComparison(string label, int baseValue, int currentValue)
    {
        EditorGUILayout.BeginHorizontal();
        
        EditorGUILayout.LabelField(label, GUILayout.Width(150));
        
        // Base value
        EditorGUILayout.LabelField($"Base: {baseValue}", GUILayout.Width(80));
        
        // Current value with color coding
        bool isModified = currentValue != baseValue;
        if (isModified)
        {
            GUI.color = currentValue > baseValue ? Color.green : Color.red;
            int difference = currentValue - baseValue;
            string sign = difference > 0 ? "+" : "";
            EditorGUILayout.LabelField($"Current: {currentValue} ({sign}{difference})", GUILayout.Width(150));
            GUI.color = Color.white;
        }
        else
        {
            EditorGUILayout.LabelField($"Current: {currentValue}", GUILayout.Width(150));
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    /// <summary>
    /// Check if any stats have been modified
    /// </summary>
    private bool HasAnyModifiers(CharacterModifiableStats baseStats, CharacterModifiableStats currentStats)
    {
        if (Mathf.Abs(currentStats.maxHealth - baseStats.maxHealth) > 0.001f) return true;
        if (Mathf.Abs(currentStats.maxPoise - baseStats.maxPoise) > 0.001f) return true;
        if (Mathf.Abs(currentStats.moveMaxSpeed - baseStats.moveMaxSpeed) > 0.001f) return true;
        if (Mathf.Abs(currentStats.rotationSpeed - baseStats.rotationSpeed) > 0.001f) return true;
        if (Mathf.Abs(currentStats.dashSpeed - baseStats.dashSpeed) > 0.001f) return true;
        if (Mathf.Abs(currentStats.dashCooldown - baseStats.dashCooldown) > 0.001f) return true;
        
        if (currentStats is NPCModifiableStats npcCurrent && baseStats is NPCModifiableStats npcBase)
        {
            if (Mathf.Abs(npcCurrent.maxStamina - npcBase.maxStamina) > 0.001f) return true;
            if (npcCurrent.additionalMutationSlots != npcBase.additionalMutationSlots) return true;
        }
        
        return false;
    }
}

