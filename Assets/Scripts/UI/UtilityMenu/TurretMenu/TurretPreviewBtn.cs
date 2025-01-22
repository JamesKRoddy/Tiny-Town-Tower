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

        // Example: Check placement requirements, if any (adjust logic as necessary).
        // Placeholder logic: Add specific turret placement constraints here if needed.

        // If canPlace is true, proceed with turret placement. Otherwise, show an error or refuse placement.
        if (canPlace)
        {
            // Optionally, deduct placement cost or perform other actions here.

            TurretPlacer.Instance.StartPlacement(turretObj);
            TurretMenu.Instance.SetScreenActive(false, 0.05f);
        }
        else
        {
            TurretMenu.Instance.DisplayErrorMessage("Cannot place this turret at the selected location!");
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
