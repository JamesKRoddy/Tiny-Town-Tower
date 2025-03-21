using UnityEngine;
using UnityEngine.UI;

public class MutationUIElement : MonoBehaviour
{
    private RectTransform rectTransform;
    private Image iconImage; // UI icon
    private bool isRotated = false;
    private Vector2Int mutationSize; // Store size instead of keeping a reference to mutation
    private GeneticMutationGrid grid;
    private Color originalColor;

    public Vector2Int Size => isRotated ? new Vector2Int(mutationSize.y, mutationSize.x) : mutationSize;

    public void Initialize(GeneticMutationObj mutation, GeneticMutationGrid grid)
    {
        this.grid = grid;
        mutationSize = mutation.size; // Store the size instead of keeping the entire object reference
        rectTransform = GetComponent<RectTransform>();
        iconImage = GetComponent<Image>();
        originalColor = iconImage.color;

        if (mutation.sprite != null)
        {
            iconImage.sprite = mutation.sprite; // Set the UI icon
        }

        // Ensure proper anchoring and alignment
        rectTransform.anchorMin = new Vector2(0, 0); // Bottom-left anchor
        rectTransform.anchorMax = new Vector2(0, 0); // Bottom-left anchor
        rectTransform.pivot = new Vector2(0, 0); // Bottom-left pivot
    }

    public void ShowWarning()
    {
        iconImage.color = Color.red;
    }

    public void HideWarning()
    {
        iconImage.color = originalColor;
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
                cellSize.x * mutationSize.y,
                cellSize.y * mutationSize.x
            );
            rectTransform.sizeDelta = newSize;
        }
        else
        {
            isRotated = false;
            rectTransform.rotation = Quaternion.identity;
            
            // Reset size after rotation
            Vector2 cellSize = grid.GetCellSize();
            Vector2 newSize = new Vector2(
                cellSize.x * mutationSize.x,
                cellSize.y * mutationSize.y
            );
            rectTransform.sizeDelta = newSize;
        }

        // Force layout update after rotation
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }
}
