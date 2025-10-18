using UnityEngine;
using System.Collections;

/// <summary>
/// Unified survival mutation that can provide health regeneration, shields, and other defensive bonuses
/// Configure values on the mutation prefab to create different survival enhancement variants
/// </summary>
public class SurvivalMutation : BaseMutationEffect
{
    [Header("Health Regeneration")]
    [Tooltip("Amount of health to regenerate per second (0 = disabled)")]
    [SerializeField] private float healthRegenPerSecond = 0f;
    
    [Tooltip("How often to apply the regeneration")]
    [SerializeField] private float regenInterval = 1f;
    
    [Header("Max Health Modification")]
    [Tooltip("Multiplier for max health (1.0 = no change, 1.5 = +50% max health)")]
    [SerializeField] private float maxHealthMultiplier = 1.0f;
    
    [Tooltip("Additional flat max health bonus")]
    [SerializeField] private int flatMaxHealthBonus = 0;
    
    [Header("Damage Reduction")]
    [Tooltip("Percentage of damage to reduce (0 = disabled, 0.1 = 10% reduction, 0.5 = 50% reduction)")]
    [Range(0f, 1f)]
    [SerializeField] private float damageReductionPercentage = 0f;
    
    private static int activeInstancesCount = 0;
    private IDamageable damageable;
    private Coroutine regenCoroutine;

    protected override int ActiveInstances
    {
        get => activeInstancesCount;
        set => activeInstancesCount = value;
    }

    protected override void ApplyEffect()
    {
        if (!isActive) return;

        ActiveInstances++;
        
        // Get the damageable component from the possessed NPC
        Transform npcTransform = PlayerController.Instance._possessedNPC.GetTransform();
        damageable = npcTransform.GetComponent<IDamageable>();
        
        if (damageable == null)
        {
            Debug.LogError("No IDamageable component found on possessed NPC!");
            return;
        }

        // Apply health regeneration if enabled
        if (healthRegenPerSecond > 0f)
        {
            regenCoroutine = StartCoroutine(RegenerateHealth());
        }

        // Apply max health modification if enabled
        if (maxHealthMultiplier != 1.0f || flatMaxHealthBonus != 0)
        {
            ApplyMaxHealthModification();
        }

        // Apply damage reduction if enabled
        if (damageReductionPercentage > 0f)
        {
            ApplyDamageReduction();
        }
    }

    protected override void RemoveEffect()
    {
        ActiveInstances--;
        
        // Stop health regeneration
        if (regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
            regenCoroutine = null;
        }

        // Remove max health modification
        if (maxHealthMultiplier != 1.0f || flatMaxHealthBonus != 0)
        {
            RemoveMaxHealthModification();
        }

        // Remove damage reduction
        if (damageReductionPercentage > 0f)
        {
            RemoveDamageReduction();
        }

        damageable = null;
    }

    private IEnumerator RegenerateHealth()
    {
        while (isActive && damageable != null)
        {
            float regenAmount = healthRegenPerSecond * regenInterval * ActiveInstances;
            damageable.Heal(regenAmount);
            yield return new WaitForSeconds(regenInterval);
        }
    }

    private void ApplyMaxHealthModification()
    {
        // TODO: Implement max health modification in HumanCharacterController
        Debug.Log($"Applied max health modification: x{maxHealthMultiplier} +{flatMaxHealthBonus}");
    }

    private void RemoveMaxHealthModification()
    {
        // TODO: Implement max health modification removal in HumanCharacterController
        Debug.Log("Removed max health modification");
    }

    private void ApplyDamageReduction()
    {
        // TODO: Implement damage reduction in HumanCharacterController
        Debug.Log($"Applied damage reduction: {damageReductionPercentage * 100:F0}%");
    }

    private void RemoveDamageReduction()
    {
        // TODO: Implement damage reduction removal in HumanCharacterController
        Debug.Log("Removed damage reduction");
    }

    protected override void HandleWeaponChange(WeaponScriptableObj newWeapon)
    {
        // SurvivalMutation doesn't modify weapons directly, so we don't need to do anything special
        base.HandleWeaponChange(newWeapon);
    }

    public override string GetStatsDescription()
    {
        string description = "";
        
        if (healthRegenPerSecond > 0f)
        {
            description += $"Health Regen: +{healthRegenPerSecond:F1} HP/s\n";
        }
        
        if (maxHealthMultiplier != 1.0f || flatMaxHealthBonus != 0)
        {
            string multText = maxHealthMultiplier != 1.0f ? $"+{((maxHealthMultiplier - 1) * 100):F0}%" : "";
            string flatText = flatMaxHealthBonus != 0 ? $"+{flatMaxHealthBonus}" : "";
            string separator = multText != "" && flatText != "" ? " " : "";
            description += $"Max Health: {multText}{separator}{flatText}\n";
        }
        
        if (damageReductionPercentage > 0f)
        {
            description += $"Damage Reduction: {damageReductionPercentage * 100:F0}%\n";
        }
        
        return description.TrimEnd('\n');
    }
}

