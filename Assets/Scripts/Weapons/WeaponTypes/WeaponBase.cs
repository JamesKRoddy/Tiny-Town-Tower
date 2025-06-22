using UnityEngine;

public abstract class WeaponBase : MonoBehaviour, IPickupableItem
{
    [Header("General Weapon Stats")]
    protected WeaponScriptableObj weaponData;
    
    // Store original values for mutation restoration
    private int originalDamage;
    private float originalAttackSpeed;
    private bool originalValuesStored = false;
    
    // Store current modified values
    private int currentDamage;
    private float currentAttackSpeed;

    public WeaponScriptableObj WeaponData => weaponData;

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
            originalAttackSpeed = weaponData.attackSpeed;
            currentDamage = originalDamage;
            currentAttackSpeed = originalAttackSpeed;
            originalValuesStored = true;
        }
    }

    // Apply mutation multipliers to weapon stats (on instance, not ScriptableObject)
    public void ApplyMutationMultipliers(float damageMultiplier = 1f, float attackSpeedMultiplier = 1f, int activeInstances = 1)
    {
        if (weaponData == null) return;
        
        StoreOriginalValues();
        
        // Apply the multiplier for each active instance (stacking)
        currentDamage = Mathf.RoundToInt(originalDamage * Mathf.Pow(damageMultiplier, activeInstances));
        currentAttackSpeed = originalAttackSpeed * Mathf.Pow(attackSpeedMultiplier, activeInstances);
    }

    // Restore original weapon stats
    public void RestoreOriginalStats()
    {
        if (weaponData == null || !originalValuesStored) return;
        
        currentDamage = originalDamage;
        currentAttackSpeed = originalAttackSpeed;
    }

    // Get current damage (modified by mutations)
    public int GetCurrentDamage() => currentDamage;
    
    // Get current attack speed (modified by mutations)
    public float GetCurrentAttackSpeed() => currentAttackSpeed;

    // Virtual method to override in child classes
    public abstract void Use();

    public abstract void StopUse();

    public abstract void OnEquipped(Transform character);

    public string GetItemName() => weaponData?.objectName ?? "Unnamed Weapon";

    public string GetItemDescription() => weaponData?.description ?? "No description available";

    public Sprite GetItemImage() => weaponData?.sprite;
}
