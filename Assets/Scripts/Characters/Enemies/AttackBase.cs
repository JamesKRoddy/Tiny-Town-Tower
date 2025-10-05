using UnityEngine;
using Managers;
using System.Collections;
using System;

namespace Enemies
{
    /// <summary>
    /// Base class for all enemy attack components.
    /// Contains common attack properties and functionality shared between boss and zombie attacks.
    /// </summary>
    public abstract class AttackBase : MonoBehaviour
    {
        [Header("Attack Settings")]
        [Tooltip("Minimum range for this attack (0 = no minimum)")]
        public float minRange = 0f;
        [Tooltip("Maximum range for this attack")]
        public float maxRange = 5f;
        [Tooltip("Maximum angle deviation for attacks (degrees)")]
        public float attackAngleThreshold = 30f;
        [Tooltip("Cooldown in seconds between attacks of this type")]
        public float cooldown = 2f;
        [Tooltip("Base damage dealt by this attack")]
        public float damage = 10f;
        [Tooltip("Poise damage dealt by this attack")]
        public float poiseDamage = 15f;
        [Tooltip("Attack type ID for animator parameter (0 = default attack)")]
        public int attackType = 0;
        [Tooltip("Optional transform to use as the attack origin. If not set, will use the enemy's transform.")]
        public Transform attackOrigin;
        
        [Header("Elemental Damage")]
        [Tooltip("The elemental type of this attack. NONE means physical damage only.")]
        public AttackElement attackElement = AttackElement.NONE;
        [Tooltip("Additional elemental damage bonus (added to base damage)")]
        [Range(0, 50)]
        public int elementalDamageBonus = 0;

        [Header("Attack Effects")]
        [Tooltip("Effect played when the attack starts")]
        public EffectDefinition startEffect;
        [Tooltip("Delay in seconds before playing the start effect")]
        public float startEffectDelay = 0f;
        [Tooltip("Effect played when the enemy attacks")]
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

        [Header("Animation Settings")]
        [Tooltip("Animation trigger name for this attack")]
        public string attackTrigger = "Attack";

        [Header("Attack Game Objects")]
        [Tooltip("Game objects that will be enabled when this attack is active")]
        public GameObject[] attackGameObjects;

        protected float lastAttackTime;
        protected EnemyBase enemy;
        protected Animator animator;
        protected Transform target;

        private EffectPlayer startEffectPlayer;
        private EffectPlayer attackEffectPlayer;
        private EffectPlayer hitEffectPlayer;
        private EffectPlayer endEffectPlayer;

        protected virtual void Awake()
        {
            // Override in child classes to set default attack types
        }

        /// <summary>
        /// Initialize the attack with the enemy reference
        /// </summary>
        /// <param name="enemy">The enemy that owns this attack</param>
        public virtual void Initialize(EnemyBase enemy)
        {
            this.enemy = enemy;
            this.animator = enemy.GetComponent<Animator>();
            this.target = enemy.NavMeshTarget;
            
            // If no attack origin is set, use the enemy's transform
            if (attackOrigin == null)
            {
                attackOrigin = enemy.transform;
            }

            // Initialize effect players
            startEffectPlayer = new EffectPlayer(this, startEffect, startEffectDelay);
            attackEffectPlayer = new EffectPlayer(this, attackEffect, attackEffectDelay);
            hitEffectPlayer = new EffectPlayer(this, hitEffect, hitEffectDelay);
            endEffectPlayer = new EffectPlayer(this, endEffect, endEffectDelay);
        }

        /// <summary>
        /// Check if this attack can be used right now
        /// </summary>
        /// <returns>True if the attack can be used</returns>
        public virtual bool CanAttack()
        {
            if (target == null || enemy == null) return false;
            if (enemy.Health <= 0) return false;
            
            // Simple distance check - use raw distance without complex calculations
            float distance = Vector3.Distance(enemy.transform.position, target.position);
            
            // Check if within attack range (minRange to maxRange)
            bool inRange = distance >= minRange && distance <= maxRange;
            
            return inRange && Time.time - lastAttackTime >= cooldown;
        }

        /// <summary>
        /// Start the attack sequence
        /// </summary>
        public virtual void StartAttack()
        {
            if (enemy != null && animator != null)
            {
                animator.SetInteger("AttackType", attackType);
                animator.SetTrigger(attackTrigger);
            }
            lastAttackTime = Time.time;
            
            // Mark enemy as attacking
            enemy.isAttacking = true;
            
            // Enable attack game objects
            EnableAttackGameObjects();
            
            // Play start effect
            PlayStartEffect();
        }

        /// <summary>
        /// Called when the attack animation reaches the damage dealing frame
        /// Override in child classes for specific attack behavior
        /// </summary>
        public virtual void OnAttack()
        {
            // Play attack effect
            PlayAttackEffect();
        }

        /// <summary>
        /// Called when the attack animation ends
        /// Override in child classes for specific attack end behavior
        /// </summary>
        public virtual void OnAttackEnd()
        {
            // Mark enemy as no longer attacking
            if (enemy != null)
            {
                enemy.isAttacking = false;
            }
            
            // Reset animation parameters
            if (enemy != null && animator != null)
            {
                animator.SetInteger("AttackType", 0);
            }
            
            // Disable attack game objects
            DisableAttackGameObjects();
            
            // Play end effect
            PlayEndEffect();
        }

        /// <summary>
        /// Get the current attack range
        /// </summary>
        /// <returns>Current effective attack range</returns>
        public virtual float GetCurrentAttackRange()
        {
            return maxRange;
        }

        /// <summary>
        /// Check if the enemy should rotate towards target before attacking
        /// </summary>
        /// <returns>True if rotation is needed</returns>
        public virtual bool ShouldRotateToAttack()
        {
            if (target == null) return false;
            
            return !IsReadyToAttack();
        }

        /// <summary>
        /// Check if the enemy is properly facing the target for this attack
        /// </summary>
        /// <returns>True if properly aligned</returns>
        protected virtual bool IsReadyToAttack()
        {
            if (target == null) return false;
            
            return NavigationUtils.IsFacingTarget(enemy.transform, target, attackAngleThreshold, true);
        }

        /// <summary>
        /// Update the target reference (called when enemy finds a new target)
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
            if (target == null) return maxRange;
            return NavigationUtils.CalculateEffectiveReachDistance(enemy.transform.position, target, maxRange, 1f);
        }

        /// <summary>
        /// Check if the target is within the attack range
        /// </summary>
        /// <returns>True if target is within range</returns>
        protected virtual bool IsTargetInRange()
        {
            if (target == null) return false;
            
            float distance = Vector3.Distance(enemy.transform.position, target.position);
            return distance >= minRange && distance <= maxRange;
        }

        /// <summary>
        /// Check if the target is too close (within minimum range)
        /// </summary>
        /// <returns>True if target is too close</returns>
        public virtual bool IsTargetTooClose()
        {
            if (target == null || minRange <= 0) return false;
            
            float distance = Vector3.Distance(enemy.transform.position, target.position);
            return distance < minRange;
        }

        /// <summary>
        /// Check if the target is too far (beyond maximum range)
        /// </summary>
        /// <returns>True if target is too far</returns>
        public virtual bool IsTargetTooFar()
        {
            if (target == null) return false;
            
            float distance = Vector3.Distance(enemy.transform.position, target.position);
            return distance > maxRange;
        }

        /// <summary>
        /// Called by Unity for IK (Inverse Kinematics) updates
        /// Override in child classes to implement attack-specific IK behavior (e.g., head tracking)
        /// </summary>
        /// <param name="layerIndex">The IK layer index</param>
        public virtual void OnAnimatorIK(int layerIndex)
        {
            // Override in child classes for specific IK behavior
        }

        /// <summary>
        /// Deal damage to a target
        /// </summary>
        /// <param name="target">The target to damage</param>
        /// <param name="damageAmount">Amount of damage to deal</param>
        protected virtual void DealDamage(IDamageable target, float damageAmount)
        {
            if (target == null) return;

            // Calculate total damage including elemental bonus
            float totalDamage = damageAmount + elementalDamageBonus;

            // Deal damage with elemental type
            target.TakeDamage(totalDamage, poiseDamage, attackElement);
        }

        /// <summary>
        /// Deal damage to a single target with enhanced parameters
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
                target.TakeDamage(totalDamage, poiseAmount, enemy.transform);
            }
            else
            {
                target.TakeDamage(totalDamage, poiseAmount, attackElement, enemy.transform);
            }
        }

        /// <summary>
        /// Deal damage to all targets in a radius
        /// </summary>
        /// <param name="radius">Radius of the damage area</param>
        /// <param name="damageAmount">Amount of damage to deal</param>
        /// <param name="position">Center position of the damage area</param>
        protected virtual void DealDamageInRadius(float radius, float damageAmount, Vector3 position)
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
                        damageable.TakeDamage(totalDamage, poiseDamage, enemy.transform);
                    }
                    else
                    {
                        damageable.TakeDamage(totalDamage, poiseDamage, attackElement, enemy.transform);
                    }
                    
                    // Play hit effect at the point of impact
                    Vector3 hitPoint = hitCollider.ClosestPoint(attackPosition);
                    Vector3 hitNormal = (hitPoint - attackPosition).normalized;
                    PlayHitEffect(hitPoint, hitNormal);
                }
            }
        }

        /// <summary>
        /// Play an effect with optional delay
        /// </summary>
        /// <param name="effect">Effect to play</param>
        /// <param name="delay">Delay before playing</param>
        /// <param name="position">Position to play at</param>
        /// <param name="direction">Direction for the effect</param>
        /// <param name="rotation">Rotation for the effect</param>
        /// <param name="parent">Parent transform for the effect</param>
        protected virtual void PlayEffect(EffectDefinition effect, float delay, Vector3 position, Vector3 direction, Quaternion rotation, Transform parent = null)
        {
            if (effect != null)
            {
                if (delay > 0)
                {
                    StartCoroutine(PlayEffectDelayed(effect, delay, position, direction, rotation, parent));
                }
                else
                {
                    EffectManager.Instance.PlayEffect(position, direction, rotation, parent, effect);
                }
            }
        }

        /// <summary>
        /// Coroutine to play an effect after a delay
        /// </summary>
        private System.Collections.IEnumerator PlayEffectDelayed(EffectDefinition effect, float delay, Vector3 position, Vector3 direction, Quaternion rotation, Transform parent)
        {
            yield return new WaitForSeconds(delay);
            if (effect != null)
            {
                EffectManager.Instance.PlayEffect(position, direction, rotation, parent, effect);
            }
        }

        /// <summary>
        /// Play start effect
        /// </summary>
        protected virtual void PlayStartEffect(Vector3? position = null, Vector3? direction = null, Quaternion? rotation = null, Transform parent = null)
        {
            startEffectPlayer.Play(position, direction, rotation, parent);
        }

        /// <summary>
        /// Play attack effect
        /// </summary>
        protected virtual void PlayAttackEffect(Vector3? position = null, Vector3? direction = null, Quaternion? rotation = null, Transform parent = null)
        {
            attackEffectPlayer.Play(position, direction, rotation, parent);
        }

        /// <summary>
        /// Play hit effect
        /// </summary>
        protected virtual void PlayHitEffect(Vector3? position = null, Vector3? direction = null, Quaternion? rotation = null, Transform parent = null)
        {
            hitEffectPlayer.Play(position, direction, rotation, parent);
        }

        /// <summary>
        /// Play end effect
        /// </summary>
        protected virtual void PlayEndEffect(Vector3? position = null, Vector3? direction = null, Quaternion? rotation = null, Transform parent = null)
        {
            endEffectPlayer.Play(position, direction, rotation, parent);
        }

        /// <summary>
        /// Enable attack game objects
        /// </summary>
        protected virtual void EnableAttackGameObjects()
        {
            foreach (GameObject obj in attackGameObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                }
            }
        }

        /// <summary>
        /// Disable attack game objects
        /// </summary>
        protected virtual void DisableAttackGameObjects()
        {
            foreach (GameObject obj in attackGameObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Draw attack range gizmo
        /// </summary>
        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, maxRange);
        }
    }
}
