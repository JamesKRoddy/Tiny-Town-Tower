// Updated ChestParent Script
using System.Collections.Generic;
using UnityEngine;
using Managers;

public class ChestParent : MonoBehaviour
{
    [Header("Chest Loot Settings")]
    [SerializeField] private List<Chest> chests;
    private Chest enabledChest;

    public void SetupChest(int roomDifficulty)
    {
        if (chests == null || chests.Count == 0)
        {
            Debug.LogError("No chests assigned to ChestParent!");
            return;
        }

        // Pick a random chest to enable
        enabledChest = chests[Random.Range(0, chests.Count)];
        enabledChest.gameObject.SetActive(true);
        enabledChest.transform.position += Vector3.up * 0.5f;
        enabledChest.AssignChestLoot(GameManager.Instance.ResourceManager.GetBuildingLootTable(RogueLiteManager.Instance.BuildingManager.CurrentBuilding.buildingType, roomDifficulty));
    }
}