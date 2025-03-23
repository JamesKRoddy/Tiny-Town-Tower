using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GeneticMutationSelectPopup : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button removeButton;
    [SerializeField] private Button moveButton;
    [SerializeField] private Button closeButton;

    private GeneticMutationObj currentMutation;
    private GeneticMutationUI mutationUI;
    private MutationUIElement uiElement;
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

    public void Setup(GeneticMutationObj mutation, GeneticMutationUI ui, MutationUIElement element)
    {
        currentMutation = mutation;
        mutationUI = ui;
        uiElement = element;
        // Disable all buttons in the parent UI except popup buttons
        SetParentUIButtonsInteractable(false);

        // Show popup
        gameObject.SetActive(true);

        // Set remove button as selected
        if (removeButton != null)
        {
            EventSystem.current.SetSelectedGameObject(removeButton.gameObject);
        }
    }

    private void SetParentUIButtonsInteractable(bool interactable)
    {
        if (mutationUI == null) return;

        // Get all buttons in the parent UI
        var parentButtons = mutationUI.GetComponentsInChildren<Button>();
        foreach (var button in parentButtons)
        {
            // Skip buttons that are part of this popup
            if (button.transform.IsChildOf(transform)) continue;
            button.interactable = interactable;
        }
    }

    private void OnRemoveClicked()
    {
        if (currentMutation == null || mutationUI == null) return;

        // Remove mutation from inventory and add back to quantities
        PlayerInventory.Instance.RemoveMutation(currentMutation);
        GeneticMutationSystem.Instance.RemoveMutation(currentMutation);
        mutationUI.AddMutationBackToQuantities(currentMutation);

        // Re-enable parent UI buttons and close popup
        SetParentUIButtonsInteractable(true);
        gameObject.SetActive(false);
    }

    private void OnMoveClicked()
    {
        if (currentMutation == null || mutationUI == null) return;

        // Start moving the mutation
        mutationUI.SelectMutation(uiElement);
        // Re-enable parent UI buttons and close popup
        SetParentUIButtonsInteractable(true);
        gameObject.SetActive(false);

        // Switch to movement controls
        PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.GENETIC_MUTATION_MOVEMENT);
    }

    private void OnCloseClicked()
    {
        // Re-enable parent UI buttons and close popup
        SetParentUIButtonsInteractable(true);
        gameObject.SetActive(false);

        EventSystem.current.SetSelectedGameObject(uiElement.gameObject);
        
    }
}
