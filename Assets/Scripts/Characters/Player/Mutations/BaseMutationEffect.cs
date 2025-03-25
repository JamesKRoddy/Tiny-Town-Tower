using UnityEngine;

public abstract class BaseMutationEffect : MonoBehaviour, IPickupableItem
{
    protected bool isActive = false;
    protected GeneticMutationObj mutationData;

    // Abstract property that derived classes must implement
    protected abstract int ActiveInstances { get; set; }

    public void Initialize(GeneticMutationObj data)
    {
        mutationData = data;
    }

    public string GetItemName() => mutationData.itemName;
    public string GetItemDescription() => mutationData.itemDescription;
    public Sprite GetItemImage() => mutationData.mutationIcon;

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