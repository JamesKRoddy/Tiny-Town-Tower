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
            LootTableForBuildingType lootTableForBuildingType = buildingLootTables.Find(lootTable => lootTable.buildingType == buildingType);
            LootTableScriptableObj lootTableScriptableObj = lootTableForBuildingType.lootTable;
            ItemRarity rarity = GameManager.Instance.DifficultyManager.GetResourceRarity(roomDifficulty);
            return lootTableScriptableObj.GetLootByRarity(rarity);
        }

        internal void SpawnCharacterLoot(CharacterType characterType, int roomDifficulty, Vector3 position)
        {
            LootTableForCharacterType lootTableForCharacterType = characterLootTables.Find(lootTable => lootTable.characterType == characterType);
            LootTableScriptableObj lootTableScriptableObj = lootTableForCharacterType.lootTable;
            ItemRarity rarity = GameManager.Instance.DifficultyManager.GetResourceRarity(roomDifficulty);
            ResourceItemCount resourceItemCount = lootTableScriptableObj.GetLootByRarity(rarity);
            SpawnResourcePickup(resourceItemCount, position);
        }

        private void SpawnResourcePickup(ResourceItemCount resourceItemCount, Vector3 position)
        {
            Debug.Log("SpawnResourcePickup: " + resourceItemCount.GetResourceObj().objectName);
            GameObject resourcePickup = Instantiate(resourcePickupPrefab, position, Quaternion.identity);
            Resource resourcePickupComponent = resourcePickup.GetComponent<Resource>();
            resourcePickupComponent.Initialize(resourceItemCount.GetResourceObj(), resourceItemCount.count);
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
