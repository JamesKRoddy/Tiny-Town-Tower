using UnityEngine;
using Managers;

/// <summary>
/// Base class for all projectile types (straight, arc, homing, etc.)
/// Provides shared initialization, damage parameters, and impact handling
/// Implements IHittable for weapon reflection
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public abstract class BaseProjectile : MonoBehaviour, IHittable
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
    protected float armingDelay = 0.2f; // Delay before projectile can cause damage (allows time for reflection/dodge)
    
    // Explosion behavior control
    [Tooltip("If true, explode on any collision. If false, only explode on ground hit")]
    protected bool explodeOnAnyHit = true; // If true, explode on any collision. If false, only explode on ground hit
    
    // Reflection tracking (for player hit reflection)
    protected bool isReflected = false;
    protected Vector3 originPosition; // Original launch position (attacker position)

    /// <summary>
    /// Initialize common projectile parameters
    /// </summary>
    protected virtual void InitializeBase(float dmg, float poiseDmg, Transform attackTransform,
        AttackElement elem, EffectDefinition impactEff, bool createArea, float areaRadius,
        float areaDuration, bool triggerBased, float armingDelay = 0.2f, bool explodeOnAnyHit = true)
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
        this.armingDelay = armingDelay;
        this.explodeOnAnyHit = explodeOnAnyHit;
        
        // Store origin position (attacker position) for reflection
        originPosition = attackTransform != null ? attackTransform.position : transform.position;
        
        // Get or add rigidbody
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Log collider setup for debugging reflection detection
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Debug.Log($"[{GetType().Name}] {gameObject.name} initialized with collider: {col.GetType().Name} | Layer: {LayerMask.LayerToName(gameObject.layer)} ({gameObject.layer}) | IsTrigger: {col.isTrigger} | Position: {transform.position}");
        }
        else
        {
            Debug.LogWarning($"[{GetType().Name}] {gameObject.name} initialized WITHOUT collider! Reflection detection may fail!");
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
    /// Check if the projectile is armed (can cause damage)
    /// Projectiles have a brief arming delay after launch to allow reflection/dodge time
    /// </summary>
    /// <returns>True if the projectile is armed and can cause damage</returns>
    protected bool IsArmed()
    {
        float timeAlive = Time.time - launchTime;
        return timeAlive >= armingDelay;
    }
    
    /// <summary>
    /// Handle impact effects and damage (shared by all projectile types)
    /// </summary>
    protected virtual void HandleImpact(Vector3 hitPoint, Vector3 hitNormal, Transform hitTransform)
    {
        // Check if projectile is armed before causing damage
        if (!IsArmed() && !isReflected)
        {
            // Projectile is not armed yet - can still be reflected, but won't cause damage
            Debug.Log($"[{GetType().Name}] {gameObject.name} hit target before arming delay ({Time.time - launchTime:F3}s < {armingDelay:F3}s) - no damage dealt");
            return;
        }
        
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
    /// Override in derived classes to implement reflection behavior and VFX
    /// </summary>
    /// <param name="hitPoint">Position where the reflection occurred (for VFX)</param>
    /// <param name="hitNormal">Normal vector at the hit point (for VFX)</param>
    protected virtual void OnReflected(Vector3 hitPoint = default, Vector3 hitNormal = default)
    {
        isReflected = true;
        Debug.Log($"[{GetType().Name}] {gameObject.name} reflected by player, returning to origin at {originPosition}");
        
        // Play reflection VFX (handled by projectile classes if they override this)
        if (hitPoint != default && hitNormal != default)
        {
            PlayReflectionVFX(hitPoint, hitNormal);
        }
    }
    
    /// <summary>
    /// Play the reflection VFX at the hit point
    /// Override in derived classes to use their own VFX system
    /// </summary>
    protected virtual void PlayReflectionVFX(Vector3 hitPoint, Vector3 hitNormal)
    {
        // Default implementation - can be overridden by derived classes
        // Uses CharacterCombat static method to find and play VFX
        CharacterCombat.PlayReflectionVFX(hitPoint, hitNormal);
    }

    // ===== IHITTABLE IMPLEMENTATION =====
    
    /// <summary>
    /// Called when this projectile is hit (implements IHittable)
    /// Handles reflection logic
    /// </summary>
    public virtual void OnHit(HitInfo hitInfo)
    {
        if (!CanBeHit()) return;
        
        // Reflect the projectile
        OnReflected(hitInfo.HitPoint, hitInfo.HitNormal);
    }
    
    /// <summary>
    /// Check if this projectile can currently be hit/reflected (implements IHittable)
    /// </summary>
    public virtual bool CanBeHit()
    {
        return !isReflected && !hasHit;
    }
    
    /// <summary>
    /// Public method to reflect projectile when hit by player weapon
    /// Called from DamageUtils.PerformProjectileReflectionDetection when melee weapon hits projectile
    /// DEPRECATED: Use OnHit() via IHittable interface instead
    /// </summary>
    /// <param name="hitPoint">Position where the reflection occurred (for VFX)</param>
    /// <param name="hitNormal">Normal vector at the hit point (for VFX)</param>
    public virtual void ReflectByPlayer(Vector3 hitPoint = default, Vector3 hitNormal = default)
    {
        if (isReflected || hasHit) return; // Don't reflect if already reflected or hit
        
        OnReflected(hitPoint, hitNormal);
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

