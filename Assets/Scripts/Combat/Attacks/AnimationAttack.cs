using UnityEngine;

namespace Combat.Attacks
{
    /// <summary>
    /// Used for all attacks that directly call their damage through animation events and collision detection.
    /// Good for melee combat, punches, claws, etc.
    /// 
    /// This is the base class for weapon-based attacks (WeaponAttack) and can be used
    /// by any character type that implements IAttackOwner.
    /// </summary>
    public class AnimationAttack : AttackBase
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
            
            // Set default cooldown range for melee attacks (faster than special attacks)
            if (Mathf.Approximately(minCooldown, 2f) && Mathf.Approximately(maxCooldown, 4f)) // Only set if using defaults
            {
                minCooldown = 1.5f;  // Minimum cooldown for quick melee
                maxCooldown = 3f;    // Maximum cooldown for variety
            }
        }

        /// <summary>
        /// Initialize with IAttackOwner
        /// </summary>
        public override void Initialize(IAttackOwner attackOwner)
        {
            base.Initialize(attackOwner);
            
            // Set default elemental damage for close range attacks
            if (attackElement == AttackElement.NONE)
            {
                attackElement = AttackElement.PHYSICAL;
            }
            
            Debug.Log($"[{attackOwner.gameObject.name}] AnimationAttack initialized | Min: {minRange} | Max: {maxRange} | Radius: {attackRadius} | Damage: {damage}");
        }

        public override void OnAttack()
        {
            base.OnAttack();
            
            if (target == null)
            {
                Debug.LogWarning($"[{OwnerTransform.gameObject.name}] Close range attack called with no target!");
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
            float distanceToTarget = Vector3.Distance(OwnerTransform.position, target.position);
            float effectiveDistance = NavigationUtils.CalculateEffectiveReachDistance(OwnerTransform.position, target, currentAttackRange, 1f);
            
            if (distanceToTarget > effectiveDistance)
            {
                Debug.LogWarning($"[{OwnerTransform.gameObject.name}] Close range attack failed - target out of range | Distance: {distanceToTarget:F2} | Effective: {effectiveDistance:F2}");
                return;
            }
            
            // Check angle validation
            if (!IsReadyToAttack())
            {
                Debug.LogWarning($"[{OwnerTransform.gameObject.name}] Close range attack failed - not facing target");
                return;
            }
            
            // Deal damage in radius (exclude self to prevent self-damage)
            int targetsDamaged = DamageUtils.DealDamageInRadius(OwnerTransform.position, attackRadius, damage, poiseDamage, OwnerTransform, attackElement, targetLayer, excludeSelf: true);
            
            Debug.Log($"[{OwnerTransform.gameObject.name}] Close range attack executed | Damage: {damage} | Radius: {attackRadius} | Targets: {targetsDamaged}");
        }

        public override float GetCurrentAttackRange()
        {
            if (target == null) return maxRange;
            
            bool isAttackingBuilding = target.GetComponent<Building>() != null;
            return isAttackingBuilding ? buildingAttackRange : maxRange;
        }

        protected override void OnDrawGizmosSelected()
        {
            Transform drawTransform = OwnerTransform ?? transform;
            
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(drawTransform.position, maxRange);
            
            // Draw attack radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(drawTransform.position, attackRadius);
            
            // Draw min/max distances
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(drawTransform.position, minRange);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(drawTransform.position, maxRange);
            
            // Draw attack angle
            Vector3 rightDir = Quaternion.Euler(0, attackAngleThreshold, 0) * drawTransform.forward;
            Vector3 leftDir = Quaternion.Euler(0, -attackAngleThreshold, 0) * drawTransform.forward;
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(drawTransform.position, rightDir * maxRange);
            Gizmos.DrawRay(drawTransform.position, leftDir * maxRange);
        }
    }
}
