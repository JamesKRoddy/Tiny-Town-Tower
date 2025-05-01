using UnityEngine;


[CreateAssetMenu(fileName = "ResourceUpgradeScriptableObj", menuName = "Scriptable Objects/Camp/ResourceUpgradeScriptableObj")]
public class ResourceUpgradeScriptableObj : WorldItemBase
{
    public bool isUnlocked = false;
    public ResourceItemCount[] requiredResources;
    [Min(5f)]
    public float upgradeTime;
    public ResourceItemCount outputResource;
    public int outputAmount = 1;
}
