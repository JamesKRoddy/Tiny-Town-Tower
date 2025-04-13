using UnityEngine;

namespace Enemies.BossAttacks
{
    public class BossJumpAttack : BossAttackBase
    {
        [Header("Jump Attack Settings")]
        public float shockwaveDamage = 15f;
        public float shockwaveSpeed = 10f;
        public float shockwaveRange = 15f;
        public GameObject shockwavePrefab; // Assign in inspector

        public override void Initialize(BossBrawler boss)
        {
            base.Initialize(boss);
            attackType = 2; // Set to match the animator parameter for jump
        }

        public override void OnAttackStart()
        {
            if (shockwavePrefab != null && target != null)
            {
                // Calculate direction to player
                Vector3 direction = (target.position - transform.position).normalized;
                direction.y = 0; // Keep the shockwave horizontal

                // Spawn shockwave
                GameObject shockwave = Instantiate(shockwavePrefab, transform.position, Quaternion.LookRotation(direction));
                BossShockwave shockwaveComponent = shockwave.GetComponent<BossShockwave>();
                
                if (shockwaveComponent != null)
                {
                    shockwaveComponent.Initialize(direction, shockwaveSpeed, shockwaveRange, shockwaveDamage);
                }
            }
        }
    }
} 