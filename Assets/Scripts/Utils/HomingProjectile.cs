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
public class HomingProjectile : BaseProjectile
{
    // Homing-specific parameters
    private Transform target;
    private float homingDuration;
    private float turnSpeed;
    private float speed;
    private float baseSpeed;
    private bool explodeOnTimeout;
    
    // Homing-specific state
    private bool isTracking = false;
    private Vector3 currentDirection;

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
        // Initialize base parameters (damage, effects, etc.)
        InitializeBase(dmg, poiseDmg, attackTransform, elem, impactEff, createArea, areaRadius, areaDuration, triggerBased);
        
        // Store homing-specific parameters
        target = targetTransform;
        homingDuration = duration;
        turnSpeed = turnSpeedDegrees;
        speed = projectileSpeed;
        baseSpeed = projectileSpeed;
        explodeOnTimeout = explodeWhenExpired;
        
        // Initialize homing-specific state
        isTracking = true;
        currentDirection = transform.forward;
        
        // Configure rigidbody for homing behavior
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.linearVelocity = currentDirection * speed;
        
        // Ensure projectile has a collider for impact detection
        EnsureCollider(0.2f, false);
        
        // Point projectile in direction of travel
        if (currentDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(currentDirection);
        }
        
        Debug.Log($"[HomingProjectile] {gameObject.name} initialized | Target: {targetTransform?.name} | Speed: {speed} | Duration: {duration}s | Has Collider: {GetComponent<Collider>() != null} | CreateDamageArea: {createArea} | Radius: {areaRadius} | UseTrigger: {triggerBased}");
    }

    protected override void Update()
    {
        base.Update(); // Call base class Update for lifetime management
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
        
        Debug.Log($"[HomingProjectile] {gameObject.name} OnCollisionEnter with {collision.gameObject.name} (Layer: {LayerMask.LayerToName(collision.gameObject.layer)}) | Time Alive: {Time.time - launchTime:F2}s | Distance to target: {(target != null ? Vector3.Distance(transform.position, target.position) : -1):F2}m");
        hasHit = true;
        isTracking = false; // Stop tracking on impact

        Vector3 hitPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
        Vector3 hitNormal = collision.contacts.Length > 0 ? collision.contacts[0].normal : Vector3.up;

        HandleImpact(hitPoint, hitNormal, collision.transform);
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
        
        Debug.Log($"[HomingProjectile] {gameObject.name} OnTriggerEnter with {other.gameObject.name}");
        hasHit = true;
        isTracking = false; // Stop tracking on impact

        HandleImpact(transform.position, Vector3.up, other.transform);
    }
}


