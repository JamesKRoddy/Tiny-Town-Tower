using UnityEngine;

public class CraftableScriptableObj : WorldItemBase
{
    public ResourceItemCount[] requiredResources;
    public ResearchScriptableObj[] requiredResearch; // Research that must be completed before this can be unlocked
    [Min(5f)]
    public float craftTime;
    public ResourceItemCount[] outputResources;
    public int outputAmount = 1;
}
