using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LootTableScriptableObj", menuName = "Scriptable Objects/Camp/LootTable")]
public class LootTableScriptableObj : ScriptableObject
{
    [SerializeField] private List<ResourcePickup> lootItems;

    public ResourcePickup GetLootByRarity(ResourceRarity rarity)
    {
        List<ResourcePickup> filteredLoot = lootItems.FindAll(loot => loot.GetResourceObj().resourceRarity == rarity);
        if (filteredLoot.Count == 0) return null;

        return filteredLoot[Random.Range(0, filteredLoot.Count)];
    }
}

