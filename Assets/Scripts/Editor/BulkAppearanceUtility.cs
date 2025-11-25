using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Enemies;

/// <summary>
/// Editor window utility for bulk adding GameObjects to CharacterAppearanceSystem arrays.
/// Access via: Tools > Character Appearance > Bulk Add Utility
/// 
/// Usage:
/// 1. Select a character (SettlerNPC or HumanoidEnemy) in the hierarchy
/// 2. Select multiple appearance GameObjects
/// 3. Open this window
/// 4. Set spawn weight
/// 5. Click the array type to add them to
/// </summary>
public class BulkAppearanceUtility : EditorWindow
{
    private List<GameObject> collectedObjects = new List<GameObject>();
    private float spawnWeight = 100f;
    private Vector2 scrollPosition;
    private GameObject targetCharacter;
    private Object targetComponent;
    
    [MenuItem("Tools/Character Appearance/Bulk Add Utility")]
    public static void ShowWindow()
    {
        BulkAppearanceUtility window = GetWindow<BulkAppearanceUtility>("Bulk Add Appearance");
        window.minSize = new Vector2(400, 300);
    }
    
    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        
        // Title
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
        titleStyle.fontSize = 16;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        EditorGUILayout.LabelField("Bulk Appearance Utility", titleStyle);
        
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox("This utility helps you quickly add multiple GameObjects to appearance arrays with the same spawn weight.", MessageType.Info);
        EditorGUILayout.Space(10);
        
        // Step 1: Select target character
        EditorGUILayout.LabelField("Step 1: Select Target Character", EditorStyles.boldLabel);
        
        GameObject newTarget = EditorGUILayout.ObjectField("Character", targetCharacter, typeof(GameObject), true) as GameObject;
        if (newTarget != targetCharacter)
        {
            targetCharacter = newTarget;
            targetComponent = null;
            
            if (targetCharacter != null)
            {
                // Try to find a component with CharacterAppearanceSystem
                var settler = targetCharacter.GetComponent<SettlerNPC>();
                var humanoid = targetCharacter.GetComponent<HumanoidEnemy>();
                
                if (settler != null)
                    targetComponent = settler;
                else if (humanoid != null)
                    targetComponent = humanoid;
                    
                if (targetComponent == null)
                {
                    Debug.LogWarning($"{targetCharacter.name} doesn't have a SettlerNPC or HumanoidEnemy component!");
                }
            }
        }
        
        if (targetComponent != null)
        {
            Color originalBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = originalBg;
            
            EditorGUILayout.LabelField($"✓ Target: {targetCharacter.name} ({targetComponent.GetType().Name})", EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();
        }
        else
        {
            EditorGUILayout.HelpBox("Select a character with SettlerNPC or HumanoidEnemy component", MessageType.Warning);
        }
        
        EditorGUILayout.Space(10);
        
        // Step 2: Collect objects
        EditorGUILayout.LabelField("Step 2: Collect Appearance Objects", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Collect Selected Objects", GUILayout.Height(30)))
        {
            CollectSelectedObjects();
        }
        
        if (GUILayout.Button("Clear", GUILayout.Width(80), GUILayout.Height(30)))
        {
            collectedObjects.Clear();
        }
        
        EditorGUILayout.EndHorizontal();
        
        // Show collected objects
        if (collectedObjects.Count > 0)
        {
            EditorGUILayout.Space(5);
            
            Color originalBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = originalBg;
            
            EditorGUILayout.LabelField($"Collected: {collectedObjects.Count} objects", EditorStyles.boldLabel);
            
            // Scroll view for objects
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(100));
            foreach (GameObject obj in collectedObjects)
            {
                EditorGUILayout.ObjectField(obj, typeof(GameObject), false);
            }
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
            
            // Step 3: Set spawn weight
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Step 3: Set Spawn Weight", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            spawnWeight = EditorGUILayout.Slider("Spawn Weight", spawnWeight, 0f, 100f);
            
            // Quick weight buttons
            if (GUILayout.Button("Common\n(100)", GUILayout.Width(70)))
                spawnWeight = 100f;
            if (GUILayout.Button("Uncommon\n(50)", GUILayout.Width(70)))
                spawnWeight = 50f;
            if (GUILayout.Button("Rare\n(10)", GUILayout.Width(70)))
                spawnWeight = 10f;
            if (GUILayout.Button("Very Rare\n(1)", GUILayout.Width(70)))
                spawnWeight = 1f;
            
            EditorGUILayout.EndHorizontal();
            
            // Step 4: Add to arrays
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Step 4: Add to Array", EditorStyles.boldLabel);
            
            if (targetComponent == null)
            {
                EditorGUILayout.HelpBox("Select a target character first (Step 1)", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField("Model Options:", EditorStyles.miniBoldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Body Models", GUILayout.Height(30))) AddToArray("bodyModels");
                if (GUILayout.Button("Head Models", GUILayout.Height(30))) AddToArray("headModels");
                if (GUILayout.Button("Hair Models", GUILayout.Height(30))) AddToArray("hairModels");
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Clothing Options:", EditorStyles.miniBoldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Top Clothing", GUILayout.Height(30))) AddToArray("topClothing");
                if (GUILayout.Button("Bottom Clothing", GUILayout.Height(30))) AddToArray("bottomClothing");
                if (GUILayout.Button("Footwear", GUILayout.Height(30))) AddToArray("footwear");
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Accessories:", EditorStyles.miniBoldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Head Accessories", GUILayout.Height(30))) AddToArray("headAccessories");
                if (GUILayout.Button("Back Accessories", GUILayout.Height(30))) AddToArray("backAccessories");
                if (GUILayout.Button("Hand Accessories", GUILayout.Height(30))) AddToArray("handAccessories");
                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("No objects collected yet.\n\nSelect GameObjects in the hierarchy or project, then click 'Collect Selected Objects' above.", MessageType.Warning);
        }
    }
    
    /// <summary>
    /// Collect currently selected GameObjects
    /// </summary>
    private void CollectSelectedObjects()
    {
        collectedObjects.Clear();
        
        foreach (Object obj in Selection.objects)
        {
            if (obj is GameObject go && go != targetCharacter)
            {
                collectedObjects.Add(go);
            }
        }
        
        if (collectedObjects.Count > 0)
        {
            Debug.Log($"Collected {collectedObjects.Count} GameObjects for bulk appearance setup");
        }
        else
        {
            Debug.LogWarning("No GameObjects selected! Select objects in the hierarchy or project first.");
        }
    }
    
    /// <summary>
    /// Add collected objects to the specified array using reflection
    /// </summary>
    private void AddToArray(string arrayFieldName)
    {
        if (targetComponent == null || collectedObjects.Count == 0)
            return;
        
        SerializedObject serializedObject = new SerializedObject(targetComponent);
        SerializedProperty appearanceSystemProp = serializedObject.FindProperty("appearanceSystem");
        
        if (appearanceSystemProp == null)
        {
            Debug.LogError($"Could not find appearanceSystem field on {targetComponent.GetType().Name}");
            return;
        }
        
        SerializedProperty arrayProp = appearanceSystemProp.FindPropertyRelative(arrayFieldName);
        
        if (arrayProp == null)
        {
            Debug.LogError($"Could not find {arrayFieldName} in appearanceSystem");
            return;
        }
        
        // Add each collected object to the array
        int addedCount = 0;
        foreach (GameObject obj in collectedObjects)
        {
            int newIndex = arrayProp.arraySize;
            arrayProp.InsertArrayElementAtIndex(newIndex);
            
            SerializedProperty newElement = arrayProp.GetArrayElementAtIndex(newIndex);
            SerializedProperty modelProp = newElement.FindPropertyRelative("model");
            SerializedProperty weightProp = newElement.FindPropertyRelative("spawnWeight");
            
            if (modelProp != null && weightProp != null)
            {
                modelProp.objectReferenceValue = obj;
                weightProp.floatValue = spawnWeight;
                addedCount++;
            }
        }
        
        serializedObject.ApplyModifiedProperties();
        
        Debug.Log($"✓ Added {addedCount} objects to {targetCharacter.name}.{arrayFieldName} with weight {spawnWeight}");
        
        // Clear collected objects after successful add
        collectedObjects.Clear();
        
        // Mark the scene as dirty
        EditorUtility.SetDirty(targetComponent);
    }
}

