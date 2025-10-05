using UnityEngine;
using Managers;
using System.Collections;

/// <summary>
/// Generic projectile component that can be used for any arc-based projectile
/// </summary>
public class ArcProjectile : MonoBehaviour
{
    private Vector3 initialPosition;
    private Vector3 targetPosition;
    private float damage;
    private float poiseDamage;
    private Transform attacker;
    private AttackElement element;
    private float speed;
    private float maxHeight;
    private EffectDefinition impactEffect;
    private bool createDamageArea;
    private float damageAreaRadius;
    private float damageAreaDuration;
    private bool useTriggerBasedDamage;

    private float timeAlive = 0f;
    private bool hasHit = false;
    private float jumpDuration;
    private float maxLifetime = 10f;

    public void Initialize(Vector3 targetPos, float dmg, float poiseDmg, Transform attackTransform, 
        AttackElement elem, float projectileSpeed = 10f, float projectileMaxHeight = 5f,
        EffectDefinition impactEff = null, bool createArea = false, float areaRadius = 0f, float areaDuration = 5f, bool triggerBased = false)
    {
        initialPosition = transform.position;
        targetPosition = targetPos;
        damage = dmg;
        poiseDamage = poiseDmg;
        attacker = attackTransform;
        element = elem;
        speed = projectileSpeed;
        maxHeight = projectileMaxHeight;
        impactEffect = impactEff;
        createDamageArea = createArea;
        damageAreaRadius = areaRadius;
        damageAreaDuration = areaDuration;
        useTriggerBasedDamage = triggerBased;
        timeAlive = 0f;
        hasHit = false;

        // Calculate the duration based on distance and speed
        float distance = Vector3.Distance(
            new Vector3(initialPosition.x, 0, initialPosition.z),
            new Vector3(targetPosition.x, 0, targetPosition.z)
        );
        jumpDuration = distance / speed;
    }

    void Update()
    {
        if (hasHit) return;

        timeAlive += Time.deltaTime;

        // Destroy if exceeded max lifetime
        if (timeAlive >= maxLifetime)
        {
            CreateImpact();
            Destroy(gameObject);
            return;
        }

        float progress = timeAlive / jumpDuration;

        // Calculate the current position in the jump arc
        Vector3 currentPosition = Vector3.Lerp(initialPosition, targetPosition, progress);
        
        // Add vertical movement using a sine wave
        currentPosition.y += Mathf.Sin(progress * Mathf.PI) * maxHeight;

        // Update position
        transform.position = currentPosition;

        // Check if the projectile has hit the ground
        if (transform.position.y <= 0.1f)
        {
            CreateImpact();
            Destroy(gameObject);
        }
    }

    void CreateImpact()
    {
        if (hasHit) return;
        hasHit = true;

        GameObject impactObject = null;

        // Play impact effect
        if (impactEffect != null)
        {
            impactObject = EffectManager.Instance.PlayEffect(transform.position, Vector3.up, Quaternion.identity, null, impactEffect, damageAreaDuration);
            
            // Configure trigger-based damage if requested
            if (useTriggerBasedDamage && impactObject != null)
            {
                ConfigureTriggerDamageComponent(impactObject);
            }
        }

        // Create damage area if requested (fallback for radius-based damage)
        if (createDamageArea && damageAreaRadius > 0)
        {
            DamageUtils.CreateDamageArea(transform.position, damageAreaRadius, damage, poiseDamage, 
                attacker, element, damageAreaDuration);
        }
    }

    /// <summary>
    /// Configure the damage component on the impact effect for trigger-based damage
    /// </summary>
    private void ConfigureTriggerDamageComponent(GameObject impactObject)
    {
        if (impactObject == null) return;
        
        // Find damage area component (including inherited classes like ZombieVomitPool)
        var damageArea = impactObject.GetComponent<DamageArea>();
        if (damageArea == null)
        {
            damageArea = impactObject.GetComponentInChildren<DamageArea>();
        }
        
        if (damageArea != null)
        {
            // Configure the damage area with our attack parameters
            damageArea.SetDamage(damage, poiseDamage);
            
            Debug.Log($"[ArcProjectile] Configured trigger-based damage component ({damageArea.GetType().Name}) with damage: {damage}, poise: {poiseDamage}");
        }
        else
        {
            Debug.LogWarning($"[ArcProjectile] No DamageArea component found on impact effect for trigger-based damage");
        }
    }

    // Optional: Visualize the projectile path in editor
    private void OnDrawGizmos()
    {
        if (Application.isPlaying && !hasHit)
        {
            // Draw projectile path
            Gizmos.color = Color.yellow;
            int segments = 20;
            for (int i = 0; i < segments; i++)
            {
                float progress = i / (float)segments;
                Vector3 point = Vector3.Lerp(initialPosition, targetPosition, progress);
                point.y += Mathf.Sin(progress * Mathf.PI) * maxHeight;
                Gizmos.DrawSphere(point, 0.2f);
            }

            // Draw target position
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPosition, 0.5f);
        }
    }
}
