using UnityEngine;
using UnityEngine.AI;
using Managers;

public class RobotCharacterController : HumanCharacterController
{
    [Header("Robot Parameters")]
    [SerializeField] private float workSpeedMultiplier = 1.5f;
    private WorkTask currentWorkTask;
    private bool isWorking = false;
    private int workLayerIndex = -1;

    protected override void Awake()
    {
        base.Awake();
        characterType = CharacterType.MACHINE_ROBOT;
    }

    public override void PossessedUpdate()
    {
        if (!isWorking)
        {
            MoveCharacter();
            UpdateAnimations();
        }
        else
        {
            // Robot is working, update work progress
            if (currentWorkTask != null)
            {
                currentWorkTask.PerformTask(null); // Pass null since robot doesn't need an NPC reference
            }
        }
    }

    public void StartWork(WorkTask workTask)
    {
        if (workTask != null && !isWorking)
        {
            currentWorkTask = workTask;
            isWorking = true;
            PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.ROBOT_WORKING);
            animator.Play(currentWorkTask.workType.ToString(), workLayerIndex);
        }
    }

    public void StopWork()
    {
        if (isWorking)
        {
            isWorking = false;
            currentWorkTask = null;
            PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.ROBOT_MOVEMENT);
            animator.Play("Empty", workLayerIndex);
        }
    }

    public bool IsWorking()
    {
        return isWorking;
    }

    public WorkTask GetCurrentWorkTask()
    {
        return currentWorkTask;
    }

    public float GetWorkSpeedMultiplier()
    {
        return workSpeedMultiplier;
    }
} 