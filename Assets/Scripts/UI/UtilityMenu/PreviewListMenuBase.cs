using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class PreviewListMenuBase<TCategory, TItem> : MenuBase
{
    [Header("Shared Menu Components")]
    [SerializeField] TMP_Text screenTitle;
    public Button rightScreenBtn;
    public Button leftScreenBtn;
    [SerializeField] RectTransform screenParent;
    [SerializeField] GameObject screenPrefab;
    [SerializeField] GameObject itemButtonPrefab;

    [Header("Shared Preview UI")]
    [SerializeField] Image previewImage;
    [SerializeField] TMP_Text previewName;
    [SerializeField] TMP_Text previewDesc;

    protected Dictionary<TCategory, GameObject> screens = new Dictionary<TCategory, GameObject>();
    protected TCategory currentCategory;
    protected TItem currentSelectedItem;

    public abstract IEnumerable<TItem> GetItems();
    public abstract TCategory GetItemCategory(TItem item);
    public abstract void SetupItemButton(TItem item, GameObject button);
    public abstract Sprite GetPreviewSprite(TItem item);
    public abstract string GetPreviewDescription(TItem item);
    public abstract string GetPreviewName(TItem item);
    public abstract IEnumerable<(string resourceName, int requiredCount, int playerCount)> GetPreviewResourceCosts(TItem item);
    public abstract void UpdatePreviewSpecifics(TItem item);
    public abstract void DestroyPreviewSpecifics();

    public virtual void OnEnable()
    {
        RefreshUIAndSelectFirst();

        rightScreenBtn.onClick.AddListener(() => SwitchScreen(true));
        leftScreenBtn.onClick.AddListener(() => SwitchScreen(false));
    }

    protected void RefreshUIAndSelectFirst()
    {
        SetupScreens();
        if (screens.Count > 0)
        {
            currentCategory = new List<TCategory>(screens.Keys)[0];
            UpdateActiveScreen();
        }
        else
        {
            EventSystem.current.SetSelectedGameObject(leftScreenBtn.gameObject);
        }
    }


    public virtual void OnDisable()
    {
        // Destroy all dynamically created screens and their EventTriggers
        foreach (var kvp in screens)
        {
            foreach (Transform child in kvp.Value.transform)
            {
                EventTrigger trigger = child.GetComponent<EventTrigger>();
                if (trigger != null)
                {
                    trigger.triggers.Clear(); // Clear all listeners
                    Destroy(trigger); // Optionally remove the EventTrigger component
                }
            }

            Destroy(kvp.Value); // Destroy the screen itself
        }

        screens.Clear();

        // Remove button navigation listeners (if any other listeners are added outside EventTriggers)
        rightScreenBtn.onClick.RemoveAllListeners();
        leftScreenBtn.onClick.RemoveAllListeners();
    }

    protected void SetupScreens()
    {
        // Clear existing screens first
        foreach (var screen in screens.Values)
        {
            if (screen != null)
            {
                Destroy(screen);
            }
        }
        screens.Clear();

        foreach (var item in GetItems())
        {
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

            // Add event listener for selection
            EventTrigger trigger = itemButton.AddComponent<EventTrigger>();

            // Handle OnSelect
            EventTrigger.Entry selectEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.Select
            };
            selectEntry.callback.AddListener((data) => UpdatePreview(item));
            trigger.triggers.Add(selectEntry);

            // Optionally: Handle OnDeselect (to clear the preview)
            EventTrigger.Entry deselectEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.Deselect
            };
            deselectEntry.callback.AddListener((data) => UpdatePreview(default));
            trigger.triggers.Add(deselectEntry);
        }
    }


    protected void SwitchScreen(bool forward)
    {
        var categories = new List<TCategory>(screens.Keys);
        
        // If there are no categories, return early
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
        screenTitle.text = currentCategory.ToString();
        Debug.Log($"Updating Active Screen: {currentCategory}");
        // Get the first child GameObject or fallback to leftScreenBtn
        GameObject selectedObject = screens[currentCategory].transform.childCount > 0 
            ? screens[currentCategory].transform.GetChild(0).gameObject 
            : leftScreenBtn.gameObject;
        EventSystem.current.SetSelectedGameObject(selectedObject);
    }

    public virtual void UpdatePreview(TItem item = default)
    {
        currentSelectedItem = item;        

        // Update preview UI
        if (item != null)
        {
            previewName.text = GetPreviewName(item);
            previewImage.sprite = GetPreviewSprite(item);
            previewDesc.text = GetPreviewDescription(item);

            // Clear and populate resource cost
            UpdatePreviewSpecifics(item);
        }
        else
        {
            // Clear UI for null (no selection)
            previewName.text = string.Empty;
            previewImage.sprite = null;
            previewDesc.text = string.Empty;

            DestroyPreviewSpecifics();
        }
    }

}
