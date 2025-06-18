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
        ApplyWeaponModifiers(damageMultiplier);
    }

    protected override void RemoveEffect()
    {
        ActiveInstances--;
        ApplyWeaponModifiers(damageMultiplier);
    }

    public override string GetStatsDescription()
    {
        return $"Damage: +{((damageMultiplier - 1) * 100):F0}%";
    }
} 