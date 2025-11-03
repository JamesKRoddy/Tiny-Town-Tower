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
    public abstract class AttackBase : MonoBehaviour, IDamageDealer
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
        [Tooltip("Allow NavMeshAgent to drive rotation during attack (useful for tracking moving targets)")]
        public bool allowRotationDuringAttack = false;
        
        [Header("Elemental Damage")]
        [Tooltip("The elemental type of this attack. NONE means physical damage only.")]
        public AttackElement attackElement = AttackElement.NONE;
        [Tooltip("Additional elemental damage bonus (added to base damage)")]
        [Range(0, 50)]
        public int elementalDamageBonus = 0;

        [Header("Attack Effects")]
        [Tooltip("Effect played when the attack starts")]
        public EffectSpawnData startEffect;
        [Tooltip("Delay in seconds before playing the start effect")]
        public float startEffectDelay = 0f;
        [Tooltip("Effect played when the enemy attacks")]
        public EffectSpawnData attackEffect;
        [Tooltip("Delay in seconds before playing the attack effect")]
        public float attackEffectDelay = 0f;
        [Tooltip("Effect played when the attack hits")]
        public EffectSpawnData hitEffect;
        [Tooltip("Delay in seconds before playing the hit effect")]
        public float hitEffectDelay = 0f;
        [Tooltip("Effect played when the attack ends")]
        public EffectSpawnData endEffect;
        [Tooltip("Delay in seconds before playing the end effect")]
        public float endEffectDelay = 0f;

        [Header("Animation Settings")]
        [Tooltip("Animation trigger name for this attack")]
        public string attackTrigger = "Attack";

        [Header("Attack Game Objects")]
        [Tooltip("Game objects that will be enabled when this attack is active")]
        public GameObject[] attackGameObjects;

        /// <summary>
        /// Time when this attack was last executed (used for cooldown calculations)
        /// Made public so EnemyBase can check actual cooldown status
        /// </summary>
        public float lastAttackTime;
        protected EnemyBase enemy;
        protected Animator animator;
        protected Transform target;
        
        // Public property for external access
        public Transform Target => target;
        
        // IDamageDealer implementation
        public float BaseDamage => damage;
        public float PoiseDamage => poiseDamage;
        public AttackElement ElementType => attackElement;
        public int ElementalDamageBonus => elementalDamageBonus;
        public Transform DamageSource => attackOrigin != null ? attackOrigin : enemy?.transform;
        public Allegiance DealerAllegiance => Allegiance.HOSTILE; // Enemy attacks are hostile

        [Tooltip("VFX for the start of the attack")]
        private EffectPlayer startEffectPlayer;
        [Tooltip("VFX for the attack")]
        private EffectPlayer attackEffectPlayer;
        [Tooltip("VFX for when the attack hits its target")]
        private EffectPlayer hitEffectPlayer;
        [Tooltip("VFX for the end of the attack")]
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

            // Initialize effect players - convert EffectSpawnData to EffectDefinition for EffectPlayer
            startEffectPlayer = new EffectPlayer(this, startEffect?.effectDefinition, startEffectDelay);
            attackEffectPlayer = new EffectPlayer(this, attackEffect?.effectDefinition, attackEffectDelay);
            hitEffectPlayer = new EffectPlayer(this, hitEffect?.effectDefinition, hitEffectDelay);
            endEffectPlayer = new EffectPlayer(this, endEffect?.effectDefinition, endEffectDelay);
        }

        /// <summary>
        /// Check if this attack can be used right now
        /// </summary>
        /// <returns>True if the attack can be used</returns>
        public virtual bool CanAttack()
        {
            if (target == null || enemy == null) return false;
            if (enemy.Health <= 0) return false;
            
            // Use obstacle-aware range check for targets with NavMesh obstacles (like buildings)
            // This ensures enemies can attack buildings even though they can't path directly to the center
            bool inRange = DamageUtils.IsInRangeWithObstacles(enemy.transform.position, target, minRange, maxRange);
            bool cooldownReady = DamageUtils.IsCooldownReady(lastAttackTime, cooldown);
            
            // For ranged attacks (attacks with minimum range > 0), check line of sight
            // Melee attacks don't need line of sight since they're close-range
            bool hasLineOfSight = true;
            if (minRange > 0) // Ranged attack
            {
                hasLineOfSight = enemy.HasLineOfSight(target.position);
            }
            
            return inRange && cooldownReady && hasLineOfSight;
        }

        /// <summary>
        /// Start the attack sequence
        /// </summary>
        public virtual void StartAttack()
        {
            if (enemy != null && animator != null)
            {
                animator.SetInteger(GameConstants.AnimatorParams.AttackTypeHash, attackType);
                animator.SetTrigger(attackTrigger);
            }
            lastAttackTime = Time.time;
            
            // Mark enemy as attacking
            enemy.isAttacking = true;
            
            // Note: Rotation during attack is now handled by UpdateDuringAttack() using Quaternion.Lerp
            // This approach doesn't conflict with NavMeshAgent rotation settings
            
            // Enable attack game objects
            EnableAttackGameObjects();
            
            // Play start effect
            PlayStartEffect();
        }

        /// <summary>
        /// Called during Update while attacking - allows continuous target tracking
        /// </summary>
        public virtual void UpdateDuringAttack()
        {
            // If rotation is allowed during attack, manually rotate towards target using Quaternion.Lerp
            // This works regardless of whether the NavMeshAgent is moving or stopped
            if (allowRotationDuringAttack && enemy != null && target != null)
            {
                // Safety check: Skip if target was destroyed during attack
                if (target.gameObject == null || !target.gameObject.activeInHierarchy)
                {
                    return;
                }
                
                // Calculate direction to target
                Vector3 directionToTarget = (target.position - enemy.transform.position).normalized;
                directionToTarget.y = 0; // Keep rotation on horizontal plane
                
                if (directionToTarget != Vector3.zero)
                {
                    // Calculate target rotation
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                    
                    // Smoothly rotate towards target using enemy's rotation speed
                    enemy.transform.rotation = Quaternion.Lerp(
                        enemy.transform.rotation, 
                        targetRotation, 
                        enemy.rotationSpeed * Time.deltaTime
                    );
                }
            }
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
                animator.SetInteger(GameConstants.AnimatorParams.AttackTypeHash, 0);
            }
            
            // Note: NavMeshAgent rotation is handled by EnemyBase.EndAttack()
            // Manual rotation during attack doesn't interfere with it
            
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
            return DamageUtils.IsInRangeWithObstacles(enemy.transform.position, target, minRange, maxRange);
        }

        /// <summary>
        /// Check if the target is too close (within minimum range)
        /// </summary>
        /// <returns>True if target is too close</returns>
        public virtual bool IsTargetTooClose()
        {
            if (target == null) return false;
            return DamageUtils.IsTooClose(enemy.transform.position, target.position, minRange);
        }

        /// <summary>
        /// Check if the target is too far (beyond maximum range)
        /// </summary>
        /// <returns>True if target is too far</returns>
        public virtual bool IsTargetTooFar()
        {
            if (target == null) return false;
            return DamageUtils.IsTooFar(enemy.transform.position, target.position, maxRange);
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
        /// Deal damage to a target (IDamageDealer interface implementation)
        /// </summary>
        /// <param name="target">The target to damage</param>
        public virtual void DealDamage(IDamageable target)
        {
            DamageUtils.DealDamage(this, target);
        }
        
        /// <summary>
        /// Deal damage to a target with custom damage amounts (IDamageDealer interface implementation)
        /// </summary>
        /// <param name="target">The target to damage</param>
        /// <param name="damageAmount">Custom damage amount</param>
        /// <param name="poiseAmount">Custom poise damage amount</param>
        public virtual void DealDamage(IDamageable target, float damageAmount, float poiseAmount)
        {
            DamageUtils.DealDamage(this, target, damageAmount, poiseAmount);
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
            
            // Use the unified damage system
            int targetsDamaged = DamageUtils.DealDamageInRadius(this, attackPosition, radius, damageAmount, poiseDamage);
            
            // Play hit effect for each target (simplified - could be enhanced to track individual hit points)
            if (targetsDamaged > 0)
            {
                PlayHitEffect(attackPosition, Vector3.up);
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
        /// Play start effect using EffectSpawnData configuration
        /// </summary>
        protected virtual void PlayStartEffect(Vector3? position = null, Vector3? direction = null, Quaternion? rotation = null, Transform parent = null)
        {
            if (startEffect != null && startEffect.IsValid())
            {
                PlayEffectSpawnData(startEffect, startEffectDelay, attackOrigin ?? enemy?.transform);
            }
        }

        /// <summary>
        /// Play attack effect using EffectSpawnData configuration
        /// </summary>
        protected virtual void PlayAttackEffect(Vector3? position = null, Vector3? direction = null, Quaternion? rotation = null, Transform parent = null)
        {
            if (attackEffect != null && attackEffect.IsValid())
            {
                PlayEffectSpawnData(attackEffect, attackEffectDelay, attackOrigin ?? enemy?.transform);
            }
        }

        /// <summary>
        /// Play hit effect using EffectSpawnData configuration
        /// </summary>
        protected virtual void PlayHitEffect(Vector3? position = null, Vector3? direction = null, Quaternion? rotation = null, Transform parent = null)
        {
            if (hitEffect != null && hitEffect.IsValid())
            {
                PlayEffectSpawnData(hitEffect, hitEffectDelay, attackOrigin ?? enemy?.transform);
            }
        }

        /// <summary>
        /// Play end effect using EffectSpawnData configuration
        /// </summary>
        protected virtual void PlayEndEffect(Vector3? position = null, Vector3? direction = null, Quaternion? rotation = null, Transform parent = null)
        {
            if (endEffect != null && endEffect.IsValid())
            {
                PlayEffectSpawnData(endEffect, endEffectDelay, attackOrigin ?? enemy?.transform);
            }
        }
        
        /// <summary>
        /// Play an EffectSpawnData with optional delay
        /// </summary>
        protected virtual void PlayEffectSpawnData(EffectSpawnData effectData, float delay, Transform fallbackTransform)
        {
            if (effectData == null || !effectData.IsValid()) return;
            
            if (delay > 0)
            {
                StartCoroutine(PlayEffectSpawnDataDelayed(effectData, delay, fallbackTransform));
            }
            else
            {
                effectData.SpawnEffect(fallbackTransform);
            }
        }
        
        /// <summary>
        /// Coroutine to play an EffectSpawnData after a delay
        /// </summary>
        private IEnumerator PlayEffectSpawnDataDelayed(EffectSpawnData effectData, float delay, Transform fallbackTransform)
        {
            yield return new WaitForSeconds(delay);
            if (effectData != null && effectData.IsValid())
            {
                effectData.SpawnEffect(fallbackTransform);
            }
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
