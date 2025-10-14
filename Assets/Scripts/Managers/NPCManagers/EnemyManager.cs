using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Enemies;

namespace Managers
{
    /// <summary>
    /// ═══════════════════════════════════════════════════════════════════════════════════════
    /// ENEMY MANAGER - Group AI Coordination System
    /// ═══════════════════════════════════════════════════════════════════════════════════════
    /// 
    /// OVERVIEW:
    /// Provides "shared brain" coordination for all enemies in the game. Creates intelligent,
    /// tactical encounters where enemies work together rather than acting independently.
    /// 
    /// ═══════════════════════════════════════════════════════════════════════════════════════
    /// SETUP:
    /// 1. Create empty GameObject in scene named "EnemyManager"
    /// 2. Add this component
    /// 3. Enemies auto-register in EnemyBase.Start()
    /// 4. Tune settings in Inspector
    /// ═══════════════════════════════════════════════════════════════════════════════════════
    /// 
    /// FEATURES:
    /// 
    /// ┌─ PACK HUNTING SYSTEM ─────────────────────────────────────────────────────────────┐
    /// │ Enemies assigned different roles dynamically based on distance to player:         │
    /// │                                                                                    │
    /// │ • CHASER (maxActiveChasers closest enemies)                                       │
    /// │   - Actively pursue and flank player                                              │
    /// │   - Circle while waiting to attack                                                │
    /// │   - Get attack priority                                                           │
    /// │                                                                                    │
    /// │ • INTERCEPTOR (next closest if enabled)                                           │
    /// │   - Predict player movement using velocity                                        │
    /// │   - Position ahead to cut off escape routes                                       │
    /// │   - Block corridors and choke points                                              │
    /// │                                                                                    │
    /// │ • WANDERER (remaining enemies)                                                    │
    /// │   - Roam around player's general area                                             │
    /// │   - Create ambient threat                                                         │
    /// │   - Can become chasers if player approaches                                       │
    /// │                                                                                    │
    /// │ RESULT: Not all enemies mindlessly chase - creates tactical challenge!            │
    /// └────────────────────────────────────────────────────────────────────────────────────┘
    /// 
    /// ┌─ ATTACK COORDINATION ──────────────────────────────────────────────────────────────┐
    /// │ Prevents overwhelming player with simultaneous attacks:                            │
    /// │                                                                                    │
    /// │ • Max simultaneous attackers (default: 2)                                         │
    /// │ • Attack stagger delay between initiations (default: 0.5s)                        │
    /// │ • Post-attack cooldown before next enemy can attack (default: 1.0s)               │
    /// │ • Queue system for waiting enemies                                                │
    /// │                                                                                    │
    /// │ RESULT: Rhythmic combat with natural flow, not chaos!                             │
    /// └────────────────────────────────────────────────────────────────────────────────────┘
    /// 
    /// ┌─ FLANKING & POSITIONING ───────────────────────────────────────────────────────────┐
    /// │ Chasers spread around target in circle formation:                                 │
    /// │                                                                                    │
    /// │ • Dynamic angle assignment (min 45° apart)                                        │
    /// │ • Circling behavior - rotate positions over time (30°/s)                          │
    /// │ • Personal space enforcement (1.5m radius)                                        │
    /// │ • Works for both melee AND ranged enemies                                         │
    /// │                                                                                    │
    /// │ RESULT: Enemies attack from multiple angles, must watch surroundings!             │
    /// └────────────────────────────────────────────────────────────────────────────────────┘
    /// 
    /// ┌─ STAGGERED MOVEMENT (Fix for synchronized robot-like movement) ───────────────────┐
    /// │ Each enemy updates position independently:                                        │
    /// │                                                                                    │
    /// │ • Individual update timers per enemy                                              │
    /// │ • Random variance (±0.3s) prevents emergent synchronization                       │
    /// │ • Faster update interval (1.0s vs 2.0s)                                           │
    /// │                                                                                    │
    /// │ BEFORE: All enemies stop/start together as a group (robotic)                      │
    /// │ AFTER: Fluid continuous movement, always enemies repositioning                    │
    /// │                                                                                    │
    /// │ RESULT: Natural-looking mob behavior, no synchronized stopping!                   │
    /// └────────────────────────────────────────────────────────────────────────────────────┘
    /// 
    /// ┌─ AGGRO / THREAT SYSTEM ────────────────────────────────────────────────────────────┐
    /// │ Tracks damage dealt to prioritize targets:                                        │
    /// │                                                                                    │
    /// │ • Threat added when damage dealt (multiplied by 10)                               │
    /// │ • Threat decays over time (default: 5/sec)                                        │
    /// │ • Can query highest threat target                                                 │
    /// │                                                                                    │
    /// │ RESULT: Enemies remember who attacked them!                                       │
    /// └────────────────────────────────────────────────────────────────────────────────────┘
    /// 
    /// ═══════════════════════════════════════════════════════════════════════════════════════
    /// DIFFICULTY TUNING:
    /// 
    /// EASY MODE:
    ///   Max Attackers: 1, Stagger Delay: 1.0s, Flanking: false
    ///   → Classic "take turns" combat
    /// 
    /// NORMAL MODE (Default):
    ///   Max Attackers: 2, Stagger Delay: 0.5s, Flanking: true, Circling: true
    ///   → Dynamic, engaging encounters
    /// 
    /// HARD MODE:
    ///   Max Attackers: 3-4, Stagger Delay: 0.3s, Circling Speed: 45°/s
    ///   → High pressure, must manage multiple threats
    /// 
    /// ═══════════════════════════════════════════════════════════════════════════════════════
    /// </summary>
    public class EnemyManager : MonoBehaviour
    {
        #region Singleton

        private static EnemyManager _instance;
        public static EnemyManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<EnemyManager>();
                    if (_instance == null)
                    {
                        Debug.LogWarning("EnemyManager instance not found in the scene!");
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
            }
        }

        #endregion

        #region Serialized Fields

        [Header("Pack Hunting Settings")]
        [Tooltip("Maximum number of enemies actively chasing the player")]
        [SerializeField] private int maxActiveChasers = 3;
        
        [Tooltip("Maximum distance for enemies to actively chase player")]
        [SerializeField] private float maxChaseDistance = 30f;
        
        [Tooltip("How often to reassign roles (seconds)")]
        [SerializeField] private float roleReassignmentInterval = 2f;

        [Header("Attack Coordination Settings")]
        [Tooltip("Maximum number of enemies that can attack simultaneously")]
        [SerializeField] private int maxSimultaneousAttackers = 2;

        [Tooltip("Minimum time between attack initiations (seconds)")]
        [SerializeField] private float attackStaggerDelay = 0.5f;

        [Tooltip("Cooldown period after an enemy attacks before another can attack")]
        [SerializeField] private float postAttackCooldown = 1.0f;

        [Header("Positioning Coordination Settings")]
        [Tooltip("Enable flanking behavior - enemies spread out around target")]
        [SerializeField] private bool enableFlankingBehavior = true;

        [Tooltip("Minimum angle between enemies around target (degrees)")]
        [SerializeField] private float minAngleBetweenEnemies = 45f;

        [Tooltip("Preferred distance from target for positioning")]
        [SerializeField] private float preferredAttackDistance = 3f;

        [Tooltip("How often to update enemy positioning (seconds)")]
        [SerializeField] private float positionUpdateInterval = 1f;
        
        [Tooltip("Enable circling behavior for enemies waiting to attack")]
        [SerializeField] private bool enableCirclingBehavior = true;
        
        [Tooltip("Speed at which enemies circle around target (degrees per second)")]
        [SerializeField] private float circlingSpeed = 30f;
        
        [Tooltip("Minimum personal space distance between enemies")]
        [SerializeField] private float personalSpaceRadius = 1.5f;
        
        [Tooltip("Stagger position updates across enemies to avoid synchronized movement")]
        [SerializeField] private bool staggerPositionUpdates = true;
        
        [Tooltip("Random variance in update timing (prevents exact synchronization)")]
        [SerializeField] private float updateVariance = 0.3f;
        
        [Header("Strategic Behavior Settings")]
        [Tooltip("Enable interceptor behavior - enemies predict player movement")]
        [SerializeField] private bool enableInterceptorBehavior = true;
        
        [Tooltip("How far ahead to predict player position (seconds)")]
        [SerializeField] private float predictionTime = 2f;
        
        [Tooltip("Enable wandering for non-chasers")]
        [SerializeField] private bool enableWanderingBehavior = true;
        
        [Tooltip("Wandering radius for non-active enemies")]
        [SerializeField] private float wanderRadius = 15f;
        
        [Tooltip("How often wanderers pick new destination (seconds)")]
        [SerializeField] private float wanderInterval = 5f;

        [Header("Aggro/Threat System Settings")]
        [Tooltip("Enable aggro/threat system")]
        [SerializeField] private bool enableAggroSystem = true;

        [Tooltip("Threat decay rate per second")]
        [SerializeField] private float threatDecayRate = 5f;

        [Header("Debug Settings")]
        [Tooltip("Show debug information in console")]
        [SerializeField] private bool showDebug = false;

        #endregion

        #region Private Fields

        // Track all active enemies
        private List<EnemyBase> activeEnemies = new List<EnemyBase>();
        
        // Pack hunting roles
        public enum EnemyRole
        {
            CHASER,      // Actively pursuing player
            INTERCEPTOR, // Predicting and cutting off player
            WANDERER     // Roaming, looking for opportunities
        }
        
        private Dictionary<EnemyBase, EnemyRole> enemyRoles = new Dictionary<EnemyBase, EnemyRole>();
        private Dictionary<EnemyBase, float> nextRoleCheckTime = new Dictionary<EnemyBase, float>();
        private Dictionary<EnemyBase, Vector3> wanderDestinations = new Dictionary<EnemyBase, Vector3>();
        private Dictionary<EnemyBase, float> nextWanderTime = new Dictionary<EnemyBase, float>();
        private float lastRoleReassignmentTime = 0f;
        
        // Player tracking
        private Vector3 lastPlayerPosition = Vector3.zero;
        private Vector3 playerVelocity = Vector3.zero;

        // Attack coordination
        private List<EnemyBase> currentlyAttacking = new List<EnemyBase>();
        private float lastAttackInitiationTime = 0f;
        private Queue<EnemyBase> attackQueue = new Queue<EnemyBase>();

        // Positioning coordination
        private Dictionary<EnemyBase, Vector3> assignedPositions = new Dictionary<EnemyBase, Vector3>();
        private Dictionary<EnemyBase, float> assignedAngles = new Dictionary<EnemyBase, float>();
        private Dictionary<EnemyBase, float> nextUpdateTimes = new Dictionary<EnemyBase, float>();
        private float lastPositionUpdateTime = 0f;

        // Aggro/Threat system
        private Dictionary<Transform, float> targetThreatLevels = new Dictionary<Transform, float>();

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (activeEnemies.Count == 0) return;
            
            // Track player movement
            UpdatePlayerTracking();
            
            // Update pack hunting roles
            UpdatePackRoles();

            // Update attack coordination
            UpdateAttackCoordination();

            // Update positioning coordination
            if (enableFlankingBehavior)
            {
                if (staggerPositionUpdates)
                {
                    UpdatePositionCoordinationStaggered();
                }
                else if (Time.time - lastPositionUpdateTime >= positionUpdateInterval)
                {
                    UpdatePositionCoordination();
                    lastPositionUpdateTime = Time.time;
                }
            }

            // Update threat/aggro system
            if (enableAggroSystem)
            {
                UpdateThreatSystem();
            }

            // Clean up null references
            CleanupNullReferences();
        }

        #endregion

        #region Pack Hunting System
        
        // ═══════════════════════════════════════════════════════════════════════════════════════
        // PACK HUNTING SYSTEM - "Shared Brain" Role-Based AI
        // ═══════════════════════════════════════════════════════════════════════════════════════
        // 
        // PURPOSE:
        // Give enemies a coordinated "mob brain" instead of independent mindless chasing.
        // 
        // HOW IT WORKS:
        // 1. Track player position and velocity every frame
        // 2. Every roleReassignmentInterval (2s), sort enemies by distance
        // 3. Assign roles:
        //    - Closest N → CHASER (actively pursue and flank)
        //    - Next N   → INTERCEPTOR (predict movement, cut off)
        //    - Rest     → WANDERER (roam, create ambient threat)
        // 4. Roles dynamically shift as player moves
        // 
        // BENEFITS:
        // • Player can't just run in circles - interceptors block escape
        // • Not all enemies chase - prevents "zombie train"
        // • Wanderers create unpredictable encounters
        // • Scales naturally with enemy count
        // 
        // INTEGRATION:
        // EnemyBase.UpdateMovement() calls GetStrategicPosition(this) which returns:
        // • CHASER → flanking position around player
        // • INTERCEPTOR → predicted future position
        // • WANDERER → random wander point
        // ═══════════════════════════════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Track player movement and calculate velocity for prediction
        /// </summary>
        private void UpdatePlayerTracking()
        {
            // Get player position
            Transform playerTransform = null;
            if (PlayerController.Instance != null && PlayerController.Instance._possessedNPC != null)
            {
                playerTransform = PlayerController.Instance._possessedNPC.GetTransform();
            }
            
            if (playerTransform == null) return;
            
            Vector3 currentPlayerPosition = playerTransform.position;
            
            // Calculate velocity
            if (lastPlayerPosition != Vector3.zero)
            {
                playerVelocity = (currentPlayerPosition - lastPlayerPosition) / Time.deltaTime;
            }
            
            lastPlayerPosition = currentPlayerPosition;
        }
        
        private void UpdatePackRoles()
        {
            if (Time.time - lastRoleReassignmentTime < roleReassignmentInterval) return;
            lastRoleReassignmentTime = Time.time;
            
            // Get player position
            if (lastPlayerPosition == Vector3.zero) return;
            
            // Get all valid enemies
            var validEnemies = activeEnemies
                .Where(e => e != null && e.Health > 0)
                .ToList();
            
            if (validEnemies.Count == 0) return;
            
            // Sort by distance to player
            var sortedByDistance = validEnemies
                .OrderBy(e => Vector3.Distance(e.transform.position, lastPlayerPosition))
                .ToList();
            
            // Assign roles
            int chaserCount = 0;
            int interceptorCount = 0;
            
            foreach (var enemy in sortedByDistance)
            {
                float distanceToPlayer = Vector3.Distance(enemy.transform.position, lastPlayerPosition);
                
                // Closest enemies become chasers (up to max)
                if (chaserCount < maxActiveChasers && distanceToPlayer < maxChaseDistance)
                {
                    SetEnemyRole(enemy, EnemyRole.CHASER);
                    chaserCount++;
                }
                // Next closest become interceptors (if enabled)
                else if (enableInterceptorBehavior && interceptorCount < maxActiveChasers && distanceToPlayer < maxChaseDistance * 1.5f)
                {
                    SetEnemyRole(enemy, EnemyRole.INTERCEPTOR);
                    interceptorCount++;
                }
                // Rest become wanderers
                else
                {
                    SetEnemyRole(enemy, EnemyRole.WANDERER);
                }
            }
            
            if (showDebug)
            {
                Debug.Log($"[EnemyManager] Pack Roles: {chaserCount} Chasers, {interceptorCount} Interceptors, {validEnemies.Count - chaserCount - interceptorCount} Wanderers");
            }
        }
        
        private void SetEnemyRole(EnemyBase enemy, EnemyRole newRole)
        {
            if (!enemyRoles.ContainsKey(enemy) || enemyRoles[enemy] != newRole)
            {
                enemyRoles[enemy] = newRole;
                
                // Initialize role-specific data
                if (newRole == EnemyRole.WANDERER && !wanderDestinations.ContainsKey(enemy))
                {
                    AssignNewWanderDestination(enemy);
                }
                
                if (showDebug)
                {
                    Debug.Log($"[EnemyManager] {enemy.name} assigned role: {newRole}");
                }
            }
        }
        
        private void AssignNewWanderDestination(EnemyBase enemy)
        {
            if (lastPlayerPosition == Vector3.zero) return;
            
            // Wander in a radius around player's general area
            Vector3 randomOffset = new Vector3(
                Random.Range(-wanderRadius, wanderRadius),
                0f,
                Random.Range(-wanderRadius, wanderRadius)
            );
            
            Vector3 wanderTarget = lastPlayerPosition + randomOffset;
            
            // Sample NavMesh
            if (UnityEngine.AI.NavMesh.SamplePosition(wanderTarget, out UnityEngine.AI.NavMeshHit hit, wanderRadius, UnityEngine.AI.NavMesh.AllAreas))
            {
                wanderDestinations[enemy] = hit.position;
                nextWanderTime[enemy] = Time.time + wanderInterval;
                
                if (showDebug && Time.frameCount % 60 == 0)
                {
                    Debug.Log($"[EnemyManager] {enemy.name} assigned wander destination at {hit.position}");
                }
            }
        }
        
        /// <summary>
        /// Get the current role of an enemy
        /// </summary>
        public EnemyRole GetEnemyRole(EnemyBase enemy)
        {
            if (enemyRoles.TryGetValue(enemy, out EnemyRole role))
            {
                return role;
            }
            return EnemyRole.CHASER; // Default to chaser
        }
        
        /// <summary>
        /// Get strategic position for an enemy based on their role
        /// </summary>
        public Vector3 GetStrategicPosition(EnemyBase enemy)
        {
            if (!enemyRoles.ContainsKey(enemy))
            {
                return Vector3.zero;
            }
            
            EnemyRole role = enemyRoles[enemy];
            
            switch (role)
            {
                case EnemyRole.CHASER:
                    // Chasers use flanking positions
                    return GetAssignedPosition(enemy);
                    
                case EnemyRole.INTERCEPTOR:
                    // Interceptors predict player movement
                    return CalculateInterceptPosition(enemy);
                    
                case EnemyRole.WANDERER:
                    // Wanderers move to assigned wander points
                    return GetWanderDestination(enemy);
                    
                default:
                    return Vector3.zero;
            }
        }
        
        private Vector3 CalculateInterceptPosition(EnemyBase enemy)
        {
            if (lastPlayerPosition == Vector3.zero || playerVelocity.magnitude < 0.1f)
            {
                // Player not moving, use flanking position
                return GetAssignedPosition(enemy);
            }
            
            // Predict where player will be
            Vector3 predictedPosition = lastPlayerPosition + playerVelocity * predictionTime;
            
            // Get direction from enemy to intercept point
            Vector3 directionToIntercept = (predictedPosition - enemy.transform.position).normalized;
            
            // Position ahead of player on their path
            Vector3 interceptPoint = predictedPosition + directionToIntercept * preferredAttackDistance;
            
            // Sample NavMesh
            if (UnityEngine.AI.NavMesh.SamplePosition(interceptPoint, out UnityEngine.AI.NavMeshHit hit, 10f, UnityEngine.AI.NavMesh.AllAreas))
            {
                return hit.position;
            }
            
            // Fallback to predicted position
            if (UnityEngine.AI.NavMesh.SamplePosition(predictedPosition, out hit, 10f, UnityEngine.AI.NavMesh.AllAreas))
            {
                return hit.position;
            }
            
            return Vector3.zero;
        }
        
        private Vector3 GetWanderDestination(EnemyBase enemy)
        {
            // Check if it's time for a new wander destination
            if (!nextWanderTime.ContainsKey(enemy) || Time.time >= nextWanderTime[enemy])
            {
                AssignNewWanderDestination(enemy);
            }
            
            if (wanderDestinations.TryGetValue(enemy, out Vector3 destination))
            {
                // Check if we've reached the destination
                float distanceToDestination = Vector3.Distance(enemy.transform.position, destination);
                if (distanceToDestination < 2f)
                {
                    // Reached destination, assign new one soon
                    nextWanderTime[enemy] = Time.time + Random.Range(2f, 5f);
                }
                
                return destination;
            }
            
            return Vector3.zero;
        }
        
        #endregion

        #region Enemy Registration

        /// <summary>
        /// Register an enemy with the manager
        /// </summary>
        public void RegisterEnemy(EnemyBase enemy)
        {
            if (enemy == null || activeEnemies.Contains(enemy)) return;

            activeEnemies.Add(enemy);
            
            // Assign initial update time with stagger
            if (staggerPositionUpdates)
            {
                float staggerOffset = (activeEnemies.Count - 1) * (positionUpdateInterval / Mathf.Max(activeEnemies.Count, 1));
                nextUpdateTimes[enemy] = Time.time + staggerOffset + Random.Range(0f, updateVariance);
            }

            if (showDebug)
            {
                Debug.Log($"[EnemyManager] Registered enemy: {enemy.name} | Total active: {activeEnemies.Count}");
            }
        }

        /// <summary>
        /// Unregister an enemy from the manager
        /// </summary>
        public void UnregisterEnemy(EnemyBase enemy)
        {
            if (enemy == null) return;

            activeEnemies.Remove(enemy);
            currentlyAttacking.Remove(enemy);
            assignedPositions.Remove(enemy);

            if (showDebug)
            {
                Debug.Log($"[EnemyManager] Unregistered enemy: {enemy.name} | Total active: {activeEnemies.Count}");
            }
        }

        #endregion

        #region Attack Coordination

        /// <summary>
        /// Request permission for an enemy to attack
        /// Returns true if the enemy can attack now, false if they should wait
        /// </summary>
        public bool RequestAttackPermission(EnemyBase enemy)
        {
            if (enemy == null) return false;

            // Check if already attacking
            if (currentlyAttacking.Contains(enemy))
            {
                return true;
            }

            // Check if we've reached max simultaneous attackers
            if (currentlyAttacking.Count >= maxSimultaneousAttackers)
            {
                // Add to queue if not already there
                if (!attackQueue.Contains(enemy))
                {
                    attackQueue.Enqueue(enemy);
                    
                    if (showDebug)
                    {
                        Debug.Log($"[EnemyManager] {enemy.name} added to attack queue. Queue size: {attackQueue.Count}");
                    }
                }
                return false;
            }

            // Check stagger delay
            if (Time.time - lastAttackInitiationTime < attackStaggerDelay)
            {
                if (!attackQueue.Contains(enemy))
                {
                    attackQueue.Enqueue(enemy);
                }
                return false;
            }

            // Permission granted
            currentlyAttacking.Add(enemy);
            lastAttackInitiationTime = Time.time;

            if (showDebug)
            {
                Debug.Log($"[EnemyManager] Attack permission granted to {enemy.name}. Currently attacking: {currentlyAttacking.Count}");
            }

            return true;
        }

        /// <summary>
        /// Notify the manager that an enemy has finished attacking
        /// </summary>
        public void NotifyAttackComplete(EnemyBase enemy)
        {
            if (enemy == null) return;

            currentlyAttacking.Remove(enemy);

            if (showDebug)
            {
                Debug.Log($"[EnemyManager] {enemy.name} completed attack. Currently attacking: {currentlyAttacking.Count}");
            }

            // Try to process queue after cooldown
            StartCoroutine(ProcessAttackQueueAfterDelay());
        }

        private IEnumerator ProcessAttackQueueAfterDelay()
        {
            yield return new WaitForSeconds(postAttackCooldown);

            // Process queue
            if (attackQueue.Count > 0 && currentlyAttacking.Count < maxSimultaneousAttackers)
            {
                EnemyBase nextEnemy = attackQueue.Dequeue();
                
                // Verify enemy is still valid
                if (nextEnemy != null && nextEnemy.Health > 0)
                {
                    currentlyAttacking.Add(nextEnemy);
                    lastAttackInitiationTime = Time.time;

                    if (showDebug)
                    {
                        Debug.Log($"[EnemyManager] Processed queue - {nextEnemy.name} can now attack");
                    }
                }
            }
        }

        private void UpdateAttackCoordination()
        {
            // Remove enemies that are no longer attacking
            currentlyAttacking.RemoveAll(e => e == null || !e.isAttacking);

            // Clean up queue
            var tempQueue = new Queue<EnemyBase>();
            while (attackQueue.Count > 0)
            {
                var enemy = attackQueue.Dequeue();
                if (enemy != null && enemy.Health > 0)
                {
                    tempQueue.Enqueue(enemy);
                }
            }
            attackQueue = tempQueue;
        }

        #endregion

        #region Positioning Coordination
        
        // ═══════════════════════════════════════════════════════════════════════════════════════
        // POSITIONING COORDINATION - Flanking & Staggered Updates
        // ═══════════════════════════════════════════════════════════════════════════════════════
        // 
        // FLANKING SYSTEM:
        // Chasers spread in a circle around target, maintaining angle separation and personal space.
        // Circling enabled: positions rotate slowly (30°/s default) for dynamic combat.
        // 
        // STAGGERED UPDATES (Fix for synchronized movement):
        // 
        // PROBLEM:
        // All enemies updating positions at once → all stop/start together → robotic movement
        // 
        // SOLUTION:
        // Each enemy has individual update timer with random variance:
        // 
        // Old behavior (staggerPositionUpdates = false):
        //   Time 0s:  ALL enemies update → all move
        //   Time 2s:  ALL enemies update → all move
        //   Result: Synchronized, stop-start as a group
        // 
        // New behavior (staggerPositionUpdates = true, default):
        //   Time 0.0s: Enemy 1 updates → moves
        //   Time 0.3s: Enemy 2 updates → moves
        //   Time 0.6s: Enemy 3 updates → moves
        //   Time 1.0s: Enemy 1 updates again → keeps moving
        //   Result: Fluid, continuous movement, no group stopping
        // 
        // BENEFITS:
        // • No synchronized stopping/starting
        // • More responsive (1s interval vs 2s)
        // • Better frame pacing (updates spread across frames)
        // • Random variance prevents emergent synchronization
        // ═══════════════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Get the assigned position for an enemy, or Vector3.zero if none assigned
        /// </summary>
        public Vector3 GetAssignedPosition(EnemyBase enemy)
        {
            if (assignedPositions.TryGetValue(enemy, out Vector3 position))
            {
                return position;
            }
            return Vector3.zero;
        }

        /// <summary>
        /// Check if an enemy should move to a flanking position
        /// </summary>
        public bool ShouldRepositionForFlanking(EnemyBase enemy)
        {
            if (!enableFlankingBehavior) return false;
            if (enemy == null || enemy.NavMeshTarget == null) return false;

            Vector3 assignedPos = GetAssignedPosition(enemy);
            if (assignedPos == Vector3.zero) return false;

            // Check if enemy is far from assigned position
            float distanceToAssigned = Vector3.Distance(enemy.transform.position, assignedPos);
            return distanceToAssigned > 2f;
        }

        private void UpdatePositionCoordination()
        {
            // Group enemies by their targets
            var enemyGroups = activeEnemies
                .Where(e => e != null && e.Health > 0 && e.NavMeshTarget != null)
                .GroupBy(e => e.NavMeshTarget)
                .ToList();

            foreach (var group in enemyGroups)
            {
                Transform target = group.Key;
                List<EnemyBase> enemies = group.ToList();

                if (enemies.Count < 1) continue; // Need at least one enemy

                // Calculate flanking positions around the target
                AssignFlankingPositions(enemies, target);
            }
        }
        
        private void UpdatePositionCoordinationStaggered()
        {
            // Check each enemy individually for updates
            foreach (var enemy in activeEnemies)
            {
                if (enemy == null || enemy.Health <= 0 || enemy.NavMeshTarget == null) continue;
                
                // Check if it's time for this enemy's update
                if (!nextUpdateTimes.ContainsKey(enemy))
                {
                    // First time - assign immediate update with small random offset
                    nextUpdateTimes[enemy] = Time.time + Random.Range(0f, updateVariance);
                }
                
                if (Time.time >= nextUpdateTimes[enemy])
                {
                    // Update this enemy's position
                    UpdateSingleEnemyPosition(enemy);
                    
                    // Schedule next update with variance
                    nextUpdateTimes[enemy] = Time.time + positionUpdateInterval + Random.Range(-updateVariance, updateVariance);
                }
            }
        }
        
        private void UpdateSingleEnemyPosition(EnemyBase enemy)
        {
            if (enemy == null || enemy.NavMeshTarget == null) return;
            
            Transform target = enemy.NavMeshTarget;
            
            // Get all enemies targeting the same target
            var allies = activeEnemies
                .Where(e => e != null && e.Health > 0 && e.NavMeshTarget == target)
                .ToList();
            
            // Update position for this group
            AssignFlankingPositions(allies, target);
        }

        private void AssignFlankingPositions(List<EnemyBase> enemies, Transform target)
        {
            if (target == null) return;

            int enemyCount = enemies.Count;
            float angleIncrement = 360f / enemyCount;

            // Use consistent base angle for current target, with slow rotation if circling enabled
            float baseAngle = 0f;
            if (enableCirclingBehavior)
            {
                baseAngle = Time.time * circlingSpeed; // Rotate over time
            }

            for (int i = 0; i < enemyCount; i++)
            {
                EnemyBase enemy = enemies[i];
                if (enemy == null) continue;

                // Try to keep existing angle if enemy already has one, otherwise assign new
                float angle;
                if (assignedAngles.ContainsKey(enemy))
                {
                    // Update existing angle with circling
                    if (enableCirclingBehavior && !enemy.isAttacking)
                    {
                        assignedAngles[enemy] += circlingSpeed * positionUpdateInterval;
                        angle = assignedAngles[enemy];
                    }
                    else
                    {
                        angle = assignedAngles[enemy];
                    }
                }
                else
                {
                    // Assign new angle based on current position if possible
                    Vector3 directionFromTarget = (enemy.transform.position - target.position).normalized;
                    angle = Mathf.Atan2(directionFromTarget.x, directionFromTarget.z) * Mathf.Rad2Deg;
                    assignedAngles[enemy] = angle;
                }
                
                // Ensure minimum angle separation from other enemies
                foreach (var otherEnemy in enemies)
                {
                    if (otherEnemy == enemy || otherEnemy == null) continue;
                    if (!assignedAngles.ContainsKey(otherEnemy)) continue;
                    
                    float otherAngle = assignedAngles[otherEnemy];
                    float angleDiff = Mathf.Abs(Mathf.DeltaAngle(angle, otherAngle));
                    
                    if (angleDiff < minAngleBetweenEnemies)
                    {
                        // Push away from other enemy
                        angle += minAngleBetweenEnemies - angleDiff;
                        assignedAngles[enemy] = angle;
                    }
                }

                // Calculate distance based on enemy type
                float distance = preferredAttackDistance;
                
                // Adjust distance based on personal space
                Vector3 idealDirection = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                Vector3 idealPosition = target.position + idealDirection * distance;
                
                // Check for other enemies nearby and adjust distance to maintain personal space
                foreach (var otherEnemy in enemies)
                {
                    if (otherEnemy == enemy || otherEnemy == null) continue;
                    
                    float distToOther = Vector3.Distance(idealPosition, otherEnemy.transform.position);
                    if (distToOther < personalSpaceRadius)
                    {
                        // Push outward
                        distance += personalSpaceRadius - distToOther;
                    }
                }

                // Calculate final position
                Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                Vector3 targetPosition = target.position + direction * distance;

                // Sample NavMesh for valid position
                if (UnityEngine.AI.NavMesh.SamplePosition(targetPosition, out UnityEngine.AI.NavMeshHit hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    assignedPositions[enemy] = hit.position;

                    if (showDebug && Time.frameCount % 120 == 0) // Log occasionally
                    {
                        Debug.Log($"[EnemyManager] Assigned flanking position to {enemy.name} at angle {angle:F0}° from {target.name}");
                    }
                }
            }
        }

        #endregion

        #region Aggro/Threat System

        /// <summary>
        /// Add threat to a target
        /// </summary>
        public void AddThreat(Transform target, float amount)
        {
            if (!enableAggroSystem || target == null) return;

            if (!targetThreatLevels.ContainsKey(target))
            {
                targetThreatLevels[target] = 0f;
            }

            targetThreatLevels[target] += amount;

            if (showDebug)
            {
                Debug.Log($"[EnemyManager] Added {amount} threat to {target.name}. Total: {targetThreatLevels[target]:F1}");
            }
        }

        /// <summary>
        /// Get the threat level for a target
        /// </summary>
        public float GetThreatLevel(Transform target)
        {
            if (target == null || !targetThreatLevels.ContainsKey(target))
            {
                return 0f;
            }
            return targetThreatLevels[target];
        }

        /// <summary>
        /// Get the highest threat target
        /// </summary>
        public Transform GetHighestThreatTarget()
        {
            if (targetThreatLevels.Count == 0) return null;

            Transform highestThreat = null;
            float maxThreat = 0f;

            foreach (var kvp in targetThreatLevels)
            {
                if (kvp.Value > maxThreat && kvp.Key != null)
                {
                    maxThreat = kvp.Value;
                    highestThreat = kvp.Key;
                }
            }

            return highestThreat;
        }

        private void UpdateThreatSystem()
        {
            // Decay threat over time
            var keys = targetThreatLevels.Keys.ToList();
            foreach (var target in keys)
            {
                if (target == null)
                {
                    targetThreatLevels.Remove(target);
                    continue;
                }

                targetThreatLevels[target] = Mathf.Max(0f, targetThreatLevels[target] - threatDecayRate * Time.deltaTime);
            }

            // Remove zero threat entries
            targetThreatLevels = targetThreatLevels
                .Where(kvp => kvp.Value > 0.01f)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Get all active enemies
        /// </summary>
        public List<EnemyBase> GetActiveEnemies()
        {
            return new List<EnemyBase>(activeEnemies);
        }

        /// <summary>
        /// Get enemies targeting a specific transform
        /// </summary>
        public List<EnemyBase> GetEnemiesTargeting(Transform target)
        {
            return activeEnemies
                .Where(e => e != null && e.NavMeshTarget == target)
                .ToList();
        }

        /// <summary>
        /// Get number of enemies currently attacking
        /// </summary>
        public int GetAttackingEnemyCount()
        {
            return currentlyAttacking.Count;
        }

        private void CleanupNullReferences()
        {
            activeEnemies.RemoveAll(e => e == null);
            currentlyAttacking.RemoveAll(e => e == null);
            
            var keysToRemove = assignedPositions.Keys.Where(e => e == null).ToList();
            foreach (var key in keysToRemove)
            {
                assignedPositions.Remove(key);
                assignedAngles.Remove(key);
                nextUpdateTimes.Remove(key);
                enemyRoles.Remove(key);
                nextRoleCheckTime.Remove(key);
                wanderDestinations.Remove(key);
                nextWanderTime.Remove(key);
            }
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Update attack coordination settings
        /// </summary>
        public void SetAttackCoordinationSettings(int maxAttackers, float staggerDelay, float cooldown)
        {
            maxSimultaneousAttackers = Mathf.Max(1, maxAttackers);
            attackStaggerDelay = Mathf.Max(0f, staggerDelay);
            postAttackCooldown = Mathf.Max(0f, cooldown);

            if (showDebug)
            {
                Debug.Log($"[EnemyManager] Updated attack settings: MaxAttackers={maxSimultaneousAttackers}, StaggerDelay={attackStaggerDelay}, Cooldown={postAttackCooldown}");
            }
        }

        /// <summary>
        /// Enable or disable flanking behavior
        /// </summary>
        public void SetFlankingEnabled(bool enabled)
        {
            enableFlankingBehavior = enabled;
        }

        /// <summary>
        /// Enable or disable aggro system
        /// </summary>
        public void SetAggroEnabled(bool enabled)
        {
            enableAggroSystem = enabled;
        }

        #endregion
    }
}

