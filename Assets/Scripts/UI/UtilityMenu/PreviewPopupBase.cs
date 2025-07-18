using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using TMPro;


public abstract class PreviewPopupBase<TItem, TCategory> : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] protected Button closeButton;

    protected TItem currentItem;
    protected PreviewListMenuBase<TCategory, TItem> parentMenu;
    protected GameObject selectedElement;
    public bool isActive = false;

    [Header("Tootlip Options")]
    [SerializeField] protected GameObject tooltip;
    [SerializeField] protected TMP_Text tooltipText;

    protected virtual void Start()
    {
        HideTooltip();
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);
    }

/// <summary>
/// Displaying the popup
/// </summary>
/// <param name="item"></param> Item context in the popup
/// <param name="menu"></param> Menu that opened the popup
/// <param name="element"></param> Gameobject that will be selected when the popup is closed, usually the button that opened it
    public virtual void DisplayPopup(TItem item, PreviewListMenuBase<TCategory, TItem> menu, GameObject element = null)
    {
        Debug.Log($"Displaying popup for {item}");
        currentItem = item;
        parentMenu = menu;
        selectedElement = element ?? menu.FirstSelectedElement;
        
        // Disable all buttons in the parent UI except popup buttons
        SetParentUIButtonsInteractable(false);

        // Show popup
        gameObject.SetActive(true);

        isActive = true;

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
        PlayerUIManager.Instance.SetSelectedGameObject(obj);
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
        isActive = false;

        if (selectedElement != null)
        {
            PlayerUIManager.Instance.SetSelectedGameObject(selectedElement);
        }
    }

    protected virtual void ShowTooltip(int optionIndex)
    {
        if(tooltip != null)
            tooltip.SetActive(true);
        else
            Debug.LogWarning("Tooltip is not set");
    }

    protected virtual void HideTooltip()
    {
        if(tooltip != null)
            tooltip.SetActive(false);
    }
}


