using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class MutationUIElement : MonoBehaviour
{
    private RectTransform rectTransform;
    private Image iconImage; // UI icon
    private GeneticMutationGrid grid;
    
    private bool isSelected = false;
    private bool isHighlighted = false;
    public GeneticMutationObj mutation; // Made public to access from GeneticMutationGrid
    private Button button;
    private List<GameObject> cellObjects = new List<GameObject>();
    private GeneticMutationObj.RotationState currentRotation = GeneticMutationObj.RotationState.ROT_0;

    public bool IsSelected => isSelected;

    // Use the actual bounding box size for movement/placement
    public Vector2Int Size => mutation.GetActualSizeForRotation(currentRotation);

    public void Initialize(GeneticMutationObj mutation, GeneticMutationGrid grid)
    {
        button = GetComponent<Button>();
        if (button == null)
        {
            button = gameObject.AddComponent<Button>();
        }

        if (mutation == null)
        {
            Debug.LogError("MutationUIElement.Initialize: mutation is null!");
            return;
        }

        if (grid == null)
        {
            Debug.LogError("MutationUIElement.Initialize: grid is null!");
            return;
        }

        this.mutation = mutation;
        this.grid = grid;
        rectTransform = GetComponent<RectTransform>();
        
        // Set the parent container to be transparent
        iconImage = GetComponent<Image>();
        if (iconImage != null)
        {
            iconImage.color = new Color(1, 1, 1, 0);
        }
        
        // Set up button events using EventTrigger
        SetupButtonEvents();

        // Clear any existing cell objects
        ClearCellObjects();

        Vector2 cellSize = grid.GetCellSize();
        rectTransform.sizeDelta = new Vector2(
            cellSize.x * GeneticMutationObj.MAX_SHAPE_SIZE,
            cellSize.y * GeneticMutationObj.MAX_SHAPE_SIZE
        );

        // Create cell objects for the shape
        CreateCellObjects();

        // Ensure proper anchoring and alignment
        rectTransform.anchorMin = new Vector2(0, 0); // Bottom-left anchor
        rectTransform.anchorMax = new Vector2(0, 0); // Bottom-left anchor
        rectTransform.pivot = new Vector2(0, 0); // Bottom-left pivot
    }

    private void SetupButtonEvents()
    {
        EventTrigger trigger = GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = gameObject.AddComponent<EventTrigger>();
        }

        // Handle OnSelect
        EventTrigger.Entry selectEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.Select
        };
        selectEntry.callback.AddListener((data) => OnSelect());
        trigger.triggers.Add(selectEntry);

        // Handle OnDeselect
        EventTrigger.Entry deselectEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.Deselect
        };
        deselectEntry.callback.AddListener((data) => OnDeselect());
        trigger.triggers.Add(deselectEntry);

        // Handle OnClick
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
    }

    private void OnSelect()
    {
        isHighlighted = true;
        UpdateCellColors();
    }

    private void OnDeselect()
    {
        isHighlighted = false;
        UpdateCellColors();
    }

    public void OnPointerEnter(PointerEventData eventData) { }
    public void OnPointerExit(PointerEventData eventData) { }

    private void CreateCellObjects()
    {
        if (mutation == null || grid == null)
        {
            Debug.LogError("MutationUIElement.CreateCellObjects: mutation or grid is null!");
            return;
        }

        Vector2 cellSize = grid.GetCellSize();
        var boundingBox = GetCurrentBoundingBox();
        bool[,] rotatedShape = mutation.GetRotatedShape(currentRotation);

        // Set the container size to match the actual size of the mutation
        rectTransform.sizeDelta = new Vector2(
            cellSize.x * boundingBox.width,
            cellSize.y * boundingBox.height
        );

        for (int y = 0; y < GeneticMutationObj.MAX_SHAPE_SIZE; y++)
        {
            for (int x = 0; x < GeneticMutationObj.MAX_SHAPE_SIZE; x++)
            {
                if (rotatedShape[x, y])
                {
                    GameObject cell = new GameObject($"Cell_{x}_{y}");
                    cell.transform.SetParent(transform, false);

                    // Add Image component
                    Image cellImage = cell.AddComponent<Image>();
                    if (mutation.sprite != null)
                    {
                        cellImage.sprite = mutation.sprite;
                    }
                    else
                    {
                        Debug.LogWarning($"Mutation {mutation.objectName} has no sprite assigned!");
                    }
                    cellImage.color = button.colors.normalColor;

                    // Set up RectTransform
                    RectTransform cellRect = cell.GetComponent<RectTransform>();
                    cellRect.anchorMin = new Vector2(0, 0);
                    cellRect.anchorMax = new Vector2(0, 0);
                    cellRect.pivot = new Vector2(0, 0);
                    cellRect.sizeDelta = new Vector2(cellSize.x * 0.9f, cellSize.y * 0.9f); // Make it slightly smaller than the grid cell

                    // Position the cell relative to the bounding box
                    cellRect.anchoredPosition = new Vector2(
                        (x - boundingBox.minX) * cellSize.x,
                        (y - boundingBox.minY) * cellSize.y
                    );

                    cellObjects.Add(cell);
                }
            }
        }
    }

    private void UpdateCellColors()
    {
        Color targetColor = button.colors.normalColor;
        if (isSelected)
            targetColor = button.colors.selectedColor;
        else if (isHighlighted)
            targetColor = button.colors.highlightedColor;

        foreach (var cell in cellObjects)
        {
            if (cell != null)
            {
                cell.GetComponent<Image>().color = targetColor;
            }
        }
    }

    private void ClearCellObjects()
    {
        foreach (var cell in cellObjects)
        {
            if (cell != null)
            {
                Destroy(cell);
            }
        }
        cellObjects.Clear();
    }

    public void SetupButtonClick()
    {
        // Ensure the main mutation object has a button
        if (button == null)
        {
            button = GetComponent<Button>();
        }
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnButtonClicked);
        }
    }

    private void OnButtonClicked()
    {
        if (mutation != null)
        {
            var mutationUI = GetComponentInParent<GeneticMutationUI>();
            if (mutationUI != null)
            {
                mutationUI.OnMutationClicked(mutation, this);
            }
        }
    }

    public void ShowWarning()
    {
        foreach (var cell in cellObjects)
        {
            if (cell != null)
            {
                cell.GetComponent<Image>().color = button.colors.disabledColor;
            }
        }
    }

    public void HideWarning()
    {
        UpdateCellColors();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateCellColors();
    }

    public void SetGridPosition(Vector2Int position, Vector2 cellSize)
    {
        if (rectTransform == null) return;
        
        // Position is already in grid coordinates, no need to adjust for bounding box
        Vector2 anchoredPosition = new Vector2(
            position.x * cellSize.x,
            position.y * cellSize.y
        );
        
        rectTransform.anchoredPosition = anchoredPosition;
        gameObject.SetActive(true);
    }

    public bool IsPositionValid(Vector2Int position, int gridWidth, int gridHeight)
    {
        var boundingBox = GetCurrentBoundingBox();
        Vector2Int size = Size;

        // Check if the position would place any cell outside the grid
        if (position.x < 0 || position.y < 0 ||
            position.x + size.x > gridWidth ||
            position.y + size.y > gridHeight)
        {
            return false;
        }

        return true;
    }

    public Vector2Int GetClampedPosition(Vector2Int position, int gridWidth, int gridHeight)
    {
        var boundingBox = GetCurrentBoundingBox();
        Vector2Int size = Size;

        // Calculate the maximum allowed position that keeps all cells within bounds
        int maxX = gridWidth - size.x;
        int maxY = gridHeight - size.y;

        Vector2Int clamped = new Vector2Int(
            Mathf.Clamp(position.x, 0, maxX),
            Mathf.Clamp(position.y, 0, maxY)
        );

        return clamped;
    }

    public void RotateLeft()
    {
        currentRotation = (GeneticMutationObj.RotationState)(((int)currentRotation + 3) % 4);
        UpdateRotation();
    }

    public void RotateRight()
    {
        currentRotation = (GeneticMutationObj.RotationState)(((int)currentRotation + 1) % 4);
        UpdateRotation();
    }

    private void UpdateRotation()
    {
        ClearCellObjects();
        CreateCellObjects();
        UpdateCellColors();
    }

    // Returns the current shape grid
    public bool[,] GetCurrentShapeGrid()
    {
        return mutation.GetRotatedShape(currentRotation);
    }

    public bool IsPositionFilled(int x, int y)
    {
        int N = GeneticMutationObj.MAX_SHAPE_SIZE;
        if (x < 0 || y < 0 || x >= N || y >= N)
            return false;
        var grid = GetCurrentShapeGrid();
        return grid[x, y];
    }

    private void OnDestroy()
    {
        ClearCellObjects();
    }

    // Enumerates all filled cell local positions for the current rotation state
    public IEnumerable<Vector2Int> GetFilledCellLocalPositions()
    {
        int N = GeneticMutationObj.MAX_SHAPE_SIZE;
        var grid = GetCurrentShapeGrid();
        var box = GetCurrentBoundingBox();
        for (int y = 0; y < N; y++)
        {
            for (int x = 0; x < N; x++)
            {
                if (grid[x, y])
                {
                    // Normalize to bounding box
                    yield return new Vector2Int(x - box.minX, y - box.minY);
                }
            }
        }
    }

    // Returns the bounding box for the current rotation state
    public (int minX, int minY, int width, int height) GetCurrentBoundingBox()
    {
        return mutation.GetBoundingBoxForRotation(currentRotation);
    }
}
