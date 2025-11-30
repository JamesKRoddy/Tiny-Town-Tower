using UnityEngine;
using Managers;
using System.Collections;

/// <summary>
/// Generic projectile component that can be used for any arc-based projectile
/// Inherits from BaseProjectile for shared functionality
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ArcProjectile : BaseProjectile
{
    private Vector3 initialPosition;
    private Vector3 targetPosition;
    private float speed;
    private float maxHeight;
    private float originalSpeed; // Store original speed for reflection speed boost

    private float timeAlive = 0f;
    private float jumpDuration;
    private float reflectedTimeAlive = 0f; // Track time since reflection
    private Vector3 reflectionStartPosition; // Position where projectile was reflected
    private float reflectionJumpDuration; // Duration for return trip
    private float lastDebugLogTime = 0f; // For debug logging

    /// <summary>
    /// Initialize the arc projectile using parameter class
    /// </summary>
    public void Initialize(ArcProjectileParams parameters)
    {
        if (parameters == null)
        {
            Debug.LogError($"[ArcProjectile] {gameObject.name} Initialize called with null parameters!");
            return;
        }
        
        Initialize(parameters.targetPosition, parameters.damage, parameters.poiseDamage, parameters.attacker,
            parameters.element, parameters.speed, parameters.maxHeight, parameters.impactEffect,
            parameters.createDamageArea, parameters.damageAreaRadius, parameters.damageAreaDuration,
            parameters.useTriggerBasedDamage, parameters.armingDelay, parameters.explodeOnAnyHit);
    }

    /// <summary>
    /// Initialize the arc projectile with damage and effect parameters
    /// </summary>
    public void Initialize(Vector3 targetPos, float dmg, float poiseDmg, Transform attackTransform, 
        AttackElement elem, float projectileSpeed = 10f, float projectileMaxHeight = 5f,
        EffectDefinition impactEff = null, bool createArea = false, float areaRadius = 0f, float areaDuration = 5f, bool triggerBased = false,
        float armingDelay = 0.2f, bool explodeOnAnyHit = false)
    {
        // Initialize base projectile parameters (damage, effects, etc.)
        InitializeBase(dmg, poiseDmg, attackTransform, elem, impactEff, createArea, areaRadius, areaDuration, triggerBased, armingDelay, explodeOnAnyHit);
        
        // Override maxLifetime for arc projectiles (they need more time to complete arc)
        maxLifetime = 30f; // Increased for slow projectiles to allow time to hit ground
        
        initialPosition = transform.position;
        targetPosition = targetPos;
        speed = projectileSpeed;
        originalSpeed = projectileSpeed; // Store original speed for reflection speed boost
        maxHeight = projectileMaxHeight;
        timeAlive = 0f;
        reflectedTimeAlive = 0f;

        // Ensure projectile has a collider for player hit detection
        // Use large radius (1.0) for easy reflection and set as trigger
        EnsureCollider(1.0f, true);
        
        // Log collider setup for debugging reflection detection
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            string radiusInfo = col is SphereCollider sc ? $"Radius: {sc.radius:F2}" : "N/A";
            Debug.Log($"[ArcProjectile] {gameObject.name} initialized | Collider: {col.GetType().Name} | Layer: {LayerMask.LayerToName(gameObject.layer)} ({gameObject.layer}) | IsTrigger: {col.isTrigger} | {radiusInfo} (LARGE for easy reflection) | Position: {transform.position}");
        }
        else
        {
            Debug.LogWarning($"[ArcProjectile] {gameObject.name} initialized WITHOUT collider! Reflection detection will fail!");
        }

        // Calculate the duration based on distance and speed
        float distance = Vector3.Distance(
            new Vector3(initialPosition.x, 0, initialPosition.z),
            new Vector3(targetPosition.x, 0, targetPosition.z)
        );
        jumpDuration = distance / speed;
    }

    /// <summary>
    /// Override EnsureCollider to use large radius for easy reflection detection
    /// </summary>
    protected override void EnsureCollider(float defaultRadius = 1.0f, bool isTrigger = true)
    {
        // Ensure Rigidbody exists (required for OnTriggerEnter to work with moving objects)
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true; // Don't use physics, we're moving it manually
            rb.useGravity = false;
        }

        base.EnsureCollider(defaultRadius, isTrigger);
        
        // Make sure sphere collider has large radius for reflection detection
        Collider col = GetComponent<Collider>();
        if (col is SphereCollider sphere && sphere.radius < 1.0f)
        {
            sphere.radius = 1.0f;
            Debug.Log($"[ArcProjectile] {gameObject.name} increased collider radius to 1.0 for easier reflection detection");
        }
    }

    protected override void Update()
    {
        if (hasHit) return;
        
        // Call base Update for lifetime management
        base.Update();

        timeAlive += Time.deltaTime;
        
        // Log position every 0.2 seconds for debugging hit detection
        if (Time.time - lastDebugLogTime >= 0.2f)
        {
            Debug.Log($"[ArcProjectile] {gameObject.name} position: {transform.position:F2} | TimeAlive: {timeAlive:F2}s | CanBeHit: {CanBeHit()} | IsReflected: {isReflected}");
            lastDebugLogTime = Time.time;
        }

        // Destroy if exceeded max lifetime (handled by base class, but we override maxLifetime)
        float timeAliveFromBase = Time.time - launchTime;
        if (timeAliveFromBase >= maxLifetime)
        {
            CreateImpact();
            Destroy(gameObject);
            return;
        }

        // Handle reflected projectile movement
        if (isReflected)
        {
            reflectedTimeAlive += Time.deltaTime;
            
            // Update target to attacker's target transform position each frame (tracks moving enemies to their mesh)
            Vector3 currentTargetOrigin = initialPosition;
            if (attacker != null)
            {
                IDamageable damageable = attacker.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    Transform targetTransform = damageable.GetTargetTransform();
                    if (targetTransform != null)
                    {
                        currentTargetOrigin = targetTransform.position;
                    }
                    else
                    {
                        currentTargetOrigin = attacker.position;
                    }
                }
                else
                {
                    currentTargetOrigin = attacker.position;
                }
            }
            
            // Use proper arc trajectory for reflected movement (similar to forward movement)
            // Don't check distance to target - let it complete arc or hit ground/collider
            // This prevents mid-air explosions when passing through enemy colliders
            // Calculate progress along the return arc (0 to 1)
            float returnProgress = reflectedTimeAlive / reflectionJumpDuration;
            returnProgress = Mathf.Clamp01(returnProgress);
            
            // Start from reflection position, end at attacker's current position
            Vector3 reflectionStartHorizontal = new Vector3(reflectionStartPosition.x, 0, reflectionStartPosition.z);
            Vector3 targetHorizontal = new Vector3(currentTargetOrigin.x, 0, currentTargetOrigin.z);
            
            // Interpolate horizontal position
            Vector3 currentHorizontalPos = Vector3.Lerp(reflectionStartHorizontal, targetHorizontal, returnProgress);
            
            // Add vertical arc using sine wave (like forward movement)
            float arcHeight = Mathf.Sin(returnProgress * Mathf.PI) * maxHeight;
            
            // Combine horizontal and vertical position
            Vector3 newPosition = new Vector3(
                currentHorizontalPos.x,
                currentTargetOrigin.y + arcHeight,
                currentHorizontalPos.z
            );
            
            transform.position = newPosition;
            
            // Rotate towards target (use horizontal direction for rotation, recalculate after position update)
            Vector3 horizontalToTarget = new Vector3(currentTargetOrigin.x, 0, currentTargetOrigin.z) - new Vector3(newPosition.x, 0, newPosition.z);
            if (horizontalToTarget.magnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(horizontalToTarget.normalized);
            }

        // Check if the reflected projectile has hit the ground (if explodeOnAnyHit is false)
        // Or check distance if explodeOnAnyHit is true (will hit ground or attacker)
        if (!explodeOnAnyHit)
        {
            // Only explode on ground hit
            if (transform.position.y <= 0.1f)
            {
                CreateImpact();
                Destroy(gameObject);
            }
        }
        // If explodeOnAnyHit is true, let OnTriggerEnter handle collisions
        
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

        // Check if the projectile has hit the ground (if explodeOnAnyHit is false)
        if (!explodeOnAnyHit)
        {
            // Only explode on ground hit
            if (transform.position.y <= 0.1f)
            {
                CreateImpact();
                Destroy(gameObject);
            }
        }
        // If explodeOnAnyHit is true, let OnTriggerEnter handle collisions
    }

    /// <summary>
    /// Detect collision with player or weapon to reflect projectile back to origin
    /// Also detects collision with attacker when reflected
    /// Uses standardized player detection pattern from BaseProjectile
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
        
        // If reflected, check if hitting the attacker
        if (isReflected)
        {
            bool isAttacker = attacker != null && (other.gameObject == attacker.gameObject || other.transform.IsChildOf(attacker));
            if (isAttacker)
            {
                // Hit the attacker, create impact
                Debug.Log($"[ArcProjectile] {gameObject.name} reflected projectile hit attacker {attacker.name}");
                CreateImpact();
                Destroy(gameObject);
                return;
            }
            // Ignore other collisions when reflected (unless explodeOnAnyHit is true)
            if (explodeOnAnyHit)
            {
                // If explodeOnAnyHit is true, explode on any collision when reflected
                IDamageable damageable = other.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    CreateImpact();
                    Destroy(gameObject);
                    return;
                }
            }
            return;
        }

        // Check if hit by player or player weapon using standardized detection from base class
        if (IsHitByPlayer(other))
        {
            // Calculate hit point and normal for VFX
            Vector3 hitPoint = transform.position;
            Vector3 hitNormal = (transform.position - (other.ClosestPoint(transform.position) - other.transform.position)).normalized;
            if (hitNormal == Vector3.zero)
            {
                hitNormal = -transform.forward;
            }
            
            // Use OnReflected() to properly handle reflection (will call ReflectToOrigin())
            OnReflected(hitPoint, hitNormal);
            return;
        }
        
        // If explodeOnAnyHit is true, explode on any collision (not just player)
        if (explodeOnAnyHit && !isReflected)
        {
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                CreateImpact();
                Destroy(gameObject);
                return;
            }
        }
    }

    // IsHitByPlayer is inherited from BaseProjectile - no need to redefine

    /// <summary>
    /// Reflect the projectile back to its origin (the attacker that fired it)
    /// When attacker is available, tracks their current position so projectile follows moving enemies
    /// </summary>
    private void ReflectToOrigin()
    {
        if (isReflected) return; // Already reflected

        isReflected = true;
        reflectedTimeAlive = 0f;
        
        // Increase speed by 1.5x when reflected
        speed = originalSpeed * 1.5f;
        
        // Store the position where reflection occurred
        reflectionStartPosition = transform.position;

        // Use attacker's target transform position if available (so projectile tracks moving enemies to their mesh)
        // Otherwise use initial spawn position
        Vector3 targetOrigin = initialPosition;
        if (attacker != null)
        {
            IDamageable damageable = attacker.GetComponent<IDamageable>();
            if (damageable != null)
            {
                Transform targetTransform = damageable.GetTargetTransform();
                if (targetTransform != null)
                {
                    targetOrigin = targetTransform.position;
                }
                else
                {
                    targetOrigin = attacker.position;
                }
            }
            else
            {
                targetOrigin = attacker.position;
            }
        }

        // Calculate new duration for return trip (using increased speed)
        // Use horizontal distance for duration calculation
        float returnDistance = Vector3.Distance(
            new Vector3(reflectionStartPosition.x, 0, reflectionStartPosition.z),
            new Vector3(targetOrigin.x, 0, targetOrigin.z)
        );
        reflectionJumpDuration = returnDistance / speed;
        
        // Ensure we have a valid duration
        if (reflectionJumpDuration <= 0f)
        {
            reflectionJumpDuration = 1f; // Fallback to 1 second if distance is too small
        }

        // Update target to origin (will be updated each frame to track moving attacker)
        targetPosition = targetOrigin;

        Debug.Log($"[ArcProjectile] {gameObject.name} reflected by player at {reflectionStartPosition}, returning to attacker at {(attacker != null ? attacker.name : "NULL")} position {targetOrigin} | Speed increased to {speed} (1.5x original)");
    }

    // ===== IHITTABLE IMPLEMENTATION =====
    // OnHit and CanBeHit are inherited from BaseProjectile
    // Override OnReflected to handle arc-specific reflection behavior
    
    /// <summary>
    /// Override OnReflected to handle arc-specific reflection logic
    /// </summary>
    protected override void OnReflected(Vector3 hitPoint = default, Vector3 hitNormal = default)
    {
        // Handle arc-specific reflection (speed boost, trajectory, etc.) FIRST
        // This must be called before base.OnReflected() which sets isReflected = true
        ReflectToOrigin();
        
        // Then call base implementation (will set isReflected flag and log)
        base.OnReflected(hitPoint, hitNormal);
        
        // Play reflection VFX
        if (hitPoint != default && hitNormal != default)
        {
            PlayReflectionVFX(hitPoint, hitNormal);
        }
    }

    // IsArmed is inherited from BaseProjectile - no need to redefine

    /// <summary>
    /// Create impact effect and damage area at projectile hit location
    /// </summary>
    void CreateImpact()
    {
        if (hasHit) return;
        
        // Check if projectile is armed before causing damage
        if (!IsArmed() && !isReflected)
        {
            // Projectile is not armed yet - can still be reflected, but won't cause damage
            Debug.Log($"[ArcProjectile] {gameObject.name} hit target before arming delay ({Time.time - launchTime:F3}s < {armingDelay:F3}s) - no damage dealt");
            return;
        }
        
        hasHit = true;

        GameObject impactObject = null;

        // Raycast down to find ground position - spawn pool on ground, not in air
        Vector3 impactPosition = transform.position;
        Vector3 impactNormal = Vector3.up;
        
        // Use ground detection mask to ignore characters and find actual ground
        int groundLayerMask = GameConstants.Layers.GroundDetectionMask;
        RaycastHit groundHit;
        
        // Raycast down from current position (with some upward offset to ensure we don't start inside ground)
        Vector3 rayStart = transform.position + Vector3.up * 0.5f;
        float rayDistance = transform.position.y + 10f; // Cast far enough to reach ground from any height
        
        if (Physics.Raycast(rayStart, Vector3.down, out groundHit, rayDistance, groundLayerMask, QueryTriggerInteraction.Ignore))
        {
            impactPosition = groundHit.point;
            impactNormal = groundHit.normal;
            Debug.Log($"[ArcProjectile] {gameObject.name} found ground at {impactPosition} (was at {transform.position})");
        }
        else
        {
            // Fallback: if no ground found, use horizontal position at Y=0
            impactPosition = new Vector3(transform.position.x, 0f, transform.position.z);
            Debug.LogWarning($"[ArcProjectile] {gameObject.name} no ground found, using fallback position {impactPosition}");
        }

        // Play impact effect at ground position
        if (impactEffect != null)
        {
            impactObject = EffectManager.Instance.PlayEffect(
                impactPosition,
                impactNormal,
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
            DamageUtils.CreateInstantDamageArea(impactPosition, damageAreaRadius, damage, poiseDamage, 
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
            
            // Draw reflection detection radius (large sphere around projectile)
            Collider col = GetComponent<Collider>();
            if (col is SphereCollider sphere)
            {
                Gizmos.color = isReflected ? Color.green : Color.cyan;
                Gizmos.DrawWireSphere(transform.position, sphere.radius);
            }
        }
    }
}
