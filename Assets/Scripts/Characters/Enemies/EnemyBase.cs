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
    /// - "WalkType": Float controlling walk animation (0=idle, 1=moving)
    /// - Note: Velocity parameters removed - animation drives speed directly
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    public class EnemyBase : MonoBehaviour, IDamageable
    {
        #region Serialized Fields
        
        [Header("Character Type")]
        [SerializeField] protected CharacterType characterType = CharacterType.ZOMBIE_MELEE;

        [Header("Movement Settings")]
        [SerializeField] protected bool useRootMotion = false;
        [SerializeField] protected float stoppingDistance = 1.5f;
        [SerializeField] protected float rotationSpeed = 10f;
        [SerializeField] protected float movementSpeed = 3.5f;
        [SerializeField] protected float acceleration = 8f;
        [SerializeField] protected float angularSpeed = 120f;
        [SerializeField] protected float obstacleBoundsOffset = 1f; // Additional distance to add to obstacle bounds

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
            
            // Initialize stuck detection
            lastPosition = transform.position;
            lastStuckCheckTime = Time.time;
            
            // Initialize reachability check
            lastReachabilityCheckTime = Time.time;
            
            // Initialize target search timer
            lastTargetSearchTime = Time.time;
        }
        
        protected virtual void OnDestroy()
        {
            OnTargetDestroyedEvent -= OnTargetDestroyed;
        }

        protected virtual void Update()
        {
            // Don't do anything if dead
            if (Health <= 0) return;

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
            if (!useRootMotion || Health <= 0 || !agent.isOnNavMesh) return;
            
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

        private void InitializeTimers()
        {
            lastPosition = transform.position;
            lastStuckCheckTime = Time.time;
            lastReachabilityCheckTime = Time.time;
        }

        protected virtual void SetupRootMotion()
        {
            // Configuration is now handled in SetupNavMeshAgent()
            
            // Ensure the character is on the NavMesh
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                // Only move if we're not already very close to a valid position
                if (Vector3.Distance(transform.position, hit.position) > 0.1f)
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
            // Don't update movement if no target
            if (navMeshTarget == null) return;
            
            // Calculate effective attack distance considering obstacles using shared utility
            float effectiveAttackDistance = NavigationUtils.CalculateEffectiveReachDistance(transform.position, navMeshTarget, stoppingDistance, obstacleBoundsOffset);
            
            // Update the destination continuously
            agent.SetDestination(navMeshTarget.position);
            
            // Update animation parameters based on agent velocity
            UpdateAnimationParameters();
            
            // Check if we're stuck (not moving towards target)
            CheckIfStuck();
            
            // Handle rotation towards target (only if not using root motion, as agent handles rotation)
            if (!useRootMotion)
            {
                UpdateRotation();
            }
        }

        private void UpdateAnimationParameters()
        {
            if (animator == null) return;
            
            bool shouldMove = false;
            
            if (useRootMotion)
            {
                // For root motion, determine movement based on whether we have a target and aren't attacking
                // The animation will drive the actual speed - we just tell it whether to move or not
                if (navMeshTarget != null && !isAttacking)
                {
                    float distanceToTarget = Vector3.Distance(transform.position, navMeshTarget.position);
                    float effectiveStoppingDistance = NavigationUtils.CalculateEffectiveReachDistance(transform.position, navMeshTarget, stoppingDistance, obstacleBoundsOffset);
                    
                    // Only move if we're not already at the target
                    shouldMove = distanceToTarget > effectiveStoppingDistance;
                }
            }
            else
            {
                // For non-root motion, use agent's actual velocity
                shouldMove = agent.velocity.magnitude > 0.1f;
            }
            
            // Set walk type based on movement (this is what RandomZombieAnimation uses)
            animator.SetFloat("WalkType", shouldMove ? 1f : 0f);
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
                    isStuck = isStuck && agent.velocity.magnitude < 0.1f;
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
                float angle = i * 45f * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 3f;
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

        /// <summary>
        /// Calculate the effective attack distance considering NavMesh obstacles and their bounds
        /// </summary>
        /// <param name="target">The target transform</param>
        /// <returns>The effective distance required to attack this target</returns>
        protected virtual float CalculateEffectiveAttackDistance(Transform target)
        {
            return NavigationUtils.CalculateEffectiveReachDistance(transform.position, target, stoppingDistance, obstacleBoundsOffset);
        }

        /// <summary>
        /// Check if the enemy is close enough to attack the target
        /// </summary>
        /// <param name="target">The target to check distance to</param>
        /// <returns>True if close enough to attack</returns>
        protected virtual bool IsCloseEnoughToAttack(Transform target)
        {
            return NavigationUtils.IsCloseEnoughToReach(transform.position, target, stoppingDistance, obstacleBoundsOffset);
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
                animator.SetFloat("WalkType", 1);
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
                animator.SetFloat("WalkType", 0);
                agent.ResetPath();
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
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
        /// Check if the target is still a valid, attackable target
        /// </summary>
        private bool IsTargetStillValid(Transform target)
        {
            if (target == null || target.gameObject == null) return false;
            
            var damageable = target.GetComponent<IDamageable>();
            if (damageable == null || damageable.Health <= 0) return false;
            
            // Special check for walls - they might be destroyed but still have health > 0
            if (target.GetComponent<WallBuilding>() is WallBuilding wallBuilding)
            {
                if (wallBuilding.IsDestroyed || wallBuilding.IsBeingDestroyed) return false;
            }
            
            // Check if the target is still active in the scene
            if (!target.gameObject.activeInHierarchy) return false;
            
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
            yield return new WaitForSeconds(0.2f);
            FindNewTarget();
        }
        
        public static void NotifyTargetDestroyed(Transform destroyedTarget)
        {
            OnTargetDestroyedEvent?.Invoke(destroyedTarget);
        }

        #endregion

        #region Combat & Damage

        protected virtual void BeginAttackSequence()
        {
            animator.SetBool("Attack", true);
            isAttacking = true;

            if (useRootMotion)
            {
                agent.updateRotation = false;
            }
        }

        protected virtual void EndAttack()
        {
            animator.SetBool("Attack", false);
            isAttacking = false;

            if (useRootMotion)
            {
                agent.updateRotation = true;
            }
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
                
                if (NavMesh.SamplePosition(newPosition, out NavMeshHit hit, 0.5f, NavMesh.AllAreas))
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

        public void Die()
        {
            OnDeath?.Invoke();
            
            // Play death animation
            if (animator != null)
            {
                animator.SetTrigger("Dead");
            }
            
            // Disable components
            isAttacking = false;
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

        internal float GetDamageValue()
        {
            return damage;
        }

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

        public void SetEnemyDestination(Vector3 navMeshTarget)
        {
            agent.SetDestination(navMeshTarget);
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
            float effectiveDistance = CalculateEffectiveAttackDistance(navMeshTarget);
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
