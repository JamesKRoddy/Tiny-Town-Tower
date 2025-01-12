// Updated ChestParent Script
using System.Collections.Generic;
using UnityEngine;

public class ChestParent : MonoBehaviour
{
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
        enabledChest.AssignChestLoot(roomDifficulty);
    }
}