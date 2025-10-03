using UnityEngine;

namespace Enemies.BossAttacks
{
    public class BossRoarAttack : BossAttackBase
    {
        [Header("Roar Settings")]
        public float aoeRadius = 5f;

        public override void Initialize(EnemyBase enemy)
        {
            base.Initialize(enemy);
            
            if (enemy is Boss bossEnemy)
            {
                // Set default elemental damage for roar attacks (can be overridden in inspector)
                if (attackElement == AttackElement.NONE)
                {
                    attackElement = AttackElement.SHADOW; // Roar attacks could be shadow/psychic damage
                }
            }
        }

        public override void OnAttack()
        {
            PlayAttackEffect(attackOrigin.position, attackOrigin.forward);
            DealDamageInRadius(aoeRadius, damage, transform.position);
        }
    }
} 