using UnityEngine;

[CreateAssetMenu(fileName = "SeedScriptableObject", menuName = "Scriptable Objects/Camp/SeedScriptableObject")]
public class SeedScriptableObject : ResourceScriptableObj
{
    [Header("Seed Information")]
    public ResourceScriptableObj cropToGrow;
    public int growthRate;
    public int yieldAmount;
    
}
