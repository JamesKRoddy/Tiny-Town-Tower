using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom property drawer for AppearanceOption to display it compactly in the inspector.
/// Shows the model field and quick rarity preset buttons for spawn weight.
/// </summary>
[CustomPropertyDrawer(typeof(AppearanceOption))]
public class AppearanceOptionDrawer : PropertyDrawer
{
    private const float SPACING = 3f;
    private const float BUTTON_WIDTH = 50f;
    private const float VALUE_FIELD_WIDTH = 40f;
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        // Get the properties
        SerializedProperty modelProp = property.FindPropertyRelative("model");
        SerializedProperty weightProp = property.FindPropertyRelative("spawnWeight");
        
        // Calculate rects
        float indent = EditorGUI.indentLevel * 15f;
        float availableWidth = position.width - indent;
        
        // Model field gets remaining space after buttons and value field
        float buttonsWidth = (BUTTON_WIDTH * 4) + (SPACING * 3); // 4 buttons
        float modelWidth = availableWidth - buttonsWidth - VALUE_FIELD_WIDTH - SPACING * 2;
        
        Rect modelRect = new Rect(position.x + indent, position.y, modelWidth, position.height);
        
        // Quick preset buttons
        Rect commonBtn = new Rect(modelRect.xMax + SPACING, position.y, BUTTON_WIDTH, position.height);
        Rect uncommonBtn = new Rect(commonBtn.xMax + SPACING, position.y, BUTTON_WIDTH, position.height);
        Rect rareBtn = new Rect(uncommonBtn.xMax + SPACING, position.y, BUTTON_WIDTH, position.height);
        Rect ultraRareBtn = new Rect(rareBtn.xMax + SPACING, position.y, BUTTON_WIDTH, position.height);
        
        // Value field
        Rect valueRect = new Rect(ultraRareBtn.xMax + SPACING, position.y, VALUE_FIELD_WIDTH, position.height);
        
        // Temporarily disable indent for consistent alignment
        int oldIndent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        
        // Draw model field with label
        EditorGUI.PropertyField(modelRect, modelProp, label);
        
        // Get current weight for button highlighting
        float currentWeight = weightProp.floatValue;
        
        // Draw preset buttons with color coding
        Color originalColor = GUI.backgroundColor;
        
        // Common (100)
        GUI.backgroundColor = Mathf.Approximately(currentWeight, 100f) ? Color.green : originalColor;
        if (GUI.Button(commonBtn, new GUIContent("100", "Common (100)")))
        {
            weightProp.floatValue = 100f;
        }
        
        // Uncommon (50)
        GUI.backgroundColor = Mathf.Approximately(currentWeight, 50f) ? Color.cyan : originalColor;
        if (GUI.Button(uncommonBtn, new GUIContent("50", "Uncommon (50)")))
        {
            weightProp.floatValue = 50f;
        }
        
        // Rare (10)
        GUI.backgroundColor = Mathf.Approximately(currentWeight, 10f) ? Color.yellow : originalColor;
        if (GUI.Button(rareBtn, new GUIContent("10", "Rare (10)")))
        {
            weightProp.floatValue = 10f;
        }
        
        // Ultra Rare (1)
        GUI.backgroundColor = Mathf.Approximately(currentWeight, 1f) ? new Color(1f, 0.5f, 0f) : originalColor;
        if (GUI.Button(ultraRareBtn, new GUIContent("1", "Ultra Rare (1)")))
        {
            weightProp.floatValue = 1f;
        }
        
        GUI.backgroundColor = originalColor;
        
        // Draw weight value field (editable for custom values)
        EditorGUI.BeginChangeCheck();
        float editedWeight = EditorGUI.FloatField(valueRect, currentWeight);
        if (EditorGUI.EndChangeCheck())
        {
            weightProp.floatValue = Mathf.Clamp(editedWeight, 0f, 100f);
        }
        
        // Restore indent
        EditorGUI.indentLevel = oldIndent;
        
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight + 2f; // Add a bit of spacing
    }
}

