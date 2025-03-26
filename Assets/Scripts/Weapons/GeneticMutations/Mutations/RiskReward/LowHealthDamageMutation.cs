using UnityEngine;

public class LowHealthDamageMutation : BaseMutationEffect
{
    [SerializeField] private float damageMultiplier = 2f;
    [SerializeField] private float healthThreshold = 0.3f; // 30% health threshold
    [SerializeField] private float vulnerabilityMultiplier = 1.5f;
    private CharacterInventory characterInventory;
    private IDamageable damageable;
    private WeaponScriptableObj originalWeapon;
    private bool isLowHealth;
    private static int activeInstancesCount = 0;

    protected override int ActiveInstances
    {
        get => activeInstancesCount;
        set => activeInstancesCount = value;
    }

    protected override void ApplyEffect()
    {
        if (!isActive) return;

        ActiveInstances++;
        // Get the required components from the possessed NPC
        Transform npcTransform = PlayerController.Instance._possessedNPC.GetTransform();
        characterInventory = npcTransform.GetComponent<CharacterInventory>();
        damageable = npcTransform.GetComponent<IDamageable>();

        if (characterInventory == null || damageable == null)
        {
            Debug.LogError("Required components not found on possessed NPC!");
            return;
        }

        // Store original weapon
        originalWeapon = characterInventory.equippedWeaponScriptObj;
    }

    protected override void RemoveEffect()
    {
        ActiveInstances--;
        if (characterInventory != null && originalWeapon != null)
        {
            if (ActiveInstances > 0 && isLowHealth)
            {
                // If there are still active instances and we're in low health, recalculate damage
                WeaponScriptableObj modifiedWeapon = ScriptableObject.CreateInstance<WeaponScriptableObj>();
                modifiedWeapon.damage = Mathf.RoundToInt(originalWeapon.damage * Mathf.Pow(damageMultiplier, ActiveInstances));
                modifiedWeapon.weaponElement = originalWeapon.weaponElement;
                modifiedWeapon.prefab = originalWeapon.prefab;
                modifiedWeapon.animationType = originalWeapon.animationType;
                characterInventory.EquipWeapon(modifiedWeapon);
            }
            else
            {
                // Restore original weapon
                characterInventory.EquipWeapon(originalWeapon);
            }
        }
        characterInventory = null;
        damageable = null;
        originalWeapon = null;
        isLowHealth = false;
    }

    private void Update()
    {
        if (!isActive || damageable == null || characterInventory == null) return;

        float currentHealthPercent = damageable.Health / damageable.MaxHealth;
        bool wasLowHealth = isLowHealth;
        isLowHealth = currentHealthPercent <= healthThreshold;

        // Only update weapon when crossing the threshold
        if (wasLowHealth != isLowHealth)
        {
            if (isLowHealth)
            {
                // Create a modified version of the weapon with increased damage
                WeaponScriptableObj modifiedWeapon = ScriptableObject.CreateInstance<WeaponScriptableObj>();
                modifiedWeapon.damage = Mathf.RoundToInt(originalWeapon.damage * Mathf.Pow(damageMultiplier, ActiveInstances));
                modifiedWeapon.weaponElement = originalWeapon.weaponElement;
                modifiedWeapon.prefab = originalWeapon.prefab;
                modifiedWeapon.animationType = originalWeapon.animationType;

                // Equip the modified weapon
                characterInventory.EquipWeapon(modifiedWeapon);
            }
            else
            {
                // Restore original weapon
                characterInventory.EquipWeapon(originalWeapon);
            }
        }
    }
} 