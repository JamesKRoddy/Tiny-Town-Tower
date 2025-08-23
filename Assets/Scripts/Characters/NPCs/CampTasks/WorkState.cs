using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Managers;

public class WorkState : _TaskState
{
    #region Task State
    public WorkTask assignedTask;
    private bool isTaskBeingPerformed = false;
    private bool hasReachedTask = false;
    private float timeAtTaskLocation = 0f;
    private bool needsPrecisePositioning = false;
    #endregion

    #region Movement Parameters
    private class MovementSettings
    {
        public float minDistanceToTask = 0.5f;
        public float taskStartDelay = 0.5f;
        public float precisePositioningThreshold = 0.1f;
    }
    private MovementSettings movementSettings;
    #endregion

    protected override void Awake()
    {
        base.Awake();
    }

    public override void OnEnterState()
    {        
        if (assignedTask == null)
        {
            Debug.LogWarning($"[WorkState] {gameObject.name} entered work state with no assigned task");
            return;
        }

        InitializeWorkState();
        SetupNavMeshPath();
    }

    private void InitializeWorkState()
    {
        movementSettings = new MovementSettings();
        assignedTask.StopWork += StopWork;
    }

    private void SetupNavMeshPath()
    {
        Vector3 taskPosition = assignedTask.GetNavMeshDestination().position;
        
        Debug.Log($"[WorkState] Setting up NavMesh path for {npc.name} from {transform.position} to {taskPosition}");
        
        // Use base class helper for stopping distance
        agent.stoppingDistance = GetEffectiveStoppingDistance(assignedTask.GetNavMeshDestination(), 0.5f);
        agent.SetDestination(taskPosition);
        agent.speed = MaxSpeed();
        agent.angularSpeed = npc.rotationSpeed;
        agent.isStopped = false;
        agent.updatePosition = true;
        agent.updateRotation = true;
        needsPrecisePositioning = false;
        
        // Check if the path is valid
        if (agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathInvalid)
        {
            Debug.LogWarning($"[WorkState] NavMesh path is invalid for {npc.name} to {taskPosition}");
        }
        else if (agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathPartial)
        {
            Debug.LogWarning($"[WorkState] NavMesh path is partial for {npc.name} to {taskPosition}");
        }
        
        Debug.Log($"[WorkState] NavMesh setup complete for {npc.name}. Agent destination: {agent.destination}, isStopped: {agent.isStopped}, pathStatus: {agent.pathStatus}");
    }

    public void UpdateTaskDestination()
    {
        if (assignedTask != null)
        {
            Debug.Log($"[WorkState] Updating task destination for {npc.name} to {assignedTask.GetType().Name} at {assignedTask.GetNavMeshDestination().position}");
            
            hasReachedTask = false;
            isTaskBeingPerformed = false;
            timeAtTaskLocation = 0f;
            SetupNavMeshPath();
        }
        else
        {
            Debug.LogWarning($"[WorkState] UpdateTaskDestination called but assignedTask is null for {npc.name}");
        }
    }

    public override void OnExitState()
    {
        if (isTaskBeingPerformed)
        {
            isTaskBeingPerformed = false;
        }
        
        ResetAgentState();
        
        if (assignedTask != null)
        {
            assignedTask.StopWork -= StopWork;
            assignedTask = null;
        }
    }

    public override void UpdateState()
    {
        if (assignedTask == null) return;

        Transform taskDestination = assignedTask.GetNavMeshDestination();
        
        // Use base class helper for destination reached checking
        bool hasReachedDestination = HasReachedDestination(taskDestination, 0.5f);

        // Debug logging for movement issues
        if (Time.frameCount % 60 == 0) // Log every 60 frames to avoid spam
        {            
            // Check if agent is stuck
            if (agent.velocity.magnitude < 0.1f && !agent.isStopped && !hasReachedDestination)
            {
                Debug.LogWarning($"[WorkState] {npc.name} appears to be stuck! Distance: {Vector3.Distance(transform.position, taskDestination.position):F2}");
            }
        }

        if (hasReachedDestination)
        {
            HandleReachedTask();
        }
        else
        {
            HandleMovingToTask();
        }

        UpdateAnimations();
    }

    private void HandleReachedTask()
    {
        if (!hasReachedTask)
        {
            Debug.Log($"[WorkState] {npc.name} reached task destination - Task: {assignedTask.GetType().Name}");
            
            hasReachedTask = true;
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            timeAtTaskLocation = 0f;

            // Check if we need precise positioning
            var precisePosition = assignedTask.GetPrecisePosition();
            if (precisePosition != null)
            {
                float distanceToExactPosition = Vector3.Distance(transform.position, precisePosition.position);
                needsPrecisePositioning = distanceToExactPosition > movementSettings.precisePositioningThreshold;
                
                Debug.Log($"[WorkState] {npc.name} needs precise positioning: {needsPrecisePositioning} (distance: {distanceToExactPosition:F2})");
                
                if (needsPrecisePositioning)
                {
                    agent.updatePosition = false;
                    agent.updateRotation = false;
                }
            }
            else
            {
                needsPrecisePositioning = false;
                Debug.Log($"[WorkState] {npc.name} no precise positioning needed");
            }
        }

        if (needsPrecisePositioning)
        {
            UpdatePositionAndRotation();
        }

        StartTaskIfReady();
    }

    private void UpdatePositionAndRotation()
    {
        var precisePosition = assignedTask.GetPrecisePosition();
        if (needsPrecisePositioning && precisePosition != null)
        {
            // Use lerping for precise positioning
            transform.position = Vector3.Lerp(transform.position, precisePosition.position, 
                Time.deltaTime * 5f);
            
            // Use centralized rotation utility
            NavigationUtils.RotateTowardsWorkPoint(transform, precisePosition, 5f);

            // Check if we've reached the precise position
            float distanceToExactPosition = Vector3.Distance(transform.position, precisePosition.position);
            if (distanceToExactPosition <= movementSettings.precisePositioningThreshold)
            {
                needsPrecisePositioning = false;
                transform.position = precisePosition.position;
                transform.rotation = precisePosition.rotation;
            }
        }
    }

    private void StartTaskIfReady()
    {
        timeAtTaskLocation += Time.deltaTime;
        if (timeAtTaskLocation >= movementSettings.taskStartDelay && !isTaskBeingPerformed && !needsPrecisePositioning)
        {
            Debug.Log($"[WorkState] Starting task for {npc.name} - Task: {assignedTask.GetType().Name}, Animation: {assignedTask.GetAnimationClipName()}");
            
            // Start work animation and begin working
            npc.PlayWorkAnimation(assignedTask.GetAnimationClipName());
            assignedTask.PerformTask(npc); // Ensure worker is in task's worker list
            isTaskBeingPerformed = true;
            
            Debug.Log($"[WorkState] Task started for {npc.name} - isTaskBeingPerformed: {isTaskBeingPerformed}");
        }
        
        // If we're performing the task, do the work each frame
        if (isTaskBeingPerformed)
        {
            bool canContinue = assignedTask.DoWork(npc, Time.deltaTime);
            if (!canContinue)
            {
                Debug.Log($"[WorkState] DoWork returned false for {npc.name} - stopping task performance");
                // Work is complete or stopped, let StopWork handle the transition
                isTaskBeingPerformed = false;
            }
        }
    }

    private void HandleMovingToTask()
    {
        if (hasReachedTask)
        {
            hasReachedTask = false;
            agent.isStopped = false;
            agent.updatePosition = true;
            agent.updateRotation = true;
            needsPrecisePositioning = false;
        }
    }

    private void UpdateAnimations()
    {
        float maxSpeed = MaxSpeed();
        float currentSpeedNormalized = agent.velocity.magnitude / maxSpeed;
        animator.SetFloat("Speed", currentSpeedNormalized);
    }

    public override float MaxSpeed()
    {
        return npc.moveMaxSpeed * 0.4f;
    }

    public override TaskType GetTaskType()
    {
        return TaskType.WORK;
    }

    public void AssignTask(WorkTask task)
    {
        Debug.Log($"[WorkState] AssignTask called for {npc.name} - Previous task: {(assignedTask != null ? assignedTask.GetType().Name : "null")}, New task: {(task != null ? task.GetType().Name : "null")}");
        
        // Unsubscribe from the previous task's StopWork event if there was one
        if (assignedTask != null)
        {
            assignedTask.StopWork -= StopWork;
            Debug.Log($"[WorkState] Unsubscribed from StopWork event for previous task: {assignedTask.GetType().Name}");
        }
        
        assignedTask = task;
        
        // Subscribe to the new task's StopWork event
        if (assignedTask != null)
        {
            assignedTask.StopWork += StopWork;
            Debug.Log($"[WorkState] Subscribed to StopWork event for new task: {assignedTask.GetType().Name}");
        }
    }

    public void StopWork()
    {
        // Don't stop work if:
        // 1. Task has queued tasks waiting
        // 2. Task is currently occupied by workers
        // 3. Task is a QueuedWorkTask with current work in progress
        if (assignedTask != null)
        {
            bool hasQueuedTasks = assignedTask.HasQueuedTasks;
            bool isOccupied = assignedTask.IsOccupied;
            bool hasCurrentWork = false;
            
            // Check if it's a QueuedWorkTask with current work
            if (assignedTask is QueuedWorkTask queuedTask)
            {
                hasCurrentWork = queuedTask.HasCurrentWork;
            }
            
            if (hasQueuedTasks || isOccupied || hasCurrentWork)
            {
                Debug.Log($"[WorkState] Not stopping work for {npc.name} - HasQueuedTasks: {hasQueuedTasks}, IsOccupied: {isOccupied}, HasCurrentWork: {hasCurrentWork}");
                return;
            }
        }

        Debug.Log($"[WorkState] Stopping work for {npc.name} - task completed");

        // Stop the work animation since the current task is complete
        if (npc is SettlerNPC settler)
        {
            settler.StopWorkAnimation();
        }

        // Use shared method to assign work or wander
        TryAssignWorkOrWander();
    }
}