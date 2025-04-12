using UnityEngine;
using Managers;

namespace Enemies
{
    public class BossBrawler : Boss
    {
        [Header("Attack Settings")]
        public float aoeDamage = 10f;
        public float aoeRadius = 5f;
        public float shockwaveDamage = 15f;
        public float shockwaveSpeed = 10f;
        public float shockwaveRange = 15f;
        public GameObject shockwavePrefab; // Assign in inspector

        // Called by animation event for AoE attack
        public void AoEAttack()
        {
            // Find all colliders in the AoE radius
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, aoeRadius);
            
            foreach (var hitCollider in hitColliders)
            {
                IDamageable damageable = hitCollider.GetComponent<IDamageable>();
                if (damageable != null && damageable.GetAllegiance() == Allegiance.FRIENDLY)
                {
                    damageable.TakeDamage(aoeDamage, transform);
                }
            }

            // Play AoE VFX
            EffectManager.Instance.PlayHitEffect(transform.position + Vector3.up, Vector3.up, this);
        }

        // Called by animation event for shockwave attack
        public void GroundShockwave()
        {
            if (shockwavePrefab != null && navMeshTarget != null)
            {
                // Calculate direction to player
                Vector3 direction = (navMeshTarget.position - transform.position).normalized;
                direction.y = 0; // Keep the shockwave horizontal

                // Spawn shockwave
                GameObject shockwave = Instantiate(shockwavePrefab, transform.position, Quaternion.LookRotation(direction));
                BossShockwave shockwaveComponent = shockwave.GetComponent<BossShockwave>();
                
                if (shockwaveComponent != null)
                {
                    shockwaveComponent.Initialize(direction, shockwaveSpeed, shockwaveRange, shockwaveDamage);
                }
            }
        }

        protected override void StartAttack()
        {
            base.StartAttack();
            // Additional logic for boss attack can be added here
        }

        private void OnDrawGizmos()
        {
            // Draw AoE attack radius
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // Red with transparency
            Gizmos.DrawWireSphere(transform.position, aoeRadius);
            
            // Draw shockwave range if we have a target
            if (navMeshTarget != null)
            {
                Gizmos.color = new Color(0f, 0f, 1f, 0.3f); // Blue with transparency
                Vector3 direction = (navMeshTarget.position - transform.position).normalized;
                direction.y = 0;
                Gizmos.DrawLine(transform.position, transform.position + direction * shockwaveRange);
            }
        }
    }
}
