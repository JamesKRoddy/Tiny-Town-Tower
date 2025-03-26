using UnityEngine;

public abstract class BaseMutationEffect : MonoBehaviour, IPickupableItem
{
    protected bool isActive = false;
    protected GeneticMutationObj mutationData;
    protected CharacterInventory characterInventory;

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
        // Get the inventory component from the possessed NPC
        characterInventory = PlayerController.Instance._possessedNPC.GetTransform().GetComponent<CharacterInventory>();
        if (characterInventory != null)
        {
            // Subscribe to weapon changes
            characterInventory.OnWeaponEquipped += HandleWeaponChange;
        }
        ApplyEffect();
    }

    public virtual void OnUnequip()
    {
        isActive = false;
        if (characterInventory != null)
        {
            // Unsubscribe from weapon changes
            characterInventory.OnWeaponEquipped -= HandleWeaponChange;
        }
        RemoveEffect();
        characterInventory = null;
    }

    protected abstract void ApplyEffect();
    protected abstract void RemoveEffect();

    // Handle weapon changes
    protected virtual void HandleWeaponChange(WeaponScriptableObj newWeapon)
    {
        if (isActive)
        {
            // Reapply the effect with the new weapon
            RemoveEffect();
            ApplyEffect();
        }
    }

    // Public property to access mutation data
    public GeneticMutationObj MutationData => mutationData;
} 