using UnityEngine;

public class CombatEnhancementMutation : BaseMutationEffect
{
    [SerializeField] private float damageMultiplier = 1.5f;
    private WeaponScriptableObj originalWeapon;
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
        if (characterInventory == null)
        {
            Debug.LogError("No CharacterInventory component found on possessed NPC!");
            return;
        }

        // Store original weapon if not already stored
        if (originalWeapon == null)
        {
            originalWeapon = characterInventory.equippedWeaponScriptObj;
        }

        // Create a modified version of the weapon
        if (originalWeapon != null)
        {
            WeaponScriptableObj modifiedWeapon = ScriptableObject.CreateInstance<WeaponScriptableObj>();
            modifiedWeapon.damage = Mathf.RoundToInt(originalWeapon.damage * Mathf.Pow(damageMultiplier, ActiveInstances));
            modifiedWeapon.weaponElement = originalWeapon.weaponElement;
            modifiedWeapon.prefab = originalWeapon.prefab;
            modifiedWeapon.animationType = originalWeapon.animationType;

            // Equip the modified weapon
            characterInventory.EquipWeapon(modifiedWeapon);
        }
    }

    protected override void RemoveEffect()
    {
        ActiveInstances--;
        if (characterInventory != null && originalWeapon != null)
        {
            if (ActiveInstances > 0)
            {
                // If there are still active instances, recalculate damage with remaining stacks
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
        originalWeapon = null;
    }

    protected override void HandleWeaponChange(WeaponScriptableObj newWeapon)
    {
        if (isActive)
        {
            // Store the new weapon as the original if we don't have one yet
            if (originalWeapon == null)
            {
                originalWeapon = newWeapon;
            }
            // Reapply the effect
            base.HandleWeaponChange(newWeapon);
        }
    }
} 