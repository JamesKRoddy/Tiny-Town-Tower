using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GeneticMutationSelectPopup : PreviewPopupBase<GeneticMutationObj, GeneticMutation, GeneticMutationUI>
{
    [Header("UI Elements")]
    [SerializeField] private Button removeButton;
    [SerializeField] private Button moveButton;

    private MutationUIElement uiElement;

    protected override void Start()
    {
        base.Start();
        
        // Setup button listeners
        if (removeButton != null)
            removeButton.onClick.AddListener(OnRemoveClicked);
        if (moveButton != null)
            moveButton.onClick.AddListener(OnMoveClicked);
    }

    public override void Setup(GeneticMutationObj mutation, GeneticMutationUI menu, GameObject element)
    {
        base.Setup(mutation, menu, element);
        uiElement = element.GetComponent<MutationUIElement>();

        // Set remove button as selected
        if (removeButton != null)
        {
            EventSystem.current.SetSelectedGameObject(removeButton.gameObject);
        }
    }

    private void OnRemoveClicked()
    {
        if (currentItem == null || parentMenu == null) return;

        // Remove mutation from inventory and add back to quantities
        PlayerInventory.Instance.RemoveMutation(currentItem);
        parentMenu.AddMutationBackToQuantities(currentItem);
        Destroy(uiElement.gameObject);
        // Re-enable parent UI buttons and close popup
        SetParentUIButtonsInteractable(true);
        gameObject.SetActive(false);
    }

    private void OnMoveClicked()
    {
        if (currentItem == null || parentMenu == null) return;

        // Start moving the mutation
        parentMenu.SelectMutation(uiElement);
        // Re-enable parent UI buttons and close popup
        SetParentUIButtonsInteractable(true);
        gameObject.SetActive(false);

        // Switch to movement controls
        PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.GENETIC_MUTATION_MOVEMENT);
    }
}
