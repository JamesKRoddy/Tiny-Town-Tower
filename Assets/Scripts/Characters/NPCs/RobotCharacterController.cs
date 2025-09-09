using UnityEngine;
using UnityEngine.AI;
using Managers;

public class RobotCharacterController : HumanCharacterController, INarrativeTarget
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
        // Don't do anything if dead
        if (Health <= 0) 
        {
            return;
        }
        
        // Update conversation rotation if in conversation
        UpdateConversationRotation();
        
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
                // We're close enough, just handle rotation using centralized utility
                NavigationUtils.RotateTowardsWorkPoint(transform, workLocation, 5f);
            }
        }
        else{
            // We're at the work location, just rotate to face it using centralized utility
            NavigationUtils.RotateTowardsTarget(transform, currentWorkTask.transform, 5f, true);
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
            PlayWorkAnimation(currentWorkTask.GetAnimationClipName());
            
            // Call the base class StartWork to handle the work coroutine
            base.StartWork(newTask);

            currentWorkTask.StopWork += HandleWorkCompleted;
        }
    }

    public void StopWork()
    {
        if (isWorking)
        {
            currentWorkTask.StopWork -= HandleWorkCompleted;
            isWorking = false;
            
            // Call the base class StopWork to handle the work coroutine
            base.StopWork();
            
            currentWorkTask = null;
            animator.Play("Empty", workLayerIndex);
        }
    }

    #region Conversation Control

    private bool isInConversation = false;

    /// <summary>
    /// Pauses the robot's AI and movement during conversations
    /// </summary>
    public void PauseForConversation()
    {
        isInConversation = true;
        
        // Stop any current work
        if (isWorking)
        {
            StopWork();
        }
    }

    /// <summary>
    /// Resumes the robot's AI and movement after conversations
    /// </summary>
    public void ResumeAfterConversation()
    {
        isInConversation = false;
    }

    /// <summary>
    /// Update conversation rotation - called during conversation to face the player
    /// </summary>
    public void UpdateConversationRotation()
    {
        if (!isInConversation || PlayerController.Instance?._possessedNPC == null) return;

        Transform playerTransform = PlayerController.Instance._possessedNPC.GetTransform();
        NavigationUtils.RotateTowardsPlayerForConversation(transform, playerTransform, rotationSpeed);
    }

    #endregion

    public new Transform GetTransform()
    {
        return transform;
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