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
        var precisePosition = assignedTask.GetPrecisePosition();
        
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
        if (!hasReachedTask)
        {
            hasReachedTask = true;
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            timeAtTaskLocation = 0f;

            // Check if we need precise positioning
            var precisePosition = assignedTask.GetPrecisePosition();
            if (precisePosition != null)
            {
                // Always move to precise position if workLocationTransform is assigned
                // This ensures NPCs are positioned correctly for work animations
                needsPrecisePositioning = true;
                agent.updatePosition = false;
                agent.updateRotation = false;
            }
            else
            {
                needsPrecisePositioning = false;
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
            Vector3 oldPosition = transform.position;
            float distanceToTarget = Vector3.Distance(transform.position, precisePosition.position);
            
            // Use faster lerping for precise positioning (8f for quicker movement)
            float lerpSpeed = 8f;
            transform.position = Vector3.Lerp(transform.position, precisePosition.position, 
                Time.deltaTime * lerpSpeed);
            
            // Use centralized rotation utility
            NavigationUtils.RotateTowardsWorkPoint(transform, precisePosition, lerpSpeed);

            // Check if we've reached the precise position
            float newDistance = Vector3.Distance(transform.position, precisePosition.position);
            

            
            // Use a more generous threshold (0.05f) or if we're very close, snap to position
            if (newDistance <= 0.05f || newDistance >= distanceToTarget) // If we're very close or not getting closer
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