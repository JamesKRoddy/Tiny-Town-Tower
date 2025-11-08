using UnityEngine;
using UnityEngine.AI;

namespace Enemies
{
    /// <summary>
    /// Base robot class that inherits from ModularEnemy (uses the modular attack system).
    /// Robots are mechanical enemies with enhanced durability and various attack patterns.
    /// 
    /// Key Features:
    /// - Typically higher health and poise
    /// - Different movement characteristics (more rigid/mechanical)
    /// - Uses MACHINE character type for spawning effects
    /// - Can explode on death dealing area damage
    /// - Shield visuals (Note: actual damage reduction would require making EnemyBase.TakeDamage virtual)
    /// 
    /// Usage:
    /// 1. Use this as a base for specific robot types (LightRobot, HeavyRobot, etc.)
    /// 2. Add attack components (AnimationAttack, ProjectileAttack, BeamAttack, etc.)
    /// 3. Configure robot-specific properties in inspector
    /// 4. Set appropriate CharacterType in derived classes (MACHINE_ROBOT, MACHINE_DRONE, etc.)
    /// </summary>
    public class Robot : ModularEnemy
    {
        #region Inspector Fields

        [Header("Robot Settings")]
        [Tooltip("Whether this robot has a shield (visual only - actual damage reduction would require modifying EnemyBase.TakeDamage)")]
        [SerializeField] protected bool hasShield = false;
        
        [Tooltip("Whether the robot explodes on death")]
        [SerializeField] protected bool explodesOnDeath = false;
        
        [Tooltip("Explosion damage (if explodesOnDeath is true)")]
        [SerializeField] protected float explosionDamage = 20f;
        
        [Tooltip("Explosion radius (if explodesOnDeath is true)")]
        [SerializeField] protected float explosionRadius = 5f;
        
        [Tooltip("Effect to play when exploding")]
        [SerializeField] protected EffectSpawnData explosionEffect;
        
        [Tooltip("Visual shield GameObject (will be shown/hidden based on shield status)")]
        [SerializeField] protected GameObject shieldVisual;

        #endregion

        #region Protected Fields

        /// <summary>
        /// Track if shield is currently active
        /// </summary>
        protected bool shieldActive = true;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            // Robots use root motion for animation-driven movement
            // IMPORTANT: Set this BEFORE calling base.Awake() so NavMeshAgent is configured correctly
            useRootMotion = true;
            
            base.Awake();
            
            // Enable shield visual if configured
            if (shieldVisual != null)
            {
                shieldVisual.SetActive(hasShield && shieldActive);
            }
        }

        protected override void Start()
        {
            base.Start();
            
            Debug.Log($"[{gameObject.name}] Robot initialized | Shield: {hasShield} | Explodes: {explodesOnDeath}");
        }

        #endregion

        #region Combat & Damage

        /// <summary>
        /// Override Die to add explosion behavior
        /// </summary>
        public override void Die()
        {
            Debug.Log($"[{gameObject.name}] Robot Die() called! Explodes: {explodesOnDeath}");
            
            // Call base die first
            base.Die();
            
            // Explode if configured
            if (explodesOnDeath)
            {
                ExplodeOnDeath();
            }
        }

        /// <summary>
        /// Handle explosion on death
        /// </summary>
        protected virtual void ExplodeOnDeath()
        {
            Debug.Log($"[{gameObject.name}] Robot exploding with damage: {explosionDamage}, radius: {explosionRadius}");
            
            // Play explosion effect
            if (explosionEffect != null && explosionEffect.IsValid())
            {
                explosionEffect.SpawnEffect(transform);
            }
            
            // Deal damage in radius (uses AttackElement.FIRE for explosion damage)
            DamageUtils.DealDamageInRadius(
                transform.position,
                explosionRadius,
                explosionDamage,
                0f, // No poise damage
                transform,
                AttackElement.FIRE,
                LayerMask.GetMask("Default", "Player", "NPC", "Building") // Hit everything
            );
        }

        #endregion

        #region Shield Management

        /// <summary>
        /// Disable the shield (can be called when shield takes enough damage, etc.)
        /// </summary>
        public virtual void DisableShield()
        {
            if (!hasShield || !shieldActive) return;
            
            shieldActive = false;
            
            if (shieldVisual != null)
            {
                shieldVisual.SetActive(false);
            }
            
            Debug.Log($"[{gameObject.name}] Shield disabled!");
        }

        /// <summary>
        /// Enable the shield (can be called to restore shield, etc.)
        /// </summary>
        public virtual void EnableShield()
        {
            if (!hasShield) return;
            
            shieldActive = true;
            
            if (shieldVisual != null)
            {
                shieldVisual.SetActive(true);
            }
            
            Debug.Log($"[{gameObject.name}] Shield enabled!");
        }

        #endregion

        #region Debug Visualization

        /// <summary>
        /// Draw gizmos to show explosion radius
        /// </summary>
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            
            // Draw explosion radius if configured
            if (explodesOnDeath)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // Orange, semi-transparent
                Gizmos.DrawSphere(transform.position, explosionRadius);
                
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, explosionRadius);
            }
        }

        #endregion
    }
}

