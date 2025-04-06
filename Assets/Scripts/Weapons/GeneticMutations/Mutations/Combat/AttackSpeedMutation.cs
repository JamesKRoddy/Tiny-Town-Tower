using UnityEngine;

public class AttackSpeedMutation : BaseMutationEffect
{
    [SerializeField] private float attackSpeedMultiplier = 1.5f;
    [SerializeField] private float damageReductionMultiplier = 1.0f;
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

        UpdateEquippedWeapon(damageReductionMultiplier, attackSpeedMultiplier);
    }

    protected override void RemoveEffect()
    {
        ActiveInstances--;
        UpdateEquippedWeapon(damageReductionMultiplier, attackSpeedMultiplier);
    }
} 