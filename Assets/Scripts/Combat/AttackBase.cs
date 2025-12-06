using UnityEngine;
using Managers;
using System.Collections;
using System;

namespace Combat
{
    /// <summary>
    /// Base class for all attack components.
    /// Contains common attack properties and functionality shared between all characters.
    /// 
    /// UNIFIED ATTACK SYSTEM:
    /// This class works with any character type via IAttackOwner interface:
    /// - Enemies (EnemyBase, ModularEnemy)
    /// - NPCs (SettlerNPC with weapons)
    /// - Player-controlled characters
    /// 
    /// VISUAL SYSTEM (2 Arrays):
    /// This class supports two types of visual elements:
    /// 
    /// 1. PERMANENT EQUIPMENT (attackEquipment):
    ///    - Always visible equipment that shows what the character has
    ///    - Examples: dynamite sticks, guns, swords, rocket launchers
    ///    - Stays ENABLED all the time
    ///    - Lets players see character capabilities at a glance
    /// 
    /// 2. TEMPORARY EFFECTS (attackEffectObjects):
    ///    - Visual effects that appear only during attacks
    ///    - Examples: weapon trails, muzzle flashes, charge effects, telegraphs
    ///    - Starts DISABLED, auto-enables during attack, auto-disables after
    ///    - Adds visual flair to attacks
    /// 
    /// WORKFLOW:
    /// 1. Create equipment models as children (e.g., dynamite sticks)
    /// 2. Add them to attackEquipment array → Always visible
    /// 3. Create effect objects as children (e.g., explosion trails)
    /// 4. Add them to attackEffectObjects array → Toggle with attacks
    /// 5. Start effect objects disabled in the scene
    /// </summary>
    public abstract class AttackBase : MonoBehaviour, IDamageDealer
    {
        [Header("Attack Description")]
        [Tooltip("Custom description of this attack for documentation.\n\n" +
                 "Use this to explain:\n" +
                 "• What this attack does\n" +
                 "• How to configure it properly\n" +
                 "• Special behaviors or requirements\n" +
                 "• Tips for designers/developers")]
        [TextArea(3, 8)]
        public string attackDescription = "";
        
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
        [Tooltip("Optional transform to use as the attack origin. If not set, will use the owner's transform.")]
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
        [Tooltip("Effect played when the character attacks")]
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
        [Tooltip("Use animator to trigger attack timing. If false, uses timeline settings below.")]
        public bool useAnimatorTiming = true;
        [Tooltip("Animation trigger name for this attack (only used if useAnimatorTiming is true)")]
        public string attackTrigger = "Attack";
        
        [Header("Timeline Settings (when useAnimatorTiming is false)")]
        [Tooltip("Delay before calling AttackWarning (visual indicator)")]
        public float warningDelay = 0.3f;
        [Tooltip("Delay before calling Attack (actual damage execution)")]
        public float attackDelay = 0.6f;
        [Tooltip("Delay before calling AttackEnd (cleanup and return to movement)")]
        public float attackEndDelay = 1.0f;

        [Header("Attack Visual Equipment")]
        [Tooltip("Permanent equipment GameObjects that are ALWAYS visible.\n\n" +
                 "These show what attack the character has:\n" +
                 "• Dynamite sticks for ExplosionAttack\n" +
                 "• Gun model for ProjectileAttack\n" +
                 "• Sword/weapon for melee attacks\n" +
                 "• Rocket launcher for missile attacks\n\n" +
                 "These stay enabled all the time so players can see the character's capabilities.")]
        public GameObject[] attackEquipment;
        
        [Tooltip("Temporary effect GameObjects that are ENABLED during attacks only.\n\n" +
                 "These add visual flair during attacks:\n" +
                 "• Weapon trails (sword trails, gun barrel smoke)\n" +
                 "• Muzzle flashes\n" +
                 "• Charge effects (glowing energy, particle buildup)\n" +
                 "• Attack telegraphs (ground circles, warning indicators)\n\n" +
                 "These start disabled and auto-enable/disable with attacks.")]
        public GameObject[] attackEffectObjects;

        /// <summary>
        /// Time when this attack was last executed (used for cooldown calculations)
        /// Made public so owner can check actual cooldown status
        /// </summary>
        public float lastAttackTime;
        
        // Owner reference - works with any character type
        protected IAttackOwner owner;
        
        protected Animator animator;
        protected Transform target;
        private bool hasValidAnimator;
        
        // Public property for external access
        public Transform Target => target;
        
        // Owner accessor for derived classes
        protected IAttackOwner Owner => owner;
        
        // Owner transform helper
        protected Transform OwnerTransform => owner?.transform ?? transform;
        
        // IDamageDealer implementation
        public float BaseDamage => damage;
        public float PoiseDamage => poiseDamage;
        public AttackElement ElementType => attackElement;
        public int ElementalDamageBonus => elementalDamageBonus;
        public Transform DamageSource => attackOrigin != null ? attackOrigin : OwnerTransform;
        
        /// <summary>
        /// Allegiance determined by owner
        /// </summary>
        public Allegiance DealerAllegiance => owner?.GetAllegiance() ?? Allegiance.NEUTRAL;

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
        /// Determine whether this attack has a usable animator to drive its timing.
        /// Cached each time StartAttack() runs to avoid repeated property lookups.
        /// </summary>
        protected bool HasValidAnimator()
        {
            return hasValidAnimator;
        }

        /// <summary>
        /// Should this attack execute immediately (without waiting for animation events)?
        /// Returns true only when there's no animator AND not using timeline timing.
        /// Child attacks can override to force animation-driven timing even if an animator exists.
        /// </summary>
        public virtual bool ShouldExecuteImmediately()
        {
            // Only execute immediately if we have no animator AND not using timeline
            // If using timeline, the coroutine will handle timing
            return !HasValidAnimator() && useAnimatorTiming;
        }

        /// <summary>
        /// Initialize the attack with any IAttackOwner (NPCs, players, enemies)
        /// </summary>
        /// <param name="attackOwner">The character that owns this attack</param>
        public virtual void Initialize(IAttackOwner attackOwner)
        {
            this.owner = attackOwner;
            this.animator = attackOwner.OwnerAnimator;
            this.target = attackOwner.AttackTarget;
            
            // If no attack origin is set, use the owner's transform
            if (attackOrigin == null)
            {
                attackOrigin = attackOwner.transform;
            }

            // Initialize effect players
            InitializeEffectPlayers();
        }
        
        /// <summary>
        /// Initialize effect players
        /// </summary>
        private void InitializeEffectPlayers()
        {
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
            if (target == null || owner == null) return false;
            if (owner.Health <= 0) return false;
            
            // Use obstacle-aware range check for targets with NavMesh obstacles (like buildings)
            bool inRange = DamageUtils.IsInRangeWithObstacles(OwnerTransform.position, target, minRange, maxRange);
            bool cooldownReady = DamageUtils.IsCooldownReady(lastAttackTime, cooldown);
            
            // For ranged attacks (attacks with minimum range > 0), check line of sight
            bool hasLineOfSight = true;
            if (minRange > 0) // Ranged attack
            {
                hasLineOfSight = owner.HasLineOfSight(target.position);
            }
            
            return inRange && cooldownReady && hasLineOfSight;
        }

        /// <summary>
        /// Start the attack sequence
        /// </summary>
        public virtual void StartAttack()
        {
            hasValidAnimator = animator != null && animator.runtimeAnimatorController != null;

            lastAttackTime = Time.time;
            
            // Mark owner as attacking
            if (owner != null)
            {
                owner.IsAttacking = true;
            }
            
            // Enable temporary attack effect objects
            EnableAttackEffectObjects();
            
            // Play start effect
            PlayStartEffect();
            
            // Choose between animator-driven or timeline-driven attack
            if (useAnimatorTiming && hasValidAnimator)
            {
                // Ensure required animator parameters exist; otherwise log error and abort
                bool hasAttackType = AnimatorHasParameter(animator, GameConstants.AnimatorParams.AttackTypeHash, AnimatorControllerParameterType.Int);
                bool hasAttackTrigger = AnimatorHasParameter(animator, Animator.StringToHash(attackTrigger), AnimatorControllerParameterType.Trigger);

                if (!hasAttackType || !hasAttackTrigger)
                {
                    Debug.LogError($"[{owner?.gameObject.name ?? "Unknown"}] StartAttack() - Missing animator params (AttackType:{hasAttackType}, Trigger:{hasAttackTrigger}). Attack aborted; please add these parameters to the animator.");
                    return;
                }

                // Animator-driven: trigger animation, animator events will call AttackWarning/Attack/AttackEnd
                if (owner != null && animator != null)
                {
                    Debug.Log($"[{owner.gameObject.name}] 🎬 StartAttack() - Triggering animator | AttackType: {attackType} | Trigger: {attackTrigger}");
                    animator.SetInteger(GameConstants.AnimatorParams.AttackTypeHash, attackType);
                    animator.SetTrigger(attackTrigger);
                }
                else
                {
                    Debug.LogWarning($"[{owner?.gameObject.name ?? "Unknown"}] StartAttack() - Cannot trigger animation: owner={owner != null}, animator={animator != null}");
                }
            }
            else
            {
                // Timeline-driven: manually call AttackWarning/Attack/AttackEnd at specified delays
                StartCoroutine(ExecuteAttackTimeline());
            }
        }
        
        /// <summary>
        /// Execute the attack using timeline delays instead of animator events
        /// </summary>
        private IEnumerator ExecuteAttackTimeline()
        {
            string ownerName = owner?.gameObject.name ?? "Unknown";
            Debug.Log($"[{ownerName}] Timeline attack started | Warning: {warningDelay}s, Attack: {attackDelay}s, End: {attackEndDelay}s");
            
            // Wait for warning delay, then show warning
            if (warningDelay > 0)
            {
                yield return new WaitForSeconds(warningDelay);
            }
            
            Debug.Log($"[{ownerName}] Timeline: Calling AttackWarning");
            if (owner != null)
            {
                owner.AttackWarning();
            }
            
            // Wait for attack delay (from start, not from warning)
            float remainingDelay = attackDelay - warningDelay;
            if (remainingDelay > 0)
            {
                yield return new WaitForSeconds(remainingDelay);
            }
            
            Debug.Log($"[{ownerName}] Timeline: Executing Attack");
            // Execute the actual attack
            OnAttack();
            if (owner != null)
            {
                owner.Attack();
            }
            
            // Wait for end delay (from start, not from attack)
            remainingDelay = attackEndDelay - attackDelay;
            if (remainingDelay > 0)
            {
                yield return new WaitForSeconds(remainingDelay);
            }
            
            Debug.Log($"[{ownerName}] Timeline: Calling AttackEnd");
            // End the attack
            OnAttackEnd();
        }

        /// <summary>
        /// Check if the animator has a parameter of given hash and type
        /// </summary>
        private bool AnimatorHasParameter(Animator anim, int hash, AnimatorControllerParameterType type)
        {
            if (anim == null || anim.runtimeAnimatorController == null) return false;
            foreach (var param in anim.parameters)
            {
                if (param.nameHash == hash && param.type == type)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Called during Update while attacking - allows continuous target tracking
        /// </summary>
        public virtual void UpdateDuringAttack()
        {
            // If rotation is allowed during attack, manually rotate towards target using Quaternion.Lerp
            if (allowRotationDuringAttack && owner != null && target != null)
            {
                // Safety check: Skip if target was destroyed during attack
                if (target.gameObject == null || !target.gameObject.activeInHierarchy)
                {
                    return;
                }
                
                // Calculate direction to target
                Vector3 directionToTarget = (target.position - OwnerTransform.position).normalized;
                directionToTarget.y = 0; // Keep rotation on horizontal plane
                
                if (directionToTarget != Vector3.zero)
                {
                    // Calculate target rotation
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                    
                    // Smoothly rotate towards target using owner's rotation speed
                    OwnerTransform.rotation = Quaternion.Lerp(
                        OwnerTransform.rotation, 
                        targetRotation, 
                        owner.OwnerRotationSpeed * Time.deltaTime
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
            // Mark owner as no longer attacking
            if (owner != null)
            {
                owner.IsAttacking = false;
            }
            
            // Reset animation parameters
            if (owner != null && animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetInteger(GameConstants.AnimatorParams.AttackTypeHash, 0);
            }
            
            // Disable temporary attack effect objects
            DisableAttackEffectObjects();
            
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
        /// Check if the owner should rotate towards target before attacking
        /// </summary>
        /// <returns>True if rotation is needed</returns>
        public virtual bool ShouldRotateToAttack()
        {
            if (target == null) return false;
            
            return !IsReadyToAttack();
        }

        /// <summary>
        /// Check if the owner is properly facing the target for this attack
        /// </summary>
        /// <returns>True if properly aligned</returns>
        protected virtual bool IsReadyToAttack()
        {
            if (target == null) return false;
            
            return NavigationUtils.IsFacingTarget(OwnerTransform, target, attackAngleThreshold, true);
        }

        /// <summary>
        /// Update the target reference (called when owner finds a new target)
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
            return NavigationUtils.CalculateEffectiveReachDistance(OwnerTransform.position, target, maxRange, 1f);
        }

        /// <summary>
        /// Check if the target is within the attack range
        /// </summary>
        /// <returns>True if target is within range</returns>
        protected virtual bool IsTargetInRange()
        {
            if (target == null) return false;
            return DamageUtils.IsInRangeWithObstacles(OwnerTransform.position, target, minRange, maxRange);
        }

        /// <summary>
        /// Check if the target is too close (within minimum range)
        /// </summary>
        /// <returns>True if target is too close</returns>
        public virtual bool IsTargetTooClose()
        {
            if (target == null) return false;
            return DamageUtils.IsTooClose(OwnerTransform.position, target.position, minRange);
        }

        /// <summary>
        /// Check if the target is too far (beyond maximum range)
        /// </summary>
        /// <returns>True if target is too far</returns>
        public virtual bool IsTargetTooFar()
        {
            if (target == null) return false;
            return DamageUtils.IsTooFar(OwnerTransform.position, target.position, maxRange);
        }

        /// <summary>
        /// Called by Unity for IK (Inverse Kinematics) updates
        /// Override in child classes to implement attack-specific IK behavior
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
        /// Deal damage to a target with custom damage amounts
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
            Vector3 attackPosition = attackOrigin != null ? attackOrigin.position : position;
            
            int targetsDamaged = DamageUtils.DealDamageInRadius(this, attackPosition, radius, damageAmount, poiseDamage);
            
            if (targetsDamaged > 0)
            {
                PlayHitEffect(attackPosition, Vector3.up);
            }
        }

        /// <summary>
        /// Play an effect with optional delay
        /// </summary>
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
        private IEnumerator PlayEffectDelayed(EffectDefinition effect, float delay, Vector3 position, Vector3 direction, Quaternion rotation, Transform parent)
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
                PlayEffectSpawnData(startEffect, startEffectDelay, attackOrigin ?? OwnerTransform);
            }
        }

        /// <summary>
        /// Play attack effect using EffectSpawnData configuration
        /// </summary>
        protected virtual void PlayAttackEffect(Vector3? position = null, Vector3? direction = null, Quaternion? rotation = null, Transform parent = null)
        {
            if (attackEffect != null && attackEffect.IsValid())
            {
                PlayEffectSpawnData(attackEffect, attackEffectDelay, attackOrigin ?? OwnerTransform);
            }
        }

        /// <summary>
        /// Play hit effect using EffectSpawnData configuration
        /// </summary>
        protected virtual void PlayHitEffect(Vector3? position = null, Vector3? direction = null, Quaternion? rotation = null, Transform parent = null)
        {
            if (hitEffect != null && hitEffect.IsValid())
            {
                PlayEffectSpawnData(hitEffect, hitEffectDelay, attackOrigin ?? OwnerTransform);
            }
        }

        /// <summary>
        /// Play end effect using EffectSpawnData configuration
        /// </summary>
        protected virtual void PlayEndEffect(Vector3? position = null, Vector3? direction = null, Quaternion? rotation = null, Transform parent = null)
        {
            if (endEffect != null && endEffect.IsValid())
            {
                PlayEffectSpawnData(endEffect, endEffectDelay, attackOrigin ?? OwnerTransform);
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
        /// Enable temporary attack effect objects during attacks
        /// </summary>
        protected virtual void EnableAttackEffectObjects()
        {
            if (attackEffectObjects == null) return;
            
            foreach (GameObject obj in attackEffectObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                }
            }
        }

        /// <summary>
        /// Disable temporary attack effect objects after attacks
        /// </summary>
        protected virtual void DisableAttackEffectObjects()
        {
            if (attackEffectObjects == null) return;
            
            foreach (GameObject obj in attackEffectObjects)
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
            // Draw min range (too close zone) - yellow
            if (minRange > 0)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, minRange);
            }
            
            // Draw max range (attack range) - red
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, maxRange);
        }
    }
}
