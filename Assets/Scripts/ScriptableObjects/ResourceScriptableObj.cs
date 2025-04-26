using UnityEngine;

[CreateAssetMenu(fileName = "ResourceScriptableObj", menuName = "Scriptable Objects/Camp/ResourceScriptableObj")]
public class ResourceScriptableObj : WorldItemBase
{
    [Header("Resource Information")]
    public ResourceCategory category;
    public ResourceRarity rarity;
    public GameObject prefab;
}

public static class DifficultyRarityMapper
{
    public static ResourceRarity GetResourceRarity(int roomDifficulty)
    {
        if (roomDifficulty < 20)
            return ResourceRarity.COMMON;
        if (roomDifficulty < 40)
            return ResourceRarity.RARE;
        if (roomDifficulty < 60)
            return ResourceRarity.EPIC;

        return ResourceRarity.LEGENDARY;
    }
}