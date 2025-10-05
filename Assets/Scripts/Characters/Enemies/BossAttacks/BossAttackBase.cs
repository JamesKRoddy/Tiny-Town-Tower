using UnityEngine;
using Managers;
using System.Collections;
using System;

namespace Enemies.BossAttacks
{  
    public abstract class BossAttackBase : AttackBase
    {

        protected Boss boss;
        protected Animator animator;
        protected Transform target;

        private EffectPlayer startEffectPlayer;
        private EffectPlayer attackEffectPlayer;
        private EffectPlayer hitEffectPlayer;
        private EffectPlayer endEffectPlayer;

        public override void Initialize(EnemyBase enemy)
        {
            if (enemy is Boss bossEnemy)
            {
                this.boss = bossEnemy;
                this.animator = bossEnemy.GetComponent<Animator>();
                this.target = bossEnemy.NavMeshTarget;
                
                // If no attack origin is set, use the boss's transform
                if (attackOrigin == null)
                {
                    attackOrigin = bossEnemy.transform;
                }

                // Initialize effect players
                startEffectPlayer = new EffectPlayer(this, startEffect, startEffectDelay);
                attackEffectPlayer = new EffectPlayer(this, attackEffect, attackEffectDelay);
                hitEffectPlayer = new EffectPlayer(this, hitEffect, hitEffectDelay);
                endEffectPlayer = new EffectPlayer(this, endEffect, endEffectDelay);
            }
            else
            {
                Debug.LogError($"[{gameObject.name}] BossAttackBase can only be initialized with a Boss enemy");
            }
        }

        public override bool CanAttack()
        {
            if (target == null) return false;
            
            // Use shared navigation utility for sophisticated distance checking
            float effectiveDistance = NavigationUtils.CalculateEffectiveReachDistance(transform.position, target, maxRange, 1f);
            float distance = Vector3.Distance(transform.position, target.position);
            
            return distance <= effectiveDistance && base.CanAttack();
        }

        /// <summary>
        /// Calculate the effective attack distance considering NavMesh obstacles and their bounds
        /// </summary>
        /// <param name="target">The target transform</param>
        /// <returns>The effective distance required to attack this target</returns>
        protected virtual float CalculateEffectiveAttackDistance(Transform target)
        {
            return NavigationUtils.CalculateEffectiveReachDistance(transform.position, target, maxRange, 1f);
        }

        public override void StartAttack()
        {
            if (animator != null)
            {
                animator.SetInteger("AttackType", attackType);
            }
            lastAttackTime = Time.time;
            
            // Enable attack game objects
            EnableAttackGameObjects();
            
            // Play start effect
            PlayStartEffect();
        }

        public override void OnAttack()
        {
            // Play attack effect
            PlayAttackEffect();
        }

        public override void OnAttackEnd()
        {
            // Disable attack game objects
            DisableAttackGameObjects();
            
            // Play end effect
            PlayEndEffect();
        }

        protected void PlayStartEffect(Vector3? position = null, Vector3? normal = null, Quaternion? rotation = null, Transform parent = null)
        {
            startEffectPlayer.Play(position, normal, rotation, parent);
        }

        protected void PlayAttackEffect(Vector3? position = null, Vector3? normal = null, Quaternion? rotation = null, Transform parent = null)
        {
            attackEffectPlayer.Play(position, normal, rotation, parent);
        }

        protected void PlayHitEffect(Vector3? position = null, Vector3? normal = null, Quaternion? rotation = null, Transform parent = null)
        {
            hitEffectPlayer.Play(position, normal, rotation, parent);
        }

        protected void PlayEndEffect(Vector3? position = null, Vector3? normal = null, Quaternion? rotation = null, Transform parent = null)
        {
            endEffectPlayer.Play(position, normal, rotation, parent);
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
                    // Check if the target is still active (this will catch NPCs in bunkers)
                    if (!hitCollider.gameObject.activeInHierarchy)
                    {
                        continue; // Skip inactive targets (like NPCs in bunkers)
                    }
                    
                    // Calculate total damage including elemental bonus
                    float totalDamage = damageAmount;
                    if (attackElement != AttackElement.NONE)
                    {
                        totalDamage += elementalDamageBonus;
                    }
                    
                    // Boss attacks deal high poise damage
                    float bossPoiseDamage = totalDamage * 0.8f; // 80% of health damage as poise damage
                    
                    // Apply damage with elemental type
                    if (attackElement == AttackElement.NONE || attackElement == AttackElement.PHYSICAL)
                    {
                        damageable.TakeDamage(totalDamage, bossPoiseDamage, transform);
                    }
                    else
                    {
                        damageable.TakeDamage(totalDamage, bossPoiseDamage, attackElement, transform);
                    }
                    
                    // Play hit effect at the point of impact
                    Vector3 hitPoint = hitCollider.ClosestPoint(attackPosition);
                    Vector3 hitNormal = (hitPoint - attackPosition).normalized;
                    PlayHitEffect(hitPoint, hitNormal);
                }
            }
        }

        protected virtual void OnEnable()
        {
            if (attackGameObjects != null)
            {
                foreach (var go in attackGameObjects)
                {
                    if (go != null)
                    {
                        go.SetActive(true);
                    }
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (attackGameObjects != null)
            {
                foreach (var go in attackGameObjects)
                {
                    if (go != null)
                    {
                        go.SetActive(false);
                    }
                }
            }
        }
    }

} 