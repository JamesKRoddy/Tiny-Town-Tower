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
        public float minDistanceToTask;
        public float approachSpeedMultiplier;
        public float decelerationDistance;
        public float finalApproachThreshold;
        public float taskStartDelay;

        public MovementSettings(WorkType workType)
        {
            if (workType == WorkType.BUILD_STRUCTURE)
            {
                minDistanceToTask = 0.3f;
                approachSpeedMultiplier = 0.3f;
            }
            else
            {
                minDistanceToTask = 0.5f;
                approachSpeedMultiplier = 0.5f;
            }
            decelerationDistance = 2.0f;
            finalApproachThreshold = 1.0f;
            taskStartDelay = 0.5f;
        }
    }
    private MovementSettings movementSettings;
    #endregion

    #region Pathfinding
    private class PathfindingState
    {
        public Vector3 validWorkPosition;
        public Vector3 lastPosition;
        public float lastPositionCheckTime;
        public float stuckTime;
        public float lastPathRecalculationTime;
        public bool isRecalculatingPath;
        public bool isInFinalApproach;

        public float positionCheckInterval = 0.5f;
        public float stuckThreshold = 3f;
        public float pathRecalculationInterval = 1.0f;
        public float navMeshSampleRadius = 5.0f;

        public PathfindingState()
        {
            lastPositionCheckTime = Time.time;
            lastPathRecalculationTime = Time.time;
            stuckTime = 0f;
            isRecalculatingPath = false;
            isInFinalApproach = false;
        }
    }
    private PathfindingState pathfinding;
    #endregion

    protected override void Awake()
    {
        base.Awake();
        pathfinding = new PathfindingState();
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

        movementSettings = new MovementSettings(assignedTask.workType);
        pathfinding.lastPosition = transform.position;
        pathfinding.validWorkPosition = assignedTask.WorkTaskTransform().position;
        
        assignedTask.StopWork += StopWork;
    }

    private void SetupNavMeshPath()
    {
        Vector3 taskPosition = assignedTask.WorkTaskTransform().position;
        bool foundValidPosition = FindValidNavMeshPosition(taskPosition);

        if (foundValidPosition)
        {
            ConfigureAgentForMovement();
        }
        else
        {
            Debug.LogWarning($"[WorkState] {gameObject.name} could not find valid NavMesh position for work task, falling back to direct position");
            ConfigureAgentForDirectMovement(taskPosition);
        }
    }

    private bool FindValidNavMeshPosition(Vector3 taskPosition)
    {
        NavMeshHit hit;
        
        // Try direct position first
        if (NavMesh.SamplePosition(taskPosition, out hit, pathfinding.navMeshSampleRadius, NavMesh.AllAreas))
        {
            pathfinding.validWorkPosition = hit.position;
            return true;
        }

        // Try surrounding positions
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * pathfinding.navMeshSampleRadius;
            Vector3 testPosition = taskPosition + offset;

            if (NavMesh.SamplePosition(testPosition, out hit, pathfinding.navMeshSampleRadius, NavMesh.AllAreas))
            {
                NavMeshPath path = new NavMeshPath();
                if (NavMesh.CalculatePath(hit.position, taskPosition, NavMesh.AllAreas, path))
                {
                    pathfinding.validWorkPosition = hit.position;
                    return true;
                }
            }
        }

        return false;
    }

    private void ConfigureAgentForMovement()
    {
        agent.stoppingDistance = movementSettings.minDistanceToTask;
        agent.SetDestination(pathfinding.validWorkPosition);
        agent.speed = MaxSpeed();
        agent.angularSpeed = npc.rotationSpeed;
        agent.isStopped = false;
    }

    private void ConfigureAgentForDirectMovement(Vector3 position)
    {
        agent.stoppingDistance = movementSettings.minDistanceToTask;
        agent.SetDestination(position);
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
        pathfinding.isInFinalApproach = false;
    }

    public override void UpdateState()
    {
        if (assignedTask == null) return;

        float distanceToTask = Vector3.Distance(transform.position, pathfinding.validWorkPosition);
        float currentTime = Time.time;

        UpdateFinalApproach(distanceToTask);
        UpdatePathfinding(currentTime);
        UpdateMovementSpeed(distanceToTask);
        UpdateTaskExecution(distanceToTask, currentTime);
        UpdateAnimations();
    }

    private void UpdateFinalApproach(float distanceToTask)
    {
        if (!pathfinding.isInFinalApproach && distanceToTask <= movementSettings.finalApproachThreshold)
        {
            pathfinding.isInFinalApproach = true;
            agent.stoppingDistance = movementSettings.minDistanceToTask;
            agent.speed = MaxSpeed() * movementSettings.approachSpeedMultiplier;
        }
    }

    private void UpdatePathfinding(float currentTime)
    {
        // Check for stuck condition
        if (currentTime - pathfinding.lastPositionCheckTime >= pathfinding.positionCheckInterval)
        {
            Vector3 currentPosition = transform.position;
            float distanceMoved = Vector3.Distance(currentPosition, pathfinding.lastPosition);
            
            if (distanceMoved < 0.01f)
            {
                pathfinding.stuckTime += pathfinding.positionCheckInterval;
                if (pathfinding.stuckTime >= pathfinding.stuckThreshold && !pathfinding.isRecalculatingPath)
                {
                    RecalculatePath();
                }
            }
            else
            {
                pathfinding.stuckTime = 0f;
            }
            
            pathfinding.lastPosition = currentPosition;
            pathfinding.lastPositionCheckTime = currentTime;
        }

        // Periodic path recalculation
        if (!pathfinding.isInFinalApproach && 
            currentTime - pathfinding.lastPathRecalculationTime >= pathfinding.pathRecalculationInterval && 
            !pathfinding.isRecalculatingPath)
        {
            RecalculatePath();
        }
    }

    private void UpdateMovementSpeed(float distanceToTask)
    {
        if (distanceToTask <= movementSettings.decelerationDistance)
        {
            float speedMultiplier = Mathf.Lerp(movementSettings.approachSpeedMultiplier, 1f, 
                distanceToTask / movementSettings.decelerationDistance);
            agent.speed = MaxSpeed() * speedMultiplier;
        }
        else if (!pathfinding.isInFinalApproach)
        {
            agent.speed = MaxSpeed();
        }
    }

    private void UpdateTaskExecution(float distanceToTask, float currentTime)
    {
        if (!agent.pathPending && (agent.remainingDistance <= movementSettings.minDistanceToTask || 
            distanceToTask <= movementSettings.minDistanceToTask))
        {
            HandleReachedTask();
        }
        else
        {
            HandleMovingToTask();
        }
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

    private void RecalculatePath()
    {
        pathfinding.isRecalculatingPath = true;
        pathfinding.lastPathRecalculationTime = Time.time;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, pathfinding.navMeshSampleRadius, NavMesh.AllAreas))
        {
            NavMeshPath path = new NavMeshPath();
            if (NavMesh.CalculatePath(hit.position, assignedTask.WorkTaskTransform().position, NavMesh.AllAreas, path))
            {
                pathfinding.validWorkPosition = hit.position;
                agent.SetDestination(pathfinding.validWorkPosition);
            }
        }

        pathfinding.isRecalculatingPath = false;
    }

    private void UpdateAnimations()
    {
        // Use the base class's animation update logic
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