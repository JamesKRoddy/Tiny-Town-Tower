using UnityEngine;

namespace Enemies
{
    public class MeleeZombie : Zombie
    {
        [Header("Melee Settings")]
        [SerializeField] protected float meleeDamage = 2f;
        [SerializeField] protected float meleeAttackRadius = 1.5f; // Radius of the attack sphere
        [SerializeField] protected LayerMask targetLayer; // Layer mask for valid targets

        // Called by animation event or timing logic
        public void MeleeAttack()
        {
            if (navMeshTarget == null) return;

            float distanceToTarget = Vector3.Distance(transform.position, navMeshTarget.position);

            if (distanceToTarget <= attackRange)
            {
                // Check if target is in front of us
                Vector3 directionToTarget = (navMeshTarget.position - transform.position).normalized;
                float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

                if (angleToTarget <= 45f) // Allow a wider attack angle
                {
                    // Try to get IDamageable from the target
                    IDamageable target = navMeshTarget.GetComponent<IDamageable>();
                    if (target != null)
                    {
                        target.TakeDamage(meleeDamage, transform);
                        Debug.Log($"Player hit by melee attack for {meleeDamage} damage");
                    }
                    else
                    {
                        Debug.LogWarning("Target does not implement IDamageable");
                    }
                }
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
