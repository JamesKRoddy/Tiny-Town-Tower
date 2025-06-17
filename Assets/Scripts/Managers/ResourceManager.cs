using System;
using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    public class ResourceManager : MonoBehaviour
    {
        [SerializeField] private List<LootTableForCharacterType> characterLootTables;

        [SerializeField] private List<LootTableForBuildingType> buildingLootTables;

        internal ResourceItemCount GetBuildingLootTable(BuildingType buildingType, int roomDifficulty)
        {
            LootTableForBuildingType lootTableForBuildingType = buildingLootTables.Find(lootTable => lootTable.buildingType == buildingType);
            LootTableScriptableObj lootTableScriptableObj = lootTableForBuildingType.lootTable;
            ResourceRarity rarity = DifficultyRarityMapper.GetResourceRarity(roomDifficulty);
            return lootTableScriptableObj.GetLootByRarity(rarity);
        }
    }

    [System.Serializable]
    internal class LootTableForBuildingType
    {
        public BuildingType buildingType;
        public LootTableScriptableObj lootTable;
    }

    [System.Serializable]
    internal class LootTableForCharacterType
    {
        public CharacterType characterType;
        public LootTableScriptableObj lootTable;
    }
}
