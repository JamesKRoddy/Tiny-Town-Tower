using UnityEngine;
using Managers;

namespace Combat.Attacks
{
    /// <summary>
    /// Projectile-based attack that fires projectiles at targets.
    /// Good for ranged attacks, spit attacks, magic missiles, etc.
    /// </summary>
    public class ProjectileAttack : AttackBase
    {
        [Header("Projectile Settings")]
        [Tooltip("Height offset for projectile spawn")]
        public float projectileSpawnHeight = 0.0f;
        [Tooltip("Speed of the projectile")]
        public float projectileSpeed = 10f;
        [Tooltip("Maximum height of the projectile arc")]
        public float projectileMaxHeight = 5f;
        [Tooltip("Whether to create a damage area on impact")]
        public bool createDamageAreaOnImpact = true;
        [Tooltip("Duration of the damage area created on impact")]
        public float impactDamageDuration = 5f;
        [Tooltip("Whether to use trigger-based damage (requires component on impact effect)")]
        public bool useTriggerBasedDamage = true;
        [Tooltip("Fallback radius if no trigger component is found")]
        public float fallbackDamageRadius = 2f;
        [Tooltip("If true, projectile explodes on any collision. If false, only explodes on ground hit (default false for arc projectiles like vomit pools)")]
        public bool explodeOnAnyHit = false;

        
        protected override void Awake()
        {
            base.Awake();
            // Set default attack type for projectile attacks
            if (attackType == 0)
            {
                attackType = 2; // Projectile attack type
            }
            
            // Set default range values for projectile attacks
            if (minRange == 0 && maxRange == 5) // Only set defaults if they haven't been customized
            {
                minRange = 5f;   // Minimum range for projectile attacks
                maxRange = 15f;  // Maximum range for projectile attacks
            }
            
            // Set default angle threshold for projectile attacks
            if (attackAngleThreshold == 30) // Only set default if it hasn't been customized
            {
                attackAngleThreshold = 5f; // Projectile attacks need precise aiming
            }
        }

        // Store the target position when attack begins
        private Vector3 attackTargetPosition;

        public override void Initialize(IAttackOwner attackOwner)
        {
            base.Initialize(attackOwner);
            
            // Set default elemental damage for projectile attacks
            if (attackElement == AttackElement.NONE)
            {
                attackElement = AttackElement.PHYSICAL;
            }
            
            // Validate effects
            if (attackEffect == null || !attackEffect.IsValid())
            {
                Debug.LogError("Attack effect (projectile) definition is not assigned or invalid on ProjectileAttack on " + attackOwner.gameObject.name);
            }
            
            Debug.Log($"[{attackOwner.gameObject.name}] ProjectileAttack initialized | Min: {minRange} | Max: {maxRange} | Damage: {damage}");
        }

        public override void StartAttack()
        {
            base.StartAttack();
            
            // Store the target position when attack begins
            if (target != null)
            {
                attackTargetPosition = target.position;
            }
            
            Debug.Log($"[{OwnerTransform.gameObject.name}] Projectile attack started | Target: {target.name} | Distance: {Vector3.Distance(OwnerTransform.position, target.position):F2}");
        }
        
        /// <summary>
        /// Override to prevent start effect from playing in StartAttack()
        /// It will only play in OnAttack() when projectile is successfully created
        /// </summary>
        protected override void PlayStartEffect(Vector3? position = null, Vector3? direction = null, Quaternion? rotation = null, Transform parent = null)
        {
            // Only play start effect if parameters are provided (meaning it's being called from OnAttack, not StartAttack)
            if (startEffect != null && startEffect.IsValid() && position.HasValue)
            {
                Vector3 effectPos = position.Value;
                Vector3 effectDir = direction.HasValue ? direction.Value : Vector3.forward;
                Quaternion effectRot = rotation.HasValue ? rotation.Value : Quaternion.identity;
                Transform effectParent = parent ?? attackOrigin ?? OwnerTransform;
                
                PlayEffectSpawnDataAtPosition(startEffect, startEffectDelay, effectParent, effectPos, effectDir, effectRot);
            }
        }
        
        /// <summary>
        /// Helper to play EffectSpawnData with explicit position/direction/rotation
        /// </summary>
        private void PlayEffectSpawnDataAtPosition(EffectSpawnData effectData, float delay, Transform parent, Vector3 position, Vector3 direction, Quaternion rotation)
        {
            if (effectData == null || !effectData.IsValid()) return;
            
            if (delay > 0)
            {
                StartCoroutine(PlayEffectSpawnDataAtPositionDelayed(effectData, delay, parent, position, direction, rotation));
            }
            else
            {
                EffectManager.Instance?.PlayEffect(position, direction, rotation, parent, effectData.effectDefinition);
            }
        }
        
        private System.Collections.IEnumerator PlayEffectSpawnDataAtPositionDelayed(EffectSpawnData effectData, float delay, Transform parent, Vector3 position, Vector3 direction, Quaternion rotation)
        {
            yield return new WaitForSeconds(delay);
            if (effectData != null && effectData.IsValid())
            {
                EffectManager.Instance?.PlayEffect(position, direction, rotation, parent, effectData.effectDefinition);
            }
        }

        public override void OnAttack()
        {
            if (target == null)
            {
                Debug.LogWarning($"[{OwnerTransform.gameObject.name}] Projectile attack called with no target!");
                return;
            }
            
            // Always fire in the direction we were aiming when the attack started
            Vector3 direction = (attackTargetPosition - OwnerTransform.position).normalized;
            
            // Calculate damage radius based on settings
            float damageRadius = CalculateDamageRadius();
            bool useTriggerDetection = (damageRadius == 0f); // Special value indicates trigger-based detection
            
            // Fire the projectile using the utility
            GameObject projectile = DamageUtils.FireProjectileWithEffect(
                OwnerTransform.position + Vector3.up * projectileSpawnHeight,
                direction,
                Quaternion.LookRotation(direction),
                attackTargetPosition,
                damage,
                poiseDamage,
                OwnerTransform,
                attackElement,
                attackEffect?.effectDefinition,
                hitEffect?.effectDefinition,
                createDamageAreaOnImpact,
                damageRadius,
                impactDamageDuration,
                useTriggerDetection,
                ProjectileType.ARC,
                projectileSpeed,
                projectileMaxHeight,
                explodeOnAnyHit: explodeOnAnyHit
            );
            
            Debug.Log($"[{OwnerTransform.gameObject.name}] Projectile attack executed | Projectile fired towards: {attackTargetPosition} | Damage: {damage} | Radius: {damageRadius}");
        }

        /// <summary>
        /// Determines the damage detection method and radius for the impact effect
        /// </summary>
        private float CalculateDamageRadius()
        {
            if (!useTriggerBasedDamage || hitEffect == null || !hitEffect.IsValid())
            {
                return fallbackDamageRadius;
            }

            // Check if the impact effect has a damage component that can handle triggers
            if (hitEffect.effectDefinition != null && hitEffect.effectDefinition.prefabs != null && hitEffect.effectDefinition.prefabs.Length > 0)
            {
                var prefabEntry = hitEffect.effectDefinition.prefabs[0];
                GameObject effectPrefab = prefabEntry != null ? prefabEntry.prefab : null;
                
                if (effectPrefab != null)
                {
                    var damageArea = effectPrefab.GetComponent<DamageArea>();
                    if (damageArea == null)
                    {
                        damageArea = effectPrefab.GetComponentInChildren<DamageArea>();
                    }
                    
                    if (damageArea != null)
                    {
                        Debug.Log($"[{OwnerTransform.gameObject.name}] Using trigger-based damage detection with {damageArea.GetType().Name} component");
                        return 0f; // Special value to indicate trigger-based detection
                    }
                    
                    var triggerComponent = effectPrefab.GetComponent<Collider>();
                    if (triggerComponent == null)
                    {
                        triggerComponent = effectPrefab.GetComponentInChildren<Collider>();
                    }
                    
                    if (triggerComponent != null && triggerComponent.isTrigger)
                    {
                        Debug.Log($"[{OwnerTransform.gameObject.name}] Found trigger collider, but no DamageArea component. Using fallback radius.");
                    }
                }
            }

            Debug.Log($"[{OwnerTransform.gameObject.name}] No trigger-capable damage component found in impact effect '{hitEffect?.effectDefinition?.name}', using fallback radius: {fallbackDamageRadius}");
            return fallbackDamageRadius;
        }


        protected override void OnDrawGizmosSelected()
        {
            Transform drawTransform = OwnerTransform ?? transform;
            
            // Draw min attack range
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(drawTransform.position, minRange);
            
            // Draw max attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(drawTransform.position, maxRange);
            
            // Draw attack angle
            Vector3 rightDir = Quaternion.Euler(0, attackAngleThreshold, 0) * drawTransform.forward;
            Vector3 leftDir = Quaternion.Euler(0, -attackAngleThreshold, 0) * drawTransform.forward;
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(drawTransform.position, rightDir * maxRange);
            Gizmos.DrawRay(drawTransform.position, leftDir * maxRange);
            
            // Draw projectile spawn height
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(drawTransform.position + Vector3.up * projectileSpawnHeight, 0.5f);
        }
    }
}
