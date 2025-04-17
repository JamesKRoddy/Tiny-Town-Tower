using UnityEngine;
using Managers;
using System.Collections;

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

        [Header("Attack Effects")]
        [Tooltip("Effect played when the attack starts")]
        public EffectDefinition startEffect;
        [Tooltip("Delay in seconds before playing the start effect")]
        public float startEffectDelay = 0f;
        
        [Tooltip("Effect played when the attack hits")]
        public EffectDefinition hitEffect;
        [Tooltip("Delay in seconds before playing the hit effect")]
        public float hitEffectDelay = 0f;
        
        [Tooltip("Effect played when the attack ends")]
        public EffectDefinition endEffect;
        [Tooltip("Delay in seconds before playing the end effect")]
        public float endEffectDelay = 0f;

        protected Boss boss;
        protected Animator animator;
        protected Transform target;
        protected float lastAttackTime;
        protected Coroutine startEffectCoroutine; // Track the start effect coroutine
        protected Coroutine endEffectCoroutine;   // Track the end effect coroutine
        protected Coroutine hitEffectCoroutine;   // Track the hit effect coroutine

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
            StartCoroutine(PlayEndEffectWithDelay());
        }

        private IEnumerator PlayEffectWithDelay(EffectDefinition effect, Vector3 position, Vector3 direction, float delay)
        {
            if (delay > 0)
            {
                yield return new WaitForSeconds(delay);
            }
            if (effect != null)
            {
                EffectManager.Instance.PlayEffect(position, direction, effect);
            }
        }

        protected IEnumerator PlayStartEffectWithDelay()
        {
            yield return PlayEffectWithDelay(startEffect, attackOrigin.position, attackOrigin.forward, startEffectDelay);
        }

        protected IEnumerator PlayHitEffectWithDelay(Vector3 position, Vector3 normal)
        {
            yield return PlayEffectWithDelay(hitEffect, position, normal, hitEffectDelay);
        }

        protected IEnumerator PlayEndEffectWithDelay()
        {
            yield return PlayEffectWithDelay(endEffect, attackOrigin.position, attackOrigin.forward, endEffectDelay);
        }

        protected void PlayStartEffect()
        {
            if (startEffect != null)
            {
                startEffectCoroutine = StartCoroutine(PlayStartEffectWithDelay());
            }
        }

        protected void PlayHitEffect(Vector3 position, Vector3 normal)
        {
            if (hitEffect != null)
            {
                hitEffectCoroutine = StartCoroutine(PlayHitEffectWithDelay(position, normal));
            }
        }

        protected void PlayEndEffect()
        {
            Debug.Log("[BossAttackBase] Playing end effect");
            if (endEffect != null)
            {
                endEffectCoroutine = StartCoroutine(PlayEndEffectWithDelay());
            }
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
                    // Play hit effect at the point of impact
                    Vector3 hitPoint = hitCollider.ClosestPoint(attackPosition);
                    Vector3 hitNormal = (hitPoint - attackPosition).normalized;
                    StartCoroutine(PlayHitEffectWithDelay(hitPoint, hitNormal));
                }
            }
        }
    }
} 