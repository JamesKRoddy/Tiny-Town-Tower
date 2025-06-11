using UnityEngine;
using System;

[CreateAssetMenu(fileName = "GeneticMutation", menuName = "Scriptable Objects/GeneticMutation")]
public class GeneticMutationObj : ResourceScriptableObj
{
    public const int MAX_SHAPE_SIZE = 4; // Maximum size for the shape grid

    [Serializable]
    public class ShapeRow
    {
        public bool[] cells = new bool[MAX_SHAPE_SIZE];
    }

    [SerializeField] private ShapeRow[] shapeRows = new ShapeRow[MAX_SHAPE_SIZE];
    public GameObject mutationEffectPrefab; // Reference to the prefab containing the mutation effect component
    public Sprite mutationIcon;

    private void OnEnable()
    {
        // Initialize the shape if it hasn't been initialized
        if (shapeRows == null || shapeRows.Length != MAX_SHAPE_SIZE)
        {
            shapeRows = new ShapeRow[MAX_SHAPE_SIZE];
            for (int i = 0; i < MAX_SHAPE_SIZE; i++)
            {
                shapeRows[i] = new ShapeRow();
            }
        }
    }

    // Property to access the shape data
    public bool[,] shape
    {
        get
        {
            bool[,] result = new bool[MAX_SHAPE_SIZE, MAX_SHAPE_SIZE];
            for (int y = 0; y < MAX_SHAPE_SIZE; y++)
            {
                for (int x = 0; x < MAX_SHAPE_SIZE; x++)
                {
                    result[x, y] = shapeRows[y].cells[x];
                }
            }
            return result;
        }
        set
        {
            for (int y = 0; y < MAX_SHAPE_SIZE; y++)
            {
                for (int x = 0; x < MAX_SHAPE_SIZE; x++)
                {
                    shapeRows[y].cells[x] = value[x, y];
                }
            }
        }
    }

    // Helper property to get the actual size of the shape
    public Vector2Int GetShapeSize()
    {
        return new Vector2Int(MAX_SHAPE_SIZE, MAX_SHAPE_SIZE);
    }

    // Helper method to check if a position is filled in the shape
    public bool IsPositionFilled(int x, int y)
    {
        if (x < 0 || x >= MAX_SHAPE_SIZE || y < 0 || y >= MAX_SHAPE_SIZE)
            return false;
        return shapeRows[y].cells[x];
    }
}
