using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class BuildingPreviewBtn : PreviewButtonBase<BuildingScriptableObj>
{
    protected override void OnButtonClicked()
    {
        bool canBuild = true;

        foreach (var requiredItem in data._resourceCost)
        {
            int playerCount = PlayerInventory.Instance.GetItemCount(requiredItem.resource);
            if (playerCount < requiredItem.count)
            {
                canBuild = false;
                break;
            }
        }

        if (canBuild)
        {
            PlayerUIManager.Instance.buildMenu.SetScreenActive(false, 0.1f, () => BuildingPlacer.Instance.StartPlacement(data));
        }
        else
        {
            PlayerUIManager.Instance.buildMenu.DisplayErrorMessage("Not enough resources to build this structure!");
        }
    }

    public void SetupButton(BuildingScriptableObj buildingObjRef)
    {
        base.SetupButton(buildingObjRef, buildingObjRef.sprite, buildingObjRef.objectName);
    }
}

