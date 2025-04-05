using UnityEngine;

public abstract class BaseMutationEffect : MonoBehaviour, IPickupableItem
{
    protected bool isActive = false;
    protected GeneticMutationObj mutationData;
    protected CharacterInventory characterInventory;
    private WeaponScriptableObj _originalWeapon;

    protected WeaponScriptableObj OriginalWeapon
    {
        get => _originalWeapon;
        set => _originalWeapon = value;
    }

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
        _originalWeapon = null;
    }

    protected abstract void ApplyEffect();
    protected abstract void RemoveEffect();

    // Handle weapon changes
    protected virtual void HandleWeaponChange(WeaponScriptableObj newWeapon)
    {
        if (isActive)
        {
            // Store the new weapon as the original if we don't have one yet
            if (_originalWeapon == null)
            {
                _originalWeapon = newWeapon;
            }
            // Reapply the effect
            RemoveEffect();
            ApplyEffect();
        }
    }

    // Helper method to create a modified weapon with updated stats
    protected WeaponScriptableObj CreateModifiedWeapon(float damageMultiplier = 1f, float attackSpeedMultiplier = 1f)
    {
        if (_originalWeapon == null) return null;

        WeaponScriptableObj modifiedWeapon = ScriptableObject.CreateInstance<WeaponScriptableObj>();
        modifiedWeapon.damage = Mathf.RoundToInt(_originalWeapon.damage * Mathf.Pow(damageMultiplier, ActiveInstances));
        modifiedWeapon.attackSpeed = _originalWeapon.attackSpeed * Mathf.Pow(attackSpeedMultiplier, ActiveInstances);
        modifiedWeapon.weaponElement = _originalWeapon.weaponElement;
        modifiedWeapon.prefab = _originalWeapon.prefab;
        modifiedWeapon.animationType = _originalWeapon.animationType;
        return modifiedWeapon;
    }

    // Helper method to equip a modified weapon or restore the original
    protected void UpdateEquippedWeapon(float damageMultiplier = 1f, float attackSpeedMultiplier = 1f)
    {
        if (characterInventory == null || _originalWeapon == null) return;

        if (ActiveInstances > 0)
        {
            WeaponScriptableObj modifiedWeapon = CreateModifiedWeapon(damageMultiplier, attackSpeedMultiplier);
            if (modifiedWeapon != null)
            {
                characterInventory.EquipWeapon(modifiedWeapon);
            }
        }
        else
        {
            characterInventory.EquipWeapon(_originalWeapon);
        }
    }

    // Public property to access mutation data
    public GeneticMutationObj MutationData => mutationData;
} 