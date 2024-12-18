using System.Collections.Generic;
using UnityEngine;

public class WorkManager : MonoBehaviour
{
    private static WorkManager _instance;

    public static WorkManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<WorkManager>();
                if (_instance == null)
                {
                    Debug.LogWarning("WorkManager instance not found in the scene!");
                }
            }
            return _instance;
        }
    }

    private Queue<WorkTask> workQueue = new Queue<WorkTask>(); // Queue to hold available tasks
    public delegate void TaskAssigned(WorkTask task); // Event to notify NPCs when a task is assigned
    public event TaskAssigned OnTaskAssigned;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    // Method to add a new work task to the queue
    public void AddWorkTask(WorkTask newTask)
    {
        workQueue.Enqueue(newTask);
        AssignTaskToWanderingNPCs();
    }

    // Method to assign tasks to wandering NPCs
    private void AssignTaskToWanderingNPCs()
    {
        if (workQueue.Count > 0)
        {
            WorkTask taskToAssign = workQueue.Dequeue(); // Get the next task from the queue

            // Raise the event to notify NPCs that a task is available
            OnTaskAssigned?.Invoke(taskToAssign);
        }
    }
}
