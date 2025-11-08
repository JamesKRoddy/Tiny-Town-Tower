using UnityEngine;
using UnityEngine.AI;

namespace Enemies
{
    /// <summary>
    /// Base drone class that inherits from ModularEnemy (uses the modular attack system).
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
    /// 5. IMPORTANT: Add a child GameObject for the visual mesh/model (will be auto-detected)
    ///    - The child will bob/tilt while the parent (NavMeshAgent) stays grounded
    ///    - If no child found, will bob the entire GameObject (legacy behavior)
    /// </summary>
    public class Drone : ModularEnemy
    {
        #region Inspector Fields

        [Header("Drone Flight Settings")]
        [Tooltip("Child transform containing the visual mesh (auto-detected if null). This will bob while root stays grounded.")]
        [SerializeField] protected Transform visualMesh;
        
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
            // IMPORTANT: Set this BEFORE calling base.Awake() so NavMeshAgent is configured correctly
            useRootMotion = false;
            
            base.Awake();

            if (agent != null)
            {
                agent.updateRotation = false;
            }
            
            // Randomize bobbing offset for variety
            bobbingTimeOffset = Random.Range(0f, 2f * Mathf.PI);
        }

        protected override void Start()
        {
            base.Start();
            
            // Auto-detect visual mesh if not assigned
            if (visualMesh == null && transform.childCount > 0)
            {
                // Use the first child as the visual mesh
                visualMesh = transform.GetChild(0);
                Debug.Log($"[{gameObject.name}] Auto-detected visual mesh: {visualMesh.name}");
            }
            
            if (visualMesh == null)
            {
                Debug.LogWarning($"[{gameObject.name}] No visual mesh child found! Bobbing will move the entire GameObject (including NavMeshAgent). Add a child GameObject for the mesh to avoid pathfinding issues.");
            }
            
            // Set initial hover height
            baseHeight = hoverHeight;
            
            // Spawn hover effect (attach to visual mesh if available, otherwise root)
            if (hoverEffectPrefab != null)
            {
                Transform effectParent = visualMesh != null ? visualMesh : transform;
                hoverEffectInstance = Instantiate(hoverEffectPrefab, effectParent);
                hoverEffectInstance.transform.localPosition = Vector3.zero;
            }
            
            Debug.Log($"[{gameObject.name}] Drone initialized at hover height: {hoverHeight}, bobbing: {enableHoverBobbing}, strafe: {strafeMovement}, visualMesh: {visualMesh?.name ?? "None (legacy mode)"}");
        }

        protected override void Update()
        {
            base.Update();
            
            if (Health <= 0) return;
            
            // Apply hover height and bobbing
            ApplyHoverHeight();

            // Rotate to face the current target
            ApplyLookRotation();
            
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
        /// If visualMesh is assigned, only the child bobs (NavMeshAgent stays grounded)
        /// If visualMesh is null, the entire GameObject bobs (legacy behavior)
        /// </summary>
        protected virtual void ApplyHoverHeight()
        {
            if (agent == null || !agent.isOnNavMesh) return;
            
            // Calculate bobbing offset
            float bobbingOffset = 0f;
            if (enableHoverBobbing)
            {
                float time = Time.time * bobbingSpeed + bobbingTimeOffset;
                bobbingOffset = Mathf.Sin(time) * bobbingAmplitude;
            }
            
            if (visualMesh != null)
            {
                // NEW BEHAVIOR: NavMeshAgent handles root position (agent.updatePosition = true)
                // We only control the visual mesh child's local position for hovering/bobbing
                // This keeps pathfinding independent from visual effects
                Vector3 targetLocalPosition = Vector3.up * (baseHeight + bobbingOffset);
                visualMesh.localPosition = Vector3.Lerp(visualMesh.localPosition, targetLocalPosition, Time.deltaTime * 5f);
            }
            else
            {
                // LEGACY BEHAVIOR: Move entire GameObject (including NavMeshAgent)
                // This can cause pathfinding issues but maintains backwards compatibility
                // We manually override the position since there's no separate visual mesh
                Vector3 navMeshPosition = agent.nextPosition;
                Vector3 targetPosition = navMeshPosition + Vector3.up * (baseHeight + bobbingOffset);
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 5f);
            }
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
            
            // Apply tilt rotation to visual mesh
            if (visualMesh != null)
            {
                // Apply tilt to visual mesh (doesn't interfere with root facing direction or NavMeshAgent)
                visualMesh.localRotation = currentTilt;
            }
            else if (transform.childCount > 0)
            {
                // LEGACY: Apply to first child if no visualMesh assigned
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

        /// <summary>
        /// Keep the drone oriented toward its current target while allowing the NavMeshAgent to control position.
        /// </summary>
        protected virtual void ApplyLookRotation()
        {
            if (navMeshTarget == null) return;

            Vector3 direction = navMeshTarget.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
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
            
            // Show root position (NavMeshAgent position)
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
            
            // Show visual mesh position (if assigned)
            if (visualMesh != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(visualMesh.position, 0.2f);
                Gizmos.DrawLine(transform.position, visualMesh.position);
            }
            
            // Draw hover height line from root
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * hoverHeight);
            
            // Draw hover height sphere
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            Gizmos.DrawSphere(transform.position + Vector3.up * hoverHeight, 0.5f);
            
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

