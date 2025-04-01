using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventoryMenu : PreviewListMenuBase<ResourceCategory, ResourceScriptableObj>, IControllerInput
{
    [Header("Inventory Preview UI")]
    [SerializeField] private GameObject previewResourceCostPrefab;
    [SerializeField] private RectTransform previewResourceCostParent;

    public override void Setup()
    {
        base.Setup();
        PlayerInput.Instance.OnUpdatePlayerControls += SetPlayerControlType;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.IN_MENU);
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PlayerInput.Instance.OnUpdatePlayerControls -= SetPlayerControlType;
    }

    public void SetPlayerControlType(PlayerControlType controlType)
    {
        if (PlayerUIManager.Instance.currentMenu != this)
            return;

        switch (controlType)
        {
            case PlayerControlType.IN_MENU:
                PlayerInput.Instance.OnRBPressed += rightScreenBtn.onClick.Invoke;
                PlayerInput.Instance.OnLBPressed += leftScreenBtn.onClick.Invoke;
                PlayerInput.Instance.OnBPressed += () => PlayerUIManager.Instance.utilityMenu.EnableUtilityMenu();
                break;
            default:
                break;
        }
    }

    public override void SetScreenActive(bool active, float delay = 0.0f, Action onDone = null)
    {
        PlayerUIManager.Instance.SetScreenActive(this, active, delay);
    }

    public override IEnumerable<ResourceScriptableObj> GetItems()
    {
        var inventory = PlayerInventory.Instance.GetFullInventory();
        foreach (var item in inventory)
        {
            if (item.resource != null && item.count > 0)
            {
                yield return item.resource;
            }
        }
    }

    public override ResourceCategory GetItemCategory(ResourceScriptableObj item)
    {
        return item.category;
    }

    public override void SetupItemButton(ResourceScriptableObj item, GameObject button)
    {
        var buttonComponent = button.GetComponent<InventoryPreviewBtn>();
        if (buttonComponent != null)
        {
            int quantity = PlayerInventory.Instance.GetItemCount(item);
            buttonComponent.SetupButton(item, item.sprite, quantity.ToString());
        }
    }

    public override string GetPreviewName(ResourceScriptableObj item)
    {
        return item.objectName;
    }

    public override Sprite GetPreviewSprite(ResourceScriptableObj item)
    {
        return item.sprite;
    }

    public override string GetPreviewDescription(ResourceScriptableObj item)
    {
        return item.description;
    }

    public override IEnumerable<(string resourceName, int requiredCount, int playerCount)> GetPreviewResourceCosts(ResourceScriptableObj item)
    {
        yield break;
    }

    public override void UpdatePreviewSpecifics(ResourceScriptableObj item)
    {
        if (previewResourceCostParent == null) return;

        foreach (Transform child in previewResourceCostParent)
        {
            Destroy(child.gameObject);
        }

        if (item != null)
        {
            int quantity = PlayerInventory.Instance.GetItemCount(item);
            GameObject resourceCostUI = Instantiate(previewResourceCostPrefab, previewResourceCostParent);
            resourceCostUI.GetComponentInChildren<TMPro.TMP_Text>().text = $"Quantity: {quantity}";
        }
    }

    public override void DestroyPreviewSpecifics()
    {
        if (previewResourceCostParent == null) return;

        foreach (Transform child in previewResourceCostParent)
        {
            Destroy(child.gameObject);
        }
    }

    protected override void OnItemSelected(ResourceScriptableObj item)
    {
        base.OnItemSelected(item);
        if (item != null)
        {
            UpdatePreviewSpecifics(item);
        }
    }

    protected override void OnItemDeselected()
    {
        base.OnItemDeselected();
        DestroyPreviewSpecifics();
    }
}
