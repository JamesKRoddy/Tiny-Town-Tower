using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TurretMenu : PreviewListMenuBase<TurretCategory, TurretScriptableObject>, IControllerInput
{
    [Header("Turret Menu Preview UI")]
    [SerializeField] GameObject previewResourceCostPrefab;
    [SerializeField] RectTransform previewResourceCostParent;

    [Header("Full list of Turret Scriptable Objects")]
    public TurretScriptableObject[] turretScriptableObjs;

    public override void Setup()
    {
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
    }

    public void OnDestroy()
    {
        if (PlayerInput.Instance != null)
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
        PlayerUIManager.Instance.SetScreenActive(this, active, delay, onDone);
    }

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
                resourceCost.resource.resourceName,
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
}
