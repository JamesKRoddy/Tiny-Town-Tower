using UnityEngine;

/// <summary>
/// Base class for elemental-based mutations
/// Provides common functionality for elemental damage and resistance mutations
/// </summary>
public abstract class BaseElementalMutation : BaseMutationEffect
{
    [Header("Elemental Settings")]
    [Tooltip("The elemental type this mutation affects")]
    [SerializeField] protected AttackElement affectedElement = AttackElement.FIRE;
    
    protected HumanCharacterController npcController;
    protected bool isApplied = false;

    protected override void ApplyEffect()
    {
        if (!isActive) return;

        // Get the NPC controller
        npcController = PlayerController.Instance._possessedNPC as HumanCharacterController;
        if (npcController == null)
        {
            Debug.LogError($"{GetType().Name}: Could not get HumanCharacterController from possessed NPC!");
            return;
        }

        // Apply the specific elemental effect
        ApplyElementalEffect();
        isApplied = true;
    }

    protected override void RemoveEffect()
    {
        if (isApplied)
        {
            // Remove the specific elemental effect
            RemoveElementalEffect();
            isApplied = false;
        }
        
        npcController = null;
    }

    /// <summary>
    /// Apply the specific elemental effect - to be implemented by derived classes
    /// </summary>
    protected abstract void ApplyElementalEffect();

    /// <summary>
    /// Remove the specific elemental effect - to be implemented by derived classes
    /// </summary>
    protected abstract void RemoveElementalEffect();

    /// <summary>
    /// Get the current resistance level for the affected element
    /// </summary>
    protected DamageResistance GetCurrentResistance()
    {
        return npcController?.GetResistance(affectedElement) ?? DamageResistance.NORMAL;
    }

    /// <summary>
    /// Set a new resistance level for the affected element
    /// This would need to be implemented in HumanCharacterController
    /// </summary>
    protected void SetResistance(DamageResistance newResistance)
    {
        if (npcController == null) return;
        
        // TODO: Implement SetResistance method in HumanCharacterController
        Debug.Log($"Setting {affectedElement} resistance to {newResistance} for {npcController.name}");
    }

    /// <summary>
    /// Get the current elemental damage bonus for the affected element
    /// This would need to be implemented in HumanCharacterController
    /// </summary>
    protected int GetElementalDamageBonus()
    {
        if (npcController == null) return 0;
        
        // TODO: Implement GetElementalDamageBonus method in HumanCharacterController
        return 0;
    }

    /// <summary>
    /// Set the elemental damage bonus for the affected element
    /// This would need to be implemented in HumanCharacterController
    /// </summary>
    protected void SetElementalDamageBonus(int bonus)
    {
        if (npcController == null) return;
        
        // TODO: Implement SetElementalDamageBonus method in HumanCharacterController
        Debug.Log($"Setting {affectedElement} damage bonus to {bonus} for {npcController.name}");
    }
}

