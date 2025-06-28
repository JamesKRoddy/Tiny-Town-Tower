using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using System;
using Managers;

namespace Enemies
{
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
        }
        
        protected virtual void OnDestroy()
        {
            OnTargetDestroyedEvent -= OnTargetDestroyed;
        }

        protected virtual void Update()
        {
            // Don't do anything if dead or no target
            if (Health <= 0 || navMeshTarget == null)
                return;

            // Check if current target is still valid
            if (!IsTargetStillValid(navMeshTarget))
            {
                Debug.Log($"{gameObject.name}: Target {navMeshTarget?.name} is no longer valid, finding new target");
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

            // Get the root motion delta (movement from animation)
            Vector3 rootMotion = animator.deltaPosition;
            rootMotion.y = 0; // We don't want to apply any vertical movement (gravity, etc.)

            // Calculate the new position
            Vector3 newPosition = transform.position + rootMotion;

            // Sample the NavMesh with a larger radius to ensure we stay on it
            if (NavMesh.SamplePosition(newPosition, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
            {
                // Move the enemy using root motion
                transform.position = hit.position;
                
                // Update agent's position to match with root motion
                agent.nextPosition = hit.position;
            }
            else if (NavMesh.FindClosestEdge(transform.position, out NavMeshHit edgeHit, NavMesh.AllAreas))
            {
                // Move to the nearest valid position
                transform.position = edgeHit.position;
                agent.Warp(edgeHit.position);
            }
            else
            {
                // If we can't find any valid position, disable root motion temporarily
                useRootMotion = false;
                agent.updatePosition = true;
                agent.updateRotation = true;
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
            agent.updateRotation = true;
            agent.updateUpAxis = false;
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
            agent.updatePosition = false;
            agent.updateRotation = true;
            
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                agent.Warp(hit.position);
            }
            else
            {
                Debug.LogWarning($"Enemy {gameObject.name} could not be placed on NavMesh at spawn position {transform.position}");
            }
        }

        #endregion

        #region Movement & Targeting

        private void UpdateMovement()
        {
            // Update the destination continuously
            agent.SetDestination(navMeshTarget.position);
            
            // Check if we're stuck (not moving towards target)
            CheckIfStuck();
            
            // Handle rotation towards target
            UpdateRotation();
        }

        private void UpdateRotation()
        {
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
                
                if (distanceMoved < stuckThreshold && agent.velocity.magnitude < 0.1f)
                {
                    Debug.Log($"{gameObject.name}: Detected as stuck, attempting to get unstuck");
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
                        Debug.Log($"{gameObject.name}: Found unstuck path to {testPosition}");
                        agent.SetDestination(testPosition);
                        return;
                    }
                }
            }
            
            Debug.Log($"{gameObject.name}: No unstuck path found, trying direct movement");
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
                        Debug.Log($"{gameObject.name}: Current target {navMeshTarget.name} is no longer reachable, finding new target");
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
                Debug.Log($"{gameObject.name}: Found new target {newTarget.name}");
            }
            else
            {
                navMeshTarget = null;
                if (agent != null)
                {
                    agent.ResetPath();
                    agent.isStopped = true;
                }
                Debug.LogWarning($"{gameObject.name}: No new target found! Staying in place.");
            }
        }

        /// <summary>
        /// Find a camp target with simple prioritization
        /// </summary>
        private Transform FindCampTarget()
        {
            List<Transform> npcTargets = new List<Transform>();
            List<Transform> buildingTargets = new List<Transform>();
            List<Transform> wallTargets = new List<Transform>();
            
            Debug.Log($"{gameObject.name}: Starting FindCampTarget...");
            
            // Cache the FindObjectsByType result to avoid multiple calls
            var allDamageables = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            Debug.Log($"{gameObject.name}: Found {allDamageables.Length} total objects to check");
            
            // Process all damageable objects in a single pass
            foreach (var obj in allDamageables)
            {
                if (obj is IDamageable damageable && damageable.GetAllegiance() == Allegiance.FRIENDLY)
                {
                    Transform targetTransform = obj.transform;
                    Debug.Log($"{gameObject.name}: Found friendly target {obj.name} of type {obj.GetType().Name}");
                    
                    CategorizeTarget(obj, damageable, targetTransform, npcTargets, buildingTargets, wallTargets);
                }
            }
            
            Debug.Log($"{gameObject.name}: Categorized targets - NPCs: {npcTargets.Count}, Buildings: {buildingTargets.Count}, Walls: {wallTargets.Count}");
            
            // Simple priority: NPCs > Buildings > Walls
            Transform target = FindClosestReachableTarget(npcTargets);
            if (target != null) 
            {
                Debug.Log($"{gameObject.name}: Found NPC target {target.name}");
                return target;
            }
            
            target = FindClosestReachableTarget(buildingTargets);
            if (target != null) 
            {
                Debug.Log($"{gameObject.name}: Found building target {target.name}");
                return target;
            }
            
            target = FindClosestReachableTarget(wallTargets);
            if (target != null) 
            {
                Debug.Log($"{gameObject.name}: Found wall target {target.name}");
                return target;
            }
            
            Debug.LogWarning($"{gameObject.name}: No targets found! NPCs: {npcTargets.Count}, Buildings: {buildingTargets.Count}, Walls: {wallTargets.Count}");
            return null;
        }

        private void CategorizeTarget(MonoBehaviour obj, IDamageable damageable, Transform targetTransform, 
            List<Transform> npcTargets, List<Transform> buildingTargets, List<Transform> wallTargets)
        {
            // Check health once at the start - if no health, don't categorize
            if (damageable.Health <= 0)
            {
                Debug.Log($"{gameObject.name}: {obj.name} has no health ({damageable.Health}), skipping");
                return;
            }

            if (obj is HumanCharacterController)
            {
                npcTargets.Add(targetTransform);
                Debug.Log($"{gameObject.name}: Added NPC target {obj.name} with health {damageable.Health}");
            }
            else if (obj is Building building)
            {
                if (building.IsOperational())
                {
                    if (building is WallBuilding wallBuilding)
                    {
                        // Double-check that the wall is not destroyed and still exists
                        if (!wallBuilding.IsDestroyed && !wallBuilding.IsBeingDestroyed && wallBuilding.gameObject != null)
                        {
                            wallTargets.Add(targetTransform);
                            Debug.Log($"{gameObject.name}: Added wall target {obj.name}");
                        }
                        else
                        {
                            Debug.Log($"{gameObject.name}: Wall {obj.name} is destroyed or being destroyed");
                        }
                    }
                    else
                    {
                        buildingTargets.Add(targetTransform);
                        Debug.Log($"{gameObject.name}: Added building target {obj.name}");
                    }
                }
                else
                {
                    Debug.Log($"{gameObject.name}: Building {obj.name} is not operational");
                }
            }
            else if (obj.GetType().Name.Contains("Turret"))
            {
                buildingTargets.Add(targetTransform);
                Debug.Log($"{gameObject.name}: Added turret target {obj.name}");
            }
        }

        private Transform FindClosestReachableTarget(List<Transform> targets)
        {
            Debug.Log($"{gameObject.name}: Checking {targets.Count} potential targets for reachability");
            
            foreach (var target in targets)
            {
                if (target == null) continue;
                
                float distance = Vector3.Distance(transform.position, target.position);
                Debug.Log($"{gameObject.name}: Checking target {target.name} at distance {distance:F1}");
                
                bool isReachable = IsTargetReachable(target.position);
                Debug.Log($"{gameObject.name}: Target {target.name} reachable: {isReachable}");
                
                if (isReachable)
                {
                    Debug.Log($"{gameObject.name}: Selected {target.name} as reachable target at distance {distance:F1}");
                    return target;
                }
            }
            
            Debug.LogWarning($"{gameObject.name}: No reachable targets found from {targets.Count} potential targets!");
            return null;
        }

        private bool IsTargetReachable(Vector3 targetPosition)
        {
            Debug.Log($"{gameObject.name}: Checking reachability to position {targetPosition}");
            
            // Try direct pathfinding first
            NavMeshPath path = new NavMeshPath();
            bool pathFound = NavMesh.CalculatePath(transform.position, targetPosition, NavMesh.AllAreas, path);
            
            Debug.Log($"{gameObject.name}: Direct path to target - Found: {pathFound}, Status: {path.status}");
            
            if (pathFound && path.status == NavMeshPathStatus.PathComplete)
            {
                Debug.Log($"{gameObject.name}: Direct path successful!");
                return true;
            }
            
            // Try nearby positions if direct path fails
            Vector3 directionToTarget = (targetPosition - transform.position).normalized;
            float[] testDistances = { 2f, 3f, 4f, 5f };
            
            foreach (float distance in testDistances)
            {
                Vector3 nearTargetPosition = targetPosition - directionToTarget * distance;
                
                NavMeshPath nearPath = new NavMeshPath();
                bool nearPathFound = NavMesh.CalculatePath(transform.position, nearTargetPosition, NavMesh.AllAreas, nearPath);
                
                Debug.Log($"{gameObject.name}: Near path at {distance}m - Found: {nearPathFound}, Status: {nearPath.status}");
                
                if (nearPathFound && nearPath.status == NavMeshPathStatus.PathComplete)
                {
                    Debug.Log($"{gameObject.name}: Near path successful at {distance}m!");
                    return true;
                }
            }
            
            // Fallback for very close targets
            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
            if (distanceToTarget < 10f)
            {
                Debug.Log($"{gameObject.name}: Pathfinding failed, but target is very close ({distanceToTarget:F1}m). Using fallback movement.");
                return true;
            }
            
            Debug.LogWarning($"{gameObject.name}: No path found to target or nearby positions!");
            return false;
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
            
            return damageable.GetAllegiance() == Allegiance.FRIENDLY;
        }

        #endregion

        #region Target Destruction Handling

        public void OnTargetDestroyed(Transform destroyedTarget)
        {
            if (navMeshTarget == destroyedTarget)
            {
                Debug.Log($"{gameObject.name}: Target {destroyedTarget?.name} was destroyed, finding new target after delay");
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
                
                if (NavMesh.SamplePosition(newPosition, out NavMeshHit hit, 0.1f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
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

        #endregion
    }
}
