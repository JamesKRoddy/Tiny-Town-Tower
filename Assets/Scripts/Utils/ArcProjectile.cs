using UnityEngine;
using Managers;
using System.Collections;

/// <summary>
/// Generic projectile component that can be used for any arc-based projectile
/// Implements IHittable for weapon reflection
/// </summary>
public class ArcProjectile : MonoBehaviour, IHittable
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
    private float maxLifetime = 30f; // Increased for slow projectiles to allow time to hit ground
    private bool isReflected = false; // Track if projectile has been reflected by player
    private float reflectedTimeAlive = 0f; // Track time since reflection
    private Vector3 reflectionStartPosition; // Position where projectile was reflected
    private float reflectionJumpDuration; // Duration for return trip
    private float originalSpeed; // Store original speed for reflection speed boost
    private float launchTime; // Time when projectile was launched
    private float armingDelay = 0.2f; // Delay before projectile can cause damage (allows time for reflection/dodge)
    private float lastDebugLogTime = 0f; // For debug logging

    /// <summary>
    /// Initialize the arc projectile with damage and effect parameters
    /// </summary>
    public void Initialize(Vector3 targetPos, float dmg, float poiseDmg, Transform attackTransform, 
        AttackElement elem, float projectileSpeed = 10f, float projectileMaxHeight = 5f,
        EffectDefinition impactEff = null, bool createArea = false, float areaRadius = 0f, float areaDuration = 5f, bool triggerBased = false,
        float armingDelay = 0.2f)
    {
        initialPosition = transform.position;
        targetPosition = targetPos;
        damage = dmg;
        poiseDamage = poiseDmg;
        attacker = attackTransform;
        element = elem;
        speed = projectileSpeed;
        originalSpeed = projectileSpeed; // Store original speed for reflection speed boost
        maxHeight = projectileMaxHeight;
        impactEffect = impactEff;
        createDamageArea = createArea;
        damageAreaRadius = areaRadius;
        damageAreaDuration = areaDuration;
        useTriggerBasedDamage = triggerBased;
        this.armingDelay = armingDelay;
        timeAlive = 0f;
        launchTime = Time.time;
        hasHit = false;
        isReflected = false;
        reflectedTimeAlive = 0f;

        // Ensure projectile has a collider for player hit detection
        EnsureCollider();
        
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
    /// Ensure the projectile has a collider and rigidbody for player hit detection and reflection
    /// Uses a LARGE collider (radius 1.0) to make reflection much more forgiving
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
            // MUCH larger collider for easier reflection detection (1.0 instead of 0.2)
            sphereCol.radius = 1.0f;
            sphereCol.isTrigger = true; // Use trigger for player/weapon detection
            Debug.LogWarning($"[ArcProjectile] {gameObject.name} missing collider, added SphereCollider automatically with radius 1.0 for easy reflection");
        }
        else
        {
            // Make sure existing collider is a trigger and is large enough
            col.isTrigger = true;
            
            // If it's a sphere collider, ensure it's at least 1.0 radius for reflection detection
            if (col is SphereCollider sphere && sphere.radius < 1.0f)
            {
                sphere.radius = 1.0f;
                Debug.Log($"[ArcProjectile] {gameObject.name} increased collider radius to 1.0 for easier reflection detection");
            }
        }
    }

    void Update()
    {
        if (hasHit) return;

        timeAlive += Time.deltaTime;
        
        // Log position every 0.2 seconds for debugging hit detection
        if (Time.time - lastDebugLogTime >= 0.2f)
        {
            Debug.Log($"[ArcProjectile] {gameObject.name} position: {transform.position:F2} | TimeAlive: {timeAlive:F2}s | CanBeHit: {CanBeHit()} | IsReflected: {isReflected}");
            lastDebugLogTime = Time.time;
        }

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
            
            // Calculate horizontal distance to target
            Vector3 horizontalToTarget = new Vector3(currentTargetOrigin.x, 0, currentTargetOrigin.z) - new Vector3(transform.position.x, 0, transform.position.z);
            float horizontalDistance = horizontalToTarget.magnitude;
            
            // Check if close enough to impact
            if (horizontalDistance < 1.0f) // Increased threshold to ensure impact triggers
            {
                // Reached attacker, create impact
                Debug.Log($"[ArcProjectile] {gameObject.name} reached attacker at horizontal distance {horizontalDistance:F2}m, creating impact");
                CreateImpact();
                Destroy(gameObject);
                return;
            }
            
            // Use proper arc trajectory for reflected movement (similar to forward movement)
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
            
            // Rotate towards target
            Vector3 horizontalDir = horizontalToTarget.normalized;
            if (horizontalDir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(horizontalDir);
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
    /// Also detects collision with attacker when reflected
    /// Uses standardized player detection pattern
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
            // Ignore other collisions when reflected
            return;
        }

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
    
    /// <summary>
    /// Called when this projectile is hit (implements IHittable)
    /// Handles reflection logic
    /// </summary>
    public void OnHit(HitInfo hitInfo)
    {
        if (!CanBeHit()) 
        {
            Debug.Log($"[ArcProjectile] {gameObject.name} OnHit called but CanBeHit returned false (reflected: {isReflected}, hit: {hasHit})");
            return;
        }
        
        Debug.Log($"[ArcProjectile] {gameObject.name} OnHit called - reflecting projectile back to attacker");
        
        // Play reflection VFX
        CharacterCombat.PlayReflectionVFX(hitInfo.HitPoint, hitInfo.HitNormal);
        
        // Reflect the projectile
        ReflectToOrigin();
    }
    
    /// <summary>
    /// Check if this projectile can currently be hit/reflected (implements IHittable)
    /// </summary>
    public bool CanBeHit()
    {
        return !isReflected && !hasHit;
    }
    
    /// <summary>
    /// Public method to reflect projectile when hit by player weapon
    /// DEPRECATED: Use OnHit() via IHittable interface instead
    /// </summary>
    /// <param name="hitPoint">Position where the reflection occurred (for VFX)</param>
    /// <param name="hitNormal">Normal vector at the hit point (for VFX)</param>
    public void ReflectByPlayer(Vector3 hitPoint = default, Vector3 hitNormal = default)
    {
        if (isReflected || hasHit)
        {
            Debug.Log($"[ArcProjectile] {gameObject.name} ReflectByPlayer called but already reflected ({isReflected}) or hit ({hasHit}) - ignoring");
            return; // Don't reflect if already reflected or hit
        }
        
        Debug.Log($"[ArcProjectile] {gameObject.name} ReflectByPlayer called - reflecting projectile back to attacker");
        
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
        
        ReflectToOrigin();
    }

    /// <summary>
    /// Check if the projectile is armed (can cause damage)
    /// Projectiles have a brief arming delay after launch to allow reflection/dodge time
    /// </summary>
    /// <returns>True if the projectile is armed and can cause damage</returns>
    private bool IsArmed()
    {
        float timeAlive = Time.time - launchTime;
        return timeAlive >= armingDelay;
    }

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
