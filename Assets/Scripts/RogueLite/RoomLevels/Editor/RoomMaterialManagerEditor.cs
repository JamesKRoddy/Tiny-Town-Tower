using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(RoomMaterialManager))]
public class RoomMaterialManagerEditor : Editor
{
    private SerializedProperty materialGroupsProp;
    private SerializedProperty randomizeOnStartProp;
    private SerializedProperty logRandomizationProp;
    
    private void OnEnable()
    {
        materialGroupsProp = serializedObject.FindProperty("materialGroups");
        randomizeOnStartProp = serializedObject.FindProperty("randomizeOnStart");
        logRandomizationProp = serializedObject.FindProperty("logRandomization");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        var manager = (RoomMaterialManager)target;
        
        EditorGUILayout.Space(5);
        
        // Header
        EditorGUILayout.LabelField("Room Material Manager", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Centralized material randomization for room objects. Use the Setup Tool to auto-configure.", MessageType.Info);
        
        EditorGUILayout.Space(5);
        
        // Quick setup button
        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("Open Material Setup Tool", GUILayout.Height(30)))
        {
            var window = RoomMaterialSetupTool.ShowWindow();
            window.SetRoomRoot(manager.gameObject);
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.Space(10);
        
        // Settings
        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(randomizeOnStartProp, new GUIContent("Randomize On Start"));
        EditorGUILayout.PropertyField(logRandomizationProp, new GUIContent("Log Randomization"));
        
        EditorGUILayout.Space(10);
        
        // Test buttons
        EditorGUILayout.LabelField("Testing", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Randomize Now"))
        {
            manager.RandomizeAllMaterials();
        }
        
        if (GUILayout.Button("Clear All Groups"))
        {
            if (EditorUtility.DisplayDialog("Clear All Groups", 
                "Are you sure you want to clear all material groups?", "Yes", "No"))
            {
                manager.ClearAllGroups();
                EditorUtility.SetDirty(target);
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        // Material Groups
        EditorGUILayout.LabelField($"Material Groups ({materialGroupsProp.arraySize})", EditorStyles.boldLabel);
        
        if (materialGroupsProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("No material groups configured. Use the 'Open Material Setup Tool' button above to auto-configure.", MessageType.Warning);
        }
        else
        {
            for (int i = 0; i < materialGroupsProp.arraySize; i++)
            {
                DrawMaterialGroup(materialGroupsProp.GetArrayElementAtIndex(i), i);
            }
        }
        
        EditorGUILayout.Space(10);
        
        // Add new group button
        if (GUILayout.Button("+ Add Empty Group"))
        {
            manager.AddGroup("New Group");
            EditorUtility.SetDirty(target);
        }
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void DrawMaterialGroup(SerializedProperty groupProp, int index)
    {
        var groupNameProp = groupProp.FindPropertyRelative("groupName");
        var synchronizeProp = groupProp.FindPropertyRelative("synchronizeGroup");
        var rendererMaterialsProp = groupProp.FindPropertyRelative("rendererMaterials");
        var renderersProp = groupProp.FindPropertyRelative("renderers");
        var variantsProp = groupProp.FindPropertyRelative("materialVariants");
        var foldoutProp = groupProp.FindPropertyRelative("foldout");
        
        // Check if using new or legacy system
        bool usingNewSystem = rendererMaterialsProp != null && rendererMaterialsProp.arraySize > 0;
        
        EditorGUILayout.BeginVertical("box");
        
        // Header with foldout
        EditorGUILayout.BeginHorizontal();
        
        foldoutProp.boolValue = EditorGUILayout.Foldout(foldoutProp.boolValue, "", true);
        
        EditorGUILayout.LabelField($"{groupNameProp.stringValue}", EditorStyles.boldLabel);
        
        GUILayout.FlexibleSpace();
        
        // Stats
        if (usingNewSystem)
        {
            int uniqueRenderers = 0;
            int totalVariants = 0;
            System.Collections.Generic.HashSet<Renderer> seenRenderers = new System.Collections.Generic.HashSet<Renderer>();
            
            for (int i = 0; i < rendererMaterialsProp.arraySize; i++)
            {
                var infoProp = rendererMaterialsProp.GetArrayElementAtIndex(i);
                var rendererProp = infoProp.FindPropertyRelative("renderer");
                var varProp = infoProp.FindPropertyRelative("variants");
                
                if (rendererProp.objectReferenceValue != null)
                {
                    seenRenderers.Add((Renderer)rendererProp.objectReferenceValue);
                }
                totalVariants += varProp.arraySize;
            }
            uniqueRenderers = seenRenderers.Count;
            
            EditorGUILayout.LabelField($"R:{uniqueRenderers} V:{totalVariants}", GUILayout.Width(60));
        }
        else
        {
            EditorGUILayout.LabelField($"R:{renderersProp.arraySize} V:{variantsProp.arraySize}", GUILayout.Width(60));
        }
        
        // Delete button
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("X", GUILayout.Width(25)))
        {
            if (EditorUtility.DisplayDialog("Delete Group", 
                $"Delete material group '{groupNameProp.stringValue}'?", "Yes", "No"))
            {
                DeleteGroup(index);
                EditorUtility.SetDirty(target);
            }
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndHorizontal();
        
        // Foldout content
        if (foldoutProp.boolValue)
        {
            EditorGUI.indentLevel++;
            
            EditorGUILayout.PropertyField(groupNameProp, new GUIContent("Group Name"));
            
            if (synchronizeProp.boolValue && usingNewSystem)
            {
                EditorGUILayout.PropertyField(synchronizeProp, new GUIContent("Synchronize Group", 
                    "All renderers pick the SAME variant INDEX from their own materials"));
            }
            else
            {
                EditorGUILayout.PropertyField(synchronizeProp, new GUIContent("Synchronize Group", 
                    "All renderers use the same random material"));
            }
            
            EditorGUILayout.Space(5);
            
            // Display new or legacy system
            if (usingNewSystem)
            {
                DrawRendererMaterials(rendererMaterialsProp);
            }
            else
            {
                DrawLegacySystem(renderersProp, variantsProp);
            }
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }
    
    private void DrawRendererMaterials(SerializedProperty rendererMaterialsProp)
    {
        EditorGUILayout.LabelField($"Renderer Materials ({rendererMaterialsProp.arraySize})", EditorStyles.miniBoldLabel);
        
        if (rendererMaterialsProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("No renderer materials configured", MessageType.Warning);
            return;
        }
        
        int displayCount = Mathf.Min(rendererMaterialsProp.arraySize, 5);
        
        for (int i = 0; i < displayCount; i++)
        {
            var infoProp = rendererMaterialsProp.GetArrayElementAtIndex(i);
            var rendererProp = infoProp.FindPropertyRelative("renderer");
            var matIndexProp = infoProp.FindPropertyRelative("materialIndex");
            var variantsProp = infoProp.FindPropertyRelative("variants");
            
            EditorGUILayout.BeginVertical("box");
            
            string rendererName = rendererProp.objectReferenceValue != null ? 
                ((Renderer)rendererProp.objectReferenceValue).gameObject.name : "None";
            
            EditorGUILayout.LabelField($"{rendererName} [Slot {matIndexProp.intValue}] - {variantsProp.arraySize} variants", EditorStyles.miniLabel);
            
            // Show first 2 variants as preview
            for (int v = 0; v < Mathf.Min(variantsProp.arraySize, 2); v++)
            {
                var variantProp = variantsProp.GetArrayElementAtIndex(v);
                if (variantProp.objectReferenceValue != null)
                {
                    EditorGUILayout.LabelField($"  • {variantProp.objectReferenceValue.name}", EditorStyles.miniLabel);
                }
            }
            if (variantsProp.arraySize > 2)
            {
                EditorGUILayout.LabelField($"  ... and {variantsProp.arraySize - 2} more", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        if (rendererMaterialsProp.arraySize > displayCount)
        {
            EditorGUILayout.LabelField($"... and {rendererMaterialsProp.arraySize - displayCount} more", EditorStyles.miniLabel);
        }
    }
    
    private void DrawLegacySystem(SerializedProperty renderersProp, SerializedProperty variantsProp)
    {
        // Renderers
        EditorGUILayout.LabelField($"Renderers ({renderersProp.arraySize}) [Legacy]", EditorStyles.miniBoldLabel);
        if (renderersProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("No renderers assigned", MessageType.Warning);
        }
        else
        {
            for (int i = 0; i < Mathf.Min(renderersProp.arraySize, 5); i++)
            {
                EditorGUILayout.PropertyField(renderersProp.GetArrayElementAtIndex(i), 
                    new GUIContent($"Renderer {i}"));
            }
            if (renderersProp.arraySize > 5)
            {
                EditorGUILayout.LabelField($"... and {renderersProp.arraySize - 5} more", EditorStyles.miniLabel);
            }
        }
        
        EditorGUILayout.Space(5);
        
        // Material Variants
        EditorGUILayout.LabelField($"Material Variants ({variantsProp.arraySize}) [Legacy]", EditorStyles.miniBoldLabel);
        if (variantsProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("No material variants", MessageType.Warning);
        }
        else
        {
            for (int i = 0; i < variantsProp.arraySize; i++)
            {
                var variantProp = variantsProp.GetArrayElementAtIndex(i);
                var materialProp = variantProp.FindPropertyRelative("material");
                var indexProp = variantProp.FindPropertyRelative("materialIndex");
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(materialProp, new GUIContent($"[{indexProp.intValue}]"), GUILayout.ExpandWidth(true));
                EditorGUILayout.PropertyField(indexProp, GUIContent.none, GUILayout.Width(40));
                EditorGUILayout.EndHorizontal();
            }
        }
    }
    
    private void DeleteGroup(int index)
    {
        materialGroupsProp.DeleteArrayElementAtIndex(index);
    }
}


