using UnityEngine;
using Managers;

namespace Enemies.BossAttacks
{
    public abstract class BossAttackBase : MonoBehaviour
    {
        [Header("Attack Settings")]
        public float range = 5f;
        public float cooldown = 2f;
        public float damage = 10f;
        public int attackType = 0; // Used to set the animator parameter
        [Tooltip("Optional transform to use as the attack origin. If not set, will use the boss's transform.")]
        public Transform attackOrigin;

        protected Boss boss;
        protected Animator animator;
        protected Transform target;
        protected float lastAttackTime;

        public virtual void Initialize(Boss boss)
        {
            this.boss = boss;
            this.animator = boss.GetComponent<Animator>();
            this.target = boss.NavMeshTarget;
            
            // If no attack origin is set, use the boss's transform
            if (attackOrigin == null)
            {
                attackOrigin = boss.transform;
            }
        }

        public virtual bool CanAttack()
        {
            if (target == null) return false;
            float distance = Vector3.Distance(transform.position, target.position);
            return distance <= range && Time.time >= lastAttackTime + cooldown;
        }

        public virtual void Execute()
        {
            if (animator != null)
            {
                animator.SetInteger("AttackType", attackType);
            }
            lastAttackTime = Time.time;
        }

        public virtual void OnAttackStart()
        {
            // Override in child classes for specific attack start behavior
        }

        public virtual void OnAttackEnd()
        {
            // Override in child classes for specific attack end behavior
        }

        protected void PlayHitEffect(Vector3 position, Vector3 normal)
        {
            EffectManager.Instance.PlayHitEffect(position, normal, boss);
        }

        protected void DealDamageInRadius(float radius, float damageAmount, Vector3 position)
        {
            // Use the attack origin's position if provided, otherwise use the given position
            Vector3 attackPosition = attackOrigin != null ? attackOrigin.position : position;
            
            // Find all colliders in the radius
            Collider[] hitColliders = Physics.OverlapSphere(attackPosition, radius);
            
            foreach (var hitCollider in hitColliders)
            {
                IDamageable damageable = hitCollider.GetComponent<IDamageable>();
                if (damageable != null && damageable.GetAllegiance() == Allegiance.FRIENDLY)
                {
                    damageable.TakeDamage(damageAmount, transform);
                }
            }

            // Play hit effect at the attack origin
            PlayHitEffect(attackPosition + Vector3.up, Vector3.up);
        }
    }
} 