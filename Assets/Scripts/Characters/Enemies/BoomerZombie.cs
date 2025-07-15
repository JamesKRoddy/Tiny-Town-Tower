using UnityEngine;
using UnityEngine.AI;
using Managers;

namespace Enemies
{
    public class BoomerZombie : Zombie
    {
        #region Constants
        
        private const float EXPLOSION_RADIUS = 5f;
        private const float EXPLOSION_DAMAGE = 50f;
        
        #endregion
        
        [Header("Boomer Settings")]
        [SerializeField] private float explosionRadius = EXPLOSION_RADIUS;
        [SerializeField] private float explosionDamage = EXPLOSION_DAMAGE;
        [SerializeField] private EffectDefinition explosionEffect;
        [SerializeField] private float detonationDistance = 1.5f; // Distance at which to explode
        
        [Header("Movement Settings")]
        [SerializeField] private float chargeSpeed = 8f; // Faster than normal zombies
        [SerializeField] private bool useChargeAnimation = true;
        
        private bool isCharging = false;
        private bool hasExploded = false;

        protected override void Awake()
        {
            base.Awake();
            
            // Override speed for boomer
            if (agent != null)
            {
                agent.speed = chargeSpeed;
            }
            
            // Validate explosion effect
            if (explosionEffect == null)
            {
                Debug.LogError("Explosion effect definition is not assigned to BoomerZombie on " + gameObject.name);
            }
        }

        protected override void Update()
        {
            base.Update();

            if (navMeshTarget == null || hasExploded) return;

            float distanceToTarget = Vector3.Distance(transform.position, navMeshTarget.position);
            
            // Check if we should explode
            if (distanceToTarget <= detonationDistance && !hasExploded)
            {
                Explode();
                return;
            }
            
            // Handle charging behavior
            HandleChargingBehavior(distanceToTarget);
        }

        private void HandleChargingBehavior(float distanceToTarget)
        {
            // Don't use normal attack logic - just charge towards target
            if (isAttacking) return;
            
            // Set animation parameter for charging
            if (animator != null && useChargeAnimation)
            {
                animator.SetFloat("Speed", agent.velocity.magnitude);
            }
            
            // Ensure we're moving towards the target
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(navMeshTarget.position);
            }
        }

        private void Explode()
        {
            if (hasExploded) return;
            
            hasExploded = true;
            Debug.Log($"[BoomerZombie] {gameObject.name} exploding!");
            
            // Play explosion effect
            if (explosionEffect != null)
            {
                EffectManager.Instance.PlayEffect(
                    transform.position,
                    Vector3.zero,
                    Quaternion.identity,
                    null,
                    explosionEffect
                );
            }
            
            // Find all colliders in explosion radius
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
            
            foreach (Collider hitCollider in hitColliders)
            {
                // Apply damage to any damageable entity
                IDamageable damageable = hitCollider.GetComponent<IDamageable>();
                if (damageable != null && damageable != this)
                {
                    damageable.TakeDamage(explosionDamage);
                    Debug.Log($"[BoomerZombie] {gameObject.name} dealt {explosionDamage} damage to {hitCollider.name}");
                }
            }
            
            // Destroy the boomer
            Destroy(gameObject);
        }

        // Override attack methods to prevent normal attacks
        protected override void BeginAttackSequence()
        {
            // Boomers don't use normal attacks - they explode instead
            return;
        }

        // Override death to prevent normal death behavior
        public override void Die()
        {
            // If we die before exploding, still explode
            if (!hasExploded)
            {
                Explode();
            }
            else
            {
                base.Die();
            }
        }

        // Optional: Add visual indicator when close to target
        private void OnDrawGizmosSelected()
        {
            // Draw explosion radius
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
            
            // Draw detonation distance
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detonationDistance);
        }
    }
} 