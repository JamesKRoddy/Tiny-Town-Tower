using UnityEngine;

public class CombatEnhancementMutation : BaseMutationEffect
{
    [SerializeField] private float damageMultiplier = 1.5f;
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
        if (OriginalWeapon == null)
        {
            OriginalWeapon = characterInventory.equippedWeaponScriptObj;
        }

        UpdateEquippedWeapon(damageMultiplier);
    }

    protected override void RemoveEffect()
    {
        ActiveInstances--;
        UpdateEquippedWeapon(damageMultiplier);
    }
} 