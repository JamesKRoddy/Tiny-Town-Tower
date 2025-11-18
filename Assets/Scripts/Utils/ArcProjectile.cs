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
    private bool isReflected = false; // Track if projectile has been reflected by player
    private float reflectedTimeAlive = 0f; // Track time since reflection
    private Vector3 reflectionStartPosition; // Position where projectile was reflected
    private float reflectionJumpDuration; // Duration for return trip

    /// <summary>
    /// Initialize the arc projectile with damage and effect parameters
    /// </summary>
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
        isReflected = false;
        reflectedTimeAlive = 0f;

        // Ensure projectile has a collider for player hit detection
        EnsureCollider();

        // Calculate the duration based on distance and speed
        float distance = Vector3.Distance(
            new Vector3(initialPosition.x, 0, initialPosition.z),
            new Vector3(targetPosition.x, 0, targetPosition.z)
        );
        jumpDuration = distance / speed;
    }

    /// <summary>
    /// Ensure the projectile has a collider and rigidbody for player hit detection and reflection
    /// </summary>
    private void EnsureCollider()
    {
        // Ensure Rigidbody exists (required for OnTriggerEnter to work with moving objects)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true; // Don't use physics, we're moving it manually
            rb.useGravity = false;
        }

        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            SphereCollider sphereCol = gameObject.AddComponent<SphereCollider>();
            sphereCol.radius = 0.2f;
            sphereCol.isTrigger = true; // Use trigger for player/weapon detection
            Debug.LogWarning($"[ArcProjectile] {gameObject.name} missing collider, added SphereCollider automatically");
        }
        else if (!col.isTrigger)
        {
            // If collider exists but isn't a trigger, make it one for player detection
            col.isTrigger = true;
        }
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

        // Handle reflected projectile movement
        if (isReflected)
        {
            reflectedTimeAlive += Time.deltaTime;
            float returnProgress = reflectedTimeAlive / reflectionJumpDuration;

            // Calculate position going back to origin (attacker position) with arc
            if (returnProgress < 1f)
            {
                Vector3 returnPos = Vector3.Lerp(reflectionStartPosition, initialPosition, returnProgress);
                // Add vertical movement using a sine wave for the return arc (shorter arc)
                returnPos.y += Mathf.Sin(returnProgress * Mathf.PI) * maxHeight * 0.5f;
                transform.position = returnPos;
            }
            else
            {
                // Reached origin, create impact
                CreateImpact();
                Destroy(gameObject);
                return;
            }

            // Check if the reflected projectile has hit the ground
            if (transform.position.y <= 0.1f)
            {
                CreateImpact();
                Destroy(gameObject);
            }
            return;
        }

        // Normal projectile movement (before reflection)
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

    /// <summary>
    /// Detect collision with player or weapon to reflect projectile back to origin
    /// Uses standardized player detection pattern
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        if (hasHit || isReflected) return; // Don't process if already hit or reflected

        // Check if hit by player or player weapon using standardized detection
        if (IsHitByPlayer(other))
        {
            // Reflect projectile back to origin (attacker position)
            ReflectToOrigin();
        }
    }

    /// <summary>
    /// Check if a collider belongs to the player or player weapon
    /// Standardized detection method (matches BaseProjectile pattern)
    /// </summary>
    private bool IsHitByPlayer(Collider other)
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
    /// Reflect the projectile back to its origin (the attacker that fired it)
    /// Uses initialPosition which is the origin (attacker position)
    /// </summary>
    private void ReflectToOrigin()
    {
        if (isReflected) return; // Already reflected

        isReflected = true;
        reflectedTimeAlive = 0f;
        
        // Store the position where reflection occurred
        reflectionStartPosition = transform.position;

        // Calculate new duration for return trip
        float returnDistance = Vector3.Distance(
            new Vector3(reflectionStartPosition.x, 0, reflectionStartPosition.z),
            new Vector3(initialPosition.x, 0, initialPosition.z)
        );
        reflectionJumpDuration = returnDistance / speed;

        // Update target to origin
        targetPosition = initialPosition;

        Debug.Log($"[ArcProjectile] {gameObject.name} reflected by player at {reflectionStartPosition}, returning to origin at {initialPosition}");
    }

    /// <summary>
    /// Create impact effect and damage area at projectile hit location
    /// </summary>
    void CreateImpact()
    {
        if (hasHit) return;
        hasHit = true;

        GameObject impactObject = null;

        // Play impact effect at projectile position
        if (impactEffect != null)
        {
            impactObject = EffectManager.Instance.PlayEffect(
                transform.position,
                Vector3.up,
                Quaternion.identity,
                null,
                impactEffect,
                duration: damageAreaDuration);
            
            // Configure trigger-based damage if requested
            if (useTriggerBasedDamage && impactObject != null)
            {
                ConfigureTriggerDamageComponent(impactObject);
            }
        }

        // Create damage area if requested (fallback for radius-based damage)
        if (createDamageArea && damageAreaRadius > 0 && !useTriggerBasedDamage)
        {
            // Use instant damage area for explosions (not lingering damage zones)
            DamageUtils.CreateInstantDamageArea(transform.position, damageAreaRadius, damage, poiseDamage, 
                attacker, element, null); // VFX already played above
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
