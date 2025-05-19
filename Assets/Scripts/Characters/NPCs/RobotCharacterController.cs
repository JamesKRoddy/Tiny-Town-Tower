using UnityEngine;
using UnityEngine.AI;
using Managers;

public class RobotCharacterController : HumanCharacterController
{
    [Header("Robot Parameters")]
    [SerializeField] private float workSpeedMultiplier = 1.5f;
    [SerializeField] private float moveSpeed = 5f;
    private WorkTask currentWorkTask;
    private bool isWorking = false;
    private int workLayerIndex = 3;

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
            // Robot is working, handle work location movement
            if (currentWorkTask != null)
            {
                MoveToWorkLocation();
            }
            UpdateAnimations();
        }
    }

    private void MoveToWorkLocation()
    {
        if (currentWorkTask == null) return;

        Transform workLocation = currentWorkTask.WorkTaskTransform();
        if (workLocation != null && workLocation != currentWorkTask.transform)
        {
            // Move to work location
            Vector3 targetPosition = workLocation.position;
            Quaternion targetRotation = workLocation.rotation;

            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
            
            // Only move if we're not close enough
            if (distanceToTarget > 0.1f)
            {
                // Move towards the target position
                Vector3 moveDirection = (targetPosition - transform.position).normalized;
                transform.position += moveDirection * moveSpeed * Time.deltaTime;
            }
            else
            {
                // We're close enough, just handle rotation
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            }
        }
        else{
            // We're at the work location, just rotate to face it
            Vector3 directionToWork = (currentWorkTask.transform.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(directionToWork);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    public override void StartWork(WorkTask newTask)
    {
        if(newTask == currentWorkTask){
            return;
        }
        
        if (newTask != null && !isWorking)
        {
            currentWorkTask = newTask;
            isWorking = true;
            animator.Play(currentWorkTask.GetAnimationClipName(), workLayerIndex);
            // Start the work task once
            currentWorkTask.PerformTask(this);

            currentWorkTask.StopWork += HandleWorkCompleted;
        }
    }

    public void StopWork()
    {
        if (isWorking)
        {
            currentWorkTask.StopWork -= HandleWorkCompleted;
            isWorking = false;
            currentWorkTask.StopWorkCoroutine();
            currentWorkTask = null;
            animator.Play("Empty", workLayerIndex);

            if(PlayerInput.Instance.CurrentControlType != PlayerControlType.IN_MENU){
                PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.ROBOT_MOVEMENT);
            }
        }
    }

    private void HandleWorkCompleted()
    {        
        // Check if there are more tasks in the queue
        if (currentWorkTask != null && !currentWorkTask.IsTaskCompleted)
        {
            // Keep the animation playing and let the WorkTask handle the next task
            return;
        }
        
        // Only stop work if there are truly no more tasks
        if (currentWorkTask.IsTaskCompleted)
        {
            StopWork();
        }
    }

    protected void OnDisable()
    {
        if (currentWorkTask != null)
        {
            currentWorkTask.StopWork -= HandleWorkCompleted;
        }
    }

    public bool IsWorking()
    {
        return isWorking;
    }
} 