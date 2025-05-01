using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LootTableScriptableObj", menuName = "Scriptable Objects/Camp/LootTable")]
public class LootTableScriptableObj : ScriptableObject
{
    [SerializeField] private List<ResourceItemCount> lootItems;

    public ResourceItemCount GetLootByRarity(ResourceRarity rarity)
    {
        List<ResourceItemCount> filteredLoot = lootItems.FindAll(loot => loot.GetResourceObj().rarity == rarity);
        if (filteredLoot.Count == 0) return null;

        return filteredLoot[Random.Range(0, filteredLoot.Count)];
    }
}

