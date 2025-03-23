using UnityEngine;

public class AttackSpeedMutation : BaseMutationEffect
{
    [SerializeField] private float attackSpeedMultiplier = 1.5f;
    [SerializeField] private float damageReductionMultiplier = 0.8f;
    private CharacterInventory characterInventory;
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
        // Get the inventory component from the possessed NPC
        characterInventory = PlayerController.Instance._possessedNPC.GetTransform().GetComponent<CharacterInventory>();
        if (characterInventory == null)
        {
            Debug.LogError("No CharacterInventory component found on possessed NPC!");
            return;
        }

        // Store original weapon
        originalWeapon = characterInventory.equippedWeaponScriptObj;

        // Create a modified version of the weapon
        if (originalWeapon != null)
        {
            WeaponScriptableObj modifiedWeapon = ScriptableObject.CreateInstance<WeaponScriptableObj>();
            modifiedWeapon.damage = Mathf.RoundToInt(originalWeapon.damage * Mathf.Pow(damageReductionMultiplier, ActiveInstances));
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
                modifiedWeapon.damage = Mathf.RoundToInt(originalWeapon.damage * Mathf.Pow(damageReductionMultiplier, ActiveInstances));
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
        originalWeapon = null;
    }
} 