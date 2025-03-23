using UnityEngine;

public class CombatEnhancementMutation : BaseMutationEffect
{
    [SerializeField] private float damageMultiplier = 1.5f;
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
        characterInventory = PlayerController.Instance._possessedNPC.GetTransform().GetComponent<CharacterInventory>();
        if (characterInventory == null) return;

        originalWeapon = characterInventory.equippedWeaponScriptObj;
        if (originalWeapon != null)
        {
            WeaponScriptableObj modifiedWeapon = ScriptableObject.CreateInstance<WeaponScriptableObj>();
            modifiedWeapon.damage = Mathf.RoundToInt(originalWeapon.damage * Mathf.Pow(damageMultiplier, ActiveInstances));
            modifiedWeapon.weaponElement = originalWeapon.weaponElement;
            modifiedWeapon.prefab = originalWeapon.prefab;
            modifiedWeapon.animationType = originalWeapon.animationType;

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
                // If no more active instances, restore original weapon
                characterInventory.EquipWeapon(originalWeapon);
            }
        }
        characterInventory = null;
        originalWeapon = null;
    }
} 