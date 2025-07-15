using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using System;
using Managers;

namespace Enemies
{
    /// <summary>
    /// Enemy base class that provides common functionality for all enemy types.
    /// Handles movement, targeting, health, and basic AI behaviors.
    /// 
    /// NAVIGATION SYSTEM:
    /// - Uses Unity's NavMesh for pathfinding and obstacle avoidance
    /// - Supports both traditional NavMesh movement and pure root motion
    /// - Root motion: Animation drives movement completely, NavMesh agent handles pathfinding + rotation
    /// - Non-root motion: NavMesh agent drives both movement and turning
    /// 
    /// ROOT MOTION CONFIGURATION:
    /// - Set useRootMotion = true for pure animation-driven movement
    /// - Agent calculates paths and handles rotation, animation controls speed/movement
    /// - Character follows animation exactly, staying on NavMesh
    /// - Excellent animation quality with natural movement patterns
    /// 
    /// ANIMATION PARAMETERS:
    /// - "move": Boolean indicating if the character should be moving
    /// - "Speed": Float controlling walk animation (0=idle, 1=full speed)
    /// - Note: Speed is normalized based on NavMeshAgent velocity
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    public class EnemyBase : MonoBehaviour, IDamageable
    {
        #region Constants
        
        protected const float DEFAULT_ATTACK_ANGLE_THRESHOLD = 45f;
        protected const float MOVEMENT_VELOCITY_THRESHOLD = 0.1f;
        protected const float TARGET_SEARCH_DELAY = 0.2f;
        protected const float NAVMESH_SAMPLE_DISTANCE = 2.0f;
        protected const float UNSTUCK_SEARCH_RADIUS = 3f;
        protected const float KNOCKBACK_SAMPLE_DISTANCE = 0.5f;
        
        // Attack rotation constants
        protected const float ATTACK_READY_ANGLE_THRESHOLD = 5f; // Must be within 5 degrees to attack
        protected const float ROTATION_TOWARDS_TARGET_SPEED_MULTIPLIER = 3f; // Faster rotation when preparing to attack
        
        #endregion
        
        #region Serialized Fields
        
        [Header("Character Type")]
        [SerializeField] protected CharacterType characterType = CharacterType.ZOMBIE_MELEE;

        [Header("Movement Settings")]
        [SerializeField] protected bool useRootMotion = false;
        [SerializeField] protected float stoppingDistance = 1.5f;
        [SerializeField] protected float rotationSpeed = 10f; // Only used for non-root motion
        [SerializeField] protected float movementSpeed = 3.5f;
        [SerializeField] protected float acceleration = 8f;
        [SerializeField] protected float angularSpeed = 120f;
        [SerializeField] protected float obstacleBoundsOffset = 1f;

        // Add these fields for better root motion control
        [Header("Root Motion Settings")]
        [SerializeField] protected float rootMotionMultiplier = 1f;
        [SerializeField] protected bool debugRootMotion = false; // Enable to debug teleporting issues

        [Header("Health Settings")]
        [SerializeField] private float health = 100f;
        [SerializeField] private float maxHealth = 100f;

        #endregion

        #region Protected Fields
        
        protected NavMeshAgent agent;
        protected Animator animator;
        protected Transform navMeshTarget;        
        protected bool isAttacking = false;
        protected bool isRotatingToAttack = false; // New state for rotation phase before attack
        protected float damage;

        // Material flash effect
        protected SkinnedMeshRenderer skinnedMeshRenderer;
        protected Material originalMaterial;
        protected Material flashMaterial;
        protected float flashDuration = 0.5f;
        protected Color flashColor = new Color(1f, 0.1f, 0.1f);

        #endregion

        #region Public Properties & Events
        
        public Transform NavMeshTarget => navMeshTarget;
        public float Health
        {
            get => health;
            set => health = Mathf.Clamp(value, 0, maxHealth);
        }
        public float MaxHealth
        {
            get => maxHealth;
            set => maxHealth = value;
        }
        public CharacterType CharacterType => characterType;
        public Allegiance GetAllegiance() => Allegiance.HOSTILE;

        public event Action<float, float> OnDamageTaken;
        public event Action<float, float> OnHeal;
        public event Action OnDeath;
        public static event System.Action<Transform> OnTargetDestroyedEvent;

        #endregion

        #region Private Fields - Targeting & Movement
        
        private float lastStuckCheckTime = 0f;
        private Vector3 lastPosition = Vector3.zero;
        private float stuckThreshold = 0.5f;
        private float stuckCheckInterval = 2f;
        
        private float lastReachabilityCheckTime = 0f;
        private float reachabilityCheckInterval = 1f;

        private float lastTargetSearchTime = 0f;
        private float targetSearchInterval = 3f; // Check for new targets every 3 seconds

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            InitializeComponents();
            SetupNavMeshAgent();
            SetupMaterialFlash();
            
            if (useRootMotion)
            {
                SetupRootMotion();
            }
        }

        protected virtual void Start()
        {
            // Subscribe to target destroyed events
            OnTargetDestroyedEvent += OnTargetDestroyed;
            
            // Initialize health
            Health = maxHealth;
            
            // Find initial target
            FindNewTarget();
            
            // Initialize timers
            lastPosition = transform.position;
            lastStuckCheckTime = Time.time;
            lastReachabilityCheckTime = Time.time;
            lastTargetSearchTime = Time.time;
        }
        
        protected virtual void OnDestroy()
        {
            OnTargetDestroyedEvent -= OnTargetDestroyed;
        }

        protected virtual void Update()
        {
            // Don't do anything if dead
            if (Health <= 0) 
            {
                if (Time.frameCount % 60 == 0) // Log every 60 frames to avoid spam
                {
                    Debug.Log($"[{gameObject.name}] Update called while dead! Health: {Health}");
                }
                return;
            }

            // If no target, periodically check for new targets
            if (navMeshTarget == null)
            {
                CheckForNewTargetsPeriodically();
                return;
            }

            // Check if current target is still valid
            if (!IsTargetStillValid(navMeshTarget))
            {
                FindNewTarget();
                return;
            }

            // Periodically check if current target is still reachable
            CheckTargetReachability();

            if (!isAttacking)
            {
                UpdateMovement();
            }
        }

        // This method is called by the Animator when root motion is being applied
        protected virtual void OnAnimatorMove()
        {
            if (!useRootMotion || Health <= 0 || !agent.isOnNavMesh) 
            {
                if (Health <= 0 && Time.frameCount % 60 == 0) // Log every 60 frames for dead zombies
                {
                    Debug.Log($"[{gameObject.name}] OnAnimatorMove called while dead! Health: {Health}, useRootMotion: {useRootMotion}");
                }
                return;
            }

            Vector3 oldPosition = transform.position;
            if (debugRootMotion)
            {
                Debug.Log($"[{gameObject.name}] OnAnimatorMove - Old Pos: {oldPosition}, Root Delta: {animator.deltaPosition}");
            }

            // Pure root motion approach: Animation drives movement completely
            Vector3 rootMotion = animator.deltaPosition * rootMotionMultiplier;
            rootMotion.y = 0; // Ignore vertical movement from animation

            // Calculate new position based purely on root motion
            Vector3 newPosition = transform.position + rootMotion;

            // Only ensure we stay on the NavMesh - don't constrain to agent position
            if (NavMesh.SamplePosition(newPosition, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                
                // Update the agent's position to follow the character (not the other way around)
                agent.nextPosition = hit.position;
            }
            else
            {
                // If we can't find a valid NavMesh position, try a smaller step
                Vector3 smallerStep = transform.position + rootMotion * 0.5f;
                if (NavMesh.SamplePosition(smallerStep, out NavMeshHit smallerHit, 1.0f, NavMesh.AllAreas))
            {
                    transform.position = smallerHit.position;
                    agent.nextPosition = smallerHit.position;
            }
                // If still no valid position, don't move this frame (stay where we are)
            }
            
            if (debugRootMotion)
            {
                Vector3 newPos = transform.position;
                float distanceMoved = Vector3.Distance(oldPosition, newPos);
                if (distanceMoved > 2f) // Only log if we moved a significant distance
                {
                    Debug.LogWarning($"[{gameObject.name}] Large movement detected! Distance: {distanceMoved}, Old: {oldPosition}, New: {newPos}");
                }
            }
        }

        #endregion

        #region Initialization

        private void InitializeComponents()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
        }

        private void SetupNavMeshAgent()
        {
            agent.stoppingDistance = stoppingDistance;
            agent.speed = movementSpeed;
            agent.acceleration = acceleration;
            agent.angularSpeed = angularSpeed;
            agent.updateUpAxis = false;
            
            // Configure for root motion if enabled
            if (useRootMotion)
            {
                agent.updatePosition = false;  // Root motion drives position
                agent.updateRotation = true;   // Agent drives rotation for pathfinding
            }
            else
            {
                agent.updatePosition = true;   // Agent drives position
                agent.updateRotation = true;   // Agent drives rotation
            }
        }

        private void SetupMaterialFlash()
        {
            skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null)
            {
                originalMaterial = skinnedMeshRenderer.material;
                flashMaterial = new Material(originalMaterial);
                flashMaterial.color = flashColor;
            }
        }



        protected virtual void SetupRootMotion()
        {
            // Configuration is now handled in SetupNavMeshAgent()
            
            // Ensure the character is on the NavMesh
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, NAVMESH_SAMPLE_DISTANCE, NavMesh.AllAreas))
            {
                // Only move if we're not already very close to a valid position
                if (Vector3.Distance(transform.position, hit.position) > MOVEMENT_VELOCITY_THRESHOLD)
            {
                transform.position = hit.position;
                agent.Warp(hit.position);
                    Debug.Log($"Enemy {gameObject.name} moved to valid NavMesh position: {hit.position}");
                }
            }
            else
            {
                Debug.LogWarning($"Enemy {gameObject.name} could not be placed on NavMesh at spawn position {transform.position}");
                // If we can't place on NavMesh, disable root motion
                useRootMotion = false;
                SetupNavMeshAgent(); // Reconfigure agent without root motion
            }
        }

        #endregion

        #region Movement & Targeting

        private void UpdateMovement()
        {
            if (navMeshTarget == null) return;
            
            // For root motion zombies, check if we should stop the agent
            if (useRootMotion)
            {
                float distanceToTarget = Vector3.Distance(transform.position, navMeshTarget.position);
                float effectiveAttackDistance = NavigationUtils.CalculateEffectiveReachDistance(transform.position, navMeshTarget, stoppingDistance, obstacleBoundsOffset);
                
                // Stop agent when in attack range or during attack phases
                // But maintain a minimum distance to prevent spinning
                float minDistance = 1.0f;
                bool shouldStop = (distanceToTarget <= effectiveAttackDistance && distanceToTarget >= minDistance) || isAttacking || isRotatingToAttack;
                
                if (shouldStop)
                {
                    if (!agent.isStopped)
                    {
                        agent.isStopped = true;
                        agent.velocity = Vector3.zero;
                    }
                }
                else
                {
                    // Resume movement when out of attack range or too close
                    if (agent.isStopped)
                    {
                        agent.isStopped = false;
                    }
                }
            }
            
            // Update the destination continuously
            agent.SetDestination(navMeshTarget.position);
            
            // Update animation parameters
            UpdateAnimationParameters();
            
            // Check if we're stuck (not moving towards target)
            CheckIfStuck();
            
            // For root motion, let Unity handle rotation automatically
            // For non-root motion, manually handle rotation
            if (!useRootMotion)
            {
                UpdateRotation();
            }
        }

        private void UpdateAnimationParameters()
        {
            if (animator == null) return;
            
            // Calculate normalized speed based on agent velocity
            float velocity = agent.velocity.magnitude;
            float normalizedSpeed = Mathf.Clamp01(velocity / movementSpeed);
            
            // Set Speed parameter (0-1 range) based on normalized velocity
            animator.SetFloat("Speed", normalizedSpeed);
        }

        private void UpdateRotation()
        {
            if (navMeshTarget == null) return;
            
            if (agent.velocity.magnitude > 0.1f)
            {
                Vector3 direction = (navMeshTarget.position - transform.position).normalized;
                direction.y = 0;
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }
        }

        private void CheckIfStuck()
        {
            if (Time.time - lastStuckCheckTime > stuckCheckInterval)
            {
                float distanceMoved = Vector3.Distance(transform.position, lastPosition);
                
                // For root motion, only check if we haven't moved physically
                // For non-root motion, also check agent velocity
                bool isStuck = distanceMoved < stuckThreshold;
                if (!useRootMotion)
                {
                    isStuck = isStuck && agent.velocity.magnitude < MOVEMENT_VELOCITY_THRESHOLD;
                }
                
                if (isStuck && navMeshTarget != null)
                {
                    AttemptToGetUnstuck();
                }
                
                lastPosition = transform.position;
                lastStuckCheckTime = Time.time;
            }
        }
        
        private void AttemptToGetUnstuck()
        {
            Vector3 directionToTarget = (navMeshTarget.position - transform.position).normalized;
            
            for (int i = 0; i < 8; i++)
            {
                float angle = i * DEFAULT_ATTACK_ANGLE_THRESHOLD * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * UNSTUCK_SEARCH_RADIUS;
                Vector3 testPosition = navMeshTarget.position + offset;
                
                NavMeshPath testPath = new NavMeshPath();
                if (NavMesh.CalculatePath(transform.position, testPosition, NavMesh.AllAreas, testPath))
                {
                    if (testPath.status == NavMeshPathStatus.PathComplete || testPath.status == NavMeshPathStatus.PathPartial)
                    {
                        agent.SetDestination(testPosition);
                        return;
                    }
                }
            }
            
            Vector3 directTarget = navMeshTarget.position + directionToTarget * 2f;
            agent.SetDestination(directTarget);
        }

        private void CheckTargetReachability()
        {
            if (Time.time - lastReachabilityCheckTime > reachabilityCheckInterval)
            {
                if (navMeshTarget != null)
                {
                    bool isStillReachable = IsTargetReachable(navMeshTarget.position);
                    if (!isStillReachable)
                    {
                        FindNewTarget();
                    }
                }
                lastReachabilityCheckTime = Time.time;
            }
        }



        #endregion

        #region Target Finding

        private void FindNewTarget()
        {
            Transform newTarget = null;

            if (GameManager.Instance == null)
            {
                Debug.LogWarning($"{gameObject.name}: GameManager.Instance is null, cannot determine game mode");
                return;
            }

            switch (GameManager.Instance.CurrentGameMode)
            {
                case GameMode.ROGUE_LITE:
                    if (PlayerController.Instance != null && PlayerController.Instance._possessedNPC != null)
                    {
                        newTarget = PlayerController.Instance._possessedNPC.GetTransform();
                    }
                    break;
                    
                case GameMode.CAMP:
                case GameMode.CAMP_ATTACK:
                    newTarget = FindCampTarget();
                    break;
                    
                default:
                    Debug.LogWarning($"No targeting logic for game mode: {GameManager.Instance.CurrentGameMode}");
                    break;
            }

            if (newTarget != null)
            {
                navMeshTarget = newTarget;
                // Speed will be set by UpdateAnimationParameters based on agent velocity
            }
            else
            {
                // No new target found, clear current target and stop moving
                navMeshTarget = null;
                StopMoving();
                Debug.LogWarning($"{gameObject.name}: No new target found! Stopping movement.");
            }
        }

        private void StopMoving()
        {
            if (agent != null)
            {
                agent.ResetPath();
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                // Speed will be set to 0 by UpdateAnimationParameters when velocity is zero
            }
        }

        /// <summary>
        /// Find a camp target with simple prioritization
        /// </summary>
        private Transform FindCampTarget()
        {
            List<Transform> npcTargets = new List<Transform>();
            List<Transform> buildingTargets = new List<Transform>();
            
            // Use CampManager's cached target system for efficiency
            CampManager.Instance.GetCategorizedTargets(npcTargets, buildingTargets);
            
            // Simple priority: NPCs > Buildings (including turrets)
            Transform target = FindClosestReachableTarget(npcTargets);
            if (target != null) 
            {
                return target;
            }
            
            target = FindClosestReachableTarget(buildingTargets);
            if (target != null) 
            {
                return target;
            }
            
            Debug.LogWarning($"{gameObject.name}: No targets found! NPCs: {npcTargets.Count}, Buildings: {buildingTargets.Count}");
            return null;
        }



        private Transform FindClosestReachableTarget(List<Transform> targets)
        {
            return NavigationUtils.FindClosestReachableTarget(transform.position, targets, 5f);
        }

        private bool IsTargetReachable(Vector3 targetPosition)
        {
            return NavigationUtils.IsTargetReachable(transform.position, targetPosition, 5f);
        }

        /// <summary>
        /// Check if the target is still a valid, attackable target.
        /// This method is used both for target selection and attack validation.
        /// </summary>
        protected bool IsTargetStillValid(Transform target)
        {
            if (target == null || target.gameObject == null) return false;
            
            // Check if the target is still active in the scene (catches NPCs in bunkers)
            if (!target.gameObject.activeInHierarchy) return false;
            
            var damageable = target.GetComponent<IDamageable>();
            if (damageable == null || damageable.Health <= 0) return false;
            
            // Special check for walls - they might be destroyed but still have health > 0
            if (target.GetComponent<WallBuilding>() is WallBuilding wallBuilding)
            {
                if (wallBuilding.IsDestroyed || wallBuilding.IsBeingDestroyed) return false;
            }
            
            return damageable.GetAllegiance() == Allegiance.FRIENDLY;
        }

        #endregion

        #region Target Destruction Handling

        public void OnTargetDestroyed(Transform destroyedTarget)
        {
            if (navMeshTarget == destroyedTarget)
            {
                StartCoroutine(FindNewTargetAfterDelay());
            }
        }
        
        private IEnumerator FindNewTargetAfterDelay()
        {
            yield return new WaitForSeconds(TARGET_SEARCH_DELAY);
            FindNewTarget();
        }
        
        public static void NotifyTargetDestroyed(Transform destroyedTarget)
        {
            OnTargetDestroyedEvent?.Invoke(destroyedTarget);
        }

        #endregion

        #region Attack Rotation Utilities

        /// <summary>
        /// Checks if the enemy is properly facing the target and ready to attack
        /// </summary>
        /// <returns>True if within attack-ready angle threshold</returns>
        protected bool IsReadyToAttack()
        {
            if (navMeshTarget == null) return false;
            
            Vector3 directionToTarget = (navMeshTarget.position - transform.position).normalized;
            float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
            
            return angleToTarget <= ATTACK_READY_ANGLE_THRESHOLD;
        }

        /// <summary>
        /// Rotates towards target with enhanced speed for attack preparation
        /// </summary>
        /// <returns>True if rotation is complete and ready to attack</returns>
        protected bool RotateTowardsTargetForAttack()
        {
            if (navMeshTarget == null) return false;
            
            // Don't rotate if dead
            if (Health <= 0) 
            {
                Debug.Log($"[{gameObject.name}] RotateTowardsTargetForAttack called while dead! Health: {Health}");
                return false;
            }

            Vector3 direction = (navMeshTarget.position - transform.position).normalized;
            direction.y = 0;
            if (direction == Vector3.zero) return true;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            float enhancedRotationSpeed = rotationSpeed * ROTATION_TOWARDS_TARGET_SPEED_MULTIPLIER;
            
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, enhancedRotationSpeed * Time.deltaTime);
            
            return IsReadyToAttack();
        }

        #endregion

        #region Attack Validation

        /// <summary>
        /// Validates if an attack can be performed on the current target
        /// </summary>
        /// <param name="attackRange">The range for this specific attack</param>
        /// <param name="angleThreshold">Maximum angle deviation for attack</param>
        /// <param name="distanceToTarget">Current distance to target (output)</param>
        /// <param name="angleToTarget">Current angle to target (output)</param>
        /// <returns>True if attack is valid</returns>
        protected bool ValidateAttack(float attackRange, float angleThreshold, out float distanceToTarget, out float angleToTarget)
        {
            distanceToTarget = 0f;
            angleToTarget = 0f;

            // Basic target validation
            if (navMeshTarget == null || !IsTargetStillValid(navMeshTarget))
            {
                return false;
            }

            // Distance validation
            distanceToTarget = Vector3.Distance(transform.position, navMeshTarget.position);
            float effectiveAttackDistance = NavigationUtils.CalculateEffectiveReachDistance(transform.position, navMeshTarget, attackRange, obstacleBoundsOffset);
            
            if (distanceToTarget > effectiveAttackDistance)
            {
                return false;
            }

            // Angle validation
            Vector3 directionToTarget = (navMeshTarget.position - transform.position).normalized;
            angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
            
            return angleToTarget <= angleThreshold;
        }

        #endregion

        #region Combat & Damage

        protected virtual void BeginAttackSequence()
        {
            animator.SetBool("Attack", true);
            isAttacking = true;
            isRotatingToAttack = false; // Stop rotation phase

            // Stop rotation completely during attacks for both root motion and non-root motion
            if (useRootMotion)
            {
                agent.updateRotation = false;
            }
            // For non-root motion, rotation will be prevented in the update logic by checking isAttacking
        }

        protected virtual void EndAttack()
        {
            animator.SetBool("Attack", false);
            isAttacking = false;
            isRotatingToAttack = false; // Reset rotation state

            // Resume rotation after attack
            if (useRootMotion)
            {
                agent.updateRotation = true;
            }
            // For non-root motion, rotation will resume automatically in update logic
        }

        public void TakeDamage(float amount, Transform damageSource = null)
        {
            float previousHealth = Health;
            Health -= amount;

            if (animator != null)
            {
                animator.ResetTrigger("Attack");
                animator.Play("Default", 1, 0);
                animator.SetTrigger("Damaged");
            }

            OnDamageTaken?.Invoke(amount, Health);

            if (damageSource != null)
            {
                Vector3 hitPoint = transform.position + Vector3.up * 1.5f;
                Vector3 hitNormal = (transform.position - damageSource.position).normalized;
                EffectManager.Instance.PlayHitEffect(hitPoint, hitNormal, this);
                
                HandleDamageReaction(damageSource);
            }

            if (Health <= 0)
            {
                Die();
            }
        }

        protected virtual void HandleDamageReaction(Transform damageSource)
        {
            Vector3 direction = (damageSource.position - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f);

                // Add knockback effect
                Vector3 knockbackDirection = -direction;
                float maxKnockbackDistance = 1.0f;
                float distanceFromSource = Vector3.Distance(transform.position, damageSource.position);
                float knockbackDistance = Mathf.Lerp(maxKnockbackDistance, maxKnockbackDistance * 0.3f, distanceFromSource / 5f);
                Vector3 newPosition = transform.position + knockbackDirection * knockbackDistance;

                if (NavMesh.SamplePosition(newPosition, out NavMeshHit hit, knockbackDistance, NavMesh.AllAreas))
                {
                    StartCoroutine(KnockbackRoutine(hit.position));
                }
            }
        }

        private IEnumerator KnockbackRoutine(Vector3 targetPosition)
        {
            float duration = 0.2f;
            float elapsed = 0f;
            Vector3 startPosition = transform.position;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                Vector3 newPosition = Vector3.Lerp(startPosition, targetPosition, t);
                
                if (NavMesh.SamplePosition(newPosition, out NavMeshHit hit, KNOCKBACK_SAMPLE_DISTANCE, NavMesh.AllAreas))
                {
                    if (useRootMotion)
                    {
                        // For root motion, just move the transform - the agent will catch up
                        transform.position = hit.position;
                    }
                    else
                    {
                        // For non-root motion, use agent.Warp
                    agent.Warp(hit.position);
                    }
                }
                
                yield return null;
            }
        }

        public virtual void Die()
        {
            Debug.Log($"[{gameObject.name}] Die() called! Health: {Health}");
            
            OnDeath?.Invoke();
            
            // Play death animation
            if (animator != null)
            {
                Debug.Log($"[{gameObject.name}] Setting death animation and disabling root motion. applyRootMotion before: {animator.applyRootMotion}");
                animator.SetTrigger("Dead");
                // Disable root motion to prevent dead zombies from rotating
                animator.applyRootMotion = false;
                Debug.Log($"[{gameObject.name}] applyRootMotion after: {animator.applyRootMotion}");
            }
            
            // Disable components
            isAttacking = false;
            isRotatingToAttack = false; // Stop any rotation attempts
            agent.enabled = false;
            GetComponent<Collider>().enabled = false;

            // Drop loot with 50% chance
            int shouldDropLoot = UnityEngine.Random.Range(0, 100);
            if (shouldDropLoot < 50)
            {
                GameManager.Instance.ResourceManager.SpawnCharacterLoot(characterType, DifficultyManager.Instance.GetCurrentWaveDifficulty(), transform.position + Vector3.up * 1.0f);
            }

            // Play death VFX
            Vector3 deathPoint = transform.position + Vector3.up * 1.5f;
            Vector3 deathNormal = Vector3.up;
            EffectManager.Instance.PlayDeathEffect(deathPoint, deathNormal, this);

            // Destroy after delay
            Destroy(gameObject, 10f);
        }

        #endregion

        #region Utility Methods

        public void Heal(float amount)
        {
            Health = Mathf.Min(Health + amount, MaxHealth);
        }

        /// <summary>
        /// Called from the animator to flash the material when the enemy is attacking
        /// </summary>
        public void AttackWarning()
        {
            if (skinnedMeshRenderer != null)
            {
                StartCoroutine(FlashMaterialCoroutine());
            }
        }

        private IEnumerator FlashMaterialCoroutine()
        {
            skinnedMeshRenderer.material = flashMaterial;
            yield return new WaitForSeconds(flashDuration);
            skinnedMeshRenderer.material = originalMaterial;
        }

        internal void Setup(Transform navAgentTarget)
        {
            navMeshTarget = navAgentTarget;
        }

        /// <summary>
        /// Sets the destination for the NavMesh agent
        /// </summary>
        public void SetDestination(Vector3 destination)
        {
            agent.SetDestination(destination);
        }

        private void CheckForNewTargetsPeriodically()
        {
            if (Time.time - lastTargetSearchTime > targetSearchInterval)
            {
                FindNewTarget();
                lastTargetSearchTime = Time.time;
            }
        }

        #endregion

        #region Debug Visualization

        /// <summary>
        /// Draw debug gizmos to visualize attack ranges and obstacle bounds
        /// </summary>
        protected virtual void OnDrawGizmosSelected()
        {
            if (navMeshTarget == null) return;

            // Draw the base stopping distance
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, stoppingDistance);

            // Draw the effective attack distance
            float effectiveDistance = NavigationUtils.CalculateEffectiveReachDistance(transform.position, navMeshTarget, stoppingDistance, obstacleBoundsOffset);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, effectiveDistance);

            // Draw a line to the target
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, navMeshTarget.position);

            // Draw the target's bounds if it has a collider
            Collider targetCollider = navMeshTarget.GetComponent<Collider>();
            if (targetCollider != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(targetCollider.bounds.center, targetCollider.bounds.size);
            }

            // Draw NavMeshObstacle bounds if present
            NavMeshObstacle obstacle = navMeshTarget.GetComponent<NavMeshObstacle>();
            if (obstacle != null)
            {
                Gizmos.color = Color.magenta;
                Vector3 obstacleSize = obstacle.size;
                Gizmos.DrawWireCube(navMeshTarget.position, obstacleSize);
            }
        }

        #endregion
    }
}
