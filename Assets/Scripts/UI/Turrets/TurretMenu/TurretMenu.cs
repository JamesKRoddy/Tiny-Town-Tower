using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Managers;

public class TurretMenu : PreviewListMenuBase<TurretCategory, TurretScriptableObject>, IControllerInput
{
    [Header("Turret Menu Preview UI")]
    [SerializeField] GameObject previewResourceCostPrefab;
    [SerializeField] RectTransform previewResourceCostParent;

    [Header("Full list of Turret Scriptable Objects")]
    public TurretScriptableObject[] turretScriptableObjs;

    public override IEnumerable<TurretScriptableObject> GetItems()
    {
        return turretScriptableObjs; // Array of turrets from inspector
    }

    public override TurretCategory GetItemCategory(TurretScriptableObject item)
    {
        return TurretCategory.NONE; // Placeholder if no specific category exists
    }

    public override void SetupItemButton(TurretScriptableObject item, GameObject button)
    {
        var buttonComponent = button.GetComponent<TurretPreviewBtn>();
        buttonComponent.SetupButton(item);
    }

    public override string GetPreviewName(TurretScriptableObject item)
    {
        return item._name;
    }

    public override Sprite GetPreviewSprite(TurretScriptableObject item)
    {
        return item._sprite;
    }

    public override string GetPreviewDescription(TurretScriptableObject item)
    {
        return item._description;
    }

    public override IEnumerable<(string resourceName, int requiredCount, int playerCount)> GetPreviewResourceCosts(TurretScriptableObject item)
    {
        foreach (var resourceCost in item._resourceCost)
        {
            yield return (
                resourceCost.resource.objectName,
                resourceCost.count,
                PlayerInventory.Instance.GetItemCount(resourceCost.resource)
            );
        }
    }

    public override void UpdatePreviewSpecifics(TurretScriptableObject item)
    {
        foreach (Transform child in previewResourceCostParent)
        {
            Destroy(child.gameObject);
        }

        foreach (var (resourceName, requiredCount, playerCount) in GetPreviewResourceCosts(item))
        {
            GameObject resourceCostUI = Instantiate(previewResourceCostPrefab, previewResourceCostParent);
            resourceCostUI.GetComponentInChildren<TMP_Text>().text = $"{resourceName} : {requiredCount} ({playerCount})";
        }
    }

    public override void DestroyPreviewSpecifics()
    {
        foreach (Transform child in previewResourceCostParent)
        {
            Destroy(child.gameObject);
        }
    }

    public void StartTurretWave()
    {
        TurretManager.Instance.StartWave();
    }
}
