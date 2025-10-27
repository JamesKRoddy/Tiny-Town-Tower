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
    private Vector3 distributedDestination; // Store the actual distributed destination we're navigating to
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
        
        // WorkState-specific stopping distance - NPCs need to be closer for work tasks
        stoppingDistance = 0.2f;
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
        // Get distributed work position to prevent NPCs from clustering
        distributedDestination = assignedTask.GetDistributedWorkPosition(npc.transform.position);
        
        Debug.Log($"[WorkState] {npc.name} setting up path to construction site at {distributedDestination}");
        
        // Set destination to the distributed position
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            // Set the agent's stopping distance for work tasks
            agent.stoppingDistance = stoppingDistance;
            
            // Make sure the agent is not stopped from a previous state
            agent.isStopped = false;
            agent.SetDestination(distributedDestination);
            Debug.Log($"[WorkState] {npc.name} NavMeshAgent destination set to {distributedDestination}, stopping distance: {agent.stoppingDistance}");
        }
        else
        {
            Debug.LogWarning($"[WorkState] {npc.name} cannot set destination - agent null: {agent == null}, enabled: {agent?.enabled}, onNavMesh: {agent?.isOnNavMesh}");
        }
        
        needsPrecisePositioning = false;
        
        // Check if the path is valid
        if (agent != null && agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathInvalid)
        {
            Debug.LogWarning($"[WorkState] NavMesh path is invalid for {npc.name} to {distributedDestination}");
        }
        else if (agent != null && agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathPartial)
        {
            Debug.LogWarning($"[WorkState] NavMesh path is partial for {npc.name} to {distributedDestination}");
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

        // Check against the actual distributed destination we're navigating to
        // Use the agent's stopping distance plus a small buffer to account for NavMesh behavior
        // and distributed positioning around obstacles. This ensures the threshold is always
        // achievable by the agent.
        float distanceToDestination = Vector3.Distance(npc.transform.position, distributedDestination);
        float reachThreshold = agent != null ? agent.stoppingDistance + 0.5f : stoppingDistance + 0.5f;
        bool hasReachedDestination = distanceToDestination <= reachThreshold;

        // Debug log every 2 seconds to track movement
        if (Time.frameCount % 120 == 0)
        {
            Debug.Log($"[WorkState] {npc.name} distance to work destination: {distanceToDestination:F2}, threshold: {reachThreshold:F2}, destination: {distributedDestination}, hasReached: {hasReachedDestination}");
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
    
    /// <summary>
    /// Override stamina update for work-specific enhanced drain
    /// </summary>
    public override void UpdateStamina()
    {
        // Work drains stamina 1.5x faster than normal activities
        float baseDrain = npc.GetBaseStaminaDrainRate();
        float workDrain = baseDrain * 1.5f * Time.deltaTime;
        
        npc.ApplyStaminaChange(-workDrain, "Work drain");
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

        // Clear the assigned task since work is complete
        assignedTask = null;

        // Use shared method to assign work or wander
        TryAssignWorkOrWander();
    }
}