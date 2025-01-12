using UnityEngine;

[CreateAssetMenu(fileName = "ResourceScriptableObj", menuName = "Scriptable Objects/ResourceScriptableObj")]
public class ResourceScriptableObj : ScriptableObject
{
    [Header("Resource Information")]
    public string resourceName; // Name of the resource
    [TextArea(3, 5)] // Allows for multi-line text in the Inspector
    public string resourceDescription; // Description of the resource
    public ResourceCategory resourceCategory;
    public ResourceRarity resourceRarity;
    public Sprite resourceSprite; // Sprite for the UI representation
}

[System.Serializable]
public enum ResourceCategory
{
    GENERAL,
    FOOD,
    ELECTRICITY,
    DECORATION,
    WEAPONS,
    BASIC_BUILDING_MATERIAL,
    AMMO,
    QUEST_ITEM
}

// Define Loot Rarity and Difficulty Mapper
public enum ResourceRarity
{
    COMMON,
    RARE,
    EPIC,
    LEGENDARY
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