using Managers;
using UnityEngine;
using Enemies;

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
        animator.SetFloat("HitDirectionX", hitDirection.x);  // Left/Right component
        animator.SetFloat("HitDirectionY", hitDirection.y);  // Forward/Back component
        
        // Trigger the damaged animation
        animator.SetTrigger("Damaged");
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
        animator.SetFloat("HitDirectionX", hitDirection.x);  // Left/Right component
        animator.SetFloat("HitDirectionY", hitDirection.y);  // Forward/Back component
        
        // Trigger the knockback animation
        animator.SetTrigger("Knockback");
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
        animator.SetFloat("HitDirectionX", hitDirection.x);  // Left/Right component
        animator.SetFloat("HitDirectionY", hitDirection.y);  // Forward/Back component
        
        // Set the damage type parameter for elemental animations
        animator.SetInteger("DamageType", (int)damageType);
        
        // Trigger the damaged animation
        animator.SetTrigger("Damaged");
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
        animator.SetFloat("HitDirectionX", hitDirection.x);  // Left/Right component
        animator.SetFloat("HitDirectionY", hitDirection.y);  // Forward/Back component
        
        // Set the damage type parameter for elemental animations
        animator.SetInteger("DamageType", (int)damageType);
        
        // Trigger the knockback animation
        animator.SetTrigger("Knockback");
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
            
            Allegiance allegiance = damageable.GetAllegiance();
            Debug.Log($"[DamageUtils] Found damageable: {hitCollider.name}, Allegiance: {allegiance}");
            
            if (allegiance != Allegiance.FRIENDLY)
            {
                Debug.Log($"[DamageUtils] Skipping {hitCollider.name} - not FRIENDLY allegiance");
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
            
            // Apply damage with elemental type (using unified method)
            Debug.Log($"[DamageUtils] Calling TakeDamage on {hitCollider.name} - Damage: {damageAmount}, Poise: {poiseDamage}, Element: {element}");
            damageable.TakeDamage(damageAmount, poiseDamage, element, attacker);
            
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
        
        // Apply damage with elemental type (using unified method)
        target.TakeDamage(damageAmount, poiseDamage, element, attacker);
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
    /// <param name="visualEffect">Visual effect for the damage area</param>
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
        
        // Add visual effect if provided
        if (visualEffect != null)
        {
            GameObject visualObj = EffectManager.Instance.PlayEffect(position, Vector3.up, Quaternion.identity, 
                damageAreaObj.transform, visualEffect, duration);
            
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
    /// Fire a projectile with arc trajectory using an effect definition
    /// </summary>
    /// <param name="startPosition">Starting position of the projectile</param>
    /// <param name="direction">Direction of the projectile</param>
    /// <param name="rotation">Rotation of the projectile</param>
    /// <param name="targetPosition">Target position for the projectile</param>
    /// <param name="damage">Damage the projectile deals on impact</param>
    /// <param name="poiseDamage">Poise damage the projectile deals on impact</param>
    /// <param name="attacker">The attacker (for damage source tracking)</param>
    /// <param name="element">Elemental type of the damage</param>
    /// <param name="projectileEffect">Effect definition for the projectile visual</param>
    /// <param name="impactEffect">Effect to play on impact</param>
    /// <param name="createDamageArea">Whether to create a damage area on impact</param>
    /// <param name="damageAreaRadius">Radius of the damage area (if createDamageArea is true)</param>
    /// <param name="damageAreaDuration">Duration of the damage area (if createDamageArea is true)</param>
    /// <returns>The spawned projectile GameObject</returns>
    public static GameObject FireProjectileWithEffect(Vector3 startPosition, Vector3 direction, Quaternion rotation,
        Vector3 targetPosition, float damage, float poiseDamage, Transform attacker, AttackElement element,
        EffectDefinition projectileEffect, EffectDefinition impactEffect = null, bool createDamageArea = false,
        float damageAreaRadius = 0f, float damageAreaDuration = 5f, bool useTriggerBasedDamage = false)
    {
        if (projectileEffect == null)
        {
            Debug.LogError("Projectile effect definition is null!");
            return null;
        }
        
        // Play the projectile effect and get the spawned GameObject
        GameObject projectileObj = EffectManager.Instance.PlayEffect(startPosition, direction, rotation, null, projectileEffect);
        
        // Add projectile component to the spawned object
        if (projectileObj != null)
        {
            ArcProjectile projectile = projectileObj.GetComponent<ArcProjectile>();
            if (projectile == null)
            {
                projectile = projectileObj.AddComponent<ArcProjectile>();
            }
            projectile.Initialize(targetPosition, damage, poiseDamage, attacker, element, 10f, 5f, 
                impactEffect, createDamageArea, damageAreaRadius, damageAreaDuration, useTriggerBasedDamage);
        }
        
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
        
        // Apply damage with elemental type (using unified method)
        target.TakeDamage(totalDamage, poiseAmount, dealer.ElementType, dealer.DamageSource);
        
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
        
        // Apply status effects for weapons
        if (dealer is WeaponBase weaponBase)
        {
            ApplyWeaponStatusEffects(weaponBase, target);
        }
        
        // Trigger camera shake for player weapons
        if (dealer is WeaponBase)
        {
            TriggerHitCameraShake(dealer.DamageSource);
        }
    }
    
    /// <summary>
    /// Apply status effects from weapon
    /// </summary>
    private static void ApplyWeaponStatusEffects(WeaponBase weapon, IDamageable target)
    {
        if (weapon.PossibleStatusEffects == null || weapon.PossibleStatusEffects.Length == 0) return;
        if (weapon.StatusEffectChance <= 0f) return;
        
        // Check if status effect should be applied
        if (Random.Range(0f, 1f) <= weapon.StatusEffectChance)
        {
            // Select a random status effect from possible effects
            StatusEffectType selectedEffect = weapon.PossibleStatusEffects[Random.Range(0, weapon.PossibleStatusEffects.Length)];
            
            // Apply the status effect
            ApplyStatusEffect(target, selectedEffect, weapon.StatusEffectDuration);
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
    private static bool IsValidTarget(IDamageable target, IDamageDealer dealer)
    {
        Allegiance targetAllegiance = target.GetAllegiance();
        Allegiance dealerAllegiance = dealer.DealerAllegiance;
        
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
