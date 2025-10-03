using UnityEngine;
using Managers;

namespace Enemies.ZombieAttacks
{
    /// <summary>
    /// Boomer attack component for zombies.
    /// Handles explosive attacks that deal area damage and kill the zombie.
    /// </summary>
    public class BoomerZombieAttack : ZombieAttackBase
    {
        [Header("Explosion Settings")]
        [Tooltip("Radius of the explosion")]
        public float explosionRadius = 5f;
        [Tooltip("Damage dealt by the explosion")]
        public float explosionDamage = 50f;
        [Tooltip("Poise damage multiplier for explosion (multiplied by explosion damage)")]
        public float poiseDamageMultiplier = 1.2f;
        
        protected override void Awake()
        {
            base.Awake();
            // Set default attack type for boomer attacks
            if (attackType == 0)
            {
                attackType = 4; // Boomer attack type
            }
        }
        
        [Header("Explosion Effects")]
        [Tooltip("Effect played when the boomer explodes")]
        public EffectDefinition explosionEffect;
        
        [Header("Boomer Behavior")]
        [Tooltip("Whether the boomer should explode when it dies")]
        public bool explodeOnDeath = true;
        [Tooltip("Whether the boomer should explode when it takes damage")]
        public bool explodeOnDamage = false;
        [Tooltip("Minimum damage required to trigger explosion on damage")]
        public float minDamageToExplode = 10f;

        private bool hasExploded = false;

        public override void Initialize(EnemyBase enemy)
        {
            base.Initialize(enemy);
            
            if (enemy is Zombie zombieEnemy)
            {
                // Set default elemental damage for explosion attacks (can be overridden in inspector)
                if (attackElement == AttackElement.NONE)
                {
                    attackElement = AttackElement.FIRE; // Explosions could be fire damage
                }
                
                // Set default range to explosion radius
                range = explosionRadius;
                
                // Validate explosion effect
                if (explosionEffect == null)
                {
                    Debug.LogError("Explosion effect definition is not assigned to BoomerZombieAttack on " + zombieEnemy.gameObject.name);
                }
                
                Debug.Log($"[{zombieEnemy.gameObject.name}] BoomerZombieAttack initialized | ExplosionRadius: {explosionRadius} | ExplosionDamage: {explosionDamage}");
            }
        }

        public override bool CanAttack()
        {
            if (!base.CanAttack()) return false;
            if (hasExploded) return false;
            
            float distanceToTarget = Vector3.Distance(zombie.transform.position, target.position);
            
            // Can attack when close enough to target
            return distanceToTarget <= range;
        }

        public override void StartAttack()
        {
            base.StartAttack();
            
            // Play start effect (warning effect)
            PlayStartEffect(zombie.transform.position + Vector3.up * 1.0f, Vector3.up, Quaternion.identity);
            
            Debug.Log($"[{zombie.gameObject.name}] Boomer attack started | Target: {target.name} | Distance: {Vector3.Distance(zombie.transform.position, target.position):F2}");
        }

        public override void OnAttack()
        {
            if (hasExploded) return;
            
            hasExploded = true;
            Debug.Log($"[{zombie.gameObject.name}] Boomer exploding!");
            
            // Play explosion effect
            if (explosionEffect != null)
            {
                EffectManager.Instance.PlayEffect(
                    zombie.transform.position + Vector3.up * 1.0f,
                    Vector3.zero,
                    Quaternion.identity,
                    null,
                    explosionEffect
                );
            }
            
            // Deal explosion damage in radius
            DealExplosionDamage();
            
            // Play attack effect
            PlayAttackEffect(zombie.transform.position + Vector3.up * 1.0f, Vector3.up, Quaternion.identity);
            
            Debug.Log($"[{zombie.gameObject.name}] Boomer explosion executed | Damage: {explosionDamage} | Radius: {explosionRadius}");
        }

        public override void OnAttackEnd()
        {
            base.OnAttackEnd();
            
            // Boomer dies after exploding
            if (zombie != null)
            {
                zombie.Die();
            }
        }

        /// <summary>
        /// Deal explosion damage to all targets in radius
        /// </summary>
        private void DealExplosionDamage()
        {
            // Find all colliders in explosion radius
            Collider[] hitColliders = Physics.OverlapSphere(zombie.transform.position, explosionRadius);
            
            foreach (Collider hitCollider in hitColliders)
            {
                    // Apply damage to any damageable entity (except self)
                    IDamageable damageable = hitCollider.GetComponent<IDamageable>();
                    if (damageable != null && (object)damageable != zombie)
                {
                    // Check if the target is still active (this will catch NPCs in bunkers)
                    if (!hitCollider.gameObject.activeInHierarchy)
                    {
                        continue; // Skip inactive targets (like NPCs in bunkers)
                    }
                    
                    // Explosion deals high poise damage
                    float totalPoiseDamage = explosionDamage * poiseDamageMultiplier;
                    
                    // Apply damage with elemental type
                    if (attackElement == AttackElement.NONE || attackElement == AttackElement.PHYSICAL)
                    {
                        damageable.TakeDamage(explosionDamage, totalPoiseDamage, zombie.transform);
                    }
                    else
                    {
                        damageable.TakeDamage(explosionDamage, totalPoiseDamage, attackElement, zombie.transform);
                    }
                    
                    // Play hit effect at the point of impact
                    Vector3 hitPoint = hitCollider.ClosestPoint(zombie.transform.position);
                    Vector3 hitNormal = (hitPoint - zombie.transform.position).normalized;
                    PlayHitEffect(hitPoint, hitNormal);
                    
                    Debug.Log($"[{zombie.gameObject.name}] Boomer dealt {explosionDamage} damage and {totalPoiseDamage} poise damage to {hitCollider.name}");
                }
            }
        }

        /// <summary>
        /// Check if the boomer should explode when taking damage
        /// </summary>
        /// <param name="damageAmount">Amount of damage taken</param>
        /// <returns>True if should explode</returns>
        public bool ShouldExplodeOnDamage(float damageAmount)
        {
            return explodeOnDamage && damageAmount >= minDamageToExplode && !hasExploded;
        }

        /// <summary>
        /// Check if the boomer should explode on death
        /// </summary>
        /// <returns>True if should explode</returns>
        public bool ShouldExplodeOnDeath()
        {
            return explodeOnDeath && !hasExploded;
        }

        /// <summary>
        /// Force the boomer to explode (called from external sources)
        /// </summary>
        public void ForceExplode()
        {
            if (hasExploded) return;
            
            hasExploded = true;
            
            // Play explosion effect
            if (explosionEffect != null)
            {
                EffectManager.Instance.PlayEffect(
                    zombie.transform.position + Vector3.up * 1.0f,
                    Vector3.zero,
                    Quaternion.identity,
                    null,
                    explosionEffect
                );
            }
            
            // Deal explosion damage
            DealExplosionDamage();
            
            // Kill the zombie
            if (zombie != null)
            {
                zombie.Die();
            }
            
            Debug.Log($"[{zombie.gameObject.name}] Boomer force exploded!");
        }

        /// <summary>
        /// Check if the zombie should rotate towards target before attacking
        /// </summary>
        /// <returns>True if rotation is needed</returns>
        public override bool ShouldRotateToAttack()
        {
            if (target == null) return false;
            
            return !IsReadyToAttack(45f); // Boomers don't need precise aiming
        }

        /// <summary>
        /// Get the current attack range
        /// </summary>
        /// <returns>Current effective attack range</returns>
        public override float GetCurrentAttackRange()
        {
            return range;
        }

        /// <summary>
        /// Check if the boomer has already exploded
        /// </summary>
        /// <returns>True if already exploded</returns>
        public bool HasExploded()
        {
            return hasExploded;
        }

        /// <summary>
        /// Draw debug gizmos for boomer attack
        /// </summary>
        protected override void OnDrawGizmosSelected()
        {
            if (zombie == null) return;
            
            // Draw explosion radius
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(zombie.transform.position, explosionRadius);
            
            // Draw detonation distance
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(zombie.transform.position, range);
            
            // Draw explosion center
            Gizmos.color = new Color(1f, 0.5f, 0f); // Orange color
            Gizmos.DrawWireSphere(zombie.transform.position + Vector3.up * 1.0f, 0.5f);
        }
    }
}
