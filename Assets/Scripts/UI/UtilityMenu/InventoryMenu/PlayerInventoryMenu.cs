using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventoryMenu : PreviewListMenuBase<ResourceCategory, ResourceScriptableObj>, IControllerInput
{
    [Header("Inventory Preview UI")]
    [SerializeField] private GameObject previewResourceCostPrefab;
    [SerializeField] private RectTransform previewResourceCostParent;

    public override IEnumerable<ResourceScriptableObj> GetItems()
    {
        var inventory = PlayerInventory.Instance.GetFullInventory();
        foreach (var item in inventory)
        {
            if (item.resourceScriptableObj != null && item.count > 0)
            {
                yield return item.resourceScriptableObj;
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
