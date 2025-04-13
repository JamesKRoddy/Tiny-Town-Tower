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

        protected BossBrawler boss;
        protected Animator animator;
        protected Transform target;
        protected float lastAttackTime;

        public virtual void Initialize(BossBrawler boss)
        {
            this.boss = boss;
            this.animator = boss.GetComponent<Animator>();
            this.target = boss.NavMeshTarget;
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

        // Called by animation events
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
    }
} 