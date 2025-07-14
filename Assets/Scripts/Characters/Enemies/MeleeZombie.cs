using UnityEngine;

namespace Enemies
{
    public class MeleeZombie : Zombie
    {
        [Header("Melee Settings")]
        [SerializeField] protected float meleeDamage = 2f;
        [SerializeField] protected float meleeAttackRadius = 1.5f; // Radius of the attack sphere
        [SerializeField] protected LayerMask targetLayer; // Layer mask for valid targets
        [SerializeField] protected float buildingAttackRange = 5f; // Special attack range for buildings (separate from NPC combat)

        // Called by animation event or timing logic
        public void MeleeAttack()
        {
            if (navMeshTarget == null) return;
            
            // Check if target is still valid (active, alive, not in bunker, etc.)
            if (!IsTargetStillValid(navMeshTarget))
            {
                Debug.Log($"[MeleeZombie] {gameObject.name} target {navMeshTarget.name} is no longer valid - aborting attack");
                return;
            }

            // Use building-specific attack range if attacking a building, otherwise use normal attack range
            float currentAttackRange = attackRange;
            if (navMeshTarget.GetComponent<Building>() != null)
            {
                currentAttackRange = buildingAttackRange;
            }
            
            // Use shared navigation utility to calculate effective attack distance
            float effectiveAttackDistance = NavigationUtils.CalculateEffectiveReachDistance(transform.position, navMeshTarget, currentAttackRange, 1.0f);
            float distanceToTarget = Vector3.Distance(transform.position, navMeshTarget.position);
            
            // Debug logging for attack attempts
            Debug.Log($"[MeleeZombie] {gameObject.name} attacking {navMeshTarget.name}: " +
                     $"Range={currentAttackRange:F1}, EffectiveDistance={effectiveAttackDistance:F1}, " +
                     $"ActualDistance={distanceToTarget:F1}, InRange={distanceToTarget <= effectiveAttackDistance}");
            
            if (distanceToTarget <= effectiveAttackDistance)
            {
                // Check if target is in front of us
                Vector3 directionToTarget = (navMeshTarget.position - transform.position).normalized;
                float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

                Debug.Log($"[MeleeZombie] {gameObject.name} angle check: " +
                         $"Angle={angleToTarget:F1}°, InAngle={angleToTarget <= 45f}");

                if (angleToTarget <= 45f) // Allow a wider attack angle
                {
                    // Try to get IDamageable from the target
                    IDamageable target = navMeshTarget.GetComponent<IDamageable>();
                    if (target != null)
                    {
                        Debug.Log($"[MeleeZombie] {gameObject.name} dealing {meleeDamage} damage to {navMeshTarget.name}");
                        target.TakeDamage(meleeDamage, transform);
                    }
                    else
                    {
                        Debug.LogError($"[MeleeZombie] {gameObject.name}: Target {navMeshTarget.name} does not implement IDamageable! Target type: {navMeshTarget.GetType().Name}");
                    }
                }
                else
                {
                    Debug.Log($"[MeleeZombie] {gameObject.name} failed angle check - not facing target");
                }
            }
            else
            {
                Debug.Log($"[MeleeZombie] {gameObject.name} failed distance check - too far from target");
            }
        }

        /// <summary>
        /// Check if the target is still a valid, attackable target
        /// </summary>
        private bool IsTargetStillValid(Transform target)
        {
            if (target == null || target.gameObject == null) return false;
            
            // Check if the target is still active in the scene (this will catch NPCs in bunkers)
            if (!target.gameObject.activeInHierarchy) return false;
            
            var damageable = target.GetComponent<IDamageable>();
            if (damageable == null || damageable.Health <= 0) return false;
            
            // Special check for walls - they might be destroyed but still have health > 0
            if (target.GetComponent<WallBuilding>() is WallBuilding wallBuilding)
            {
                if (wallBuilding.IsDestroyed || wallBuilding.IsBeingDestroyed) return false;
            }
            
            return damageable.GetAllegiance() == Allegiance.FRIENDLY;
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
