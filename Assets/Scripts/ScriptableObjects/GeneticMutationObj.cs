using UnityEngine;
using System;

[CreateAssetMenu(fileName = "GeneticMutation", menuName = "Scriptable Objects/GeneticMutation")]
public class GeneticMutationObj : ResourceScriptableObj
{
    public const int MAX_SHAPE_SIZE = 4; // Maximum size for the shape grid

    public enum RotationState
    {
        ROT_0,    // 0 degrees
        ROT_90,   // 90 degrees clockwise
        ROT_180,  // 180 degrees
        ROT_270   // 270 degrees clockwise
    }

    [Serializable]
    public class ShapeRow
    {
        public bool[] cells = new bool[MAX_SHAPE_SIZE];
    }

    [SerializeField] private ShapeRow[] shapeRows = new ShapeRow[MAX_SHAPE_SIZE];
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

    // Returns the minimal bounding box (minX, minY, width, height) of the shape
    public (int minX, int minY, int width, int height) GetBoundingBox()
    {
        int minX = MAX_SHAPE_SIZE, minY = MAX_SHAPE_SIZE, maxX = -1, maxY = -1;
        for (int y = 0; y < MAX_SHAPE_SIZE; y++)
        {
            for (int x = 0; x < MAX_SHAPE_SIZE; x++)
            {
                if (shapeRows[y].cells[x])
                {
                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }
        }
        if (maxX < minX || maxY < minY) // No cells filled
            return (0, 0, 1, 1);
        return (minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    // Returns the actual size (width, height) of the shape
    public Vector2Int GetActualSize()
    {
        var box = GetBoundingBox();
        return new Vector2Int(box.width, box.height);
    }

    // Get the rotated shape for a given rotation state
    public bool[,] GetRotatedShape(RotationState rotation)
    {
        bool[,] original = shape;
        bool[,] rotated = new bool[MAX_SHAPE_SIZE, MAX_SHAPE_SIZE];

        switch (rotation)
        {
            case RotationState.ROT_0:
                // No rotation needed
                for (int y = 0; y < MAX_SHAPE_SIZE; y++)
                    for (int x = 0; x < MAX_SHAPE_SIZE; x++)
                        rotated[x, y] = original[x, y];
                break;

            case RotationState.ROT_90:
                // 90 degrees clockwise
                for (int y = 0; y < MAX_SHAPE_SIZE; y++)
                    for (int x = 0; x < MAX_SHAPE_SIZE; x++)
                        rotated[y, MAX_SHAPE_SIZE - 1 - x] = original[x, y];
                break;

            case RotationState.ROT_180:
                // 180 degrees
                for (int y = 0; y < MAX_SHAPE_SIZE; y++)
                    for (int x = 0; x < MAX_SHAPE_SIZE; x++)
                        rotated[MAX_SHAPE_SIZE - 1 - x, MAX_SHAPE_SIZE - 1 - y] = original[x, y];
                break;

            case RotationState.ROT_270:
                // 270 degrees clockwise (or 90 degrees counter-clockwise)
                for (int y = 0; y < MAX_SHAPE_SIZE; y++)
                    for (int x = 0; x < MAX_SHAPE_SIZE; x++)
                        rotated[MAX_SHAPE_SIZE - 1 - y, x] = original[x, y];
                break;
        }

        return rotated;
    }

    // Get the bounding box for a given rotation state
    public (int minX, int minY, int width, int height) GetBoundingBoxForRotation(RotationState rotation)
    {
        bool[,] rotatedShape = GetRotatedShape(rotation);
        int minX = MAX_SHAPE_SIZE, minY = MAX_SHAPE_SIZE, maxX = -1, maxY = -1;

        for (int y = 0; y < MAX_SHAPE_SIZE; y++)
        {
            for (int x = 0; x < MAX_SHAPE_SIZE; x++)
            {
                if (rotatedShape[x, y])
                {
                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }
        }

        if (maxX < minX || maxY < minY)
            return (0, 0, 1, 1);
        return (minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    // Get the actual size for a given rotation state
    public Vector2Int GetActualSizeForRotation(RotationState rotation)
    {
        var box = GetBoundingBoxForRotation(rotation);
        return new Vector2Int(box.width, box.height);
    }
}
