using UnityEngine;
using UnityEngine.AI;
using System.Collections;

namespace Enemies
{
    public class Zombie : EnemyBase
    {
        #region Constants
        
        private const float MELEE_ATTACK_ANGLE_THRESHOLD = 30f;
        
        #endregion
        
        [Header("Attack Settings")]
        [SerializeField] protected float attackRange = 2f; // Keep reasonable for NPC vs zombie combat
        [SerializeField] protected float attackCooldown = 1.5f;
        [SerializeField] protected float approachDistance = 3f; // Distance at which to start slowing down
        protected float lastAttackTime;
        protected float originalSpeed;

        protected override void Awake()
        {
            useRootMotion = true; // Enable root motion for the zombie
            base.Awake();
            originalSpeed = agent.speed; // Store original speed
        }

        protected override void Update()
        {
            base.Update(); // Call base Update to handle destination setting

            if (navMeshTarget == null) return;

            // Cache distance calculation to avoid repeated Vector3.Distance calls
            float distanceToTarget = Vector3.Distance(transform.position, navMeshTarget.position);
            float effectiveAttackDistance = NavigationUtils.CalculateEffectiveReachDistance(transform.position, navMeshTarget, stoppingDistance, obstacleBoundsOffset);
            
            // Debug distance and attack state
            Debug.DrawLine(transform.position, navMeshTarget.position, Color.yellow);

            // Handle movement speed adjustment for non-root motion
            HandleMovementSpeed(distanceToTarget, effectiveAttackDistance);

            // Handle attack logic
            HandleAttackLogic(distanceToTarget, effectiveAttackDistance);
        }

        /// <summary>
        /// Handles movement speed adjustment for non-root motion zombies based on distance to target
        /// </summary>
        private void HandleMovementSpeed(float distanceToTarget, float effectiveAttackDistance)
        {
            // For root motion, we don't manually adjust agent speed - the animation drives movement
            // The agent handles pathfinding and turning, animation handles forward movement
            // Also stop movement during rotation-to-attack phase and during attacks
            if (useRootMotion || isAttacking || isRotatingToAttack) return;

            if (distanceToTarget <= effectiveAttackDistance)
            {
                // Stop completely when in attack range or preparing to attack
                agent.speed = 0f;
            }
            else if (distanceToTarget <= approachDistance)
            {
                // Slow down when approaching attack range
                float speedFactor = (distanceToTarget - effectiveAttackDistance) / (approachDistance - effectiveAttackDistance);
                agent.speed = originalSpeed * speedFactor;
            }
            else
            {
                // Normal speed when far away
                agent.speed = originalSpeed;
            }
        }

        /// <summary>
        /// Handles attack logic with proper rotation-before-attack behavior
        /// </summary>
        private void HandleAttackLogic(float distanceToTarget, float effectiveAttackDistance)
        {
            // Check for attack range and cooldown
            if (distanceToTarget > effectiveAttackDistance || isAttacking || Time.time < lastAttackTime + attackCooldown)
            {
                isRotatingToAttack = false; // Stop rotation phase if out of range
                return;
            }

            // Phase 1: Rotation phase - rotate towards target until properly aligned
            if (!IsReadyToAttack())
            {
                isRotatingToAttack = true;
                
                // Rotate towards target (works for both root motion and non-root motion)
                RotateTowardsTargetForAttack();
                return;
            }

            // Phase 2: Attack phase - we're properly aligned, execute attack
            if (isRotatingToAttack || IsReadyToAttack())
            {
                BeginAttackSequence();
                lastAttackTime = Time.time;
                isRotatingToAttack = false;
            }
        }

        protected override void BeginAttackSequence()
        {
            base.BeginAttackSequence();
            
            // Stop movement during attack only if we're on a valid NavMesh
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
        }

        protected override void EndAttack()
        {
            base.EndAttack();
            
            // Resume movement after attack only if we're on a valid NavMesh
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                
                // For non-root motion, reset to original speed
                if (!useRootMotion)
                {
                    agent.speed = originalSpeed;
                }
                // For root motion, the animation will drive the speed
            }
        }


    }
}
