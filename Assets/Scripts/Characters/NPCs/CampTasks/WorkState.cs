using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Managers;

public class WorkState : _TaskState
{
    #region Task State
    private WorkTask assignedTask;
    private bool isTaskBeingPerformed = false;
    private bool hasReachedTask = false;
    private float timeAtTaskLocation = 0f;
    private int workLayerIndex = -1;
    #endregion

    #region Movement Parameters
    private class MovementSettings
    {
        public float minDistanceToTask = 0.5f;
        public float taskStartDelay = 0.5f;
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
            
            if(assignedTask.WorkTaskTransform() != assignedTask.transform)
            {
                agent.updatePosition = false;
                agent.updateRotation = false;
            }
        }

        UpdatePositionAndRotation();
        StartTaskIfReady();
    }

    private void UpdatePositionAndRotation()
    {
        if(assignedTask.WorkTaskTransform() != assignedTask.transform)
        {
            transform.position = Vector3.Lerp(transform.position, assignedTask.WorkTaskTransform().position, 
                Time.deltaTime * 5f);
            transform.rotation = Quaternion.Slerp(transform.rotation, assignedTask.WorkTaskTransform().rotation, 
                Time.deltaTime * 5f);
        }
        else
        {
            Vector3 directionToTask = (assignedTask.WorkTaskTransform().position - transform.position).normalized;
            directionToTask.y = 0;
            if (directionToTask != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTask);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
                    npc.rotationSpeed * Time.deltaTime);
            }
        }
    }

    private void StartTaskIfReady()
    {
        timeAtTaskLocation += Time.deltaTime;
        if (timeAtTaskLocation >= movementSettings.taskStartDelay && !isTaskBeingPerformed)
        {
            assignedTask.PerformTask(npc);
            if (workLayerIndex != -1)
            {
                animator.Play(assignedTask.workType.ToString(), workLayerIndex);
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
            if(assignedTask.WorkTaskTransform() != assignedTask.transform)
            {
                agent.updatePosition = true;
                agent.updateRotation = true;
            }
        }
    }

    private void UpdateAnimations()
    {
        float maxSpeed = MaxSpeed();
        float currentSpeedNormalized = agent.velocity.magnitude / maxSpeed;
        animator.SetFloat("Speed", currentSpeedNormalized);
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
        Debug.Log($"[WorkState] Stopping work for {npc.name}");
        if (assignedTask != null && (assignedTask.HasQueuedTasks || assignedTask.IsOccupied))
        {
            return;
        }

        npc.ChangeTask(TaskType.WANDER);
    }
}