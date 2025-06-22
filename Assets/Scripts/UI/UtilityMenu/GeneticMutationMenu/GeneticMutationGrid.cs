using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

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
        Debug.Log($"Max slots from player inventory: {maxSlots}");

        int possessedNPCSlots = 0;

        //Get mutation slots on possessed NPC
        if(PlayerController.Instance._possessedNPC != null){
            possessedNPCSlots = (PlayerController.Instance._possessedNPC as SettlerNPC).additionalMutationSlots;
            Debug.Log($"Max slots from possessed NPC: {possessedNPCSlots}");
        }

        //Combine them
        maxSlots = maxSlots + possessedNPCSlots;
        Debug.Log($"Total max slots: {maxSlots}");
        
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

    public bool CanPlaceMutation(Vector2Int position, MutationUIElement mutationElement)
    {
        // First check if the position is valid for the mutation
        if (!mutationElement.IsPositionValid(position, gridWidth, gridHeight))
        {
            return false;
        }

        // Get the filled cell positions relative to the mutation's origin
        var filledPositions = mutationElement.GetFilledCellLocalPositions();

        // Check each filled position for collisions
        foreach (var localPos in filledPositions)
        {
            Vector2Int gridPos = position + localPos;
            
            // Check if the cell is within grid bounds
            if (gridPos.x < 0 || gridPos.x >= gridWidth || gridPos.y < 0 || gridPos.y >= gridHeight)
            {
                return false;
            }

            // Check if there's already a mutation at this position
            if (grid[gridPos.x, gridPos.y] != null)
            {
                return false;
            }
        }

        return true;
    }

    public void PlaceMutation(MutationUIElement element, Vector2Int position, Vector2Int size)
    {
        // Clear any existing positions for this element
        ClearPosition(element);

        // Place the element in all its filled positions
        var filledPositions = element.GetFilledCellLocalPositions();

        foreach (var localPos in filledPositions)
        {
            int gridX = position.x + localPos.x;
            int gridY = position.y + localPos.y;

            if (gridX >= 0 && gridX < gridWidth && gridY >= 0 && gridY < gridHeight)
            {
                grid[gridX, gridY] = element;
                if (visualGrid[gridX, gridY] != null)
                    visualGrid[gridX, gridY].GetComponent<Image>().color = new Color(1, 1, 1, 0);
            }
            else
            {
                Debug.LogWarning($"[PlaceMutation] Attempted to place cell outside grid at {new Vector2Int(gridX, gridY)}");
            }
        }

        element.SetGridPosition(position, GetCellSize());
        element.SetupButtonClick();
        PlayerUIManager.Instance.SetSelectedGameObject(element.gameObject);
    }

    public void ClearPosition(MutationUIElement element)
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (grid[x, y] == element)
                {
                    grid[x, y] = null;
                    if (visualGrid[x, y] != null)
                        visualGrid[x, y].GetComponent<Image>().color = emptySlotColor;
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

