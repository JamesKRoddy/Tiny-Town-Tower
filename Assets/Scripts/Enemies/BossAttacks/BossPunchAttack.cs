using UnityEngine;

namespace Enemies.BossAttacks
{
    public class BossPunchAttack : BossAttackBase
    {
        [Header("Punch Settings")]
        public float punchRadius = 2f;

        public override void StartAttack()
        {
            base.StartAttack();
            PlayStartEffect(attackOrigin.position, attackOrigin.forward, attackOrigin.rotation);
        }

        public override void Initialize(Boss boss)
        {
            base.Initialize(boss);
        }

        public override void OnAttack()
        {
            DealDamageInRadius(punchRadius, damage, transform.position);
        }
    }
} 