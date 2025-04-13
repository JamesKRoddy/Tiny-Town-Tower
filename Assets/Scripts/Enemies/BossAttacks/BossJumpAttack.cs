using UnityEngine;

namespace Enemies.BossAttacks
{
    public class BossJumpAttack : BossAttackBase
    {
        [Header("Jump Settings")]
        public float jumpRadius = 3f;
        public float jumpDamage = 15f;

        public override void Initialize(BossBrawler boss)
        {
            base.Initialize(boss);
            attackType = 2; // Set to match the animator parameter for jump
        }

        public override void OnAttackStart()
        {
            DealDamageInRadius(jumpRadius, jumpDamage, transform.position);
        }
    }
} 