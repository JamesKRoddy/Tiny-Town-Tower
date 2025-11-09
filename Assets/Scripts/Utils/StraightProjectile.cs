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

    /// <summary>
    /// Initialize the straight projectile with damage and effect parameters
    /// </summary>
    public void Initialize(Vector3 direction, float dmg, float poiseDmg, Transform attackTransform,
        AttackElement elem, float projectileSpeed = 20f,
        EffectDefinition impactEff = null, bool createArea = false, float areaRadius = 0f, 
        float areaDuration = 5f, bool triggerBased = false)
    {
        // Initialize base parameters (damage, effects, etc.)
        InitializeBase(dmg, poiseDmg, attackTransform, elem, impactEff, createArea, areaRadius, areaDuration, triggerBased);
        
        // Store straight projectile-specific parameters
        speed = projectileSpeed;
        
        // Configure rigidbody for straight projectile behavior
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        
        // Set initial velocity
        rb.linearVelocity = direction.normalized * speed;
        
        // Ensure projectile has a collider for impact detection
        EnsureCollider(0.15f, false);
        
        // Point projectile in direction of travel
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
        
        Debug.Log($"[StraightProjectile] {gameObject.name} initialized | Speed: {speed} | Has Collider: {GetComponent<Collider>() != null}");
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        
        Debug.Log($"[StraightProjectile] {gameObject.name} OnCollisionEnter with {collision.gameObject.name}");
        hasHit = true;

        Vector3 hitPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
        Vector3 hitNormal = collision.contacts.Length > 0 ? collision.contacts[0].normal : Vector3.up;

        HandleImpact(hitPoint, hitNormal, collision.transform);
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
        
        Debug.Log($"[StraightProjectile] {gameObject.name} OnTriggerEnter with {other.gameObject.name}");
        hasHit = true;

        HandleImpact(transform.position, Vector3.up, other.transform);
    }
}

