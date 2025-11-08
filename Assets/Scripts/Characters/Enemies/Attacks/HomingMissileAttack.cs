using UnityEngine;
using Managers;

namespace Enemies.Attacks
{
    /// <summary>
    /// Homing missile attack that fires projectiles which track targets for a duration.
    /// Inherits from ProjectileAttack to reuse projectile spawning logic.
    /// 
    /// The fired missile prefab should have a HomingMissile component attached to handle tracking.
    /// When the tracking duration expires or the missile hits something, it explodes.
    /// 
    /// Perfect for drones, robots, or any enemy that needs smart projectiles.
    /// 
    /// Usage:
    /// 1. Attach this component to your enemy
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
        /// Override default projectile ranges so drones can fire at close range and pursue the player.
        /// </summary>
        public override void Initialize(EnemyBase enemy)
        {
            base.Initialize(enemy);
            
            Debug.Log($"[{enemy.gameObject.name}] HomingMissileAttack initialized | Duration: {homingDuration}s | TurnSpeed: {turnSpeed}°/s");
        }

        public override void OnAttack()
        {
            if (target == null)
            {
                Debug.LogWarning($"[{enemy.gameObject.name}] Homing missile attack called with no target!");
                return;
            }

            if (attackEffect == null || !attackEffect.IsValid() || attackEffect.effectDefinition == null)
            {
                Debug.LogError($"[{enemy.gameObject.name}] HomingMissileAttack requires a projectile effect definition. Please assign one on the attack component.");
                return;
            }
            
            // Calculate launch direction
            Vector3 launchDirection = (target.position - enemy.transform.position).normalized;
            float damageRadius = CalculateDamageRadius();
            
            // Fire the missile using the utility (ProjectileType.HOMING tells the system we'll configure our own component)
            GameObject missile = DamageUtils.FireProjectileWithEffect(
                enemy.transform.position + Vector3.up * projectileSpawnHeight,
                launchDirection,
                Quaternion.LookRotation(launchDirection),
                target.position,
                damage,
                poiseDamage,
                enemy.transform,
                attackElement,
                attackEffect.effectDefinition,
                hitEffect?.effectDefinition,
                createDamageAreaOnImpact,
                damageRadius,
                impactDamageDuration,
                useTriggerBasedDamage,
                ProjectileType.HOMING  // Indicate this is a homing projectile (we configure the component ourselves)
            );
            
            if (missile != null)
            {
                // Configure the homing behavior
                ConfigureHomingMissile(missile, launchDirection, damageRadius);
                
                Debug.Log($"[{enemy.gameObject.name}] Homing missile fired at {target.name} | Duration: {homingDuration}s");
            }
            else
            {
                Debug.LogError($"[{enemy.gameObject.name}] Failed to fire homing missile!");
            }
        }

        /// <summary>
        /// Configure the homing missile component with tracking parameters and damage info
        /// </summary>
        private void ConfigureHomingMissile(GameObject missile, Vector3 launchDirection, float damageRadius)
        {
            // Get or add the homing missile component
            HomingProjectile homingComponent = missile.GetComponent<HomingProjectile>();
            if (homingComponent == null)
            {
                homingComponent = missile.AddComponent<HomingProjectile>();
                Debug.LogWarning($"[{enemy.gameObject.name}] Missile prefab didn't have HomingProjectile component, added automatically");
            }
            
            // Configure homing behavior using standardized Initialize signature
            homingComponent.Initialize(
                target,                                      // targetTransform
                damage,                                      // dmg
                poiseDamage,                                 // poiseDmg
                enemy.transform,                             // attackTransform
                attackElement,                               // elem
                projectileSpeed * missileSpeedMultiplier,   // projectileSpeed
                homingDuration,                              // duration
                turnSpeed,                                   // turnSpeedDegrees
                explodeOnTimeout,                            // explodeWhenExpired
                hitEffect?.effectDefinition,                 // impactEff
                createDamageAreaOnImpact,                    // createArea
                damageRadius,                                // areaRadius
                impactDamageDuration,                        // areaDuration
                useTriggerBasedDamage                        // triggerBased
            );
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
                GameObject effectPrefab = hitEffect.effectDefinition.prefabs[0];
                var damageArea = effectPrefab.GetComponent<DamageArea>();
                if (damageArea != null)
                {
                    return 0f; // Trigger-based detection
                }
            }

            return fallbackDamageRadius;
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            
            if (enemy == null) return;
            
            // Draw missile arc visualization (approximate)
            Gizmos.color = Color.magenta;
            Vector3 startPos = enemy.transform.position + Vector3.up * projectileSpawnHeight;
            
            // Show multiple trajectory lines for visualization
            for (int i = 0; i < 5; i++)
            {
                float angle = (i - 2) * 15f; // -30 to +30 degrees
                Vector3 direction = Quaternion.Euler(0, angle, 0) * enemy.transform.forward;
                Gizmos.DrawRay(startPos, direction * maxRange * 0.5f);
            }
        }
    }
}

