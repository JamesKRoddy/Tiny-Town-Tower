using UnityEngine;
using Combat;
using Managers;
using System.Linq;

/// <summary>
/// Utility class for handling damage-related calculations and animations
/// Reduces code duplication between different character types
/// Uses 2D blend trees for natural and flexible animation blending
/// </summary>
public static class DamageUtils
{
    /// <summary>
    /// Calculates the hit direction as a Vector2 for 2D blend tree animation
    /// Returns a normalized 2D vector where:
    /// X = left/right component (-1 = left, 1 = right)
    /// Y = forward/backward component (-1 = back, 1 = front)
    /// This allows for smooth blending between any hit direction
    /// </summary>
    /// <param name="characterTransform">Transform of the character taking damage</param>
    /// <param name="damageSource">Transform of the damage source, null if unknown</param>
    /// <returns>Vector2 for 2D blend tree control</returns>
    public static Vector2 CalculateHitDirection(Transform characterTransform, Transform damageSource)
    {
        if (damageSource == null)
        {
            // Default to front hit if source is unknown
            return Vector2.up; // (0, 1) = front
        }

        // Calculate direction from damage source to this character
        Vector3 damageDirection = (characterTransform.position - damageSource.position).normalized;
        
        // Get the character's forward and right directions
        Vector3 characterForward = characterTransform.forward;
        Vector3 characterRight = characterTransform.right;
        
        // Calculate dot products for both forward and right components
        float forwardComponent = Vector3.Dot(damageDirection, characterForward);  // Y component
        float rightComponent = Vector3.Dot(damageDirection, characterRight);      // X component
        
        // Return as Vector2 for 2D blend tree
        // X = right/left (-1 to 1), Y = forward/back (-1 to 1)
        return new Vector2(rightComponent, forwardComponent);
    }

    /// <summary>
    /// Triggers the damaged animation with 2D direction parameters for blend tree control
    /// </summary>
    /// <param name="animator">The character's animator component</param>
    /// <param name="hitDirection">2D direction vector from CalculateHitDirection</param>
    public static void TriggerDamagedAnimation(Animator animator, Vector2 hitDirection)
    {
        if (animator == null) return;

        // Set the 2D hit direction parameters for the blend tree
        animator.SetFloat(GameConstants.AnimatorParams.HitDirectionXHash, hitDirection.x);  // Left/Right component
        animator.SetFloat(GameConstants.AnimatorParams.HitDirectionYHash, hitDirection.y);  // Forward/Back component
        
        // Trigger the damaged animation
        animator.SetTrigger(GameConstants.AnimatorParams.DamagedHash);
    }

    /// <summary>
    /// Triggers the knockback animation with 2D direction parameters for blend tree control
    /// </summary>
    /// <param name="animator">The character's animator component</param>
    /// <param name="hitDirection">2D direction vector from CalculateHitDirection</param>
    public static void TriggerKnockbackAnimation(Animator animator, Vector2 hitDirection)
    {
        if (animator == null) return;

        // Set the 2D hit direction parameters for the blend tree
        animator.SetFloat(GameConstants.AnimatorParams.HitDirectionXHash, hitDirection.x);  // Left/Right component
        animator.SetFloat(GameConstants.AnimatorParams.HitDirectionYHash, hitDirection.y);  // Forward/Back component
        
        // Trigger the knockback animation
        animator.SetTrigger(GameConstants.AnimatorParams.KnockbackHash);
    }

    /// <summary>
    /// Applies poise damage to a character and checks if poise is broken
    /// </summary>
    /// <param name="character">The character taking poise damage</param>
    /// <param name="poiseDamage">Amount of poise damage to apply</param>
    /// <param name="onPoiseBroken">Callback for when poise is broken</param>
    /// <returns>True if poise was broken (reached 0 or below)</returns>
    public static bool ApplyPoiseDamage(IDamageable character, float poiseDamage, System.Action<float, float> onPoiseBroken = null)
    {
        if (character == null) return false;

        float previousPoise = character.Poise;
        character.Poise = Mathf.Max(0, character.Poise - poiseDamage);
        
        bool poiseBroken = previousPoise > 0 && character.Poise <= 0;
        
        if (poiseBroken)
        {
            onPoiseBroken?.Invoke(poiseDamage, character.Poise);
        }
        
        return poiseBroken;
    }

    /// <summary>
    /// Restores poise to a character (useful for recovery over time)
    /// </summary>
    /// <param name="character">The character to restore poise to</param>
    /// <param name="amount">Amount of poise to restore</param>
    public static void RestorePoise(IDamageable character, float amount)
    {
        if (character == null) return;
        
        character.Poise = Mathf.Min(character.MaxPoise, character.Poise + amount);
    }

    /// <summary>
    /// Calculates hit point and normal for VFX effects
    /// </summary>
    /// <param name="characterTransform">Transform of the character taking damage</param>
    /// <param name="damageSource">Transform of the damage source</param>
    /// <param name="hitHeightOffset">Height offset for the hit point (default: 1.5f)</param>
    /// <returns>Tuple containing hit point and hit normal</returns>
    public static (Vector3 hitPoint, Vector3 hitNormal) CalculateHitPointAndNormal(
        Transform characterTransform, 
        Transform damageSource, 
        float hitHeightOffset = 1.5f)
    {
        Vector3 hitPoint = characterTransform.position + Vector3.up * hitHeightOffset;
        
        Vector3 hitNormal = damageSource != null 
            ? (characterTransform.position - damageSource.position).normalized 
            : Vector3.up; // Use upward direction as fallback
            
        return (hitPoint, hitNormal);
    }

    /// <summary>
    /// Applies damage to a character with automatic 2D direction detection
    /// </summary>
    /// <param name="character">The character taking damage</param>
    /// <param name="amount">Amount of damage to take</param>
    /// <param name="damageSource">Transform of the damage source</param>
    /// <param name="animator">The character's animator component</param>
    /// <param name="characterTransform">Transform of the character taking damage</param>
    /// <param name="onDamageTaken">Callback for when damage is taken</param>
    /// <param name="onDeath">Callback for when character dies</param>
    /// <param name="playHitEffect">Whether to play hit VFX (default: true)</param>
    /// <returns>The calculated 2D hit direction vector</returns>
    public static Vector2 ApplyDamage(
        IDamageable character,
        float amount,
        Transform damageSource,
        Animator animator,
        Transform characterTransform,
        System.Action<float, float> onDamageTaken = null,
        System.Action onDeath = null,
        bool playHitEffect = true)
    {
        // Calculate 2D hit direction
        Vector2 hitDirection = CalculateHitDirection(characterTransform, damageSource);
        
        // Trigger animation with 2D direction parameters
        TriggerDamagedAnimation(animator, hitDirection);
        
        // Play hit VFX if requested
        if (playHitEffect && damageSource != null)
        {
            var (hitPoint, hitNormal) = CalculateHitPointAndNormal(characterTransform, damageSource);
            EffectManager.Instance.PlayHitEffect(hitPoint, hitNormal, character);
        }
        
        return hitDirection;
    }

    /// <summary>
    /// Applies damage with poise damage to a character with automatic 2D direction detection
    /// </summary>
    /// <param name="character">The character taking damage</param>
    /// <param name="amount">Amount of damage to take</param>
    /// <param name="poiseDamage">Amount of poise damage to take</param>
    /// <param name="damageSource">Transform of the damage source</param>
    /// <param name="animator">The character's animator component</param>
    /// <param name="characterTransform">Transform of the character taking damage</param>
    /// <param name="onDamageTaken">Callback for when damage is taken</param>
    /// <param name="onPoiseBroken">Callback for when poise is broken</param>
    /// <param name="onDeath">Callback for when character dies</param>
    /// <param name="playHitEffect">Whether to play hit VFX (default: true)</param>
    /// <returns>The calculated 2D hit direction vector and whether poise was broken</returns>
    public static (Vector2 hitDirection, bool poiseBroken) ApplyDamageWithPoise(
        IDamageable character,
        float amount,
        float poiseDamage,
        Transform damageSource,
        Animator animator,
        Transform characterTransform,
        System.Action<float, float> onDamageTaken = null,
        System.Action<float, float> onPoiseBroken = null,
        System.Action onDeath = null,
        bool playHitEffect = true)
    {
        // Calculate 2D hit direction
        Vector2 hitDirection = CalculateHitDirection(characterTransform, damageSource);
        
        // Apply health damage
        float previousHealth = character.Health;
        character.Health -= amount;
        
        // Clamp health to 0
        if (character.Health < 0)
        {
            character.Health = 0;
        }
        
        // Invoke damage taken callback
        onDamageTaken?.Invoke(amount, character.Health);
        
        // Apply poise damage and check if poise is broken
        bool poiseBroken = ApplyPoiseDamage(character, poiseDamage, onPoiseBroken);
        
        // Trigger appropriate animation based on poise state
        if (poiseBroken)
        {
            TriggerKnockbackAnimation(animator, hitDirection);
        }
        else
        {
            TriggerDamagedAnimation(animator, hitDirection);
        }
        
        // Play hit VFX if requested
        if (playHitEffect && damageSource != null)
        {
            var (hitPoint, hitNormal) = CalculateHitPointAndNormal(characterTransform, damageSource);
            EffectManager.Instance.PlayHitEffect(hitPoint, hitNormal, character);
        }
        
        // Check for death
        if (character.Health <= 0)
        {
            onDeath?.Invoke();
        }
        
        return (hitDirection, poiseBroken);
    }

    /// <summary>
    /// Applies damage to a character with explicit 2D direction specification
    /// </summary>
    /// <param name="character">The character taking damage</param>
    /// <param name="amount">Amount of damage to take</param>
    /// <param name="hitDirection">Explicit 2D hit direction vector</param>
    /// <param name="damageSource">Transform of the damage source (optional, for VFX)</param>
    /// <param name="animator">The character's animator component</param>
    /// <param name="characterTransform">Transform of the character taking damage</param>
    /// <param name="onDamageTaken">Callback for when damage is taken</param>
    /// <param name="onDeath">Callback for when character dies</param>
    /// <param name="playHitEffect">Whether to play hit VFX (default: true)</param>
    public static void ApplyDamageWithDirection(
        IDamageable character,
        float amount,
        Vector2 hitDirection,
        Transform damageSource,
        Animator animator,
        Transform characterTransform,
        System.Action<float, float> onDamageTaken = null,
        System.Action onDeath = null,
        bool playHitEffect = true)
    {
        // Use the explicitly specified 2D hit direction
        TriggerDamagedAnimation(animator, hitDirection);
        
        // Play hit VFX if requested
        if (playHitEffect && damageSource != null)
        {
            var (hitPoint, hitNormal) = CalculateHitPointAndNormal(characterTransform, damageSource);
            EffectManager.Instance.PlayHitEffect(hitPoint, hitNormal, character);
        }
    }

    /// <summary>
    /// Calculates damage multiplier based on character's resistance to a damage type
    /// </summary>
    /// <param name="resistance">The character's resistance level</param>
    /// <returns>Damage multiplier (0.0 to 2.0)</returns>
    public static float GetDamageMultiplier(DamageResistance resistance)
    {
        switch (resistance)
        {
            case DamageResistance.IMMUNE:
                return 0.0f;
            case DamageResistance.RESISTANT:
                return 0.5f;
            case DamageResistance.NORMAL:
                return 1.0f;
            case DamageResistance.WEAK:
                return 1.5f;
            case DamageResistance.VULNERABLE:
                return 2.0f;
            default:
                return 1.0f;
        }
    }

    /// <summary>
    /// Applies elemental damage to a character with automatic resistance calculation
    /// </summary>
    /// <param name="character">The character taking damage</param>
    /// <param name="amount">Base amount of damage to take</param>
    /// <param name="damageType">Type of elemental damage</param>
    /// <param name="damageSource">Transform of the damage source</param>
    /// <param name="animator">The character's animator component</param>
    /// <param name="characterTransform">Transform of the character taking damage</param>
    /// <param name="onDamageTaken">Callback for when damage is taken</param>
    /// <param name="onDeath">Callback for when character dies</param>
    /// <param name="playHitEffect">Whether to play hit VFX (default: true)</param>
    /// <returns>The calculated 2D hit direction vector and final damage amount</returns>
    public static (Vector2 hitDirection, float finalDamage) ApplyElementalDamage(
        IDamageable character,
        float amount,
        AttackElement damageType,
        Transform damageSource,
        Animator animator,
        Transform characterTransform,
        System.Action<float, float> onDamageTaken = null,
        System.Action onDeath = null,
        bool playHitEffect = true)
    {
        // Get character's damage multiplier for this damage type
        float damageMultiplier = character.GetDamageMultiplier(damageType);
        float finalDamage = amount * damageMultiplier;

        // Skip damage if immune (multiplier is 0)
        if (damageMultiplier <= 0f)
        {
            return (Vector2.zero, 0f);
        }

        // Calculate 2D hit direction
        Vector2 hitDirection = CalculateHitDirection(characterTransform, damageSource);
        
        // CRITICAL: Actually apply the health damage!
        float healthBefore = character.Health;
        character.Health = Mathf.Max(0, character.Health - finalDamage);
        float actualDamage = healthBefore - character.Health;
        
        Debug.Log($"[DamageUtils] ApplyElementalDamage - Health: {healthBefore:F1} -> {character.Health:F1} (damage: {actualDamage:F1})");
        
        // Invoke damage taken callback
        onDamageTaken?.Invoke(actualDamage, character.Health);
        
        // Set damage type parameter for animations
        TriggerElementalDamagedAnimation(animator, hitDirection, damageType);
        
        // Play elemental hit VFX if requested
        if (playHitEffect && damageSource != null)
        {
            var (hitPoint, hitNormal) = CalculateHitPointAndNormal(characterTransform, damageSource);
            EffectManager.Instance.PlayElementalHitEffect(hitPoint, hitNormal, character, damageType);
        }
        
        // Check for death
        if (character.Health <= 0)
        {
            onDeath?.Invoke();
        }
        
        return (hitDirection, finalDamage);
    }

    /// <summary>
    /// Applies elemental damage with poise damage to a character with automatic resistance calculation
    /// </summary>
    /// <param name="character">The character taking damage</param>
    /// <param name="amount">Base amount of damage to take</param>
    /// <param name="poiseDamage">Amount of poise damage to take</param>
    /// <param name="damageType">Type of elemental damage</param>
    /// <param name="damageSource">Transform of the damage source</param>
    /// <param name="animator">The character's animator component</param>
    /// <param name="characterTransform">Transform of the character taking damage</param>
    /// <param name="onDamageTaken">Callback for when damage is taken</param>
    /// <param name="onPoiseBroken">Callback for when poise is broken</param>
    /// <param name="onDeath">Callback for when character dies</param>
    /// <param name="playHitEffect">Whether to play hit VFX (default: true)</param>
    /// <returns>The calculated 2D hit direction vector, final damage amount, and whether poise was broken</returns>
    public static (Vector2 hitDirection, float finalDamage, bool poiseBroken) ApplyElementalDamageWithPoise(
        IDamageable character,
        float amount,
        float poiseDamage,
        AttackElement damageType,
        Transform damageSource,
        Animator animator,
        Transform characterTransform,
        System.Action<float, float> onDamageTaken = null,
        System.Action<float, float> onPoiseBroken = null,
        System.Action onDeath = null,
        bool playHitEffect = true)
    {
        // Get character's damage multiplier for this damage type
        float damageMultiplier = character.GetDamageMultiplier(damageType);
        float finalDamage = amount * damageMultiplier;

        // Skip damage if immune (multiplier is 0)
        if (damageMultiplier <= 0f)
        {
            return (Vector2.zero, 0f, false);
        }

        // Calculate 2D hit direction
        Vector2 hitDirection = CalculateHitDirection(characterTransform, damageSource);
        
        // CRITICAL: Actually apply the health damage!
        float healthBefore = character.Health;
        character.Health = Mathf.Max(0, character.Health - finalDamage);
        float actualDamage = healthBefore - character.Health;
        
        Debug.Log($"[DamageUtils] ApplyElementalDamageWithPoise - Health: {healthBefore:F1} -> {character.Health:F1} (damage: {actualDamage:F1})");
        
        // Invoke damage taken callback
        onDamageTaken?.Invoke(actualDamage, character.Health);
        
        // Apply poise damage and check if poise is broken
        bool poiseBroken = ApplyPoiseDamage(character, poiseDamage, onPoiseBroken);
        
        // Trigger appropriate animation based on poise state and damage type
        if (poiseBroken)
        {
            TriggerElementalKnockbackAnimation(animator, hitDirection, damageType);
        }
        else
        {
            TriggerElementalDamagedAnimation(animator, hitDirection, damageType);
        }
        
        // Play elemental hit VFX if requested
        if (playHitEffect && damageSource != null)
        {
            var (hitPoint, hitNormal) = CalculateHitPointAndNormal(characterTransform, damageSource);
            EffectManager.Instance.PlayElementalHitEffect(hitPoint, hitNormal, character, damageType);
        }
        
        // Check for death
        if (character.Health <= 0)
        {
            onDeath?.Invoke();
        }
        
        return (hitDirection, finalDamage, poiseBroken);
    }

    /// <summary>
    /// Triggers the elemental damaged animation with 2D direction and damage type parameters
    /// </summary>
    /// <param name="animator">The character's animator component</param>
    /// <param name="hitDirection">2D direction vector from CalculateHitDirection</param>
    /// <param name="damageType">Type of elemental damage</param>
    public static void TriggerElementalDamagedAnimation(Animator animator, Vector2 hitDirection, AttackElement damageType)
    {
        if (animator == null) return;

        // Set the 2D hit direction parameters for the blend tree
        animator.SetFloat(GameConstants.AnimatorParams.HitDirectionXHash, hitDirection.x);  // Left/Right component
        animator.SetFloat(GameConstants.AnimatorParams.HitDirectionYHash, hitDirection.y);  // Forward/Back component
        
        // Set the damage type parameter for elemental animations
        animator.SetInteger(GameConstants.AnimatorParams.DamageTypeHash, (int)damageType);
        
        // Trigger the damaged animation
        animator.SetTrigger(GameConstants.AnimatorParams.DamagedHash);
    }

    /// <summary>
    /// Triggers the elemental knockback animation with 2D direction and damage type parameters
    /// </summary>
    /// <param name="animator">The character's animator component</param>
    /// <param name="hitDirection">2D direction vector from CalculateHitDirection</param>
    /// <param name="damageType">Type of elemental damage</param>
    public static void TriggerElementalKnockbackAnimation(Animator animator, Vector2 hitDirection, AttackElement damageType)
    {
        if (animator == null) return;

        // Set the 2D hit direction parameters for the blend tree
        animator.SetFloat(GameConstants.AnimatorParams.HitDirectionXHash, hitDirection.x);  // Left/Right component
        animator.SetFloat(GameConstants.AnimatorParams.HitDirectionYHash, hitDirection.y);  // Forward/Back component
        
        // Set the damage type parameter for elemental animations
        animator.SetInteger(GameConstants.AnimatorParams.DamageTypeHash, (int)damageType);
        
        // Trigger the knockback animation
        animator.SetTrigger(GameConstants.AnimatorParams.KnockbackHash);
    }

    // ===== AREA DAMAGE UTILITIES =====

    /// <summary>
    /// Deal damage to all targets in a radius
    /// </summary>
    /// <param name="center">Center position of the damage area</param>
    /// <param name="radius">Radius of the damage area</param>
    /// <param name="damageAmount">Amount of damage to deal</param>
    /// <param name="poiseDamage">Amount of poise damage to deal</param>
    /// <param name="attacker">The attacker (for damage source tracking)</param>
    /// <param name="element">Elemental type of the damage</param>
    /// <param name="layerMask">Layer mask for valid targets (-1 for all layers)</param>
    /// <param name="excludeSelf">Whether to exclude the attacker from damage</param>
    /// <returns>Number of targets damaged</returns>
    public static int DealDamageInRadius(Vector3 center, float radius, float damageAmount, float poiseDamage, 
        Transform attacker, AttackElement element = AttackElement.PHYSICAL, LayerMask layerMask = default(LayerMask), bool excludeSelf = false)
    {
        int targetsDamaged = 0;
        
        Debug.Log($"[DamageUtils] DealDamageInRadius called - Center: {center}, Radius: {radius}, Damage: {damageAmount}, Poise: {poiseDamage}");
        
        // Find all colliders in the radius
        Collider[] hitColliders = Physics.OverlapSphere(center, radius, layerMask);
        Debug.Log($"[DamageUtils] Found {hitColliders.Length} colliders in radius");
        
        foreach (var hitCollider in hitColliders)
        {
            IDamageable damageable = hitCollider.GetComponent<IDamageable>();
            
            if (damageable == null)
            {
                Debug.Log($"[DamageUtils] Skipping {hitCollider.name} - no IDamageable component");
                continue;
            }
            
            Allegiance targetAllegiance = damageable.GetAllegiance();
            Debug.Log($"[DamageUtils] Found damageable: {hitCollider.name}, Allegiance: {targetAllegiance}");
            
            // Only skip NEUTRAL entities (protected quest NPCs, invulnerable objects)
            if (targetAllegiance == Allegiance.NEUTRAL)
            {
                Debug.Log($"[DamageUtils] Skipping {hitCollider.name} - NEUTRAL allegiance (protected)");
                continue;
            }
            
            // Check if the target is still active (this will catch NPCs in bunkers)
            if (!hitCollider.gameObject.activeInHierarchy)
            {
                Debug.Log($"[DamageUtils] Skipping {hitCollider.name} - not active in hierarchy");
                continue;
            }
            
            // Exclude self if requested
            if (excludeSelf && hitCollider.transform == attacker)
            {
                Debug.Log($"[DamageUtils] Skipping {hitCollider.name} - is attacker (excludeSelf)");
                continue;
            }
            
            // Apply damage with elemental type (using DamageInfo)
            Debug.Log($"[DamageUtils] Calling TakeDamage on {hitCollider.name} - Damage: {damageAmount}, Poise: {poiseDamage}, Element: {element}");
            var damageInfo = DamageInfo.Create(damageAmount, poiseDamage, element, attacker, playHitVFX: true, isEnvironmental: false);
            damageable.TakeDamage(damageInfo);
            
            targetsDamaged++;
        }
        
        Debug.Log($"[DamageUtils] DealDamageInRadius complete - Targets damaged: {targetsDamaged}");
        return targetsDamaged;
    }
    
    /// <summary>
    /// Deal damage to a single target
    /// </summary>
    /// <param name="target">The target to damage</param>
    /// <param name="damageAmount">Amount of damage to deal</param>
    /// <param name="poiseDamage">Amount of poise damage to deal</param>
    /// <param name="attacker">The attacker (for damage source tracking)</param>
    /// <param name="element">Elemental type of the damage</param>
    public static void DealDamageToTarget(IDamageable target, float damageAmount, float poiseDamage, 
        Transform attacker, AttackElement element = AttackElement.PHYSICAL)
    {
        if (target == null) return;
        
        // Apply damage with elemental type (using DamageInfo)
        var damageInfo = DamageInfo.Create(damageAmount, poiseDamage, element, attacker, playHitVFX: true, isEnvironmental: false);
        target.TakeDamage(damageInfo);
    }

    /// <summary>
    /// Create a temporary damage area that deals damage over time
    /// </summary>
    /// <param name="position">Position of the damage area</param>
    /// <param name="radius">Radius of the damage area</param>
    /// <param name="damage">Damage per tick</param>
    /// <param name="poiseDamage">Poise damage per tick</param>
    /// <param name="attacker">The attacker (for damage source tracking)</param>
    /// <param name="element">Elemental type of the damage</param>
    /// <param name="duration">How long the area lasts</param>
    /// <param name="damageInterval">How often damage is dealt (seconds)</param>
    /// <param name="visualEffect">Visual effect for the damage area (will be parented to damage area)</param>
    /// <param name="allegiance">Allegiance of the damage area (auto-detected from attacker if not specified)</param>
    /// <returns>The created damage area GameObject</returns>
    public static GameObject CreateDamageArea(Vector3 position, float radius, float damage, float poiseDamage,
        Transform attacker, AttackElement element = AttackElement.PHYSICAL, float duration = 5f, 
        float damageInterval = 0.5f, EffectDefinition visualEffect = null, Allegiance? allegiance = null)
    {
        // Create the damage area GameObject
        GameObject damageAreaObj = new GameObject($"DamageArea_{element}");
        damageAreaObj.transform.position = position;
        
        // Add sphere collider for trigger detection
        SphereCollider sphereCollider = damageAreaObj.AddComponent<SphereCollider>();
        sphereCollider.isTrigger = true;
        sphereCollider.radius = radius;
        
        // Add rigidbody (required for triggers)
        Rigidbody rb = damageAreaObj.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        
        // Auto-detect allegiance from attacker if not specified
        Allegiance areaAllegiance = allegiance ?? GetAllegianceFromTransform(attacker);
        
        // Add the damage area component
        TemporaryDamageArea damageArea = damageAreaObj.AddComponent<TemporaryDamageArea>();
        damageArea.Setup(damage, poiseDamage, duration, element, 0, attacker, damageInterval, areaAllegiance);
        
        // Add visual effect if provided (parented to damage area)
        if (visualEffect != null)
        {
            GameObject visualObj = EffectManager.Instance.PlayEffect(position, Vector3.up, Quaternion.identity, 
                damageAreaObj.transform, visualEffect, duration: duration);
            
            if (visualObj != null)
            {
                // Add scaling animation to the visual effect
                DamageAreaVisual visual = visualObj.AddComponent<DamageAreaVisual>();
                visual.Setup(duration);
            }
        }
        
        return damageAreaObj;
    }
    
    /// <summary>
    /// Create a simple instant damage area (explosion-like)
    /// </summary>
    /// <param name="position">Position of the damage area</param>
    /// <param name="radius">Radius of the damage area</param>
    /// <param name="damage">Damage to deal</param>
    /// <param name="poiseDamage">Poise damage to deal</param>
    /// <param name="attacker">The attacker (for damage source tracking)</param>
    /// <param name="element">Elemental type of the damage</param>
    /// <param name="visualEffect">Visual effect for the explosion</param>
    /// <returns>Number of targets damaged</returns>
    public static int CreateInstantDamageArea(Vector3 position, float radius, float damage, float poiseDamage,
        Transform attacker, AttackElement element = AttackElement.PHYSICAL, EffectDefinition visualEffect = null)
    {
        // Deal damage instantly
        int targetsDamaged = DealDamageInRadius(position, radius, damage, poiseDamage, attacker, element, -1, true);
        
        // Play visual effect
        if (visualEffect != null)
        {
            EffectManager.Instance.PlayEffect(position, Vector3.up, Quaternion.identity, null, visualEffect);
        }
        
        return targetsDamaged;
    }

    // ===== RANGE AND COOLDOWN UTILITIES =====
    
    /// <summary>
    /// Check if a target is within attack range
    /// </summary>
    /// <param name="attackerPos">Position of the attacker</param>
    /// <param name="targetPos">Position of the target</param>
    /// <param name="minRange">Minimum attack range (0 = no minimum)</param>
    /// <param name="maxRange">Maximum attack range</param>
    /// <returns>True if target is within range</returns>
    public static bool IsInRange(Vector3 attackerPos, Vector3 targetPos, float minRange, float maxRange)
    {
        float distance = Vector3.Distance(attackerPos, targetPos);
        return distance >= minRange && distance <= maxRange;
    }
    
    /// <summary>
    /// Check if a target is within attack range, accounting for NavMesh obstacles
    /// This is crucial for attacking buildings and other structures with NavMesh obstacles,
    /// as enemies cannot path directly to their center point.
    /// </summary>
    /// <param name="attackerPos">Position of the attacker</param>
    /// <param name="target">Target transform to check</param>
    /// <param name="minRange">Minimum attack range (0 = no minimum)</param>
    /// <param name="maxRange">Maximum attack range</param>
    /// <param name="obstacleBoundsOffset">Additional offset for obstacle bounds (default: 1f)</param>
    /// <returns>True if target is within effective attack range</returns>
    public static bool IsInRangeWithObstacles(Vector3 attackerPos, Transform target, float minRange, float maxRange, float obstacleBoundsOffset = 1f)
    {
        if (target == null) return false;
        
        float distance = Vector3.Distance(attackerPos, target.position);
        
        // For minimum range, use simple distance (enemies need to stay away regardless of obstacles)
        if (minRange > 0 && distance < minRange)
        {
            return false;
        }
        
        // For maximum range, calculate effective reach distance considering obstacles
        float effectiveMaxRange = NavigationUtils.CalculateEffectiveReachDistance(attackerPos, target, maxRange, obstacleBoundsOffset);
        
        return distance <= effectiveMaxRange;
    }
    
    /// <summary>
    /// Check if a target is too close (within minimum range)
    /// </summary>
    /// <param name="attackerPos">Position of the attacker</param>
    /// <param name="targetPos">Position of the target</param>
    /// <param name="minRange">Minimum attack range</param>
    /// <returns>True if target is too close</returns>
    public static bool IsTooClose(Vector3 attackerPos, Vector3 targetPos, float minRange)
    {
        if (minRange <= 0) return false;
        
        float distance = Vector3.Distance(attackerPos, targetPos);
        return distance < minRange;
    }
    
    /// <summary>
    /// Check if a target is too far (beyond maximum range)
    /// </summary>
    /// <param name="attackerPos">Position of the attacker</param>
    /// <param name="targetPos">Position of the target</param>
    /// <param name="maxRange">Maximum attack range</param>
    /// <returns>True if target is too far</returns>
    public static bool IsTooFar(Vector3 attackerPos, Vector3 targetPos, float maxRange)
    {
        float distance = Vector3.Distance(attackerPos, targetPos);
        return distance > maxRange;
    }
    
    /// <summary>
    /// Check if enough time has passed since the last attack (cooldown check)
    /// </summary>
    /// <param name="lastAttackTime">Time of the last attack</param>
    /// <param name="cooldown">Cooldown duration in seconds</param>
    /// <returns>True if cooldown has elapsed</returns>
    public static bool IsCooldownReady(float lastAttackTime, float cooldown)
    {
        return Time.time - lastAttackTime >= cooldown;
    }
    
    /// <summary>
    /// Get remaining cooldown time
    /// </summary>
    /// <param name="lastAttackTime">Time of the last attack</param>
    /// <param name="cooldown">Cooldown duration in seconds</param>
    /// <returns>Remaining cooldown time (0 if ready)</returns>
    public static float GetRemainingCooldown(float lastAttackTime, float cooldown)
    {
        return Mathf.Max(0f, cooldown - (Time.time - lastAttackTime));
    }
    
    /// <summary>
    /// Get cooldown progress as a percentage (0 to 1, where 1 = ready)
    /// </summary>
    /// <param name="lastAttackTime">Time of the last attack</param>
    /// <param name="cooldown">Cooldown duration in seconds</param>
    /// <returns>Cooldown progress (0 to 1)</returns>
    public static float GetCooldownProgress(float lastAttackTime, float cooldown)
    {
        if (cooldown <= 0) return 1f;
        return Mathf.Clamp01((Time.time - lastAttackTime) / cooldown);
    }

    // ===== HIT DETECTION UTILITIES =====
    
    /// <summary>
    /// Perform a separate, larger OverlapBox specifically for detecting and hitting hittable objects (projectiles, props, etc).
    /// This runs before the main damage box cast and uses OverlapBox for better detection of fast-moving objects.
    /// </summary>
    /// <param name="dealer">The damage dealer performing the attack</param>
    /// <param name="center">Center point of the overlap box</param>
    /// <param name="boxSize">Size of the box (full extents, not half extents)</param>
    /// <param name="rotation">Rotation of the box</param>
    /// <param name="hitTargets">HashSet to track already hit targets (prevents double-hitting)</param>
    /// <returns>Number of hittable objects hit (e.g., projectiles reflected)</returns>
    public static int PerformProjectileReflectionDetection(IDamageDealer dealer, Vector3 center, Vector3 boxSize,
        Quaternion rotation, System.Collections.Generic.HashSet<Collider> hitTargets = null)
    {
        if (dealer == null) return 0;
        
        int objectsHit = 0;
        
        // Use OverlapBox for better detection of fast-moving objects (doesn't require continuous collision)
        // Use QueryTriggerInteraction.Collide to detect trigger colliders (projectiles use triggers for player detection)
        // Use half extents for OverlapBox
        Collider[] colliders = Physics.OverlapBox(center, boxSize * 0.5f, rotation, -1, QueryTriggerInteraction.Collide);
        
        Debug.Log($"[DamageUtils] PerformProjectileReflectionDetection - Box center: {center} | Box size: {boxSize} | Box rotation: {rotation.eulerAngles} | Found {colliders.Length} colliders");
        
        // DIRECTLY check for ALL IHittable objects in scene (more reliable than Physics.OverlapBox for newly-spawned colliders)
        IHittable[] allHittables = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IHittable>().ToArray();
        if (allHittables.Length > 0)
        {
            Debug.Log($"[DamageUtils] Found {allHittables.Length} IHittable objects in scene:");
            foreach (var hittable in allHittables)
            {
                if (hittable is MonoBehaviour mb && mb != null)
                {
                    float distance = Vector3.Distance(center, mb.transform.position);
                    
                    // Transform the position to the box's local space for accurate AABB check
                    Vector3 localPos = Quaternion.Inverse(rotation) * (mb.transform.position - center);
                    Vector3 halfExtents = boxSize * 0.5f;
                    bool inBox = Mathf.Abs(localPos.x) <= halfExtents.x && 
                                 Mathf.Abs(localPos.y) <= halfExtents.y && 
                                 Mathf.Abs(localPos.z) <= halfExtents.z;
                    
                    Debug.Log($"[DamageUtils]   - {mb.gameObject.name} at {mb.transform.position} | Distance: {distance:F2}m | InBox: {inBox} | CanBeHit: {hittable.CanBeHit()}");
                    
                    // PROCESS IHittables that are in the box directly (bypasses Physics.OverlapBox issues with new colliders)
                    if (inBox && hittable.CanBeHit())
                    {
                        // Skip if already hit
                        Collider hittableCollider = mb.GetComponent<Collider>();
                        if (hitTargets != null && hittableCollider != null && hitTargets.Contains(hittableCollider))
                        {
                            Debug.Log($"[DamageUtils] Skipping {mb.gameObject.name} - already hit this collider");
                            continue;
                        }
                        
                        // Calculate hit info
                        Vector3 hitPoint = mb.transform.position;
                        Vector3 hitNormal = (hitPoint - center).normalized;
                        if (hitNormal == Vector3.zero)
                        {
                            hitNormal = -mb.transform.forward;
                        }
                        
                        Debug.Log($"[DamageUtils] ✅ HITTING IHittable {mb.gameObject.name} directly at position {hitPoint}");
                        
                        // Create hit info and call OnHit
                        var hitInfo = new HitInfo(hitPoint, hitNormal, dealer.DamageSource, dealer.BaseDamage, dealer);
                        hittable.OnHit(hitInfo);
                        
                        // Mark as hit
                        if (hitTargets != null && hittableCollider != null)
                        {
                            hitTargets.Add(hittableCollider);
                        }
                        
                        objectsHit++;
                    }
                }
            }
        }
        
        // Log all detected colliders immediately
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null && colliders[i].gameObject != null)
            {
                Debug.Log($"[DamageUtils] Detected collider [{i}]: {colliders[i].gameObject.name} at {colliders[i].transform.position} | Layer: {LayerMask.LayerToName(colliders[i].gameObject.layer)} ({colliders[i].gameObject.layer}) | IsTrigger: {colliders[i].isTrigger}");
            }
        }
        
        foreach (Collider collider in colliders)
        {
            if (collider == null || collider.gameObject == null) continue;
            
            // Skip if we've already hit this collider
            if (hitTargets != null && hitTargets.Contains(collider))
            {
                Debug.Log($"[DamageUtils] Skipping {collider.gameObject.name} - already hit");
                continue;
            }
            
            // Debug log what we found
            Debug.Log($"[DamageUtils] Checking collider: {collider.gameObject.name} | Layer: {LayerMask.LayerToName(collider.gameObject.layer)} ({collider.gameObject.layer}) | IsTrigger: {collider.isTrigger}");
            
            // Check for IHittable interface (unified system for projectiles, props, etc.)
            IHittable hittable = collider.GetComponent<IHittable>();
            if (hittable != null)
            {
                Debug.Log($"[DamageUtils] Found IHittable on {collider.gameObject.name}!");
                
                if (!hittable.CanBeHit())
                {
                    Debug.Log($"[DamageUtils] IHittable {collider.gameObject.name} CanBeHit returned false - skipping");
                    continue;
                }
                
                // Calculate hit info
                Vector3 hitPoint = collider.transform.position;
                Vector3 hitNormal = (hitPoint - center).normalized;
                if (hitNormal == Vector3.zero)
                {
                    // Fallback: use opposite of object's forward direction
                    hitNormal = -collider.transform.forward;
                }
                
                Debug.Log($"[DamageUtils] Hitting IHittable {collider.gameObject.name} at position {hitPoint}");
                
                // Create hit info and call OnHit
                var hitInfo = new HitInfo(hitPoint, hitNormal, dealer.DamageSource, dealer.BaseDamage, dealer);
                hittable.OnHit(hitInfo);
                
                // Track this target
                if (hitTargets != null)
                {
                    hitTargets.Add(collider);
                }
                
                objectsHit++;
                continue;
            }
            
            // Also check in parent in case collider is on a child object
            IHittable hittableInParent = collider.GetComponentInParent<IHittable>();
            if (hittableInParent != null)
            {
                Debug.Log($"[DamageUtils] Found IHittable in PARENT of {collider.gameObject.name}! Parent: {(hittableInParent as MonoBehaviour)?.gameObject.name ?? "Unknown"}");
                
                if (!hittableInParent.CanBeHit())
                {
                    Debug.Log($"[DamageUtils] IHittable parent CanBeHit returned false - skipping");
                    continue;
                }
                
                // Get the parent's transform for position
                Transform parentTransform = (hittableInParent as MonoBehaviour)?.transform;
                if (parentTransform != null)
                {
                    Vector3 hitPoint = parentTransform.position;
                    Vector3 hitNormal = (hitPoint - center).normalized;
                    if (hitNormal == Vector3.zero)
                    {
                        hitNormal = -parentTransform.forward;
                    }
                    
                    Debug.Log($"[DamageUtils] Hitting IHittable (from parent) at position {hitPoint}");
                    
                    var hitInfo = new HitInfo(hitPoint, hitNormal, dealer.DamageSource, dealer.BaseDamage, dealer);
                    hittableInParent.OnHit(hitInfo);
                    
                    if (hitTargets != null)
                    {
                    hitTargets.Add(collider);
                    }
                    
                    objectsHit++;
                }
            }
        }
        
        return objectsHit;
    }
    
    /// <summary>
    /// Perform a box cast to detect and damage targets in an area
    /// </summary>
    /// <param name="dealer">The damage dealer performing the attack</param>
    /// <param name="origin">Origin point of the box cast</param>
    /// <param name="boxSize">Size of the box (half extents)</param>
    /// <param name="direction">Direction to cast the box</param>
    /// <param name="rotation">Rotation of the box</param>
    /// <param name="distance">Distance to cast the box</param>
    /// <param name="layerMask">Layer mask for valid targets</param>
    /// <param name="hitTargets">HashSet to track already hit targets (prevents double-hitting)</param>
    /// <returns>Number of new targets hit</returns>
    public static int PerformBoxCastDamage(IDamageDealer dealer, Vector3 origin, Vector3 boxSize, 
        Vector3 direction, Quaternion rotation, float distance, LayerMask layerMask, 
        System.Collections.Generic.HashSet<Collider> hitTargets = null)
    {
        if (dealer == null) return 0;
        
        int targetsHit = 0;
        
        // Perform the BoxCast
        RaycastHit[] hits = Physics.BoxCastAll(origin, boxSize * 0.5f, direction, rotation, distance, layerMask);
        
        foreach (RaycastHit hit in hits)
        {
            // Skip if we've already hit this collider
            if (hitTargets != null && hitTargets.Contains(hit.collider))
                continue;
            
            // Check for hittable objects (projectiles, props, etc.) via IHittable interface
            IHittable hittable = hit.collider.GetComponent<IHittable>();
            if (hittable != null && hittable.CanBeHit())
            {
                // Use hit collider's transform position (BoxCast hit.point may be unreliable)
                Vector3 hitPoint = hit.collider.transform.position;
                
                // Calculate hit normal - direction from weapon origin to target
                Vector3 hitNormal = (hitPoint - origin).normalized;
                if (hitNormal == Vector3.zero)
                {
                    // Fallback: use opposite of target's forward direction
                    hitNormal = -hit.collider.transform.forward;
                }
                
                // Create HitInfo and call OnHit via IHittable interface
                var hitInfo = new HitInfo(hitPoint, hitNormal, dealer.DamageSource, dealer.BaseDamage, dealer);
                hittable.OnHit(hitInfo);
                
                // Track this target
                if (hitTargets != null)
                {
                    hitTargets.Add(hit.collider);
                }
                
                targetsHit++;
                continue;
            }
            
            // Check for damageable targets (enemies, etc.)
            IDamageable target = hit.collider.GetComponent<IDamageable>();
            if (target != null && IsValidTarget(target, dealer))
            {
                // Deal damage using the unified system
                DealDamage(dealer, target);
                
                // Track this target
                if (hitTargets != null)
                {
                    hitTargets.Add(hit.collider);
                }
                
                targetsHit++;
            }
        }
        
        return targetsHit;
    }
    
    /// <summary>
    /// Perform a sphere cast to detect and damage targets
    /// </summary>
    /// <param name="dealer">The damage dealer performing the attack</param>
    /// <param name="origin">Origin point of the sphere cast</param>
    /// <param name="radius">Radius of the sphere</param>
    /// <param name="direction">Direction to cast the sphere</param>
    /// <param name="distance">Distance to cast the sphere</param>
    /// <param name="layerMask">Layer mask for valid targets</param>
    /// <param name="hitTargets">HashSet to track already hit targets (prevents double-hitting)</param>
    /// <returns>Number of new targets hit</returns>
    public static int PerformSphereCastDamage(IDamageDealer dealer, Vector3 origin, float radius,
        Vector3 direction, float distance, LayerMask layerMask,
        System.Collections.Generic.HashSet<Collider> hitTargets = null)
    {
        if (dealer == null) return 0;
        
        int targetsHit = 0;
        
        // Perform the SphereCast
        RaycastHit[] hits = Physics.SphereCastAll(origin, radius, direction, distance, layerMask);
        
        foreach (RaycastHit hit in hits)
        {
            // Skip if we've already hit this collider
            if (hitTargets != null && hitTargets.Contains(hit.collider))
                continue;
            
            IDamageable target = hit.collider.GetComponent<IDamageable>();
            if (target != null && IsValidTarget(target, dealer))
            {
                // Deal damage using the unified system
                DealDamage(dealer, target);
                
                // Track this target
                if (hitTargets != null)
                {
                    hitTargets.Add(hit.collider);
                }
                
                targetsHit++;
            }
        }
        
        return targetsHit;
    }
    
    /// <summary>
    /// Perform a single raycast to detect and damage a target
    /// </summary>
    /// <param name="dealer">The damage dealer performing the attack</param>
    /// <param name="origin">Origin point of the raycast</param>
    /// <param name="direction">Direction of the raycast</param>
    /// <param name="maxDistance">Maximum distance of the raycast</param>
    /// <param name="layerMask">Layer mask for valid targets</param>
    /// <param name="hitInfo">Output hit information</param>
    /// <returns>True if a valid target was hit and damaged</returns>
    public static bool PerformRaycastDamage(IDamageDealer dealer, Vector3 origin, Vector3 direction,
        float maxDistance, LayerMask layerMask, out RaycastHit hitInfo)
    {
        if (dealer == null)
        {
            hitInfo = default;
            return false;
        }
        
        if (Physics.Raycast(origin, direction, out hitInfo, maxDistance, layerMask))
        {
            IDamageable target = hitInfo.collider.GetComponent<IDamageable>();
            if (target != null && IsValidTarget(target, dealer))
            {
                // Deal damage using the unified system
                DealDamage(dealer, target);
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Perform a capsule cast to detect and damage targets (useful for sweeping melee attacks)
    /// </summary>
    /// <param name="dealer">The damage dealer performing the attack</param>
    /// <param name="point1">Start point of the capsule</param>
    /// <param name="point2">End point of the capsule</param>
    /// <param name="radius">Radius of the capsule</param>
    /// <param name="direction">Direction to cast the capsule</param>
    /// <param name="distance">Distance to cast the capsule</param>
    /// <param name="layerMask">Layer mask for valid targets</param>
    /// <param name="hitTargets">HashSet to track already hit targets (prevents double-hitting)</param>
    /// <returns>Number of new targets hit</returns>
    public static int PerformCapsuleCastDamage(IDamageDealer dealer, Vector3 point1, Vector3 point2,
        float radius, Vector3 direction, float distance, LayerMask layerMask,
        System.Collections.Generic.HashSet<Collider> hitTargets = null)
    {
        if (dealer == null) return 0;
        
        int targetsHit = 0;
        
        // Perform the CapsuleCast
        RaycastHit[] hits = Physics.CapsuleCastAll(point1, point2, radius, direction, distance, layerMask);
        
        foreach (RaycastHit hit in hits)
        {
            // Skip if we've already hit this collider
            if (hitTargets != null && hitTargets.Contains(hit.collider))
                continue;
            
            IDamageable target = hit.collider.GetComponent<IDamageable>();
            if (target != null && IsValidTarget(target, dealer))
            {
                // Deal damage using the unified system
                DealDamage(dealer, target);
                
                // Track this target
                if (hitTargets != null)
                {
                    hitTargets.Add(hit.collider);
                }
                
                targetsHit++;
            }
        }
        
        return targetsHit;
    }
    
    /// <summary>
    /// Perform an overlap box check to detect and damage targets in a stationary area
    /// </summary>
    /// <param name="dealer">The damage dealer performing the attack</param>
    /// <param name="center">Center of the box</param>
    /// <param name="halfExtents">Half extents of the box</param>
    /// <param name="rotation">Rotation of the box</param>
    /// <param name="layerMask">Layer mask for valid targets</param>
    /// <param name="hitTargets">HashSet to track already hit targets (prevents double-hitting)</param>
    /// <returns>Number of new targets hit</returns>
    public static int PerformOverlapBoxDamage(IDamageDealer dealer, Vector3 center, Vector3 halfExtents,
        Quaternion rotation, LayerMask layerMask,
        System.Collections.Generic.HashSet<Collider> hitTargets = null)
    {
        if (dealer == null) return 0;
        
        int targetsHit = 0;
        
        // Perform the OverlapBox
        Collider[] colliders = Physics.OverlapBox(center, halfExtents, rotation, layerMask);
        
        foreach (Collider collider in colliders)
        {
            // Skip if we've already hit this collider
            if (hitTargets != null && hitTargets.Contains(collider))
                continue;
            
            IDamageable target = collider.GetComponent<IDamageable>();
            if (target != null && IsValidTarget(target, dealer))
            {
                // Deal damage using the unified system
                DealDamage(dealer, target);
                
                // Track this target
                if (hitTargets != null)
                {
                    hitTargets.Add(collider);
                }
                
                targetsHit++;
            }
        }
        
        return targetsHit;
    }
    
    /// <summary>
    /// Draw a box cast gizmo for visualization in the editor
    /// </summary>
    /// <param name="origin">Origin of the box cast</param>
    /// <param name="boxSize">Size of the box</param>
    /// <param name="rotation">Rotation of the box</param>
    /// <param name="color">Color of the gizmo</param>
    public static void DrawBoxCastGizmo(Vector3 origin, Vector3 boxSize, Quaternion rotation, Color color)
    {
        Gizmos.color = color;
        Gizmos.matrix = Matrix4x4.TRS(origin, rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, boxSize);
    }

    // ===== PROJECTILE UTILITIES =====

    /// <summary>
    /// Fire an arc projectile using parameter class (cleaner API)
    /// </summary>
    public static GameObject FireArcProjectile(Vector3 startPosition, Quaternion rotation, EffectDefinition projectileEffect, ArcProjectileParams parameters)
    {
        if (parameters == null)
        {
            Debug.LogError("[DamageUtils] ❌ ArcProjectileParams is NULL!");
            return null;
        }
        
        Vector3 direction = (parameters.targetPosition - startPosition).normalized;
        if (direction == Vector3.zero) direction = Vector3.forward;
        
        return FireProjectileWithEffect(startPosition, direction, rotation, parameters.targetPosition,
            parameters.damage, parameters.poiseDamage, parameters.attacker, parameters.element,
            projectileEffect, parameters.impactEffect, parameters.createDamageArea,
            parameters.damageAreaRadius, parameters.damageAreaDuration, parameters.useTriggerBasedDamage,
            ProjectileType.ARC, parameters.speed, parameters.maxHeight, null, 0f, 0f, true,
            parameters.armingDelay, parameters.explodeOnAnyHit);
    }

    /// <summary>
    /// Fire a straight projectile using parameter class (cleaner API)
    /// </summary>
    public static GameObject FireStraightProjectile(Vector3 startPosition, Quaternion rotation, EffectDefinition projectileEffect, StraightProjectileParams parameters)
    {
        if (parameters == null)
        {
            Debug.LogError("[DamageUtils] ❌ StraightProjectileParams is NULL!");
            return null;
        }
        
        return FireProjectileWithEffect(startPosition, parameters.direction, rotation, Vector3.zero,
            parameters.damage, parameters.poiseDamage, parameters.attacker, parameters.element,
            projectileEffect, parameters.impactEffect, parameters.createDamageArea,
            parameters.damageAreaRadius, parameters.damageAreaDuration, parameters.useTriggerBasedDamage,
            ProjectileType.STRAIGHT, parameters.speed, 0f, null, 0f, 0f, true,
            parameters.armingDelay, false);
    }

    /// <summary>
    /// Fire a homing projectile using parameter class (cleaner API)
    /// </summary>
    public static GameObject FireHomingProjectile(Vector3 startPosition, Quaternion rotation, EffectDefinition projectileEffect, HomingProjectileParams parameters)
    {
        if (parameters == null)
        {
            Debug.LogError("[DamageUtils] ❌ HomingProjectileParams is NULL!");
            return null;
        }
        
        Vector3 direction = Vector3.forward;
        if (parameters.targetTransform != null)
        {
            direction = (parameters.targetTransform.position - startPosition).normalized;
        }
        
        return FireProjectileWithEffect(startPosition, direction, rotation, Vector3.zero,
            parameters.damage, parameters.poiseDamage, parameters.attacker, parameters.element,
            projectileEffect, parameters.impactEffect, parameters.createDamageArea,
            parameters.damageAreaRadius, parameters.damageAreaDuration, parameters.useTriggerBasedDamage,
            ProjectileType.HOMING, parameters.speed, 0f, parameters.targetTransform,
            parameters.homingDuration, parameters.turnSpeed, parameters.explodeOnTimeout,
            parameters.armingDelay, false);
    }

    /// <summary>
    /// Fire a projectile using an effect definition and attach the specified projectile behavior
    /// LEGACY METHOD: Consider using FireArcProjectile, FireStraightProjectile, or FireHomingProjectile for cleaner code
    /// </summary>
    /// <param name="startPosition">Starting position of the projectile</param>
    /// <param name="direction">Direction of the projectile</param>
    /// <param name="rotation">Rotation of the projectile</param>
    /// <param name="targetPosition">Target position for the projectile (used by ARC type)</param>
    /// <param name="damage">Damage the projectile deals on impact</param>
    /// <param name="poiseDamage">Poise damage the projectile deals on impact</param>
    /// <param name="attacker">The attacker (for damage source tracking)</param>
    /// <param name="element">Elemental type of the damage</param>
    /// <param name="projectileEffect">Effect definition for the projectile visual</param>
    /// <param name="impactEffect">Effect to play on impact</param>
    /// <param name="createDamageArea">Whether to create a damage area on impact</param>
    /// <param name="damageAreaRadius">Radius of the damage area (if createDamageArea is true)</param>
    /// <param name="damageAreaDuration">Duration of the damage area (if createDamageArea is true)</param>
    /// <param name="useTriggerBasedDamage">Whether to use trigger-based damage detection</param>
    /// <param name="projectileType">Type of projectile behavior to attach (STRAIGHT, ARC, HOMING, etc.)</param>
    /// <param name="projectileSpeed">Speed of the projectile (default varies by type)</param>
    /// <param name="projectileMaxHeight">Max height for arc projectiles (default 5f)</param>
    /// <param name="targetTransform">Target Transform for homing projectiles (required for HOMING type)</param>
    /// <param name="homingDuration">How long homing projectiles track the target (default 3f)</param>
    /// <param name="turnSpeed">Turn speed for homing projectiles in degrees/second (default 180f)</param>
    /// <param name="explodeOnTimeout">Whether homing projectiles explode when tracking expires (default true)</param>
    /// <param name="armingDelay">Delay before projectile can cause damage, allowing time for reflection/dodge (default 0.2f)</param>
    /// <param name="explodeOnAnyHit">If true, explode on any collision. If false, only explode on ground hit (default false for arc projectiles)</param>
    /// <returns>The spawned projectile GameObject</returns>
    public static GameObject FireProjectileWithEffect(Vector3 startPosition, Vector3 direction, Quaternion rotation,
        Vector3 targetPosition, float damage, float poiseDamage, Transform attacker, AttackElement element,
        EffectDefinition projectileEffect, EffectDefinition impactEffect = null, bool createDamageArea = false,
        float damageAreaRadius = 0f, float damageAreaDuration = 5f, bool useTriggerBasedDamage = false,
        ProjectileType projectileType = ProjectileType.NONE, float projectileSpeed = 0f, float projectileMaxHeight = 5f,
        Transform targetTransform = null, float homingDuration = 3f, float turnSpeed = 180f, bool explodeOnTimeout = true,
        float armingDelay = 0.2f, bool explodeOnAnyHit = false)
    {
        Debug.Log($"[DamageUtils] ===== FIRE PROJECTILE WITH EFFECT START =====");
        Debug.Log($"[DamageUtils] Projectile Type: {projectileType} | Start Position: {startPosition} | Direction: {direction}");
        Debug.Log($"[DamageUtils] Attacker: {(attacker != null ? attacker.name : "NULL")} | Damage: {damage} | Poise: {poiseDamage} | Element: {element}");
        
        if (projectileEffect == null)
        {
            Debug.LogError("[DamageUtils] ❌ Projectile effect definition is NULL!");
            return null;
        }
        
        Debug.Log($"[DamageUtils] Projectile Effect Definition: {projectileEffect.name}");
        
        if (EffectManager.Instance == null)
        {
            Debug.LogError("[DamageUtils] ❌ EffectManager.Instance is NULL! Cannot spawn projectile effect.");
            return null;
        }
        
        Debug.Log($"[DamageUtils] EffectManager.Instance found, spawning projectile effect...");
        
        // Play the projectile effect and get the spawned GameObject
        GameObject projectileObj = EffectManager.Instance.PlayEffect(startPosition, direction, rotation, null, projectileEffect);
        
        if (projectileObj == null)
        {
            Debug.LogError("[DamageUtils] ❌ EffectManager.Instance.PlayEffect() returned NULL! Failed to spawn projectile effect!");
            Debug.LogError($"[DamageUtils]   - Effect Definition: {projectileEffect.name}");
            Debug.LogError($"[DamageUtils]   - Start Position: {startPosition}");
            Debug.LogError($"[DamageUtils]   - Effect Definition prefabs count: {(projectileEffect.prefabs != null ? projectileEffect.prefabs.Length.ToString() : "NULL")}");
            if (projectileEffect.prefabs != null && projectileEffect.prefabs.Length > 0)
            {
                Debug.LogError($"[DamageUtils]   - First prefab: {(projectileEffect.prefabs[0] != null && projectileEffect.prefabs[0].prefab != null ? projectileEffect.prefabs[0].prefab.name : "NULL")}");
            }
            return null;
        }
        
        Debug.Log($"[DamageUtils] ✅ Projectile GameObject spawned: {projectileObj.name} | Active: {projectileObj.activeSelf}");
        Debug.Log($"[DamageUtils] Projectile Position: {projectileObj.transform.position} | Rotation: {projectileObj.transform.rotation}");
        
        // Attach the appropriate projectile behavior based on type
        switch (projectileType)
        {
            case ProjectileType.STRAIGHT:
                {
                    float speed = projectileSpeed > 0f ? projectileSpeed : 20f;
                    StraightProjectile straightProj = projectileObj.GetComponent<StraightProjectile>();
                    if (straightProj == null)
                    {
                        straightProj = projectileObj.AddComponent<StraightProjectile>();
                    }
                    
                    // Create parameter class for cleaner initialization
                    StraightProjectileParams straightParams = new StraightProjectileParams(
                        direction, speed, damage, poiseDamage, attacker, element, impactEffect,
                        createDamageArea, damageAreaRadius, damageAreaDuration, useTriggerBasedDamage, armingDelay);
                    straightProj.Initialize(straightParams);
                }
                break;
                
            case ProjectileType.ARC:
                {
                    float speed = projectileSpeed > 0f ? projectileSpeed : 10f;
                    ArcProjectile arcProj = projectileObj.GetComponent<ArcProjectile>();
                    if (arcProj == null)
                    {
                        arcProj = projectileObj.AddComponent<ArcProjectile>();
                    }
                    
                    // Create parameter class for cleaner initialization
                    ArcProjectileParams arcParams = new ArcProjectileParams(
                        targetPosition, speed, projectileMaxHeight, damage, poiseDamage, attacker, element, impactEffect,
                        createDamageArea, damageAreaRadius, damageAreaDuration, useTriggerBasedDamage, armingDelay, explodeOnAnyHit);
                    arcProj.Initialize(arcParams);
                }
                break;
                
            case ProjectileType.HOMING:
                {
                    Debug.Log($"[DamageUtils] Processing HOMING projectile type...");
                    
                    if (targetTransform == null)
                    {
                        Debug.LogWarning("[DamageUtils] ⚠️ HOMING projectile type requires a targetTransform parameter. Homing projectile may not track correctly.");
                    }
                    else
                    {
                        Debug.Log($"[DamageUtils] Target Transform: {targetTransform.name} | Position: {targetTransform.position}");
                    }
                    
                    float speed = projectileSpeed > 0f ? projectileSpeed : 15f;
                    Debug.Log($"[DamageUtils] Projectile Speed: {speed} (provided: {projectileSpeed})");
                    
                    HomingProjectile homingProj = projectileObj.GetComponent<HomingProjectile>();
                    if (homingProj == null)
                    {
                        Debug.Log($"[DamageUtils] HomingProjectile component not found, adding...");
                        homingProj = projectileObj.AddComponent<HomingProjectile>();
                        if (homingProj == null)
                        {
                            Debug.LogError("[DamageUtils] ❌ Failed to add HomingProjectile component!");
                            return null;
                        }
                        Debug.Log($"[DamageUtils] ✅ HomingProjectile component added");
                    }
                    else
                    {
                        Debug.Log($"[DamageUtils] HomingProjectile component already exists");
                    }
                    
                    Debug.Log($"[DamageUtils] Initializing HomingProjectile with:");
                    Debug.Log($"[DamageUtils]   - Target: {(targetTransform != null ? targetTransform.name : "NULL")}");
                    Debug.Log($"[DamageUtils]   - Damage: {damage} | Poise: {poiseDamage} | Element: {element}");
                    Debug.Log($"[DamageUtils]   - Speed: {speed} | Duration: {homingDuration}s | Turn Speed: {turnSpeed}°/s");
                    Debug.Log($"[DamageUtils]   - Explode On Timeout: {explodeOnTimeout}");
                    Debug.Log($"[DamageUtils]   - Create Damage Area: {createDamageArea} | Radius: {damageAreaRadius} | Duration: {damageAreaDuration}");
                    Debug.Log($"[DamageUtils]   - Use Trigger Based: {useTriggerBasedDamage}");
                    Debug.Log($"[DamageUtils]   - Impact Effect: {(impactEffect != null ? impactEffect.name : "NULL")}");
                    
                    try
                    {
                        // Create parameter class for cleaner initialization
                        HomingProjectileParams homingParams = new HomingProjectileParams(
                            targetTransform, speed, homingDuration, turnSpeed, explodeOnTimeout,
                            damage, poiseDamage, attacker, element, impactEffect,
                            createDamageArea, damageAreaRadius, damageAreaDuration, useTriggerBasedDamage, armingDelay);
                        homingProj.Initialize(homingParams);
                        Debug.Log($"[DamageUtils] ✅ HomingProjectile.Initialize() completed successfully");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[DamageUtils] ❌ EXCEPTION during HomingProjectile.Initialize(): {e.Message}");
                        Debug.LogError($"[DamageUtils] Stack Trace: {e.StackTrace}");
                        return null;
                    }
                }
                break;
                
            case ProjectileType.NONE:
                // No projectile behavior - check if prefab already has one
                ArcProjectile existingArc = projectileObj.GetComponent<ArcProjectile>();
                if (existingArc != null)
                {
                    float speed = projectileSpeed > 0f ? projectileSpeed : 10f;
                    existingArc.Initialize(targetPosition, damage, poiseDamage, attacker, element, speed, projectileMaxHeight,
                        impactEffect, createDamageArea, damageAreaRadius, damageAreaDuration, useTriggerBasedDamage, armingDelay);
                }
                
                StraightProjectile existingStraight = projectileObj.GetComponent<StraightProjectile>();
                if (existingStraight != null)
                {
                    float speed = projectileSpeed > 0f ? projectileSpeed : 20f;
                    existingStraight.Initialize(direction, damage, poiseDamage, attacker, element, speed,
                        impactEffect, createDamageArea, damageAreaRadius, damageAreaDuration, useTriggerBasedDamage, armingDelay);
                }
                break;
        }
        
        Debug.Log($"[DamageUtils] ✅ Projectile setup complete. Returning: {(projectileObj != null ? projectileObj.name : "NULL")}");
        Debug.Log($"[DamageUtils] ===== FIRE PROJECTILE WITH EFFECT END =====");
        
        return projectileObj;
    }
    
    /// <summary>
    /// Deal damage to a single target using the damage dealer's properties (unified system)
    /// </summary>
    /// <param name="dealer">The damage dealer</param>
    /// <param name="target">The target to damage</param>
    public static void DealDamage(IDamageDealer dealer, IDamageable target)
    {
        if (dealer == null || target == null) return;
        
        DealDamage(dealer, target, dealer.BaseDamage, dealer.PoiseDamage);
    }
    
    /// <summary>
    /// Deal damage to a single target with custom damage amounts (unified system)
    /// </summary>
    /// <param name="dealer">The damage dealer</param>
    /// <param name="target">The target to damage</param>
    /// <param name="damageAmount">Custom damage amount</param>
    /// <param name="poiseAmount">Custom poise damage amount</param>
    public static void DealDamage(IDamageDealer dealer, IDamageable target, float damageAmount, float poiseAmount)
    {
        if (dealer == null || target == null) return;
        
        // Calculate total damage including elemental bonus
        float totalDamage = damageAmount;
        if (dealer.ElementType != AttackElement.NONE)
        {
            totalDamage += dealer.ElementalDamageBonus;
        }
        
        // Apply damage using DamageInfo from dealer
        var damageInfo = new DamageInfo(dealer, amount: totalDamage, poiseDamage: poiseAmount);
        target.TakeDamage(damageInfo);
        
        // Apply additional effects based on dealer type
        ApplyAdditionalEffects(dealer, target, totalDamage);
    }
    
    /// <summary>
    /// Deal damage to all targets in a radius using the unified system
    /// </summary>
    /// <param name="dealer">The damage dealer</param>
    /// <param name="center">Center position of the damage area</param>
    /// <param name="radius">Radius of the damage area</param>
    /// <param name="damageAmount">Custom damage amount (optional)</param>
    /// <param name="poiseAmount">Custom poise damage amount (optional)</param>
    /// <param name="targetLayer">Layer mask for targets (optional)</param>
    /// <returns>Number of targets damaged</returns>
    public static int DealDamageInRadius(IDamageDealer dealer, Vector3 center, float radius, 
        float? damageAmount = null, float? poiseAmount = null, LayerMask? targetLayer = null)
    {
        if (dealer == null) return 0;
        
        float actualDamage = damageAmount ?? dealer.BaseDamage;
        float actualPoise = poiseAmount ?? dealer.PoiseDamage;
        
        // Find all colliders in the radius
        Collider[] hitColliders = Physics.OverlapSphere(center, radius);
        int targetsDamaged = 0;
        
        foreach (var hitCollider in hitColliders)
        {
            // Apply layer mask filter if provided
            if (targetLayer.HasValue && !IsInLayerMask(hitCollider.gameObject, targetLayer.Value))
                continue;
                
            IDamageable damageable = hitCollider.GetComponent<IDamageable>();
            if (damageable != null && IsValidTarget(damageable, dealer))
            {
                // Check if the target is still active (this will catch NPCs in bunkers)
                if (!hitCollider.gameObject.activeInHierarchy)
                {
                    continue; // Skip inactive targets (like NPCs in bunkers)
                }
                
                DealDamage(dealer, damageable, actualDamage, actualPoise);
                targetsDamaged++;
            }
        }
        
        return targetsDamaged;
    }
    
    /// <summary>
    /// Apply additional effects based on the dealer type
    /// </summary>
    private static void ApplyAdditionalEffects(IDamageDealer dealer, IDamageable target, float totalDamage)
    {
        // Apply threat for enemy attacks
        if (dealer is AttackBase attackBase)
        {
            if (Managers.EnemyManager.Instance != null && attackBase.Target != null)
            {
                Managers.EnemyManager.Instance.AddThreat(attackBase.Target, totalDamage * 10f);
            }
        }
        
        // Apply status effects for WeaponAttack
        if (dealer is Combat.Attacks.WeaponAttack weaponAttack)
        {
            ApplyWeaponAttackStatusEffects(weaponAttack, target);
            // Trigger camera shake for player weapons
            TriggerHitCameraShake(dealer.DamageSource);
        }
    }
    
    /// <summary>
    /// Apply status effects from WeaponAttack
    /// </summary>
    private static void ApplyWeaponAttackStatusEffects(Combat.Attacks.WeaponAttack weaponAttack, IDamageable target)
    {
        WeaponScriptableObj weaponData = weaponAttack.WeaponData;
        if (weaponData == null) return;
        if (weaponData.possibleStatusEffects == null || weaponData.possibleStatusEffects.Length == 0) return;
        if (weaponData.statusEffectChance <= 0f) return;
        
        // Check if status effect should be applied
        if (Random.Range(0f, 1f) <= weaponData.statusEffectChance)
        {
            // Select a random status effect from possible effects
            StatusEffectType selectedEffect = weaponData.possibleStatusEffects[Random.Range(0, weaponData.possibleStatusEffects.Length)];
            
            // Apply the status effect
            ApplyStatusEffect(target, selectedEffect, weaponData.statusEffectDuration);
        }
    }
    
    /// <summary>
    /// Apply a specific status effect to a target
    /// </summary>
    private static void ApplyStatusEffect(IDamageable target, StatusEffectType effectType, float duration)
    {
        // This is a basic implementation - you may want to integrate with your status effect system
        Debug.Log($"Applied {effectType} status effect to {target} for {duration} seconds");
        
        // TODO: Integrate with your status effect system here
        // Example: StatusEffectManager.Instance.ApplyEffect(target, effectType, duration);
    }
    
    /// <summary>
    /// Trigger camera shake when hitting an enemy, but only if the attacker is player-controlled
    /// </summary>
    private static void TriggerHitCameraShake(Transform damageSource)
    {
        if (damageSource == null) return;
        
        // Check if this attack came from the player-controlled character
        if (PlayerController.Instance != null && 
            PlayerController.Instance._possessedNPC != null &&
            PlayerCamera.Instance != null)
        {
            // Get the transform of the possessed NPC
            Transform possessedTransform = PlayerController.Instance._possessedNPC.GetTransform();
            
            // If the damage source matches the possessed character, trigger camera shake
            if (damageSource == possessedTransform)
            {
                PlayerCamera.Instance.ShakeFromHittingEnemy();
            }
        }
    }
    
    /// <summary>
    /// Check if a target is valid for this damage dealer
    /// 
    /// SIMPLIFIED DAMAGE RULES:
    /// - HOSTILE dealers → Damage FRIENDLY targets (enemies attack players/NPCs/turrets)
    /// - FRIENDLY dealers → Damage HOSTILE targets (players/turrets attack enemies)
    /// - NEUTRAL dealers → Damage ALL targets except NEUTRAL (environmental hazards hurt everyone)
    /// - ALL dealers → NEVER damage NEUTRAL targets (quest NPCs, invulnerable objects protected)
    /// </summary>
    /// <param name="target">The target to check</param>
    /// <param name="dealer">The damage dealer</param>
    /// <returns>True if this dealer can damage this target</returns>
    public static bool IsValidTarget(IDamageable target, IDamageDealer dealer)
    {
        return IsValidTarget(dealer.DealerAllegiance, target);
    }
    
    /// <summary>
    /// Allegiance-based damage targeting rules overload.
    /// Use this when you have an Allegiance directly instead of an IDamageDealer.
    /// </summary>
    /// <param name="dealerAllegiance">The allegiance of the damage dealer</param>
    /// <param name="target">The target to check</param>
    /// <returns>True if this allegiance can damage this target</returns>
    public static bool IsValidTarget(Allegiance dealerAllegiance, IDamageable target)
    {
        Allegiance targetAllegiance = target.GetAllegiance();
        
        // Rule 1: NEVER damage NEUTRAL entities (quest NPCs, invulnerable objects)
        if (targetAllegiance == Allegiance.NEUTRAL)
        {
            return false;
        }
        
        // Rule 2: HOSTILE dealers (enemies) → Damage FRIENDLY targets only
        if (dealerAllegiance == Allegiance.HOSTILE)
        {
            return targetAllegiance == Allegiance.FRIENDLY;
        }
        
        // Rule 3: FRIENDLY dealers (players, turrets) → Damage HOSTILE targets only
        if (dealerAllegiance == Allegiance.FRIENDLY)
        {
            return targetAllegiance == Allegiance.HOSTILE;
        }
        
        // Rule 4: NEUTRAL dealers (environmental hazards) → Damage ALL except NEUTRAL
        if (dealerAllegiance == Allegiance.NEUTRAL)
        {
            return targetAllegiance != Allegiance.NEUTRAL;
        }
        
        // Fallback: No damage (shouldn't reach here)
        return false;
    }
    
    /// <summary>
    /// Check if a GameObject is in the specified layer mask
    /// </summary>
    private static bool IsInLayerMask(GameObject obj, LayerMask layerMask)
    {
        return (layerMask.value & (1 << obj.layer)) != 0;
    }
    
    /// <summary>
    /// Get the allegiance from a transform by checking for IDamageable component
    /// Used to auto-detect allegiance when creating damage areas
    /// </summary>
    /// <param name="source">The transform to check</param>
    /// <returns>The allegiance (defaults to NEUTRAL if not found)</returns>
    public static Allegiance GetAllegianceFromTransform(Transform source)
    {
        if (source == null)
        {
            return Allegiance.NEUTRAL; // No source = environmental hazard
        }
        
        // Check for IDamageable on the source or its parents
        IDamageable damageable = source.GetComponent<IDamageable>();
        if (damageable == null)
        {
            damageable = source.GetComponentInParent<IDamageable>();
        }
        
        if (damageable != null)
        {
            return damageable.GetAllegiance();
        }
        
        // If no IDamageable found, default to NEUTRAL (environmental)
        return Allegiance.NEUTRAL;
    }
}
