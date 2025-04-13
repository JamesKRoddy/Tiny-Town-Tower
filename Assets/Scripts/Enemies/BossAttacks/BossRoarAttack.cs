using UnityEngine;

namespace Enemies.BossAttacks
{
    public class BossRoarAttack : BossAttackBase
    {
        [Header("Roar Settings")]
        public float aoeRadius = 5f;
        public float aoeDamage = 10f;

        public override void Initialize(BossBrawler boss)
        {
            base.Initialize(boss);
            attackType = 1; // Set to match the animator parameter for roar
        }

        public override void OnAttackStart()
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
            PlayHitEffect(transform.position + Vector3.up, Vector3.up);
        }
    }
} 