using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Managers;

public class BuildMenu : PreviewListMenuBase<PlaceableObjectCategory, PlaceableObjectParent>, IControllerInput
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

    public override IEnumerable<PlaceableObjectParent> GetItems()
    {
        // Return both buildings and turrets
        if (CampManager.Instance?.BuildManager != null)
        {
            // Add buildings
            foreach (var building in CampManager.Instance.BuildManager.buildingScriptableObjs)
            {
                yield return building;
            }
            
            // Add turrets
            foreach (var turret in CampManager.Instance.BuildManager.turretScriptableObjs)
            {
                yield return turret;
            }
        }
    }

    public override PlaceableObjectCategory GetItemCategory(PlaceableObjectParent item)
    {        
        return item.placeableObjectCategory;
    }

    public override void SetupItemButton(PlaceableObjectParent item, GameObject button)
    {
        var buttonComponent = button.GetComponent<BuildingPreviewBtn>();
        if (buttonComponent != null)
        {
            buttonComponent.SetupButton(item);
        }
    }

    public override string GetPreviewName(PlaceableObjectParent item)
    {
        return item.objectName;
    }

    public override Sprite GetPreviewSprite(PlaceableObjectParent item)
    {
        return item.sprite;
    }

    public override string GetPreviewDescription(PlaceableObjectParent item)
    {
        return item.description;
    }

    public override IEnumerable<(string resourceName, int requiredCount, int playerCount)> GetPreviewResourceCosts(PlaceableObjectParent item)
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

    public override void UpdatePreviewSpecifics(PlaceableObjectParent item)
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

    protected override void OnItemClicked(PlaceableObjectParent item)
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

    public void StartBuildingPlacement(PlaceableObjectParent placeableObject)
    {
        SetScreenActive(false, 0.1f, () => {
            Managers.PlacementManager.Instance.StartPlacement(placeableObject);
        });
    }
}

