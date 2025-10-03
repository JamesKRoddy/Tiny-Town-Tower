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
            PlayStartEffect(attackOrigin.position, attackOrigin.forward, attackOrigin.rotation, attackOrigin);
        }

        public override void Initialize(EnemyBase enemy)
        {
            base.Initialize(enemy);
            
            if (enemy is Boss bossEnemy)
            {
                // Set default elemental damage for punch attacks (can be overridden in inspector)
                if (attackElement == AttackElement.NONE)
                {
                    attackElement = AttackElement.PHYSICAL;
                }
            }
        }

        public override void OnAttack()
        {
            DealDamageInRadius(punchRadius, damage, transform.position);
        }
    }
} 