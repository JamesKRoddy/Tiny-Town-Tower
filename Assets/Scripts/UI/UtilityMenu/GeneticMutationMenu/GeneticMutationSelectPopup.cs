using UnityEngine;
using UnityEngine.UI;

public class GeneticMutationSelectPopup : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button removeButton;
    [SerializeField] private Button moveButton;
    [SerializeField] private Button closeButton;

    private GeneticMutationObj currentMutation;
    private GeneticMutationUI mutationUI;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Setup button listeners
        if (removeButton != null)
            removeButton.onClick.AddListener(OnRemoveClicked);
        if (moveButton != null)
            moveButton.onClick.AddListener(OnMoveClicked);
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);
    }

    public void Setup(GeneticMutationObj mutation, GeneticMutationUI ui)
    {
        currentMutation = mutation;
        mutationUI = ui;

        // Show popup
        gameObject.SetActive(true);
    }

    private void OnRemoveClicked()
    {
        if (currentMutation == null || mutationUI == null) return;

        // Remove mutation from inventory and add back to quantities
        PlayerInventory.Instance.RemoveMutation(currentMutation);
        GeneticMutationSystem.Instance.RemoveMutation(currentMutation);
        mutationUI.AddMutationBackToQuantities(currentMutation);

        // Close popup
        gameObject.SetActive(false);
    }

    private void OnMoveClicked()
    {
        if (currentMutation == null || mutationUI == null) return;

        // Start moving the mutation
        mutationUI.SelectMutation(currentMutation, true);

        // Close popup
        gameObject.SetActive(false);
    }

    private void OnCloseClicked()
    {
        gameObject.SetActive(false);
    }
}
