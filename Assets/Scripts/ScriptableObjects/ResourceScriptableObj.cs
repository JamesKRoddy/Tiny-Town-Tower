using UnityEngine;

[CreateAssetMenu(fileName = "ResourceScriptableObj", menuName = "Scriptable Objects/Camp/ResourceScriptableObj")]
public class ResourceScriptableObj : WorldItemBase
{
    [Header("Resource Information")]
    public GameObject prefab;
}

[System.Serializable]
public class ResourceItemCount
{
    public ResourceScriptableObj resourceScriptableObj;
    public int count;

    /// <summary>
    /// Constructor to initialize the ResourcePickup.
    /// </summary>
    /// <param name="resourceObj">The ResourceScriptableObj for this pickup.</param>
    /// <param name="initialCount">The initial count of the resource.</param>
    public ResourceItemCount(ResourceScriptableObj resourceObj, int initialCount = 1)
    {
        resourceScriptableObj = resourceObj;
        count = initialCount;
    }

    /// <summary>
    /// Attempts to retrieve the chest item as a ResourceScriptableObj.
    /// Logs an error if the cast fails.
    /// </summary>
    public ResourceScriptableObj GetResourceObj()
    {
        if (!resourceScriptableObj)
        {
            Debug.LogError("Resource has no item");
            return null;
        }

        return resourceScriptableObj;
    }
}