using UnityEngine;
using Managers;

namespace Combat.Attacks
{
    /// <summary>
    /// Homing missile attack that fires projectiles which track targets for a duration.
    /// Inherits from ProjectileAttack to reuse projectile spawning logic.
    /// 
    /// The fired missile prefab should have a HomingMissile component attached to handle tracking.
    /// When the tracking duration expires or the missile hits something, it explodes.
    /// 
    /// Perfect for drones, robots, or any character that needs smart projectiles.
    /// 
    /// Usage:
    /// 1. Attach this component to your character
    /// 2. Assign attackEffect to a missile prefab (must have HomingMissile component)
    /// 3. Assign hitEffect for explosion VFX
    /// 4. Configure tracking duration, turn speed, etc. in inspector
    /// 5. The missile will automatically track the target for the specified duration
    /// </summary>
    public class HomingMissileAttack : ProjectileAttack
    {
        [Header("Homing Missile Settings")]
        [Tooltip("How long the missile tracks the target (seconds)")]
        [SerializeField] private float homingDuration = 3f;
        
        [Tooltip("How fast the missile turns towards target (degrees/second)")]
        [SerializeField] private float turnSpeed = 180f;
        
        [Tooltip("Whether the missile explodes when tracking expires (or just falls)")]
        [SerializeField] private bool explodeOnTimeout = true;
        
        [Tooltip("Speed boost applied to missiles (multiplier on projectileSpeed)")]
        [SerializeField] private float missileSpeedMultiplier = 1.2f;
        
        protected override void Awake()
        {
            base.Awake();
            
            // Homing missiles can be fired at point-blank range, so override the default projectile min range
            if (Mathf.Approximately(minRange, 5f))
            {
                minRange = 0f;
            }
            
            // Extend default max range slightly to allow long-distance tracking if designer hasn't provided a custom value
            if (Mathf.Approximately(maxRange, 15f))
            {
                maxRange = 22f;
            }
            
            // Set default attack type for homing missiles
            if (attackType == 0)
            {
                attackType = 5; // Homing missile attack type
            }
            
            // Set default element to explosive
            if (attackElement == AttackElement.NONE)
            {
                attackElement = AttackElement.FIRE;
            }
        }

        /// <summary>
        /// Override default projectile ranges so characters can fire at close range and pursue the target.
        /// </summary>
        public override void Initialize(IAttackOwner attackOwner)
        {
            base.Initialize(attackOwner);
            
            Debug.Log($"[{attackOwner.gameObject.name}] HomingMissileAttack initialized | Duration: {homingDuration}s | TurnSpeed: {turnSpeed}°/s");
        }

        public override void OnAttack()
        {
            Debug.Log($"[{OwnerTransform.gameObject.name}] ===== HOMING MISSILE ATTACK START =====");
            
            if (target == null)
            {
                Debug.LogWarning($"[{OwnerTransform.gameObject.name}] Homing missile attack called with no target!");
                return;
            }
            
            Debug.Log($"[{OwnerTransform.gameObject.name}] Target: {target.name} | Position: {target.position}");

            if (attackEffect == null)
            {
                Debug.LogError($"[{OwnerTransform.gameObject.name}] attackEffect is NULL!");
                return;
            }
            
            if (!attackEffect.IsValid())
            {
                Debug.LogError($"[{OwnerTransform.gameObject.name}] attackEffect.IsValid() returned FALSE!");
                return;
            }
            
            if (attackEffect.effectDefinition == null)
            {
                Debug.LogError($"[{OwnerTransform.gameObject.name}] attackEffect.effectDefinition is NULL!");
                return;
            }
            
            Debug.Log($"[{OwnerTransform.gameObject.name}] attackEffect validated | Name: {attackEffect.effectDefinition.name}");
            
            // Use attack origin if provided, otherwise use owner position
            Transform spawnTransform = attackOrigin != null ? attackOrigin : OwnerTransform;
            Vector3 spawnPosition = spawnTransform.position + Vector3.up * projectileSpawnHeight;
            
            Debug.Log($"[{OwnerTransform.gameObject.name}] Spawn Transform: {(attackOrigin != null ? attackOrigin.name : OwnerTransform.name)} | Spawn Position: {spawnPosition} | Spawn Height Offset: {projectileSpawnHeight}");
            
            // Calculate launch direction from spawn position
            Vector3 launchDirection = (target.position - spawnPosition).normalized;
            float damageRadius = CalculateDamageRadius();
            
            Debug.Log($"[{OwnerTransform.gameObject.name}] Launch Direction: {launchDirection} | Damage Radius: {damageRadius}");
            Debug.Log($"[{OwnerTransform.gameObject.name}] Missile Parameters:");
            Debug.Log($"[{OwnerTransform.gameObject.name}]   - Damage: {damage} | Poise: {poiseDamage} | Element: {attackElement}");
            Debug.Log($"[{OwnerTransform.gameObject.name}]   - Speed: {projectileSpeed * missileSpeedMultiplier} (base: {projectileSpeed} * multiplier: {missileSpeedMultiplier})");
            Debug.Log($"[{OwnerTransform.gameObject.name}]   - Duration: {homingDuration}s | Turn Speed: {turnSpeed}°/s | Explode On Timeout: {explodeOnTimeout}");
            Debug.Log($"[{OwnerTransform.gameObject.name}]   - Create Damage Area: {createDamageAreaOnImpact} | Impact Duration: {impactDamageDuration}");
            Debug.Log($"[{OwnerTransform.gameObject.name}]   - Use Trigger Based Damage: {useTriggerBasedDamage}");
            Debug.Log($"[{OwnerTransform.gameObject.name}]   - Attacker: {(owner != null ? owner.gameObject.name : "NULL")}");
            Debug.Log($"[{OwnerTransform.gameObject.name}]   - Hit Effect: {(hitEffect != null && hitEffect.IsValid() ? hitEffect.effectDefinition.name : "NULL")}");
            
            // Fire the missile using the utility (standardized like other projectile types)
            Debug.Log($"[{OwnerTransform.gameObject.name}] Calling DamageUtils.FireProjectileWithEffect...");
            GameObject missile = DamageUtils.FireProjectileWithEffect(
                spawnPosition,
                launchDirection,
                Quaternion.LookRotation(launchDirection),
                target.position,
                damage,
                poiseDamage,
                OwnerTransform,
                attackElement,
                attackEffect.effectDefinition,
                hitEffect?.effectDefinition,
                createDamageAreaOnImpact,
                damageRadius,
                impactDamageDuration,
                useTriggerBasedDamage,
                ProjectileType.HOMING,                           // Projectile type
                projectileSpeed * missileSpeedMultiplier,       // projectileSpeed (with multiplier)
                5f,                                             // projectileMaxHeight (not used for homing)
                target,                                         // targetTransform (required for homing)
                homingDuration,                                 // homingDuration
                turnSpeed,                                      // turnSpeed
                explodeOnTimeout                               // explodeOnTimeout
            );
            
            if (missile != null)
            {
                Debug.Log($"[{OwnerTransform.gameObject.name}] ✅ MISSILE CREATED SUCCESSFULLY: {missile.name}");
                
                // Fire the start effect at the launch point when the missile is created (thruster flare, etc.)
                if (startEffect != null && startEffect.IsValid())
                {
                    Debug.Log($"[{OwnerTransform.gameObject.name}] Playing start effect (muzzle flash)...");
                    PlayStartEffect(spawnPosition, launchDirection, Quaternion.LookRotation(launchDirection), spawnTransform);
                }
                else
                {
                    Debug.Log($"[{OwnerTransform.gameObject.name}] No start effect to play (startEffect is null or invalid)");
                }
                
                Debug.Log($"[{OwnerTransform.gameObject.name}] ✅ Homing missile fired from {spawnPosition} at {target.name} | Duration: {homingDuration}s");
            }
            else
            {
                Debug.LogError($"[{OwnerTransform.gameObject.name}] ❌ FAILED TO FIRE HOMING MISSILE! DamageUtils.FireProjectileWithEffect returned NULL!");
            }
            
            Debug.Log($"[{OwnerTransform.gameObject.name}] ===== HOMING MISSILE ATTACK END =====");
        }

        /// <summary>
        /// Calculate damage radius (reuse parent logic)
        /// </summary>
        private float CalculateDamageRadius()
        {
            if (!useTriggerBasedDamage || hitEffect == null || !hitEffect.IsValid())
            {
                return fallbackDamageRadius;
            }

            // Check if the impact effect has a damage component
            if (hitEffect.effectDefinition != null && hitEffect.effectDefinition.prefabs != null && hitEffect.effectDefinition.prefabs.Length > 0)
            {
                var prefabEntry = hitEffect.effectDefinition.prefabs[0];
                GameObject effectPrefab = prefabEntry != null ? prefabEntry.prefab : null;
                if (effectPrefab != null)
                {
                    var damageArea = effectPrefab.GetComponent<DamageArea>();
                    if (damageArea != null)
                    {
                        return 0f; // Trigger-based detection
                    }
                }
            }

            return fallbackDamageRadius;
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            
            Transform drawTransform = OwnerTransform ?? transform;
            
            // Draw missile arc visualization (approximate)
            Gizmos.color = Color.magenta;
            Vector3 startPos = drawTransform.position + Vector3.up * projectileSpawnHeight;
            
            // Show spawn position
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(startPos, 0.2f);
            
            // Show multiple trajectory lines for visualization
            Gizmos.color = Color.magenta;
            for (int i = 0; i < 5; i++)
            {
                float angle = (i - 2) * 15f; // -30 to +30 degrees
                Vector3 direction = Quaternion.Euler(0, angle, 0) * drawTransform.forward;
                Gizmos.DrawRay(startPos, direction * maxRange * 0.5f);
            }
        }
    }
}
