using UnityEngine;
using Managers;

/// <summary>
/// Straight-line projectile component that moves in a constant direction
/// Used for bullets, lasers, arrows, etc. that don't arc or home
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class StraightProjectile : BaseProjectile
{
    private float speed;
    private float originalSpeed; // Store original speed for reflection speed boost
    private Vector3 currentDirection;

    /// <summary>
    /// Initialize the straight projectile with damage and effect parameters
    /// </summary>
    public void Initialize(Vector3 direction, float dmg, float poiseDmg, Transform attackTransform,
        AttackElement elem, float projectileSpeed = 20f,
        EffectDefinition impactEff = null, bool createArea = false, float areaRadius = 0f, 
        float areaDuration = 5f, bool triggerBased = false, float armingDelay = 0.2f)
    {
        // Initialize base parameters (damage, effects, etc.)
        InitializeBase(dmg, poiseDmg, attackTransform, elem, impactEff, createArea, areaRadius, areaDuration, triggerBased, armingDelay);
        
        // Store straight projectile-specific parameters
        speed = projectileSpeed;
        originalSpeed = projectileSpeed; // Store original speed for reflection speed boost
        currentDirection = direction.normalized;
        
        // Configure rigidbody for straight projectile behavior
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        
        // Set initial velocity
        rb.linearVelocity = currentDirection * speed;
        
        // Ensure projectile has a collider for impact detection
        // Use trigger for player hit detection (will still trigger OnCollisionEnter for non-trigger collisions)
        EnsureCollider(0.15f, false);
        
        // Also ensure we have a trigger collider for player detection
        EnsureTriggerColliderForReflection();
        
        // Point projectile in direction of travel
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
        
        Debug.Log($"[StraightProjectile] {gameObject.name} initialized | Speed: {speed} | Has Collider: {GetComponent<Collider>() != null}");
    }

    /// <summary>
    /// Ensure projectile has a trigger collider for player reflection detection
    /// (separate from the main collision collider)
    /// </summary>
    private void EnsureTriggerColliderForReflection()
    {
        // Check if we already have a trigger collider
        Collider[] colliders = GetComponents<Collider>();
        bool hasTrigger = false;
        foreach (var col in colliders)
        {
            if (col.isTrigger)
            {
                hasTrigger = true;
                break;
            }
        }

        // Add a trigger collider if we don't have one (for player detection)
        if (!hasTrigger)
        {
            SphereCollider triggerCol = gameObject.AddComponent<SphereCollider>();
            triggerCol.radius = 0.2f;
            triggerCol.isTrigger = true;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        
        // Check if hit by player first (before normal impact) - don't check isReflected here to allow reflection
        if (!isReflected && IsHitByPlayer(collision.transform))
        {
            ReflectProjectile();
            return; // Don't impact, reflect instead
        }
        
        // If reflected, only process collisions with the attacker (enemy that fired it)
        if (isReflected)
        {
            bool isAttacker = attacker != null && (collision.gameObject == attacker.gameObject || collision.transform.IsChildOf(attacker));
            if (isAttacker)
            {
                // Hit the attacker, create impact
                Debug.Log($"[StraightProjectile] {gameObject.name} reflected projectile hit attacker {attacker.name}");
                hasHit = true;
                Vector3 impactPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
                Vector3 impactNormal = collision.contacts.Length > 0 ? collision.contacts[0].normal : Vector3.up;
                HandleImpact(impactPoint, impactNormal, attacker);
                return;
            }
            // Ignore other collisions when reflected (let distance check handle impact)
            return;
        }
        
        Debug.Log($"[StraightProjectile] {gameObject.name} OnCollisionEnter with {collision.gameObject.name}");
        hasHit = true;

        Vector3 hitPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
        Vector3 hitNormal = collision.contacts.Length > 0 ? collision.contacts[0].normal : Vector3.up;

        HandleImpact(hitPoint, hitNormal, collision.transform);
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
        
        // Only process player collisions in OnTriggerEnter (trigger collider is for player reflection)
        // Ground, walls, and other solid objects should be handled by OnCollisionEnter instead
        if (!isReflected && IsHitByPlayer(other))
        {
            ReflectProjectile();
            return; // Reflect instead of impacting
        }
        
        // Ignore all other trigger collisions (ground, walls, etc.) - they should use OnCollisionEnter
        // Don't process impact here - let OnCollisionEnter handle non-player collisions
    }

    void FixedUpdate()
    {
        if (hasHit) return;

        // Handle reflected projectile movement
        if (isReflected)
        {
            // Reverse direction back to origin
            Vector3 toOrigin = (GetOriginPosition() - transform.position).normalized;
            currentDirection = toOrigin;
            rb.linearVelocity = currentDirection * speed;
            
            // Rotate towards origin
            if (toOrigin != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(toOrigin);
            }
            
            // Check if reached origin (or close enough)
            float distanceToOrigin = Vector3.Distance(transform.position, GetOriginPosition());
            if (distanceToOrigin < 1.0f) // Increased threshold to ensure impact triggers
            {
                // Reached origin, create impact
                Vector3 impactPos = GetOriginPosition();
                Debug.Log($"[StraightProjectile] {gameObject.name} reached attacker at distance {distanceToOrigin:F2}m, creating impact");
                HandleImpact(impactPos, Vector3.up, attacker);
                return;
            }
        }
    }

    /// <summary>
    /// Reflect the projectile back to its origin (called when hit by player)
    /// </summary>
    private void ReflectProjectile()
    {
        if (isReflected) return; // Already reflected
        
        OnReflected(); // Call base method to set isReflected flag
        
        // Increase speed by 1.5x when reflected
        speed = originalSpeed * 1.5f;
        
        // Reverse direction immediately
        Vector3 toOrigin = (GetOriginPosition() - transform.position).normalized;
        currentDirection = toOrigin;
        rb.linearVelocity = currentDirection * speed;
        
        // Rotate towards origin
        if (toOrigin != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(toOrigin);
        }
        
        Debug.Log($"[StraightProjectile] {gameObject.name} reflected by player, returning to origin at {GetOriginPosition()} | Speed increased to {speed} (1.5x original)");
    }

    /// <summary>
    /// Override to call private ReflectProjectile method which handles direction reversal
    /// </summary>
    public override void ReflectByPlayer(Vector3 hitPoint = default, Vector3 hitNormal = default)
    {
        if (isReflected || hasHit) return; // Don't reflect if already reflected or hit
        
        // Play reflection VFX if hit point/normal provided
        if (hitPoint != default && hitNormal != default)
        {
            CharacterCombat.PlayReflectionVFX(hitPoint, hitNormal);
        }
        else
        {
            // Use projectile position as fallback
            Vector3 fallbackHitPoint = transform.position;
            Vector3 fallbackHitNormal = -transform.forward;
            CharacterCombat.PlayReflectionVFX(fallbackHitPoint, fallbackHitNormal);
        }
        
        ReflectProjectile();
    }
}

