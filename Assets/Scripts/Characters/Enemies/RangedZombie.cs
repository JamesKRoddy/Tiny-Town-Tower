using UnityEngine;
using UnityEngine.AI;
using Managers;

namespace Enemies
{
    public class RangedZombie : Zombie
    {
        [Header("Ranged Attack Settings")]
        [SerializeField] protected float rangedDamage = 15f;
        [SerializeField] protected float shootCooldown = 2f;
        [SerializeField] protected float minAttackRange = 5f;
        [SerializeField] protected float maxAttackRange = 15f;
        [SerializeField] protected float attackRotationSpeed = 2f;
        [SerializeField] protected float postAttackRotationPause = 0.5f;
        protected float lastShootTime;
        protected float rotationPauseEndTime;

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

            // For root motion, let the agent handle most rotation automatically
            // For non-root motion, handle rotation manually with special logic for attacks
            if (!useRootMotion)
            {
                // Always try to face the target, but slower during attack and paused after firing
                Vector3 direction = (navMeshTarget.position - transform.position).normalized;
                direction.y = 0;
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    float currentRotationSpeed = 0f;

                    // Determine rotation speed based on state
                    if (Time.time < rotationPauseEndTime)
                    {
                        currentRotationSpeed = 0f;
                    }
                    else if (isAttacking)
                    {
                        currentRotationSpeed = attackRotationSpeed;
                    }
                    else
                    {
                        currentRotationSpeed = rotationSpeed;
                    }

                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, currentRotationSpeed * Time.deltaTime);
                }
            }

            // Only attack if within range and cooldown is ready
            if (distanceToTarget >= minAttackRange && 
                distanceToTarget <= maxAttackRange && 
                !isAttacking && 
                Time.time >= lastShootTime + shootCooldown)
            {
                // Check if we're facing the target (within 45 degrees)
                Vector3 directionToTarget = (navMeshTarget.position - transform.position).normalized;
                float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
                if (angleToTarget <= 45f)
                {
                    BeginAttackSequence();
                    lastShootTime = Time.time;
                }
            }
        }

        // Called by animation event
        public void RangedAttack()
        {
            if (navMeshTarget == null) return;

            // Calculate direction to target
            Vector3 direction = (navMeshTarget.position - transform.position).normalized;
            
            // Play the vomit projectile effect and get the spawned GameObject
            GameObject projectileObj = EffectManager.Instance.PlayEffect(
                transform.position + Vector3.up * 1.5f, // Spawn slightly above the zombie
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

            // Start rotation pause
            rotationPauseEndTime = Time.time + postAttackRotationPause;
            
            Debug.Log("Ranged zombie fired projectile");
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
