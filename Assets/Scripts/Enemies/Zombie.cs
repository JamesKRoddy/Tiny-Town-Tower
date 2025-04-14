using UnityEngine;
using UnityEngine.AI;
using System.Collections;

namespace Enemies
{
    public class Zombie : EnemyBase
    {
        public float attackRange = 2f;

        protected override void Awake()
        {
            useRootMotion = true; // Enable root motion for the zombie
            base.Awake();
        }

        protected override void Update()
        {
            base.Update(); // Call base Update to handle destination setting

            // Check for attack range
            if (navMeshTarget != null && Vector3.Distance(transform.position, navMeshTarget.position) <= attackRange && !isAttacking)
            {
                StartAttack();
            }
        }

        protected virtual void MoveTowardsPlayer()
        {
            if (!isAttacking)
            {
                // The NavMeshAgent moves the zombie, but root motion from animation drives actual movement
                SetEnemyDestination(navMeshTarget.position);
            }
        }

    }
}
