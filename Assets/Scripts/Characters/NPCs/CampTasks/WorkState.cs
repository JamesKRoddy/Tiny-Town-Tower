using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Managers;

public class WorkState : _TaskState
{
    private WorkTask assignedTask;
    private bool isTaskBeingPerformed = false;
    private float minDistanceToTask = 0.5f;
    private float taskStartDelay = 0.5f;
    private float timeAtTaskLocation = 0f;
    private bool hasReachedTask = false;
    private int workLayerIndex = -1;
    private float approachSpeedMultiplier = 0.5f;
    private float decelerationDistance = 2.0f;
    private Vector3 validWorkPosition; // Store the valid NavMesh position for work
    private float navMeshSampleRadius = 5.0f; // Radius to search for valid NavMesh points
    private Vector3 lastPosition;
    private float positionCheckInterval = 0.5f;
    private float lastPositionCheckTime;
    private float stuckTime = 0f;
    private float stuckThreshold = 3f;
    private float pathRecalculationInterval = 1.0f;
    private float lastPathRecalculationTime;
    private bool isRecalculatingPath = false;
    private float finalApproachThreshold = 1.0f; // Distance at which to switch to final approach
    private bool isInFinalApproach = false;
    private float constructionApproachMultiplier = 0.3f; // Slower approach for construction tasks
    private float standardApproachMultiplier = 0.5f; // Standard approach speed for other tasks

    protected override void Awake()
    {
        base.Awake();        
    }

    public override void OnEnterState()
    {        
        if (assignedTask != null)
        {
            workLayerIndex = animator.GetLayerIndex("Work Layer");
            if (workLayerIndex == -1)
            {
                Debug.LogError($"[WorkState] Could not find 'Work Layer' in animator for {gameObject.name}");
            }

            Vector3 taskPosition = assignedTask.WorkTaskTransform().position;
            validWorkPosition = taskPosition;
            lastPosition = transform.position;
            lastPositionCheckTime = Time.time;
            stuckTime = 0f;

            // Adjust approach speed based on work type
            if (assignedTask.workType == WorkType.BUILD_STRUCTURE)
            {
                approachSpeedMultiplier = constructionApproachMultiplier;
                minDistanceToTask = 0.3f; // Closer approach for construction
            }
            else
            {
                approachSpeedMultiplier = standardApproachMultiplier;
                minDistanceToTask = 0.5f;
            }

            // Try to find a valid position on the NavMesh around the work position
            NavMeshHit hit;
            float searchRadius = navMeshSampleRadius;
            bool foundValidPosition = false;
            
            // First try to find a point directly on the NavMesh
            if (NavMesh.SamplePosition(taskPosition, out hit, searchRadius, NavMesh.AllAreas))
            {
                validWorkPosition = hit.position;
                foundValidPosition = true;
            }
            else
            {
                // If no direct point found, try to find a point around the work position
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * 45f * Mathf.Deg2Rad;
                    Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * searchRadius;
                    Vector3 testPosition = taskPosition + offset;

                    if (NavMesh.SamplePosition(testPosition, out hit, searchRadius, NavMesh.AllAreas))
                    {
                        NavMeshPath path = new NavMeshPath();
                        if (NavMesh.CalculatePath(hit.position, taskPosition, NavMesh.AllAreas, path))
                        {
                            validWorkPosition = hit.position;
                            foundValidPosition = true;
                            break;
                        }
                    }
                }
            }

            if (foundValidPosition)
            {
                agent.stoppingDistance = minDistanceToTask;
                agent.SetDestination(validWorkPosition);
                agent.speed = MaxSpeed();
                agent.angularSpeed = npc.rotationSpeed;
                isTaskBeingPerformed = false;
                hasReachedTask = false;
                timeAtTaskLocation = 0f;
                agent.isStopped = false;
                
                assignedTask.StopWork += StopWork;
            }
            else
            {
                Debug.LogWarning($"[WorkState] {gameObject.name} could not find valid NavMesh position for work task, falling back to direct position");
                agent.stoppingDistance = minDistanceToTask;
                agent.SetDestination(taskPosition);
                agent.speed = MaxSpeed();
                agent.angularSpeed = npc.rotationSpeed;
                isTaskBeingPerformed = false;
                hasReachedTask = false;
                timeAtTaskLocation = 0f;
                agent.isStopped = false;
                
                assignedTask.StopWork += StopWork;
            }
        }
        else
        {
            Debug.LogWarning($"[WorkState] {gameObject.name} entered work state with no assigned task");
        }
    }

    public override void OnExitState()
    {
        if (isTaskBeingPerformed)
        {
            animator.Play("Empty", workLayerIndex);
            isTaskBeingPerformed = false;
        }
        agent.speed = npc.moveMaxSpeed;
        agent.angularSpeed = npc.rotationSpeed;
        agent.isStopped = false;
        agent.stoppingDistance = 0f;
        isInFinalApproach = false;
        
        // Make sure NavMeshAgent updates are re-enabled when leaving the state
        agent.updatePosition = true;
        agent.updateRotation = true;
        
        // Unsubscribe from the StopWork event
        if (assignedTask != null)
        {
            assignedTask.StopWork -= StopWork;
        }
    }

    public override void UpdateState()
    {
        if (assignedTask == null) return;

        float distanceToTask = Vector3.Distance(transform.position, validWorkPosition);
        float currentTime = Time.time;

        // Check if we should switch to final approach
        if (!isInFinalApproach && distanceToTask <= finalApproachThreshold)
        {
            isInFinalApproach = true;
            agent.stoppingDistance = minDistanceToTask;
            agent.speed = MaxSpeed() * approachSpeedMultiplier;
        }

        // Check for stuck condition and path recalculation
        if (currentTime - lastPositionCheckTime >= positionCheckInterval)
        {
            Vector3 currentPosition = transform.position;
            float distanceMoved = Vector3.Distance(currentPosition, lastPosition);
            
            if (distanceMoved < 0.01f)
            {
                stuckTime += positionCheckInterval;

                if (stuckTime >= stuckThreshold && !isRecalculatingPath)
                {
                    RecalculatePath();
                }
            }
            else
            {
                stuckTime = 0f;
            }
            
            lastPosition = currentPosition;
            lastPositionCheckTime = currentTime;
        }

        // Periodically recalculate path to ensure we're on the best route
        if (!isInFinalApproach && currentTime - lastPathRecalculationTime >= pathRecalculationInterval && !isRecalculatingPath)
        {
            RecalculatePath();
        }

        // Adjust speed based on distance to task
        if (distanceToTask <= decelerationDistance)
        {
            float speedMultiplier = Mathf.Lerp(approachSpeedMultiplier, 1f, distanceToTask / decelerationDistance);
            agent.speed = MaxSpeed() * speedMultiplier;
        }
        else if (!isInFinalApproach)
        {
            agent.speed = MaxSpeed();
        }

        // Check if we've reached the end of our path or are close enough to the task
        if (!agent.pathPending && (agent.remainingDistance <= minDistanceToTask || distanceToTask <= minDistanceToTask))
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

            // If we're at a specific work location, smoothly interpolate to the target position and rotation
            if(assignedTask.WorkTaskTransform() != assignedTask.transform)
            {
                // Smoothly interpolate position
                transform.position = Vector3.Lerp(transform.position, assignedTask.WorkTaskTransform().position, Time.deltaTime * 5f);
                // Smoothly interpolate rotation
                transform.rotation = Quaternion.Slerp(transform.rotation, assignedTask.WorkTaskTransform().rotation, Time.deltaTime * 5f);
            }
            else
            {
                // Only rotate towards the task if we're not at a specific work location
                Vector3 directionToTask = (assignedTask.WorkTaskTransform().position - transform.position).normalized;
                directionToTask.y = 0; // Keep rotation only on the Y axis
                if (directionToTask != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTask);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, npc.rotationSpeed * Time.deltaTime);
                }
            }

            // Wait a small delay before starting the task to ensure NPC has stopped
            timeAtTaskLocation += Time.deltaTime;
            
            if (timeAtTaskLocation >= taskStartDelay && !isTaskBeingPerformed)
            {
                // Perform the task
                assignedTask.PerformTask(npc);
                if (workLayerIndex != -1)
                {
                    animator.Play(assignedTask.workType.ToString(), workLayerIndex);
                }
                isTaskBeingPerformed = true;
            }
        }
        else
        {
            // If we're not at the destination, ensure we're moving
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
    }

    private void RecalculatePath()
    {
        isRecalculatingPath = true;
        lastPathRecalculationTime = Time.time;

        // Find a new valid position
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, navMeshSampleRadius, NavMesh.AllAreas))
        {
            // Calculate path to the work position
            NavMeshPath path = new NavMeshPath();
            if (NavMesh.CalculatePath(hit.position, assignedTask.WorkTaskTransform().position, NavMesh.AllAreas, path))
            {
                validWorkPosition = hit.position;
                agent.SetDestination(validWorkPosition);
            }
        }

        isRecalculatingPath = false;
    }

    public override float MaxSpeed()
    {
        return npc.moveMaxSpeed;
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
        // Check if there are more tasks in the queue or if the current task is still active
        if (assignedTask != null && (assignedTask.HasQueuedTasks || assignedTask.IsOccupied))
        {
            // Stay in work state and continue with next task
            return;
        }

        // Only change to wander if no more tasks and not occupied
        npc.ChangeTask(TaskType.WANDER);
    }
}