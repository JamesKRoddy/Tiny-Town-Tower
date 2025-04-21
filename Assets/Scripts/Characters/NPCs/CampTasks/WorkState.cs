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

    protected override void Awake()
    {
        base.Awake();
    }

    public override void OnEnterState()
    {        
        if (assignedTask != null)
        {
            Vector3 taskPosition = assignedTask.WorkTaskTransform().position;

            // Check if the task position is on the NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(taskPosition, out hit, 1.0f, NavMesh.AllAreas))
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
            }
            else
            {
                Debug.LogError($"[WorkState] {gameObject.name} task position is not valid on NavMesh! Position: {taskPosition}");
                StopWork();
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
            animator.SetInteger("WorkType", 0);
            isTaskBeingPerformed = false;
        }
        agent.speed = npc.moveMaxSpeed;
        agent.angularSpeed = npc.rotationSpeed;
        agent.isStopped = false;
        agent.stoppingDistance = 0f;
        
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
            }

            // Wait a small delay before starting the task to ensure NPC has stopped
            timeAtTaskLocation += Time.deltaTime;
            
            if (timeAtTaskLocation >= taskStartDelay && !isTaskBeingPerformed)
            {
                // Perform the task
                assignedTask.PerformTask(npc);
                animator.SetInteger("WorkType", (int)assignedTask.workType);
                isTaskBeingPerformed = true;
            }
        }
        else
        {
            // Reset task state if we're moving away
            if (isTaskBeingPerformed)
            {
                animator.SetInteger("WorkType", 0);
                isTaskBeingPerformed = false;
            }
            hasReachedTask = false;
            agent.isStopped = false;
            timeAtTaskLocation = 0f;
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
        npc.ChangeTask(TaskType.WANDER);
    }
}