using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GeneticMutationObj))]
public class GeneticMutationObjEditor : Editor
{
    private const float CELL_SIZE = 20f;
    private const float PADDING = 10f;
    private const float GRID_PADDING = 2f;

    public override void OnInspectorGUI()
    {
        GeneticMutationObj mutation = (GeneticMutationObj)target;

        serializedObject.Update();

        // Draw all properties except shapeRows
        SerializedProperty prop = serializedObject.GetIterator();
        if (prop.NextVisible(true))
        {
            do
            {
                // Skip the script field and shapeRows field
                if (prop.name == "m_Script" || prop.name == "shapeRows")
                    continue;
                
                EditorGUILayout.PropertyField(prop, true);
            }
            while (prop.NextVisible(false));
        }

        serializedObject.ApplyModifiedProperties();

        // Validate and display mutation prefab information
        EditorGUILayout.Space(10);
        DrawMutationPrefabInfo(mutation);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Shape Editor", EditorStyles.boldLabel);

        // Draw the shape grid
        DrawShapeGrid(mutation);

        // Add buttons for common operations
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear Shape"))
        {
            ClearShape(mutation);
        }
        if (GUILayout.Button("Rotate 90° Clockwise"))
        {
            RotateShapeClockwise(mutation);
        }
        if (GUILayout.Button("Rotate 90° Counter-Clockwise"))
        {
            RotateShapeCounterClockwise(mutation);
        }
        EditorGUILayout.EndHorizontal();

        // Mark the object as dirty if changes were made
        if (GUI.changed)
        {
            EditorUtility.SetDirty(mutation);
        }
    }

    private void DrawShapeGrid(GeneticMutationObj mutation)
    {
        // Calculate the total size of the grid
        float totalSize = GeneticMutationObj.MAX_SHAPE_SIZE * (CELL_SIZE + GRID_PADDING) + PADDING * 2;
        Rect gridRect = GUILayoutUtility.GetRect(totalSize, totalSize);

        // Draw the background
        EditorGUI.DrawRect(gridRect, new Color(0.2f, 0.2f, 0.2f));

        // Draw the grid cells
        for (int y = 0; y < GeneticMutationObj.MAX_SHAPE_SIZE; y++)
        {
            for (int x = 0; x < GeneticMutationObj.MAX_SHAPE_SIZE; x++)
            {
                float cellX = gridRect.x + PADDING + x * (CELL_SIZE + GRID_PADDING);
                float cellY = gridRect.y + PADDING + y * (CELL_SIZE + GRID_PADDING);
                Rect cellRect = new Rect(cellX, cellY, CELL_SIZE, CELL_SIZE);

                // Draw cell background
                Color cellColor = mutation.IsPositionFilled(x, y) ? Color.green : Color.gray;
                EditorGUI.DrawRect(cellRect, cellColor);

                // Handle mouse input
                if (Event.current.type == EventType.MouseDown && cellRect.Contains(Event.current.mousePosition))
                {
                    bool[,] currentShape = mutation.shape;
                    currentShape[x, y] = !currentShape[x, y];
                    mutation.shape = currentShape;
                    Event.current.Use();
                    GUI.changed = true;
                }
            }
        }

        // Draw grid lines
        Handles.color = Color.black;
        for (int i = 0; i <= GeneticMutationObj.MAX_SHAPE_SIZE; i++)
        {
            float pos = gridRect.x + PADDING + i * (CELL_SIZE + GRID_PADDING);
            Handles.DrawLine(
                new Vector3(pos, gridRect.y + PADDING),
                new Vector3(pos, gridRect.y + PADDING + GeneticMutationObj.MAX_SHAPE_SIZE * (CELL_SIZE + GRID_PADDING))
            );
            Handles.DrawLine(
                new Vector3(gridRect.x + PADDING, pos),
                new Vector3(gridRect.x + PADDING + GeneticMutationObj.MAX_SHAPE_SIZE * (CELL_SIZE + GRID_PADDING), pos)
            );
        }
    }

    private void ClearShape(GeneticMutationObj mutation)
    {
        bool[,] emptyShape = new bool[GeneticMutationObj.MAX_SHAPE_SIZE, GeneticMutationObj.MAX_SHAPE_SIZE];
        mutation.shape = emptyShape;
        GUI.changed = true;
    }

    private void RotateShapeClockwise(GeneticMutationObj mutation)
    {
        bool[,] currentShape = mutation.shape;
        bool[,] newShape = new bool[GeneticMutationObj.MAX_SHAPE_SIZE, GeneticMutationObj.MAX_SHAPE_SIZE];
        
        for (int y = 0; y < GeneticMutationObj.MAX_SHAPE_SIZE; y++)
        {
            for (int x = 0; x < GeneticMutationObj.MAX_SHAPE_SIZE; x++)
            {
                newShape[y, GeneticMutationObj.MAX_SHAPE_SIZE - 1 - x] = currentShape[x, y];
            }
        }

        mutation.shape = newShape;
        GUI.changed = true;
    }

    private void RotateShapeCounterClockwise(GeneticMutationObj mutation)
    {
        bool[,] currentShape = mutation.shape;
        bool[,] newShape = new bool[GeneticMutationObj.MAX_SHAPE_SIZE, GeneticMutationObj.MAX_SHAPE_SIZE];
        
        for (int y = 0; y < GeneticMutationObj.MAX_SHAPE_SIZE; y++)
        {
            for (int x = 0; x < GeneticMutationObj.MAX_SHAPE_SIZE; x++)
            {
                newShape[GeneticMutationObj.MAX_SHAPE_SIZE - 1 - y, x] = currentShape[x, y];
            }
        }

        mutation.shape = newShape;
        GUI.changed = true;
    }

    private void DrawMutationPrefabInfo(GeneticMutationObj mutation)
    {
        EditorGUILayout.LabelField("Mutation Prefab Validation", EditorStyles.boldLabel);
        
        if (mutation.prefab == null)
        {
            EditorGUILayout.HelpBox("No prefab assigned! Please assign a GameObject with a mutation component (CombatMutation, ElementalMutation, SurvivalMutation, or ConditionalMutation).", MessageType.Warning);
            return;
        }

        // Check if the prefab has a BaseMutationEffect component
        BaseMutationEffect mutationEffect = mutation.prefab.GetComponent<BaseMutationEffect>();
        
        if (mutationEffect == null)
        {
            EditorGUILayout.HelpBox($"ERROR: Prefab '{mutation.prefab.name}' does not have a BaseMutationEffect component!\n\n" +
                                   "Please add one of these components to the prefab:\n" +
                                   "• CombatMutation - For damage, attack speed, poise, or elemental damage modifiers\n" +
                                   "• ElementalMutation - For elemental damage, resistance, or conversion effects\n" +
                                   "• SurvivalMutation - For health regen, max health, or damage reduction\n" +
                                   "• ConditionalMutation - For conditional/triggered mutations", 
                                   MessageType.Error);
            
            // Button to select the prefab for easy editing
            if (GUILayout.Button($"Select Prefab '{mutation.prefab.name}' to Fix"))
            {
                Selection.activeObject = mutation.prefab;
            }
        }
        else
        {
            // Valid prefab - show success and description
            EditorGUILayout.HelpBox($"✓ Valid mutation prefab: {mutationEffect.GetType().Name}", MessageType.Info);
            
            // Try to get and display the mutation description
            try
            {
                string description = mutationEffect.GetStatsDescription();
                
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Mutation Effect:", EditorStyles.boldLabel);
                
                // Draw a styled box for the description
                GUIStyle descriptionStyle = new GUIStyle(GUI.skin.box);
                descriptionStyle.alignment = TextAnchor.MiddleLeft;
                descriptionStyle.padding = new RectOffset(10, 10, 10, 10);
                descriptionStyle.normal.textColor = Color.white;
                descriptionStyle.fontSize = 12;
                descriptionStyle.wordWrap = true;
                
                EditorGUILayout.LabelField(description, descriptionStyle);
            }
            catch (System.Exception e)
            {
                EditorGUILayout.HelpBox($"Could not get mutation description: {e.Message}", MessageType.Warning);
            }
            
            // Button to select the prefab
            EditorGUILayout.Space(5);
            if (GUILayout.Button($"Edit Prefab '{mutation.prefab.name}'"))
            {
                Selection.activeObject = mutation.prefab;
            }
        }
    }
} 