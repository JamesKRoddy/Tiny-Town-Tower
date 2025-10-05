using UnityEngine;

namespace Enemies.Attacks
{
    /// <summary>
    /// Close-range attack that deals damage in a small radius around the attacker.
    /// Good for melee combat, punches, claws, etc.
    /// </summary>
    public class CloseRangeAttack : AttackBase
    {
        [Header("Close Range Settings")]
        [Tooltip("Radius of the attack damage area")]
        public float attackRadius = 1.5f;
        [Tooltip("Layer mask for valid targets")]
        public LayerMask targetLayer = -1;
        [Tooltip("Special attack range for buildings (separate from NPC combat)")]
        public float buildingAttackRange = 5f;
        
        protected override void Awake()
        {
            base.Awake();
            // Set default attack type for close range attacks
            if (attackType == 0)
            {
                attackType = 1; // Close range attack type
            }
            
            // Set default range values for close range attacks
            if (minRange == 0 && maxRange == 5) // Only set defaults if they haven't been customized
            {
                minRange = 0f;  // Minimum distance for close range attacks
                maxRange = 1.5f;  // Maximum distance for close range attacks
            }
            
            // Set default angle threshold for close range attacks
            if (attackAngleThreshold == 30) // Only set default if it hasn't been customized
            {
                attackAngleThreshold = 45f; // Close range attacks are more forgiving with angle
            }
        }

        public override void Initialize(EnemyBase enemy)
        {
            base.Initialize(enemy);
            
            // Set default elemental damage for close range attacks
            if (attackElement == AttackElement.NONE)
            {
                attackElement = AttackElement.PHYSICAL;
            }
            
            Debug.Log($"[{enemy.gameObject.name}] CloseRangeAttack initialized | Min: {minRange} | Max: {maxRange} | Radius: {attackRadius} | Damage: {damage}");
        }

        public override void OnAttack()
        {
            if (target == null)
            {
                Debug.LogWarning($"[{enemy.gameObject.name}] Close range attack called with no target!");
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
            float distanceToTarget = Vector3.Distance(enemy.transform.position, target.position);
            float effectiveDistance = NavigationUtils.CalculateEffectiveReachDistance(enemy.transform.position, target, currentAttackRange, 1f);
            
            if (distanceToTarget > effectiveDistance)
            {
                Debug.LogWarning($"[{enemy.gameObject.name}] Close range attack failed - target out of range | Distance: {distanceToTarget:F2} | Effective: {effectiveDistance:F2}");
                return;
            }
            
            // Check angle validation
            if (!IsReadyToAttack())
            {
                Debug.LogWarning($"[{enemy.gameObject.name}] Close range attack failed - not facing target");
                return;
            }
            
            // Debug: Check what's in the attack radius
            Collider[] hitColliders = Physics.OverlapSphere(enemy.transform.position, attackRadius, targetLayer);
            Debug.Log($"[{enemy.gameObject.name}] Attack radius check: Found {hitColliders.Length} colliders");
            
            foreach (var collider in hitColliders)
            {
                IDamageable damageable = collider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    Debug.Log($"[{enemy.gameObject.name}] Found damageable: {collider.name} | Allegiance: {damageable.GetAllegiance()}");
                }
                else
                {
                    Debug.Log($"[{enemy.gameObject.name}] Found non-damageable: {collider.name}");
                }
            }
            
            // Deal damage in radius
            int targetsDamaged = DamageUtils.DealDamageInRadius(enemy.transform.position, attackRadius, damage, poiseDamage, enemy.transform, attackElement, targetLayer);
            
            Debug.Log($"[{enemy.gameObject.name}] Close range attack executed | Damage: {damage} | Radius: {attackRadius} | Targets: {targetsDamaged}");
        }

        public override float GetCurrentAttackRange()
        {
            if (target == null) return maxRange;
            
            bool isAttackingBuilding = target.GetComponent<Building>() != null;
            return isAttackingBuilding ? buildingAttackRange : maxRange;
        }

        protected override void OnDrawGizmosSelected()
        {
            if (enemy == null) return;
            
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(enemy.transform.position, maxRange);
            
            // Draw attack radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(enemy.transform.position, attackRadius);
            
            // Draw min/max distances
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(enemy.transform.position, minRange);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(enemy.transform.position, maxRange);
            
            // Draw attack angle
            Vector3 rightDir = Quaternion.Euler(0, attackAngleThreshold, 0) * enemy.transform.forward;
            Vector3 leftDir = Quaternion.Euler(0, -attackAngleThreshold, 0) * enemy.transform.forward;
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(enemy.transform.position, rightDir * maxRange);
            Gizmos.DrawRay(enemy.transform.position, leftDir * maxRange);
        }
    }
}
