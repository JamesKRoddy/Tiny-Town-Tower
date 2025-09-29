using UnityEngine;

public abstract class WeaponBase : MonoBehaviour, IPickupableItem
{
    [Header("General Weapon Stats")]
    protected WeaponScriptableObj weaponData;
    
    // Store original values for mutation restoration
    private int originalDamage;
    private float originalPoiseDamage;
    private float originalAttackSpeed;
    private int originalElementalDamageBonus;
    private bool originalValuesStored = false;
    
    // Store current modified values
    private int currentDamage;
    private float currentPoiseDamage;
    private float currentAttackSpeed;
    private int currentElementalDamageBonus;

    public WeaponScriptableObj WeaponData => weaponData;
    
    // Getter properties for current weapon stats
    public int CurrentDamage => currentDamage;
    public float CurrentPoiseDamage => currentPoiseDamage;
    public float CurrentAttackSpeed => currentAttackSpeed;
    public int CurrentElementalDamageBonus => currentElementalDamageBonus;
    
    // Elemental properties
    public AttackElement WeaponElement => weaponData?.weaponElement ?? AttackElement.NONE;
    public bool IsPureElemental => weaponData?.isPureElemental ?? false;
    public StatusEffectType[] PossibleStatusEffects => weaponData?.possibleStatusEffects ?? new StatusEffectType[0];
    public float StatusEffectChance => weaponData?.statusEffectChance ?? 0f;
    public float StatusEffectDuration => weaponData?.statusEffectDuration ?? 0f;

    public virtual void Initialize(ResourceScriptableObj data, int count = 1)
    {
        if (data is WeaponScriptableObj weaponScriptableObj)
        {
            weaponData = weaponScriptableObj;
            StoreOriginalValues();
        }
        else
        {
            Debug.LogError($"Attempted to initialize weapon with incorrect data type: {data.GetType()}");
        }
    }

    // Store original weapon values for mutation restoration
    private void StoreOriginalValues()
    {
        if (!originalValuesStored && weaponData != null)
        {
            originalDamage = weaponData.damage;
            originalPoiseDamage = weaponData.poiseDamage;
            originalAttackSpeed = weaponData.attackSpeed;
            originalElementalDamageBonus = weaponData.elementalDamageBonus;
            currentDamage = originalDamage;
            currentPoiseDamage = originalPoiseDamage;
            currentAttackSpeed = originalAttackSpeed;
            currentElementalDamageBonus = originalElementalDamageBonus;
            originalValuesStored = true;
        }
    }

    // Apply mutation multipliers to weapon stats (on instance, not ScriptableObject)
    public void ApplyMutationMultipliers(float damageMultiplier = 1f, float poiseDamageMultiplier = 1f, float attackSpeedMultiplier = 1f, float elementalDamageMultiplier = 1f, int activeInstances = 1)
    {
        if (weaponData == null) return;
        
        StoreOriginalValues();
        
        // Apply the multiplier for each active instance (stacking)
        currentDamage = Mathf.RoundToInt(originalDamage * Mathf.Pow(damageMultiplier, activeInstances));
        currentPoiseDamage = originalPoiseDamage * Mathf.Pow(poiseDamageMultiplier, activeInstances);
        currentAttackSpeed = originalAttackSpeed * Mathf.Pow(attackSpeedMultiplier, activeInstances);
        currentElementalDamageBonus = Mathf.RoundToInt(originalElementalDamageBonus * Mathf.Pow(elementalDamageMultiplier, activeInstances));
    }

    // Restore original weapon stats
    public void RestoreOriginalStats()
    {
        if (weaponData == null || !originalValuesStored) return;
        
        currentDamage = originalDamage;
        currentPoiseDamage = originalPoiseDamage;
        currentAttackSpeed = originalAttackSpeed;
        currentElementalDamageBonus = originalElementalDamageBonus;
    }

    // Get current damage (modified by mutations)
    public int GetCurrentDamage() => currentDamage;
    
    // Get current poise damage (modified by mutations)
    public float GetCurrentPoiseDamage() => currentPoiseDamage;
    
    // Get current attack speed (modified by mutations)
    public float GetCurrentAttackSpeed() => currentAttackSpeed;
    
    // Get current elemental damage bonus (modified by mutations)
    public int GetCurrentElementalDamageBonus() => currentElementalDamageBonus;

    /// <summary>
    /// Deals damage to a target using this weapon's elemental properties
    /// </summary>
    /// <param name="target">The target to damage</param>
    /// <param name="damageSource">The source of the damage (usually the weapon or character)</param>
    public virtual void DealDamage(IDamageable target, Transform damageSource)
    {
        if (target == null) return;
        
        // Calculate total damage
        int totalDamage = GetCurrentDamage();
        float poiseDamage = GetCurrentPoiseDamage();
        AttackElement elementType = WeaponElement;
        
        // Add elemental damage bonus if weapon has elemental properties
        if (elementType != AttackElement.NONE)
        {
            totalDamage += GetCurrentElementalDamageBonus();
        }
        
        // Deal damage based on weapon type
        if (elementType == AttackElement.NONE || elementType == AttackElement.PHYSICAL)
        {
            // Physical damage
            target.TakeDamage(totalDamage, poiseDamage, damageSource);
        }
        else
        {
            // Elemental damage
            target.TakeDamage(totalDamage, poiseDamage, elementType, damageSource);
        }
        
        // Apply status effects
        ApplyStatusEffects(target, damageSource);
    }
    
    /// <summary>
    /// Applies status effects based on weapon properties
    /// </summary>
    /// <param name="target">The target to apply status effects to</param>
    /// <param name="damageSource">The source of the damage</param>
    protected virtual void ApplyStatusEffects(IDamageable target, Transform damageSource)
    {
        if (PossibleStatusEffects == null || PossibleStatusEffects.Length == 0) return;
        if (StatusEffectChance <= 0f) return;
        
        // Check if status effect should be applied
        if (Random.Range(0f, 1f) <= StatusEffectChance)
        {
            // Select a random status effect from possible effects
            StatusEffectType selectedEffect = PossibleStatusEffects[Random.Range(0, PossibleStatusEffects.Length)];
            
            // Apply the status effect
            ApplyStatusEffect(target, selectedEffect);
        }
    }
    
    /// <summary>
    /// Applies a specific status effect to a target
    /// Override this method in derived classes for custom status effect application
    /// </summary>
    /// <param name="target">The target to apply the effect to</param>
    /// <param name="effectType">The type of status effect to apply</param>
    protected virtual void ApplyStatusEffect(IDamageable target, StatusEffectType effectType)
    {
        // This is a basic implementation - you may want to integrate with your status effect system
        Debug.Log($"Applied {effectType} status effect to {target} for {StatusEffectDuration} seconds");
        
        // TODO: Integrate with your status effect system here
        // Example: StatusEffectManager.Instance.ApplyEffect(target, effectType, StatusEffectDuration);
    }
    
    /// <summary>
    /// Gets the total damage this weapon would deal (including elemental bonus)
    /// </summary>
    /// <returns>Total damage amount</returns>
    public int GetTotalDamage()
    {
        int totalDamage = GetCurrentDamage();
        
        if (WeaponElement != AttackElement.NONE)
        {
            totalDamage += GetCurrentElementalDamageBonus();
        }
        
        return totalDamage;
    }

    // Virtual method to override in child classes
    public abstract void Use();

    public abstract void StopUse();

    public abstract void OnEquipped(Transform character);

    public string GetItemName() => weaponData?.objectName ?? "Unnamed Weapon";

    public string GetItemDescription() => weaponData?.description ?? "No description available";

    public Sprite GetItemImage() => weaponData?.sprite;
}
