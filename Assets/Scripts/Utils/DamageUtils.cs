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
        
        // Set damage type parameter for animations
        TriggerElementalDamagedAnimation(animator, hitDirection, damageType);
        
        // Play elemental hit VFX if requested
        if (playHitEffect && damageSource != null)
        {
            var (hitPoint, hitNormal) = CalculateHitPointAndNormal(characterTransform, damageSource);
            EffectManager.Instance.PlayElementalHitEffect(hitPoint, hitNormal, character, damageType);
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
        
        // Find all colliders in the radius
        Collider[] hitColliders = Physics.OverlapSphere(center, radius, layerMask);
        
        foreach (var hitCollider in hitColliders)
        {
            IDamageable damageable = hitCollider.GetComponent<IDamageable>();
            if (damageable != null && damageable.GetAllegiance() == Allegiance.FRIENDLY)
            {
                // Check if the target is still active (this will catch NPCs in bunkers)
                if (!hitCollider.gameObject.activeInHierarchy)
                {
                    continue; // Skip inactive targets
                }
                
                // Exclude self if requested
                if (excludeSelf && hitCollider.transform == attacker)
                {
                    continue;
                }
                
                // Apply damage with elemental type
                if (element == AttackElement.NONE || element == AttackElement.PHYSICAL)
                {
                    damageable.TakeDamage(damageAmount, poiseDamage, attacker);
                }
                else
                {
                    damageable.TakeDamage(damageAmount, poiseDamage, element, attacker);
                }
                
                targetsDamaged++;
            }
        }
        
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
        
        // Apply damage with elemental type
        if (element == AttackElement.NONE || element == AttackElement.PHYSICAL)
        {
            target.TakeDamage(damageAmount, poiseDamage, attacker);
        }
        else
        {
            target.TakeDamage(damageAmount, poiseDamage, element, attacker);
        }
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
    /// <returns>The created damage area GameObject</returns>
    public static GameObject CreateDamageArea(Vector3 position, float radius, float damage, float poiseDamage,
        Transform attacker, AttackElement element = AttackElement.PHYSICAL, float duration = 5f, 
        float damageInterval = 0.5f, EffectDefinition visualEffect = null)
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
        
        // Add the damage area component
        TemporaryDamageArea damageArea = damageAreaObj.AddComponent<TemporaryDamageArea>();
        damageArea.Setup(damage, poiseDamage, duration, element, 0, attacker, damageInterval);
        
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
        
        // Apply damage with elemental type
        if (dealer.ElementType == AttackElement.NONE || dealer.ElementType == AttackElement.PHYSICAL)
        {
            target.TakeDamage(totalDamage, poiseAmount, dealer.DamageSource);
        }
        else
        {
            target.TakeDamage(totalDamage, poiseAmount, dealer.ElementType, dealer.DamageSource);
        }
        
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
    /// </summary>
    private static bool IsValidTarget(IDamageable target, IDamageDealer dealer)
    {
        // For enemy attacks, only damage friendly targets
        if (dealer is AttackBase)
        {
            return target.GetAllegiance() == Allegiance.FRIENDLY;
        }
        
        // For weapons, only damage non-friendly targets
        if (dealer is WeaponBase)
        {
            return target.GetAllegiance() != Allegiance.FRIENDLY;
        }
        
        // Default: allow all targets
        return true;
    }
    
    /// <summary>
    /// Check if a GameObject is in the specified layer mask
    /// </summary>
    private static bool IsInLayerMask(GameObject obj, LayerMask layerMask)
    {
        return (layerMask.value & (1 << obj.layer)) != 0;
    }
}
