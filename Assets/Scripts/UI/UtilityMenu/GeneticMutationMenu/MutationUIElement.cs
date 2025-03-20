using UnityEngine;
using UnityEngine.UI;

public class MutationUIElement : MonoBehaviour
{
    private RectTransform rectTransform;
    private Image iconImage; // UI icon
    private bool isRotated = false;
    private Vector2Int mutationSize; // Store size instead of keeping a reference to mutation

    public Vector2Int Size => isRotated ? new Vector2Int(mutationSize.y, mutationSize.x) : mutationSize;

    public void Initialize(GeneticMutationObj mutation, GeneticMutationGrid grid)
    {
        mutationSize = mutation.size; // Store the size instead of keeping the entire object reference
        rectTransform = GetComponent<RectTransform>();
        iconImage = GetComponent<Image>();

        if (mutation.sprite != null)
        {
            iconImage.sprite = mutation.sprite; // Set the UI icon
        }
    }

    public void SetGridPosition(Vector2Int position, Vector2 cellSize)
    {
        if (rectTransform == null) return;

        // Offset position so the mutation is centered in the grid cell
        Vector2 anchoredPosition = new Vector2(position.x * cellSize.x, position.y * cellSize.y);
        rectTransform.anchoredPosition = anchoredPosition;
    }


    public void Rotate()
    {
        isRotated = !isRotated;
        rectTransform.rotation = Quaternion.Euler(0, 0, isRotated ? 90 : 0);
    }
}
