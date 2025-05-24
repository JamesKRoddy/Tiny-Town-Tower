using UnityEngine;

[CreateAssetMenu(fileName = "SeedScriptableObject", menuName = "Scriptable Objects/Camp/SeedScriptableObject")]
public class SeedScriptableObject : ResourceScriptableObj
{
    [Header("Seed Information")]
    public ResourceScriptableObj cropToGrow;
    public int growthRate;
    public int yieldAmount;
    
    [Header("Growth Visuals")]
    public GameObject[] growthStagePrefabs; // Prefabs representing different growth stages
    public GameObject deadCropPrefab; // Prefab to show when the crop dies
}
