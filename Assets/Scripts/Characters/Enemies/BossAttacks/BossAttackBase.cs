using UnityEngine;
using Managers;
using System.Collections;
using System;

namespace Enemies.BossAttacks
{  
    public abstract class BossAttackBase : AttackBase
    {

        protected Boss boss;

        public override void Initialize(EnemyBase enemy)
        {
            base.Initialize(enemy);
            
            if (enemy is Boss bossEnemy)
            {
                this.boss = bossEnemy;
            }
            else
            {
                Debug.LogError($"[{gameObject.name}] BossAttackBase can only be initialized with a Boss enemy");
            }
        }

        public override bool CanAttack()
        {
            // Use base class CanAttack() which already handles range checking
            return base.CanAttack();
        }


        public override void StartAttack()
        {
            // Use base class StartAttack() which handles all the standard functionality
            base.StartAttack();
        }

        public override void OnAttack()
        {
            // Use base class OnAttack() which handles attack effects
            base.OnAttack();
        }

        public override void OnAttackEnd()
        {
            // Use base class OnAttackEnd() which handles cleanup
            base.OnAttackEnd();
        }


    }

} 