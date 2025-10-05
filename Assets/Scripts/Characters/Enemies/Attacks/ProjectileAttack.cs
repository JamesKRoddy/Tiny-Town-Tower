using UnityEngine;
using Managers;

namespace Enemies.Attacks
{
    /// <summary>
    /// Projectile-based attack that fires projectiles at targets.
    /// Good for ranged attacks, spit attacks, magic missiles, etc.
    /// </summary>
    public class ProjectileAttack : AttackBase
    {
        [Header("Projectile Settings")]
        [Tooltip("Height offset for projectile spawn")]
        public float projectileSpawnHeight = 1.5f;
        [Tooltip("Speed of the projectile")]
        public float projectileSpeed = 10f;
        [Tooltip("Maximum height of the projectile arc")]
        public float projectileMaxHeight = 5f;
        [Tooltip("Whether to create a damage area on impact")]
        public bool createDamageAreaOnImpact = true;
        [Tooltip("Radius of the damage area created on impact")]
        public float impactDamageRadius = 2f;
        [Tooltip("Duration of the damage area created on impact")]
        public float impactDamageDuration = 5f;

        [Header("Projectile Effects")]
        [Tooltip("Effect definition for the projectile visual")]
        public EffectDefinition projectileEffect;
        [Tooltip("Effect definition for impact")]
        public EffectDefinition impactEffect;
        
        protected override void Awake()
        {
            base.Awake();
            // Set default attack type for projectile attacks
            if (attackType == 0)
            {
                attackType = 2; // Projectile attack type
            }
            
            // Set default range values for projectile attacks
            if (minRange == 0 && maxRange == 5) // Only set defaults if they haven't been customized
            {
                minRange = 5f;   // Minimum range for projectile attacks
                maxRange = 15f;  // Maximum range for projectile attacks
            }
            
            // Set default angle threshold for projectile attacks
            if (attackAngleThreshold == 30) // Only set default if it hasn't been customized
            {
                attackAngleThreshold = 5f; // Projectile attacks need precise aiming
            }
        }

        // Store the target position when attack begins
        private Vector3 attackTargetPosition;

        public override void Initialize(EnemyBase enemy)
        {
            base.Initialize(enemy);
            
            // Set default elemental damage for projectile attacks
            if (attackElement == AttackElement.NONE)
            {
                attackElement = AttackElement.PHYSICAL;
            }
            
            // Validate effects
            if (projectileEffect == null)
            {
                Debug.LogError("Projectile effect definition is not assigned to ProjectileAttack on " + enemy.gameObject.name);
            }
            
            Debug.Log($"[{enemy.gameObject.name}] ProjectileAttack initialized | Min: {minRange} | Max: {maxRange} | Damage: {damage}");
        }

        public override void StartAttack()
        {
            base.StartAttack();
            
            // Store the target position when attack begins
            if (target != null)
            {
                attackTargetPosition = target.position;
            }
            
            Debug.Log($"[{enemy.gameObject.name}] Projectile attack started | Target: {target.name} | Distance: {Vector3.Distance(enemy.transform.position, target.position):F2}");
        }

        public override void OnAttack()
        {
            if (target == null)
            {
                Debug.LogWarning($"[{enemy.gameObject.name}] Projectile attack called with no target!");
                return;
            }
            
            // Always fire in the direction we were aiming when the attack started
            Vector3 direction = (attackTargetPosition - enemy.transform.position).normalized;
            
            // Fire the projectile using the utility
            GameObject projectile = DamageUtils.FireProjectileWithEffect(
                enemy.transform.position + Vector3.up * projectileSpawnHeight,
                direction,
                Quaternion.LookRotation(direction),
                attackTargetPosition,
                damage,
                poiseDamage,
                enemy.transform,
                attackElement,
                projectileEffect,
                impactEffect,
                createDamageAreaOnImpact,
                impactDamageRadius,
                impactDamageDuration
            );
            
            Debug.Log($"[{enemy.gameObject.name}] Projectile attack executed | Projectile fired towards: {attackTargetPosition} | Damage: {damage}");
        }

        protected override void OnDrawGizmosSelected()
        {
            if (enemy == null) return;
            
            // Draw min attack range
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(enemy.transform.position, minRange);
            
            // Draw max attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(enemy.transform.position, maxRange);
            
            // Draw attack angle
            Vector3 rightDir = Quaternion.Euler(0, attackAngleThreshold, 0) * enemy.transform.forward;
            Vector3 leftDir = Quaternion.Euler(0, -attackAngleThreshold, 0) * enemy.transform.forward;
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(enemy.transform.position, rightDir * maxRange);
            Gizmos.DrawRay(enemy.transform.position, leftDir * maxRange);
            
            // Draw projectile spawn height
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(enemy.transform.position + Vector3.up * projectileSpawnHeight, 0.5f);
        }
    }
}
