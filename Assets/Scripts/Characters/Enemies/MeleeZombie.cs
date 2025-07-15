using UnityEngine;

namespace Enemies
{
    public class MeleeZombie : Zombie
    {
        #region Constants
        
        // Use the attack-ready threshold from base class for validation
        private const float MELEE_ATTACK_ANGLE_THRESHOLD = 45f; // Now using stricter threshold
        
        #endregion
        
        [Header("Melee Settings")]
        [SerializeField] protected float meleeDamage = 2f;
        [SerializeField] protected float meleeAttackRadius = 1.5f; // Radius of the attack sphere
        [SerializeField] protected LayerMask targetLayer; // Layer mask for valid targets
        [SerializeField] protected float buildingAttackRange = 5f; // Special attack range for buildings (separate from NPC combat)

        // Called by animation event or timing logic
        protected override void Attack()
        {
            // Don't attack if dead
            if (Health <= 0) return;
            
            // Use building-specific attack range if attacking a building, otherwise use normal attack range
            float currentAttackRange = attackRange;
            if (navMeshTarget != null && navMeshTarget.GetComponent<Building>() != null)
            {
                currentAttackRange = buildingAttackRange;
            }
            
            // Use base class validation
            if (!ValidateAttack(currentAttackRange, MELEE_ATTACK_ANGLE_THRESHOLD, out float distanceToTarget, out float angleToTarget))
            {
                // Log failure reason
                if (navMeshTarget == null)
                {
                    Debug.Log($"[MeleeZombie] {gameObject.name}: No target - aborting attack");
                }
                else if (!IsTargetStillValid(navMeshTarget))
                {
                    Debug.Log($"[MeleeZombie] {gameObject.name}: Target {navMeshTarget.name} invalid - aborting attack");
                }
                else
                {
                    float effectiveDistance = NavigationUtils.CalculateEffectiveReachDistance(transform.position, navMeshTarget, currentAttackRange, 1.0f);
                    string reason = distanceToTarget > effectiveDistance ? "RANGE_FAIL" : "ANGLE_FAIL";
                    Debug.Log($"[MeleeZombie] {gameObject.name} → {navMeshTarget.name}: " +
                             $"Dist={distanceToTarget:F1}, Angle={angleToTarget:F1}° {reason}");
                }
                return;
            }

            // Execute the attack
            IDamageable target = navMeshTarget.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(meleeDamage, transform);
                
                // Success logging
                Debug.Log($"[MeleeZombie] {gameObject.name} → {navMeshTarget.name}: " +
                         $"DMG={meleeDamage}, Range={currentAttackRange:F1}, " +
                         $"Dist={distanceToTarget:F1}, Angle={angleToTarget:F1}° SUCCESS");
            }
            else
            {
                Debug.LogError($"[MeleeZombie] {gameObject.name}: Target {navMeshTarget.name} missing IDamageable!");
            }
        }



        // Optional: Visualize attack range in editor
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // Draw attack angle
            Vector3 rightDir = Quaternion.Euler(0, 45, 0) * transform.forward;
            Vector3 leftDir = Quaternion.Euler(0, -45, 0) * transform.forward;
            Gizmos.DrawRay(transform.position, rightDir * attackRange);
            Gizmos.DrawRay(transform.position, leftDir * attackRange);
        }
    }
}
