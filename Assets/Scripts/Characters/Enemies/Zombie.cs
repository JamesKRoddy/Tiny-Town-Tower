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

        [Header("Backstep Settings")]
        [SerializeField] protected bool enableBackstep = true;
        [SerializeField] protected float backstepTriggerDistance = 1.0f; // Distance that triggers backstep
        [SerializeField] protected float backstepTargetDistance = 1.5f; // Distance to backstep to
        [SerializeField] protected float backstepSpeed = -1f; // Negative speed for backstep animation

        protected override void Awake()
        {
            useRootMotion = true; // Enable root motion for the zombie
            base.Awake();
            originalSpeed = agent.speed; // Store original speed
        }

        protected override void Update()
        {
            if (Health <= 0) return;

            base.Update(); // Call base Update to handle destination setting

            if (navMeshTarget == null) return;

            // Cache distance calculation to avoid repeated Vector3.Distance calls
            float distanceToTarget = Vector3.Distance(transform.position, navMeshTarget.position);
            float effectiveAttackDistance = NavigationUtils.CalculateEffectiveReachDistance(transform.position, navMeshTarget, stoppingDistance, obstacleBoundsOffset);
            
            // Debug distance and attack state
            Debug.DrawLine(transform.position, navMeshTarget.position, Color.yellow);

            // Simple backstep logic
            if (enableBackstep && !isAttacking)
            {
                if (distanceToTarget < backstepTriggerDistance)
                {
                    // Too close - backstep
                    if (animator != null)
                    {
                        animator.SetFloat("Speed", backstepSpeed);
                    }
                    return; // Skip attack logic while backstepping
                }
                else if (distanceToTarget >= backstepTargetDistance)
                {
                    // Far enough away - resume normal speed
                    if (animator != null)
                    {
                        animator.SetFloat("Speed", 1f);
                    }
                }
            }

            // Handle attack logic
            HandleAttackLogic(distanceToTarget, effectiveAttackDistance);
        }

        /// <summary>
        /// Handles attack logic with proper rotation-before-attack behavior
        /// </summary>
        private void HandleAttackLogic(float distanceToTarget, float effectiveAttackDistance)
        {
            if (Health <= 0) return;

            // Check for attack range and cooldown
            if (distanceToTarget > effectiveAttackDistance || isAttacking || Time.time < lastAttackTime + attackCooldown)
            {
                isRotatingToAttack = false; // Stop rotation phase if out of range
                return;
            }

            // Prevent getting too close to the target (causes spinning during attack animation)
            float minDistance = 1.0f; // Minimum distance to maintain
            if (distanceToTarget < minDistance)
            {
                // Move away from target slightly
                Vector3 directionFromTarget = (transform.position - navMeshTarget.position).normalized;
                Vector3 targetPosition = navMeshTarget.position + directionFromTarget * minDistance;
                
                // Set destination to move away
                agent.isStopped = false;
                agent.SetDestination(targetPosition);
                isRotatingToAttack = false;
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
            
            // Ensure we're not too close to the target before starting attack
            if (navMeshTarget != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, navMeshTarget.position);
                float minAttackDistance = 1.2f; // Slightly more than the minimum distance
                
                if (distanceToTarget < minAttackDistance)
                {
                    // Move away slightly to prevent overlap during attack
                    Vector3 directionFromTarget = (transform.position - navMeshTarget.position).normalized;
                    Vector3 targetPosition = navMeshTarget.position + directionFromTarget * minAttackDistance;
                    
                    // Only move if we can find a valid NavMesh position
                    if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                    {
                        transform.position = hit.position;
                        agent.nextPosition = hit.position;
                    }
                }
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

        /// <summary>
        /// Override collision detection to allow backstep movement
        /// </summary>
        protected override Vector3 IsRootMotionCollisionSafe(Vector3 rootMotion, out bool collisionDetected)
        {
            // If we're backstepping (negative speed), allow movement away from target
            if (animator != null && animator.GetFloat("Speed") < 0)
            {
                // Check if we're moving away from the target
                if (navMeshTarget != null)
                {
                    Vector3 directionToTarget = (navMeshTarget.position - transform.position).normalized;
                    float dotProduct = Vector3.Dot(rootMotion.normalized, directionToTarget);
                    
                    // If moving away from target (negative dot product), allow movement
                    if (dotProduct < 0)
                    {
                        collisionDetected = false;
                        return rootMotion;
                    }
                }
            }
            
            // Use base collision detection for all other cases
            return base.IsRootMotionCollisionSafe(rootMotion, out collisionDetected);
        }
    }
}
