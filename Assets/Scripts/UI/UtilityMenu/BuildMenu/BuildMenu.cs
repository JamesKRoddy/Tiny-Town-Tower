using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Managers;

public class BuildMenu : PreviewListMenuBase<BuildingCategory, BuildingScriptableObj>, IControllerInput
{
    [Header("Build Menu Preview UI")]
    [SerializeField] GameObject previewResourceCostPrefab;
    [SerializeField] RectTransform previewResourceCostParent;

    private BuildingPreviewBtn selectedButton;

    public override void SetPlayerControls(PlayerControlType controlType)
    {
        base.SetPlayerControls(controlType);
        switch (controlType)
        {
            case PlayerControlType.BUILDING_PLACEMENT:
                if (selectedButton != null)
                {
                    PlayerInput.Instance.OnBPressed += () => {
                        EnableBuildMenu();
                        PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.IN_MENU);
                    };
                }
                break;
            default:
                break;
        }
    }

    public override IEnumerable<BuildingScriptableObj> GetItems()
    {
        return CampManager.Instance.BuildManager.buildingScriptableObjs;
    }

    public override BuildingCategory GetItemCategory(BuildingScriptableObj item)
    {
        return item.buildingCategory;
    }

    public override void SetupItemButton(BuildingScriptableObj item, GameObject button)
    {
        var buttonComponent = button.GetComponent<BuildingPreviewBtn>();
        if (buttonComponent != null)
        {
            buttonComponent.SetupButton(item);
        }
    }

    public override string GetPreviewName(BuildingScriptableObj item)
    {
        return item.objectName;
    }

    public override Sprite GetPreviewSprite(BuildingScriptableObj item)
    {
        return item.sprite;
    }

    public override string GetPreviewDescription(BuildingScriptableObj item)
    {
        return item.description;
    }

    public override IEnumerable<(string resourceName, int requiredCount, int playerCount)> GetPreviewResourceCosts(BuildingScriptableObj item)
    {
        foreach (var resourceCost in item._resourceCost)
        {
            yield return (
                resourceCost.resourceScriptableObj.objectName,
                resourceCost.count,
                PlayerInventory.Instance.GetItemCount(resourceCost.resourceScriptableObj)
            );
        }
    }

    public override void UpdatePreviewSpecifics(BuildingScriptableObj item)
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

    protected override void OnItemClicked(BuildingScriptableObj item)
    {
        if (item != null)
        {
            var buttonComponent = EventSystem.current.currentSelectedGameObject?.GetComponent<BuildingPreviewBtn>();
            if (buttonComponent != null)
            {
                selectedButton = buttonComponent;
                PlayerUIManager.Instance.buildMenu.SetScreenActive(false, 0.1f, () => EnableBuildMenu());
            }
        }
    }

    public void EnableBuildMenu()
    {
        SetScreenActive(true, 0.1f);
    }

    public void StartBuildingPlacement(BuildingScriptableObj buildingToPlace)
    {
        SetScreenActive(false, 0.1f, () => Managers.PlacementManager.Instance.StartBuildingPlacement(buildingToPlace));
    }
}

