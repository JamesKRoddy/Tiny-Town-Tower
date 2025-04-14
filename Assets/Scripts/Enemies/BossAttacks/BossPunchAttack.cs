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
            attackType = 0; // Set to match the animator parameter for punch
        }

        public override void OnAttackStart()
        {
            DealDamageInRadius(punchRadius, damage, transform.position);
        }
    }
} 