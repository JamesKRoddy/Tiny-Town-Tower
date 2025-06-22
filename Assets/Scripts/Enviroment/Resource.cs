using UnityEngine;

public class Resource : MonoBehaviour, IPickupableItem, IInteractive<ResourceItemCount>
{
    [SerializeField] private ResourceScriptableObj resourceData; // Reference to the ScriptableObject
    [SerializeField] private int resourceCount; // Amount of the resource

    public void Initialize(ResourceScriptableObj data, int count = 1)
    {
        if (data is ResourceScriptableObj resourceScriptableObj)
        {
            this.resourceData = resourceScriptableObj;
            this.resourceCount = count;
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

    public ResourceItemCount Interact()
    {
        // Create a pickup object
        ResourceItemCount pickup = new ResourceItemCount(resourceData, resourceCount);

        // Optionally destroy this resource's GameObject
        Destroy(gameObject);

        return pickup;
    }

    public bool CanInteract() => true;

    public string GetInteractionText() => $"Pickup: {resourceData.objectName}";

    object IInteractiveBase.Interact() => Interact();
}
