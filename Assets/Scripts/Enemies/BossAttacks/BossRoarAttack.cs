using UnityEngine;

namespace Enemies.BossAttacks
{
    public class BossRoarAttack : BossAttackBase
    {
        [Header("Roar Settings")]
        public float aoeRadius = 5f;
        public float aoeDamage = 10f;

        public override void Initialize(Boss boss)
        {
            base.Initialize(boss);
        }

        public override void OnAttackStart()
        {
            DealDamageInRadius(aoeRadius, aoeDamage, transform.position);
        }
    }
} 