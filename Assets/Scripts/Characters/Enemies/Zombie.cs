using UnityEngine;
using UnityEngine.AI;
using System.Collections;

namespace Enemies
{
    public class Zombie : EnemyBase
    {
        [Header("Attack Settings")]
        [SerializeField] protected float attackRange = 2f;
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

            // Use the sophisticated distance checking that considers obstacles
            float effectiveAttackDistance = CalculateEffectiveAttackDistance(navMeshTarget);
            float distanceToTarget = Vector3.Distance(transform.position, navMeshTarget.position);
            
            // Debug distance and attack state
            Debug.DrawLine(transform.position, navMeshTarget.position, Color.yellow);

            // Adjust speed based on distance to target using effective distance
            if (!isAttacking)
            {
                if (distanceToTarget <= effectiveAttackDistance)
                {
                    // Stop completely when in attack range
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

            // Check for attack range and cooldown using effective distance
            if (distanceToTarget <= effectiveAttackDistance && !isAttacking && Time.time >= lastAttackTime + attackCooldown)
            {
                // Face the target before attacking
                Vector3 direction = (navMeshTarget.position - transform.position).normalized;
                direction.y = 0;
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }

                // Only attack if we're facing the target (within 30 degrees)
                float angleToTarget = Vector3.Angle(transform.forward, direction);
                if (angleToTarget <= 30f)
                {
                    BeginAttackSequence();
                    lastAttackTime = Time.time;
                }
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
                agent.speed = originalSpeed; // Reset to original speed
            }
        }

        protected virtual void MoveTowardsPlayer()
        {
            if (!isAttacking && navMeshTarget != null)
            {
                // The NavMeshAgent moves the zombie, but root motion from animation drives actual movement
                SetEnemyDestination(navMeshTarget.position);
            }
        }
    }
}
