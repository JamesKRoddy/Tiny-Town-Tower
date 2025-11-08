using UnityEngine;
using Managers;

/// <summary>
/// Straight-line projectile component that moves in a constant direction
/// Used for bullets, lasers, arrows, etc. that don't arc or home
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class StraightProjectile : MonoBehaviour
{
    private float damage;
    private float poiseDamage;
    private Transform attacker;
    private AttackElement element;
    private float speed;
    private EffectDefinition impactEffect;
    private bool createDamageArea;
    private float damageAreaRadius;
    private float damageAreaDuration;
    private bool useTriggerBasedDamage;
    
    private Rigidbody rb;
    private bool hasHit = false;
    private float maxLifetime = 10f;
    private float timeAlive = 0f;

    /// <summary>
    /// Initialize the straight projectile with damage and effect parameters
    /// </summary>
    public void Initialize(Vector3 direction, float dmg, float poiseDmg, Transform attackTransform,
        AttackElement elem, float projectileSpeed = 20f,
        EffectDefinition impactEff = null, bool createArea = false, float areaRadius = 0f, 
        float areaDuration = 5f, bool triggerBased = false)
    {
        damage = dmg;
        poiseDamage = poiseDmg;
        attacker = attackTransform;
        element = elem;
        speed = projectileSpeed;
        impactEffect = impactEff;
        createDamageArea = createArea;
        damageAreaRadius = areaRadius;
        damageAreaDuration = areaDuration;
        useTriggerBasedDamage = triggerBased;
        hasHit = false;
        timeAlive = 0f;

        // Get or add rigidbody
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Configure rigidbody for projectile behavior
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        
        // Set initial velocity
        rb.linearVelocity = direction.normalized * speed;
        
        // Point projectile in direction of travel
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    void Update()
    {
        if (hasHit) return;

        timeAlive += Time.deltaTime;

        // Destroy after max lifetime to prevent infinite projectiles
        if (timeAlive >= maxLifetime)
        {
            Destroy(gameObject);
        }
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

