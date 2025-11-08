using UnityEngine;
using UnityEngine.AI;

namespace Enemies
{
    /// <summary>
    /// Base drone class that inherits from Zombie (uses the modular attack system).
    /// Drones are flying enemies that hover above the ground and can attack from the air.
    /// 
    /// Key Features:
    /// - Hovers at a configured height above ground
    /// - Can attack from above, making them harder to hit
    /// - Typically uses ranged attacks (projectiles, beams)
    /// - Bobbing/floating animation for realistic hover effect
    /// 
    /// Technical Implementation:
    /// - Uses NavMesh for pathfinding but offsets position vertically
    /// - Applies smooth hovering motion with sine wave
    /// - Can strafe and circle targets while maintaining height
    /// 
    /// Usage:
    /// 1. Use this as a base for specific drone types (ScoutDrone, AttackDrone, etc.)
    /// 2. Add attack components (ProjectileAttack, BeamAttack recommended)
    /// 3. Configure hover height and movement in inspector
    /// 4. Set CharacterType to MACHINE_DRONE
    /// 5. Ensure the model has appropriate hover animations
    /// </summary>
    public class Drone : ModularEnemy
    {
        #region Inspector Fields

        [Header("Drone Flight Settings")]
        [Tooltip("Height above ground to hover at")]
        [SerializeField] protected float hoverHeight = 3f;
        
        [Tooltip("Whether to add bobbing motion while hovering")]
        [SerializeField] protected bool enableHoverBobbing = true;
        
        [Tooltip("Speed of the bobbing motion")]
        [SerializeField] protected float bobbingSpeed = 1f;
        
        [Tooltip("Amplitude of the bobbing motion (how much it moves up/down)")]
        [SerializeField] protected float bobbingAmplitude = 0.3f;
        
        [Tooltip("Whether to tilt the drone based on movement direction")]
        [SerializeField] protected bool enableTilting = true;
        
        [Tooltip("Maximum tilt angle in degrees")]
        [SerializeField] protected float maxTiltAngle = 15f;
        
        [Tooltip("How quickly the drone tilts")]
        [SerializeField] protected float tiltSpeed = 3f;
        
        [Tooltip("Effect to play continuously while hovering (propeller effects, etc.)")]
        [SerializeField] protected GameObject hoverEffectPrefab;
        
        [Tooltip("Whether to strafe around targets instead of moving directly at them")]
        [SerializeField] protected bool strafeMovement = true;

        #endregion

        #region Protected Fields

        /// <summary>
        /// Base height position (before bobbing is applied)
        /// </summary>
        protected float baseHeight;
        
        /// <summary>
        /// Time offset for bobbing (randomized per drone for variety)
        /// </summary>
        protected float bobbingTimeOffset;
        
        /// <summary>
        /// Current tilt rotation
        /// </summary>
        protected Quaternion currentTilt = Quaternion.identity;
        
        /// <summary>
        /// Hover effect instance
        /// </summary>
        protected GameObject hoverEffectInstance;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            // Drones don't use root motion (flying movement is custom)
            useRootMotion = false;
            
            base.Awake();
            
            // Randomize bobbing offset for variety
            bobbingTimeOffset = Random.Range(0f, 2f * Mathf.PI);
        }

        protected override void Start()
        {
            base.Start();
            
            // Set initial hover height
            baseHeight = hoverHeight;
            
            // Spawn hover effect
            if (hoverEffectPrefab != null)
            {
                hoverEffectInstance = Instantiate(hoverEffectPrefab, transform);
                hoverEffectInstance.transform.localPosition = Vector3.zero;
            }
            
            Debug.Log($"[{gameObject.name}] Drone initialized at hover height: {hoverHeight}, bobbing: {enableHoverBobbing}, strafe: {strafeMovement}");
        }

        protected override void Update()
        {
            base.Update();
            
            if (Health <= 0) return;
            
            // Apply hover height and bobbing
            ApplyHoverHeight();
            
            // Apply tilt based on movement
            if (enableTilting)
            {
                ApplyMovementTilt();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            // Clean up hover effect
            if (hoverEffectInstance != null)
            {
                Destroy(hoverEffectInstance);
            }
        }

        #endregion

        #region Movement & Hovering

        /// <summary>
        /// Apply hover height and bobbing effect
        /// </summary>
        protected virtual void ApplyHoverHeight()
        {
            if (agent == null || !agent.isOnNavMesh) return;
            
            // Get the NavMesh position (ground level)
            Vector3 navMeshPosition = agent.nextPosition;
            
            // Calculate bobbing offset
            float bobbingOffset = 0f;
            if (enableHoverBobbing)
            {
                float time = Time.time * bobbingSpeed + bobbingTimeOffset;
                bobbingOffset = Mathf.Sin(time) * bobbingAmplitude;
            }
            
            // Apply hover height and bobbing
            Vector3 targetPosition = navMeshPosition + Vector3.up * (baseHeight + bobbingOffset);
            
            // Smoothly move to target position
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 5f);
        }

        /// <summary>
        /// Apply tilt based on movement direction
        /// </summary>
        protected virtual void ApplyMovementTilt()
        {
            if (agent == null) return;
            
            // Get movement direction
            Vector3 velocity = agent.velocity;
            
            if (velocity.magnitude > 0.1f)
            {
                // Calculate tilt based on velocity
                float forwardTilt = -velocity.z * maxTiltAngle / agent.speed;
                float sideTilt = velocity.x * maxTiltAngle / agent.speed;
                
                // Clamp tilt angles
                forwardTilt = Mathf.Clamp(forwardTilt, -maxTiltAngle, maxTiltAngle);
                sideTilt = Mathf.Clamp(sideTilt, -maxTiltAngle, maxTiltAngle);
                
                // Create target tilt rotation
                Quaternion targetTilt = Quaternion.Euler(forwardTilt, 0f, sideTilt);
                
                // Smoothly interpolate to target tilt
                currentTilt = Quaternion.Slerp(currentTilt, targetTilt, Time.deltaTime * tiltSpeed);
            }
            else
            {
                // Return to neutral position when not moving
                currentTilt = Quaternion.Slerp(currentTilt, Quaternion.identity, Time.deltaTime * tiltSpeed);
            }
            
            // Apply tilt rotation (in addition to facing direction)
            if (transform.childCount > 0)
            {
                // Apply tilt to child object (model) instead of root to not interfere with facing direction
                Transform model = transform.GetChild(0);
                model.localRotation = currentTilt;
            }
        }

        /// <summary>
        /// Override movement to add strafing behavior
        /// </summary>
        protected override void UpdateMovement()
        {
            if (strafeMovement && navMeshTarget != null && agent != null && agent.isOnNavMesh)
            {
                // Get distance to target
                float distanceToTarget = Vector3.Distance(transform.position, navMeshTarget.position);
                
                // If within attack range, strafe around target
                if (distanceToTarget < maxRange && distanceToTarget > minRange)
                {
                    ApplyStrafeMovement();
                    return;
                }
            }
            
            // Otherwise use normal movement
            base.UpdateMovement();
        }

        /// <summary>
        /// Apply strafing movement around target
        /// </summary>
        protected virtual void ApplyStrafeMovement()
        {
            if (navMeshTarget == null || agent == null) return;
            
            // Calculate circle point around target
            Vector3 directionToTarget = (transform.position - navMeshTarget.position).normalized;
            float strafeAngle = 45f * Mathf.Sign(Random.Range(-1f, 1f)); // Strafe left or right
            Vector3 strafeDirection = Quaternion.Euler(0f, strafeAngle, 0f) * directionToTarget;
            
            // Maintain preferred distance from target
            float preferredDistance = (maxRange + minRange) / 2f;
            Vector3 strafeTarget = navMeshTarget.position + strafeDirection * preferredDistance;
            
            // Set destination
            agent.SetDestination(strafeTarget);
        }

        #endregion

        #region Combat

        /// <summary>
        /// Override Die to add falling behavior
        /// </summary>
        public override void Die()
        {
            Debug.Log($"[{gameObject.name}] Drone Die() called! Starting fall...");
            
            // Disable hover effect
            if (hoverEffectInstance != null)
            {
                hoverEffectInstance.SetActive(false);
            }
            
            // Call base die
            base.Die();
            
            // Start falling
            StartCoroutine(FallToGround());
        }

        /// <summary>
        /// Coroutine to handle drone falling to ground when destroyed
        /// </summary>
        protected virtual System.Collections.IEnumerator FallToGround()
        {
            // Disable NavMesh agent
            if (agent != null)
            {
                agent.enabled = false;
            }
            
            // Fall to ground
            float fallSpeed = 2f;
            while (transform.position.y > 0.5f)
            {
                transform.position += Vector3.down * fallSpeed * Time.deltaTime;
                fallSpeed += 5f * Time.deltaTime; // Accelerate fall
                yield return null;
            }
            
            // Impact with ground
            Vector3 finalPosition = transform.position;
            finalPosition.y = 0.1f;
            transform.position = finalPosition;
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Get minimum attack distance (for ranged attacks)
        /// </summary>
        protected float minRange
        {
            get
            {
                // Get minimum range from attacks
                AttackBase[] attacks = GetComponents<AttackBase>();
                float min = 0f;
                foreach (var attack in attacks)
                {
                    if (attack != null && attack.minRange > min)
                    {
                        min = attack.minRange;
                    }
                }
                return min;
            }
        }

        /// <summary>
        /// Get maximum attack distance
        /// </summary>
        protected float maxRange
        {
            get
            {
                // Get maximum range from attacks
                AttackBase[] attacks = GetComponents<AttackBase>();
                float max = 10f; // Default
                foreach (var attack in attacks)
                {
                    if (attack != null && attack.maxRange > max)
                    {
                        max = attack.maxRange;
                    }
                }
                return max;
            }
        }

        #endregion

        #region Debug Visualization

        /// <summary>
        /// Draw gizmos to show hover height and attack ranges
        /// </summary>
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            
            // Draw hover height line
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position - Vector3.up * hoverHeight);
            
            // Draw hover height sphere
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            Gizmos.DrawSphere(transform.position - Vector3.up * hoverHeight, 0.5f);
            
            // Draw strafe circle at preferred distance
            if (strafeMovement)
            {
                float preferredDistance = (maxRange + minRange) / 2f;
                Gizmos.color = Color.yellow;
                // Draw circle at ground level
                for (int i = 0; i < 36; i++)
                {
                    float angle1 = i * 10f * Mathf.Deg2Rad;
                    float angle2 = (i + 1) * 10f * Mathf.Deg2Rad;
                    Vector3 point1 = transform.position + new Vector3(Mathf.Cos(angle1), 0f, Mathf.Sin(angle1)) * preferredDistance;
                    Vector3 point2 = transform.position + new Vector3(Mathf.Cos(angle2), 0f, Mathf.Sin(angle2)) * preferredDistance;
                    point1.y = transform.position.y;
                    point2.y = transform.position.y;
                    Gizmos.DrawLine(point1, point2);
                }
            }
        }

        #endregion
    }
}

