using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof (GridLayoutGroup))]
public class GeneticMutationGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private Vector2Int cellSize = new Vector2Int(50, 50);
    [SerializeField] public GameObject emptySlotPrefab;
    [SerializeField] public GameObject mutationSlotPrefab;
    [SerializeField] private Color emptySlotColor = new Color(1, 1, 1, 0.2f); // Light transparency for empty slots

    private MutationUIElement[,] grid;
    private GridLayoutGroup gridLayout;
    private int gridWidth;
    private int gridHeight;
    private GameObject[,] visualGrid; // Visual representation of the grid

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
        visualGrid = new GameObject[gridWidth, gridHeight];

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

    // Get the grid width
    public int GetGridWidth()
    {
        return gridWidth;
    }

    // Get the grid height
    public int GetGridHeight()
    {
        return gridHeight;
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
                GameObject emptySlot = Instantiate(emptySlotPrefab, transform);
                emptySlot.name = $"Slot ({x},{y})";
                
                // Set up the visual grid cell
                RectTransform rectTransform = emptySlot.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0, 0);
                rectTransform.anchorMax = new Vector2(0, 0);
                rectTransform.pivot = new Vector2(0, 0);
                rectTransform.sizeDelta = new Vector2(cellSize.x, cellSize.y);
                rectTransform.anchoredPosition = new Vector2(x * cellSize.x, y * cellSize.y);
                
                // Set color
                emptySlot.GetComponent<Image>().color = emptySlotColor;
                
                // Store reference
                visualGrid[x, y] = emptySlot;
            }
        }
    }

    public bool CanPlaceMutation(Vector2Int position, MutationUIElement element)
    {
        Debug.Log($"[CanPlaceMutation] Checking position {position} with size {element.Size} on grid {gridWidth}x{gridHeight}");
        // Log the mutation shape as a matrix
        Debug.Log($"[CanPlaceMutation] Shape matrix for {element.mutation.objectName}:");
        for (int y = element.Size.y - 1; y >= 0; y--)
        {
            string row = "";
            for (int x = 0; x < element.Size.x; x++)
            {
                row += element.IsPositionFilled(x, y) ? "X " : ". ";
            }
            Debug.Log($"[CanPlaceMutation] {row}");
        }
        // Log current grid state
        Debug.Log("[CanPlaceMutation] Current grid state:");
        for (int y = gridHeight - 1; y >= 0; y--)
        {
            string row = "";
            for (int x = 0; x < gridWidth; x++)
            {
                row += grid[x, y] != null ? "X " : ". ";
            }
            Debug.Log($"[CanPlaceMutation] {row}");
        }
        // Log each cell checked
        for (int y = 0; y < element.Size.y; y++)
        {
            for (int x = 0; x < element.Size.x; x++)
            {
                if (element.IsPositionFilled(x, y))
                {
                    int gridX = position.x + x;
                    int gridY = position.y + y;
                    Debug.Log($"[CanPlaceMutation] Checking cell ({gridX},{gridY})");
                    if (gridX < 0 || gridX >= gridWidth || gridY < 0 || gridY >= gridHeight)
                    {
                        Debug.LogWarning($"[CanPlaceMutation] Position {gridX},{gridY} is out of bounds!");
                        return false;
                    }
                    if (grid[gridX, gridY] != null && grid[gridX, gridY] != element)
                    {
                        Debug.LogWarning($"[CanPlaceMutation] Cell at {gridX},{gridY} is occupied by {grid[gridX, gridY].mutation.objectName}!");
                        return false;
                    }
                }
            }
        }
        Debug.Log("[CanPlaceMutation] Placement is valid.");
        return true;
    }

    public void PlaceMutation(MutationUIElement element, Vector2Int position, Vector2Int size)
    {
        Debug.Log($"[PlaceMutation] Placing mutation {element.mutation.objectName} at {position} with size {size}");
        // Log the mutation shape as a matrix
        Debug.Log($"[PlaceMutation] Shape matrix for {element.mutation.objectName}:");
        for (int y = size.y - 1; y >= 0; y--)
        {
            string row = "";
            for (int x = 0; x < size.x; x++)
            {
                row += element.IsPositionFilled(x, y) ? "X " : ". ";
            }
            Debug.Log($"[PlaceMutation] {row}");
        }
        // Log the current state of the grid before placement
        Debug.Log("[PlaceMutation] Grid state before placement:");
        for (int y = gridHeight - 1; y >= 0; y--)
        {
            string row = "";
            for (int x = 0; x < gridWidth; x++)
            {
                row += grid[x, y] != null ? "X " : ". ";
            }
            Debug.Log($"[PlaceMutation] {row}");
        }
        // Clear the old position first
        ClearPosition(element);
        // Place the mutation in the new position
        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                if (element.IsPositionFilled(x, y))
                {
                    int gridX = position.x + x;
                    int gridY = position.y + y;
                    Debug.Log($"[PlaceMutation] Setting cell ({gridX},{gridY})");
                    if (gridX >= 0 && gridX < gridWidth && gridY >= 0 && gridY < gridHeight)
                    {
                        grid[gridX, gridY] = element;
                        if (visualGrid[gridX, gridY] != null)
                        {
                            visualGrid[gridX, gridY].GetComponent<Image>().color = new Color(1, 1, 1, 0);
                        }
                        Debug.Log($"[PlaceMutation] Placed cell at {gridX},{gridY}");
                    }
                }
            }
        }
        // Log the final state of the grid after placement
        Debug.Log("[PlaceMutation] Grid state after placement:");
        for (int y = gridHeight - 1; y >= 0; y--)
        {
            string row = "";
            for (int x = 0; x < gridWidth; x++)
            {
                row += grid[x, y] != null ? "X " : ". ";
            }
            Debug.Log($"[PlaceMutation] {row}");
        }
        element.SetGridPosition(position, GetCellSize());
        element.SetupButtonClick();
        PlayerUIManager.Instance.SetSelectedGameObject(element.gameObject);
    }

    public void ClearPosition(MutationUIElement element)
    {
        Debug.Log($"[ClearPosition] Clearing position for mutation: {element.mutation.objectName}");
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (grid[x, y] == element)
                {
                    Debug.Log($"[ClearPosition] Clearing cell ({x},{y})");
                    grid[x, y] = null;
                    if (visualGrid[x, y] != null)
                    {
                        visualGrid[x, y].GetComponent<Image>().color = emptySlotColor;
                    }
                    Debug.Log($"[ClearPosition] Cleared cell at {x},{y}");
                }
            }
        }
        // Log the grid state after clearing
        Debug.Log("[ClearPosition] Grid state after clearing:");
        for (int y = gridHeight - 1; y >= 0; y--)
        {
            string row = "";
            for (int x = 0; x < gridWidth; x++)
            {
                row += grid[x, y] != null ? "X " : ". ";
            }
            Debug.Log($"[ClearPosition] {row}");
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
        // Destroy all child objects
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // Reset grid arrays
        grid = new MutationUIElement[gridWidth, gridHeight];
        visualGrid = new GameObject[gridWidth, gridHeight];
    }

    /// <summary>
    /// Adds a mutation to the grid if space is available.
    /// </summary>
    public bool AddMutation(GeneticMutationObj mutation)
    {
        // Find first available spot
        for (int x = 0; x < gridWidth - GeneticMutationObj.MAX_SHAPE_SIZE + 1; x++)
        {
            for (int y = 0; y < gridHeight - GeneticMutationObj.MAX_SHAPE_SIZE + 1; y++)
            {
                Vector2Int position = new Vector2Int(x, y);

                // Create a temporary UI element to check placement
                GameObject newSlot = Instantiate(mutationSlotPrefab, transform);
                MutationUIElement uiElement = newSlot.GetComponent<MutationUIElement>();
                if (uiElement == null)
                {
                    uiElement = newSlot.AddComponent<MutationUIElement>();
                }
                
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

