using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Enemies;

namespace Managers
{
    /// <summary>
    /// Manages group enemy behavior coordination including:
    /// - Attack staggering (prevents all enemies attacking at once)
    /// - Aggro/threat management
    /// - Flanking and positioning coordination
    /// - Making combat more challenging and engaging
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
        [SerializeField] private float positionUpdateInterval = 2f;
        
        [Tooltip("Enable circling behavior for enemies waiting to attack")]
        [SerializeField] private bool enableCirclingBehavior = true;
        
        [Tooltip("Speed at which enemies circle around target (degrees per second)")]
        [SerializeField] private float circlingSpeed = 30f;
        
        [Tooltip("Minimum personal space distance between enemies")]
        [SerializeField] private float personalSpaceRadius = 1.5f;

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

        // Attack coordination
        private List<EnemyBase> currentlyAttacking = new List<EnemyBase>();
        private float lastAttackInitiationTime = 0f;
        private Queue<EnemyBase> attackQueue = new Queue<EnemyBase>();

        // Positioning coordination
        private Dictionary<EnemyBase, Vector3> assignedPositions = new Dictionary<EnemyBase, Vector3>();
        private Dictionary<EnemyBase, float> assignedAngles = new Dictionary<EnemyBase, float>();
        private float lastPositionUpdateTime = 0f;

        // Aggro/Threat system
        private Dictionary<Transform, float> targetThreatLevels = new Dictionary<Transform, float>();

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (activeEnemies.Count == 0) return;

            // Update attack coordination
            UpdateAttackCoordination();

            // Update positioning coordination
            if (enableFlankingBehavior && Time.time - lastPositionUpdateTime >= positionUpdateInterval)
            {
                UpdatePositionCoordination();
                lastPositionUpdateTime = Time.time;
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

        #region Enemy Registration

        /// <summary>
        /// Register an enemy with the manager
        /// </summary>
        public void RegisterEnemy(EnemyBase enemy)
        {
            if (enemy == null || activeEnemies.Contains(enemy)) return;

            activeEnemies.Add(enemy);

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

                if (enemies.Count < 2) continue; // No need to coordinate single enemies

                // Calculate flanking positions around the target
                AssignFlankingPositions(enemies, target);
            }
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

