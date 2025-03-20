using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof (GridLayoutGroup))]
public class GeneticMutationGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 10;// TODO pull these from the player inventory where it will store the max number of mutations from
    [SerializeField] private int gridHeight = 6;
    [SerializeField] private Vector2Int cellSize = new Vector2Int(50, 50);
    [SerializeField] public GameObject mutationSlotPrefab;

    private MutationUIElement[,] grid;
    private GridLayoutGroup gridLayout;

    private void Awake()
    {
        grid = new MutationUIElement[gridWidth, gridHeight];
        gridLayout = GetComponent<GridLayoutGroup>();

        if (gridLayout)
        {
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = gridWidth;
            gridLayout.cellSize = new Vector2(cellSize.x, cellSize.y);
        }

        // Instantiate empty grid slots for visibility
        GenerateEmptySlots();
    }

    /// <summary>
    /// Creates empty slot visuals so the player can see the grid.
    /// </summary>
    private void GenerateEmptySlots()
    {
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                GameObject emptySlot = Instantiate(mutationSlotPrefab, transform);
                emptySlot.name = $"Slot ({x},{y})";
                emptySlot.GetComponent<Image>().color = new Color(1, 1, 1, 0.2f); // Light transparency
            }
        }
    }


    public bool CanPlaceMutation(Vector2Int position, Vector2Int size)
    {
        if (position.x + size.x > gridWidth || position.y + size.y > gridHeight)
            return false;

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                if (grid[position.x + x, position.y + y] != null)
                    return false;
            }
        }
        return true;
    }

    public void PlaceMutation(MutationUIElement element, Vector2Int position, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                grid[position.x + x, position.y + y] = element;
            }
        }

        element.transform.SetParent(transform, false); // Assign to grid layout group
    }

    public void ClearGrid()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        grid = new MutationUIElement[gridWidth, gridHeight];
    }

    /// <summary>
    /// Adds a mutation to the grid if space is available.
    /// </summary>
    public bool AddMutation(GeneticMutationObj mutation)
    {
        Vector2Int mutationSize = mutation.size;

        // Find first available spot
        for (int x = 0; x < gridWidth - mutationSize.x + 1; x++)
        {
            for (int y = 0; y < gridHeight - mutationSize.y + 1; y++)
            {
                Vector2Int position = new Vector2Int(x, y);

                if (CanPlaceMutation(position, mutationSize))
                {
                    GameObject newSlot = Instantiate(mutationSlotPrefab, transform);
                    MutationUIElement uiElement = newSlot.GetComponent<MutationUIElement>();
                    uiElement.Initialize(mutation, this);
                    PlaceMutation(uiElement, position, mutationSize);
                    return true;
                }
            }
        }

        Debug.LogWarning($"No space available for mutation: {mutation.objectName}");
        return false;
    }

    public Vector2Int ClampToGrid(Vector2Int position, Vector2Int size)
    {
        int clampedX = Mathf.Clamp(position.x, 0, gridWidth - size.x);
        int clampedY = Mathf.Clamp(position.y, 0, gridHeight - size.y);
        return new Vector2Int(clampedX, clampedY);
    }

    /// <summary>
    /// Returns the cell size from the GridLayoutGroup.
    /// </summary>
    public Vector2 GetCellSize()
    {
        if (gridLayout != null)
        {
            return gridLayout.cellSize;
        }
        else
        {
            Debug.LogWarning("GridLayoutGroup is missing! Returning default cell size.");
            return new Vector2(cellSize.x, cellSize.y);
        }
    }
}
