using System.Collections.Generic;
using System.Linq;
using Managers;
using UnityEngine;

/// <summary>
/// Centralized utility class for managing status effect priority, categories, and display logic.
/// This provides a single source of truth for how status effects interact and are presented.
/// </summary>
public static class StatusEffectUtils
{
    #region Status Effect Categories & Slots
    
    /// <summary>
    /// Categories for organizing status effects by their nature and mutual exclusivity
    /// Effects in the same category can coexist unless specifically marked as conflicting
    /// </summary>
    public enum StatusCategory
    {
        HEALTH_HUNGER,       // Hunger-related: Hungry, Starving (mutually exclusive)
        HEALTH_FATIGUE,      // Fatigue-related: Tired, Exhausted (mutually exclusive)
        HEALTH_ILLNESS,      // Illness-related: Sick (standalone)
        ACTIVITY,            // Working, Sleeping, Eating (mutually exclusive - only one activity at a time)
        COMBAT,              // Fighting, Fleeing (mutually exclusive)
        ELEMENTAL_FIRE,      // Fire-related: On Fire, Burning (can stack with other elements)
        ELEMENTAL_COLD,      // Cold-related: Frozen (can stack with other elements)
        ELEMENTAL_ELECTRIC,  // Electric-related: Electrocuted (can stack with other elements)
        MEDICAL,             // Receiving treatment (standalone)
        POSITIVE             // Healthy (only shows when no negative conditions)
    }
    
    /// <summary>
    /// Display slots for UI organization
    /// Each slot represents a "line" or "area" in the UI where effects are shown
    /// </summary>
    public enum DisplaySlot
    {
        PRIMARY_CONDITION,   // Most critical condition (combat, critical health, elemental)
        HEALTH_STATUS,       // Physical wellbeing (hunger, fatigue, sickness)
        ACTIVITY_STATUS,     // Current activity (working, sleeping, etc.)
        SPECIAL_STATUS       // Medical treatment, positive effects
    }
    
    /// <summary>
    /// Get the category for a status effect type
    /// </summary>
    public static StatusCategory GetCategory(StatusEffectType effectType)
    {
        switch (effectType)
        {
            // Hunger (mutually exclusive within category)
            case StatusEffectType.HUNGRY:
            case StatusEffectType.STARVING:
                return StatusCategory.HEALTH_HUNGER;
            
            // Fatigue (mutually exclusive within category)
            case StatusEffectType.TIRED:
            case StatusEffectType.EXHAUSTED:
                return StatusCategory.HEALTH_FATIGUE;
            
            // Illness
            case StatusEffectType.SICK:
                return StatusCategory.HEALTH_ILLNESS;
            
            // Activities (mutually exclusive - can only do one thing at a time)
            case StatusEffectType.WORKING:
            case StatusEffectType.SLEEPING:
            case StatusEffectType.EATING:
                return StatusCategory.ACTIVITY;
            
            // Combat (mutually exclusive - can't fight and flee simultaneously)
            case StatusEffectType.FIGHTING:
            case StatusEffectType.FLEEING:
                return StatusCategory.COMBAT;
            
            // Fire elementals (can stack with other element types)
            case StatusEffectType.ON_FIRE:
            case StatusEffectType.BURNING:
                return StatusCategory.ELEMENTAL_FIRE;
            
            // Cold elementals (can stack with other element types, but conflicts with fire)
            case StatusEffectType.FROZEN:
                return StatusCategory.ELEMENTAL_COLD;
            
            // Electric elementals (can stack with other element types)
            case StatusEffectType.ELECTROCUTED:
                return StatusCategory.ELEMENTAL_ELECTRIC;
            
            // Medical
            case StatusEffectType.RECEIVING_MEDICAL_TREATMENT:
                return StatusCategory.MEDICAL;
            
            // Positive
            case StatusEffectType.HEALTHY:
                return StatusCategory.POSITIVE;
            
            default:
                Debug.LogWarning($"[StatusEffectUtils] Unknown category for effect: {effectType}");
                return StatusCategory.HEALTH_HUNGER;
        }
    }
    
    /// <summary>
    /// Get the display slot where this effect should be shown in UI
    /// </summary>
    public static DisplaySlot GetDisplaySlot(StatusEffectType effectType)
    {
        var category = GetCategory(effectType);
        
        switch (category)
        {
            // Primary conditions (combat and elementals are most urgent)
            case StatusCategory.COMBAT:
            case StatusCategory.ELEMENTAL_FIRE:
            case StatusCategory.ELEMENTAL_COLD:
            case StatusCategory.ELEMENTAL_ELECTRIC:
                return DisplaySlot.PRIMARY_CONDITION;
            
            // Health status (hunger, fatigue, illness)
            case StatusCategory.HEALTH_HUNGER:
            case StatusCategory.HEALTH_FATIGUE:
            case StatusCategory.HEALTH_ILLNESS:
                return DisplaySlot.HEALTH_STATUS;
            
            // Activity status
            case StatusCategory.ACTIVITY:
                return DisplaySlot.ACTIVITY_STATUS;
            
            // Special (medical, positive)
            case StatusCategory.MEDICAL:
            case StatusCategory.POSITIVE:
                return DisplaySlot.SPECIAL_STATUS;
            
            default:
                return DisplaySlot.HEALTH_STATUS;
        }
    }
    
    #endregion
    
    #region Priority System
    
    /// <summary>
    /// Priority values for status effects (higher = more important to display/process)
    /// Priority determines which effect is shown when multiple are active
    /// </summary>
    public static int GetPriority(StatusEffectType effectType)
    {
        switch (effectType)
        {
            // CRITICAL PRIORITY (100+) - Life-threatening or combat
            case StatusEffectType.ON_FIRE:
            case StatusEffectType.BURNING:
                return 110;
            
            case StatusEffectType.FIGHTING:
                return 105;
            
            case StatusEffectType.FLEEING:
                return 104;
            
            case StatusEffectType.FROZEN:
            case StatusEffectType.ELECTROCUTED:
                return 103;
            
            // HIGH PRIORITY (80-99) - Severe health conditions
            case StatusEffectType.STARVING:
                return 90;
            
            case StatusEffectType.SICK:
                return 85;
            
            case StatusEffectType.EXHAUSTED:
                return 82;
            
            // MEDIUM PRIORITY (50-79) - Moderate health conditions
            case StatusEffectType.HUNGRY:
                return 70;
            
            case StatusEffectType.TIRED:
                return 65;
            
            case StatusEffectType.RECEIVING_MEDICAL_TREATMENT:
                return 60;
            
            // LOW PRIORITY (20-49) - Activities (informational)
            case StatusEffectType.SLEEPING:
                return 40;
            
            case StatusEffectType.EATING:
                return 35;
            
            case StatusEffectType.WORKING:
                return 30;
            
            // BASELINE (0-19) - Positive/neutral states
            case StatusEffectType.HEALTHY:
                return 10;
            
            default:
                Debug.LogWarning($"[StatusEffectUtils] Unknown priority for effect: {effectType}");
                return 0;
        }
    }
    
    /// <summary>
    /// Get the highest priority status effect from a collection
    /// NOTE: Prefer using OrganizeForDisplay() for better multi-effect handling
    /// This returns a single "most important" effect (legacy support)
    /// </summary>
    public static StatusEffectType GetPrimaryEffect(IReadOnlyCollection<StatusEffectType> activeEffects)
    {
        if (activeEffects == null || activeEffects.Count == 0)
        {
            return StatusEffectType.HEALTHY;
        }
        
        // Use new slot system - primary condition slot has highest priority
        var organized = OrganizeForDisplay(activeEffects);
        
        // Return first primary condition if any (combat, elementals)
        if (organized.PrimaryConditions.Count > 0)
        {
            return organized.PrimaryConditions[0]; // Already sorted by priority
        }
        
        // Then health status
        if (organized.HealthStatus.Count > 0)
        {
            return organized.HealthStatus[0];
        }
        
        // Then activity
        if (organized.ActivityStatus.Count > 0)
        {
            return organized.ActivityStatus[0];
        }
        
        // Finally special status
        if (organized.SpecialStatus.Count > 0)
        {
            return organized.SpecialStatus[0];
        }
        
        return StatusEffectType.HEALTHY;
    }
    
    /// <summary>
    /// Get all effects sorted by priority (highest first)
    /// </summary>
    public static List<StatusEffectType> GetEffectsByPriority(IReadOnlyCollection<StatusEffectType> activeEffects)
    {
        if (activeEffects == null || activeEffects.Count == 0)
        {
            return new List<StatusEffectType>();
        }
        
        return activeEffects.OrderByDescending(effect => GetPriority(effect)).ToList();
    }
    
    #endregion
    
    #region Slot-Based Display System
    
    /// <summary>
    /// Result class for organized display of status effects
    /// </summary>
    public class DisplayOrganization
    {
        public List<StatusEffectType> PrimaryConditions = new List<StatusEffectType>(); // Combat, elementals
        public List<StatusEffectType> HealthStatus = new List<StatusEffectType>();      // Hunger, fatigue, sickness
        public List<StatusEffectType> ActivityStatus = new List<StatusEffectType>();    // Working, sleeping
        public List<StatusEffectType> SpecialStatus = new List<StatusEffectType>();     // Medical, healthy
        
        public bool HasAnyEffects()
        {
            return PrimaryConditions.Count > 0 || HealthStatus.Count > 0 || 
                   ActivityStatus.Count > 0 || SpecialStatus.Count > 0;
        }
    }
    
    /// <summary>
    /// Organize status effects by display slot for UI presentation
    /// This handles the smart filtering and organization for multi-effect display
    /// </summary>
    public static DisplayOrganization OrganizeForDisplay(IReadOnlyCollection<StatusEffectType> activeEffects)
    {
        var result = new DisplayOrganization();
        
        if (activeEffects == null || activeEffects.Count == 0)
        {
            result.SpecialStatus.Add(StatusEffectType.HEALTHY);
            return result;
        }
        
        // Group effects by category first
        var byCategory = new Dictionary<StatusCategory, List<StatusEffectType>>();
        foreach (var effect in activeEffects)
        {
            var category = GetCategory(effect);
            if (!byCategory.ContainsKey(category))
            {
                byCategory[category] = new List<StatusEffectType>();
            }
            byCategory[category].Add(effect);
        }
        
        // PRIMARY CONDITION SLOT - All critical conditions (can show multiple)
        AddToPrimaryConditions(result, byCategory);
        
        // HEALTH STATUS SLOT - Show most severe from each health category
        AddToHealthStatus(result, byCategory);
        
        // ACTIVITY STATUS SLOT - Show current activity (only one)
        AddToActivityStatus(result, byCategory);
        
        // SPECIAL STATUS SLOT - Medical or healthy
        AddToSpecialStatus(result, byCategory, activeEffects);
        
        return result;
    }
    
    private static void AddToPrimaryConditions(DisplayOrganization result, Dictionary<StatusCategory, List<StatusEffectType>> byCategory)
    {
        // Combat (pick highest priority if multiple)
        if (byCategory.ContainsKey(StatusCategory.COMBAT))
        {
            var combatEffect = byCategory[StatusCategory.COMBAT]
                .OrderByDescending(e => GetPriority(e))
                .First();
            result.PrimaryConditions.Add(combatEffect);
        }
        
        // Elementals (can have multiple - show all active elemental types)
        if (byCategory.ContainsKey(StatusCategory.ELEMENTAL_FIRE))
        {
            // Pick highest priority fire effect
            result.PrimaryConditions.Add(byCategory[StatusCategory.ELEMENTAL_FIRE]
                .OrderByDescending(e => GetPriority(e)).First());
        }
        
        if (byCategory.ContainsKey(StatusCategory.ELEMENTAL_COLD))
        {
            result.PrimaryConditions.Add(byCategory[StatusCategory.ELEMENTAL_COLD]
                .OrderByDescending(e => GetPriority(e)).First());
        }
        
        if (byCategory.ContainsKey(StatusCategory.ELEMENTAL_ELECTRIC))
        {
            result.PrimaryConditions.Add(byCategory[StatusCategory.ELEMENTAL_ELECTRIC]
                .OrderByDescending(e => GetPriority(e)).First());
        }
        
        // Sort by priority
        result.PrimaryConditions = result.PrimaryConditions
            .OrderByDescending(e => GetPriority(e))
            .ToList();
    }
    
    private static void AddToHealthStatus(DisplayOrganization result, Dictionary<StatusCategory, List<StatusEffectType>> byCategory)
    {
        // Hunger - show most severe (STARVING over HUNGRY)
        if (byCategory.ContainsKey(StatusCategory.HEALTH_HUNGER))
        {
            var hungerEffect = byCategory[StatusCategory.HEALTH_HUNGER]
                .OrderByDescending(e => GetPriority(e))
                .First();
            result.HealthStatus.Add(hungerEffect);
        }
        
        // Fatigue - show most severe (EXHAUSTED over TIRED)
        if (byCategory.ContainsKey(StatusCategory.HEALTH_FATIGUE))
        {
            var fatigueEffect = byCategory[StatusCategory.HEALTH_FATIGUE]
                .OrderByDescending(e => GetPriority(e))
                .First();
            result.HealthStatus.Add(fatigueEffect);
        }
        
        // Illness - always show if present
        if (byCategory.ContainsKey(StatusCategory.HEALTH_ILLNESS))
        {
            result.HealthStatus.AddRange(byCategory[StatusCategory.HEALTH_ILLNESS]);
        }
        
        // Sort by priority
        result.HealthStatus = result.HealthStatus
            .OrderByDescending(e => GetPriority(e))
            .ToList();
    }
    
    private static void AddToActivityStatus(DisplayOrganization result, Dictionary<StatusCategory, List<StatusEffectType>> byCategory)
    {
        // Activity - only show if no critical conditions
        if (byCategory.ContainsKey(StatusCategory.ACTIVITY))
        {
            // Show activity unless there's a critical condition (priority >= 100)
            bool hasCritical = result.PrimaryConditions.Any(e => GetPriority(e) >= 100);
            
            if (!hasCritical)
            {
                // Pick highest priority activity
                var activity = byCategory[StatusCategory.ACTIVITY]
                    .OrderByDescending(e => GetPriority(e))
                    .First();
                result.ActivityStatus.Add(activity);
            }
        }
    }
    
    private static void AddToSpecialStatus(DisplayOrganization result, Dictionary<StatusCategory, List<StatusEffectType>> byCategory, IReadOnlyCollection<StatusEffectType> activeEffects)
    {
        // Medical treatment
        if (byCategory.ContainsKey(StatusCategory.MEDICAL))
        {
            result.SpecialStatus.AddRange(byCategory[StatusCategory.MEDICAL]);
        }
        
        // Healthy - only show if no negative health conditions
        bool hasNegativeHealth = byCategory.ContainsKey(StatusCategory.HEALTH_HUNGER) ||
                                 byCategory.ContainsKey(StatusCategory.HEALTH_FATIGUE) ||
                                 byCategory.ContainsKey(StatusCategory.HEALTH_ILLNESS);
        
        if (!hasNegativeHealth && activeEffects.Contains(StatusEffectType.HEALTHY))
        {
            result.SpecialStatus.Add(StatusEffectType.HEALTHY);
        }
    }
    
    /// <summary>
    /// Get a flattened list of displayable effects (for simple UI that can't handle slots)
    /// Returns effects that can meaningfully coexist, with smart filtering
    /// </summary>
    public static List<StatusEffectType> GetDisplayableEffects(IReadOnlyCollection<StatusEffectType> activeEffects)
    {
        var organized = OrganizeForDisplay(activeEffects);
        
        var result = new List<StatusEffectType>();
        result.AddRange(organized.PrimaryConditions);
        result.AddRange(organized.HealthStatus);
        result.AddRange(organized.ActivityStatus);
        result.AddRange(organized.SpecialStatus);
        
        return result;
    }
    
    #endregion
    
    #region Effect Filtering and Grouping
    
    /// <summary>
    /// Get all effects in a specific category
    /// </summary>
    public static List<StatusEffectType> GetEffectsInCategory(
        IReadOnlyCollection<StatusEffectType> activeEffects, 
        StatusCategory category)
    {
        if (activeEffects == null || activeEffects.Count == 0)
        {
            return new List<StatusEffectType>();
        }
        
        return activeEffects.Where(effect => GetCategory(effect) == category).ToList();
    }
    
    /// <summary>
    /// Check if the target has any negative health conditions
    /// Uses new category system to detect hunger, fatigue, or illness
    /// </summary>
    public static bool HasNegativeHealthCondition(IReadOnlyCollection<StatusEffectType> activeEffects)
    {
        if (activeEffects == null || activeEffects.Count == 0)
        {
            return false;
        }
        
        return activeEffects.Any(effect =>
        {
            var cat = GetCategory(effect);
            return cat == StatusCategory.HEALTH_HUNGER ||
                   cat == StatusCategory.HEALTH_FATIGUE ||
                   cat == StatusCategory.HEALTH_ILLNESS;
        });
    }
    
    /// <summary>
    /// Check if the target has any critical conditions (life-threatening)
    /// </summary>
    public static bool HasCriticalCondition(IReadOnlyCollection<StatusEffectType> activeEffects)
    {
        if (activeEffects == null || activeEffects.Count == 0)
        {
            return false;
        }
        
        return activeEffects.Any(effect => GetPriority(effect) >= 100);
    }
    
    #endregion
    
    #region Compatibility and Conflicts
    
    /// <summary>
    /// Check if two status effects are mutually exclusive (can't exist together)
    /// Uses the new category system for intelligent conflict detection
    /// </summary>
    public static bool AreEffectsConflicting(StatusEffectType effect1, StatusEffectType effect2)
    {
        if (effect1 == effect2) return false; // Same effect is not a conflict
        
        var cat1 = GetCategory(effect1);
        var cat2 = GetCategory(effect2);
        
        // Effects in the same mutually-exclusive category conflict
        // (e.g., HUNGRY and STARVING, TIRED and EXHAUSTED, FIGHTING and FLEEING)
        if (cat1 == cat2)
        {
            switch (cat1)
            {
                case StatusCategory.HEALTH_HUNGER:
                case StatusCategory.HEALTH_FATIGUE:
                case StatusCategory.ACTIVITY:
                case StatusCategory.COMBAT:
                    return true; // These categories allow only one effect at a time
                    
                case StatusCategory.ELEMENTAL_FIRE:
                case StatusCategory.ELEMENTAL_COLD:
                case StatusCategory.ELEMENTAL_ELECTRIC:
                    // Multiple effects within same elemental type can coexist (e.g., ON_FIRE + BURNING)
                    return false;
                    
                default:
                    return false;
            }
        }
        
        // Cross-category conflicts
        // FROZEN (cold) conflicts with ON_FIRE/BURNING (fire)
        if ((cat1 == StatusCategory.ELEMENTAL_COLD && cat2 == StatusCategory.ELEMENTAL_FIRE) ||
            (cat1 == StatusCategory.ELEMENTAL_FIRE && cat2 == StatusCategory.ELEMENTAL_COLD))
        {
            return true;
        }
        
        // SLEEPING conflicts with combat activities
        if (cat1 == StatusCategory.ACTIVITY && effect1 == StatusEffectType.SLEEPING && cat2 == StatusCategory.COMBAT)
        {
            return true;
        }
        if (cat2 == StatusCategory.ACTIVITY && effect2 == StatusEffectType.SLEEPING && cat1 == StatusCategory.COMBAT)
        {
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Get effects that should be removed when a new effect is applied (due to conflicts)
    /// </summary>
    public static List<StatusEffectType> GetConflictingEffects(
        StatusEffectType newEffect, 
        IReadOnlyCollection<StatusEffectType> activeEffects)
    {
        if (activeEffects == null || activeEffects.Count == 0)
        {
            return new List<StatusEffectType>();
        }
        
        return activeEffects.Where(existing => AreEffectsConflicting(newEffect, existing)).ToList();
    }
    
    #endregion
    
    #region Health Status Conversion
    
    /// <summary>
    /// Convert status effects to a simple HealthStatus enum for UI
    /// Uses priority system to determine the most important status
    /// </summary>
    public static HealthStatus GetHealthStatus(IReadOnlyCollection<StatusEffectType> activeEffects)
    {
        if (activeEffects == null || activeEffects.Count == 0)
        {
            return HealthStatus.Healthy;
        }
        
        // Get primary (highest priority) effect
        var primaryEffect = GetPrimaryEffect(activeEffects);
        
        // Map to HealthStatus
        switch (primaryEffect)
        {
            case StatusEffectType.SICK:
                return HealthStatus.Sick;
            
            case StatusEffectType.STARVING:
                return HealthStatus.Starving;
            
            case StatusEffectType.HUNGRY:
                return HealthStatus.Hungry;
            
            case StatusEffectType.EXHAUSTED:
                return HealthStatus.Exhausted;
            
            case StatusEffectType.TIRED:
                return HealthStatus.Tired;
            
            default:
                return HealthStatus.Healthy;
        }
    }
    
    #endregion
    
    #region Debug and Display Helpers
    
    /// <summary>
    /// Get a human-readable description of a status effect
    /// </summary>
    public static string GetEffectDescription(StatusEffectType effectType)
    {
        switch (effectType)
        {
            case StatusEffectType.HUNGRY:
                return "Hungry";
            case StatusEffectType.STARVING:
                return "Starving";
            case StatusEffectType.TIRED:
                return "Tired";
            case StatusEffectType.EXHAUSTED:
                return "Exhausted";
            case StatusEffectType.SICK:
                return "Sick";
            case StatusEffectType.ON_FIRE:
            case StatusEffectType.BURNING:
                return "On Fire";
            case StatusEffectType.FROZEN:
                return "Frozen";
            case StatusEffectType.ELECTROCUTED:
                return "Electrocuted";
            case StatusEffectType.WORKING:
                return "Working";
            case StatusEffectType.SLEEPING:
                return "Sleeping";
            case StatusEffectType.EATING:
                return "Eating";
            case StatusEffectType.FIGHTING:
                return "Fighting";
            case StatusEffectType.FLEEING:
                return "Fleeing";
            case StatusEffectType.RECEIVING_MEDICAL_TREATMENT:
                return "Receiving Treatment";
            case StatusEffectType.HEALTHY:
                return "Healthy";
            default:
                return effectType.ToString();
        }
    }
    
    /// <summary>
    /// Get a color for displaying a status effect in UI
    /// </summary>
    public static Color GetEffectColor(StatusEffectType effectType)
    {
        var category = GetCategory(effectType);
        var priority = GetPriority(effectType);
        
        // Critical effects = red
        if (priority >= 100)
        {
            return new Color(1f, 0.2f, 0.2f); // Bright red
        }
        
        switch (category)
        {
            // Health conditions (hunger, fatigue, illness)
            case StatusCategory.HEALTH_HUNGER:
            case StatusCategory.HEALTH_FATIGUE:
            case StatusCategory.HEALTH_ILLNESS:
                if (priority >= 80) return new Color(1f, 0.3f, 0f); // Orange (severe)
                if (priority >= 60) return new Color(1f, 0.8f, 0f); // Yellow (moderate)
                return new Color(0.8f, 0.8f, 0f); // Dim yellow
            
            case StatusCategory.ACTIVITY:
                return new Color(0.5f, 0.8f, 1f); // Light blue
            
            case StatusCategory.COMBAT:
                return new Color(1f, 0.4f, 0f); // Red-orange
            
            // Elemental effects (all types - fire, cold, electric)
            case StatusCategory.ELEMENTAL_FIRE:
                return new Color(1f, 0.4f, 0f); // Orange-red (fire)
            
            case StatusCategory.ELEMENTAL_COLD:
                return new Color(0.4f, 0.8f, 1f); // Cyan (cold)
            
            case StatusCategory.ELEMENTAL_ELECTRIC:
                return new Color(1f, 1f, 0.2f); // Yellow (electric)
            
            case StatusCategory.MEDICAL:
                return new Color(0f, 1f, 0.5f); // Green-cyan
            
            case StatusCategory.POSITIVE:
                return new Color(0.2f, 1f, 0.2f); // Bright green
            
            default:
                return Color.white;
        }
    }
    
    #endregion
}

