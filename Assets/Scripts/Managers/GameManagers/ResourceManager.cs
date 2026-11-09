using System;
using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    public class ResourceManager : MonoBehaviour
    {

        [SerializeField] private GameObject resourcePickupPrefab;
        [SerializeField] private List<LootTableForCharacterType> characterLootTables;

        [SerializeField] private List<LootTableForBuildingType> buildingLootTables;

        internal ResourceItemCount GetBuildingLootTable(RogueLikeBuildingType buildingType, int roomDifficulty) //
        {
            if (buildingLootTables == null || buildingLootTables.Count == 0)
            {
                Debug.LogWarning($"[ResourceManager] No building loot tables configured. Cannot get loot table for {buildingType}.");
                return null;
            }
            
            LootTableForBuildingType lootTableForBuildingType = buildingLootTables.Find(lootTable => lootTable.buildingType == buildingType);
            if (lootTableForBuildingType == null)
            {
                Debug.LogWarning($"[ResourceManager] No loot table found for building type: {buildingType}.");
                return null;
            }
            
            if (lootTableForBuildingType.lootTable == null)
            {
                Debug.LogWarning($"[ResourceManager] Loot table for {buildingType} is null.");
                return null;
            }
            
            LootTableScriptableObj lootTableScriptableObj = lootTableForBuildingType.lootTable;
            ItemRarity rarity = GameManager.Instance.DifficultyManager.GetResourceRarity(roomDifficulty);
            return lootTableScriptableObj.GetLootByRarity(rarity);
        }

        internal void SpawnCharacterLoot(CharacterType characterType, int roomDifficulty, Vector3 position)
        {
            if (characterLootTables == null || characterLootTables.Count == 0)
            {
                Debug.LogWarning($"[ResourceManager] No character loot tables configured. Cannot spawn loot for {characterType}.");
                return;
            }
            
            LootTableForCharacterType lootTableForCharacterType = characterLootTables.Find(lootTable => lootTable.characterType == characterType);
            if (lootTableForCharacterType == null)
            {
                Debug.LogWarning($"[ResourceManager] No loot table found for character type: {characterType}. Cannot spawn loot.");
                return;
            }
            
            if (lootTableForCharacterType.lootTable == null)
            {
                Debug.LogWarning($"[ResourceManager] Loot table for {characterType} is null. Cannot spawn loot.");
                return;
            }
            
            LootTableScriptableObj lootTableScriptableObj = lootTableForCharacterType.lootTable;
            ItemRarity rarity = GameManager.Instance.DifficultyManager.GetResourceRarity(roomDifficulty);
            ResourceItemCount resourceItemCount = lootTableScriptableObj.GetLootByRarity(rarity);
            SpawnResourcePickup(resourceItemCount, position);
        }

        private void SpawnResourcePickup(ResourceItemCount resourceItemCount, Vector3 position)
        {
            if (resourceItemCount == null)
            {
                Debug.LogWarning("[ResourceManager] Cannot spawn resource pickup - resourceItemCount is null.");
                return;
            }
            
            if (resourcePickupPrefab == null)
            {
                Debug.LogWarning("[ResourceManager] Cannot spawn resource pickup - resourcePickupPrefab is null.");
                return;
            }
            
            Debug.Log("SpawnResourcePickup: " + resourceItemCount.GetResourceObj().objectName);
            GameObject resourcePickup = Instantiate(resourcePickupPrefab, position, Quaternion.identity);
            Resource resourcePickupComponent = resourcePickup.GetComponent<Resource>();
            if (resourcePickupComponent != null)
            {
                resourcePickupComponent.Initialize(resourceItemCount.GetResourceObj(), resourceItemCount.count);
            }
            else
            {
                Debug.LogWarning("[ResourceManager] ResourcePickup prefab does not have a Resource component.");
            }
        }
    }

    [System.Serializable]
    internal class LootTableForBuildingType
    {
        public RogueLikeBuildingType buildingType;
        public LootTableScriptableObj lootTable;
    }

    [System.Serializable]
    internal class LootTableForCharacterType
    {
        public CharacterType characterType;
        public LootTableScriptableObj lootTable;
    }
}
