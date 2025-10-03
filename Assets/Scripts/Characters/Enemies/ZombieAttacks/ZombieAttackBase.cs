using UnityEngine;
using Managers;
using System.Collections;
using System;

namespace Enemies.ZombieAttacks
{  
    /// <summary>
    /// Base class for all zombie attack components.
    /// Provides common functionality for attack range validation, cooldowns, and damage dealing.
    /// Inherits from AttackBase for shared attack functionality.
    /// </summary>
    public abstract class ZombieAttackBase : AttackBase
    {

        protected Zombie zombie;
        protected Animator animator;
        protected Transform target;

        protected override void Awake()
        {
            base.Awake();
            // Override in child classes to set default attack types
        }

        private EffectPlayer startEffectPlayer;
        private EffectPlayer attackEffectPlayer;
        private EffectPlayer hitEffectPlayer;
        private EffectPlayer endEffectPlayer;

        /// <summary>
        /// Initialize the attack component with the zombie reference
        /// </summary>
        /// <param name="enemy">The enemy that owns this attack</param>
        public override void Initialize(EnemyBase enemy)
        {
            if (enemy is Zombie zombieEnemy)
            {
                this.zombie = zombieEnemy;
                this.animator = zombieEnemy.GetComponent<Animator>();
                this.target = zombieEnemy.NavMeshTarget;
                
                // If no attack origin is set, use the zombie's transform
                if (attackOrigin == null)
                {
                    attackOrigin = zombieEnemy.transform;
                }

                // Initialize effect players
                startEffectPlayer = new EffectPlayer(this, startEffect, startEffectDelay);
                attackEffectPlayer = new EffectPlayer(this, attackEffect, attackEffectDelay);
                hitEffectPlayer = new EffectPlayer(this, hitEffect, hitEffectDelay);
                endEffectPlayer = new EffectPlayer(this, endEffect, endEffectDelay);
            }
            else
            {
                Debug.LogError($"[{gameObject.name}] ZombieAttackBase can only be initialized with a Zombie enemy");
            }
        }

        /// <summary>
        /// Check if this attack can be performed
        /// </summary>
        /// <returns>True if the attack can be performed</returns>
        public override bool CanAttack()
        {
            if (target == null || zombie == null) return false;
            if (zombie.Health <= 0) return false;
            
            // Simple distance check - use raw distance without complex calculations
            float distance = Vector3.Distance(zombie.transform.position, target.position);
            
            return distance <= range && base.CanAttack();
        }

        /// <summary>
        /// Check if the zombie is properly facing the target for this attack
        /// </summary>
        /// <param name="angleThreshold">Maximum angle deviation for attack</param>
        /// <returns>True if properly aligned</returns>
        protected virtual bool IsReadyToAttack(float angleThreshold = 30f)
        {
            if (target == null) return false;
            
            return NavigationUtils.IsFacingTarget(zombie.transform, target, angleThreshold, true);
        }

        /// <summary>
        /// Start the attack sequence
        /// </summary>
        public override void StartAttack()
        {
            if (zombie != null && zombie.animator != null)
            {
                zombie.animator.SetInteger("AttackType", attackType);
                zombie.animator.SetTrigger(attackTrigger);
            }
            lastAttackTime = Time.time;
            
            // Mark zombie as attacking
            zombie.SetAttacking(true);
            
            if (zombie.showCollisionDebug)
            {
                Debug.Log($"[{zombie.gameObject.name}] {GetType().Name} StartAttack - SetAttacking(true) | isAttacking: {zombie.isAttacking}");
            }
            
            // Enable attack game objects
            EnableAttackGameObjects();
            
            // Play start effect
            PlayStartEffect();
        }

        /// <summary>
        /// Called when the attack animation reaches the damage dealing frame
        /// Override in child classes for specific attack behavior
        /// </summary>
        public override void OnAttack()
        {
            // Play attack effect
            PlayAttackEffect();
        }

        /// <summary>
        /// Called when the attack animation ends
        /// Override in child classes for specific attack end behavior
        /// </summary>
        public override void OnAttackEnd()
        {
            // Mark zombie as no longer attacking
            if (zombie != null)
            {
                zombie.SetAttacking(false);
                
                if (zombie.showCollisionDebug)
                {
                    Debug.Log($"[{zombie.gameObject.name}] {GetType().Name} OnAttackEnd - SetAttacking(false) | isAttacking: {zombie.isAttacking}");
                }
            }
            
            // Reset animation parameters
            if (zombie != null && zombie.animator != null)
            {
                zombie.animator.SetInteger("AttackType", 0);
            }
            
            // Disable attack game objects
            DisableAttackGameObjects();
            
            // Play end effect
            PlayEndEffect();
        }

        /// <summary>
        /// Deal damage to a single target
        /// </summary>
        /// <param name="target">The target to damage</param>
        /// <param name="damageAmount">Amount of damage to deal</param>
        /// <param name="poiseAmount">Amount of poise damage to deal</param>
        protected virtual void DealDamageToTarget(IDamageable target, float damageAmount, float poiseAmount)
        {
            if (target == null) return;
            
            // Calculate total damage including elemental bonus
            float totalDamage = damageAmount;
            if (attackElement != AttackElement.NONE)
            {
                totalDamage += elementalDamageBonus;
            }
            
            // Apply damage with elemental type
            if (attackElement == AttackElement.NONE || attackElement == AttackElement.PHYSICAL)
            {
                target.TakeDamage(totalDamage, poiseAmount, zombie.transform);
            }
            else
            {
                target.TakeDamage(totalDamage, poiseAmount, attackElement, zombie.transform);
            }
        }

        /// <summary>
        /// Deal damage to all targets in a radius
        /// </summary>
        /// <param name="radius">Radius of the damage area</param>
        /// <param name="damageAmount">Amount of damage to deal</param>
        /// <param name="position">Center position of the damage area</param>
        protected override void DealDamageInRadius(float radius, float damageAmount, Vector3 position)
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
                    
                    // Apply damage with elemental type
                    if (attackElement == AttackElement.NONE || attackElement == AttackElement.PHYSICAL)
                    {
                        damageable.TakeDamage(totalDamage, poiseDamage, zombie.transform);
                    }
                    else
                    {
                        damageable.TakeDamage(totalDamage, poiseDamage, attackElement, zombie.transform);
                    }
                    
                    // Play hit effect at the point of impact
                    Vector3 hitPoint = hitCollider.ClosestPoint(attackPosition);
                    Vector3 hitNormal = (hitPoint - attackPosition).normalized;
                    PlayHitEffect(hitPoint, hitNormal);
                }
            }
        }

        /// <summary>
        /// Play the start effect
        /// </summary>
        protected override void PlayStartEffect(Vector3? position = null, Vector3? direction = null, Quaternion? rotation = null, Transform parent = null)
        {
            startEffectPlayer.Play(position, direction, rotation, parent);
        }

        /// <summary>
        /// Play the attack effect
        /// </summary>
        protected override void PlayAttackEffect(Vector3? position = null, Vector3? direction = null, Quaternion? rotation = null, Transform parent = null)
        {
            attackEffectPlayer.Play(position, direction, rotation, parent);
        }

        /// <summary>
        /// Play the hit effect
        /// </summary>
        protected override void PlayHitEffect(Vector3? position = null, Vector3? direction = null, Quaternion? rotation = null, Transform parent = null)
        {
            hitEffectPlayer.Play(position, direction, rotation, parent);
        }

        /// <summary>
        /// Play the end effect
        /// </summary>
        protected override void PlayEndEffect(Vector3? position = null, Vector3? direction = null, Quaternion? rotation = null, Transform parent = null)
        {
            endEffectPlayer.Play(position, direction, rotation, parent);
        }

        /// <summary>
        /// Update the target reference (called when zombie finds a new target)
        /// </summary>
        public virtual void UpdateTarget(Transform newTarget)
        {
            target = newTarget;
        }

        /// <summary>
        /// Get the effective attack distance considering NavMesh obstacles
        /// </summary>
        /// <returns>The effective distance required to attack the current target</returns>
        protected virtual float CalculateEffectiveAttackDistance()
        {
            if (target == null) return range;
            return NavigationUtils.CalculateEffectiveReachDistance(zombie.transform.position, target, range, 1f);
        }

        /// <summary>
        /// Get the current attack range
        /// </summary>
        /// <returns>Current effective attack range</returns>
        public override float GetCurrentAttackRange()
        {
            return range;
        }

        /// <summary>
        /// Check if the zombie should rotate towards target before attacking
        /// </summary>
        /// <returns>True if rotation is needed</returns>
        public override bool ShouldRotateToAttack()
        {
            if (target == null) return false;
            
            return !IsReadyToAttack();
        }
    }
}
