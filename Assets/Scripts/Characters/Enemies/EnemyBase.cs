using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using System;
using Managers;
using Combat;

namespace Enemies
{
    /// <summary>
    /// Enemy base class that provides common functionality for all enemy types.
    /// Handles movement, targeting, health, and basic AI behaviors.
    /// 
    /// ═══════════════════════════════════════════════════════════════════════════════════════
    /// ENEMY MANAGER INTEGRATION:
    /// ═══════════════════════════════════════════════════════════════════════════════════════
    /// This class integrates with EnemyManager for group coordination:
    /// 
    /// • AUTO-REGISTRATION: Registers/unregisters in Start() and OnDestroy()
    /// • PACK HUNTING: UpdateMovement() calls GetStrategicPosition() to get role-based destinations:
    ///   - CHASER role → flanking position around player
    ///   - INTERCEPTOR role → predicted player position (cuts off escape)
    ///   - WANDERER role → random wander point
    /// • ATTACK COORDINATION: Derived classes (e.g., Zombie) request attack permission
    /// • AGGRO TRACKING: AttackBase reports damage dealt for threat system
    /// 
    /// Result: Individual enemy AI enhanced by shared group intelligence!
    /// ═══════════════════════════════════════════════════════════════════════════════════════
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
    public class EnemyBase : MonoBehaviour, IDamageable, IStatusEffectTarget, IAttackOwner
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
        [SerializeField] public bool useRootMotion = false; // Made public for attack components
        [SerializeField] protected float stoppingDistance = 1.0f;
        [SerializeField] public float rotationSpeed = 10f; // Only used for non-root motion, made public for attack components
        [SerializeField] protected float movementSpeed = 3.5f;
        [SerializeField] protected float acceleration = 8f;
        [SerializeField] protected float angularSpeed = 120f;
        [SerializeField] protected float obstacleBoundsOffset = 1f;

        // Add these fields for better root motion control
        [Header("Root Motion Settings")]
        [SerializeField] protected float rootMotionMultiplier = 1f;
        [SerializeField] public bool showCollisionDebug = false; // Debug visualization for collision detection

        [Header("Cooldown Movement Settings")]
        [SerializeField] protected bool enableCooldownMovement = true;
        [SerializeField] protected float cooldownMovementMinDistance = 2f;
        [SerializeField] protected float cooldownMovementMaxDistance = 4f;
        [SerializeField] protected float cooldownMovementMinDuration = 1f;
        [SerializeField] protected float cooldownMovementMaxDuration = 3f;

        [Header("Head Tracking Settings")]
        [SerializeField] protected bool enableHeadTracking = true;
        [SerializeField] protected float headTrackingWeight = 0.8f;
        [SerializeField] protected float headTrackingRotationWeight = 0.5f;
        [SerializeField] protected float headTrackingLerpSpeed = 3f;
        [SerializeField] protected float maxHeadTrackingAngle = 60f;
        [SerializeField] protected float headTrackingDistance = 15f;

        [Header("Health Settings")]
        [SerializeField] private float health = 100f;
        [SerializeField] private float maxHealth = 100f;

        [Header("Poise Settings")]
        [SerializeField] private float poise = 50f;
        [SerializeField] private float maxPoise = 50f;
        [SerializeField] private float poiseRecoveryRate = 10f; // Poise recovered per second
        [SerializeField] private float poiseRecoveryDelay = 2f; // Delay before poise starts recovering

        [Header("Target Transform")]
        [Tooltip("Transform for VFX spawning and projectile targeting (typically the mesh/model). If not set, uses the main transform.")]
        [SerializeField] protected Transform targetTransform;

        #endregion

        #region Protected Fields
        
        protected NavMeshAgent agent;
        public Animator animator; // Made public for attack components
        protected Transform navMeshTarget;        
        public bool isAttacking = false; // Made public for attack components
        protected bool isRotatingToAttack = false; // New state for rotation phase before attack
        protected float damage;
        protected float lastAttackTime = -999f;
        
        // Back-away state management
        private bool isBackingAway = false;
        private float backAwayStartTime = 0f;
        private const float BACK_AWAY_DURATION = 1.5f; // Minimum time to spend backing away
        private const float BACK_AWAY_COOLDOWN = 2.0f; // Cooldown before backing away again

        // Cooldown movement state management
        private bool isMovingDuringCooldown = false;
        private bool isWaitingAtTarget = false; // New: Track if we're waiting at target position
        private float cooldownMovementStartTime = 0f;
        private float lastCooldownMovementEndTime = 0f; // Track when cooldown movement ended
        private float lastTargetChangeTime = 0f; // Track when we last changed the strafe target
        private float targetReachedTime = 0f; // New: Track when we reached the current target
        private Vector3 cooldownMovementTarget = Vector3.zero;
        private const float COOLDOWN_MOVEMENT_COOLDOWN = 1.0f; // Cooldown before starting new cooldown movement
        private const float TARGET_CHANGE_COOLDOWN = 2.5f; // Minimum time between target changes (increased for more deliberate movement)
        private const float WAIT_AT_TARGET_DURATION = 1.5f; // How long to wait at target before finding next one (increased for more deliberate behavior)

        // Head tracking state management
        private bool isHeadTrackingActive = false;
        private float currentHeadTrackingWeight = 0f;
        private Vector3 currentLookAtTarget = Vector3.zero;

        // Material flash effect
        protected SkinnedMeshRenderer skinnedMeshRenderer;
        protected MeshRenderer meshRenderer;
        protected Material originalMaterial;
        protected Material flashMaterial;
        protected float flashDuration = 0.5f;
        protected Color flashColor = new Color(1f, 0.1f, 0.1f);

        // Status Effect System - Enemy owns its gameplay status effect data (source of truth)
        // EffectManager handles VFX/presentation layer only
        protected HashSet<StatusEffectType> activeStatusEffects = new HashSet<StatusEffectType>();

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
        public float Poise
        {
            get => poise;
            set => poise = Mathf.Clamp(value, 0, maxPoise);
        }
        public float MaxPoise
        {
            get => maxPoise;
            set => maxPoise = value;
        }
        public CharacterType CharacterType => characterType;
        public Allegiance GetAllegiance() => Allegiance.HOSTILE;

        /// <summary>
        /// Gets the target transform for VFX spawning and projectile targeting.
        /// Returns the assigned targetTransform if set, otherwise falls back to the main transform.
        /// </summary>
        public virtual Transform GetTargetTransform()
        {
            return targetTransform != null ? targetTransform : transform;
        }

        public event Action<float, float> OnDamageTaken;
        public event Action<float, float> OnHeal;
        public event Action<float, float> OnPoiseBroken;
        public event Action OnDeath;
        public static event System.Action<Transform> OnTargetDestroyedEvent;
        
        // IDamageable hit reaction tracking
        public Vector3 LastHitOrigin { get; set; } = Vector3.zero;
        public float LastHitTime { get; set; } = -999f;
        public float LastHitPoiseDamage { get; set; } = 0f;

        #endregion
        
        #region IAttackOwner Implementation
        
        /// <summary>
        /// IAttackOwner: The animator component for attack animations
        /// </summary>
        public Animator OwnerAnimator => animator;
        
        /// <summary>
        /// IAttackOwner: Current attack target
        /// </summary>
        public Transform AttackTarget => navMeshTarget;
        
        /// <summary>
        /// IAttackOwner: NavMeshAgent for movement
        /// </summary>
        public NavMeshAgent OwnerNavMeshAgent => agent;
        
        /// <summary>
        /// IAttackOwner: Rotation speed for turning
        /// </summary>
        public float OwnerRotationSpeed => rotationSpeed;
        
        /// <summary>
        /// IAttackOwner: Whether currently attacking
        /// </summary>
        bool IAttackOwner.IsAttacking
        {
            get => isAttacking;
            set => isAttacking = value;
        }
        
        /// <summary>
        /// IAttackOwner: Check line of sight (explicit interface implementation)
        /// Delegates to the existing HasLineOfSight method with default height
        /// </summary>
        bool IAttackOwner.HasLineOfSight(Vector3 targetPosition)
        {
            return HasLineOfSight(targetPosition, 1.5f);
        }
        
        /// <summary>
        /// IAttackOwner: Whether using root motion
        /// </summary>
        bool IAttackOwner.UseRootMotion => useRootMotion;
        
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

        // Poise recovery fields
        private float lastPoiseDamageTime = 0f;
        private bool isPoiseBroken = false;

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
            
            // Initialize health and poise
            Health = maxHealth;
            Poise = maxPoise;
            
            // Apply character type-specific poise configuration if using defaults
            if (Mathf.Approximately(maxPoise, 50f)) // Check if using default value
            {
                ApplyCharacterTypePoiseConfig();
            }
            
            // Register with EnemyManager for group coordination
            if (EnemyManager.Instance != null)
            {
                EnemyManager.Instance.RegisterEnemy(this);
            }
            
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
            
            // Unregister from EnemyManager
            if (EnemyManager.Instance != null)
            {
                EnemyManager.Instance.UnregisterEnemy(this);
            }
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
            
            // Apply procedural knockback from hit reactions
            ApplyProceduralKnockback();

            // If no target, periodically check for new targets
            if (navMeshTarget == null)
            {
                CheckForNewTargetsPeriodically();
                return;
            }

            // Don't validate or switch targets while actively attacking
            // This prevents glitchy rotation when player moves out of range during attack
            if (!isAttacking)
            {
                // Check if current target is still valid
                if (!IsTargetStillValid(navMeshTarget))
                {
                    FindNewTarget();
                    return;
                }

                // Periodically check if current target is still reachable
                CheckTargetReachability();
            }

            // Update poise recovery
            UpdatePoiseRecovery();

            if (!isAttacking)
            {
                UpdateMovement();
            }
        }
        
        /// <summary>
        /// Applies procedural knockback based on recent hits using the IK reaction system.
        /// This creates smooth, physics-like knockback without coroutines.
        /// Can be disabled by child classes that handle knockback differently (e.g. flying drones).
        /// </summary>
        private void ApplyProceduralKnockback()
        {
            // Allow child classes to disable automatic knockback if they handle it differently
            if (!ShouldApplyProceduralKnockback()) return;
            
            // Check if we should apply knockback
            float timeSinceHit = Time.time - LastHitTime;
            if (timeSinceHit > 0.3f || LastHitOrigin == Vector3.zero) return; // Slightly longer for visible effect
            
            // Scale knockback distance based on poise damage (heavier weapons = more knockback)
            float baseKnockback = 0.75f; // Reduced from 1.5f
            float poiseScale = Mathf.Clamp(LastHitPoiseDamage / 15f, 0.4f, 2.0f); // Min 0.4x, max 2.0x (ensures minimum knockback)
            float scaledKnockback = baseKnockback * poiseScale;
            
            Vector3 knockbackOffset = IKReactionUtils.CalculateKnockbackOffset(transform, LastHitOrigin, LastHitTime, scaledKnockback, 0.3f, MaxPoise, Poise);
            
            if (knockbackOffset.magnitude > 0.001f)
            {
                // Apply knockback velocity-based (smoother and more responsive)
                Vector3 knockbackMovement = knockbackOffset * Time.deltaTime * 25f; // Increased multiplier for visible knockback
                Vector3 newPosition = transform.position + knockbackMovement;
                
                if (showCollisionDebug && timeSinceHit < 0.05f)
                {
                    Debug.Log($"[{gameObject.name}] Knockback - Poise: {LastHitPoiseDamage}, Scale: {poiseScale:F2}, " +
                             $"Offset magnitude: {knockbackOffset.magnitude:F3}, Movement: {knockbackMovement.magnitude:F3}");
                }
                
                // Validate position is on NavMesh
                if (NavMesh.SamplePosition(newPosition, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    if (useRootMotion)
                    {
                        transform.position = hit.position;
                    }
                    else if (agent != null && agent.isOnNavMesh)
                    {
                        agent.Warp(hit.position);
                    }
                }
            }
        }

        // This method is called by the Animator when root motion is being applied
        protected virtual void OnAnimatorMove()
        {
            if (!useRootMotion || Health <= 0 || !agent.isOnNavMesh) 
            {
                return;
            }

            // Pure root motion approach: Animation drives movement completely
            Vector3 rootMotion = animator.deltaPosition * rootMotionMultiplier;
            rootMotion.y = 0; // Ignore vertical movement from animation

            // If there's no movement from root motion, don't do anything
            if (rootMotion.magnitude < 0.001f)
            {
                return;
            }

            // Use the centralized root motion utility
            LayerMask collisionLayers = LayerMask.GetMask("Default", "ObstacleLayer");
            
            // Determine minimum distance based on attack state and recent attack history
            // Keep zombie within attack range (0-1.5f) by preventing getting too close
            // Use larger buffer for a short time after attacking to prevent root motion from pushing too close
            float timeSinceLastAttack = Time.time - lastAttackTime;
            bool recentlyAttacked = timeSinceLastAttack < 2.0f; // 2 seconds after attack
            float minDistance = (isAttacking || recentlyAttacked) ? 1.2f : 0.2f;
            
            bool movementApplied = RootMotionUtils.ApplyRootMotion(
                transform, 
                rootMotion, 
                agent, 
                collisionLayers, 
                navMeshTarget, 
                minDistance, 
                showCollisionDebug
            );
            
            if (!movementApplied && showCollisionDebug)
            {
                Debug.Log($"[{gameObject.name}] Root motion blocked - staying in place");
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
            // Try to get SkinnedMeshRenderer first (for animated characters)
            skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null)
            {
                originalMaterial = skinnedMeshRenderer.material;
                flashMaterial = new Material(originalMaterial);
                flashMaterial.color = flashColor;
            }
            else
            {
                // If no SkinnedMeshRenderer, try regular MeshRenderer (for drones, etc.)
                meshRenderer = GetComponentInChildren<MeshRenderer>();
                if (meshRenderer != null)
                {
                    originalMaterial = meshRenderer.material;
                    flashMaterial = new Material(originalMaterial);
                    flashMaterial.color = flashColor;
                }
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

        /// <summary>
        /// Calculate the optimal stopping distance based on available attacks.
        /// Override this method in derived classes to provide attack-specific logic.
        /// </summary>
        /// <returns>Optimal stopping distance from target</returns>
        protected virtual float CalculateOptimalStoppingDistance()
        {
            // Default behavior: use the configured stopping distance
            return stoppingDistance;
        }

        /// <summary>
        /// Get the minimum distance at which the enemy can attack.
        /// Override this method in derived classes to provide attack-specific logic.
        /// Returns 0 if there's no minimum (can attack at any close distance).
        /// </summary>
        /// <returns>Minimum attack distance (0 if none)</returns>
        protected virtual float GetMinimumAttackDistance()
        {
            // Default: no minimum distance, can attack when close
            return 0f;
        }

        protected virtual void UpdateMovement()
        {
            // Handle cooldown movement for ranged enemies (this takes priority over regular movement)
            UpdateCooldownMovement();
            
            // If we're moving during cooldown, don't let regular movement override it
            if (isMovingDuringCooldown)
            {
                return; // Skip ALL regular movement logic
            }
            
            // If no target, try to find one first
            if (navMeshTarget == null)
            {
                FindNewTarget();
                if (navMeshTarget == null)
                {
                    // Still no target, can't move
                    return;
                }
            }
            
            float distanceToTarget = Vector3.Distance(transform.position, navMeshTarget.position);
            float optimalStoppingDistance = CalculateOptimalStoppingDistance();
            float minAttackDistance = GetMinimumAttackDistance();
            
            // Only use back-away logic for enemies with a minimum attack distance (ranged enemies)
            if (minAttackDistance > 0)
            {
                // Check if we should initiate or continue backing away
                bool currentlyTooClose = distanceToTarget < minAttackDistance;
                float timeSinceBackAwayStart = Time.time - backAwayStartTime;
                
                // Debug logging for back-away system
                if (showCollisionDebug && Time.frameCount % 60 == 0) // Log once per second at 60fps
                {
                    Debug.Log($"[{gameObject.name}] BackAway State | isBackingAway: {isBackingAway} | Distance: {distanceToTarget:F2} | " +
                             $"MinAttackDist: {minAttackDistance:F2} | OptimalStop: {optimalStoppingDistance:F2} | " +
                             $"TooClose: {currentlyTooClose} | TimeSinceStart: {timeSinceBackAwayStart:F2}");
                }
                
                // State machine for backing away
                if (isBackingAway)
                {
                    // Continue backing away until duration expires AND we're beyond minimum attack distance
                    if (timeSinceBackAwayStart < BACK_AWAY_DURATION || distanceToTarget < minAttackDistance)
                    {
                        // Keep backing away to optimal distance (which should be >= minAttackDistance)
                        Vector3 directionAway = (transform.position - navMeshTarget.position).normalized;
                        Vector3 backAwayPoint = navMeshTarget.position + directionAway * (optimalStoppingDistance + 1f);
                        
                        UnityEngine.AI.NavMeshHit hit;
                        if (UnityEngine.AI.NavMesh.SamplePosition(backAwayPoint, out hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                        {
                            agent.SetDestination(hit.position);
                            agent.isStopped = false;
                        }
                    }
                    else
                    {
                        // Done backing away - now beyond minimum attack distance
                        Debug.Log($"[{gameObject.name}] Finished backing away - Distance: {distanceToTarget:F2} >= MinDist: {minAttackDistance:F2}");
                        isBackingAway = false;
                    }
                }
                else if (currentlyTooClose && timeSinceBackAwayStart > BACK_AWAY_COOLDOWN)
                {
                    // Initiate new back-away
                    Debug.Log($"[{gameObject.name}] Starting back-away - Distance: {distanceToTarget:F2} < MinDist: {minAttackDistance:F2}");
                    isBackingAway = true;
                    backAwayStartTime = Time.time;
                    
                    // Calculate back-away destination
                    Vector3 directionAway = (transform.position - navMeshTarget.position).normalized;
                    Vector3 backAwayPoint = navMeshTarget.position + directionAway * (optimalStoppingDistance + 1f);
                    
                    UnityEngine.AI.NavMeshHit hit;
                    if (UnityEngine.AI.NavMesh.SamplePosition(backAwayPoint, out hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                    {
                        agent.SetDestination(hit.position);
                        agent.isStopped = false;
                    }
                }
                else
                {
                    // Normal movement - check for strategic position from EnemyManager (based on role)
                    Vector3 strategicPosition = Vector3.zero;
                    if (Managers.EnemyManager.Instance != null)
                    {
                        strategicPosition = Managers.EnemyManager.Instance.GetStrategicPosition(this);
                    }
                    
                    // Prefer strategic position if available
                    if (strategicPosition != Vector3.zero)
                    {
                        float distanceToStrategicPos = Vector3.Distance(transform.position, strategicPosition);
                        
                        // Use strategic position for movement
                        agent.SetDestination(strategicPosition);
                        
                        // Check if we're at strategic position
                        bool atStrategicPosition = distanceToStrategicPos < 1.5f;
                        
                        // For root motion, handle stopping
                        if (useRootMotion)
                        {
                            // Only stop when actually attacking
                            bool shouldStop = (isAttacking || isRotatingToAttack);
                            
                            if (shouldStop)
                            {
                                if (!agent.isStopped)
                                {
                                    agent.isStopped = true;
                                    agent.velocity = Vector3.zero;
                                }
                            }
                            else if (agent.isStopped && !atStrategicPosition)
                            {
                                agent.isStopped = false;
                            }
                        }
                    }
                    else
                    {
                        // No strategic position, move toward target
                        // For buildings with NavMeshObstacle, use distributed positions to avoid clustering
                        Vector3 targetDestination = GetDistributedDestination(navMeshTarget, optimalStoppingDistance);
                        agent.SetDestination(targetDestination);
                        
                        // For root motion zombies, check if we should stop the agent
                        if (useRootMotion)
                        {
                            // Stop agent when at optimal distance or during attack phases
                            bool shouldStop = (distanceToTarget <= optimalStoppingDistance) || isAttacking || isRotatingToAttack;
                            
                            if (shouldStop)
                            {
                                if (!agent.isStopped)
                                {
                                    if (showCollisionDebug)
                                    {
                                        Debug.Log($"[{gameObject.name}] Stopping agent - Distance: {distanceToTarget:F2} <= Optimal: {optimalStoppingDistance:F2} | " +
                                                 $"isAttacking: {isAttacking} | isRotating: {isRotatingToAttack}");
                                    }
                                    agent.isStopped = true;
                                    agent.velocity = Vector3.zero;
                                }
                            }
                            else
                            {
                                // Resume movement when out of optimal distance
                                if (agent.isStopped)
                                {
                                    if (showCollisionDebug)
                                    {
                                        Debug.Log($"[{gameObject.name}] Resuming agent - Distance: {distanceToTarget:F2} > Optimal: {optimalStoppingDistance:F2}");
                                    }
                                    agent.isStopped = false;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                // Melee enemy (no minimum attack distance) - use coordinated movement with pack roles
                
                // ─────────────────────────────────────────────────────────────────────────────────
                // COOLDOWN BEHAVIOR: Stop moving when in attack range but waiting for cooldown
                // Prevents enemies from running around/circling player while waiting to attack
                // ─────────────────────────────────────────────────────────────────────────────────
                bool inRangeButOnCooldown = false;
                bool inRangeButNoLOS = false;
                
                // Check all attacks to determine movement behavior
                // Uses obstacle-aware range checking for consistency with AttackBase.CanAttack()
                var attackComponents = GetComponents<AttackBase>();
                foreach (var attack in attackComponents)
                {
                    if (attack != null && attack.enabled)
                    {
                        // Check if in range using obstacle-aware logic (handles buildings with NavMeshObstacle)
                        bool inRange = DamageUtils.IsInRangeWithObstacles(transform.position, navMeshTarget, attack.minRange, attack.maxRange);
                        
                        if (inRange)
                        {
                            bool cooldownReady = DamageUtils.IsCooldownReady(attack.lastAttackTime, attack.cooldown);
                            
                            if (!cooldownReady)
                            {
                                inRangeButOnCooldown = true;
                                if (showCollisionDebug)
                                {
                                    Debug.Log($"[{gameObject.name}] Attack {attack.GetType().Name} on cooldown");
                                }
                            }
                            else if (attack.minRange > 0) // Ranged attack
                            {
                                // In range and cooldown ready, so check if LOS is blocking
                                if (!HasLineOfSight(navMeshTarget.position))
                                {
                                    inRangeButNoLOS = true;
                                    if (showCollisionDebug)
                                    {
                                        Debug.Log($"[{gameObject.name}] Attack {attack.GetType().Name} blocked by LOS - need to reposition");
                                    }
                                }
                            }
                        }
                    }
                }
                
                // If in range but on actual cooldown, stay still (don't circle or move around)
                if (inRangeButOnCooldown && !isAttacking)
                {
                    if (!agent.isStopped)
                    {
                        if (showCollisionDebug)
                        {
                            Debug.Log($"[{gameObject.name}] Stopping - in attack range but on cooldown");
                        }
                        agent.isStopped = true;
                        agent.velocity = Vector3.zero;
                    }
                }
                // If in range but no LOS, try to reposition to get a clear shot
                else if (inRangeButNoLOS && !isAttacking)
                {
                    // Try to find a position with clear LOS
                    Vector3 repositionTarget = FindPositionWithLineOfSight();
                    
                    if (repositionTarget != Vector3.zero)
                    {
                        agent.SetDestination(repositionTarget);
                        if (agent.isStopped)
                        {
                            if (showCollisionDebug)
                            {
                                Debug.Log($"[{gameObject.name}] Repositioning to find clear line of sight");
                            }
                            agent.isStopped = false;
                        }
                    }
                    else
                    {
                        // Can't find a good position, just move towards target to get closer
                        agent.SetDestination(navMeshTarget.position);
                        if (agent.isStopped)
                        {
                            agent.isStopped = false;
                        }
                    }
                }
                else
                {
                    // Check for strategic position from EnemyManager (based on role: chaser/interceptor/wanderer)
                    Vector3 strategicPosition = Vector3.zero;
                    if (Managers.EnemyManager.Instance != null)
                    {
                        strategicPosition = Managers.EnemyManager.Instance.GetStrategicPosition(this);
                    }
                    
                    // Prefer strategic position if available
                    if (strategicPosition != Vector3.zero)
                    {
                        float distanceToStrategicPos = Vector3.Distance(transform.position, strategicPosition);
                        
                        // Use strategic position for movement
                        agent.SetDestination(strategicPosition);
                        
                        // Check if we're at strategic position
                        bool atStrategicPosition = distanceToStrategicPos < 1.5f;
                        
                        // For root motion zombies, handle stopping
                        if (useRootMotion)
                        {
                            // Only stop when actually attacking or rotating to attack
                            // Allow movement to strategic position even when at attack range
                            bool shouldStop = (isAttacking || isRotatingToAttack);
                            
                            if (shouldStop)
                            {
                                if (!agent.isStopped)
                                {
                                    if (showCollisionDebug)
                                    {
                                        Debug.Log($"[{gameObject.name}] Stopping for attack - isAttacking: {isAttacking} | isRotating: {isRotatingToAttack}");
                                    }
                                    agent.isStopped = true;
                                    agent.velocity = Vector3.zero;
                                }
                            }
                            else
                            {
                                // Keep moving unless at exact strategic position
                                if (agent.isStopped && !atStrategicPosition)
                                {
                                    if (showCollisionDebug)
                                    {
                                        Debug.Log($"[{gameObject.name}] Resuming movement to strategic position");
                                    }
                                    agent.isStopped = false;
                                }
                            }
                        }
                    }
                    else
                    {
                        // No strategic position assigned, move toward target
                        // For buildings with NavMeshObstacle, use distributed positions to avoid clustering
                        Vector3 targetDestination = GetDistributedDestination(navMeshTarget, optimalStoppingDistance);
                        agent.SetDestination(targetDestination);
                        
                        // For root motion zombies, check if we should stop the agent
                        if (useRootMotion)
                        {
                            // Stop agent when at optimal distance or during attack phases
                            bool shouldStop = (distanceToTarget <= optimalStoppingDistance) || isAttacking || isRotatingToAttack;
                            
                            if (shouldStop)
                            {
                                if (!agent.isStopped)
                                {
                                    if (showCollisionDebug)
                                    {
                                        Debug.Log($"[{gameObject.name}] Stopping agent - Distance: {distanceToTarget:F2} <= Optimal: {optimalStoppingDistance:F2} | " +
                                                 $"isAttacking: {isAttacking} | isRotating: {isRotatingToAttack}");
                                    }
                                    agent.isStopped = true;
                                    agent.velocity = Vector3.zero;
                                }
                            }
                            else
                            {
                                // Resume movement when out of optimal distance
                                if (agent.isStopped)
                                {
                                    if (showCollisionDebug)
                                    {
                                        Debug.Log($"[{gameObject.name}] Resuming agent - Distance: {distanceToTarget:F2} > Optimal: {optimalStoppingDistance:F2}");
                                    }
                                    agent.isStopped = false;
                                }
                            }
                        }
                    }
                }
            }
            
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

        /// <summary>
        /// Handle movement during attack cooldowns for ranged enemies to make them feel more natural
        /// </summary>
        private void UpdateCooldownMovement()
        {
            // Check if cooldown movement is enabled
            if (!enableCooldownMovement)
            {
                return;
            }

            // Only apply to ranged enemies (those with minimum attack distance)
            float minAttackDistance = GetMinimumAttackDistance();
            if (minAttackDistance <= 0)
            {
                return; // Not a ranged enemy
            }

            // Only move during cooldowns when not attacking
            if (isAttacking || navMeshTarget == null)
            {
                return;
            }

            // Check if we're in a cooldown state (can't attack due to cooldown, not distance/angle)
            bool inCooldown = IsInAttackCooldown();
            if (!inCooldown)
            {
                // Reset cooldown movement state when not in cooldown
                if (isMovingDuringCooldown)
                {
                    isMovingDuringCooldown = false;
                    lastCooldownMovementEndTime = Time.time;
                }
                return;
            }

            float timeSinceCooldownMovementStart = Time.time - cooldownMovementStartTime;

            // State machine for cooldown movement
            if (isMovingDuringCooldown)
            {
                // Continue moving until duration expires
                if (timeSinceCooldownMovementStart < cooldownMovementMaxDuration)
                {
                    // Check if we're too close to the player - but with very strict conditions
                    // Only interrupt if we're in immediate danger (very close) and have been at this target long enough
                    float distanceToPlayer = Vector3.Distance(transform.position, navMeshTarget.position);
                    float minDistance = GetMinimumAttackDistance();
                    float timeSinceLastTargetChange = Time.time - lastTargetChangeTime;
                    
                    // Only find new target if CRITICALLY too close (within 50% of minDistance) AND enough time has passed
                    // This prevents constant repositioning and allows movement to complete
                    if (distanceToPlayer < (minDistance * 0.5f) && timeSinceLastTargetChange > TARGET_CHANGE_COOLDOWN)
                    {
                        FindNewCooldownMovementTarget();
                        lastTargetChangeTime = Time.time;
                        isWaitingAtTarget = false; // Reset waiting state
                    }
                    
                    // Check if we've reached the target
                    float distanceToCooldownTarget = Vector3.Distance(transform.position, cooldownMovementTarget);
                    
                    // Check if we've reached the target and should start waiting
                    // Only consider "reached" if we've been moving for at least 1 second AND we're close to the target
                    if (!isWaitingAtTarget && 
                        distanceToCooldownTarget < agent.stoppingDistance + 0.3f && 
                        timeSinceLastTargetChange > 1.0f)
                    {
                        // We've reached the target, start waiting
                        isWaitingAtTarget = true;
                        targetReachedTime = Time.time;
                        agent.isStopped = true; // Stop the agent while waiting
                    }
                    
                    // If we're waiting at the target
                    if (isWaitingAtTarget)
                    {
                        float timeSpentWaiting = Time.time - targetReachedTime;
                        
                        // Check if we've waited long enough
                        if (timeSpentWaiting >= WAIT_AT_TARGET_DURATION && timeSinceLastTargetChange > TARGET_CHANGE_COOLDOWN)
                        {
                            // Wait period complete, find a new target
                            FindNewCooldownMovementTarget();
                            lastTargetChangeTime = Time.time;
                            isWaitingAtTarget = false;
                            agent.isStopped = false;
                        }
                    }
                    else
                    {
                        // Keep moving towards cooldown movement target
                        agent.SetDestination(cooldownMovementTarget);
                        agent.isStopped = false;
                        
                        // Update animation parameters to ensure Speed parameter is set for root motion
                        UpdateAnimationParameters();
                    }
                }
                else
                {
                    // Cooldown movement duration expired
                    isMovingDuringCooldown = false;
                    isWaitingAtTarget = false;
                    lastCooldownMovementEndTime = Time.time;
                }
            }
            else
            {
                // Start new cooldown movement if enough time has passed since last movement ended
                float timeSinceLastCooldownMovement = Time.time - lastCooldownMovementEndTime;
                
                if (timeSinceLastCooldownMovement > COOLDOWN_MOVEMENT_COOLDOWN)
                {
                    StartCooldownMovement();
                }
            }
        }

        /// <summary>
        /// Check if the enemy is in an attack cooldown by checking actual attack components
        /// This specifically checks for cooldown timers, NOT line of sight or range issues
        /// </summary>
        /// <returns>True if in cooldown</returns>
        private bool IsInAttackCooldown()
        {
            if (navMeshTarget == null) return false;

            // Check if any attack component is on actual cooldown (not just unable to attack)
            bool anyAttackOnCooldown = false;
            var attackComponents = GetComponents<AttackBase>();
            
            foreach (var attack in attackComponents)
            {
                if (attack != null && attack.enabled)
                {
                    // Check if in range using obstacle-aware logic (consistent with CanAttack)
                    bool inRange = DamageUtils.IsInRangeWithObstacles(transform.position, navMeshTarget, attack.minRange, attack.maxRange);
                    bool cooldownReady = DamageUtils.IsCooldownReady(attack.lastAttackTime, attack.cooldown);
                    
                    // If in range but cooldown is NOT ready, then we're actually in cooldown
                    if (inRange && !cooldownReady)
                    {
                        anyAttackOnCooldown = true;
                        break;
                    }
                }
            }
            
            return anyAttackOnCooldown;
        }

        /// <summary>
        /// Start a new cooldown movement sequence
        /// </summary>
        private void StartCooldownMovement()
        {
            if (navMeshTarget == null) return;

            FindNewCooldownMovementTarget();
            isMovingDuringCooldown = true;
            cooldownMovementStartTime = Time.time;
            lastTargetChangeTime = Time.time; // Initialize the target change timer
        }

        /// <summary>
        /// Find a position with clear line of sight to the target
        /// Tries multiple positions around the current location to find one with LOS
        /// </summary>
        /// <returns>A valid position with LOS, or Vector3.zero if none found</returns>
        private Vector3 FindPositionWithLineOfSight()
        {
            if (navMeshTarget == null) return Vector3.zero;
            
            Vector3 currentPos = transform.position;
            Vector3 targetPos = navMeshTarget.position;
            float maxAttackRange = GetMaximumAttackRange();
            float minAttackDistance = GetMinimumAttackDistance();
            
            // Try multiple positions around the current location
            // Start with positions at a comfortable attack distance
            float[] testDistances = { minAttackDistance + 2f, minAttackDistance + 4f, maxAttackRange * 0.7f };
            float[] testAngles = { -90f, -45f, 0f, 45f, 90f, -135f, 135f, 180f }; // Try various angles
            
            foreach (float distance in testDistances)
            {
                foreach (float angle in testAngles)
                {
                    // Calculate test position relative to target
                    Vector3 directionFromTarget = (currentPos - targetPos).normalized;
                    Vector3 rotatedDirection = Quaternion.AngleAxis(angle, Vector3.up) * directionFromTarget;
                    Vector3 testPosition = targetPos + rotatedDirection * distance;
                    
                    // Check if position is valid on NavMesh
                    UnityEngine.AI.NavMeshHit navHit;
                    if (UnityEngine.AI.NavMesh.SamplePosition(testPosition, out navHit, 3f, UnityEngine.AI.NavMesh.AllAreas))
                    {
                        // Check if this position has LOS to target
                        Vector3 rayOrigin = navHit.position + Vector3.up * 1.5f;
                        Vector3 rayTarget = targetPos + Vector3.up * 1.5f;
                        Vector3 direction = rayTarget - rayOrigin;
                        float rayDistance = direction.magnitude;
                        
                        // Use same layer mask as HasLineOfSight - ignore Player and Enemy layers
                        LayerMask ignoreMask = LayerMask.GetMask("Player", "Enemy", "Ignore Raycast");
                        LayerMask obstacleMask = ~ignoreMask;
                        RaycastHit hit;
                        
                        if (!Physics.Raycast(rayOrigin, direction.normalized, out hit, rayDistance, obstacleMask))
                        {
                            // Found a position with clear LOS!
                            if (showCollisionDebug)
                            {
                                Debug.Log($"[{gameObject.name}] Found clear position at angle {angle}° and distance {distance:F2}");
                            }
                            return navHit.position;
                        }
                    }
                }
            }
            
            // No clear position found
            return Vector3.zero;
        }
        
        /// <summary>
        /// Find a new target position for cooldown movement - strafe around the player at optimal range
        /// </summary>
        private void FindNewCooldownMovementTarget()
        {
            if (navMeshTarget == null) return;

            Vector3 currentPos = transform.position;
            Vector3 targetPos = navMeshTarget.position;
            float minAttackDistance = GetMinimumAttackDistance();
            
            // Calculate the desired distance (add significant buffer beyond min distance)
            float desiredDistance = minAttackDistance + UnityEngine.Random.Range(2f, 5f);
            
            // Get direction to target
            Vector3 directionToTarget = (targetPos - currentPos).normalized;
            
            // Choose a random strafe angle (left or right, 60-120 degrees)
            float strafeAngle = UnityEngine.Random.Range(-120f, 120f);
            if (Mathf.Abs(strafeAngle) < 60f)
            {
                strafeAngle += strafeAngle < 0 ? -60f : 60f; // Ensure minimum 60 degree angle
            }
            
            // Calculate strafe direction
            Vector3 strafeDirection = Quaternion.AngleAxis(strafeAngle, Vector3.up) * directionToTarget;
            
            // Calculate target position at desired distance from player
            Vector3 strafePosition = targetPos + strafeDirection * desiredDistance;
            
            // Sample NavMesh to find valid position
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(strafePosition, out hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
            {
                cooldownMovementTarget = hit.position;
            }
            else
            {
                // Fallback: try a simpler position - just move to the side
                Vector3 rightVector = Vector3.Cross(directionToTarget, Vector3.up);
                float sideDirection = UnityEngine.Random.value > 0.5f ? 1f : -1f;
                Vector3 sidePosition = currentPos + rightVector * sideDirection * 3f;
                
                if (UnityEngine.AI.NavMesh.SamplePosition(sidePosition, out hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    cooldownMovementTarget = hit.position;
                }
                else
                {
                    // Ultimate fallback: move slightly forward
                    cooldownMovementTarget = currentPos + directionToTarget * 2f;
                }
            }
        }

        /// <summary>
        /// Get the maximum attack range from all available attacks
        /// </summary>
        /// <returns>Maximum attack range</returns>
        public float GetMaximumAttackRange()
        {
            float maxRange = 0f;

            // Check Zombie attack components
            var zombieComponent = GetComponent<ModularEnemy>();
            if (zombieComponent != null)
            {
                var attackComponents = GetComponents<AttackBase>();
                foreach (var attack in attackComponents)
                {
                    if (attack != null && attack.enabled)
                    {
                        maxRange = Mathf.Max(maxRange, attack.maxRange);
                    }
                }
            }

            // Fallback to a reasonable default
            return maxRange > 0 ? maxRange : 10f;
        }

        /// <summary>
        /// Get a distributed destination around a target to prevent clustering.
        /// Uses crowd avoidance for buildings with NavMeshObstacles, direct position otherwise.
        /// 
        /// HOW IT WORKS:
        /// 1. For buildings: Creates a ring of positions around the building at attack range
        /// 2. Tests each position for nearby agents (crowding)
        /// 3. Scores positions: closer to enemy + fewer nearby agents = better score
        /// 4. Returns the best position, naturally spreading enemies around the building
        /// 
        /// TUNING:
        /// - positionCount (12): More positions = smoother distribution, but slower
        /// - crowdingRadius (2.5f): Larger = more spread out, smaller = tighter grouping
        /// </summary>
        /// <param name="target">The target to move toward</param>
        /// <param name="attackRange">The desired attack range/stopping distance</param>
        /// <returns>A position to path to around the target</returns>
        private Vector3 GetDistributedDestination(Transform target, float attackRange)
        {
            if (target == null) return transform.position;
            
            // Check if target has a NavMeshObstacle (typically buildings/structures)
            NavMeshObstacle obstacle = target.GetComponent<NavMeshObstacle>();
            
            if (obstacle != null)
            {
                // Target is a building/structure - use distributed positioning with crowd avoidance
                return NavigationUtils.FindDistributedPositionAroundTarget(
                    transform.position, 
                    target, 
                    attackRange, 
                    obstacleBoundsOffset,
                    12,  // Test 12 positions around the building (every 30 degrees)
                    2.5f // Crowding radius - consider positions crowded if 2.5 units from other agents
                );
            }
            else
            {
                // Target is a character (NPC/player) - path directly to them
                // NavMesh agent avoidance will handle avoiding other agents
                return target.position;
            }
        }

        /// <summary>
        /// Check if animator is valid and has a controller assigned
        /// Prevents warnings when enemies don't have animation controllers (like drones)
        /// </summary>
        protected bool HasValidAnimator()
        {
            return animator != null && animator.runtimeAnimatorController != null;
        }

        private void UpdateAnimationParameters()
        {
            if (!HasValidAnimator()) return;
            
            // Calculate normalized speed based on agent velocity
            float velocity = agent.velocity.magnitude;
            float normalizedSpeed = Mathf.Clamp01(velocity / movementSpeed);
            
            // Set Speed parameter (0-1 range) based on normalized velocity
            animator.SetFloat(GameConstants.AnimatorParams.SpeedHash, normalizedSpeed);
        }

        private void UpdateRotation()
        {
            if (navMeshTarget == null) return;
            
            // Use centralized rotation utility
            NavigationUtils.HandleMovementRotation(transform, navMeshTarget, agent.velocity, rotationSpeed, MOVEMENT_VELOCITY_THRESHOLD);
        }

        /// <summary>
        /// Get the current attack component for IK forwarding
        /// Override in child classes to provide the current attack
        /// </summary>
        /// <returns>The current attack component, or null if none</returns>
        protected virtual AttackBase GetCurrentAttackForIK()
        {
            return null;
        }

        /// <summary>
        /// Called by Unity for IK (Inverse Kinematics) updates
        /// Implements general head tracking for all enemies when not attacking
        /// </summary>
        /// <param name="layerIndex">The IK layer index</param>
        protected virtual void OnAnimatorIK(int layerIndex)
        {
            if (animator == null) return;
            
            // Check if currently reacting to a hit
            bool isReactingToHit = animator.isHuman && LastHitOrigin != Vector3.zero && 
                                   (Time.time - LastHitTime) < 0.6f; // Within reaction duration (matches IKReactionUtils)
            
            // Priority 1: Process hit reactions (immediate, stateless IK reactions)
            if (isReactingToHit)
            {
                // Scale reaction intensity based on poise damage (typical weapon poise: 5-25)
                float scaledIntensity = Mathf.Clamp(LastHitPoiseDamage / 20f, 0.5f, 1.5f); // Min 0.5, max 1.5 (ensures visible reaction)
                
                if (showCollisionDebug && (Time.time - LastHitTime) < 0.05f)
                {
                    Debug.Log($"[{gameObject.name}] IK Reaction - Poise: {LastHitPoiseDamage}, Scaled Intensity: {scaledIntensity:F2}");
                }
                
                IKReactionUtils.ApplyHitReactionIK(animator, transform, LastHitOrigin, LastHitTime, 0.6f, scaledIntensity);
                // Hit reactions take full priority - return to avoid conflicts
                return;
            }
            
            // Priority 2: If attacking, forward IK to the current attack component
            if (isAttacking)
            {
                AttackBase attack = GetCurrentAttackForIK();
                if (attack != null)
                {
                    attack.OnAnimatorIK(layerIndex);
                    return;
                }
            }
            
            // Priority 3: Perform general head tracking if enabled
            if (!enableHeadTracking) return;
            
            // Check if we should do head tracking
            bool shouldTrackHead = ShouldPerformHeadTracking();
            
            if (shouldTrackHead && navMeshTarget != null && Health > 0)
            {
                // Calculate target position with Y offset for head height
                Vector3 targetPosition = navMeshTarget.position + Vector3.up * 1.5f; // Assume target head height
                
                // Calculate direction from current position to target
                Vector3 directionToTarget = (targetPosition - transform.position).normalized;
                
                // Check if target is within head rotation limits
                float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
                float distanceToTarget = Vector3.Distance(transform.position, navMeshTarget.position);
                
                if (angleToTarget <= maxHeadTrackingAngle && distanceToTarget <= headTrackingDistance)
                {
                    // Enable head tracking
                    isHeadTrackingActive = true;
                    
                    // Smoothly lerp the look-at target position to prevent snappy head movements
                    currentLookAtTarget = Vector3.Lerp(currentLookAtTarget, targetPosition, headTrackingLerpSpeed * Time.deltaTime);
                    
                    currentHeadTrackingWeight = Mathf.Lerp(currentHeadTrackingWeight, headTrackingWeight, headTrackingLerpSpeed * Time.deltaTime);
                }
                else
                {
                    // Target is outside head tracking limits
                    isHeadTrackingActive = false;
                    currentHeadTrackingWeight = Mathf.Lerp(currentHeadTrackingWeight, 0f, headTrackingLerpSpeed * Time.deltaTime);
                }
            }
            else
            {
                // No target or shouldn't track, disable head tracking
                isHeadTrackingActive = false;
                currentHeadTrackingWeight = Mathf.Lerp(currentHeadTrackingWeight, 0f, headTrackingLerpSpeed * Time.deltaTime);
            }
            
            // Apply head IK weights (only if animator has a controller)
            if (HasValidAnimator())
            {
            if (currentHeadTrackingWeight > 0.01f)
            {
                animator.SetLookAtWeight(currentHeadTrackingWeight, headTrackingRotationWeight, 0f, 0f, 0f);
                animator.SetLookAtPosition(currentLookAtTarget);
            }
            else
            {
                animator.SetLookAtWeight(0f);
                }
            }
        }

        /// <summary>
        /// Determine if the enemy should perform head tracking
        /// Override in child classes to customize when head tracking should occur
        /// </summary>
        /// <returns>True if head tracking should be performed</returns>
        protected virtual bool ShouldPerformHeadTracking()
        {
            // Don't track head when attacking (let attack components handle their own IK)
            if (isAttacking) return false;
            
            // Don't track head when dead
            if (Health <= 0) return false;
            
            // Don't track head when stunned/poise broken
            if (IsPoiseBroken()) return false;
            
            // Track head when we have a target and are moving or idle
            return navMeshTarget != null;
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
                    else
                    {
                        Debug.LogWarning($"[{gameObject.name}] PlayerController.Instance or _possessedNPC is null in ROGUE_LITE mode");
                    }
                    break;
                    
                case GameMode.CAMP:
                case GameMode.CAMP_ATTACK:
                    newTarget = FindCampTarget();
                    Debug.Log($"[{gameObject.name}] Found camp target: {(newTarget != null ? newTarget.name : "null")}");
                    break;
                    
                default:
                    Debug.LogWarning($"No targeting logic for game mode: {GameManager.Instance.CurrentGameMode}");
                    break;
            }

            if (newTarget != null)
            {
                navMeshTarget = newTarget;
                
                // Resume movement if agent was stopped
                if (agent != null && agent.isOnNavMesh && agent.isStopped)
                {
                    agent.isStopped = false;
                    Debug.Log($"[{gameObject.name}] Target acquired: {newTarget.name} - resuming movement");
                }
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
            
            return NavigationUtils.IsFacingTarget(transform, navMeshTarget, ATTACK_READY_ANGLE_THRESHOLD, true);
        }

        /// <summary>
        /// Rotates towards target with enhanced speed for attack preparation.
        /// Virtual so derived classes (like HumanoidEnemy) can customize rotation behavior.
        /// </summary>
        /// <returns>True if rotation is complete and ready to attack</returns>
        protected virtual bool RotateTowardsTargetForAttack()
        {
            if (navMeshTarget == null) return false;
            
            // Don't rotate if dead
            if (Health <= 0) 
            {
                return false;
            }

            return NavigationUtils.RotateTowardsTargetForAction(transform, navMeshTarget, rotationSpeed, ROTATION_TOWARDS_TARGET_SPEED_MULTIPLIER, ATTACK_READY_ANGLE_THRESHOLD, true);
        }

        /// <summary>
        /// Checks if the enemy is currently in a poise-broken state
        /// </summary>
        /// <returns>True if poise is broken and enemy should be stunned</returns>
        protected bool IsPoiseBroken()
        {
            return isPoiseBroken || Poise <= 0;
        }

        /// <summary>
        /// Gets the poise damage this enemy deals with its attacks
        /// </summary>
        /// <returns>Poise damage amount for this enemy's attacks</returns>
        public float GetAttackPoiseDamage()
        {
            // Base poise damage based on character type
            float basePoiseDamage = 0f;
            
            switch (characterType)
            {
                case CharacterType.ZOMBIE_MELEE:
                    basePoiseDamage = 15f;
                    break;
                case CharacterType.ZOMBIE_SPITTER:
                    basePoiseDamage = 8f; // Lower poise damage for ranged attacks
                    break;
                case CharacterType.ZOMBIE_TANK:
                    basePoiseDamage = 25f; // Higher poise damage for tank
                    break;
                case CharacterType.MACHINE_DRONE:
                    basePoiseDamage = 12f;
                    break;
                case CharacterType.MACHINE_ROBOT:
                    basePoiseDamage = 20f;
                    break;
                case CharacterType.BOSS_1:
                case CharacterType.BOSS_2:
                case CharacterType.BOSS_3:
                    basePoiseDamage = 30f; // High poise damage for bosses
                    break;
                default:
                    basePoiseDamage = 10f; // Default poise damage
                    break;
            }
            
            return basePoiseDamage;
        }

        #endregion

        #region Attack Validation

        /// <summary>
        /// Checks if there is a clear line of sight from this enemy to the target.
        /// Uses raycasting to detect walls and obstacles that would block ranged attacks.
        /// </summary>
        /// <param name="targetPosition">The target position to check line of sight to</param>
        /// <param name="checkHeight">Height offset for the raycast origin (default: 1.5f for center mass)</param>
        /// <returns>True if there's a clear line of sight, false if blocked by obstacles</returns>
        public bool HasLineOfSight(Vector3 targetPosition, float checkHeight = 1.5f)
        {
            if (navMeshTarget == null) return false;

            // Start raycast from enemy's center mass height
            Vector3 rayOrigin = transform.position + Vector3.up * checkHeight;
            Vector3 rayTarget = targetPosition + Vector3.up * checkHeight;
            Vector3 direction = rayTarget - rayOrigin;
            float distance = direction.magnitude;

            // Layer mask: Ignore Settler and Enemy layers - we only want to detect walls/obstacles
            // Using inverse mask (~) to ignore specific layers instead of specifying which to hit
            LayerMask ignoreMask = LayerMask.GetMask("Settler", "Enemy", "Ignore Raycast");
            LayerMask obstacleMask = ~ignoreMask; // Invert to hit everything EXCEPT these layers
            
            // Perform the raycast
            RaycastHit hit;
            bool hasLineOfSight = !Physics.Raycast(rayOrigin, direction.normalized, out hit, distance, obstacleMask);
            
            // Debug visualization if enabled
            if (showCollisionDebug)
            {
                Color rayColor = hasLineOfSight ? Color.green : Color.red;
                Debug.DrawLine(rayOrigin, rayTarget, rayColor, 0.1f);
                
                if (!hasLineOfSight && hit.collider != null)
                {
                    Debug.DrawLine(rayOrigin, hit.point, Color.yellow, 0.1f);
                    Debug.Log($"[{gameObject.name}] Line of sight blocked by {hit.collider.gameObject.name} at distance {hit.distance:F2}");
                }
            }
            
            return hasLineOfSight;
        }

        /// <summary>
        /// Validates if an attack can be performed on the current target.
        /// Includes distance, angle, and line-of-sight checks.
        /// </summary>
        /// <param name="attackRange">The range for this specific attack</param>
        /// <param name="angleThreshold">Maximum angle deviation for attack</param>
        /// <param name="distanceToTarget">Current distance to target (output)</param>
        /// <param name="angleToTarget">Current angle to target (output)</param>
        /// <param name="requireLineOfSight">Whether to check for line of sight (default: true for ranged attacks)</param>
        /// <returns>True if attack is valid</returns>
        protected bool ValidateAttack(float attackRange, float angleThreshold, out float distanceToTarget, out float angleToTarget, bool requireLineOfSight = true)
        {
            distanceToTarget = 0f;
            angleToTarget = 0f;

            // Basic target validation
            if (navMeshTarget == null || !IsTargetStillValid(navMeshTarget))
            {
                return false;
            }

            // Don't attack if poise is broken (enemy is stunned)
            if (IsPoiseBroken())
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
            
            if (angleToTarget > angleThreshold)
            {
                return false;
            }

            // Line of sight validation (especially important for ranged attacks)
            if (requireLineOfSight && !HasLineOfSight(navMeshTarget.position))
            {
                if (showCollisionDebug)
                {
                    Debug.Log($"[{gameObject.name}] Attack blocked: No line of sight to target");
                }
                return false;
            }
            
            return true;
        }

        #endregion

        #region Combat & Damage


        public virtual void Attack()
        {
            Debug.LogWarning($"Attack not overridden for {gameObject.name}");
        }

        /// <summary>
        /// IAttackOwner: Called when the attack animation ends
        /// This is the standardized animation event method for all characters
        /// Handles all attack cleanup (animator state, movement, rotation)
        /// </summary>
        public virtual void AttackEnd()
        {
            // Reset animator state
            if (HasValidAnimator())
            {
                animator.SetBool(GameConstants.AnimatorParams.AttackHash, false);
            }
            
            // Reset attack state
            isAttacking = false;
            isRotatingToAttack = false;
            
            // Resume rotation after attack
            if (useRootMotion && agent != null)
            {
                agent.updateRotation = true;
            }
            // For non-root motion, rotation will resume automatically in update logic
        }

        protected virtual void BeginAttackSequence()
        {
            if (HasValidAnimator())
        {
            animator.SetBool(GameConstants.AnimatorParams.AttackHash, true);
            }
            
            isAttacking = true;
            isRotatingToAttack = false; // Stop rotation phase

            // Stop rotation completely during attacks for both root motion and non-root motion
            if (useRootMotion)
            {
                agent.updateRotation = false;
            }
            // For non-root motion, rotation will be prevented in the update logic by checking isAttacking
        }


        /// <summary>
        /// Unified method to handle all types of damage using DamageInfo struct
        /// </summary>
        /// <param name="damageInfo">Complete damage information including source, type, and flags</param>
        public void TakeDamage(DamageInfo damageInfo)
        {
            // Prevent taking damage if already dead
            if (Health <= 0) return;
            
            // Extract parameters from DamageInfo
            float amount = damageInfo.Amount;
            float poiseDamage = damageInfo.PoiseDamage;
            AttackElement damageType = damageInfo.ElementType;
            Transform damageSource = damageInfo.SourceTransform;
            bool playHitVFX = damageInfo.PlayHitVFX;
            
            bool hasPoiseDamage = poiseDamage > 0f;
            bool hasElementalDamage = damageType != AttackElement.NONE;
            bool poiseBroken = false;
            float finalDamage = amount;

            // Handle different damage types with appropriate utilities
            if (hasPoiseDamage && hasElementalDamage)
            {
                // Full damage: poise + elemental
                var result = DamageUtils.ApplyElementalDamageWithPoise(this, amount, poiseDamage, damageType, 
                    damageSource, animator, transform, OnDamageTaken, OnPoiseBroken, OnDeath, playHitVFX);
                finalDamage = result.Item2;
                poiseBroken = result.Item3;
                
                // Skip if immune to this damage type
                if (finalDamage <= 0) return;
                
                Health -= finalDamage;
            }
            else if (hasElementalDamage)
            {
                // Elemental damage only
                var result = DamageUtils.ApplyElementalDamage(this, amount, damageType, 
                    damageSource, animator, transform, OnDamageTaken, OnDeath, playHitVFX);
                finalDamage = result.Item2;
                
                // Skip if immune to this damage type
                if (finalDamage <= 0) return;
                
                Health -= finalDamage;
            }
            else if (hasPoiseDamage)
            {
                // Poise damage only
                var result = DamageUtils.ApplyDamageWithPoise(this, amount, poiseDamage, 
                    damageSource, animator, transform, OnDamageTaken, OnPoiseBroken, OnDeath, playHitVFX);
                poiseBroken = result.Item2;
                Health -= amount;
            }
            else
            {
                // Basic damage only
                Health -= amount;
                DamageUtils.ApplyDamage(this, amount, damageSource, animator, transform, 
                    OnDamageTaken, OnDeath, playHitVFX);
            }

            // Update poise damage tracking
            if (hasPoiseDamage)
            {
                lastPoiseDamageTime = Time.time;
                if (poiseBroken)
                {
                    isPoiseBroken = true;
                }
            }

            // Track hit for procedural IK reactions
            if (damageSource != null)
            {
                // Skip IK reactions if poise broken (to avoid conflicts with stagger animations)
                if (!poiseBroken || !hasPoiseDamage)
                {
                    LastHitOrigin = damageSource.position;
                    LastHitTime = Time.time;
                    LastHitPoiseDamage = hasPoiseDamage ? poiseDamage : 10f; // Use actual poise or default
                }
                HandleDamageReaction(damageSource);
            }

            // Check for death
            if (Health <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// Determines if procedural knockback should be applied automatically.
        /// Override in child classes to disable if custom knockback handling is needed.
        /// </summary>
        protected virtual bool ShouldApplyProceduralKnockback()
        {
            return true; // Default: apply knockback for all enemies
        }
        
        protected virtual void HandleDamageReaction(Transform damageSource)
        {
            // Knockback is now handled procedurally via IKReactionUtils.CalculateKnockbackOffset()
            // which is applied in Update() for smooth, continuous knockback
            // No coroutines or instant teleports needed!
        }

        public virtual void Die()
        {
            Debug.Log($"[{gameObject.name}] Die() called! Health: {Health}");
            
            OnDeath?.Invoke();
            
            // Play death animation
            if (HasValidAnimator())
            {
                Debug.Log($"[{gameObject.name}] Setting death animation and disabling root motion. applyRootMotion before: {animator.applyRootMotion}");
                animator.SetTrigger(GameConstants.AnimatorParams.DeadHash);
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
                if (GameManager.Instance != null && GameManager.Instance.ResourceManager != null && GameManager.Instance.DifficultyManager != null)
                {
                    GameManager.Instance.ResourceManager.SpawnCharacterLoot(characterType, GameManager.Instance.DifficultyManager.GetCurrentWaveDifficulty(), transform.position + Vector3.up * 1.0f);
                }
                else
                {
                    Debug.LogWarning($"[{gameObject.name}] Cannot spawn loot - GameManager, ResourceManager, or DifficultyManager is null.");
                }
            }

            // Play death VFX
            Vector3 deathPoint = transform.position + Vector3.up * 1.5f;
            Vector3 deathNormal = Vector3.up;
            PlayDeathVFX(deathPoint, deathNormal);

            // Destroy after delay
            Destroy(gameObject, 10f);
        }
        
        /// <summary>
        /// Virtual method to play death VFX. Override in child classes to customize (e.g., parent effects to transform)
        /// </summary>
        protected virtual void PlayDeathVFX(Vector3 position, Vector3 normal)
        {
            EffectManager.Instance.PlayDeathEffect(position, normal, this);
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
        public virtual void AttackWarning()
        {
            Debug.Log($"[{gameObject.name}] AttackWarning called! Has SkinnedMeshRenderer: {skinnedMeshRenderer != null}, Has MeshRenderer: {meshRenderer != null}");
            
            if (skinnedMeshRenderer != null || meshRenderer != null)
            {
                StartCoroutine(FlashMaterialCoroutine());
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] AttackWarning: No renderer found for flash effect!");
            }
        }

        private IEnumerator FlashMaterialCoroutine()
        {
            // Flash the appropriate renderer
            if (skinnedMeshRenderer != null)
            {
                skinnedMeshRenderer.material = flashMaterial;
                yield return new WaitForSeconds(flashDuration);
                skinnedMeshRenderer.material = originalMaterial;
            }
            else if (meshRenderer != null)
            {
                meshRenderer.material = flashMaterial;
                yield return new WaitForSeconds(flashDuration);
                meshRenderer.material = originalMaterial;
            }
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

        private void UpdatePoiseRecovery()
        {
            // Only recover poise if enough time has passed since last poise damage
            if (Time.time - lastPoiseDamageTime > poiseRecoveryDelay && Poise < MaxPoise)
            {
                float recoveryAmount = poiseRecoveryRate * Time.deltaTime;
                DamageUtils.RestorePoise(this, recoveryAmount);
                
                // Reset poise broken state if we've recovered enough
                if (isPoiseBroken && Poise > MaxPoise * 0.5f)
                {
                    isPoiseBroken = false;
                }
            }
        }

        private void ApplyCharacterTypePoiseConfig()
        {
            switch (characterType)
            {
                // Human types - moderate poise
                case CharacterType.HUMAN_MALE_1:
                case CharacterType.HUMAN_MALE_2:
                case CharacterType.HUMAN_FEMALE_1:
                case CharacterType.HUMAN_FEMALE_2:
                    MaxPoise = 40f;
                    Poise = MaxPoise;
                    poiseRecoveryRate = 8f;
                    poiseRecoveryDelay = 2f;
                    break;

                // Zombie types - varying poise based on size/strength
                case CharacterType.ZOMBIE_MELEE:
                    MaxPoise = 60f;
                    Poise = MaxPoise;
                    poiseRecoveryRate = 5f;
                    poiseRecoveryDelay = 3f;
                    break;
                case CharacterType.ZOMBIE_SPITTER:
                    MaxPoise = 30f;
                    Poise = MaxPoise;
                    poiseRecoveryRate = 5f;
                    poiseRecoveryDelay = 3f;
                    break;
                case CharacterType.ZOMBIE_TANK:
                    MaxPoise = 120f;
                    Poise = MaxPoise;
                    poiseRecoveryRate = 5f;
                    poiseRecoveryDelay = 3f;
                    break;

                // Machine types - high poise resistance
                case CharacterType.MACHINE_DRONE:
                    MaxPoise = 80f;
                    Poise = MaxPoise;
                    poiseRecoveryRate = 12f;
                    poiseRecoveryDelay = 1f;
                    break;
                case CharacterType.MACHINE_TURRET_BASE_TARGET:
                    MaxPoise = 200f;
                    Poise = MaxPoise;
                    poiseRecoveryRate = 15f;
                    poiseRecoveryDelay = 1f;
                    break;
                case CharacterType.MACHINE_ROBOT:
                    MaxPoise = 100f;
                    Poise = MaxPoise;
                    poiseRecoveryRate = 12f;
                    poiseRecoveryDelay = 1f;
                    break;

                // Boss types - very high poise
                case CharacterType.BOSS_1:
                case CharacterType.BOSS_2:
                case CharacterType.BOSS_3:
                    MaxPoise = 300f;
                    Poise = MaxPoise;
                    poiseRecoveryRate = 10f;
                    poiseRecoveryDelay = 2.5f;
                    break;

                default:
                    // Keep default values
                    break;
            }
        }

        #endregion

        #region Debug Visualization

        /// <summary>
        /// Draw debug gizmos to visualize attack ranges and obstacle bounds
        /// </summary>
        protected virtual void OnDrawGizmosSelected()
        {
            // Get all attack components to determine combined attack ranges
            var attackComponents = GetComponents<AttackBase>();
            float minAttackRange = float.MaxValue;
            float maxAttackRange = 0f;
            bool hasAttacks = false;
            
            foreach (var attack in attackComponents)
            {
                if (attack != null && attack.enabled)
                {
                    hasAttacks = true;
                    minAttackRange = Mathf.Min(minAttackRange, attack.minRange);
                    maxAttackRange = Mathf.Max(maxAttackRange, attack.maxRange);
                }
            }
            
            // Draw combined attack ranges if enemy has attacks
            if (hasAttacks)
            {
                // If no minimum range found (all attacks have 0 min range), set to 0
                if (minAttackRange == float.MaxValue)
                {
                    minAttackRange = 0f;
                }
                
                // Draw minimum attack range (yellow) - area where enemy tries to stay away from
                if (minAttackRange > 0)
                {
            Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(transform.position, minAttackRange);
                }

                // Draw maximum attack range (red) - furthest the enemy can attack
                if (maxAttackRange > 0)
                {
            Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(transform.position, maxAttackRange);
                }
            }
            else
            {
                // Fallback: Draw the base stopping distance if no attacks found
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, stoppingDistance);
            }

            // Draw a line to the target
            if (navMeshTarget != null)
            {
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

            // Draw cooldown movement visualization
            if (isMovingDuringCooldown && cooldownMovementTarget != Vector3.zero)
            {
                // Draw cooldown movement target
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(cooldownMovementTarget, 0.5f);
                
                // Draw line to cooldown movement target
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, cooldownMovementTarget);
                
                // Draw arrow indicating direction
                Vector3 direction = (cooldownMovementTarget - transform.position).normalized;
                Vector3 arrowHead = cooldownMovementTarget - direction * 0.5f;
                Vector3 arrowLeft = arrowHead + Quaternion.AngleAxis(45f, Vector3.up) * -direction * 0.3f;
                Vector3 arrowRight = arrowHead + Quaternion.AngleAxis(-45f, Vector3.up) * -direction * 0.3f;
                
                Gizmos.DrawLine(arrowHead, arrowLeft);
                Gizmos.DrawLine(arrowHead, arrowRight);
            }

            // Draw head tracking visualization
            if (enableHeadTracking && isHeadTrackingActive && currentLookAtTarget != Vector3.zero)
            {
                // Draw line from enemy head to look-at target
                Vector3 headPosition = transform.position + Vector3.up * 1.6f; // Approximate head height
                Gizmos.color = Color.white;
                Gizmos.DrawLine(headPosition, currentLookAtTarget);
                
                // Draw head tracking target position
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(currentLookAtTarget, 0.2f);
                
                // Draw head tracking angle limits
                if (navMeshTarget != null)
                {
                    Vector3 targetPos = navMeshTarget.position + Vector3.up * 1.5f;
                    Vector3 directionToTarget = (targetPos - transform.position).normalized;
                    
                    // Draw left angle limit
                    Vector3 leftLimit = Quaternion.AngleAxis(-maxHeadTrackingAngle, Vector3.up) * transform.forward;
                    Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
                    Gizmos.DrawRay(transform.position + Vector3.up * 1.6f, leftLimit * headTrackingDistance);
                    
                    // Draw right angle limit
                    Vector3 rightLimit = Quaternion.AngleAxis(maxHeadTrackingAngle, Vector3.up) * transform.forward;
                    Gizmos.DrawRay(transform.position + Vector3.up * 1.6f, rightLimit * headTrackingDistance);
                }
            }
        }

        #endregion

        #region Elemental Damage System

        [Header("Elemental Resistances")]
        [SerializeField] private ElementalResistance[] resistances = new ElementalResistance[0];

        /// <summary>
        /// Gets the character's resistance to a specific damage type
        /// </summary>
        /// <param name="damageType">The damage type to check resistance for</param>
        /// <returns>The resistance level for this damage type</returns>
        public DamageResistance GetResistance(AttackElement damageType)
        {
            if (resistances != null)
            {
                foreach (var resistance in resistances)
                {
                    if (resistance != null && resistance.damageType == damageType)
                    {
                        return resistance.resistance;
                    }
                }
            }
            return DamageResistance.NORMAL;
        }
        
        /// <summary>
        /// Gets the damage multiplier for a specific damage type
        /// </summary>
        /// <param name="damageType">The damage type to check multiplier for</param>
        /// <returns>The damage multiplier (0.0 to 3.0)</returns>
        public float GetDamageMultiplier(AttackElement damageType)
        {
            return DamageUtils.GetDamageMultiplier(GetResistance(damageType));
        }

        #endregion
        
        #region IStatusEffectTarget Implementation
        
        // ========================================
        // GAMEPLAY DATA (Source of Truth)
        // This enemy owns its status effect data
        // ========================================
        
        /// <summary>
        /// Add a status effect to this enemy (gameplay data)
        /// This is the source of truth for active effects
        /// </summary>
        /// <param name="effectType">The type of status effect to add</param>
        /// <returns>True if added, false if already present</returns>
        public bool AddStatusEffect(StatusEffectType effectType)
        {
            return activeStatusEffects.Add(effectType);
        }
        
        /// <summary>
        /// Remove a status effect from this enemy (gameplay data)
        /// </summary>
        /// <param name="effectType">The type of status effect to remove</param>
        /// <returns>True if removed, false if not present</returns>
        public bool RemoveStatusEffect(StatusEffectType effectType)
        {
            return activeStatusEffects.Remove(effectType);
        }
        
        /// <summary>
        /// Check if this enemy has a specific status effect
        /// </summary>
        /// <param name="effectType">The type of status effect to check</param>
        /// <returns>True if the effect is active</returns>
        public bool HasStatusEffect(StatusEffectType effectType)
        {
            return activeStatusEffects.Contains(effectType);
        }
        
        /// <summary>
        /// Get all active status effects on this enemy
        /// </summary>
        /// <returns>Read-only collection of active status effect types</returns>
        public IReadOnlyCollection<StatusEffectType> GetActiveStatusEffects()
        {
            return activeStatusEffects;
        }
        
        /// <summary>
        /// Gets the character type for status effect configuration lookups
        /// Required by IStatusEffectTarget interface
        /// </summary>
        /// <returns>The CharacterType for this enemy</returns>
        public CharacterType GetCharacterType()
        {
            return characterType;
        }
        
        // ========================================
        // GAMEPLAY CALLBACKS (Effect Behavior)
        // Called by EffectManager after data changes
        // ========================================
        
        /// <summary>
        /// Called when a status effect is applied (after AddStatusEffect)
        /// Handle gameplay logic like movement penalties, AI behavior changes, etc.
        /// </summary>
        public virtual void OnStatusEffectApplied(StatusEffectType effectType, float duration)
        {
            Debug.Log($"[{gameObject.name}] Status effect applied: {effectType} for {duration}s");
            
            // Handle specific gameplay effects
            switch (effectType)
            {
                case StatusEffectType.ON_FIRE:
                    Debug.Log($"[{gameObject.name}] is on fire! Taking damage over time.");
                    // Could add panic/aggro behavior
                    break;
                    
                case StatusEffectType.FROZEN:
                    Debug.Log($"[{gameObject.name}] is frozen! Movement slowed.");
                    // Could reduce movement speed, animation speed
                    if (agent != null)
                    {
                        // Example: Reduce speed by 50%
                        agent.speed *= 0.5f;
                    }
                    break;
                    
                case StatusEffectType.ELECTROCUTED:
                    Debug.Log($"[{gameObject.name}] is electrocuted!");
                    // Could add stun behavior, interrupt attacks
                    break;
            }
        }
        
        /// <summary>
        /// Called when a status effect is removed (after RemoveStatusEffect)
        /// Handle cleanup logic for gameplay effects
        /// </summary>
        public virtual void OnStatusEffectRemoved(StatusEffectType effectType)
        {
            Debug.Log($"[{gameObject.name}] Status effect removed: {effectType}");
            
            // Handle specific cleanup
            switch (effectType)
            {
                case StatusEffectType.FROZEN:
                    Debug.Log($"[{gameObject.name}] is no longer frozen.");
                    // Restore movement speed
                    if (agent != null)
                    {
                        // Example: Restore speed
                        agent.speed = movementSpeed;
                    }
                    break;
                    
                case StatusEffectType.ELECTROCUTED:
                    Debug.Log($"[{gameObject.name}] is no longer electrocuted.");
                    break;
            }
        }
        
        /// <summary>
        /// Utility: Apply a status effect with VFX through EffectManager
        /// For external systems that want both gameplay AND visual effects
        /// </summary>
        public void ApplyStatusEffectWithVFX(StatusEffectType effectType, float duration = 0f)
        {
            if (EffectManager.Instance != null)
            {
                EffectManager.Instance.ApplyStatusEffect(this, effectType, duration);
            }
        }
        
        /// <summary>
        /// Utility: Remove a status effect with VFX through EffectManager
        /// For external systems that want both gameplay AND visual effects
        /// </summary>
        public void RemoveStatusEffectWithVFX(StatusEffectType effectType)
        {
            if (EffectManager.Instance != null)
            {
                EffectManager.Instance.RemoveStatusEffect(this, effectType);
            }
        }
        
        #endregion
    }
}
