using UnityEngine;

namespace Enemies.BossAttacks
{
    public class BossPunchAttack : BossAttackBase
    {
        [Header("Punch Settings")]
        public float punchRadius = 2f;

        public override void Initialize(BossBrawler boss)
        {
            base.Initialize(boss);
            attackType = 0; // Set to match the animator parameter for punch
        }

        public override void OnAttackStart()
        {
            // Find all colliders in the punch radius
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, punchRadius);
            
            foreach (var hitCollider in hitColliders)
            {
                IDamageable damageable = hitCollider.GetComponent<IDamageable>();
                if (damageable != null && damageable.GetAllegiance() == Allegiance.FRIENDLY)
                {
                    damageable.TakeDamage(damage, transform);
                }
            }

            // Play hit effect
            PlayHitEffect(transform.position + Vector3.up, Vector3.up);
        }
    }
} 