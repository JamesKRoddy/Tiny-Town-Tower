using UnityEngine;
using UnityEngine.AI;

public enum WorkType
{
    FARMING,
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

        // If a task is assigned, perform it
        if (assignedTask != null)
        {
            assignedTask.PerformTask(npc);
        }
    }

    public override void OnExitState()
    {
        // Exit work state logic
        Debug.Log("Exiting Work task");
        assignedTask = null; // Reset task
    }

    public override void UpdateState()
    {
        if (assignedTask != null)
        {
            assignedTask.PerformTask(npc); // Continue performing the task
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

    //TODO call this to assign work manually
    public void AssignTask(WorkTask task)
    {
        assignedTask = task; // Assign a task to the NPC
        npc.ChangeTask(TaskType.WORK);
    }

    //TODO call this to assign work automatically
    public void AutoAssignTask()
    {
        // Example of auto-assigning a task based on priority
        WorkTask[] availableTasks = FindObjectsOfType<WorkTask>(); // Find all work tasks in the scene

        WorkTask highestPriorityTask = null;

        // Determine which task to pick based on priority
        foreach (var task in availableTasks)
        {
            if (highestPriorityTask == null || task.workType < highestPriorityTask.workType)
            {
                highestPriorityTask = task;
            }
        }

        // Assign the highest priority task to the NPC
        if (highestPriorityTask != null)
        {
            AssignTask(highestPriorityTask);
            Debug.Log($"Assigned task: {highestPriorityTask.workType}");
        }
    }
}
