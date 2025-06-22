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
        ApplyWeaponModifiers(damageReductionMultiplier, attackSpeedMultiplier);
    }

    protected override void RemoveEffect()
    {
        ActiveInstances--;
        ApplyWeaponModifiers(damageReductionMultiplier, attackSpeedMultiplier);
    }

    public override string GetStatsDescription()
    {
        return $"Attack Speed: +{((attackSpeedMultiplier - 1) * 100):F0}%\nDamage: {((damageReductionMultiplier - 1) * 100):F0}%";
    }
} 