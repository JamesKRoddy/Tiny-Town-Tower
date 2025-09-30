using UnityEngine;

/// <summary>
/// Mutation that increases elemental damage dealt by the NPC's attacks
/// </summary>
public class ElementalDamageMutation : BaseElementalMutation
{
    [Header("Elemental Damage Settings")]
    
    [Tooltip("Multiplier for elemental damage bonus (1.0 = no change, 2.0 = double damage)")]
    [SerializeField] private float elementalDamageMultiplier = 2.0f;
    
    [Tooltip("Additional flat elemental damage bonus")]
    [SerializeField] private int flatElementalDamageBonus = 5;
    
    private static int activeInstancesCount = 0;

    protected override int ActiveInstances
    {
        get => activeInstancesCount;
        set => activeInstancesCount = value;
    }

    protected override void ApplyElementalEffect()
    {
        // Apply elemental damage bonus to the NPC's attack element
        int currentBonus = GetElementalDamageBonus();
        int newBonus = Mathf.RoundToInt(currentBonus * elementalDamageMultiplier) + flatElementalDamageBonus;
        SetElementalDamageBonus(newBonus);
        
        Debug.Log($"Applied elemental damage bonus: {affectedElement} x{elementalDamageMultiplier} +{flatElementalDamageBonus}");
    }

    protected override void RemoveElementalEffect()
    {
        // Remove elemental damage bonus
        SetElementalDamageBonus(0);
        
        Debug.Log($"Removed elemental damage bonus for {affectedElement}");
    }

    public override string GetStatsDescription()
    {
        return $"{affectedElement} Damage: +{((elementalDamageMultiplier - 1) * 100):F0}% +{flatElementalDamageBonus}";
    }
}
