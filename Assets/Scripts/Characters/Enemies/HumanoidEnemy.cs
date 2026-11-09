using UnityEngine;
using UnityEngine.AI;

namespace Enemies
{
    /// <summary>
    /// Base class for humanoid enemies (zombies, robots, humanoid bosses).
    /// Extends ModularEnemy with humanoid-specific features like melee rotation, IK handling, etc.
    /// 
    /// This class handles:
    /// - Humanoid-specific rotation logic for melee attacks
    /// - Animator IK for humanoid rigs
    /// - Root motion configuration for walking/running animations
    /// 
    /// Usage:
    /// 1. Use this as the base for any humanoid enemy (Zombie, Robot, HumanoidBoss)
    /// 2. Set useRootMotion = true in Awake() before calling base.Awake() if using root motion
    /// 3. Add attack components (CloseRangeAttack, ProjectileAttack, etc.)
    /// 4. Configure attack selection strategy in inspector
    /// </summary>
    public class HumanoidEnemy : ModularEnemy
    {
        #region Appearance System
        
        /// <summary>
        /// Procedural appearance system for randomizing enemy look
        /// </summary>
        [Header("Appearance System")]
        [SerializeField] protected CharacterAppearanceSystem appearanceSystem;
        
        /// <summary>
        /// Randomize enemy appearance on spawn
        /// </summary>
        [SerializeField] protected bool randomizeAppearanceOnSpawn = true;
        
        #endregion
        
        #region Constants
        
        /// <summary>
        /// Angle threshold for melee attacks - enemy must face target within this angle
        /// </summary>
        protected const float MELEE_ATTACK_ANGLE_THRESHOLD = 30f;
        
        #endregion
        
        #region Unity Lifecycle
        
        protected override void Awake()
        {
            // Note: Derived classes should set useRootMotion before calling base.Awake()
            // if they want to use root motion animations
            base.Awake();
            
            // Initialize appearance system
            if (appearanceSystem != null)
            {
                appearanceSystem.Initialize(transform, gameObject.name);
                
                // Randomize appearance if configured
                if (randomizeAppearanceOnSpawn)
                {
                    appearanceSystem.RandomizeAppearance();
                }
            }
        }
        
        #endregion
        
        #region Combat - Humanoid Specific
        
        /// <summary>
        /// Rotate towards target for melee attacks.
        /// Humanoids typically need precise facing for melee strikes with tighter angle threshold.
        /// </summary>
        /// <returns>True if rotation is complete and ready to attack</returns>
        protected override bool RotateTowardsTargetForAttack()
        {
            if (navMeshTarget == null || Health <= 0) return false;
            
            // Use attack-specific angle threshold if available
            float angleThreshold = MELEE_ATTACK_ANGLE_THRESHOLD;
            var current = GetCurrentAttack();
            if (current != null)
            {
                angleThreshold = Mathf.Max(angleThreshold, current.attackAngleThreshold);
            }
            
            // Aggressive rotation - use base class implementation with custom threshold
            Vector3 direction = (navMeshTarget.position - transform.position).normalized;
            direction.y = 0;
            
            if (direction.sqrMagnitude < 0.0001f) return true;
            
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            float rotationSpeed = GetRotationSpeedWithAnimatorMultiplier() * ROTATION_TOWARDS_TARGET_SPEED_MULTIPLIER;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            
            // Check if we're facing the target (within threshold)
            float angle = Quaternion.Angle(transform.rotation, targetRotation);
            return angle <= angleThreshold;
        }
        
        #endregion
        
        #region Animation & IK
        
        /// <summary>
        /// Called by Unity for IK (Inverse Kinematics) updates.
        /// Handles both general head tracking and attack-specific IK behavior for humanoid rigs.
        /// This override adds humanoid-specific IK handling on top of ModularEnemy's IK.
        /// </summary>
        protected override void OnAnimatorIK(int layerIndex)
        {
            // Call base (ModularEnemy) which handles attack IK and hit reactions
            base.OnAnimatorIK(layerIndex);
            
            // Additional humanoid-specific IK can be added here if needed
            // For example: finger positioning, foot IK, etc.
            
            // Note: Most IK is already handled by base classes:
            // - EnemyBase handles hit reaction IK (torso/head recoil)
            // - ModularEnemy forwards attack IK to current attack component
            // - AttackBase components handle weapon aiming IK
        }
        
        #endregion
        
        #region Public Accessors
        
        /// <summary>
        /// Get access to the enemy's appearance system
        /// </summary>
        public CharacterAppearanceSystem GetAppearanceSystem()
        {
            return appearanceSystem;
        }
        
        /// <summary>
        /// Manually trigger appearance randomization
        /// </summary>
        public void RandomizeAppearance()
        {
            if (appearanceSystem != null)
            {
                appearanceSystem.RandomizeAppearance();
            }
            else
            {
                Debug.LogWarning($"[HumanoidEnemy] {name} - Cannot randomize appearance: appearanceSystem is null");
            }
        }
        
        /// <summary>
        /// Set specific appearance data (for save/load or predefined appearances)
        /// </summary>
        public void SetAppearance(CharacterAppearanceData appearanceData)
        {
            if (appearanceSystem != null)
            {
                appearanceSystem.SetAppearance(appearanceData);
            }
            else
            {
                Debug.LogWarning($"[HumanoidEnemy] {name} - Cannot set appearance: appearanceSystem is null");
            }
        }
        
        /// <summary>
        /// Get current appearance data (for save/load)
        /// </summary>
        public CharacterAppearanceData GetCurrentAppearanceData()
        {
            if (appearanceSystem != null)
            {
                return appearanceSystem.GetCurrentAppearanceData();
            }
            else
            {
                Debug.LogWarning($"[HumanoidEnemy] {name} - Cannot get appearance data: appearanceSystem is null");
                return null;
            }
        }
        
        #endregion
        
        #region Debug
        
        /// <summary>
        /// Draw debug gizmos for humanoid enemies
        /// </summary>
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            
            // Draw melee attack cone
            if (Application.isPlaying && navMeshTarget != null)
            {
                Vector3 directionToTarget = (navMeshTarget.position - transform.position).normalized;
                float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
                
                // Show if within melee attack angle
                if (angleToTarget <= MELEE_ATTACK_ANGLE_THRESHOLD)
                {
                    Gizmos.color = Color.green;
                }
                else
                {
                    Gizmos.color = Color.red;
                }
                
                // Draw forward direction
                Gizmos.DrawRay(transform.position + Vector3.up, transform.forward * 2f);
                
                // Draw attack angle cone
                Vector3 rightBound = Quaternion.Euler(0, MELEE_ATTACK_ANGLE_THRESHOLD, 0) * transform.forward;
                Vector3 leftBound = Quaternion.Euler(0, -MELEE_ATTACK_ANGLE_THRESHOLD, 0) * transform.forward;
                
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                Gizmos.DrawRay(transform.position + Vector3.up, rightBound * 2f);
                Gizmos.DrawRay(transform.position + Vector3.up, leftBound * 2f);
            }
        }
        
        #endregion
    }
}

