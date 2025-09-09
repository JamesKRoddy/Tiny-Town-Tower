using UnityEngine;

public class CraftableScriptableObj : WorldItemBase
{
    public ResourceItemCount[] requiredResources;
    public ResearchScriptableObj[] requiredResearch; // Research that must be completed before this can be unlocked
    public float craftTimeInGameHours = 1f; // Time to craft in game hours (default: 1 game hour)
    public ResourceItemCount[] outputResources;
    public int outputAmount = 1;
}
