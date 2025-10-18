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
            // Check if this is a ZombieVomitPool that needs special setup
            var vomitPool = damageArea as ZombieVomitPool;
            if (vomitPool != null)
            {
                // Call the full Setup method to trigger the scaling animation
                float scaleDuration = damageAreaDuration > 0 ? Mathf.Min(0.5f, damageAreaDuration * 0.2f) : 0.5f;
                
                // Calculate scale based on radius or use prefab's collider size if radius is 0 (trigger-based)
                Vector3 scale;
                if (damageAreaRadius > 0)
                {
                    // Radius-based: use the provided radius
                    scale = new Vector3(damageAreaRadius * 2f, 0.3f, damageAreaRadius * 2f);
                }
                else
                {
                    // Trigger-based: check if there's a serialized targetScale value on the prefab
                    // First, try to get the target scale directly from the ZombieVomitPool component
                    // This will use the value set in the Inspector on the prefab
                    System.Reflection.FieldInfo targetScaleField = vomitPool.GetType().GetField("targetScale", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (targetScaleField != null)
                    {
                        Vector3 prefabTargetScale = (Vector3)targetScaleField.GetValue(vomitPool);
                        // Only use it if it's not zero (meaning it was set in the prefab)
                        if (prefabTargetScale != Vector3.zero && prefabTargetScale.x > 0.1f)
                        {
                            scale = prefabTargetScale;
                            Debug.Log($"[ArcProjectile] Using prefab's targetScale: {scale}");
                        }
                        else
                        {
                            // Fallback to a default reasonable size
                            scale = new Vector3(2f, 0.3f, 2f);
                            Debug.Log($"[ArcProjectile] Prefab targetScale was zero, using default scale: {scale}");
                        }
                    }
                    else
                    {
                        // Fallback to a default reasonable size
                        scale = new Vector3(2f, 0.3f, 2f);
                        Debug.LogWarning($"[ArcProjectile] Could not read targetScale from prefab, using default scale: {scale}");
                    }
                }
                
                // Auto-detect allegiance from attacker (enemy projectiles are HOSTILE)
                Allegiance vomitAllegiance = DamageUtils.GetAllegianceFromTransform(attacker);
                
                vomitPool.Setup(damage, poiseDamage, damageAreaDuration, scaleDuration, scale, element, 0, attacker, vomitAllegiance);
                
                Debug.Log($"[ArcProjectile] Configured ZombieVomitPool with damage: {damage}, poise: {poiseDamage}, duration: {damageAreaDuration}, element: {element}, scale: {scale}, allegiance: {vomitAllegiance}");
            }
            // Check if this is a TemporaryDamageArea (but not ZombieVomitPool)
            else if (damageArea as TemporaryDamageArea != null)
            {
                var tempArea = damageArea as TemporaryDamageArea;
                
                // Auto-detect allegiance from attacker
                Allegiance areaAllegiance = DamageUtils.GetAllegianceFromTransform(attacker);
                
                // Call the full Setup method
                tempArea.Setup(damage, poiseDamage, damageAreaDuration, element, 0, attacker, 1f, areaAllegiance);
                
                Debug.Log($"[ArcProjectile] Configured TemporaryDamageArea with damage: {damage}, poise: {poiseDamage}, duration: {damageAreaDuration}, element: {element}, allegiance: {areaAllegiance}");
            }
            else
            {
                // For regular DamageArea, just set the damage and elemental properties
                damageArea.SetDamage(damage, poiseDamage);
                damageArea.SetElementalProperties(element, 0);
                
                // Auto-detect and set allegiance
                Allegiance areaAllegiance = DamageUtils.GetAllegianceFromTransform(attacker);
                damageArea.SetAllegiance(areaAllegiance);
                
                if (attacker != null)
                {
                    damageArea.SetDamageSource(attacker);
                }
                
                Debug.Log($"[ArcProjectile] Configured trigger-based damage component ({damageArea.GetType().Name}) with damage: {damage}, poise: {poiseDamage}, element: {element}, allegiance: {areaAllegiance}");
            }
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
