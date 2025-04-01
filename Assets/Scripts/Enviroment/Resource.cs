using UnityEngine;

public class Resource : MonoBehaviour, IPickupableItem, IInteractive<ResourcePickup>
{
    [SerializeField] private ResourceScriptableObj resourceData; // Reference to the ScriptableObject
    [SerializeField] private int resourceCount; // Amount of the resource

    void Start()
    {
        Initialize(resourceData);

        var pickup = resourceData.prefab.GetComponent<IPickupableItem>();
        if(pickup != null) pickup.Initialize(resourceData); // Doing this because the prefab might not have a IPickupableItem component, in the case of a weapon
    }

    public void Initialize(ResourceScriptableObj data)
    {
        if (data is ResourceScriptableObj resourceScriptableObj)
        {
            this.resourceData = resourceScriptableObj;
        }
        else
        {
            throw new System.NotImplementedException();
        }
    }

    // Accessors that pull data from the ScriptableObject
    public string GetItemDescription() => resourceData.description;
    public Sprite GetItemImage() => resourceData.sprite;
    public string GetItemName() => resourceData.objectName;
    public ResourceCategory GetResourceType() => resourceData.category;

    public int GetResourceCount() => resourceCount; // Return the count if needed

    public ResourcePickup Interact()
    {
        // Create a pickup object
        ResourcePickup pickup = new ResourcePickup(resourceData, resourceCount);

        // Optionally destroy this resource's GameObject
        Destroy(gameObject);

        return pickup;
    }

    public bool CanInteract() => true;

    public string GetInteractionText() => $"Pickup: {resourceData.objectName}";

    object IInteractiveBase.Interact() => Interact();
}

[System.Serializable]
public class ResourcePickup
{
    [SerializeField] private ResourceScriptableObj resourceScriptableObj; // UnityEngine.Object to hold a reference to a compatible object
    public int count = 1;

    /// <summary>
    /// Constructor to initialize the ResourcePickup.
    /// </summary>
    /// <param name="resourceObj">The ResourceScriptableObj for this pickup.</param>
    /// <param name="initialCount">The initial count of the resource.</param>
    public ResourcePickup(ResourceScriptableObj resourceObj, int initialCount = 1)
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
