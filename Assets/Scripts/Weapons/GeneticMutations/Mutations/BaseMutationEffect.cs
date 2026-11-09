using UnityEngine;
using Combat.Attacks;

/// <summary>
/// Base class for mutation effects.
/// 
/// WEAPON SYSTEM:
/// - Uses WeaponAttack component for applying mutation effects
/// - Subscribes to weapon changes via CharacterInventory
/// </summary>
public abstract class BaseMutationEffect : MonoBehaviour, IPickupableItem
{
    protected bool isActive = false;
    protected GeneticMutationObj mutationData;
    protected CharacterInventory characterInventory;
    private WeaponAttack currentWeaponAttack;

    // Abstract property that derived classes must implement
    protected abstract int ActiveInstances { get; set; }

    public void Initialize(ResourceScriptableObj data, int count = 1)
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
            // Apply effect to currently equipped weapon attack
            currentWeaponAttack = characterInventory.weaponAttack;
            ApplyEffect();
        }
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
        currentWeaponAttack = null;
    }

    protected abstract void ApplyEffect();
    protected abstract void RemoveEffect();

    // Handle weapon changes - apply effects to new weapons
    protected virtual void HandleWeaponChange(WeaponScriptableObj newWeapon)
    {
        if (!isActive) return;

        // Get the WeaponAttack component
        currentWeaponAttack = characterInventory?.weaponAttack;
        
        // Apply effects to the new weapon
        ApplyEffect();
    }

    // Helper method to apply mutation multipliers to the current weapon
    protected void ApplyWeaponModifiers(float damageMultiplier = 1f, float attackSpeedMultiplier = 1f)
    {
        if (currentWeaponAttack == null) return;

        if (ActiveInstances > 0)
        {
            currentWeaponAttack.ApplyMutationMultipliers(damageMultiplier, 1f, attackSpeedMultiplier, 1f, ActiveInstances);
        }
        else
        {
            currentWeaponAttack.RestoreOriginalStats();
        }
    }
    
    // Helper method to apply mutation multipliers with all parameters
    protected void ApplyWeaponModifiers(float damageMultiplier, float poiseDamageMultiplier, float attackSpeedMultiplier, float elementalDamageMultiplier)
    {
        if (currentWeaponAttack == null) return;

        if (ActiveInstances > 0)
        {
            currentWeaponAttack.ApplyMutationMultipliers(damageMultiplier, poiseDamageMultiplier, attackSpeedMultiplier, elementalDamageMultiplier, ActiveInstances);
        }
        else
        {
            currentWeaponAttack.RestoreOriginalStats();
        }
    }

    // Public property to access mutation data
    public GeneticMutationObj MutationData => mutationData;
    
    // Protected accessor for current weapon attack
    protected WeaponAttack CurrentWeaponAttack => currentWeaponAttack;

    // Abstract method that derived classes must implement to return their stats description
    public abstract string GetStatsDescription();
} 
