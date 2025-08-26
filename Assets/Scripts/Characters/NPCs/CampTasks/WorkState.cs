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
        
        // Use base class method for consistent NavMesh setup
        SetupNavMeshForWorkTask(assignedTask.GetNavMeshDestination(), 0.5f);
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
    }

    public void UpdateTaskDestination()
    {
        if (assignedTask != null)
        {
            // Stop work animation when changing to a new task destination
            if (npc is SettlerNPC settler)
            {
                settler.StopWorkAnimation();
            }
            
            hasReachedTask = false;
            isTaskBeingPerformed = false;
            timeAtTaskLocation = 0f;
            SetupNavMeshPath();
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
        var precisePosition = assignedTask.GetPrecisePosition();
        bool justReached = HandleReachedDestination(ref hasReachedTask, ref needsPrecisePositioning, precisePosition);
        
        if (justReached)
        {
            timeAtTaskLocation = 0f;
        }

        if (needsPrecisePositioning)
        {
            UpdatePrecisePositioning(precisePosition, ref needsPrecisePositioning);
        }

        StartTaskIfReady();
    }



    private void StartTaskIfReady()
    {
        timeAtTaskLocation += Time.deltaTime;
        if (timeAtTaskLocation >= movementSettings.taskStartDelay && !isTaskBeingPerformed && !needsPrecisePositioning)
        {
            // Start work animation and begin working
            npc.PlayWorkAnimation(assignedTask.GetAnimationClipName());
            assignedTask.PerformTask(npc); // Ensure worker is in task's worker list
            isTaskBeingPerformed = true;
        }
        
        // If we're performing the task, do the work each frame
        if (isTaskBeingPerformed)
        {
            bool canContinue = assignedTask.DoWork(npc, Time.deltaTime);
            if (!canContinue)
            {
                // Work is complete or stopped, let StopWork handle the transition
                isTaskBeingPerformed = false;
            }
        }
    }

    private void HandleMovingToTask()
    {
        HandleMovingFromDestination(ref hasReachedTask, ref needsPrecisePositioning);
    }

    private void UpdateAnimations()
    {
        // Use base class method for consistent animation updates
        UpdateMovementAnimation();
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
        // Unsubscribe from the previous task's StopWork event if there was one
        if (assignedTask != null)
        {
            assignedTask.StopWork -= StopWork;
        }
        
        assignedTask = task;
        
        // Subscribe to the new task's StopWork event
        if (assignedTask != null)
        {
            assignedTask.StopWork += StopWork;
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
                return;
            }
        }



        // Stop the work animation since the current task is complete
        if (npc is SettlerNPC settler)
        {
            settler.StopWorkAnimation();
        }

        // Use shared method to assign work or wander
        TryAssignWorkOrWander();
    }
}