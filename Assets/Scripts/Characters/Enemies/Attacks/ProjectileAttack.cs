using UnityEngine;
using Managers;

namespace Enemies.Attacks
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

        public override void Initialize(EnemyBase enemy)
        {
            base.Initialize(enemy);
            
            // Set default elemental damage for projectile attacks
            if (attackElement == AttackElement.NONE)
            {
                attackElement = AttackElement.PHYSICAL;
            }
            
            // Validate effects
            if (attackEffect == null || !attackEffect.IsValid())
            {
                Debug.LogError("Attack effect (projectile) definition is not assigned or invalid on ProjectileAttack on " + enemy.gameObject.name);
            }
            
            Debug.Log($"[{enemy.gameObject.name}] ProjectileAttack initialized | Min: {minRange} | Max: {maxRange} | Damage: {damage}");
        }

        public override void StartAttack()
        {
            // Do everything the base does EXCEPT play the start effect
            // The start effect should only play when the projectile is successfully created in OnAttack()
            base.StartAttack();
            
            // Cancel the start effect that was played by base.StartAttack()
            // We'll play it in OnAttack() only when the projectile is successfully created
            // (This prevents the muzzle flash from playing when no missile is spawned)
            
            // Store the target position when attack begins
            if (target != null)
            {
                attackTargetPosition = target.position;
            }
            
            Debug.Log($"[{enemy.gameObject.name}] Projectile attack started | Target: {target.name} | Distance: {Vector3.Distance(enemy.transform.position, target.position):F2}");
        }
        
        /// <summary>
        /// Override to prevent start effect from playing in StartAttack()
        /// It will only play in OnAttack() when projectile is successfully created
        /// When called with parameters (from OnAttack), actually play the effect
        /// </summary>
        protected override void PlayStartEffect(Vector3? position = null, Vector3? direction = null, Quaternion? rotation = null, Transform parent = null)
        {
            // Only play start effect if parameters are provided (meaning it's being called from OnAttack, not StartAttack)
            // This prevents the muzzle flash from playing when the missile fails to spawn
            if (startEffect != null && startEffect.IsValid() && position.HasValue)
            {
                // Use provided position/direction/rotation/parent if available, otherwise use defaults
                Vector3 effectPos = position.Value;
                Vector3 effectDir = direction.HasValue ? direction.Value : Vector3.forward;
                Quaternion effectRot = rotation.HasValue ? rotation.Value : Quaternion.identity;
                Transform effectParent = parent ?? attackOrigin ?? enemy?.transform;
                
                // Play the effect at the specified location (muzzle flash at spawn position)
                PlayEffectSpawnDataAtPosition(startEffect, startEffectDelay, effectParent, effectPos, effectDir, effectRot);
            }
            // If no parameters provided (called from base.StartAttack()), do nothing
        }
        
        /// <summary>
        /// Helper to play EffectSpawnData with explicit position/direction/rotation
        /// </summary>
        private void PlayEffectSpawnDataAtPosition(EffectSpawnData effectData, float delay, Transform parent, Vector3 position, Vector3 direction, Quaternion rotation)
        {
            if (effectData == null || !effectData.IsValid()) return;
            
            // Spawn at the specified world position with the specified rotation
            if (delay > 0)
            {
                StartCoroutine(PlayEffectSpawnDataAtPositionDelayed(effectData, delay, parent, position, direction, rotation));
            }
            else
            {
                // Use EffectManager to spawn at world position
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
                Debug.LogWarning($"[{enemy.gameObject.name}] Projectile attack called with no target!");
                return;
            }
            
            // Always fire in the direction we were aiming when the attack started
            Vector3 direction = (attackTargetPosition - enemy.transform.position).normalized;
            
            // Calculate damage radius based on settings
            float damageRadius = CalculateDamageRadius();
            bool useTriggerDetection = (damageRadius == 0f); // Special value indicates trigger-based detection
            
                // Fire the projectile using the utility
                GameObject projectile = DamageUtils.FireProjectileWithEffect(
                    enemy.transform.position + Vector3.up * projectileSpawnHeight,
                    direction,
                    Quaternion.LookRotation(direction),
                    attackTargetPosition,
                    damage,
                    poiseDamage,
                    enemy.transform,
                    attackElement,
                    attackEffect?.effectDefinition,
                    hitEffect?.effectDefinition,
                    createDamageAreaOnImpact,
                    damageRadius,
                    impactDamageDuration,
                    useTriggerDetection,
                    ProjectileType.ARC,
                    projectileSpeed,          // Pass projectile speed from component
                    projectileMaxHeight       // Pass max height from component
                );
            
            Debug.Log($"[{enemy.gameObject.name}] Projectile attack executed | Projectile fired towards: {attackTargetPosition} | Damage: {damage} | Radius: {damageRadius}");
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
                // Check for damage area components that can handle OnTriggerEnter
                // This will find DamageArea or any class that inherits from it (like ZombieVomitPool)
                var damageArea = effectPrefab.GetComponent<DamageArea>();
                if (damageArea == null)
                {
                    damageArea = effectPrefab.GetComponentInChildren<DamageArea>();
                }
                
                // If we found a damage area component, return 0 to indicate trigger-based detection
                if (damageArea != null)
                {
                    Debug.Log($"[{enemy.gameObject.name}] Using trigger-based damage detection with {damageArea.GetType().Name} component");
                    return 0f; // Special value to indicate trigger-based detection
                }
                
                // Check for other trigger-capable components
                var triggerComponent = effectPrefab.GetComponent<Collider>();
                if (triggerComponent == null)
                {
                    triggerComponent = effectPrefab.GetComponentInChildren<Collider>();
                }
                
                if (triggerComponent != null && triggerComponent.isTrigger)
                {
                    Debug.Log($"[{enemy.gameObject.name}] Found trigger collider, but no DamageArea component. Using fallback radius.");
                    }
                }
            }

            // Fallback to radius-based detection
            Debug.Log($"[{enemy.gameObject.name}] No trigger-capable damage component found in impact effect '{hitEffect?.effectDefinition?.name}', using fallback radius: {fallbackDamageRadius}");
            return fallbackDamageRadius;
        }


        protected override void OnDrawGizmosSelected()
        {
            if (enemy == null) return;
            
            // Draw min attack range
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(enemy.transform.position, minRange);
            
            // Draw max attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(enemy.transform.position, maxRange);
            
            // Draw attack angle
            Vector3 rightDir = Quaternion.Euler(0, attackAngleThreshold, 0) * enemy.transform.forward;
            Vector3 leftDir = Quaternion.Euler(0, -attackAngleThreshold, 0) * enemy.transform.forward;
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(enemy.transform.position, rightDir * maxRange);
            Gizmos.DrawRay(enemy.transform.position, leftDir * maxRange);
            
            // Draw projectile spawn height
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(enemy.transform.position + Vector3.up * projectileSpawnHeight, 0.5f);
        }
    }
}
