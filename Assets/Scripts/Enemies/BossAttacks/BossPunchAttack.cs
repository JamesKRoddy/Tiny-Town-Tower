using UnityEngine;

namespace Enemies.BossAttacks
{
    public class BossPunchAttack : BossAttackBase
    {
        [Header("Punch Settings")]
        public float punchRadius = 2f;

        public override void Initialize(Boss boss)
        {
            base.Initialize(boss);
        }

        public override void OnAttackStart()
        {
            DealDamageInRadius(punchRadius, damage, transform.position);
        }
    }
} 