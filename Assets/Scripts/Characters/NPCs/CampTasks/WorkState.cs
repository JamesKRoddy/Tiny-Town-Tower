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

            // Try to find a valid position on the NavMesh
            NavMeshHit hit;
            float searchRadius = 5.0f; // Start with a reasonable search radius
            bool foundValidPosition = false;
            
            // Try multiple search radii if needed
            while (searchRadius <= 20.0f && !foundValidPosition)
            {
                if (NavMesh.SamplePosition(taskPosition, out hit, searchRadius, NavMesh.AllAreas))
                {
                    agent.stoppingDistance = stoppingDistance;
                    agent.SetDestination(hit.position);
                    agent.speed = MaxSpeed();
                    agent.angularSpeed = npc.rotationSpeed;
                    isTaskBeingPerformed = false;
                    hasReachedTask = false;
                    timeAtTaskLocation = 0f;
                    agent.isStopped = false;
                    
                    // Subscribe to the StopWork event
                    assignedTask.StopWork += StopWork;
                    foundValidPosition = true;
                }
                else
                {
                    searchRadius += 5.0f; // Increase search radius
                }
            }

            if (!foundValidPosition)
            {
                // If we still can't find a valid position, try to get as close as possible
                agent.stoppingDistance = stoppingDistance;
                agent.SetDestination(taskPosition);
                agent.speed = MaxSpeed();
                agent.angularSpeed = npc.rotationSpeed;
                isTaskBeingPerformed = false;
                hasReachedTask = false;
                timeAtTaskLocation = 0f;
                agent.isStopped = false;
                
                // Subscribe to the StopWork event
                assignedTask.StopWork += StopWork;
                
                Debug.LogWarning($"[WorkState] {gameObject.name} could not find valid NavMesh position, attempting to get as close as possible");
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

        float distanceToTask = Vector3.Distance(transform.position, assignedTask.WorkTaskTransform().position);

        // Check if we've reached the end of our path or are close enough to the task
        if (!agent.pathPending && (agent.remainingDistance <= stoppingDistance || distanceToTask <= minDistanceToTask))
        {
            if (!hasReachedTask)
            {
                hasReachedTask = true;
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                timeAtTaskLocation = 0f;
                
                if(assignedTask.WorkTaskTransform() != assignedTask.transform)
                {
                    // Disable NavMeshAgent control to allow manual positioning
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
            // Reset task state if we're moving away
            if (isTaskBeingPerformed)
            {
                if (workLayerIndex != -1)
                {
                    animator.Play("Empty", workLayerIndex);
                }
                isTaskBeingPerformed = false;
            }
            hasReachedTask = false;
            agent.isStopped = false;
            timeAtTaskLocation = 0f;
            
            // Re-enable NavMeshAgent control when moving
            agent.updatePosition = true;
            agent.updateRotation = true;

            // Rotate towards the task while moving
            Vector3 directionToTask = (assignedTask.WorkTaskTransform().position - transform.position).normalized;
            directionToTask.y = 0; // Keep rotation only on the Y axis
            if (directionToTask != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTask);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, npc.rotationSpeed * Time.deltaTime);
            }
        }
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
            Debug.Log($"[WorkState] {npc.name} has more tasks in queue or is still occupied, continuing work");
            return;
        }

        // Only change to wander if no more tasks and not occupied
        Debug.Log($"[WorkState] {npc.name} has no more tasks and is not occupied, returning to wander");
        npc.ChangeTask(TaskType.WANDER);
    }
}