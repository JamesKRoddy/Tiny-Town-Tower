using UnityEngine;

/// <summary>
/// Mutation that modifies the NPC's resistance to specific elemental damage types
/// Can make NPCs immune, resistant, weak, or vulnerable to specific elements
/// </summary>
public class ElementalResistanceMutation : BaseElementalMutation
{
    [Header("Elemental Resistance Settings")]
    
    [Tooltip("The resistance level to apply")]
    [SerializeField] private DamageResistance resistanceLevel = DamageResistance.RESISTANT;
    
    [Tooltip("Whether to override existing resistance or add to it")]
    [SerializeField] private bool overrideResistance = true;
    
    private static int activeInstancesCount = 0;
    private ElementalResistance originalResistance;

    protected override int ActiveInstances
    {
        get => activeInstancesCount;
        set => activeInstancesCount = value;
    }

    protected override void ApplyElementalEffect()
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

    protected override void RemoveElementalEffect()
    {
        // Restore original resistance
        if (originalResistance != null)
        {
            SetResistance(originalResistance.resistance);
            Debug.Log($"Restored original resistance: {affectedElement} -> {originalResistance.resistance}");
        }
    }

    public override string GetStatsDescription()
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
        
        return $"{affectedElement} Resistance: {resistanceText}";
    }
}
