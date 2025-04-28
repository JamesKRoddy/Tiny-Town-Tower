using UnityEngine;


[CreateAssetMenu(fileName = "ResourceUpgradeScriptableObj", menuName = "Scriptable Objects/Camp/ResourceUpgradeScriptableObj")]
public class ResourceUpgradeScriptableObj : WorldItemBase
{
    public bool isUnlocked = false;
    public ResourceItemCount[] requiredResources;
    public float upgradeTime;
    public ResourceScriptableObj outputResource;
    public int outputAmount = 1;
}
