using UnityEngine;

public abstract class BaseMutationEffect : MonoBehaviour
{
    protected bool isActive = false;
    protected GeneticMutationObj mutationData;

    // Abstract property that derived classes must implement
    protected abstract int ActiveInstances { get; set; }

    public virtual void Initialize(GeneticMutationObj mutationData)
    {
        this.mutationData = mutationData;
    }

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
} 