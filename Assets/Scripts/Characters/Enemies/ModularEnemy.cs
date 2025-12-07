using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Combat;
using Combat.Attacks;

namespace Enemies
{
    /// <summary>
    /// Base class for enemies that use modular attack components.
    /// Handles navigation, health, and automatically selects appropriate attacks based on distance, health, etc.
    /// 
    /// This is the foundation for all modular enemies (humanoids, drones, robots, etc.).
    /// Attach AttackBase components (AnimationAttack, ProjectileAttack, BeamAttack, etc.) to define behavior.
    /// 
    /// For humanoid enemies (zombies, robots, humanoid bosses), use HumanoidEnemy instead.
    /// For flying enemies, use Drone.
    /// </summary>
    public class ModularEnemy : EnemyBase
    {
        
        [Header("Attack System")]
        [SerializeField] protected AttackSelectionStrategy selectionStrategy = AttackSelectionStrategy.DISTANCE_BASED;
        [SerializeField] protected float attackSwitchCooldown = 0.5f;

        // Attack components and management
        private AttackBase[] attackComponents;
        private AttackBase currentAttack;
        private float lastAttackSwitchTime;
        private bool isExecutingAttack = false;
        private float attackExecutionStartTime;
        private float rotationStartTime; // Track when rotation phase started
        protected float originalSpeed;

        /// <summary>
        /// Different strategies for selecting which attack to use
        /// </summary>
        public enum AttackSelectionStrategy
        {
            PRIORITY,           // Use attacks in order of priority (first in list)
            RANDOM,             // Randomly select from available attacks
            DISTANCE_BASED,     // Select attack based on distance to target
            ROTATION_BASED      // Select attack based on rotation requirements
        }

        private float currentAnimSpeed = 1f;

        #region Attack Range Properties

        /// <summary>
        /// Get minimum attack distance (smallest minRange from all attacks)
        /// </summary>
        protected float minRange
        {
            get
            {
                if (attackComponents == null || attackComponents.Length == 0)
                    return 0f;
                
                // Get minimum range from attacks
                float min = float.MaxValue; // Start with a large value, then find the actual min
                foreach (var attack in attackComponents)
                {
                    if (attack != null && attack.minRange < min)
                    {
                        min = attack.minRange;
                    }
                }
                // Return actual min, or 0f if no attacks found
                return min < float.MaxValue ? min : 0f;
            }
        }

        /// <summary>
        /// Get maximum attack distance (largest maxRange from all attacks)
        /// </summary>
        protected float maxRange
        {
            get
            {
                if (attackComponents == null || attackComponents.Length == 0)
                {
                    if (showCollisionDebug && Time.frameCount % 120 == 0)
                    {
                        Debug.Log($"[{gameObject.name}] maxRange property: attackComponents is null or empty, returning 0");
                    }
                    return 0f; // Return 0 instead of 10, so drones with uninitialized attacks don't strafe
                }
                
                // Get maximum range from attacks
                float max = 0f;
                foreach (var attack in attackComponents)
                {
                    if (attack != null && attack.maxRange > max)
                    {
                        max = attack.maxRange;
                        if (showCollisionDebug && Time.frameCount % 120 == 0)
                        {
                            Debug.Log($"[{gameObject.name}] maxRange property: Found attack {attack.GetType().Name} with maxRange {attack.maxRange}");
                        }
                    }
                }
                
                if (showCollisionDebug && Time.frameCount % 120 == 0)
                {
                    Debug.Log($"[{gameObject.name}] maxRange property: Returning {max} from {attackComponents.Length} components");
                }
                
                // If no max found, return 0 (not initialized yet)
                return max;
            }
        }

        #endregion

        protected override void Awake()
        {
            // Note: useRootMotion should be set by derived classes (Zombie, Robot, Drone, etc.)
            // before calling base.Awake() to ensure proper NavMeshAgent configuration.
            // At runtime, root motion is controlled by animation events (EnableRootMotion/DisableRootMotion).
            base.Awake();
            originalSpeed = agent.speed; // Store original speed
            
            // Initialize attack components
            InitializeAttackComponents();
            
            Debug.Log($"[{gameObject.name}] ModularEnemy initialized with {attackComponents.Length} attack component(s)");
        }

        protected override void Start()
        {
            base.Start();
            
            // Initialize all attack components
            foreach (var attack in attackComponents)
            {
                if (attack != null)
                {
                    attack.Initialize(this);
                }
            }
        }

        private void InitializeAttackComponents()
        {
            // Find all attack components on this GameObject
            attackComponents = GetComponents<AttackBase>();
            
            if (attackComponents.Length == 0)
            {
                Debug.LogWarning($"[{gameObject.name}] No attack components found. Add attack components like CloseRangeAttack, ProjectileAttack, etc.");
            }
        }

        protected override void Update()
        {
            if (Health <= 0) return;

            base.Update(); // Call base Update to handle destination setting

            if (navMeshTarget == null) return;

            // Handle modular attack logic
            if (attackComponents.Length > 0)
            {
                HandleModularAttackLogic();
            }
        }

        /// <summary>
        /// Handle attack selection and execution using modular attack components
        /// </summary>
        private void HandleModularAttackLogic()
        {
            // Auto-reset rotation if it's been stuck too long or conditions changed
            // FIX: This prevents enemies from getting stuck in infinite rotation loops
            // which would cause animator.speed to stay at 0 forever
            if (isRotatingToAttack)
            {
                bool shouldAbandonRotation = false;
                string abandonReason = "";
                
                float timeSinceRotationStart = Time.time - rotationStartTime;
                
                // Check timeout (2 seconds)
                if (timeSinceRotationStart > 2.0f)
                {
                    shouldAbandonRotation = true;
                    abandonReason = $"timeout after {timeSinceRotationStart:F2}s";
                }
                // Check if target moved too far or angle is extreme (check every frame during rotation)
                else if (navMeshTarget != null && currentAttack != null)
                {
                    float distanceToTarget = Vector3.Distance(transform.position, navMeshTarget.position);
                    Vector3 directionToTarget = (navMeshTarget.position - transform.position).normalized;
                    directionToTarget.y = 0;
                    float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
                    
                    float attackMaxRange = currentAttack.maxRange;
                    float maxRotationDistance = attackMaxRange + 5f;
                    const float MAX_ROTATION_ANGLE = 90f;
                    
                    if (distanceToTarget > maxRotationDistance)
                    {
                        shouldAbandonRotation = true;
                        abandonReason = $"target too far ({distanceToTarget:F1} > {maxRotationDistance:F1})";
                    }
                    else if (angleToTarget > MAX_ROTATION_ANGLE)
                    {
                        shouldAbandonRotation = true;
                        abandonReason = $"angle too extreme ({angleToTarget:F1}° > {MAX_ROTATION_ANGLE}°)";
                    }
                }
                
                // Abandon rotation if any condition met
                if (shouldAbandonRotation)
                {
                    Debug.LogWarning($"[{gameObject.name}] Abandoning rotation: {abandonReason} - resuming movement");
                    isRotatingToAttack = false;
                    
                    // Resume movement
                    if (agent != null && agent.isOnNavMesh)
                    {
                        agent.isStopped = false;
                    }
                }
            }
            
            // Auto-reset isExecutingAttack if it's been too long (fallback for missing animation events)
            if (isExecutingAttack)
            {
                float timeSinceAttackStart = Time.time - attackExecutionStartTime;
                if (timeSinceAttackStart > 3.0f) // 3 second timeout for attack execution
                {
                    Debug.Log($"[{gameObject.name}] Auto-resetting stuck attack execution after timeout ({timeSinceAttackStart:F2}s)");
                    isExecutingAttack = false;
                    if (currentAttack != null)
                    {
                        currentAttack.OnAttackEnd();
                        currentAttack = null;
                    }
                    if (HasValidAnimator())
                    {
                        animator.SetInteger(GameConstants.AnimatorParams.AttackTypeHash, 0);
                    }
                }
                else
                {
                    // Update current attack during execution (allows for target tracking if enabled)
                    if (currentAttack != null)
                    {
                        currentAttack.UpdateDuringAttack();
                    }
                    
                    // For beam attacks, continue rotating towards target during attack
                    if (currentAttack is BeamAttack beamAttack)
                    {
                        // Use the generic rotation method from AttackBase
                        NavigationUtils.RotateTowardsTargetForAction(transform, navMeshTarget, rotationSpeed, 2f, beamAttack.attackAngleThreshold, true);
                    }
                    
                    return;
                }
            }

            // Don't switch attacks too frequently
            if (Time.time - lastAttackSwitchTime < attackSwitchCooldown && currentAttack != null)
            {
                return;
            }

            // Update target for all attacks
            UpdateAttackTargets();

            // Find available attacks
            var availableAttacks = GetAvailableAttacks();
            
            if (availableAttacks.Count == 0)
            {
                currentAttack = null;
                return;
            }

            // Select attack based on strategy
            AttackBase selectedAttack = SelectAttack(availableAttacks);
            
            if (selectedAttack != currentAttack)
            {
                // Switch to new attack
                if (currentAttack != null)
                {
                    currentAttack.OnAttackEnd();
                }
                
                currentAttack = selectedAttack;
                lastAttackSwitchTime = Time.time;
                
                Debug.Log($"[{gameObject.name}] Switched to attack: {currentAttack.GetType().Name}");
            }

            // Execute current attack
            if (currentAttack != null)
            {
                if (showCollisionDebug)
                {
                    Debug.Log($"[{gameObject.name}] Executing attack: {currentAttack.GetType().Name} | Distance: {Vector3.Distance(transform.position, navMeshTarget.position):F2} | NeedsRotation: {currentAttack.ShouldRotateToAttack()}");
                }
                ExecuteAttack(currentAttack);
            }
        }

        /// <summary>
        /// Update target reference for all attack components
        /// </summary>
        private void UpdateAttackTargets()
        {
            foreach (var attack in attackComponents)
            {
                if (attack != null)
                {
                    attack.UpdateTarget(navMeshTarget);
                }
            }
        }

        /// <summary>
        /// Get list of attacks that can currently be used
        /// </summary>
        private List<AttackBase> GetAvailableAttacks()
        {
            var available = new List<AttackBase>();
            
            foreach (var attack in attackComponents)
            {
                if (attack != null && attack.enabled)
                {
                    bool canAttack = attack.CanAttack();
                    if (showCollisionDebug && !canAttack)
                    {
                        Debug.Log($"[{gameObject.name}] Attack {attack.GetType().Name} not available - CanAttack: {canAttack}");
                    }
                    
                    if (canAttack)
                    {
                        available.Add(attack);
                    }
                }
            }
            
            if (showCollisionDebug)
            {
                Debug.Log($"[{gameObject.name}] Available attacks: {available.Count}/{attackComponents.Length}");
            }
            
            return available;
        }

        /// <summary>
        /// Select an attack based on the current strategy
        /// </summary>
        private AttackBase SelectAttack(List<AttackBase> availableAttacks)
        {
            if (availableAttacks.Count == 0) return null;
            if (availableAttacks.Count == 1) return availableAttacks[0];

            switch (selectionStrategy)
            {
                case AttackSelectionStrategy.PRIORITY:
                    return availableAttacks[0];
                    
                case AttackSelectionStrategy.RANDOM:
                    int randomIndex = Random.Range(0, availableAttacks.Count);
                    return availableAttacks[randomIndex];
                    
                case AttackSelectionStrategy.DISTANCE_BASED:
                    return SelectByDistance(availableAttacks);
                    
                case AttackSelectionStrategy.ROTATION_BASED:
                    return SelectByRotation(availableAttacks);
                    
                default:
                    return availableAttacks[0];
            }
        }

        /// <summary>
        /// Select attack based on distance to target
        /// </summary>
        private AttackBase SelectByDistance(List<AttackBase> availableAttacks)
        {
            if (navMeshTarget == null) return availableAttacks[0];
            
            float distanceToTarget = Vector3.Distance(transform.position, navMeshTarget.position);
            
            // Find the attack with range closest to current distance
            AttackBase bestAttack = null;
            float bestDifference = float.MaxValue;
            
            foreach (var attack in availableAttacks)
            {
                float attackRange = attack.GetCurrentAttackRange();
                float difference = Mathf.Abs(attackRange - distanceToTarget);
                
                if (difference < bestDifference)
                {
                    bestDifference = difference;
                    bestAttack = attack;
                }
            }
            
            return bestAttack ?? availableAttacks[0];
        }

        /// <summary>
        /// Select attack based on rotation requirements
        /// </summary>
        private AttackBase SelectByRotation(List<AttackBase> availableAttacks)
        {
            // Prefer attacks that don't require rotation
            var noRotationAttacks = availableAttacks.Where(a => !a.ShouldRotateToAttack()).ToList();
            if (noRotationAttacks.Count > 0)
            {
                return noRotationAttacks[0];
            }
            
            // If all require rotation, use the first one
            return availableAttacks[0];
        }

        /// <summary>
        /// Execute the selected attack
        /// </summary>
        private void ExecuteAttack(AttackBase attack)
        {
            if (attack == null) return;
            
            // Check if attack needs rotation first
            bool needsRotation = attack.ShouldRotateToAttack();
            Debug.Log($"[{gameObject.name}] ExecuteAttack | NeedsRotation: {needsRotation} | Distance: {(navMeshTarget != null ? Vector3.Distance(transform.position, navMeshTarget.position).ToString("F2") : "N/A")}");
            
            if (needsRotation)
            {
                // Set rotation state flag for animation system (if not already set)
                // FIX: This flag tells UpdateAnimationParameters() to maintain minimum speed
                // even when agent.velocity is zero, preventing animator freeze during rotation
                if (!isRotatingToAttack)
                {
                    isRotatingToAttack = true;
                    rotationStartTime = Time.time; // Start rotation timer for timeout safety
                }
                
                // SMART ROTATION ABANDONMENT:
                // Check if target has moved too far or angle is too extreme - if so, give up rotation and resume movement
                float distanceToTarget = Vector3.Distance(transform.position, navMeshTarget.position);
                Vector3 directionToTarget = (navMeshTarget.position - transform.position).normalized;
                directionToTarget.y = 0; // Flatten to horizontal plane
                float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
                
                // Get attack's max range for distance check
                float attackMaxRange = attack.maxRange;
                float maxRotationDistance = attackMaxRange + 5f; // Allow 5 units beyond max range
                const float MAX_ROTATION_ANGLE = 90f; // If target is more than 90 degrees off, just move instead
                
                // If target is too far or angle is too extreme, abandon rotation and resume movement
                if (distanceToTarget > maxRotationDistance || angleToTarget > MAX_ROTATION_ANGLE)
                {
                    Debug.Log($"[{gameObject.name}] Abandoning rotation - Distance: {distanceToTarget:F1} > {maxRotationDistance:F1} OR Angle: {angleToTarget:F1}° > {MAX_ROTATION_ANGLE}° - resuming movement");
                    isRotatingToAttack = false;
                    
                    // Resume movement towards target
                    if (agent != null && agent.isOnNavMesh)
                    {
                        agent.isStopped = false;
                    }
                    return; // Exit and let normal movement pathfinding take over
                }
                
                // Hard-stop movement while rotating so we don't keep circling
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                    agent.updateRotation = true;
                }

                // Handle rotation for specific attack types
                if (attack is BeamAttack beamAttack)
                {
                    // Use the generic rotation method from AttackBase
                    NavigationUtils.RotateTowardsTargetForAction(transform, navMeshTarget, rotationSpeed, 2f, beamAttack.attackAngleThreshold, true);
                }
                else
                {
                    // Use generic rotation
                    bool rotationComplete = RotateTowardsTargetForAttack();
                    Debug.Log($"[{gameObject.name}] Rotating towards target | Complete: {rotationComplete}");
                }
                
                // After rotation, check if we're now ready to attack
                if (!attack.ShouldRotateToAttack())
                {
                    Debug.Log($"[{gameObject.name}] Rotation complete, requesting attack permission");
                    
                    // Clear rotation flag
                    isRotatingToAttack = false;
                    
                    // Rotation complete, now try to execute the attack
                    // Request permission from EnemyManager before attacking
                    if (Managers.EnemyManager.Instance != null)
                    {
                        if (!Managers.EnemyManager.Instance.RequestAttackPermission(this))
                        {
                            Debug.Log($"[{gameObject.name}] Attack permission DENIED");
                            // Permission denied, keep circling and try again next frame
                            return;
                        }
                    }
                    
                    Debug.Log($"[{gameObject.name}] Attack permission GRANTED, executing attack");
                    
                    // Permission granted, execute attack
                    isExecutingAttack = true;
                    attackExecutionStartTime = Time.time;
                    Debug.Log($"[{gameObject.name}] 🎬 StartAttack() called | AttackType: {attack.GetType().Name}");
                    attack.StartAttack();
                    
                    // For enemies without valid animators (like drones) execute immediately
                    if (attack.ShouldExecuteImmediately())
                    {
                        attack.OnAttack();
                        // AttackEnd() will call attack.OnAttackEnd() and do all cleanup
                        AttackEnd();
                        return;
                    }
                    
                    // Update base class attack time for cooldown movement system
                    lastAttackTime = Time.time;
                }
                else
                {
                    Debug.Log($"[{gameObject.name}] Still needs rotation, trying again next frame");
                }
                // If still need rotation, we'll try again next frame
                return;
            }
            
            // No rotation needed, request permission and execute
            Debug.Log($"[{gameObject.name}] No rotation needed, requesting attack permission");
            
            if (Managers.EnemyManager.Instance != null)
            {
                if (!Managers.EnemyManager.Instance.RequestAttackPermission(this))
                {
                    Debug.Log($"[{gameObject.name}] Attack permission DENIED");
                    // Permission denied, keep circling and try again next frame
                    return;
                }
            }
            
            Debug.Log($"[{gameObject.name}] Attack permission GRANTED, executing attack");
            
            // Execute the attack (no rotation needed)
            isExecutingAttack = true;
            attackExecutionStartTime = Time.time;
            Debug.Log($"[{gameObject.name}] 🎬 StartAttack() called | AttackType: {attack.GetType().Name} | HasAnimator: {HasValidAnimator()} | UseAnimatorTiming: {attack.useAnimatorTiming}");
            attack.StartAttack();
            
            // For enemies without valid animators (like drones) execute immediately
            if (attack.ShouldExecuteImmediately())
            {
                attack.OnAttack();
                // AttackEnd() will call attack.OnAttackEnd() and do all cleanup
                AttackEnd();
                return;
            }
            
            // Update base class attack time for cooldown movement system
            lastAttackTime = Time.time;
        }


        protected override void BeginAttackSequence()
        {
            base.BeginAttackSequence();
            
            // Stop movement during attack
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
        }


        /// <summary>
        /// Called by animator events to trigger attack damage
        /// STANDARDIZED: Animation events should call this method on the damage frame
        /// </summary>
        public override void Attack()
        {
            Debug.Log($"[{gameObject.name}] 🎯 Attack() called by animation event | CurrentAttack: {(currentAttack != null ? currentAttack.GetType().Name : "NULL")}");
            if (currentAttack != null)
            {
                currentAttack.OnAttack();
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] Attack called but no current attack is active");
            }
        }

        /// <summary>
        /// Called by animator events to end attack (if animation events are set up)
        /// STANDARDIZED: Animation events should call this method at the end of the attack animation
        /// This is the public interface method from IAttackOwner
        /// Also called internally for immediate execution attacks (no animation events)
        /// </summary>
        public override void AttackEnd()
        {
            Debug.Log($"[{gameObject.name}] 🏁 AttackEnd() called | CurrentAttack: {(currentAttack != null ? currentAttack.GetType().Name : "NULL")}");
            
            // End the current attack component (if not already ended)
            if (currentAttack != null)
            {
                currentAttack.OnAttackEnd();
                currentAttack = null;
            }
            
            // Reset attack type to 0 (default)
            if (HasValidAnimator())
            {
                animator.SetInteger(GameConstants.AnimatorParams.AttackTypeHash, 0);
                animator.SetBool(GameConstants.AnimatorParams.AttackHash, false);
            }
            
            // Reset attack state
            isAttacking = false;
            isRotatingToAttack = false;
            isExecutingAttack = false;
            
            // Resume rotation after attack
            if (IsUsingRootMotion && agent != null)
            {
                agent.updateRotation = true;
            }
            
            // Notify EnemyManager that attack is complete
            if (Managers.EnemyManager.Instance != null)
            {
                Managers.EnemyManager.Instance.NotifyAttackComplete(this);
            }
            
            // Resume movement after attack
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                
                // For non-root motion, reset to original speed
                if (!IsUsingRootMotion)
                {
                    agent.speed = originalSpeed;
                }
            }
        }

        /// <summary>
        /// Set the attacking state (used by attack components)
        /// </summary>
        /// <param name="attacking">Whether the enemy is attacking</param>
        public void SetAttacking(bool attacking)
        {
            isAttacking = attacking;
            
            // Update lastAttackTime when starting an attack
            if (attacking)
            {
                lastAttackTime = Time.time;
            }
        }


        /// <summary>
        /// Calculate the optimal stopping distance based on available attacks.
        /// This ensures the enemy doesn't get closer than their closest attack range.
        /// </summary>
        /// <returns>Optimal stopping distance from target</returns>
        protected override float CalculateOptimalStoppingDistance()
        {
            if (attackComponents == null || attackComponents.Length == 0)
            {
                return base.CalculateOptimalStoppingDistance(); // Fall back to default
            }

            float closestAttackRange = float.MaxValue;
            bool foundAnyAttacks = false;

            // Find the closest attack range among all available attacks
            foreach (var attack in attackComponents)
            {
                if (attack != null && attack.enabled)
                {
                    float attackRange = attack.GetCurrentAttackRange();
                    if (attackRange > 0 && attackRange < closestAttackRange)
                    {
                        closestAttackRange = attackRange;
                        foundAnyAttacks = true;
                    }
                }
            }

            // If no attacks found, use default stopping distance
            if (!foundAnyAttacks)
            {
                return base.CalculateOptimalStoppingDistance();
            }

            // Return the closest attack range as the optimal stopping distance
            // This ensures the enemy doesn't get closer than its minimum attack range
            return closestAttackRange;
        }

        /// <summary>
        /// Get the minimum distance at which the enemy can attack.
        /// For enemies with ranged attacks, this returns the minimum attack distance.
        /// </summary>
        /// <returns>Minimum attack distance (0 if can attack at any close distance)</returns>
        protected override float GetMinimumAttackDistance()
        {
            if (attackComponents == null || attackComponents.Length == 0)
            {
                return 0f;
            }

            float minDistance = 0f;

            // Check all attack components for minimum attack distance
            foreach (var attack in attackComponents)
            {
                if (attack != null && attack.enabled)
                {
                    // Check if this is a projectile attack with a minimum distance
                    if (attack is ProjectileAttack projectileAttack)
                    {
                        minDistance = Mathf.Max(minDistance, projectileAttack.minRange);
                        if (showCollisionDebug)
                        {
                            Debug.Log($"[{gameObject.name}] ProjectileAttack minRange: {projectileAttack.minRange}");
                        }
                    }
                    // Melee attacks have no minimum (can attack at 0 distance)
                    // Laser attacks have no minimum
                    // Boomer attacks have no minimum
                }
            }

            if (showCollisionDebug && minDistance > 0)
            {
                Debug.Log($"[{gameObject.name}] GetMinimumAttackDistance returning: {minDistance}");
            }

            return minDistance;
        }

        /// <summary>
        /// Get attack component of specific type
        /// </summary>
        /// <typeparam name="T">Type of attack component</typeparam>
        /// <returns>Attack component of specified type</returns>
        public T GetAttackComponent<T>() where T : AttackBase
        {
            foreach (var attack in attackComponents)
            {
                if (attack is T)
                {
                    return attack as T;
                }
            }
            return default(T);
        }

        /// <summary>
        /// Get the currently active attack
        /// </summary>
        /// <returns>Current attack component</returns>
        public AttackBase GetCurrentAttack()
        {
            return currentAttack;
        }

        /// <summary>
        /// Override to provide current attack for IK forwarding
        /// </summary>
        protected override AttackBase GetCurrentAttackForIK()
        {
            return currentAttack;
        }

        /// <summary>
        /// Check if any attack is currently active
        /// </summary>
        /// <returns>True if an attack is active</returns>
        public bool IsAttacking()
        {
            return currentAttack != null;
        }

        /// <summary>
        /// Force end all attacks
        /// </summary>
        public void ForceEndAllAttacks()
        {
            if (currentAttack != null)
            {
                currentAttack.OnAttackEnd();
                currentAttack = null;
            }
        }

        /// <summary>
        /// Called by Unity for IK (Inverse Kinematics) updates
        /// Handles attack-specific IK behavior for modular attacks
        /// </summary>
        protected override void OnAnimatorIK(int layerIndex)
        {
            // Always call base first - it handles hit reactions with proper priority
            base.OnAnimatorIK(layerIndex);
            
            // Forward IK to current attack if actively executing
            if (currentAttack != null && isExecutingAttack)
            {
                currentAttack.OnAnimatorIK(layerIndex);
            }
        }

    }
}
