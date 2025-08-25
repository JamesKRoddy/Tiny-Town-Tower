using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class PreviewListMenuBase<TCategory, TItem> : MenuBase
{
    [Header("Shared Menu Components")]
    [SerializeField] protected TMP_Text screenTitle;
    [SerializeField] protected Button rightScreenBtn;
    [SerializeField] protected Button leftScreenBtn;
    [SerializeField] protected RectTransform screenParent;
    [SerializeField] protected GameObject screenPrefab;
    [SerializeField] protected GameObject itemButtonPrefab;

    [Header("Shared Preview UI")]
    [SerializeField] protected Image previewImage;
    [SerializeField] protected TMP_Text previewName;
    [SerializeField] protected TMP_Text previewDesc;
    [SerializeField] protected PreviewPopupBase<TItem, TCategory> selectionPopup;

    protected Dictionary<TCategory, GameObject> screens = new Dictionary<TCategory, GameObject>();
    protected TCategory currentCategory;
    protected TItem currentSelectedItem;
    public GameObject FirstSelectedElement => leftScreenBtn.gameObject; //Used for when opening the screen, the first element is selected

    public abstract IEnumerable<TItem> GetItems();
    public abstract TCategory GetItemCategory(TItem item);
    public abstract void SetupItemButton(TItem item, GameObject button);
    public abstract Sprite GetPreviewSprite(TItem item);
    public abstract string GetPreviewDescription(TItem item);
    public abstract string GetPreviewName(TItem item);
    public abstract IEnumerable<(string resourceName, int requiredCount, int playerCount)> GetPreviewResourceCosts(TItem item);
    public abstract void UpdatePreviewSpecifics(TItem item);
    public abstract void DestroyPreviewSpecifics();


    public override void SetScreenActive(bool active, float delay = 0.0f, Action onDone = null)
    {
        Debug.Log($"[PreviewListMenuBase] SetScreenActive called with active: {active}");
        base.SetScreenActive(active, delay, () => {
            Debug.Log($"[PreviewListMenuBase] Base SetScreenActive callback executed, active: {active}");
            if (active)
            {
                Debug.Log($"[PreviewListMenuBase] Calling RefreshUIAndSelectFirst");
                RefreshUIAndSelectFirst();
                SetupScreenButtons();
            }
            else
            {
                if (selectionPopup != null && selectionPopup.isActive)
                {
                    selectionPopup.OnCloseClicked();
                }
                CleanupScreens();
                CleanupScreenButtons();
            }
            onDone?.Invoke();
        });
    }    

    protected override void BackPressed(){
        if(selectionPopup != null && selectionPopup.isActive){
            selectionPopup.OnCloseClicked();
        }
        else{
            PlayerUIManager.Instance.BackPressed();
        }
    }

    private void SetupScreenButtons()
    {
        if (rightScreenBtn != null)
        {
            rightScreenBtn.onClick.AddListener(() => SwitchScreen(true));
        }
        if (leftScreenBtn != null)
        {
            leftScreenBtn.onClick.AddListener(() => SwitchScreen(false));
        }
    }

    private void CleanupScreenButtons()
    {
        if (rightScreenBtn != null)
        {
            rightScreenBtn.onClick.RemoveAllListeners();
        }
        if (leftScreenBtn != null)
        {
            leftScreenBtn.onClick.RemoveAllListeners();
        }
    }
    protected void RefreshUIAndSelectFirst()
    {
        SetupScreens();
        if (screens.Count > 0)
        {
            currentCategory = new List<TCategory>(screens.Keys)[0];
            UpdateActiveScreen();
        }
        else if (leftScreenBtn != null)
        {
            PlayerUIManager.Instance.SetSelectedGameObject(FirstSelectedElement);
        }
    }

    public override void SetPlayerControls(PlayerControlType controlType){

        base.SetPlayerControls(controlType);
        
        switch (controlType)
        {
            case PlayerControlType.IN_MENU:
                PlayerInput.Instance.OnRBPressed += rightScreenBtn.onClick.Invoke;
                PlayerInput.Instance.OnLBPressed += leftScreenBtn.onClick.Invoke;
                break;
            default:
                break;
        }
    }
    protected virtual void CleanupScreens()
    {
        foreach (var screen in screens.Values)
        {
            if (screen != null)
            {
                Destroy(screen);
            }
        }
        screens.Clear();
    }

    protected void SetupScreens()
    {
        Debug.Log($"[PreviewListMenuBase] SetupScreens called");
        CleanupScreens();

        Debug.Log($"[PreviewListMenuBase] About to call GetItems()");
        var items = GetItems();
        Debug.Log($"[PreviewListMenuBase] GetItems() returned, processing items");
        
        int itemCount = 0;
        foreach (var item in items)
        {
            itemCount++;
            Debug.Log($"[PreviewListMenuBase] Processing item {itemCount}: {item}");
            
            TCategory category = GetItemCategory(item);
            if (!screens.ContainsKey(category))
            {
                GameObject screen = Instantiate(screenPrefab, screenParent);
                screens[category] = screen;
                screen.name = $"{category} Screen";
            }

            GameObject screenForCategory = screens[category];
            GameObject itemButton = Instantiate(itemButtonPrefab, screenForCategory.transform);
            SetupItemButton(item, itemButton);

            SetupItemButtonEvents(itemButton, item);
        }
        
        Debug.Log($"[PreviewListMenuBase] SetupScreens completed, processed {itemCount} items");
    }

    protected virtual void SetupItemButtonEvents(GameObject itemButton, TItem item)
    {
        EventTrigger trigger = itemButton.AddComponent<EventTrigger>();

        // Handle OnSelect
        EventTrigger.Entry selectEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.Select
        };
        selectEntry.callback.AddListener((data) => OnItemSelected(item));
        trigger.triggers.Add(selectEntry);

        // Handle OnDeselect
        EventTrigger.Entry deselectEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.Deselect
        };
        deselectEntry.callback.AddListener((data) => OnItemDeselected());
        trigger.triggers.Add(deselectEntry);

        // Handle OnClick
        Button button = itemButton.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => OnItemClicked(item));
        }
    }

    protected virtual void OnItemSelected(TItem item)
    {
        UpdatePreview(item);
    }

    protected virtual void OnItemDeselected()
    {
        UpdatePreview(default);
    }

    protected virtual void OnItemClicked(TItem item)
    {
        // Override in derived classes to handle click events
    }

    protected void SwitchScreen(bool forward)
    {
        var categories = new List<TCategory>(screens.Keys);
        
        if (categories.Count == 0)
        {
            Debug.LogWarning("No categories available to switch between.");
            return;
        }

        int currentIndex = categories.IndexOf(currentCategory);
        currentIndex = forward
            ? (currentIndex + 1) % categories.Count
            : (currentIndex - 1 + categories.Count) % categories.Count;

        currentCategory = categories[currentIndex];
        UpdateActiveScreen();
    }

    protected void UpdateActiveScreen()
    {
        foreach (var kvp in screens)
        {
            kvp.Value.SetActive(kvp.Key.Equals(currentCategory));
        }
        
        if (screenTitle != null)
        {
            screenTitle.text = currentCategory?.ToString() ?? "No Category";
        }

        GameObject selectedObject = null;
        if (screens.ContainsKey(currentCategory) && screens[currentCategory].transform.childCount > 0)
        {
            selectedObject = screens[currentCategory].transform.GetChild(0).gameObject;
        }
        else if (leftScreenBtn != null)
        {
            selectedObject = FirstSelectedElement;
        }

        if (selectedObject != null)
        {
            PlayerUIManager.Instance.SetSelectedGameObject(selectedObject);
        }
    }

    public void DisplayPopup(TItem item, GameObject selectedObject = null){
        if (selectionPopup != null)
        {
            selectionPopup.DisplayPopup(item, this, selectedObject);
        }
    }

    public virtual void UpdatePreview(TItem item = default)
    {
        currentSelectedItem = item;        

        if (item != null)
        {
            if (previewName != null)
            {
                previewName.text = GetPreviewName(item);
            }
            if (previewImage != null)
            {
                previewImage.sprite = GetPreviewSprite(item);
            }
            if (previewDesc != null)
            {
                previewDesc.text = GetPreviewDescription(item);
            }

            UpdatePreviewSpecifics(item);
        }
        else
        {
            if (previewName != null)
            {
                previewName.text = string.Empty;
            }
            if (previewImage != null)
            {
                previewImage.sprite = null;
            }
            if (previewDesc != null)
            {
                previewDesc.text = string.Empty;
            }

            DestroyPreviewSpecifics();
        }
    }
}

