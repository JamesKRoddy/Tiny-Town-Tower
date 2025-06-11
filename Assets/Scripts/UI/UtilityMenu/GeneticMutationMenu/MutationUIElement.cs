using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class MutationUIElement : MonoBehaviour
{
    private RectTransform rectTransform;
    private Image iconImage; // UI icon
    private bool isRotated = false;
    private GeneticMutationGrid grid;
    private Color originalColor;
    private Color selectedColor = new Color(0.5f, 1f, 0.5f, 1f); // Light green color for selection
    private bool isSelected = false;
    public GeneticMutationObj mutation; // Made public to access from GeneticMutationGrid
    private Button button;
    private List<GameObject> cellObjects = new List<GameObject>();

    public bool IsSelected => isSelected;

    public Vector2Int Size => isRotated ? 
        new Vector2Int(mutation.GetShapeSize().y, mutation.GetShapeSize().x) : 
        mutation.GetShapeSize();

    public void Initialize(GeneticMutationObj mutation, GeneticMutationGrid grid)
    {
        this.mutation = mutation;
        this.grid = grid;
        rectTransform = GetComponent<RectTransform>();
        iconImage = GetComponent<Image>();
        button = GetComponent<Button>();
        originalColor = iconImage.color;

        // Clear any existing cell objects
        ClearCellObjects();

        // Create cell objects for the shape
        CreateCellObjects();

        // Ensure proper anchoring and alignment
        rectTransform.anchorMin = new Vector2(0, 0); // Bottom-left anchor
        rectTransform.anchorMax = new Vector2(0, 0); // Bottom-left anchor
        rectTransform.pivot = new Vector2(0, 0); // Bottom-left pivot
    }

    private void CreateCellObjects()
    {
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
                    cellImage.sprite = mutation.sprite;
                    cellImage.color = originalColor;

                    // Set up RectTransform
                    RectTransform cellRect = cell.GetComponent<RectTransform>();
                    cellRect.anchorMin = new Vector2(0, 0);
                    cellRect.anchorMax = new Vector2(0, 0);
                    cellRect.pivot = new Vector2(0, 0);
                    cellRect.sizeDelta = new Vector2(cellSize.x, cellSize.y);
                    cellRect.anchoredPosition = new Vector2(x * cellSize.x, y * cellSize.y);

                    cellObjects.Add(cell);
                }
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
                cell.GetComponent<Image>().color = Color.red;
            }
        }
    }

    public void HideWarning()
    {
        foreach (var cell in cellObjects)
        {
            if (cell != null)
            {
                cell.GetComponent<Image>().color = isSelected ? selectedColor : originalColor;
            }
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        foreach (var cell in cellObjects)
        {
            if (cell != null)
            {
                cell.GetComponent<Image>().color = selected ? selectedColor : originalColor;
            }
        }
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

        // Force layout update
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    public void Rotate()
    {
        if (!isRotated)
        {
            isRotated = true;
            rectTransform.rotation = Quaternion.Euler(0, 0, 90);
            
            // Update size after rotation
            Vector2 cellSize = grid.GetCellSize();
            Vector2 newSize = new Vector2(
                cellSize.x * mutation.GetShapeSize().y,
                cellSize.y * mutation.GetShapeSize().x
            );
            rectTransform.sizeDelta = newSize;

            // Update cell positions
            UpdateCellPositions();
        }
        else
        {
            isRotated = false;
            rectTransform.rotation = Quaternion.identity;
            
            // Reset size after rotation
            Vector2 cellSize = grid.GetCellSize();
            Vector2 newSize = new Vector2(
                cellSize.x * mutation.GetShapeSize().x,
                cellSize.y * mutation.GetShapeSize().y
            );
            rectTransform.sizeDelta = newSize;

            // Update cell positions
            UpdateCellPositions();
        }

        // Force layout update after rotation
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    private void UpdateCellPositions()
    {
        Vector2 cellSize = grid.GetCellSize();
        ClearCellObjects();
        CreateCellObjects();
    }

    public bool IsPositionFilled(int x, int y)
    {
        if (isRotated)
        {
            // When rotated, we need to transform the coordinates
            int tempX = y;
            int tempY = GeneticMutationObj.MAX_SHAPE_SIZE - 1 - x;
            return mutation.IsPositionFilled(tempX, tempY);
        }
        return mutation.IsPositionFilled(x, y);
    }

    private void OnDestroy()
    {
        ClearCellObjects();
    }
}
