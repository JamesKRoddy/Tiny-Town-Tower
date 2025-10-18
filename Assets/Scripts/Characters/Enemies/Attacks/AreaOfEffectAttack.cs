using UnityEngine;

namespace Enemies.Attacks
{
    /// <summary>
    /// Area of effect attack that creates a persistent damage zone.
    /// Good for ground-based effects, poison clouds, fire patches, etc.
    /// </summary>
    public class AreaOfEffectAttack : AttackBase
    {
        [Header("Area of Effect Settings")]
        [Tooltip("Radius of the area of effect")]
        public float aoeRadius = 5f;
        [Tooltip("Duration of the area of effect")]
        public float aoeDuration = 5f;
        [Tooltip("How often damage is dealt (seconds)")]
        public float damageInterval = 0.5f;
        [Tooltip("Damage dealt per tick")]
        public float damagePerTick = 10f;
        [Tooltip("Poise damage dealt per tick")]
        public float poiseDamagePerTick = 5f;
        
        
        protected override void Awake()
        {
            base.Awake();
            // Set default attack type for area of effect attacks
            if (attackType == 0)
            {
                attackType = 5; // Area of effect attack type
            }
            
            // Set default range values for area of effect attacks
            if (minRange == 0 && maxRange == 5) // Only set defaults if they haven't been customized
            {
                minRange = 3f;   // Minimum range for area of effect attacks
                maxRange = 8f;   // Maximum range for area of effect attacks
            }
            
            // Set default angle threshold for area of effect attacks
            if (attackAngleThreshold == 30) // Only set default if it hasn't been customized
            {
                attackAngleThreshold = 20f; // Area of effect attacks need moderate aiming
            }
        }

        public override void Initialize(EnemyBase enemy)
        {
            base.Initialize(enemy);
            
            // Set default elemental damage for area of effect attacks
            if (attackElement == AttackElement.NONE)
            {
                attackElement = AttackElement.PHYSICAL;
            }
            
            Debug.Log($"[{enemy.gameObject.name}] AreaOfEffectAttack initialized | Min: {minRange} | Max: {maxRange} | Radius: {aoeRadius} | Duration: {aoeDuration}");
        }

        public override void OnAttack()
        {
            if (target == null)
            {
                Debug.LogWarning($"[{enemy.gameObject.name}] Area of effect attack called with no target!");
                return;
            }
            
            // Create the area of effect at the target's position
            Vector3 aoePosition = target.position;
            
            // Create damage area using utility
            GameObject damageArea = DamageUtils.CreateDamageArea(
                aoePosition, 
                aoeRadius, 
                damagePerTick, 
                poiseDamagePerTick, 
                enemy.transform, 
                attackElement, 
                aoeDuration, 
                damageInterval, 
                attackEffect
            );
            
            Debug.Log($"[{enemy.gameObject.name}] Area of effect attack executed | Position: {aoePosition} | Radius: {aoeRadius} | Duration: {aoeDuration}");
        }

        protected override void OnDrawGizmosSelected()
        {
            if (enemy == null) return;
            
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(enemy.transform.position, maxRange);
            
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
            
            // Draw area of effect radius
            if (target != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(target.position, aoeRadius);
            }
        }
    }
}
