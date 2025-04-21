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
        Debug.Log($"[WorkState] {gameObject.name} initialized");
    }

    public override void OnEnterState()
    {
        Debug.Log($"[WorkState] {gameObject.name} OnEnterState called");
        
        if (assignedTask != null)
        {
            Vector3 taskPosition = assignedTask.WorkTaskTransform().position;
            Debug.Log($"[WorkState] {gameObject.name} entering work state for task at {taskPosition}");
            
            // Check if the task position is on the NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(taskPosition, out hit, 1.0f, NavMesh.AllAreas))
            {
                Debug.Log($"[WorkState] {gameObject.name} task position is valid on NavMesh");
                agent.SetDestination(hit.position);
                agent.speed = MaxSpeed();
                agent.angularSpeed = npc.rotationSpeed;
                isTaskBeingPerformed = false;
                hasReachedTask = false;
                timeAtTaskLocation = 0f;
                agent.isStopped = false;
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
        Debug.Log($"[WorkState] {gameObject.name} OnExitState called");
        if (isTaskBeingPerformed)
        {
            animator.SetInteger("WorkType", 0);
            isTaskBeingPerformed = false;
        }
        agent.speed = npc.moveMaxSpeed;
        agent.angularSpeed = npc.rotationSpeed;
        agent.isStopped = false;
    }

    public override void UpdateState()
    {
        if (assignedTask == null) return;

        float distanceToTask = Vector3.Distance(transform.position, assignedTask.WorkTaskTransform().position);
        Debug.Log($"[WorkState] {gameObject.name} UpdateState - distance: {distanceToTask}, remaining: {agent.remainingDistance}, pending: {agent.pathPending}");

        // Check if we've reached the end of our path or are close enough to the task
        if (!agent.pathPending && (agent.remainingDistance <= minDistanceToTask || distanceToTask <= minDistanceToTask))
        {
            if (!hasReachedTask)
            {
                Debug.Log($"[WorkState] {gameObject.name} reached task location at distance {distanceToTask}");
                hasReachedTask = true;
                agent.isStopped = true;
                timeAtTaskLocation = 0f;
            }

            // Wait a small delay before starting the task to ensure NPC has stopped
            timeAtTaskLocation += Time.deltaTime;
            
            if (timeAtTaskLocation >= taskStartDelay && !isTaskBeingPerformed)
            {
                Debug.Log($"[WorkState] {gameObject.name} starting task execution after {timeAtTaskLocation} seconds");
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
                Debug.Log($"[WorkState] {gameObject.name} moving away from task at distance {distanceToTask}");
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
        Debug.Log($"[WorkState] {gameObject.name} assigned new task: {task.GetType().Name}");
        assignedTask = task;
    }

    public void StopWork()
    {
        Debug.Log($"[WorkState] {gameObject.name} stopping work and returning to wander state");
        npc.ChangeTask(TaskType.WANDER);
    }
}