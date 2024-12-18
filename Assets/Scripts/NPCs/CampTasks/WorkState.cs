using UnityEngine;
using UnityEngine.AI;

public enum WorkType
{
    NONE,
    FARMING, //TODO might have to break this down into several parts
    GATHER_WOOD,
    GATHER_ROCK,
    WEAVE_FABRIC,
    MAKE_AMMO,
    GENERATE_ELECTRICITY,
    BUILD_STRUCTURE,
}

public class WorkState : _TaskState
{
    private NavMeshAgent agent;
    private WorkTask assignedTask;
    private bool isTaskBeingPerformed;

    private void Start()
    {
        if (npc == null)
        {
            SetNPCReference(GetComponent<SettlerNPC>());
        }
        agent = npc.GetAgent();
    }

    public override void OnEnterState()
    {
        // Work state logic can be added here
        Debug.Log("Starting Work task");

        // If a task is assigned, move to the closest reachable position near the task
        if (assignedTask != null)
        {
            Transform taskTransform = assignedTask.WorkTaskTransform();
            if (taskTransform != null)
            {
                // Try to find the closest reachable point on the NavMesh to the task location
                NavMeshHit hit;
                if (NavMesh.SamplePosition(taskTransform.position, out hit, 1f, NavMesh.AllAreas))
                {
                    // Move NPC to the closest reachable position
                    agent.SetDestination(hit.position);
                }
                else
                {
                    // If no reachable position is found, stop the agent (or handle this case appropriately)
                    Debug.LogWarning("No reachable position found near the work task.");
                }
            }
        }

        assignedTask.StopWork += StopWork;
    }

    public override void OnExitState()
    {
        // Exit work state logic
        Debug.Log("Exiting Work task");
        npc.animator.SetInteger("WorkType", 0);
        assignedTask.StopWork -= StopWork;
        assignedTask = null; // Reset task        
    }

    public override void UpdateState()
    {
        if (assignedTask != null)
        {
            float distanceToTask = Vector3.Distance(agent.transform.position, assignedTask.WorkTaskTransform().position);

            // Only check if the NPC has reached the task location
            if (distanceToTask <= agent.stoppingDistance)
            {
                // Perform the task only once if the NPC has arrived at the location
                if (!isTaskBeingPerformed)
                {
                    assignedTask.PerformTask(npc);
                    npc.animator.SetInteger("WorkType", (int)assignedTask.workType);  // Set animator for the task
                    isTaskBeingPerformed = true;  // Prevent multiple task starts
                }
            }
            else
            {
                // Reset animator when the NPC leaves the task location
                if (isTaskBeingPerformed)
                {
                    npc.animator.SetInteger("WorkType", 0);  // Reset the WorkType
                    isTaskBeingPerformed = false;  // Reset task flag
                }
            }
        }
    }

    public override float MaxSpeed()
    {
        return npc.moveMaxSpeed; // Work might have normal speed
    }

    public override TaskType GetTaskType()
    {
        return TaskType.WORK; // TaskType for this state
    }

    // Assign work manually
    public void AssignWork(WorkTask task)
    {
        assignedTask = task; // Assign a task to the NPC
        npc.ChangeTask(TaskType.WORK);
    }

    public void StopWork()
    {
        npc.ChangeTask(TaskType.WANDER);
    }
}