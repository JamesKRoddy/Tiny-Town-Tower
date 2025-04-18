using UnityEngine;
using Managers;
using System.Collections;
using System;

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

        [Tooltip("Effect played when the boss attacks")]
        public EffectDefinition attackEffect;
        [Tooltip("Delay in seconds before playing the attack effect")]
        public float attackEffectDelay = 0f;
        
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

        private EffectPlayer startEffectPlayer;
        private EffectPlayer attackEffectPlayer;
        private EffectPlayer hitEffectPlayer;
        private EffectPlayer endEffectPlayer;

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

            // Initialize effect players
            startEffectPlayer = new EffectPlayer(this, startEffect, startEffectDelay);
            attackEffectPlayer = new EffectPlayer(this, attackEffect, attackEffectDelay);
            hitEffectPlayer = new EffectPlayer(this, hitEffect, hitEffectDelay);
            endEffectPlayer = new EffectPlayer(this, endEffect, endEffectDelay);
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

        public virtual void OnAttack()
        {
            // Override in child classes for specific attack start behavior
        }

        public virtual void OnAttackEnd()
        {
            // Override in child classes for specific attack end behavior
        }

        protected void PlayStartEffect(Vector3? position = null, Vector3? normal = null)
        {
            startEffectPlayer.Play(position, normal);
        }

        protected void PlayAttackEffect(Vector3? position = null, Vector3? normal = null)
        {
            attackEffectPlayer.Play(position, normal);
        }

        protected void PlayHitEffect(Vector3? position = null, Vector3? normal = null)
        {
            hitEffectPlayer.Play(position, normal);
        }

        protected void PlayEndEffect(Vector3? position = null, Vector3? normal = null)
        {
            endEffectPlayer.Play(position, normal);
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
                    PlayHitEffect(hitPoint, hitNormal);
                }
            }
        }
    }

    public class EffectPlayer
    {
        private readonly MonoBehaviour owner;
        private readonly EffectDefinition effect;
        private readonly float delay;
        private Coroutine activeCoroutine;

        public EffectPlayer(MonoBehaviour owner, EffectDefinition effect, float delay)
        {
            this.owner = owner;
            this.effect = effect;
            this.delay = delay;
        }

        public void Play(Vector3? position = null, Vector3? normal = null)
        {
            if (effect == null) return;
            
            Vector3 effectPosition = position ?? owner.transform.position;
            Vector3 effectNormal = normal ?? owner.transform.forward;
            
            activeCoroutine = owner.StartCoroutine(PlayWithDelay(effectPosition, effectNormal));
        }

        public void Stop()
        {
            if (activeCoroutine != null)
            {
                owner.StopCoroutine(activeCoroutine);
                activeCoroutine = null;
            }
        }

        private IEnumerator PlayWithDelay(Vector3 position, Vector3 normal)
        {
            if (delay > 0)
            {
                yield return new WaitForSeconds(delay);
            }
            EffectManager.Instance.PlayEffect(position, normal, effect);
        }
    }
} 