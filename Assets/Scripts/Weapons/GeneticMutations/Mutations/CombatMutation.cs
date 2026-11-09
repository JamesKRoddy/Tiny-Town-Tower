using UnityEngine;

/// <summary>
/// Unified combat mutation that can modify damage, attack speed, poise damage, and elemental damage
/// Configure values on the mutation prefab to create different combat enhancement variants
/// 
/// WEAPON SYSTEM:
/// - Uses WeaponAttack component for applying modifiers
/// - Works with the unified AttackBase system
/// </summary>
public class CombatMutation : BaseMutationEffect
{
    [Header("Combat Modifiers")]
    [Tooltip("Multiplier for weapon damage (1.0 = no change, 1.5 = +50% damage)")]
    [SerializeField] private float damageMultiplier = 1.0f;
    
    [Tooltip("Multiplier for attack speed (1.0 = no change, 1.5 = +50% attack speed)")]
    [SerializeField] private float attackSpeedMultiplier = 1.0f;
    
    [Tooltip("Multiplier for poise damage (1.0 = no change, 1.5 = +50% poise damage)")]
    [SerializeField] private float poiseDamageMultiplier = 1.0f;
    
    [Tooltip("Multiplier for elemental damage bonus (1.0 = no change, 2.0 = double elemental damage)")]
    [SerializeField] private float elementalDamageMultiplier = 1.0f;
    
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
        ApplyWeaponModifiersInternal();
    }

    protected override void RemoveEffect()
    {
        ActiveInstances--;
        ApplyWeaponModifiersInternal();
    }

    private void ApplyWeaponModifiersInternal()
    {
        if (characterInventory?.weaponAttack == null) return;

        if (ActiveInstances > 0)
        {
            characterInventory.weaponAttack.ApplyMutationMultipliers(
                damageMultiplier, 
                poiseDamageMultiplier,
                attackSpeedMultiplier, 
                elementalDamageMultiplier,
                ActiveInstances
            );
        }
        else
        {
            characterInventory.weaponAttack.RestoreOriginalStats();
        }
    }

    protected override void HandleWeaponChange(WeaponScriptableObj newWeapon)
    {
        if (isActive)
        {
            base.HandleWeaponChange(newWeapon);
        }
    }

    public override string GetStatsDescription()
    {
        string description = "";
        
        if (damageMultiplier != 1.0f)
        {
            description += $"Damage: {(damageMultiplier >= 1 ? "+" : "")}{((damageMultiplier - 1) * 100):F0}%\n";
        }
        
        if (attackSpeedMultiplier != 1.0f)
        {
            description += $"Attack Speed: {(attackSpeedMultiplier >= 1 ? "+" : "")}{((attackSpeedMultiplier - 1) * 100):F0}%\n";
        }
        
        if (poiseDamageMultiplier != 1.0f)
        {
            description += $"Poise Damage: {(poiseDamageMultiplier >= 1 ? "+" : "")}{((poiseDamageMultiplier - 1) * 100):F0}%\n";
        }
        
        if (elementalDamageMultiplier != 1.0f)
        {
            description += $"Elemental Damage: {(elementalDamageMultiplier >= 1 ? "+" : "")}{((elementalDamageMultiplier - 1) * 100):F0}%\n";
        }
        
        return description.TrimEnd('\n');
    }
}
