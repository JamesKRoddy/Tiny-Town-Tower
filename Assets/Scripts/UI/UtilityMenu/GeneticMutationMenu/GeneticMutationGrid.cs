using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof (GridLayoutGroup))]
public class GeneticMutationGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private Vector2Int cellSize = new Vector2Int(50, 50);
    [SerializeField] public GameObject mutationSlotPrefab;

    private MutationUIElement[,] grid;
    private GridLayoutGroup gridLayout;
    private int gridWidth;
    private int gridHeight;

    private void Awake()
    {
        gridLayout = GetComponent<GridLayoutGroup>();
        UpdateGridSize();
    }

    private void UpdateGridSize()
    {
        // Get the max slots from player inventory
        int maxSlots = PlayerInventory.Instance.MaxMutationSlots;
        
        // Calculate grid dimensions to be as square as possible
        gridWidth = Mathf.CeilToInt(Mathf.Sqrt(maxSlots));
        gridHeight = Mathf.CeilToInt((float)maxSlots / gridWidth);

        // Initialize grid array
        grid = new MutationUIElement[gridWidth, gridHeight];

        if (gridLayout)
        {
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = gridWidth;
            gridLayout.cellSize = new Vector2(cellSize.x, cellSize.y);
            gridLayout.spacing = Vector2.zero; // Ensure no spacing between cells
            gridLayout.padding = new RectOffset(0, 0, 0, 0); // Ensure no padding
            gridLayout.childAlignment = TextAnchor.LowerLeft; // Align children to lower left
        }

        // Clear existing slots and generate new ones
        ClearGrid();
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

    public bool CanPlaceMutation(Vector2Int position, MutationUIElement element)
    {
        Vector2Int size = element.Size;
        if (position.x + size.x > gridWidth || position.y + size.y > gridHeight)
            return false;

        // Check each cell in the shape
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                // Only check positions that are filled in the shape
                if (element.IsPositionFilled(x, y))
                {
                    if (grid[position.x + x, position.y + y] != null)
                        return false;
                }
            }
        }
        return true;
    }

    public void PlaceMutation(MutationUIElement element, Vector2Int position, Vector2Int size)
    {
        ClearPosition(element);

        // Place the mutation in the new position
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                // Only place in positions that are filled in the shape
                if (element.IsPositionFilled(x, y))
                {
                    grid[position.x + x, position.y + y] = element;
                }
            }
        }

        // Set the button click handler
        element.SetupButtonClick();

        // Set as selected object
        PlayerUIManager.Instance.SetSelectedGameObject(element.gameObject);
    }

    public void ClearPosition(MutationUIElement element)
    {
        // Find and clear the old position of this mutation
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (grid[x, y] == element)
                {
                    // Clear the old position
                    for (int oldX = 0; oldX < element.Size.x; oldX++)
                    {
                        for (int oldY = 0; oldY < element.Size.y; oldY++)
                        {
                            if (element.IsPositionFilled(oldX, oldY) &&
                                x + oldX < gridWidth && y + oldY < gridHeight)
                            {
                                grid[x + oldX, y + oldY] = null;
                            }
                        }
                    }
                    break;
                }
            }
        }
    }

    public MutationUIElement GetMutationAtPosition(Vector2Int position)
    {
        if (position.x >= 0 && position.x < gridWidth && position.y >= 0 && position.y < gridHeight)
        {
            return grid[position.x, position.y];
        }
        return null;
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
        // Find first available spot
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2Int position = new Vector2Int(x, y);

                GameObject newSlot = Instantiate(mutationSlotPrefab, transform);
                MutationUIElement uiElement = newSlot.GetComponent<MutationUIElement>();
                uiElement.Initialize(mutation, this);

                if (CanPlaceMutation(position, uiElement))
                {
                    PlaceMutation(uiElement, position, uiElement.Size);
                    return true;
                }
                else
                {
                    Destroy(newSlot);
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

    public MutationUIElement GetMutationElement(GameObject targetObject)
    {
        // Search through the grid for the mutation
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (grid[x, y] != null &&
                    grid[x, y].gameObject == targetObject)
                {
                    return grid[x, y];
                }
            }
        }
        return null;
    }
}
