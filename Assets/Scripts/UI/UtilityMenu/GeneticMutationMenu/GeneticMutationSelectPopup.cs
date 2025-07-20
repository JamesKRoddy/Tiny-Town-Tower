using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GeneticMutationSelectPopup : PreviewPopupBase<GeneticMutationObj, GeneticMutation>
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

    public override void DisplayPopup(GeneticMutationObj mutation, PreviewListMenuBase<GeneticMutation, GeneticMutationObj> menu, GameObject element)
    {
        base.DisplayPopup(mutation, menu, element);
        uiElement = element.GetComponent<MutationUIElement>();

        // Set remove button as selected
        if (removeButton != null)
        {
            PlayerUIManager.Instance.SetSelectedGameObject(removeButton.gameObject);
        }
    }

    private void OnRemoveClicked()
    {
        if (currentItem == null || parentMenu == null) return;

        var geneticMenu = parentMenu as GeneticMutationUI;
        if (geneticMenu == null) return;

        // Remove mutation from inventory and add back to quantities
        PlayerInventory.Instance.RemoveMutation(currentItem);
        geneticMenu.AddMutationBackToQuantities(currentItem);
        Destroy(uiElement.gameObject);
        // Re-enable parent UI buttons and close popup
        SetParentUIButtonsInteractable(true);
        gameObject.SetActive(false);
    }

    private void OnMoveClicked()
    {
        if (currentItem == null || parentMenu == null) return;

        var geneticMenu = parentMenu as GeneticMutationUI;
        if (geneticMenu == null) return;

        // Start moving the mutation
        geneticMenu.SelectMutation(uiElement);
        // Re-enable parent UI buttons and close popup
        SetParentUIButtonsInteractable(true);
        gameObject.SetActive(false);

        // Switch to movement controls (uses delayed update to prevent immediate A button press)
        PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.GENETIC_MUTATION_MOVEMENT);
    }
}
