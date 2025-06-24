using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TurretPreviewBtn : PreviewButtonBase<TurretScriptableObject>
{
    protected override void OnButtonClicked()
    {
        bool canPlace = true;

        foreach (var requiredItem in data._resourceCost)
        {
            int playerCount = PlayerInventory.Instance.GetItemCount(requiredItem.resourceScriptableObj);
            if (playerCount < requiredItem.count)
            {
                canPlace = false;
                break;
            }
        }

        if (canPlace)
        {
            PlayerUIManager.Instance.turretMenu.SetScreenActive(false, 0.1f, () => Managers.PlacementManager.Instance.StartTurretPlacement(data));
        }
        else
        {
            PlayerUIManager.Instance.turretMenu.DisplayErrorMessage("Not enough resources to place this turret!");
        }
    }

    public void SetupButton(TurretScriptableObject turretObjRef)
    {
        base.SetupButton(turretObjRef, turretObjRef.sprite, turretObjRef.objectName);
    }

    public void OnButtonClick()
    {
        PlayerUIManager.Instance.turretMenu.SetScreenActive(false, 0.1f, () => Managers.PlacementManager.Instance.StartTurretPlacement(data));
    }
}

