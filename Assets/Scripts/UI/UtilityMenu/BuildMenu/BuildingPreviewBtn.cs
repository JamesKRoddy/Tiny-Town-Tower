using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class BuildingPreviewBtn : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] Image buildingImage;
    [SerializeField] TMP_Text buildingNameText;

    GameObject buildingPrefab;
    BuildingScriptableObj buildingObj;

    void OnDestroy()
    {
        button.onClick.RemoveAllListeners();
    }

    void InstanciateBuildingConstruction()
    {
        // Initialize canBuild to true and set it to false if a requirement isn't met.
        bool canBuild = true;

        // Loop through each required resource and check the player's inventory.
        foreach (var requiredItem in buildingObj.buildingResourceCost)
        {
            // Check how many of this resource the player currently has.
            int playerCount = PlayerInventory.Instance.GetItemCount(requiredItem.resource);

            // If the player doesn't have enough of this resource, they can't build.
            if (playerCount < requiredItem.count)
            {
                canBuild = false;
                break;
            }
        }

        // If canBuild is true, proceed with construction placement. Otherwise, show an error or refuse placement.
        if (canBuild)
        {
            // Optionally, deduct the required resources from the player's inventory here.
            foreach (var requiredItem in buildingObj.buildingResourceCost)
            {
                PlayerInventory.Instance.RemoveItem(requiredItem.resource, requiredItem.count);
            }

            BuildingPlacer.Instance.StartPlacement(buildingObj);
            BuildMenu.Instance.SetScreenActive(false, 0.05f);
        }
        else
        {
            Debug.Log("Not enough resources to build this structure!");
            // Optionally, display a UI message to the player.
        }
    }

    public void SetupButton(BuildingScriptableObj buildingObjRef)
    {
        buildingObj = buildingObjRef;

        buildingPrefab = buildingObjRef.buildingPrefab;

        button.onClick.AddListener(InstanciateBuildingConstruction);

        if(buildingObjRef.buildingSprite != null)
            buildingImage.sprite = buildingObjRef.buildingSprite;

        buildingNameText.text = buildingObjRef.buildingName;

    }
}
