using UnityEngine;

namespace Enemies
{
    public class RangedZombie : Zombie
    {
        [Header("Ranged Attack Settings")]
        [SerializeField] protected GameObject projectilePrefab;
        [SerializeField] protected GameObject vomitPoolPrefab; // Reference to the vomit pool prefab
        [SerializeField] protected float rangedDamage = 15f;
        [SerializeField] protected float shootCooldown = 2f;
        [SerializeField] protected float minAttackRange = 5f; // Minimum distance to start attacking
        [SerializeField] protected float maxAttackRange = 15f; // Maximum effective range
        [SerializeField] protected float attackRotationSpeed = 2f; // Slower rotation during attack
        [SerializeField] protected float postAttackRotationPause = 0.5f; // How long to pause rotation after attack
        protected float lastShootTime;
        protected float rotationPauseEndTime;

        protected override void Awake()
        {
            base.Awake();
            // Override attack range for ranged zombie
            attackRange = maxAttackRange;

            // Validate prefabs
            if (projectilePrefab == null)
            {
                Debug.LogError("Projectile prefab not assigned to RangedZombie on " + gameObject.name);
            }
            if (vomitPoolPrefab == null)
            {
                Debug.LogError("Vomit pool prefab not assigned to RangedZombie on " + gameObject.name);
            }
        }

        protected override void Update()
        {
            base.Update();

            if (navMeshTarget == null) return;

            float distanceToTarget = Vector3.Distance(transform.position, navMeshTarget.position);

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
                    currentRotationSpeed = 0f; // No rotation during pause
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

            // Only attack if within range and cooldown is ready
            if (distanceToTarget >= minAttackRange && 
                distanceToTarget <= maxAttackRange && 
                !isAttacking && 
                Time.time >= lastShootTime + shootCooldown)
            {
                // Only attack if we're facing the target (within 45 degrees)
                float angleToTarget = Vector3.Angle(transform.forward, direction);
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
            
            // Spawn projectile
            GameObject projectile = Instantiate(projectilePrefab, transform.position + Vector3.up * 1.5f, Quaternion.identity);
            ZombieVomitProjectile projectileScript = projectile.GetComponent<ZombieVomitProjectile>();
            
            if (projectileScript == null)
            {
                projectileScript = projectile.AddComponent<ZombieVomitProjectile>();
            }

            // Set the vomit pool prefab
            projectileScript.vomitPoolPrefab = vomitPoolPrefab;
            
            // Initialize the projectile
            projectileScript.Initialize(direction, rangedDamage);
            
            // Start rotation pause
            rotationPauseEndTime = Time.time + postAttackRotationPause;
            
            Debug.Log("Ranged zombie fired projectile");
        }

        protected override void BeginAttackSequence()
        {
            base.BeginAttackSequence();
            
            // Stop movement during attack
            if (agent != null)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
        }

        protected override void EndAttack()
        {
            base.EndAttack();
            
            // Resume movement after attack
            if (agent != null)
            {
                agent.isStopped = false;
            }
        }
    }
}
