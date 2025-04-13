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
            DealDamageInRadius(aoeRadius, aoeDamage, transform.position);
        }
    }
} 