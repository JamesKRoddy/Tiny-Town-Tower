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
    private int workLayerIndex = -1;
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
        workLayerIndex = animator.GetLayerIndex("Work Layer");
        if (workLayerIndex == -1)
        {
            Debug.LogError($"[WorkState] Could not find 'Work Layer' in animator for {gameObject.name}");
        }

        movementSettings = new MovementSettings();
        assignedTask.StopWork += StopWork;
    }

    private void SetupNavMeshPath()
    {
        Vector3 taskPosition = assignedTask.WorkTaskTransform().position;
        agent.stoppingDistance = movementSettings.minDistanceToTask;
        agent.SetDestination(taskPosition);
        agent.speed = MaxSpeed();
        agent.angularSpeed = npc.rotationSpeed;
        agent.isStopped = false;
        agent.updatePosition = true;
        agent.updateRotation = true;
        needsPrecisePositioning = false;
    }

    public void UpdateTaskDestination()
    {
        if (assignedTask != null)
        {
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
            animator.Play("Empty", workLayerIndex);
            isTaskBeingPerformed = false;
        }
        
        ResetAgentState();
        
        if (assignedTask != null)
        {
            assignedTask.StopWork -= StopWork;
            assignedTask = null;
        }
    }

    private void ResetAgentState()
    {
        agent.speed = MaxSpeed();
        agent.angularSpeed = npc.rotationSpeed;
        agent.isStopped = false;
        agent.stoppingDistance = stoppingDistance;
        agent.updatePosition = true;
        agent.updateRotation = true;
    }

    public override void UpdateState()
    {
        if (assignedTask == null) return;

        float distanceToTask = Vector3.Distance(transform.position, assignedTask.WorkTaskTransform().position);

        if (!agent.pathPending && agent.remainingDistance <= movementSettings.minDistanceToTask)
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
            float distanceToExactPosition = Vector3.Distance(transform.position, assignedTask.WorkTaskTransform().position);
            needsPrecisePositioning = distanceToExactPosition > movementSettings.precisePositioningThreshold;
            
            if (needsPrecisePositioning)
            {
                agent.updatePosition = false;
                agent.updateRotation = false;
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
        if (needsPrecisePositioning)
        {
            // Use lerping for precise positioning
            transform.position = Vector3.Lerp(transform.position, assignedTask.WorkTaskTransform().position, 
                Time.deltaTime * 5f);
            transform.rotation = Quaternion.Slerp(transform.rotation, assignedTask.WorkTaskTransform().rotation, 
                Time.deltaTime * 5f);

            // Check if we've reached the precise position
            float distanceToExactPosition = Vector3.Distance(transform.position, assignedTask.WorkTaskTransform().position);
            if (distanceToExactPosition <= movementSettings.precisePositioningThreshold)
            {
                needsPrecisePositioning = false;
                transform.position = assignedTask.WorkTaskTransform().position;
                transform.rotation = assignedTask.WorkTaskTransform().rotation;
            }
        }
    }

    private void StartTaskIfReady()
    {
        timeAtTaskLocation += Time.deltaTime;
        if (timeAtTaskLocation >= movementSettings.taskStartDelay && !isTaskBeingPerformed && !needsPrecisePositioning)
        {
            assignedTask.PerformTask(npc);
            if (workLayerIndex != -1)
            {
                animator.Play(assignedTask.GetAnimationClipName(), workLayerIndex);
            }
            isTaskBeingPerformed = true;
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
        assignedTask = task;
    }

    public void StopWork()
    {
        if (assignedTask != null && (assignedTask.HasQueuedTasks || assignedTask.IsOccupied))
        {
            return;
        }

        npc.ChangeTask(TaskType.WANDER);
    }
}