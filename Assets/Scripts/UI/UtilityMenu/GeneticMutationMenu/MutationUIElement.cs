using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class MutationUIElement : MonoBehaviour
{
    private RectTransform rectTransform;
    private Image iconImage; // UI icon
    private bool isRotated = false;
    private GeneticMutationGrid grid;
    
    private bool isSelected = false;
    private bool isHighlighted = false;
    public GeneticMutationObj mutation; // Made public to access from GeneticMutationGrid
    private Button button;
    private List<GameObject> cellObjects = new List<GameObject>();
    private (int minX, int minY, int width, int height) boundingBox;

    public bool IsSelected => isSelected;

    // Use the actual bounding box size for movement/placement
    public Vector2Int Size => isRotated
        ? new Vector2Int(mutation.GetActualSize().y, mutation.GetActualSize().x)
        : mutation.GetActualSize();

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

        boundingBox = mutation.GetBoundingBox();
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

    // Remove IPointerEnterHandler and IPointerExitHandler since we're using EventTrigger now
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
        
        for (int y = 0; y < GeneticMutationObj.MAX_SHAPE_SIZE; y++)
        {
            for (int x = 0; x < GeneticMutationObj.MAX_SHAPE_SIZE; x++)
            {
                if (mutation.IsPositionFilled(x, y))
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
                    
                    // Offset by bounding box minX/minY so the shape is tight
                    cellRect.anchoredPosition = GetCellPosition(x, y, cellSize);

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

    private Vector2 GetCellPosition(int x, int y, Vector2 cellSize)
    {
        int bx = boundingBox.minX, by = boundingBox.minY;
        if (isRotated)
        {
            // Rotate around the bounding box
            int relX = x - bx, relY = y - by;
            return new Vector2(
                (relY) * cellSize.x,
                (Size.x - 1 - relX) * cellSize.y
            );
        }
        else
        {
            return new Vector2(
                (x - bx) * cellSize.x,
                (y - by) * cellSize.y
            );
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

        // Calculate the position in pixels based on grid position and cell size
        Vector2 anchoredPosition = new Vector2(
            position.x * cellSize.x,
            position.y * cellSize.y
        );

        // Set the position
        rectTransform.anchoredPosition = anchoredPosition;

        // Ensure it's visible in the hierarchy
        gameObject.SetActive(true);
    }

    public void Rotate()
    {
        isRotated = !isRotated;
        
        // Update cell positions without rotating the container
        UpdateCellPositions();
    }

    private void UpdateCellPositions()
    {
        if (cellObjects.Count == 0) return;
        
        Vector2 cellSize = grid.GetCellSize();
        
        // Update each cell position based on rotation
        for (int i = 0; i < cellObjects.Count; i++)
        {
            GameObject cell = cellObjects[i];
            if (cell == null) continue;
            
            // Extract coordinates from name (Cell_X_Y)
            string[] parts = cell.name.Split('_');
            if (parts.Length < 3) continue;
            
            if (int.TryParse(parts[1], out int x) && int.TryParse(parts[2], out int y))
            {
                RectTransform cellRect = cell.GetComponent<RectTransform>();
                if (cellRect != null)
                {
                    cellRect.anchoredPosition = GetCellPosition(x, y, cellSize);
                }
            }
        }
    }

    public bool IsPositionFilled(int x, int y)
    {
        if (x < 0 || y < 0 || x >= GeneticMutationObj.MAX_SHAPE_SIZE || y >= GeneticMutationObj.MAX_SHAPE_SIZE)
            return false;
            
        if (isRotated)
        {
            // When rotated, we need to transform the coordinates
            int tempX = y;
            int tempY = GeneticMutationObj.MAX_SHAPE_SIZE - 1 - x;
            
            if (tempX < 0 || tempY < 0 || 
                tempX >= GeneticMutationObj.MAX_SHAPE_SIZE || 
                tempY >= GeneticMutationObj.MAX_SHAPE_SIZE)
                return false;
                
            return mutation.IsPositionFilled(tempX, tempY);
        }
        
        return mutation.IsPositionFilled(x, y);
    }

    private void OnDestroy()
    {
        ClearCellObjects();
    }
}
