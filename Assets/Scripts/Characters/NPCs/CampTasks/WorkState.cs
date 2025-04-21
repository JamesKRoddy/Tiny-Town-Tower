using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Managers;

public class WorkState : _TaskState
{
    private WorkTask assignedTask;
    private bool isTaskBeingPerformed = false;
    private float minDistanceToTask = 0.1f; // Minimum distance to consider "close enough"

    protected override void Awake()
    {
        base.Awake();
    }

    public override void OnEnterState()
    {
        if (assignedTask != null)
        {
            agent.SetDestination(assignedTask.WorkTaskTransform().position);
            agent.speed = MaxSpeed();
            agent.angularSpeed = npc.rotationSpeed;
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
    }

    public override void UpdateState()
    {
        if (assignedTask != null)
        {
            float distanceToTask = Vector3.Distance(agent.transform.position, assignedTask.WorkTaskTransform().position);

            // Check if we've reached the end of our path or are close enough to the task
            if (!agent.pathPending && (agent.remainingDistance <= minDistanceToTask || distanceToTask <= minDistanceToTask))
            {
                // Perform the task only once if the NPC has arrived at the location
                if (!isTaskBeingPerformed)
                {
                    assignedTask.PerformTask(npc);
                    animator.SetInteger("WorkType", (int)assignedTask.workType);
                    isTaskBeingPerformed = true;
                }
            }
            else
            {
                // Reset animator when the NPC leaves the task location
                if (isTaskBeingPerformed)
                {
                    animator.SetInteger("WorkType", 0);
                    isTaskBeingPerformed = false;
                }
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
        if (isActiveAndEnabled)
        {
            OnEnterState();
        }
    }

    public void StopWork()
    {
        npc.ChangeTask(TaskType.WANDER);
    }
}