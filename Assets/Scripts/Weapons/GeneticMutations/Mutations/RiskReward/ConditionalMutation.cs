using UnityEngine;
using Managers;
using System.Collections.Generic;

/// <summary>
/// Conditional mutation that applies another mutation when specific conditions are met
/// Configure which conditions to check by setting the relevant fields (leave at default/0/NONE to ignore)
/// Multiple conditions create AND logic - ALL must be true for the mutation to apply
/// Examples: "health < 30% AND using fire weapon", "poise broken AND in combat", etc.
/// </summary>
public class ConditionalMutation : BaseMutationEffect
{
    [Header("Health Conditions")]
    [Tooltip("Check if health is below this threshold (0 = disabled, 0.01-1.0 = enabled)")]
    [Range(0f, 1f)]
    [SerializeField] private float healthBelowThreshold = 0f;
    
    [Tooltip("Check if health is above this threshold (0 = disabled, set to 0.99 for full health)")]
    [Range(0f, 1f)]
    [SerializeField] private float healthAboveThreshold = 0f;
    
    [Header("Poise Conditions")]
    [Tooltip("Check if poise is below this threshold (0 = disabled, set to 0.01 for broken poise)")]
    [Range(0f, 1f)]
    [SerializeField] private float poiseBelowThreshold = 0f;
    
    [Tooltip("Check if poise is above this threshold (0 = disabled, 0.01-1.0 = enabled)")]
    [Range(0f, 1f)]
    [SerializeField] private float poiseAboveThreshold = 0f;
    
    [Header("Weapon Conditions")]
    [Tooltip("Required weapon element (NONE = any element)")]
    [SerializeField] private AttackElement requiredWeaponElement = AttackElement.NONE;
    
    [Tooltip("Required weapon animation type (NONE = any type)")]
    [SerializeField] private WeaponAnimationType requiredWeaponType = WeaponAnimationType.NONE;
    
    [Header("Status Effect Conditions")]
    [Tooltip("Required status effects to be active on the character (leave empty = none required)")]
    [SerializeField] private StatusEffectType[] requiredStatusEffects = new StatusEffectType[0];
    
    [Header("Combat State Conditions")]
    [Tooltip("Require character to be in combat (took damage recently)")]
    [SerializeField] private bool requireInCombat = false;
    
    [Tooltip("Require character to be out of combat")]
    [SerializeField] private bool requireOutOfCombat = false;
    
    [Tooltip("Seconds since last damage to be considered out of combat")]
    [SerializeField] private float combatTimeout = 5f;
    
    [Header("Mutation to Apply")]
    [Tooltip("GameObject containing the mutation effect to apply when the condition is met")]
    [SerializeField] private GameObject mutationPrefab;
    
    [Tooltip("Whether to allow the conditional mutation to stack with itself if multiple instances are active")]
    [SerializeField] private bool allowStacking = true;
    
    private static int activeInstancesCount = 0;
    private IDamageable damageable;
    private bool conditionMet = false;
    private bool mutationApplied = false;
    private BaseMutationEffect conditionalMutationEffect; // Instantiated from prefab

    protected override int ActiveInstances
    {
        get => activeInstancesCount;
        set => activeInstancesCount = value;
    }

    // Validate configuration in the Unity Editor
    private void OnValidate()
    {
        if (mutationPrefab != null)
        {
            BaseMutationEffect mutationComponent = mutationPrefab.GetComponent<BaseMutationEffect>();
            if (mutationComponent == null)
            {
                Debug.LogWarning($"ConditionalMutation on '{gameObject.name}': Mutation prefab '{mutationPrefab.name}' does not have a BaseMutationEffect component! " +
                                 "Add CombatMutation, ElementalMutation, SurvivalMutation, or another mutation component to the prefab.", this);
            }
        }
    }

    protected override void ApplyEffect()
    {
        if (!isActive) return;

        ActiveInstances++;
        
        // Validate that we have a mutation prefab to apply
        if (mutationPrefab == null)
        {
            Debug.LogError($"{GetType().Name}: No mutation prefab assigned!");
            return;
        }
        
        // Validate that the prefab has a BaseMutationEffect component
        BaseMutationEffect mutationComponent = mutationPrefab.GetComponent<BaseMutationEffect>();
        if (mutationComponent == null)
        {
            Debug.LogError($"{GetType().Name}: Mutation prefab '{mutationPrefab.name}' does not have a BaseMutationEffect component!");
            return;
        }
        
        // Get required components from the possessed NPC
        Transform npcTransform = PlayerController.Instance._possessedNPC.GetTransform();
        damageable = npcTransform.GetComponent<IDamageable>();

        if (characterInventory == null || damageable == null)
        {
            Debug.LogError("Required components not found on possessed NPC!");
            return;
        }

        // Subscribe to relevant events based on what's configured
        if (damageable is HumanCharacterController humanController)
        {
            // Subscribe to health events if any health condition is set
            if (healthBelowThreshold > 0f || healthAboveThreshold > 0f || 
                requireInCombat || requireOutOfCombat)
            {
                humanController.OnDamageTaken += OnHealthChanged;
                humanController.OnHeal += OnHealthChanged;
            }
            
            // Subscribe to poise events if any poise condition is set
            if (poiseBelowThreshold > 0f || poiseAboveThreshold > 0f)
            {
                humanController.OnPoiseBroken += OnPoiseChanged;
            }
            
            // Subscribe to weapon change events if weapon conditions are set
            if (requiredWeaponElement != AttackElement.NONE || requiredWeaponType != WeaponAnimationType.NONE)
            {
                characterInventory.OnWeaponEquipped += OnWeaponChanged;
            }
            
            // Start periodic check for status effects if any are required
            if (requiredStatusEffects != null && requiredStatusEffects.Length > 0)
            {
                StartCoroutine(CheckStatusEffectPeriodically());
            }
            
            // Start periodic check for combat state if needed
            if (requireInCombat || requireOutOfCombat)
            {
                StartCoroutine(CheckCombatState());
            }
            
            // Do initial condition check
            CheckCondition();
        }
    }

    protected override void RemoveEffect()
    {
        ActiveInstances--;
        
        // Remove the conditional mutation if it was applied
        if (mutationApplied)
        {
            RemoveConditionalMutation();
        }

        // Stop any running coroutines
        StopAllCoroutines();
        
        // Unsubscribe from events
        if (damageable is HumanCharacterController humanController)
        {
            humanController.OnDamageTaken -= OnHealthChanged;
            humanController.OnHeal -= OnHealthChanged;
            humanController.OnPoiseBroken -= OnPoiseChanged;
        }
        
        if (characterInventory != null)
        {
            characterInventory.OnWeaponEquipped -= OnWeaponChanged;
        }

        damageable = null;
        conditionMet = false;
    }

    #region Event Handlers
    
    private void OnHealthChanged(float currentHealth, float maxHealth)
    {
        if (!isActive || damageable == null || characterInventory == null) return;
        lastCombatTime = Time.time; // Update last combat time when taking damage
        CheckCondition();
    }
    
    private void OnPoiseChanged(float currentPoise, float maxPoise)
    {
        if (!isActive || damageable == null) return;
        CheckCondition();
    }
    
    private void OnWeaponChanged(WeaponScriptableObj newWeapon)
    {
        if (!isActive) return;
        CheckCondition();
    }
    
    #endregion
    
    #region Condition Checking
    
    private void CheckCondition()
    {
        bool wasConditionMet = conditionMet;
        conditionMet = EvaluateCondition();

        // Only update mutation when crossing the threshold
        if (wasConditionMet != conditionMet)
        {
            if (conditionMet)
            {
                ApplyConditionalMutation();
            }
            else
            {
                RemoveConditionalMutation();
            }
        }
    }
    
    private bool EvaluateCondition()
    {
        if (damageable == null) return false;
        
        HumanCharacterController humanController = damageable as HumanCharacterController;
        if (humanController == null) return false;
        
        // All configured conditions must be true (AND logic)
        
        // Check health conditions
        if (healthBelowThreshold > 0f)
        {
            float healthPercent = humanController.Health / humanController.MaxHealth;
            if (healthPercent > healthBelowThreshold) return false;
        }
        
        if (healthAboveThreshold > 0f)
        {
            float healthPercent = humanController.Health / humanController.MaxHealth;
            if (healthPercent < healthAboveThreshold) return false;
        }
        
        // Check poise conditions
        if (poiseBelowThreshold > 0f)
        {
            float poisePercent = humanController.Poise / humanController.MaxPoise;
            if (poisePercent > poiseBelowThreshold) return false;
        }
        
        if (poiseAboveThreshold > 0f)
        {
            float poisePercent = humanController.Poise / humanController.MaxPoise;
            if (poisePercent < poiseAboveThreshold) return false;
        }
        
        // Check weapon element condition
        if (requiredWeaponElement != AttackElement.NONE)
        {
            if (!CheckWeaponElement(requiredWeaponElement)) return false;
        }
        
        // Check weapon type condition
        if (requiredWeaponType != WeaponAnimationType.NONE)
        {
            if (!CheckWeaponAnimationType(requiredWeaponType)) return false;
        }
        
        // Check status effect conditions (ALL required effects must be active)
        if (requiredStatusEffects != null && requiredStatusEffects.Length > 0)
        {
            foreach (var statusEffect in requiredStatusEffects)
            {
                if (!CheckStatusEffect(statusEffect)) return false;
            }
        }
        
        // Check combat state conditions
        if (requireInCombat)
        {
            if (!CheckInCombat()) return false;
        }
        
        if (requireOutOfCombat)
        {
            if (CheckInCombat()) return false;
        }
        
        // All conditions passed
        return true;
    }
    
    #endregion
    
    #region Helper Methods
    
    private bool CheckWeaponAnimationType(WeaponAnimationType type)
    {
        if (characterInventory?.equippedWeaponScriptObj == null) return false;
        return characterInventory.equippedWeaponScriptObj.animationType == type;
    }
    
    private bool CheckWeaponElement(AttackElement element)
    {
        if (characterInventory?.equippedWeaponScriptObj == null) return false;
        return characterInventory.equippedWeaponScriptObj.weaponElement == element;
    }
    
    private bool CheckStatusEffect(StatusEffectType effectType)
    {
        if (damageable == null) return false;
        
        // Check if EffectManager has this status effect active on the target
        IStatusEffectTarget target = damageable as IStatusEffectTarget;
        if (target == null) return false;
        
        return EffectManager.Instance?.HasStatusEffect(target, effectType) ?? false;
    }
    
    private bool CheckInCombat()
    {
        if (damageable is HumanCharacterController humanController)
        {
            // Check if recently took damage (within configured timeout)
            return Time.time - lastCombatTime < combatTimeout;
        }
        return false;
    }
    
    private float lastCombatTime = -999f;
    
    private System.Collections.IEnumerator CheckStatusEffectPeriodically()
    {
        while (isActive)
        {
            CheckCondition();
            yield return new WaitForSeconds(0.5f); // Check every half second
        }
    }
    
    private System.Collections.IEnumerator CheckCombatState()
    {
        while (isActive)
        {
            CheckCondition();
            yield return new WaitForSeconds(1f); // Check every second
        }
    }
    
    #endregion

    private void ApplyConditionalMutation()
    {
        if (mutationPrefab == null) return;
        
        if (!mutationApplied)
        {
            // Instantiate the mutation prefab and get its BaseMutationEffect component
            GameObject mutationInstance = Instantiate(mutationPrefab, transform);
            conditionalMutationEffect = mutationInstance.GetComponent<BaseMutationEffect>();
            
            if (conditionalMutationEffect == null)
            {
                Debug.LogError($"{GetType().Name}: Mutation prefab does not have a BaseMutationEffect component!");
                Destroy(mutationInstance);
                return;
            }
            
            // Initialize and equip the conditional mutation
            conditionalMutationEffect.Initialize(mutationData, 1);
            conditionalMutationEffect.OnEquip();
            mutationApplied = true;
            
            Debug.Log($"Applied conditional mutation: {conditionalMutationEffect.GetType().Name}");
        }
        else if (allowStacking && ActiveInstances > 1)
        {
            // If stacking is allowed and we have multiple instances, reapply to stack
            conditionalMutationEffect.OnUnequip();
            conditionalMutationEffect.OnEquip();
        }
    }

    private void RemoveConditionalMutation()
    {
        if (conditionalMutationEffect == null) return;
        
        if (mutationApplied)
        {
            conditionalMutationEffect.OnUnequip();
            mutationApplied = false;
            
            // Destroy the instantiated mutation GameObject
            if (conditionalMutationEffect != null)
            {
                Destroy(conditionalMutationEffect.gameObject);
                conditionalMutationEffect = null;
            }
            
            Debug.Log($"Removed conditional mutation");
        }
    }

    protected override void HandleWeaponChange(WeaponScriptableObj newWeapon)
    {
        if (isActive)
        {
            base.HandleWeaponChange(newWeapon);
            
            // Reapply the conditional mutation if condition is met
            if (conditionMet && mutationApplied)
            {
                CheckCondition();
            }
        }
    }

    public override string GetStatsDescription()
    {
        // Get the mutation effect from the prefab to read its description
        string mutationDesc = "No mutation assigned";
        if (mutationPrefab != null)
        {
            var mutationEffect = mutationPrefab.GetComponent<BaseMutationEffect>();
            if (mutationEffect != null)
            {
                mutationDesc = mutationEffect.GetStatsDescription();
            }
            else
            {
                mutationDesc = $"ERROR: '{mutationPrefab.name}' has no mutation component!";
            }
        }
        
        // Build condition text from configured conditions
        List<string> conditions = new List<string>();
        
        if (healthBelowThreshold > 0f)
            conditions.Add($"health < {healthBelowThreshold * 100:F0}%");
            
        if (healthAboveThreshold > 0f)
        {
            // Show "full health" for values >= 99%, otherwise show percentage
            string healthText = healthAboveThreshold >= 0.99f ? "full health" : $"health > {healthAboveThreshold * 100:F0}%";
            conditions.Add(healthText);
        }
            
        if (poiseBelowThreshold > 0f)
        {
            // Show "poise broken" for very low values, otherwise show percentage
            string poiseText = poiseBelowThreshold <= 0.01f ? "poise broken" : $"poise < {poiseBelowThreshold * 100:F0}%";
            conditions.Add(poiseText);
        }
            
        if (poiseAboveThreshold > 0f)
            conditions.Add($"poise > {poiseAboveThreshold * 100:F0}%");
            
        if (requiredWeaponElement != AttackElement.NONE)
            conditions.Add($"using {requiredWeaponElement} weapon");
            
        if (requiredWeaponType != WeaponAnimationType.NONE)
            conditions.Add($"using {requiredWeaponType} weapon");
            
        if (requiredStatusEffects != null && requiredStatusEffects.Length > 0)
        {
            foreach (var effect in requiredStatusEffects)
                conditions.Add($"has {effect}");
        }
            
        if (requireInCombat)
            conditions.Add("in combat");
            
        if (requireOutOfCombat)
            conditions.Add("out of combat");
        
        // Join all conditions with "AND"
        string conditionText = conditions.Count > 0 
            ? "when " + string.Join(" AND ", conditions)
            : "always active";
        
        return $"{mutationDesc} {conditionText}";
    }
}

