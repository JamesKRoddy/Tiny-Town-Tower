using UnityEngine;
using System;
using System.Collections.Generic;
using Managers;

public class CharacterAnimationEvents : MonoBehaviour
{
    private CharacterCombat combat;
    private HumanCharacterController controller;
    private CharacterInventory inventory;
    private int workLayerIndex;

    private Animator animator;
    
    // Footstep system
    private IDamageable damageable; // To get character type
    
    [Header("Footstep Settings")]
    [Tooltip("Enable to see debug information about footstep detection")]
    [SerializeField] private bool debugFootsteps = false;
    
    [Tooltip("Raycast distance for surface detection")]
    [SerializeField] private float surfaceDetectionDistance = 1.0f;

    /// <summary>
    /// Maps task animations to effect spawn data for work VFX
    /// Uses EffectSpawnData for flexible effect spawning at IK points
    /// </summary>
    [Serializable]
    public class TaskEffectPair
    {
        public TaskAnimation taskAnimation;
        public EffectSpawnData effectSpawnData;
    }

    [SerializeField]
    private List<TaskEffectPair> taskEffects = new List<TaskEffectPair>();

    public void Setup(CharacterCombat characterCombat =  null, HumanCharacterController characterController = null, CharacterInventory characterInventory = null)
    {
        combat = characterCombat;
        controller = characterController;
        inventory = characterInventory;
        animator = controller?.Animator;

        if (animator != null)
        {
            workLayerIndex = animator.GetLayerIndex("Work Layer");
            if (workLayerIndex == -1)
            {
                Debug.LogError($"[WorkState] Could not find 'Work Layer' in animator for {gameObject.name}");
            }
        }
        
        // Get damageable for character type
        damageable = GetComponent<IDamageable>();
    }
    
    private void Awake()
    {
        // Fallback initialization if Setup isn't called
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        if (damageable == null)
        {
            damageable = GetComponent<IDamageable>();
        }
    }

    #region Animation Events
    /// <summary>
    /// Called from animator, enables the weapon vfx
    /// HORIZONTAL_LEFT = 0, HORIZONTAL_RIGHT = 1, VERTICAL_DOWN = 2, VERTICAL_UP = 3
    /// </summary>
    /// <param name="attackDirection"></param>
    public void AttackVFX(int attackDirection)
    {
        if(combat != null)
            combat.AttackVFX(attackDirection);
    }

    /// <summary>
    /// Called from animator, enables the weapon hitbox for melee weapons
    /// </summary>
    public void UseWeapon()
    {
        if (inventory.equippedWeaponScriptObj != null)
            inventory.equippedWeaponBase.Use();
    }

    /// <summary>
    /// Called from animator, disables the weapon hitbox for melee weapons
    /// </summary>
    public void StopWeapon()
    {
        if (inventory.equippedWeaponScriptObj != null)
            inventory.equippedWeaponBase.StopUse();
    }
    
    /// <summary>
    /// Called from walk/run animations for LEFT foot
    /// Uses humanoid IK to get accurate foot position
    /// Add this as an Animation Event in your walk/run animations at the frame where the left foot touches ground
    /// </summary>
    /// <param name="intensity">Optional intensity 0-1 (controls particle scale and audio volume, default 1.0)</param>
    public void FootstepLeft(float intensity = 1.0f)
    {
        PlayFootstepAtFoot(AvatarIKGoal.LeftFoot, HumanBodyBones.LeftFoot, intensity);
    }
    
    /// <summary>
    /// Called from walk/run animations for RIGHT foot
    /// Uses humanoid IK to get accurate foot position
    /// Add this as an Animation Event in your walk/run animations at the frame where the right foot touches ground
    /// </summary>
    /// <param name="intensity">Optional intensity 0-1 (controls particle scale and audio volume, default 1.0)</param>
    public void FootstepRight(float intensity = 1.0f)
    {
        PlayFootstepAtFoot(AvatarIKGoal.RightFoot, HumanBodyBones.RightFoot, intensity);
    }
    
    /// <summary>
    /// Generic footstep event that uses character position (for non-humanoid or simple footsteps)
    /// Can be called from animations that don't need precise foot positioning
    /// </summary>
    /// <param name="intensity">Optional intensity 0-1 (controls particle scale and audio volume, default 1.0)</param>
    public void Footstep(float intensity = 1.0f)
    {
        if (Managers.EffectManager.Instance == null || damageable == null) return;
        
        // Detect surface at character position - start from ABOVE to detect triggers at any height
        Vector3 effectPosition;
        SurfaceType surfaceType = SurfaceDetector.DetectSurfaceAtCharacter(transform, out effectPosition, 2.0f, surfaceDetectionDistance + 2.5f);
        
        if (debugFootsteps)
        {
            Debug.Log($"[Footstep] {gameObject.name} - Surface: {surfaceType} at {effectPosition} (intensity: {intensity:F2})");
        }
        
        // Play footstep effect at detected surface position (handles triggers like water/poison)
        Managers.EffectManager.Instance.PlayFootstepEffect(
            effectPosition, 
            Vector3.up, 
            damageable.CharacterType, 
            surfaceType,
            intensity
        );
    }
    #endregion

    #region VFX Events
    /// <summary>
    /// Plays the effect associated with a specific task animation using EffectSpawnData
    /// </summary>
    private void PlayEffectForTaskAnimation(TaskAnimation taskAnimation)
    {
        var pair = taskEffects.Find(p => p.taskAnimation == taskAnimation);
        if (pair != null && pair.effectSpawnData != null && pair.effectSpawnData.IsValid())
        {
            // Spawn effect using EffectSpawnData, with transform as fallback if no spawnPoint is set
            pair.effectSpawnData.SpawnEffect(transform);
        }
    }

    public void PlayTaskAnimationEffect(string taskAnimationEnum)
    {
        // If string is empty, get TaskAnimation from the current WorkTask
        if (string.IsNullOrEmpty(taskAnimationEnum))
        {
            if (controller != null && controller is SettlerNPC settler)
            {
                var workTask = settler.GetAssignedWork();
                if (workTask != null)
                {
                    PlayEffectForTaskAnimation(workTask.taskAnimation);
                    return;
                }
            }
            Debug.LogWarning("[CharacterAnimationEvents] No work task found to get TaskAnimation from");
            return;
        }

        // If we have a string, try to parse it
        if (Enum.TryParse<TaskAnimation>(taskAnimationEnum, out var taskAnimation))
        {
            PlayEffectForTaskAnimation(taskAnimation);
        }
        else
        {
            Debug.LogError($"[CharacterAnimationEvents] Failed to parse animation name '{taskAnimationEnum}' to TaskAnimation enum. Make sure the animation name matches an enum value in TaskAnimation.");
        }
    }
    #endregion
    
    #region Footstep Helpers
    /// <summary>
    /// Plays footstep effect at a specific foot bone position using IK
    /// This provides accurate foot placement for humanoid characters
    /// </summary>
    /// <param name="ikGoal">Which foot IK goal to use</param>
    /// <param name="footBone">Which foot bone to use as fallback</param>
    /// <param name="intensity">Intensity 0-1 controlling particle scale and audio volume</param>
    private void PlayFootstepAtFoot(AvatarIKGoal ikGoal, HumanBodyBones footBone, float intensity = 1.0f)
    {
        if (Managers.EffectManager.Instance == null || damageable == null) return;
        
        Vector3 footPosition = transform.position; // Default fallback
        Vector3 footNormal = Vector3.up;
        bool hitGround = false;
        
        // Try to get accurate foot position from animator IK
        if (animator != null && animator.isHuman)
        {
            // Get foot bone transform
            Transform footTransform = animator.GetBoneTransform(footBone);
            if (footTransform != null)
            {
                footPosition = footTransform.position;
                
                // Create layer mask that ignores character layers
                int layerMask = GameConstants.Layers.GroundDetectionMask;
                
                // Raycast down from foot to find exact ground contact point
                // Start higher up to avoid foot being inside ground
                RaycastHit hit;
                Vector3 rayStart = footPosition + Vector3.up * 0.5f;
                float rayDistance = surfaceDetectionDistance + 0.5f; // Add extra distance for the higher start
                    
                    if (Physics.Raycast(rayStart, Vector3.down, out hit, rayDistance, layerMask, QueryTriggerInteraction.Ignore))
                    {
                        footPosition = hit.point;
                        footNormal = hit.normal;
                        hitGround = true;
                        
                        if (debugFootsteps)
                        {
                            Debug.DrawLine(rayStart, hit.point, Color.green, 2f);
                        }
                    }
                    else
                    {
                        if (debugFootsteps)
                        {
                            Debug.DrawLine(rayStart, rayStart + Vector3.down * rayDistance, Color.red, 2f);
                        }
                    }
            }
        }
        
        // Only play effect if we actually hit ground
        if (!hitGround)
        {
            return;
        }
        
        // Detect surface type - start from ABOVE character to detect triggers at any height (water, poison, etc.)
        // Start high enough to catch waist-height triggers, then raycast down through entire character
        Vector3 effectPosition;
        Vector3 detectionStart = transform.position + Vector3.up * 2.0f; // Start from above character's head
        SurfaceType surfaceType = SurfaceDetector.DetectSurface(detectionStart, out effectPosition, surfaceDetectionDistance + 2.5f, ignoreTransform: transform, debugLog: debugFootsteps);
        
        if (debugFootsteps)
        {
            string footName = ikGoal == AvatarIKGoal.LeftFoot ? "LEFT" : "RIGHT";
            Debug.Log($"[Footstep] {gameObject.name} - {footName} foot on {surfaceType} at {effectPosition} (intensity: {intensity:F2})");
        }
        
        // Play footstep effect at detected surface position (handles triggers like water/poison)
        Managers.EffectManager.Instance.PlayFootstepEffect(
            effectPosition, 
            footNormal, 
            damageable.CharacterType, 
            surfaceType,
            intensity
        );
    }
    #endregion
}
