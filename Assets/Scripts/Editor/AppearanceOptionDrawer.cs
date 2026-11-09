using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom property drawer for AppearanceOption to display it compactly in the inspector.
/// Shows the model field, preview thumbnail, and quick rarity preset buttons for spawn weight.
/// </summary>
[CustomPropertyDrawer(typeof(AppearanceOption))]
public class AppearanceOptionDrawer : PropertyDrawer
{
    private const float SPACING = 3f;
    private const float BUTTON_WIDTH = 50f;
    private const float VALUE_FIELD_WIDTH = 40f;
    
    private static float PreviewSize
    {
        get { return EditorPrefs.GetFloat("AppearanceOption_PreviewSize", 70f); }
        set { EditorPrefs.SetFloat("AppearanceOption_PreviewSize", value); }
    }
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        // Get the properties
        SerializedProperty modelProp = property.FindPropertyRelative("model");
        SerializedProperty weightProp = property.FindPropertyRelative("spawnWeight");
        SerializedProperty exclusionsProp = property.FindPropertyRelative("exclusions");
        
        // Calculate rects
        float indent = EditorGUI.indentLevel * 15f;
        float availableWidth = position.width - indent;
        
        // Preview thumbnail on the left
        float previewSize = PreviewSize;
        Rect previewRect = new Rect(position.x + indent, position.y, previewSize, previewSize);
        
        // Model field gets remaining space after preview, buttons and value field
        float buttonsWidth = (BUTTON_WIDTH * 4) + (SPACING * 3); // 4 buttons
        float modelWidth = availableWidth - previewSize - buttonsWidth - VALUE_FIELD_WIDTH - SPACING * 4;
        
        Rect modelRect = new Rect(previewRect.xMax + SPACING, position.y + (previewSize - EditorGUIUtility.singleLineHeight) / 2f, 
                                   modelWidth, EditorGUIUtility.singleLineHeight);
        
        // Quick preset buttons (vertically centered with model field)
        float buttonY = position.y + (previewSize - EditorGUIUtility.singleLineHeight) / 2f;
        Rect commonBtn = new Rect(modelRect.xMax + SPACING, buttonY, BUTTON_WIDTH, EditorGUIUtility.singleLineHeight);
        Rect uncommonBtn = new Rect(commonBtn.xMax + SPACING, buttonY, BUTTON_WIDTH, EditorGUIUtility.singleLineHeight);
        Rect rareBtn = new Rect(uncommonBtn.xMax + SPACING, buttonY, BUTTON_WIDTH, EditorGUIUtility.singleLineHeight);
        Rect ultraRareBtn = new Rect(rareBtn.xMax + SPACING, buttonY, BUTTON_WIDTH, EditorGUIUtility.singleLineHeight);
        
        // Value field
        Rect valueRect = new Rect(ultraRareBtn.xMax + SPACING, buttonY, VALUE_FIELD_WIDTH, EditorGUIUtility.singleLineHeight);
        
        // Temporarily disable indent for consistent alignment
        int oldIndent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        
        // Draw preview thumbnail
        GameObject model = modelProp.objectReferenceValue as GameObject;
        AppearanceMaterialVariants[] materialVariantsArray = null;
        int variantCount = 0;
        int componentCount = 0;
        
        if (model != null)
        {
            Texture2D preview = AssetPreview.GetAssetPreview(model);
            if (preview != null)
            {
                GUI.Box(previewRect, GUIContent.none, EditorStyles.helpBox);
                GUI.DrawTexture(previewRect, preview, ScaleMode.ScaleToFit);
            }
            else
            {
                // Show placeholder while preview loads
                GUI.Box(previewRect, "...", EditorStyles.helpBox);
            }
            
            // Check if the model has material variants (supports multiple components)
            materialVariantsArray = model.GetComponents<AppearanceMaterialVariants>();
            componentCount = materialVariantsArray.Length;
            foreach (var mv in materialVariantsArray)
            {
                if (mv != null)
                    variantCount += mv.VariantCount;
            }
        }
        else
        {
            // Show empty box when no model assigned
            GUI.Box(previewRect, "None", EditorStyles.helpBox);
        }
        
        // Draw variant info badge if present
        if (componentCount > 0 && variantCount > 0)
        {
            Rect badgeRect = new Rect(previewRect.x + 2, previewRect.yMax - 18, previewRect.width - 4, 16);
            
            // Draw background
            Color oldColor = GUI.color;
            GUI.color = new Color(0.2f, 0.8f, 0.4f, 0.9f); // Green background
            GUI.Box(badgeRect, GUIContent.none, EditorStyles.helpBox);
            GUI.color = oldColor;
            
            // Draw text - show component count if multiple, otherwise just total variants
            GUIStyle badgeStyle = new GUIStyle(EditorStyles.miniLabel);
            badgeStyle.alignment = TextAnchor.MiddleCenter;
            badgeStyle.normal.textColor = Color.white;
            badgeStyle.fontStyle = FontStyle.Bold;
            string badgeText = componentCount > 1 ? $"{componentCount}x{variantCount}" : $"{variantCount} Mats";
            GUI.Label(badgeRect, badgeText, badgeStyle);
        }
        
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
        
        // Draw exclusions section below (second row)
        float exclusionsY = position.y + previewSize + 4f;
        DrawExclusionsRow(new Rect(position.x + indent, exclusionsY, availableWidth, EditorGUIUtility.singleLineHeight), 
                         exclusionsProp);
        
        EditorGUI.EndProperty();
    }
    
    /// <summary>
    /// Draw exclusion toggle buttons
    /// </summary>
    private void DrawExclusionsRow(Rect position, SerializedProperty exclusionsProp)
    {
        int exclusionValue = exclusionsProp.intValue;
        
        EditorGUI.LabelField(new Rect(position.x, position.y, 70, position.height), "Excludes:", EditorStyles.miniLabel);
        
        float buttonX = position.x + 75;
        float buttonWidth = 50f;
        float spacing = 3f;
        
        // Define exclusion buttons with labels matching field names
        var exclusionButtons = new (AppearanceExclusions flag, string label)[]
        {
            (AppearanceExclusions.Hair, "Hair"),
            (AppearanceExclusions.Hats, "Hats"),
            (AppearanceExclusions.Helmets, "Helm"),
            (AppearanceExclusions.FaceAccessories, "Face"),
            (AppearanceExclusions.AllShoulderAccessories, "Shldr"),
            (AppearanceExclusions.AllForearmAccessories, "Forarm"),
            (AppearanceExclusions.AllHandAccessories, "Hand"),
            (AppearanceExclusions.AllUpperLegAccessories, "ULeg"),
            (AppearanceExclusions.AllCalfAccessories, "Calf"),
            (AppearanceExclusions.AllFootAccessories, "Foot"),
            (AppearanceExclusions.BackAccessories, "Back"),
            (AppearanceExclusions.TopClothing, "TopCl"),
            (AppearanceExclusions.BottomClothing, "BotCl"),
            (AppearanceExclusions.Footwear, "Ftwr")
        };
        
        Color originalBg = GUI.backgroundColor;
        
        foreach (var (flag, label) in exclusionButtons)
        {
            bool isActive = (exclusionValue & (int)flag) != 0;
            
            // Highlight active exclusions
            GUI.backgroundColor = isActive ? new Color(1f, 0.5f, 0.5f) : originalBg;
            
            Rect buttonRect = new Rect(buttonX, position.y, buttonWidth, position.height - 2);
            if (GUI.Button(buttonRect, new GUIContent(label, $"Exclude {flag}"), EditorStyles.miniButton))
            {
                // Toggle the flag
                if (isActive)
                    exclusionsProp.intValue &= ~(int)flag; // Remove flag
                else
                    exclusionsProp.intValue |= (int)flag; // Add flag
            }
            
            buttonX += buttonWidth + spacing;
        }
        
        GUI.backgroundColor = originalBg;
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return PreviewSize + EditorGUIUtility.singleLineHeight + 8f; // Preview + exclusions row + spacing
    }
    
    /// <summary>
    /// Draw preview size slider (call this from parent inspector)
    /// </summary>
    public static void DrawPreviewSizeSlider()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Preview Size", GUILayout.Width(100));
        
        float newSize = EditorGUILayout.Slider(PreviewSize, 50f, 150f);
        if (!Mathf.Approximately(newSize, PreviewSize))
        {
            PreviewSize = newSize;
        }
        
        // Quick preset buttons
        if (GUILayout.Button("S", GUILayout.Width(30))) PreviewSize = 60f;
        if (GUILayout.Button("M", GUILayout.Width(30))) PreviewSize = 80f;
        if (GUILayout.Button("L", GUILayout.Width(30))) PreviewSize = 100f;
        if (GUILayout.Button("XL", GUILayout.Width(30))) PreviewSize = 120f;
        
        EditorGUILayout.EndHorizontal();
    }
}

