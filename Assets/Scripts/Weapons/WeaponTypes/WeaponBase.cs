using UnityEngine;

public abstract class WeaponBase : MonoBehaviour, IPickupableItem, IDamageDealer
{
    [Header("General Weapon Stats")]
    protected WeaponScriptableObj weaponData;
    
    // Character reference for damage source and camera shake
    protected Transform characterTransform;
    
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
    
    // IDamageDealer implementation
    public float BaseDamage => GetCurrentDamage();
    public float PoiseDamage => GetCurrentPoiseDamage();
    public AttackElement ElementType => WeaponElement;
    public int ElementalDamageBonus => GetCurrentElementalDamageBonus();
    public Transform DamageSource => characterTransform;

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
    /// Deals damage to a target using this weapon's elemental properties (IDamageDealer interface implementation)
    /// </summary>
    /// <param name="target">The target to damage</param>
    public virtual void DealDamage(IDamageable target)
    {
        DamageUtils.DealDamage(this, target);
    }
    
    /// <summary>
    /// Deals damage to a target with custom damage amounts (IDamageDealer interface implementation)
    /// </summary>
    /// <param name="target">The target to damage</param>
    /// <param name="damageAmount">Custom damage amount</param>
    /// <param name="poiseAmount">Custom poise damage amount</param>
    public virtual void DealDamage(IDamageable target, float damageAmount, float poiseAmount)
    {
        DamageUtils.DealDamage(this, target, damageAmount, poiseAmount);
    }
    
    /// <summary>
    /// Deals damage to a target using this weapon's elemental properties (legacy method for backward compatibility)
    /// </summary>
    /// <param name="target">The target to damage</param>
    /// <param name="damageSource">The source of the damage (usually the weapon or character)</param>
    public virtual void DealDamage(IDamageable target, Transform damageSource)
    {
        DealDamage(target);
    }
    
    // Note: Status effects and camera shake are now handled by the DamageDealer utility class
    
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

    /// <summary>
    /// Called when this weapon is equipped by a character.
    /// Stores the character transform for damage calculations and camera shake.
    /// Override in derived classes but call base.OnEquipped(character) first.
    /// </summary>
    /// <param name="character">The transform of the character equipping this weapon</param>
    public virtual void OnEquipped(Transform character)
    {
        characterTransform = character;
    }

    public string GetItemName() => weaponData?.objectName ?? "Unnamed Weapon";

    public string GetItemDescription() => weaponData?.description ?? "No description available";

    public Sprite GetItemImage() => weaponData?.sprite;
}
