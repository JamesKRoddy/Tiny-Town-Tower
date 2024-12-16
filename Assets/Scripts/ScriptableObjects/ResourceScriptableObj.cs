using UnityEngine;

[CreateAssetMenu(fileName = "ResourceScriptableObj", menuName = "Scriptable Objects/ResourceScriptableObj")]
public class ResourceScriptableObj : ScriptableObject
{
    [Header("Resource Information")]
    public string resourceName; // Name of the resource
    [TextArea(3, 5)] // Allows for multi-line text in the Inspector
    public string resourceDescription; // Description of the resource
    public ResourceCategory resourceCategory;
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