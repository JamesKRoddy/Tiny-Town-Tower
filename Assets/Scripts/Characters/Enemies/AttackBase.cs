using UnityEngine;
using Managers;

namespace Enemies
{
    /// <summary>
    /// Base class for all enemy attack components.
    /// Contains common attack properties and functionality shared between boss and zombie attacks.
    /// </summary>
    public abstract class AttackBase : MonoBehaviour
    {
        [Header("Attack Settings")]
        [Tooltip("Maximum range for this attack")]
        public float range = 5f;
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
            // Override in child classes for specific initialization
        }

        /// <summary>
        /// Check if this attack can be used right now
        /// </summary>
        /// <returns>True if the attack can be used</returns>
        public virtual bool CanAttack()
        {
            return Time.time - lastAttackTime >= cooldown;
        }

        /// <summary>
        /// Start the attack sequence
        /// </summary>
        public virtual void StartAttack()
        {
            // Override in child classes for specific attack start behavior
        }

        /// <summary>
        /// Called when the attack animation reaches the damage dealing frame
        /// </summary>
        public virtual void OnAttack()
        {
            // Override in child classes for specific attack behavior
        }

        /// <summary>
        /// Called when the attack animation ends
        /// </summary>
        public virtual void OnAttackEnd()
        {
            // Override in child classes for specific attack end behavior
        }

        /// <summary>
        /// Get the current attack range
        /// </summary>
        /// <returns>Current effective attack range</returns>
        public virtual float GetCurrentAttackRange()
        {
            return range;
        }

        /// <summary>
        /// Check if the enemy should rotate towards target before attacking
        /// </summary>
        /// <returns>True if rotation is needed</returns>
        public virtual bool ShouldRotateToAttack()
        {
            return false; // Override in child classes
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
        /// Deal damage to all targets in a radius
        /// </summary>
        /// <param name="radius">Radius of the damage area</param>
        /// <param name="damageAmount">Amount of damage to deal</param>
        /// <param name="center">Center point of the damage area</param>
        protected virtual void DealDamageInRadius(float radius, float damageAmount, Vector3 center)
        {
            Collider[] colliders = Physics.OverlapSphere(center, radius);
            
            foreach (Collider collider in colliders)
            {
                IDamageable damageable = collider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    DealDamage(damageable, damageAmount);
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
            Transform origin = attackOrigin != null ? attackOrigin : transform;
            Vector3 pos = position ?? origin.position;
            Vector3 dir = direction ?? origin.forward;
            Quaternion rot = rotation ?? origin.rotation;
            Transform par = parent ?? origin;
            PlayEffect(startEffect, startEffectDelay, pos, dir, rot, par);
        }

        /// <summary>
        /// Play attack effect
        /// </summary>
        protected virtual void PlayAttackEffect(Vector3? position = null, Vector3? direction = null, Quaternion? rotation = null, Transform parent = null)
        {
            Transform origin = attackOrigin != null ? attackOrigin : transform;
            Vector3 pos = position ?? origin.position;
            Vector3 dir = direction ?? origin.forward;
            Quaternion rot = rotation ?? origin.rotation;
            Transform par = parent ?? origin;
            PlayEffect(attackEffect, attackEffectDelay, pos, dir, rot, par);
        }

        /// <summary>
        /// Play hit effect
        /// </summary>
        protected virtual void PlayHitEffect(Vector3? position = null, Vector3? direction = null, Quaternion? rotation = null, Transform parent = null)
        {
            Transform origin = attackOrigin != null ? attackOrigin : transform;
            Vector3 pos = position ?? origin.position;
            Vector3 dir = direction ?? origin.forward;
            Quaternion rot = rotation ?? origin.rotation;
            Transform par = parent ?? origin;
            PlayEffect(hitEffect, hitEffectDelay, pos, dir, rot, par);
        }

        /// <summary>
        /// Play end effect
        /// </summary>
        protected virtual void PlayEndEffect(Vector3? position = null, Vector3? direction = null, Quaternion? rotation = null, Transform parent = null)
        {
            Transform origin = attackOrigin != null ? attackOrigin : transform;
            Vector3 pos = position ?? origin.position;
            Vector3 dir = direction ?? origin.forward;
            Quaternion rot = rotation ?? origin.rotation;
            Transform par = parent ?? origin;
            PlayEffect(endEffect, endEffectDelay, pos, dir, rot, par);
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
            Gizmos.DrawWireSphere(transform.position, range);
        }
    }
}
