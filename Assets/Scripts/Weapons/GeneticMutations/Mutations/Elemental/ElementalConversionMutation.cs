using UnityEngine;

/// <summary>
/// Mutation that converts physical damage dealt by the NPC to elemental damage
/// </summary>
public class ElementalConversionMutation : BaseElementalMutation
{
    [Header("Elemental Conversion Settings")]
    
    [Tooltip("Percentage of physical damage to convert (0.0 = no conversion, 1.0 = full conversion)")]
    [Range(0f, 1f)]
    [SerializeField] private float conversionPercentage = 0.5f;
    
    [Tooltip("Additional elemental damage bonus when converting")]
    [SerializeField] private int elementalDamageBonus = 3;
    
    private static int activeInstancesCount = 0;

    protected override int ActiveInstances
    {
        get => activeInstancesCount;
        set => activeInstancesCount = value;
    }

    protected override void ApplyElementalEffect()
    {
        // Apply conversion effect
        // This would need to be implemented in HumanCharacterController
        // The NPC would need to track this mutation and apply conversion when dealing damage
        Debug.Log($"Applied elemental conversion: {conversionPercentage * 100:F0}% physical -> {affectedElement} +{elementalDamageBonus}");
    }

    protected override void RemoveElementalEffect()
    {
        // Remove conversion effect
        Debug.Log($"Removed elemental conversion for {affectedElement}");
    }

    public override string GetStatsDescription()
    {
        return $"Convert {conversionPercentage * 100:F0}% Physical → {affectedElement} +{elementalDamageBonus}";
    }
}
