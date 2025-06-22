using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;

public class AddedToInventoryPopup : MonoBehaviour
{
    [Header("Popup Settings")]
    [SerializeField] private GameObject popupItemPrefab;
    [SerializeField] private Transform popupContainer;
    [SerializeField] private GameObject popupStartPosition; // GameObject to define where popups start appearing
    [SerializeField] private float popupDuration = 3f;
    [SerializeField] private float popupSpacing = 10f;
    [SerializeField] private int maxVisiblePopups = 5;
    
    [Header("Animation Settings")]
    [SerializeField] private float slideInDuration = 0.3f;
    [SerializeField] private float slideOutDuration = 0.3f;
    [SerializeField] private AnimationCurve slideInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve slideOutCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private List<InventoryPopupItem> activePopups = new List<InventoryPopupItem>();
    private Queue<InventoryPopupItem> popupQueue = new Queue<InventoryPopupItem>();

    private void Start()
    {
        // Ensure the container is set up properly
        if (popupContainer == null)
        {
            popupContainer = transform;
        }
    }

    /// <summary>
    /// Shows a popup for an item added to inventory
    /// </summary>
    /// <param name="resourceItem">The resource item that was added</param>
    /// <param name="count">The count of items added</param>
    /// <param name="isPlayerInventory">Whether this was added to player inventory (true) or NPC inventory (false)</param>
    public void ShowInventoryPopup(ResourceScriptableObj resourceItem, int count, bool isPlayerInventory)
    {
        if (resourceItem == null) return;

        // Check if we already have a popup for this item
        InventoryPopupItem existingPopup = FindExistingPopup(resourceItem, isPlayerInventory);
        
        if (existingPopup != null)
        {
            // Update existing popup with new count
            existingPopup.UpdateCount(existingPopup.GetCurrentCount() + count);
            existingPopup.RestartTimer();
        }
        else
        {
            // Create new popup
            CreateNewPopup(resourceItem, count, isPlayerInventory);
        }
    }

    /// <summary>
    /// Shows a popup for a weapon added to inventory
    /// </summary>
    /// <param name="weapon">The weapon that was added</param>
    /// <param name="isPlayerInventory">Whether this was added to player inventory (true) or NPC inventory (false)</param>
    public void ShowWeaponPopup(WeaponScriptableObj weapon, bool isPlayerInventory)
    {
        if (weapon == null) return;

        // Check if we already have a popup for this weapon
        InventoryPopupItem existingPopup = FindExistingPopup(weapon, isPlayerInventory);
        
        if (existingPopup != null)
        {
            existingPopup.RestartTimer();
        }
        else
        {
            // Create new popup for weapon
            CreateNewPopup(weapon, 1, isPlayerInventory);
        }
    }

    /// <summary>
    /// Shows a popup for a genetic mutation added to inventory
    /// </summary>
    /// <param name="mutation">The genetic mutation that was added</param>
    /// <param name="isPlayerInventory">Whether this was added to player inventory (true) or NPC inventory (false)</param>
    public void ShowMutationPopup(GeneticMutationObj mutation, bool isPlayerInventory)
    {
        if (mutation == null) return;

        // Check if we already have a popup for this mutation
        InventoryPopupItem existingPopup = FindExistingPopup(mutation, isPlayerInventory);
        
        if (existingPopup != null)
        {
            existingPopup.RestartTimer();
        }
        else
        {
            // Create new popup for mutation
            CreateNewPopup(mutation, 1, isPlayerInventory);
        }
    }

    private InventoryPopupItem FindExistingPopup(ScriptableObject item, bool isPlayerInventory)
    {
        foreach (var popup in activePopups)
        {
            if (popup.Item == item && popup.IsPlayerInventory == isPlayerInventory)
            {
                return popup;
            }
        }
        return null;
    }

    private void CreateNewPopup(ScriptableObject item, int count, bool isPlayerInventory)
    {
        if (popupItemPrefab == null)
        {
            Debug.LogError("Popup item prefab is not assigned!");
            return;
        }

        GameObject popupObj = Instantiate(popupItemPrefab, popupContainer);
        InventoryPopupItem popupItem = popupObj.GetComponent<InventoryPopupItem>();
        
        if (popupItem == null)
        {
            popupItem = popupObj.AddComponent<InventoryPopupItem>();
        }

        popupItem.Initialize(item, count, isPlayerInventory, popupDuration, this);
        
        // Calculate the target position BEFORE adding to the list
        int targetIndex = activePopups.Count;
        Vector2 targetPosition = CalculatePopupPosition(targetIndex);
        
        // Add to the list
        activePopups.Add(popupItem);

        // Position the popup starting from the start position
        StartCoroutine(AnimatePopupIn(popupItem, targetPosition));
        
        // Manage popup queue if we have too many
        if (activePopups.Count > maxVisiblePopups)
        {
            RemoveOldestPopup();
        }
    }

    private IEnumerator AnimatePopupIn(InventoryPopupItem popup, Vector2 targetPos)
    {
        RectTransform rectTransform = popup.GetComponent<RectTransform>();
        
        // Get the start position in screen space
        Vector2 startPos = GetStartPositionInScreenSpace();
        
        // Set the popup to start position immediately
        SetPopupPosition(rectTransform, startPos);
        
        float elapsed = 0f;
        
        while (elapsed < slideInDuration)
        {
            elapsed += Time.deltaTime;
            float t = slideInCurve.Evaluate(elapsed / slideInDuration);
            Vector2 currentPos = Vector2.Lerp(startPos, targetPos, t);
            SetPopupPosition(rectTransform, currentPos);
            yield return null;
        }
        
        SetPopupPosition(rectTransform, targetPos);
    }

    private Vector2 CalculatePopupPosition(int index)
    {
        // Get the base position from the start position GameObject
        Vector2 basePosition = GetStartPositionInScreenSpace();
        
        // Calculate the offset for stacking
        float popupHeight = popupItemPrefab.GetComponent<RectTransform>().rect.height;
        float yOffset = index * (popupHeight + popupSpacing);
        
        return new Vector2(basePosition.x, basePosition.y + yOffset);
    }

    private Vector2 GetStartPositionInScreenSpace()
    {
        if (popupStartPosition != null)
        {
            RectTransform startRectTransform = popupStartPosition.GetComponent<RectTransform>();
            if (startRectTransform != null)
            {
                // Just use the anchored position directly
                return startRectTransform.anchoredPosition;
            }
        }
        
        // Fallback to bottom-right corner if no start position is set
        return new Vector2(-220, 80); // 220 pixels from right, 80 pixels from bottom
    }

    private void SetPopupPosition(RectTransform rectTransform, Vector2 position)
    {
        // Just set the anchored position directly
        rectTransform.anchoredPosition = position;
    }

    public void RemovePopup(InventoryPopupItem popup)
    {
        if (activePopups.Contains(popup))
        {
            activePopups.Remove(popup);
            StartCoroutine(AnimatePopupOut(popup));
        }
    }

    private IEnumerator AnimatePopupOut(InventoryPopupItem popup)
    {
        RectTransform rectTransform = popup.GetComponent<RectTransform>();
        Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 targetPos = startPos + new Vector2(200f, 0f); // Slide out to the right
        
        float elapsed = 0f;
        
        while (elapsed < slideOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = slideOutCurve.Evaluate(elapsed / slideOutDuration);
            Vector2 currentPos = Vector2.Lerp(startPos, targetPos, t);
            rectTransform.anchoredPosition = currentPos;
            yield return null;
        }
        
        Destroy(popup.gameObject);
        RepositionRemainingPopups();
    }

    private void RemoveOldestPopup()
    {
        if (activePopups.Count > 0)
        {
            InventoryPopupItem oldestPopup = activePopups[0];
            RemovePopup(oldestPopup);
        }
    }

    private void RepositionRemainingPopups()
    {
        for (int i = 0; i < activePopups.Count; i++)
        {
            RectTransform rectTransform = activePopups[i].GetComponent<RectTransform>();
            Vector2 newPos = CalculatePopupPosition(i);
            SetPopupPosition(rectTransform, newPos);
        }
    }
}
