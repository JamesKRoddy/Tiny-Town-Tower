using UnityEngine;
using Managers;

namespace Enemies.ZombieAttacks
{
    /// <summary>
    /// Ranged attack component for zombies.
    /// Handles projectile-based attacks with vomit projectiles.
    /// </summary>
    public class RangedZombieAttack : ZombieAttackBase
    {
        [Header("Ranged Settings")]
        [Tooltip("Minimum range for ranged attacks")]
        public float minAttackRange = 5f;
        [Tooltip("Maximum range for ranged attacks")]
        public float maxAttackRange = 15f;
        [Tooltip("Height offset for projectile spawn")]
        public float projectileSpawnHeight = 1.5f;
        
        protected override void Awake()
        {
            base.Awake();
            // Set default attack type for ranged attacks
            if (attackType == 0)
            {
                attackType = 2; // Ranged attack type
            }
        }
        
        [Header("Ranged Attack Angle")]
        [Tooltip("Maximum angle deviation for ranged attacks")]
        public float attackAngleThreshold = 5f;

        [Header("Vomit Effects")]
        [Tooltip("Effect definition for vomit projectile")]
        public EffectDefinition vomitProjectileEffect;
        [Tooltip("Effect definition for vomit pool")]
        public EffectDefinition vomitPoolEffect;

        // Store the target position when attack begins
        private Vector3 attackTargetPosition;

        public override void Initialize(EnemyBase enemy)
        {
            base.Initialize(enemy);
            
            if (enemy is Zombie zombieEnemy)
            {
                // Set default elemental damage for ranged attacks (can be overridden in inspector)
                if (attackElement == AttackElement.NONE)
                {
                    attackElement = AttackElement.POISON; // Vomit could be poison damage
                }
                
                // Set default range to max attack range
                range = maxAttackRange;
                
                // Validate effects
                if (vomitProjectileEffect == null)
                {
                    Debug.LogError("Vomit projectile effect definition is not assigned to RangedZombieAttack on " + zombieEnemy.gameObject.name);
                }
                if (vomitPoolEffect == null)
                {
                    Debug.LogError("Vomit pool effect definition is not assigned to RangedZombieAttack on " + zombieEnemy.gameObject.name);
                }
                
                Debug.Log($"[{zombieEnemy.gameObject.name}] RangedZombieAttack initialized | Range: {range} | Min: {minAttackRange} | Max: {maxAttackRange} | Damage: {damage}");
            }
        }

        public override bool CanAttack()
        {
            if (!base.CanAttack()) return false;
            
            float distanceToTarget = Vector3.Distance(zombie.transform.position, target.position);
            
            // Check if we're in the ranged attack range
            return distanceToTarget >= minAttackRange && distanceToTarget <= maxAttackRange;
        }

        public override void StartAttack()
        {
            base.StartAttack();
            
            // Store the target position when attack begins
            if (target != null)
            {
                attackTargetPosition = target.position;
            }
            
            // Play start effect
            PlayStartEffect(zombie.transform.position + Vector3.up * projectileSpawnHeight, zombie.transform.forward, zombie.transform.rotation);
            
            Debug.Log($"[{zombie.gameObject.name}] Ranged attack started | Target: {target.name} | Distance: {Vector3.Distance(zombie.transform.position, target.position):F2}");
        }

        public override void OnAttack()
        {
            if (target == null)
            {
                Debug.LogWarning($"[{zombie.gameObject.name}] Ranged attack called with no target!");
                return;
            }
            
            // Always fire in the direction we were aiming when the attack started
            Vector3 direction = (attackTargetPosition - zombie.transform.position).normalized;
            
            // Play the vomit projectile effect and get the spawned GameObject
            GameObject projectileObj = EffectManager.Instance.PlayEffect(
                zombie.transform.position + Vector3.up * projectileSpawnHeight, // Spawn slightly above the zombie
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
                    projectile.Initialize(attackTargetPosition, damage, poiseDamage, vomitPoolEffect);
                }
                else
                {
                    projectile = projectileObj.AddComponent<ZombieVomitProjectile>();
                    projectile.Initialize(attackTargetPosition, damage, poiseDamage, vomitPoolEffect);
                    Debug.LogWarning("ZombieVomitProjectile component not found on spawned projectile, added it to the projectile object");
                }
            }
            
            // Play attack effect
            PlayAttackEffect(zombie.transform.position + Vector3.up * projectileSpawnHeight, direction, Quaternion.LookRotation(direction));
            
            Debug.Log($"[{zombie.gameObject.name}] Ranged attack executed | Projectile fired towards: {attackTargetPosition} | Damage: {damage}");
        }

        public override void OnAttackEnd()
        {
            base.OnAttackEnd();
            
            // Play end effect
            PlayEndEffect(zombie.transform.position + Vector3.up * projectileSpawnHeight, zombie.transform.forward, zombie.transform.rotation);
        }

        /// <summary>
        /// Check if the zombie should rotate towards target before attacking
        /// </summary>
        /// <returns>True if rotation is needed</returns>
        public override bool ShouldRotateToAttack()
        {
            if (target == null) return false;
            
            return !IsReadyToAttack(attackAngleThreshold);
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
        /// Check if the target is within minimum range (too close for ranged attack)
        /// </summary>
        /// <returns>True if target is too close</returns>
        public bool IsTargetTooClose()
        {
            if (target == null) return false;
            
            float distance = Vector3.Distance(zombie.transform.position, target.position);
            return distance < minAttackRange;
        }

        /// <summary>
        /// Draw debug gizmos for ranged attack range
        /// </summary>
        protected override void OnDrawGizmosSelected()
        {
            if (zombie == null) return;
            
            // Draw min attack range
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(zombie.transform.position, minAttackRange);
            
            // Draw max attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(zombie.transform.position, maxAttackRange);
            
            // Draw attack angle
            Vector3 rightDir = Quaternion.Euler(0, attackAngleThreshold, 0) * zombie.transform.forward;
            Vector3 leftDir = Quaternion.Euler(0, -attackAngleThreshold, 0) * zombie.transform.forward;
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(zombie.transform.position, rightDir * maxAttackRange);
            Gizmos.DrawRay(zombie.transform.position, leftDir * maxAttackRange);
            
            // Draw projectile spawn height
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(zombie.transform.position + Vector3.up * projectileSpawnHeight, 0.5f);
        }
    }
}
