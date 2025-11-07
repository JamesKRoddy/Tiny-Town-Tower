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
        animator = controller.Animator;

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
}
