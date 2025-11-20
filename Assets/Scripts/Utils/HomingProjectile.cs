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
    
    /// <summary>
    /// Initialize the homing projectile using parameter class
    /// </summary>
    public void Initialize(HomingProjectileParams parameters)
    {
        if (parameters == null)
        {
            Debug.LogError($"[HomingProjectile] {gameObject.name} Initialize called with null parameters!");
            return;
        }
        
        Initialize(parameters.targetTransform, parameters.damage, parameters.poiseDamage, parameters.attacker,
            parameters.element, parameters.speed, parameters.homingDuration, parameters.turnSpeed,
            parameters.explodeOnTimeout, parameters.impactEffect, parameters.createDamageArea,
            parameters.damageAreaRadius, parameters.damageAreaDuration, parameters.useTriggerBasedDamage,
            parameters.armingDelay);
    }
    
    /// <summary>
    /// Initialize the homing projectile with damage, tracking, and effect parameters
    /// </summary>
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
        bool triggerBased = false,
        float armingDelay = 0.2f)
    {
        Debug.Log($"[HomingProjectile] ===== INITIALIZE START =====");
        Debug.Log($"[HomingProjectile] GameObject: {gameObject.name} | Position: {transform.position}");
        Debug.Log($"[HomingProjectile] Parameters:");
        Debug.Log($"[HomingProjectile]   - Target: {(targetTransform != null ? targetTransform.name : "NULL")}");
        Debug.Log($"[HomingProjectile]   - Damage: {dmg} | Poise: {poiseDmg} | Element: {elem}");
        Debug.Log($"[HomingProjectile]   - Attacker: {(attackTransform != null ? attackTransform.name : "NULL")}");
        Debug.Log($"[HomingProjectile]   - Speed: {projectileSpeed} | Duration: {duration}s | Turn Speed: {turnSpeedDegrees}°/s");
        Debug.Log($"[HomingProjectile]   - Explode On Timeout: {explodeWhenExpired}");
        Debug.Log($"[HomingProjectile]   - Impact Effect: {(impactEff != null ? impactEff.name : "NULL")}");
        Debug.Log($"[HomingProjectile]   - Create Area: {createArea} | Radius: {areaRadius} | Duration: {areaDuration}");
        Debug.Log($"[HomingProjectile]   - Use Trigger: {triggerBased}");
        Debug.Log($"[HomingProjectile]   - Arming Delay: {armingDelay}s");
        
        if (gameObject == null)
        {
            Debug.LogError("[HomingProjectile] ❌ GameObject is NULL!");
            return;
        }
        
        // Initialize base parameters (damage, effects, etc.)
        Debug.Log($"[HomingProjectile] Calling InitializeBase...");
        try
        {
            InitializeBase(dmg, poiseDmg, attackTransform, elem, impactEff, createArea, areaRadius, areaDuration, triggerBased, armingDelay);
            Debug.Log($"[HomingProjectile] ✅ InitializeBase completed");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[HomingProjectile] ❌ EXCEPTION in InitializeBase(): {e.Message}");
            Debug.LogError($"[HomingProjectile] Stack Trace: {e.StackTrace}");
            return;
        }
        
        if (rb == null)
        {
            Debug.LogError("[HomingProjectile] ❌ Rigidbody is NULL after InitializeBase()!");
            return;
        }
        
        Debug.Log($"[HomingProjectile] Rigidbody found: {rb.name} | Is Kinematic: {rb.isKinematic} | Use Gravity: {rb.useGravity}");
        
        // Store homing-specific parameters
            target = targetTransform;
        if (target == null)
        {
            Debug.LogWarning("[HomingProjectile] ⚠️ Target Transform is NULL! Homing will not work.");
        }
        
            homingDuration = duration;
            turnSpeed = turnSpeedDegrees;
        speed = projectileSpeed;
        baseSpeed = projectileSpeed;
            explodeOnTimeout = explodeWhenExpired;
        
        Debug.Log($"[HomingProjectile] Homing parameters stored | Target: {(target != null ? target.name : "NULL")} | Speed: {speed} | Duration: {homingDuration}s");
        
        // Initialize homing-specific state
        isTracking = true;
        currentDirection = transform.forward;
        
        Debug.Log($"[HomingProjectile] Initial direction: {currentDirection} | Is Tracking: {isTracking}");
        
        // Configure rigidbody for homing behavior
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.linearVelocity = currentDirection * speed;
        
        Debug.Log($"[HomingProjectile] Rigidbody configured | Velocity: {rb.linearVelocity} | Speed: {speed}");
        
        // Ensure projectile has a collider for impact detection
        Debug.Log($"[HomingProjectile] Ensuring collider...");
        EnsureCollider(0.2f, false);
        Collider mainCollider = GetComponent<Collider>();
        Debug.Log($"[HomingProjectile] Main Collider: {(mainCollider != null ? mainCollider.GetType().Name + " (trigger: " + mainCollider.isTrigger + ")" : "NULL")}");
        
        // Also ensure we have a trigger collider for player detection
        Debug.Log($"[HomingProjectile] Ensuring trigger collider for reflection...");
        EnsureTriggerColliderForReflection();
        Collider[] allColliders = GetComponents<Collider>();
        Debug.Log($"[HomingProjectile] Total Colliders: {allColliders.Length}");
        foreach (var col in allColliders)
        {
            Debug.Log($"[HomingProjectile]   - {col.GetType().Name} | Is Trigger: {col.isTrigger} | Enabled: {col.enabled}");
        }
            
        // Point projectile in direction of travel
        if (currentDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(currentDirection);
            Debug.Log($"[HomingProjectile] Rotation set to: {transform.rotation}");
        }
        else
        {
            Debug.LogWarning("[HomingProjectile] ⚠️ Current direction is zero! Cannot set rotation.");
        }
        
        Debug.Log($"[HomingProjectile] ✅ {gameObject.name} initialized successfully | Target: {(target != null ? target.name : "NULL")} | Speed: {speed} | Duration: {duration}s | Has Collider: {mainCollider != null} | CreateDamageArea: {createArea} | Radius: {areaRadius} | UseTrigger: {triggerBased}");
        Debug.Log($"[HomingProjectile] ===== INITIALIZE END =====");
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

    protected override void Update()
    {
        base.Update(); // Call base class Update for lifetime management
    }

    void FixedUpdate()
        {
        if (hasHit) return;

        // Handle reflected projectile movement
        if (isReflected)
        {
            isTracking = false; // Stop tracking when reflected
            
            // Reverse direction back to origin
            Vector3 toOrigin = (GetOriginPosition() - transform.position).normalized;
            currentDirection = toOrigin;
            
            // Move towards origin
            if (rb != null)
            {
                rb.linearVelocity = currentDirection * speed;
            }
            else
            {
                transform.position += currentDirection * speed * Time.fixedDeltaTime;
            }
            
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
                Debug.Log($"[HomingProjectile] {gameObject.name} reached attacker at distance {distanceToOrigin:F2}m, creating impact");
                HandleImpact(impactPos, Vector3.up, attacker);
                return;
            }
            return;
        }
            
        if (!isTracking) return;
            
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
        // === COMPREHENSIVE COLLISION LOGGING ===
        float timeAlive = Time.time - launchTime;
        float distanceToTarget = target != null ? Vector3.Distance(transform.position, target.position) : -1f;
        float distanceToAttacker = attacker != null ? Vector3.Distance(transform.position, attacker.position) : -1f;
        bool isAttacker = attacker != null && (collision.gameObject == attacker.gameObject || collision.transform.IsChildOf(attacker));
        bool isPlayerHit = IsHitByPlayer(collision.transform);
        
        Debug.Log($"[HomingProjectile] ===== COLLISION DETECTED =====");
        Debug.Log($"[HomingProjectile] Projectile: {gameObject.name} | Position: {transform.position}");
        Debug.Log($"[HomingProjectile] Hit Object: {collision.gameObject.name}");
        Debug.Log($"[HomingProjectile] Hit Layer: {LayerMask.LayerToName(collision.gameObject.layer)} ({collision.gameObject.layer})");
        Debug.Log($"[HomingProjectile] Hit Tag: {(string.IsNullOrEmpty(collision.gameObject.tag) ? "None" : collision.gameObject.tag)}");
        Debug.Log($"[HomingProjectile] Is Attacker: {isAttacker} | Attacker: {(attacker != null ? attacker.name : "NULL")}");
        Debug.Log($"[HomingProjectile] Is Player Hit: {isPlayerHit}");
        Debug.Log($"[HomingProjectile] Is Reflected: {isReflected} | Has Hit: {hasHit}");
        Debug.Log($"[HomingProjectile] Time Alive: {timeAlive:F3}s | Distance to Target: {distanceToTarget:F2}m | Distance to Attacker: {distanceToAttacker:F2}m");
        Debug.Log($"[HomingProjectile] Hit Point: {(collision.contacts.Length > 0 ? collision.contacts[0].point.ToString() : "No contacts")}");
        
        if (hasHit)
        {
            Debug.Log($"[HomingProjectile] >>> IGNORED - Already hit something");
            return;
        }
        
        // Check if hit by player first (before normal impact) - don't check isReflected here to allow reflection
        if (!isReflected && isPlayerHit)
        {
            Debug.Log($"[HomingProjectile] >>> REFLECTING - Hit by player/weapon");
            ReflectProjectile();
            return; // Don't impact, reflect instead
        }
        
        // If reflected, only process collisions with the attacker (enemy that fired it)
        if (isReflected)
        {
            if (isAttacker)
            {
                // Hit the attacker, create impact
                Debug.Log($"[HomingProjectile] >>> REFLECTED PROJECTILE HIT ATTACKER - Creating impact");
                hasHit = true;
                isTracking = false;
                
                Vector3 impactPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
                Vector3 impactNormal = collision.contacts.Length > 0 ? collision.contacts[0].normal : Vector3.up;
                
                HandleImpact(impactPoint, impactNormal, attacker);
                return;
            }
            // Ignore other collisions when reflected (let distance check handle impact)
            Debug.Log($"[HomingProjectile] >>> IGNORED - Already reflected, not attacker");
            return;
        }
        
        // Check if hitting the attacker (drone itself)
        if (isAttacker)
        {
            Debug.Log($"[HomingProjectile] >>> WARNING - Hitting attacker (drone)! This may indicate spawn position is too close.");
        }
        
        Debug.Log($"[HomingProjectile] >>> PROCESSING IMPACT");
        hasHit = true;
        isTracking = false; // Stop tracking on impact

        Vector3 hitPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
        Vector3 hitNormal = collision.contacts.Length > 0 ? collision.contacts[0].normal : Vector3.up;

        HandleImpact(hitPoint, hitNormal, collision.transform);
                }

    void OnTriggerEnter(Collider other)
    {
        // === COMPREHENSIVE TRIGGER LOGGING ===
        float timeAlive = Time.time - launchTime;
        float distanceToTarget = target != null ? Vector3.Distance(transform.position, target.position) : -1f;
        float distanceToAttacker = attacker != null ? Vector3.Distance(transform.position, attacker.position) : -1f;
        bool isAttacker = attacker != null && (other.gameObject == attacker.gameObject || other.transform.IsChildOf(attacker));
        bool isPlayerHit = IsHitByPlayer(other);
        
        Debug.Log($"[HomingProjectile] ===== TRIGGER DETECTED =====");
        Debug.Log($"[HomingProjectile] Projectile: {gameObject.name} | Position: {transform.position}");
        Debug.Log($"[HomingProjectile] Trigger Object: {other.gameObject.name}");
        Debug.Log($"[HomingProjectile] Trigger Layer: {LayerMask.LayerToName(other.gameObject.layer)} ({other.gameObject.layer})");
        Debug.Log($"[HomingProjectile] Trigger Tag: {(string.IsNullOrEmpty(other.gameObject.tag) ? "None" : other.gameObject.tag)}");
        Debug.Log($"[HomingProjectile] Is Attacker: {isAttacker} | Attacker: {(attacker != null ? attacker.name : "NULL")}");
        Debug.Log($"[HomingProjectile] Is Player Hit: {isPlayerHit}");
        Debug.Log($"[HomingProjectile] Is Reflected: {isReflected} | Has Hit: {hasHit}");
        Debug.Log($"[HomingProjectile] Time Alive: {timeAlive:F3}s | Distance to Target: {distanceToTarget:F2}m | Distance to Attacker: {distanceToAttacker:F2}m");
        
        if (hasHit)
        {
            Debug.Log($"[HomingProjectile] >>> IGNORED - Already hit something");
            return;
        }
        
        // If reflected, check if hitting the attacker
        if (isReflected)
        {
            if (isAttacker)
            {
                // Hit the attacker, create impact
                Debug.Log($"[HomingProjectile] >>> REFLECTED PROJECTILE HIT ATTACKER (trigger) - Creating impact");
        hasHit = true;
                isTracking = false;
                
                HandleImpact(transform.position, Vector3.up, attacker);
                return;
            }
            // Ignore other triggers when reflected
            Debug.Log($"[HomingProjectile] >>> IGNORED - Already reflected, not attacker");
            return;
        }
        
        // Only process player collisions in OnTriggerEnter (trigger collider is for player reflection)
        // Ground, walls, and other solid objects should be handled by OnCollisionEnter instead
        if (isPlayerHit)
        {
            Debug.Log($"[HomingProjectile] >>> REFLECTING - Hit by player/weapon (trigger)");
            ReflectProjectile();
            return; // Reflect instead of impacting
        }
        
        // Ignore all other trigger collisions (ground, walls, etc.) - they should use OnCollisionEnter
        Debug.Log($"[HomingProjectile] >>> IGNORED - Non-player trigger (will be handled by OnCollisionEnter)");
        }

    /// <summary>
    /// Reflect the projectile back to its origin (called when hit by player)
    /// </summary>
    private void ReflectProjectile()
    {
        if (isReflected) return; // Already reflected
        
        OnReflected(); // Call base method to set isReflected flag
        
        // Stop tracking
        isTracking = false;
        
        // Increase speed by 1.5x when reflected
        speed = baseSpeed * 1.5f;
        
        // Reverse direction immediately
        Vector3 toOrigin = (GetOriginPosition() - transform.position).normalized;
        currentDirection = toOrigin;
        
        if (rb != null)
        {
            rb.linearVelocity = currentDirection * speed;
        }
        
        // Rotate towards origin
        if (toOrigin != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(toOrigin);
        }
        
        Debug.Log($"[HomingProjectile] {gameObject.name} reflected by player, returning to origin at {GetOriginPosition()} | Speed increased to {speed} (1.5x original)");
    }

    /// <summary>
    /// Override to call private ReflectProjectile method which handles direction reversal and tracking stop
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


