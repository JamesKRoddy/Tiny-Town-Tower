using UnityEngine;

/// <summary>
/// Unified elemental mutation that can modify elemental damage, resistances, and conversions
/// Configure values on the mutation prefab to create different elemental enhancement variants
/// </summary>
public class ElementalMutation : BaseMutationEffect
{
    [Header("Elemental Type")]
    [Tooltip("The elemental type this mutation affects")]
    [SerializeField] private AttackElement affectedElement = AttackElement.FIRE;
    
    [Header("Elemental Damage Modification")]
    [Tooltip("Multiplier for elemental damage bonus (1.0 = no change, 2.0 = double damage)")]
    [SerializeField] private float elementalDamageMultiplier = 1.0f;
    
    [Tooltip("Additional flat elemental damage bonus")]
    [SerializeField] private int flatElementalDamageBonus = 0;
    
    [Header("Elemental Resistance Modification")]
    [Tooltip("Enable resistance modification")]
    [SerializeField] private bool modifyResistance = false;
    
    [Tooltip("The resistance level to apply")]
    [SerializeField] private DamageResistance resistanceLevel = DamageResistance.RESISTANT;
    
    [Header("Elemental Conversion")]
    [Tooltip("Percentage of physical damage to convert (0 = disabled, 1.0 = full conversion)")]
    [Range(0f, 1f)]
    [SerializeField] private float conversionPercentage = 0f;
    
    [Tooltip("Additional elemental damage bonus when converting")]
    [SerializeField] private int conversionDamageBonus = 0;
    
    private static int activeInstancesCount = 0;
    private HumanCharacterController npcController;
    private bool isApplied = false;
    private ElementalResistance originalResistance;
    private int originalElementalDamageBonus;

    protected override int ActiveInstances
    {
        get => activeInstancesCount;
        set => activeInstancesCount = value;
    }

    protected override void ApplyEffect()
    {
        if (!isActive) return;

        ActiveInstances++;
        
        // Get the NPC controller for elemental effects
        npcController = PlayerController.Instance._possessedNPC as HumanCharacterController;
        if (npcController == null)
        {
            Debug.LogError($"{GetType().Name}: Could not get HumanCharacterController from possessed NPC!");
            return;
        }

        // Apply elemental damage boost if enabled
        if (elementalDamageMultiplier != 1.0f || flatElementalDamageBonus != 0)
        {
            ApplyElementalDamageBoost();
        }

        // Apply resistance modification if enabled
        if (modifyResistance)
        {
            ApplyResistanceModification();
        }

        // Apply conversion if enabled
        if (conversionPercentage > 0f)
        {
            ApplyConversion();
        }

        isApplied = true;
    }

    protected override void RemoveEffect()
    {
        ActiveInstances--;
        
        if (isApplied)
        {
            // Remove elemental damage boost
            if (elementalDamageMultiplier != 1.0f || flatElementalDamageBonus != 0)
            {
                RemoveElementalDamageBoost();
            }

            // Restore original resistance
            if (modifyResistance && originalResistance != null)
            {
                SetResistance(originalResistance.resistance);
            }

            // Remove conversion
            if (conversionPercentage > 0f)
            {
                RemoveConversion();
            }

            isApplied = false;
        }
        
        npcController = null;
    }

    private void ApplyElementalDamageBoost()
    {
        // Store original bonus
        originalElementalDamageBonus = GetElementalDamageBonus();
        
        // Calculate new bonus
        int newBonus = Mathf.RoundToInt(originalElementalDamageBonus * elementalDamageMultiplier) + flatElementalDamageBonus;
        SetElementalDamageBonus(newBonus);
        
        Debug.Log($"Applied elemental damage bonus: {affectedElement} x{elementalDamageMultiplier} +{flatElementalDamageBonus}");
    }

    private void RemoveElementalDamageBoost()
    {
        SetElementalDamageBonus(originalElementalDamageBonus);
        Debug.Log($"Removed elemental damage bonus for {affectedElement}");
    }

    private void ApplyResistanceModification()
    {
        // Store original resistance
        originalResistance = new ElementalResistance
        {
            damageType = affectedElement,
            resistance = GetCurrentResistance()
        };

        // Apply the new resistance
        SetResistance(resistanceLevel);
        
        Debug.Log($"Applied resistance modification: {affectedElement} -> {resistanceLevel}");
    }

    private void ApplyConversion()
    {
        // This would need to be implemented in HumanCharacterController
        // The NPC would need to track this mutation and apply conversion when dealing damage
        Debug.Log($"Applied elemental conversion: {conversionPercentage * 100:F0}% physical → {affectedElement} +{conversionDamageBonus}");
    }

    private void RemoveConversion()
    {
        Debug.Log($"Removed elemental conversion for {affectedElement}");
    }

    #region Helper Methods
    
    private DamageResistance GetCurrentResistance()
    {
        return npcController?.GetResistance(affectedElement) ?? DamageResistance.NORMAL;
    }

    private void SetResistance(DamageResistance newResistance)
    {
        if (npcController == null) return;
        
        // TODO: Implement SetResistance method in HumanCharacterController
        Debug.Log($"Setting {affectedElement} resistance to {newResistance} for {npcController.name}");
    }

    private int GetElementalDamageBonus()
    {
        if (npcController == null) return 0;
        
        // TODO: Implement GetElementalDamageBonus method in HumanCharacterController
        return 0;
    }

    private void SetElementalDamageBonus(int bonus)
    {
        if (npcController == null) return;
        
        // TODO: Implement SetElementalDamageBonus method in HumanCharacterController
        Debug.Log($"Setting {affectedElement} damage bonus to {bonus} for {npcController.name}");
    }
    
    #endregion

    public override string GetStatsDescription()
    {
        string description = "";
        
        if (elementalDamageMultiplier != 1.0f || flatElementalDamageBonus != 0)
        {
            string multText = elementalDamageMultiplier != 1.0f ? $"+{((elementalDamageMultiplier - 1) * 100):F0}%" : "";
            string flatText = flatElementalDamageBonus != 0 ? $"+{flatElementalDamageBonus}" : "";
            string separator = multText != "" && flatText != "" ? " " : "";
            description += $"{affectedElement} Damage: {multText}{separator}{flatText}\n";
        }
        
        if (modifyResistance)
        {
            string resistanceText = resistanceLevel switch
            {
                DamageResistance.IMMUNE => "Immune (0% damage)",
                DamageResistance.RESISTANT => "Resistant (50% damage)",
                DamageResistance.NORMAL => "Normal (100% damage)",
                DamageResistance.WEAK => "Weak (150% damage)",
                DamageResistance.VULNERABLE => "Vulnerable (200% damage)",
                _ => "Unknown"
            };
            
            description += $"{affectedElement} Resistance: {resistanceText}\n";
        }
        
        if (conversionPercentage > 0f)
        {
            description += $"Convert {conversionPercentage * 100:F0}% Physical → {affectedElement}";
            if (conversionDamageBonus != 0)
            {
                description += $" +{conversionDamageBonus}";
            }
            description += "\n";
        }
        
        return description.TrimEnd('\n');
    }
}

