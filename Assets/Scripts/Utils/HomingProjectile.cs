using UnityEngine;
using Managers;

/// <summary>
/// Homing projectile component that tracks and follows a target.
/// Generic implementation that can be used by enemies, NPCs, turrets, etc.
/// 
/// The projectile will smoothly rotate towards the target for a specified duration,
/// then either explode or fall to the ground.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class HomingProjectile : MonoBehaviour
{
    // Damage parameters
    private float damage;
    private float poiseDamage;
    private Transform attacker;
    private AttackElement element;
    private EffectDefinition impactEffect;
    private bool createDamageArea;
    private float damageAreaRadius;
    private float damageAreaDuration;
    private bool useTriggerBasedDamage;
    
    // Homing parameters
    private Transform target;
    private float homingDuration;
    private float turnSpeed;
    private float speed;
    private float baseSpeed;
    private bool explodeOnTimeout;
    
    // State tracking
    private Rigidbody rb;
    private bool hasHit = false;
    private bool isTracking = false;
    private float launchTime;
    private Vector3 currentDirection;
    private float maxLifetime = 10f;

    /// <summary>
    /// Initialize the homing projectile with damage, tracking, and effect parameters
    /// </summary>
    /// <param name="targetTransform">Target to track</param>
    /// <param name="dmg">Damage on impact</param>
    /// <param name="poiseDmg">Poise damage on impact</param>
    /// <param name="attackTransform">Source of the attack</param>
    /// <param name="elem">Elemental type</param>
    /// <param name="projectileSpeed">Movement speed</param>
    /// <param name="duration">How long to track target (seconds)</param>
    /// <param name="turnSpeedDegrees">Turn rate (degrees/second)</param>
    /// <param name="explodeWhenExpired">Explode when tracking expires</param>
    /// <param name="impactEff">Impact effect definition</param>
    /// <param name="createArea">Create damage area on impact</param>
    /// <param name="areaRadius">Damage area radius</param>
    /// <param name="areaDuration">Damage area duration</param>
    /// <param name="triggerBased">Use trigger-based damage</param>
    public void Initialize(
        Transform targetTransform,
        float dmg,
        float poiseDmg,
        Transform attackTransform,
        AttackElement elem,
        float projectileSpeed = 15f,
        float duration = 3f,
        float turnSpeedDegrees = 180f,
        bool explodeWhenExpired = true,
        EffectDefinition impactEff = null,
        bool createArea = false,
        float areaRadius = 0f,
        float areaDuration = 5f,
        bool triggerBased = false)
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
        
        // Store homing parameters
        target = targetTransform;
        homingDuration = duration;
        turnSpeed = turnSpeedDegrees;
        speed = projectileSpeed;
        baseSpeed = projectileSpeed;
        explodeOnTimeout = explodeWhenExpired;
        
        // Initialize state
        hasHit = false;
        isTracking = true;
        launchTime = Time.time;
        currentDirection = transform.forward;
        
        // Get or add rigidbody
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Configure rigidbody for homing behavior
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.linearVelocity = currentDirection * speed;
        
        // Point projectile in direction of travel
        if (currentDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(currentDirection);
        }
    }

    void Update()
    {
        if (hasHit) return;
        
        // Destroy after max lifetime to prevent infinite projectiles
        float timeAlive = Time.time - launchTime;
        if (timeAlive >= maxLifetime)
        {
            Destroy(gameObject);
        }
    }

    void FixedUpdate()
    {
        if (!isTracking || hasHit) return;
        
        // Check if tracking duration has expired
        float timeAlive = Time.time - launchTime;
        if (timeAlive >= homingDuration)
        {
            StopTracking();
            return;
        }
        
        // Update tracking behavior
        UpdateHoming(timeAlive);
        
        // Move the missile forward
        if (rb != null)
        {
            rb.linearVelocity = currentDirection * speed;
        }
        else
        {
            transform.position += currentDirection * speed * Time.fixedDeltaTime;
        }
    }

    /// <summary>
    /// Update the homing behavior to track the target
    /// </summary>
    private void UpdateHoming(float timeAlive)
    {
        // Check if target is still valid
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            // Target destroyed, continue in current direction
            Debug.Log($"[{gameObject.name}] Target lost, continuing forward");
            isTracking = false;
            return;
        }
        
        // Calculate direction to target
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        
        // Smoothly rotate towards target
        float rotationSpeed = turnSpeed * Time.fixedDeltaTime;
        Vector3 newDirection = Vector3.RotateTowards(currentDirection, directionToTarget, rotationSpeed * Mathf.Deg2Rad, 0f);
        currentDirection = newDirection.normalized;
        
        // Update transform rotation to match direction
        if (currentDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(currentDirection);
        }
        
        // Slightly increase speed as missile tracks (feels more aggressive)
        float speedMultiplier = 1f + (timeAlive / homingDuration) * 0.2f; // Up to 20% speed increase
        speed = baseSpeed * speedMultiplier;
    }

    /// <summary>
    /// Stop tracking and handle timeout behavior
    /// </summary>
    private void StopTracking()
    {
        isTracking = false;
        
        if (explodeOnTimeout)
        {
            Debug.Log($"[{gameObject.name}] Homing duration expired, exploding");
            // The projectile should explode via its normal collision/trigger system
            // or we could force trigger it here
            TriggerExplosion();
        }
        else
        {
            Debug.Log($"[{gameObject.name}] Homing duration expired, falling");
            // Enable gravity so missile falls
            if (rb != null)
            {
                rb.useGravity = true;
            }
        }
    }

    /// <summary>
    /// Force trigger explosion (used when tracking expires)
    /// </summary>
    private void TriggerExplosion()
    {
        // The projectile behavior should handle explosion via collision
        // We can force it by triggering collision with ground or by direct call
        // For now, just let it hit something or fall
        
        // Optionally: Destroy after a delay if it hasn't hit anything
        Destroy(gameObject, 2f);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        hasHit = true;

        Vector3 hitPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
        Vector3 hitNormal = collision.contacts.Length > 0 ? collision.contacts[0].normal : Vector3.up;

        HandleImpact(hitPoint, hitNormal, collision.transform);
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
        hasHit = true;

        HandleImpact(transform.position, Vector3.up, other.transform);
    }

    /// <summary>
    /// Handle impact effects and damage
    /// </summary>
    private void HandleImpact(Vector3 hitPoint, Vector3 hitNormal, Transform hitTransform)
    {
        // Stop tracking and movement
        isTracking = false;
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
                var damageInfo = new DamageInfo
                {
                    Amount = damage,
                    PoiseDamage = poiseDamage,
                    ElementType = element,
                    SourceTransform = attacker,
                    DamageDealer = null,  // Projectile itself isn't a damage dealer
                    PlayHitVFX = true,
                    IsEnvironmentalDamage = false
                };
                damageable.TakeDamage(damageInfo);
            }
        }

        // Create damage area if configured
        if (createDamageArea)
        {
            if (useTriggerBasedDamage && impactEffect != null)
            {
                // Spawn trigger-based damage effect
                EffectManager.Instance?.PlayEffect(hitPoint, hitNormal, Quaternion.LookRotation(hitNormal), 
                    null, impactEffect, damageAreaDuration);
            }
            else
            {
                float radius = damageAreaRadius > 0f ? damageAreaRadius : 1.5f;
                DamageUtils.CreateDamageArea(
                    hitPoint,
                    radius,
                    damage,
                    poiseDamage,
                    attacker,
                    element,
                    damageAreaDuration);

                // Optional impact VFX
                if (impactEffect != null)
                {
                    EffectManager.Instance?.PlayEffect(hitPoint, hitNormal, Quaternion.LookRotation(hitNormal), 
                        null, impactEffect);
                }
            }
        }
        else if (impactEffect != null)
        {
            // Just play impact effect
            EffectManager.Instance?.PlayEffect(hitPoint, hitNormal, Quaternion.LookRotation(hitNormal), 
                null, impactEffect);
        }

        // Destroy projectile
        Destroy(gameObject);
    }
}


