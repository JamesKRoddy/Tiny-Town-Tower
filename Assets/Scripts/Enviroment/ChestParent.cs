// Updated ChestParent Script
using System.Collections.Generic;
using UnityEngine;

public class ChestParent : MonoBehaviour
{
    [Header("Chest Loot Settings")]
    [SerializeField] private List<Chest> chests;
    [SerializeField] private LootTableScriptableObj lootTableScriptableObj; // Reference to the LootTableScriptableObj
    private Chest enabledChest;

    public void SetupChest(int roomDifficulty)
    {
        if (chests == null || chests.Count == 0)
        {
            Debug.LogError("No chests assigned to ChestParent!");
            return;
        }

        // Pick a random chest to enable
        enabledChest = chests[Random.Range(0, chests.Count)]; //TODO base this on GetCurrentRoomDifficulty
        enabledChest.gameObject.SetActive(true);
        enabledChest.transform.position += Vector3.up * 0.5f;
        enabledChest.AssignChestLoot(roomDifficulty, lootTableScriptableObj);
    }
}