using UnityEngine;
using UnityEngine.UI;

public class MutationUIElement : MonoBehaviour
{
    public GeneticMutationObj mutation;
    private RectTransform rectTransform;
    private Image iconImage; // UI icon
    private bool isRotated = false;

    public Vector2Int Size => isRotated ? new Vector2Int(mutation.size.y, mutation.size.x) : mutation.size;

    public void Initialize(GeneticMutationObj mutation, GeneticMutationGrid grid)
    {
        this.mutation = mutation;
        rectTransform = GetComponent<RectTransform>();
        iconImage = GetComponent<Image>();

        if (mutation.sprite != null)
        {
            iconImage.sprite = mutation.sprite; // Set the UI icon
        }
    }

    public void SetGridPosition(Vector2Int position)
    {
        rectTransform.anchoredPosition = new Vector2(position.x * 50, position.y * 50);
    }

    public void Rotate()
    {
        isRotated = !isRotated;
        rectTransform.rotation = Quaternion.Euler(0, 0, isRotated ? 90 : 0);
    }
}
