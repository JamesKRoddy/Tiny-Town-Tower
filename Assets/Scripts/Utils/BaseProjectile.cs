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
        launchTime = Time.time;
        
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
                    null, impactEffect, damageAreaDuration);
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
}

