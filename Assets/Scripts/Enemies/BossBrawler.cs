using UnityEngine;
using Managers;
using UnityEngine.AI;

namespace Enemies
{
    public class BossBrawler : Boss
    {
        [Header("Attack Settings")]
        public float aoeDamage = 10f;
        public float aoeRadius = 5f;
        public float shockwaveDamage = 15f;
        public float shockwaveSpeed = 10f;
        public float shockwaveRange = 15f;
        public GameObject shockwavePrefab; // Assign in inspector

        [Header("Attack Ranges")]
        public float punchRange = 3f; // Short range attack
        public float roarRange = 8f; // Medium range attack
        public float jumpRange = 15f; // Long range attack

        [Header("Attack Timing")]
        public float attackCooldown = 2f; // Time between attacks
        private float lastAttackTime;

        protected override void Awake()
        {
            useRootMotion = true; // Enable root motion for the boss
            base.Awake();
        }

        protected override void Update()
        {
            base.Update();

            if (navMeshTarget != null && !isAttacking && Time.time >= lastAttackTime + attackCooldown)
            {
                float distanceToTarget = Vector3.Distance(transform.position, navMeshTarget.position);
                if (distanceToTarget <= jumpRange) // Only attack if target is within maximum attack range
                {
                    DetermineAttack();
                    lastAttackTime = Time.time;
                }
            }
        }

        void PunchAttack(){
            animator.SetInteger("AttackType", 0);
        }

        void RoarAttack(){
            animator.SetInteger("AttackType", 1);
        }

        void JumpAttack(){
            animator.SetInteger("AttackType", 2);
        }

        // Determine which attack to use based on distance to target
        public void DetermineAttack()
        {
            if (navMeshTarget == null) return;

            float distanceToTarget = Vector3.Distance(transform.position, navMeshTarget.position);

            if (distanceToTarget <= punchRange)
            {
                PunchAttack();
            }
            else if (distanceToTarget <= roarRange)
            {
                RoarAttack();
            }
            else if (distanceToTarget <= jumpRange)
            {
                JumpAttack();
            }
        }

        // Called by animation event for AoE attack
        public void AoEAttack()
        {
            // Find all colliders in the AoE radius
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, aoeRadius);
            
            foreach (var hitCollider in hitColliders)
            {
                IDamageable damageable = hitCollider.GetComponent<IDamageable>();
                if (damageable != null && damageable.GetAllegiance() == Allegiance.FRIENDLY)
                {
                    damageable.TakeDamage(aoeDamage, transform);
                }
            }

            // Play AoE VFX
            EffectManager.Instance.PlayHitEffect(transform.position + Vector3.up, Vector3.up, this);
        }

        // Called by animation event for shockwave attack
        public void GroundShockwave()
        {
            if (shockwavePrefab != null && navMeshTarget != null)
            {
                // Calculate direction to player
                Vector3 direction = (navMeshTarget.position - transform.position).normalized;
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

        private void OnDrawGizmos()
        {
            // Draw AoE attack radius
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // Red with transparency
            Gizmos.DrawWireSphere(transform.position, aoeRadius);
            
            // Draw shockwave range if we have a target
            if (navMeshTarget != null)
            {
                Gizmos.color = new Color(0f, 0f, 1f, 0.3f); // Blue with transparency
                Vector3 direction = (navMeshTarget.position - transform.position).normalized;
                direction.y = 0;
                Gizmos.DrawLine(transform.position, transform.position + direction * shockwaveRange);
            }
        }
    }
}
