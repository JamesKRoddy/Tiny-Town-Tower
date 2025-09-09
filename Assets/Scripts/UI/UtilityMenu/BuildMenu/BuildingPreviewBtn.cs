using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class BuildingPreviewBtn : PreviewButtonBase<PlaceableObjectParent>
{
    protected override void OnDefaultButtonClicked()
    {
        bool canBuild = true;

        foreach (var requiredItem in data._resourceCost)
        {
            int playerCount = PlayerInventory.Instance.GetItemCount(requiredItem.resourceScriptableObj);
            if (playerCount < requiredItem.count)
            {
                canBuild = false;
                break;
            }
        }

        if (canBuild)
        {
            PlayerUIManager.Instance.buildMenu.StartBuildingPlacement(data);
        }
        else
        {
            PlayerUIManager.Instance.buildMenu.DisplayErrorMessage($"Not enough resources to build this structure!");
        }
    }

    public void SetupButton(PlaceableObjectParent placeableObject)
    {
        base.SetupButton(placeableObject, placeableObject.sprite, placeableObject.objectName);
    }

    public void OnButtonClick()
    {
        PlayerUIManager.Instance.buildMenu.StartBuildingPlacement(data);
    }
}

