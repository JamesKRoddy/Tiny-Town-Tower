using UnityEngine;
using Managers;

/// <summary>
/// Base class for all projectile types (straight, arc, homing, etc.)
/// Provides shared initialization, damage parameters, and impact handling
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public abstract class BaseProjectile : MonoBehaviour
{
    // Damage parameters (shared by all projectile types)
    protected float damage;
    protected float poiseDamage;
    protected Transform attacker;
    protected AttackElement element;
    protected EffectDefinition impactEffect;
    protected bool createDamageArea;
    protected float damageAreaRadius;
    protected float damageAreaDuration;
    protected bool useTriggerBasedDamage;
    
    // State tracking
    protected Rigidbody rb;
    protected bool hasHit = false;
    protected float launchTime;
    protected float maxLifetime = 10f;
    
    // Reflection tracking (for player hit reflection)
    protected bool isReflected = false;
    protected Vector3 originPosition; // Original launch position (attacker position)

    /// <summary>
    /// Initialize common projectile parameters
    /// </summary>
    protected virtual void InitializeBase(float dmg, float poiseDmg, Transform attackTransform,
        AttackElement elem, EffectDefinition impactEff, bool createArea, float areaRadius,
        float areaDuration, bool triggerBased)
    {
        // Store damage parameters
        damage = dmg;
        poiseDamage = poiseDmg;
        attacker = attackTransform;
        element = elem;
        impactEffect = impactEff;
        createDamageArea = createArea;
        damageAreaRadius = areaRadius;
        damageAreaDuration = areaDuration;
        useTriggerBasedDamage = triggerBased;
        
        // Initialize state
        hasHit = false;
        isReflected = false;
        launchTime = Time.time;
        
        // Store origin position (attacker position) for reflection
        originPosition = attackTransform != null ? attackTransform.position : transform.position;
        
        // Get or add rigidbody
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
    }

    /// <summary>
    /// Ensure the projectile has a collider for impact detection
    /// </summary>
    protected virtual void EnsureCollider(float defaultRadius = 0.2f, bool isTrigger = false)
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            SphereCollider sphereCol = gameObject.AddComponent<SphereCollider>();
            sphereCol.radius = defaultRadius;
            sphereCol.isTrigger = isTrigger;
            Debug.LogWarning($"[{GetType().Name}] {gameObject.name} missing collider, added SphereCollider automatically");
        }
    }

    /// <summary>
    /// Handle impact effects and damage (shared by all projectile types)
    /// </summary>
    protected virtual void HandleImpact(Vector3 hitPoint, Vector3 hitNormal, Transform hitTransform)
    {
        Debug.Log($"[{GetType().Name}] {gameObject.name} HandleImpact called | HitPoint: {hitPoint} | CreateDamageArea: {createDamageArea} | Radius: {damageAreaRadius} | Damage: {damage}");
        
        // Stop movement
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // Apply direct hit damage
        if (hitTransform != null)
        {
            IDamageable damageable = hitTransform.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                Debug.Log($"[{GetType().Name}] Applying direct hit damage to {hitTransform.name}");
                var damageInfo = new DamageInfo
                {
                    Amount = damage,
                    PoiseDamage = poiseDamage,
                    ElementType = element,
                    SourceTransform = attacker,
                    DamageDealer = null,
                    PlayHitVFX = true,
                    IsEnvironmentalDamage = false
                };
                damageable.TakeDamage(damageInfo);
            }
            else
            {
                Debug.Log($"[{GetType().Name}] {hitTransform.name} has no IDamageable component");
            }
        }

        // Create damage area if configured
        Debug.Log($"[{GetType().Name}] Checking damage area: createDamageArea={createDamageArea}, useTriggerBasedDamage={useTriggerBasedDamage}, impactEffect={impactEffect?.name ?? "null"}");
        if (createDamageArea)
        {
            if (useTriggerBasedDamage && impactEffect != null)
            {
                // Spawn trigger-based damage effect (lingering damage zone)
                Debug.Log($"[{GetType().Name}] Creating trigger-based damage area");
                EffectManager.Instance?.PlayEffect(hitPoint, hitNormal, Quaternion.identity, 
                    null, impactEffect, duration: damageAreaDuration);
            }
            else
            {
                // Use instant radius-based damage (explosion)
                float radius = damageAreaRadius > 0f ? damageAreaRadius : 1.5f;
                Debug.Log($"[{GetType().Name}] Creating INSTANT damage area with radius {radius}");
                
                // Use CreateInstantDamageArea for immediate explosion damage
                int targetsDamaged = DamageUtils.CreateInstantDamageArea(
                    hitPoint,
                    radius,
                    damage,
                    poiseDamage,
                    attacker,
                    element,
                    impactEffect);
                
                Debug.Log($"[{GetType().Name}] Instant damage area hit {targetsDamaged} targets");
            }
        }
        else if (impactEffect != null)
        {
            // Just play impact effect (no damage area)
            EffectManager.Instance?.PlayEffect(hitPoint, hitNormal, Quaternion.identity, 
                null, impactEffect);
        }

        // Destroy projectile
        Destroy(gameObject);
    }

    protected virtual void Update()
    {
        if (hasHit) return;
        
        // Destroy after max lifetime to prevent infinite projectiles
        float timeAlive = Time.time - launchTime;
        if (timeAlive >= maxLifetime)
        {
            Destroy(gameObject);
        }
    }

    // ===== REFLECTION SYSTEM (STANDARDIZED) =====

    /// <summary>
    /// Check if a collider/transform belongs to the player or player weapon
    /// Standardized detection used by all projectile types
    /// </summary>
    /// <param name="other">The collider or transform to check</param>
    /// <returns>True if hit by player or player weapon</returns>
    protected bool IsHitByPlayer(Collider other)
    {
        if (other == null) return false;

        // Check by layer
        if (other.gameObject.layer == GameConstants.Layers.PlayerLayer ||
            other.gameObject.layer == GameConstants.Layers.WeaponLayer)
        {
            return true;
        }

        // Check by tag
        if (other.CompareTag(GameConstants.Tags.Player))
        {
            return true;
        }

        // Check if parent is player or weapon
        Transform parent = other.transform.parent;
        while (parent != null)
        {
            if (parent.gameObject.layer == GameConstants.Layers.PlayerLayer ||
                parent.CompareTag(GameConstants.Tags.Player))
            {
                return true;
            }
            parent = parent.parent;
        }

        return false;
    }

    /// <summary>
    /// Check if a transform belongs to the player or player weapon
    /// Standardized detection used by all projectile types
    /// </summary>
    /// <param name="other">The transform to check</param>
    /// <returns>True if hit by player or player weapon</returns>
    protected bool IsHitByPlayer(Transform other)
    {
        if (other == null) return false;

        // Check by layer
        if (other.gameObject.layer == GameConstants.Layers.PlayerLayer ||
            other.gameObject.layer == GameConstants.Layers.WeaponLayer)
        {
            return true;
        }

        // Check by tag
        if (other.CompareTag(GameConstants.Tags.Player))
        {
            return true;
        }

        // Check if parent is player or weapon
        Transform parent = other.parent;
        while (parent != null)
        {
            if (parent.gameObject.layer == GameConstants.Layers.PlayerLayer ||
                parent.CompareTag(GameConstants.Tags.Player))
            {
                return true;
            }
            parent = parent.parent;
        }

        return false;
    }

    /// <summary>
    /// Called when projectile is reflected by player hit
    /// Override in derived classes to implement reflection behavior
    /// </summary>
    protected virtual void OnReflected()
    {
        isReflected = true;
        Debug.Log($"[{GetType().Name}] {gameObject.name} reflected by player, returning to origin at {originPosition}");
    }

    /// <summary>
    /// Public method to reflect projectile when hit by player weapon
    /// Called from DamageUtils.PerformBoxCastDamage when melee weapon hits projectile
    /// </summary>
    public virtual void ReflectByPlayer()
    {
        if (isReflected || hasHit) return; // Don't reflect if already reflected or hit
        
        OnReflected();
    }

    /// <summary>
    /// Get the origin position (where projectile was launched from)
    /// When reflected, returns the attacker's target transform position if available (so projectile tracks moving enemies)
    /// Uses IDamageable.GetTargetTransform() to get the mesh/target transform for proper VFX and targeting
    /// Otherwise returns the stored origin position
    /// Used for reflection calculations
    /// </summary>
    /// <returns>Origin position (attacker's target transform position if reflected, otherwise launch position)</returns>
    protected Vector3 GetOriginPosition()
    {
        // When reflected, try to track the attacker's target transform position
        // This allows projectiles to follow moving enemies back to their mesh/target position
        if (isReflected && attacker != null)
        {
            // Check if attacker implements IDamageable to get the target transform (mesh)
            IDamageable damageable = attacker.GetComponent<IDamageable>();
            if (damageable != null)
            {
                Transform targetTransform = damageable.GetTargetTransform();
                if (targetTransform != null)
                {
                    return targetTransform.position;
                }
            }
            // Fallback to attacker's position if not IDamageable or no target transform
            return attacker.position;
        }
        
        // Otherwise, use the stored origin position (where projectile was launched from)
        return originPosition;
    }
}

