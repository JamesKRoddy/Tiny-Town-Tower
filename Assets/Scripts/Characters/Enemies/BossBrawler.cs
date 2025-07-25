using UnityEngine;
using Managers;
using UnityEngine.AI;
using Enemies.BossAttacks;

namespace Enemies
{
    public class BossBrawler : Boss
    {
        protected override void Awake()
        {
            useRootMotion = true; // Enable root motion for the boss
            base.Awake();
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
                        SetCurrentAttack(attack);
                        
                        // Use sophisticated distance checking that considers obstacles
                        float effectiveAttackDistance = NavigationUtils.CalculateEffectiveReachDistance(transform.position, navMeshTarget, stoppingDistance, obstacleBoundsOffset);
                        agent.stoppingDistance = Mathf.Max(attack.range, effectiveAttackDistance);
                        
                        BeginAttackSequence();
                        // Speed will be set to 0 by UpdateAnimationParameters when attacking
                        attack.StartAttack();
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
