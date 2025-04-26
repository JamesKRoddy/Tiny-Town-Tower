using UnityEngine;

[CreateAssetMenu(fileName = "ResearchScriptableObj", menuName = "Scriptable Objects/Camp/ResearchScriptableObj")]
public class ResearchScriptableObj : WorldItemBase
{
    public ResourceScriptableObj[] requiredResources;
    public ResourceScriptableObj[] outputResources;
    public int researchTime;
    public bool isUnlocked = false;    
}
