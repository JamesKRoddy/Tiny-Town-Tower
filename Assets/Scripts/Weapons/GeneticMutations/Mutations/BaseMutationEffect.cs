using UnityEngine;

public abstract class BaseMutationEffect : MonoBehaviour, IPickupableItem
{
    protected bool isActive = false;
    protected GeneticMutationObj mutationData;

    // Abstract property that derived classes must implement
    protected abstract int ActiveInstances { get; set; }

    public void Initialize(ResourceScriptableObj data)
    {
        if (data is GeneticMutationObj mutationScriptableObj)
        {
            mutationData = mutationScriptableObj;
        }
        else
        {
            Debug.LogError($"Attempted to initialize mutation with incorrect data type: {data.GetType()}");
        }
    }

    public string GetItemName() => mutationData.objectName;
    public string GetItemDescription() => mutationData.description;
    public Sprite GetItemImage() => mutationData.sprite;

    public virtual void OnEquip()
    {
        isActive = true;
        ApplyEffect();
    }

    public virtual void OnUnequip()
    {
        isActive = false;
        RemoveEffect();
    }

    protected abstract void ApplyEffect();
    protected abstract void RemoveEffect();

    // Public property to access mutation data

    public GeneticMutationObj MutationData => mutationData;
} 