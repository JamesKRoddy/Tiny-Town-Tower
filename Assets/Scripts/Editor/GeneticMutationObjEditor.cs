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

        // Draw default inspector properties
        DrawDefaultInspector();

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
        if (GUILayout.Button("Rotate 90°"))
        {
            RotateShape(mutation);
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

    private void RotateShape(GeneticMutationObj mutation)
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
} 