using UnityEngine;
using UnityEngine.AI;
using Managers;

namespace Enemies
{
    public class RangedZombie : Zombie
    {
        #region Constants
        
        // Use the attack-ready threshold from base class for validation  
        private const float RANGED_ATTACK_ANGLE_THRESHOLD = 5f; // Now using stricter threshold
        private const float PROJECTILE_SPAWN_HEIGHT = 1.5f;
        
        #endregion
        
        [Header("Ranged Attack Settings")]
        [SerializeField] protected float rangedDamage = 15f;
        [SerializeField] protected float shootCooldown = 2f;
        [SerializeField] protected float minAttackRange = 5f;
        [SerializeField] protected float maxAttackRange = 15f;
        protected float lastShootTime;

        [Header("Vomit Effects")]
        [SerializeField] private EffectDefinition vomitProjectileEffect;
        [SerializeField] private EffectDefinition vomitPoolEffect;

        protected override void Awake()
        {
            base.Awake();
            // Override attack range for ranged zombie
            attackRange = maxAttackRange;

            // Validate effects
            if (vomitProjectileEffect == null)
            {
                Debug.LogError("Vomit projectile effect definition is not assigned to RangedZombie on " + gameObject.name);
            }
            if (vomitPoolEffect == null)
            {
                Debug.LogError("Vomit pool effect definition is not assigned to RangedZombie on " + gameObject.name);
            }
        }

        protected override void Update()
        {
            base.Update();

            if (navMeshTarget == null) return;

            float distanceToTarget = Vector3.Distance(transform.position, navMeshTarget.position);

            // Handle rotation for non-root motion - but only when not attacking
            if (!useRootMotion && !isAttacking && !isRotatingToAttack)
            {
                HandleRangedRotation();
            }

            // Only attack if within range and cooldown is ready
            if (distanceToTarget >= minAttackRange && 
                distanceToTarget <= maxAttackRange && 
                !isAttacking && 
                Time.time >= lastShootTime + shootCooldown)
            {
                // Phase 1: Rotation phase - rotate towards target until properly aligned
                if (!IsReadyToAttack())
                {
                    isRotatingToAttack = true;
                    
                    // Use faster rotation for attack preparation
                    RotateTowardsTargetForAttack();
                    return;
                }

                // Phase 2: Attack phase - we're properly aligned, execute attack
                if (isRotatingToAttack || IsReadyToAttack())
                {
                    BeginAttackSequence();
                    lastShootTime = Time.time;
                    isRotatingToAttack = false;
                }
            }
            else
            {
                // Stop rotation phase if out of range or on cooldown
                isRotatingToAttack = false;
            }
        }

        // Called by animation event
        public void RangedAttack()
        {
            // Don't attack if dead
            if (Health <= 0) return;
            // Validate attack using base class method
            if (!ValidateAttack(maxAttackRange, RANGED_ATTACK_ANGLE_THRESHOLD, out float distanceToTarget, out float angleToTarget))
            {
                if (navMeshTarget == null)
                {
                    Debug.Log($"[RangedZombie] {gameObject.name}: No target - aborting attack");
                }
                else if (!IsTargetStillValid(navMeshTarget))
                {
                    Debug.Log($"[RangedZombie] {gameObject.name}: Target {navMeshTarget.name} invalid - aborting attack");
                }
                else
                {
                    Debug.Log($"[RangedZombie] {gameObject.name} → {navMeshTarget.name}: " +
                             $"Dist={distanceToTarget:F1}, Angle={angleToTarget:F1}° VALIDATION_FAIL");
                }
                return;
            }

            // Additional range check for ranged attacks (min distance)
            if (distanceToTarget < minAttackRange)
            {
                Debug.Log($"[RangedZombie] {gameObject.name} → {navMeshTarget.name}: " +
                         $"Dist={distanceToTarget:F1} < {minAttackRange:F1} TOO_CLOSE");
                return;
            }

            // Calculate direction to target
            Vector3 direction = (navMeshTarget.position - transform.position).normalized;
            
            // Play the vomit projectile effect and get the spawned GameObject
            GameObject projectileObj = EffectManager.Instance.PlayEffect(
                transform.position + Vector3.up * PROJECTILE_SPAWN_HEIGHT, // Spawn slightly above the zombie
                direction,
                Quaternion.LookRotation(direction),
                null,
                vomitProjectileEffect
            );

            // Initialize the projectile movement
            if (projectileObj != null)
            {
                ZombieVomitProjectile projectile = projectileObj.GetComponent<ZombieVomitProjectile>();
                if (projectile != null)
                {
                    projectile.Initialize(navMeshTarget.position, rangedDamage, vomitPoolEffect);
                }
                else
                {
                    projectile = projectileObj.AddComponent<ZombieVomitProjectile>();
                    projectile.Initialize(navMeshTarget.position, rangedDamage, vomitPoolEffect);
                    Debug.LogWarning("ZombieVomitProjectile component not found on spawned projectile, added it to the projectile object");
                }
            }
            
            Debug.Log("Ranged zombie fired projectile");
        }

        /// <summary>
        /// Handles basic rotation logic for movement (not attack preparation)
        /// </summary>
        private void HandleRangedRotation()
        {
            // Don't rotate if dead
            if (Health <= 0) 
            {
                Debug.Log($"[{gameObject.name}] HandleRangedRotation called while dead! Health: {Health}");
                return;
            }
            
            // Simple rotation towards target during normal movement
            Vector3 direction = (navMeshTarget.position - transform.position).normalized;
            direction.y = 0;
            if (direction == Vector3.zero) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
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
                // For root motion, the animation will drive the speed
                // For non-root motion, speed is already set in the base class
            }
        }
    }
}
