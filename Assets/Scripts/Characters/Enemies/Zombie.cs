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

        // Base zombie class - distance management handled by derived classes

        private float currentAnimSpeed = 1f;

        protected override void Awake()
        {
            useRootMotion = true; // Enable root motion for the zombie
            base.Awake();
            originalSpeed = agent.speed; // Store original speed
            
            // Base zombie - derived classes handle their own distance settings
            
            Debug.Log($"[{gameObject.name}] Zombie base class initialized");
        }

        protected override void Update()
        {
            if (Health <= 0) return;

            base.Update(); // Call base Update to handle destination setting

            if (navMeshTarget == null) return;

            // Base zombie class - derived classes handle their own attack logic
            HandleGenericAttackLogic();
        }

        /// <summary>
        /// Generic attack logic for zombie types that don't override Update
        /// </summary>
        protected virtual void HandleGenericAttackLogic()
        {
            if (Health <= 0) return;

            float distanceToTarget = Vector3.Distance(transform.position, navMeshTarget.position);
            float effectiveAttackDistance = NavigationUtils.CalculateEffectiveReachDistance(transform.position, navMeshTarget, attackRange, obstacleBoundsOffset);

            // Check for attack conditions using generic attackRange
            if (distanceToTarget <= effectiveAttackDistance && 
                !isAttacking && 
                Time.time >= lastAttackTime + attackCooldown)
            {
                // Phase 1: Rotation phase - rotate towards target until properly aligned
                if (!IsReadyToAttack())
                {
                    isRotatingToAttack = true;
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
            else
            {
                // Stop rotation phase if conditions not met
                isRotatingToAttack = false;
            }
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

        protected override void EndAttack()
        {
            base.EndAttack();
            
            // Resume movement after attack
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                
                // For non-root motion, reset to original speed
                if (!useRootMotion)
                {
                    agent.speed = originalSpeed;
                }
            }
        }

    }
}
