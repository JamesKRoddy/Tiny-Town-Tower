using UnityEngine;

namespace Enemies.ZombieAttacks
{
    /// <summary>
    /// Melee attack component for zombies.
    /// Handles close-range physical attacks with area damage.
    /// </summary>
    public class MeleeZombieAttack : ZombieAttackBase
    {
        [Header("Melee Settings")]
        [Tooltip("Radius of the melee attack area")]
        public float attackRadius = 1.5f;
        [Tooltip("Layer mask for valid targets")]
        public LayerMask targetLayer = -1;
        [Tooltip("Special attack range for buildings (separate from NPC combat)")]
        public float buildingAttackRange = 5f;
        
        protected override void Awake()
        {
            base.Awake();
            // Set default attack type for melee attacks
            if (attackType == 0)
            {
                attackType = 1; // Melee attack type
            }
            
            // Set default range values for melee attacks
            if (minRange == 0 && maxRange == 5) // Only set defaults if they haven't been customized
            {
                minRange = 1.0f;  // Minimum distance for melee attacks
                maxRange = 1.5f;  // Maximum distance for melee attacks
            }
            
            // Set default angle threshold for melee attacks
            if (attackAngleThreshold == 30) // Only set default if it hasn't been customized
            {
                attackAngleThreshold = 45f; // Melee attacks are more forgiving with angle
            }
        }
        

        public override void Initialize(EnemyBase enemy)
        {
            base.Initialize(enemy);
            
            if (enemy is Zombie zombieEnemy)
            {
                // Set default elemental damage for melee attacks (can be overridden in inspector)
                if (attackElement == AttackElement.NONE)
                {
                    attackElement = AttackElement.PHYSICAL;
                }
                
                
                Debug.Log($"[{zombieEnemy.gameObject.name}] MeleeZombieAttack initialized | Min: {minRange} | Max: {maxRange} | Radius: {attackRadius} | Damage: {damage}");
            }
        }

        public override bool CanAttack()
        {
            if (!base.CanAttack()) return false;
            
            float distanceToTarget = Vector3.Distance(zombie.transform.position, target.position);
            
            // Check if we're in the melee attack range
            bool inRange = distanceToTarget >= minRange && distanceToTarget <= maxRange;
            
            if (inRange)
            {
                Debug.Log($"[{zombie.gameObject.name}] MeleeZombieAttack CanAttack: TRUE | Distance: {distanceToTarget:F2} | Range: {minRange}-{maxRange}");
            }
            
            return inRange;
        }

        public override void StartAttack()
        {
            base.StartAttack();
            
            // Play start effect at attack origin
            PlayStartEffect(attackOrigin.position, attackOrigin.forward, attackOrigin.rotation, attackOrigin);
            
            Debug.Log($"[{zombie.gameObject.name}] Melee attack started | Target: {target.name} | Distance: {Vector3.Distance(zombie.transform.position, target.position):F2}");
        }

        public override void OnAttack()
        {
            if (target == null)
            {
                Debug.LogWarning($"[{zombie.gameObject.name}] Melee attack called with no target!");
                return;
            }
            
            // Check if we should use building-specific attack range
            float currentAttackRange = maxRange;
            bool isAttackingBuilding = target.GetComponent<Building>() != null;
            if (isAttackingBuilding)
            {
                currentAttackRange = buildingAttackRange;
            }
            
            // Validate attack conditions
            float distanceToTarget = Vector3.Distance(zombie.transform.position, target.position);
            float effectiveDistance = NavigationUtils.CalculateEffectiveReachDistance(zombie.transform.position, target, currentAttackRange, 1f);
            
            if (distanceToTarget > effectiveDistance)
            {
                Debug.LogWarning($"[{zombie.gameObject.name}] Melee attack failed - target out of range | Distance: {distanceToTarget:F2} | Effective: {effectiveDistance:F2}");
                return;
            }
            
            // Check angle validation
            if (!IsReadyToAttack())
            {
                Debug.LogWarning($"[{zombie.gameObject.name}] Melee attack failed - not facing target");
                return;
            }
            
            // Deal damage in radius for melee attacks
            DealDamageInRadius(attackRadius, damage, zombie.transform.position);
            
            // Play attack effect
            PlayAttackEffect(attackOrigin.position, attackOrigin.forward, attackOrigin.rotation, attackOrigin);
            
            Debug.Log($"[{zombie.gameObject.name}] Melee attack executed | Damage: {damage} | Radius: {attackRadius} | Target: {target.name}");
        }

        public override void OnAttackEnd()
        {
            base.OnAttackEnd();
            
            // Play end effect
            PlayEndEffect(attackOrigin.position, attackOrigin.forward, attackOrigin.rotation, attackOrigin);
        }

        /// <summary>
        /// Check if the zombie should rotate towards target before attacking
        /// </summary>
        /// <returns>True if rotation is needed</returns>
        public override bool ShouldRotateToAttack()
        {
            // Use base class ShouldRotateToAttack() which uses attackAngleThreshold
            return base.ShouldRotateToAttack();
        }

        /// <summary>
        /// Get the current attack range based on target type
        /// </summary>
        /// <returns>Current effective attack range</returns>
        public override float GetCurrentAttackRange()
        {
            if (target == null) return maxRange;
            
            bool isAttackingBuilding = target.GetComponent<Building>() != null;
            return isAttackingBuilding ? buildingAttackRange : maxRange;
        }

        /// <summary>
        /// Draw debug gizmos for melee attack range
        /// </summary>
        protected override void OnDrawGizmosSelected()
        {
            if (zombie == null) return;
            
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(zombie.transform.position, maxRange);
            
            // Draw attack radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(zombie.transform.position, attackRadius);
            
            // Draw min/max distances
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(zombie.transform.position, minRange);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(zombie.transform.position, maxRange);
            
            // Draw attack angle
            Vector3 rightDir = Quaternion.Euler(0, attackAngleThreshold, 0) * zombie.transform.forward;
            Vector3 leftDir = Quaternion.Euler(0, -attackAngleThreshold, 0) * zombie.transform.forward;
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(zombie.transform.position, rightDir * maxRange);
            Gizmos.DrawRay(zombie.transform.position, leftDir * maxRange);
        }
    }
}
