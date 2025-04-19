using UnityEngine;

namespace Enemies.BossAttacks
{
    public class BossRoarAttack : BossAttackBase
    {
        [Header("Roar Settings")]
        public float aoeRadius = 5f;

        public override void Initialize(Boss boss)
        {
            base.Initialize(boss);
        }

        public override void OnAttack()
        {
            PlayAttackEffect(attackOrigin.position, attackOrigin.forward);
            DealDamageInRadius(aoeRadius, damage, transform.position);
        }
    }
} 