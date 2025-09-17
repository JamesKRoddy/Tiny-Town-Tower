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

    [Serializable]
    public class TaskEffectPair
    {
        public TaskAnimation taskAnimation;
        public EffectDefinition effect;
        [Tooltip("Which IK point to spawn the effect at")]
        public IKPoint ikPoint = IKPoint.NONE;
        [Tooltip("If true, effect will be parented to the IK point")]
        public bool parentToIK = false;
    }

    [SerializeField]
    private List<TaskEffectPair> taskEffects = new List<TaskEffectPair>();

    private Dictionary<IKPoint, Transform> ikPoints;

    private void InitializeIKPoints()
    {
        ikPoints = new Dictionary<IKPoint, Transform>();
        if (animator != null)
        {
            // Get IK targets from animator
            ikPoints[IKPoint.LEFT_HAND] = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            ikPoints[IKPoint.RIGHT_HAND] = animator.GetBoneTransform(HumanBodyBones.RightHand);
            ikPoints[IKPoint.LEFT_FOOT] = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            ikPoints[IKPoint.RIGHT_FOOT] = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            ikPoints[IKPoint.HEAD] = animator.GetBoneTransform(HumanBodyBones.Head);
            // Weapon would need to be set manually or through a reference
        }
    }

    public void Setup(CharacterCombat characterCombat =  null, HumanCharacterController characterController = null, CharacterInventory characterInventory = null)
    {
        combat = characterCombat;
        controller = characterController;
        inventory = characterInventory;
        animator = controller.Animator;

        InitializeIKPoints();

        workLayerIndex = animator.GetLayerIndex("Work Layer");
        if (workLayerIndex == -1)
        {
            Debug.LogError($"[WorkState] Could not find 'Work Layer' in animator for {gameObject.name}");
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
    #endregion

    #region VFX Events
    private void PlayEffectForTaskAnimation(TaskAnimation taskAnimation)
    {
        var pair = taskEffects.Find(p => p.taskAnimation == taskAnimation);
        if (pair != null && pair.effect != null)
        {
            Transform effectTransform = pair.ikPoint == IKPoint.NONE ? transform : 
                (ikPoints.TryGetValue(pair.ikPoint, out var ik) ? ik : transform);
            
            EffectManager.Instance.PlayEffect(effectTransform.position, effectTransform.forward, effectTransform.rotation, pair.parentToIK ? effectTransform : null, pair.effect);
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
}
