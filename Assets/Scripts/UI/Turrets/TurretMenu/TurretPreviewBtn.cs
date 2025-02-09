using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TurretPreviewBtn : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] Image turretImage;
    [SerializeField] TMP_Text turretNameText;

    GameObject turretPrefab;
    TurretScriptableObject turretObj;

    void OnDestroy()
    {
        button.onClick.RemoveAllListeners();
    }

    void InstantiateTurretPlacement()
    {
        // Initialize canPlace to true and set it to false if a requirement isn't met.
        bool canPlace = true;

        // Loop through each required resource and check the player's inventory.
        foreach (var requiredItem in turretObj.turretResourceCost)
        {
            // Check how many of this resource the player currently has.
            int playerCount = PlayerInventory.Instance.GetItemCount(requiredItem.resource);

            // If the player doesn't have enough of this resource, they can't place the turret.
            if (playerCount < requiredItem.count)
            {
                canPlace = false;
                break;
            }
        }

        // If canPlace is true, proceed with turret placement. Otherwise, show an error or refuse placement.
        if (canPlace)
        {
            TurretMenu.Instance.SetScreenActive(false, 0.1f, () => TurretPlacer.Instance.StartPlacement(turretObj));
        }
        else
        {
            TurretMenu.Instance.DisplayErrorMessage("Not enough resources to place this turret!");
            // Optionally, display a UI message to the player.
        }
    }


    public void SetupButton(TurretScriptableObject turretObjRef)
    {
        turretObj = turretObjRef;

        turretPrefab = turretObjRef.turretPrefab;

        button.onClick.AddListener(InstantiateTurretPlacement);

        if (turretObjRef.turretSprite != null)
            turretImage.sprite = turretObjRef.turretSprite;

        turretNameText.text = turretObjRef.turretName;
    }
}
