using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class InventoryPopupItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private TMP_Text inventoryTypeText;
    [SerializeField] private Image backgroundImage;
    
    [Header("Visual Settings")]
    [SerializeField] private Color playerInventoryColor = new Color(0.2f, 0.8f, 0.2f, 0.8f);
    [SerializeField] private Color npcInventoryColor = new Color(0.8f, 0.6f, 0.2f, 0.8f);
    [SerializeField] private Color playerInventoryTextColor = Color.white;
    [SerializeField] private Color npcInventoryTextColor = Color.white;

    private ScriptableObject item;
    private int count;
    private bool isPlayerInventory;
    private float duration;
    private AddedToInventoryPopup popupManager;
    private Coroutine timerCoroutine;

    public ScriptableObject Item => item;
    public bool IsPlayerInventory => isPlayerInventory;

    public int GetCurrentCount() => count;

    public void Initialize(ScriptableObject item, int count, bool isPlayerInventory, float duration, AddedToInventoryPopup popupManager)
    {
        this.item = item;
        this.count = count;
        this.isPlayerInventory = isPlayerInventory;
        this.duration = duration;
        this.popupManager = popupManager;

        UpdateVisuals();
        StartTimer();
    }

    private void UpdateVisuals()
    {
        if (item == null) return;

        // Set item icon
        if (itemIcon != null)
        {
            Sprite sprite = GetItemSprite();
            if (sprite != null)
            {
                itemIcon.sprite = sprite;
                itemIcon.enabled = true;
            }
            else
            {
                itemIcon.enabled = false;
            }
        }

        // Set item name
        if (itemNameText != null)
        {
            itemNameText.text = GetItemName();
        }

        // Set count (only show if count > 1)
        if (countText != null)
        {
            if (count > 1)
            {
                countText.text = $"x{count}";
                countText.gameObject.SetActive(true);
            }
            else
            {
                countText.gameObject.SetActive(false);
            }
        }

        // Set inventory type indicator
        if (inventoryTypeText != null)
        {
            inventoryTypeText.text = isPlayerInventory ? "Player" : "NPC";
            inventoryTypeText.color = isPlayerInventory ? playerInventoryTextColor : npcInventoryTextColor;
        }

        // Set background color
        if (backgroundImage != null)
        {
            backgroundImage.color = isPlayerInventory ? playerInventoryColor : npcInventoryColor;
        }
    }

    private Sprite GetItemSprite()
    {
        if (item is ResourceScriptableObj resource)
        {
            return resource.sprite;
        }
        else if (item is WeaponScriptableObj weapon)
        {
            return weapon.sprite;
        }
        else if (item is GeneticMutationObj mutation)
        {
            return mutation.sprite;
        }
        return null;
    }

    private string GetItemName()
    {
        if (item is ResourceScriptableObj resource)
        {
            return resource.objectName;
        }
        else if (item is WeaponScriptableObj weapon)
        {
            return weapon.objectName;
        }
        else if (item is GeneticMutationObj mutation)
        {
            return mutation.objectName;
        }
        return "Unknown Item";
    }

    public void UpdateCount(int newCount)
    {
        count = newCount;
        UpdateVisuals();
    }

    public void RestartTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
        StartTimer();
    }

    private void StartTimer()
    {
        timerCoroutine = StartCoroutine(TimerCoroutine());
    }

    private IEnumerator TimerCoroutine()
    {
        yield return new WaitForSeconds(duration);
        
        if (popupManager != null)
        {
            popupManager.RemovePopup(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
    }
} 