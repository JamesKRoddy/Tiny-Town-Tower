using UnityEngine;
using Managers;
using System.Collections;
using System;

namespace Enemies.ZombieAttacks
{
    [RequireComponent(typeof(Zombie))]
    /// <summary>
    /// Base class for all zombie attack components.
    /// Provides zombie-specific functionality on top of the generic AttackBase.
    /// </summary>
    public abstract class ZombieAttackBase : AttackBase
    {
        protected Zombie zombie;

        protected override void Awake()
        {
            base.Awake();
            // Override in child classes to set default attack types
        }

        /// <summary>
        /// Initialize the attack component with the zombie reference
        /// </summary>
        /// <param name="enemy">The enemy that owns this attack</param>
        public override void Initialize(EnemyBase enemy)
        {
            base.Initialize(enemy);
            
            if (enemy is Zombie zombieEnemy)
            {
                this.zombie = zombieEnemy;
            }
            else
            {
                Debug.LogError($"[{gameObject.name}] ZombieAttackBase can only be initialized with a Zombie enemy");
            }
        }

        /// <summary>
        /// Start the attack sequence with zombie-specific debug logging
        /// </summary>
        public override void StartAttack()
        {
            base.StartAttack();
            
            if (zombie != null && zombie.showCollisionDebug)
            {
                Debug.Log($"[{zombie.gameObject.name}] {GetType().Name} StartAttack - isAttacking: {zombie.isAttacking}");
            }
        }

        /// <summary>
        /// Called when the attack animation ends with zombie-specific debug logging
        /// </summary>
        public override void OnAttackEnd()
        {
            base.OnAttackEnd();
            
            if (zombie != null && zombie.showCollisionDebug)
            {
                Debug.Log($"[{zombie.gameObject.name}] {GetType().Name} OnAttackEnd - isAttacking: {zombie.isAttacking}");
            }
        }

    }
}
