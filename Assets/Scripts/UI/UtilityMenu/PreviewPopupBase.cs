using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;


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

/// <summary>
/// Displaying the popup
/// </summary>
/// <param name="item"></param> Item context in the popup
/// <param name="menu"></param> Menu that opened the popup
/// <param name="element"></param> Gameobject that will be selected when the popup is closed, usually the button that opened it
    public virtual void Setup(TItem item, PreviewListMenuBase<TCategory, TItem> menu, GameObject element = null)
    {
        currentItem = item;
        parentMenu = menu;
        selectedElement = element ?? closeButton?.gameObject;
        
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
        if (closeButton != null)
        {
            // Use a small delay to prevent immediate button press
            StartCoroutine(SetSelectedAfterDelay(closeButton.gameObject));
        }
    }

    private IEnumerator SetSelectedAfterDelay(GameObject obj)
    {
        yield return new WaitForEndOfFrame();
        EventSystem.current.SetSelectedGameObject(obj);
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


