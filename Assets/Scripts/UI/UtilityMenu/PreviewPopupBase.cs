using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public abstract class PreviewPopupBase<TItem, TCategory> : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] protected Button closeButton;

    protected TItem currentItem;
    protected PreviewListMenuBase<TCategory, TItem> parentMenu;
    protected GameObject selectedElement;

    protected virtual void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);
    }

    public virtual void Setup(TItem item, PreviewListMenuBase<TCategory, TItem> menu, GameObject element)
    {
        currentItem = item;
        parentMenu = menu;
        selectedElement = element;
        
        // Disable all buttons in the parent UI except popup buttons
        SetParentUIButtonsInteractable(false);

        // Show popup
        gameObject.SetActive(true);

        // Set first button as selected
        SetupInitialSelection();
    }

    protected virtual void SetupInitialSelection()
    {
        // Override in derived classes to set initial button selection
    }

    protected void SetParentUIButtonsInteractable(bool interactable)
    {
        if (parentMenu == null) return;

        // Get all buttons in the parent UI
        var parentButtons = parentMenu.GetComponentsInChildren<Button>();
        foreach (var button in parentButtons)
        {
            // Skip buttons that are part of this popup
            if (button.transform.IsChildOf(transform)) continue;
            button.interactable = interactable;
        }
    }

    public virtual void OnCloseClicked()
    {
        // Re-enable parent UI buttons and close popup
        SetParentUIButtonsInteractable(true);
        gameObject.SetActive(false);

        if (selectedElement != null)
        {
            EventSystem.current.SetSelectedGameObject(selectedElement);
        }
    }
}


