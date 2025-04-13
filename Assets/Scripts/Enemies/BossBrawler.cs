using UnityEngine;
using Managers;
using UnityEngine.AI;

namespace Enemies
{
    public class BossBrawler : Boss
    {
        protected override void Awake()
        {
            useRootMotion = true; // Enable root motion for the boss
            base.Awake();
            InitializeAttacks();
        }

        private void InitializeAttacks()
        {
            foreach (var attack in attacks)
            {
                if (attack != null)
                {
                    attack.Initialize(this);
                }
            }
        }

        protected override void Update()
        {
            base.Update();

            if (navMeshTarget != null && !isAttacking)
            {
                // Try to execute the first available attack
                foreach (var attack in attacks)
                {
                    if (attack != null && attack.CanAttack())
                    {
                        attack.Execute();
                        break;
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (attacks != null)
            {
                foreach (var attack in attacks)
                {
                    if (attack != null)
                    {
                        // Draw attack range
                        Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // Red with transparency
                        Gizmos.DrawWireSphere(transform.position, attack.range);
                    }
                }
            }
        }
    }
}
