using UnityEngine;


[CreateAssetMenu(fileName = "ResourceUpgradeScriptableObj", menuName = "Scriptable Objects/Camp/ResourceUpgradeScriptableObj")]
public class ResourceUpgradeScriptableObj : ScriptableObject
{
    public string objectName;
    public string description;
    public Sprite sprite;
    public bool isUnlocked = false;
    public ResourceItemCount[] requiredResources;
    public float upgradeTime;
    public ResourceScriptableObj outputResource;
    public int outputAmount = 1;
}
