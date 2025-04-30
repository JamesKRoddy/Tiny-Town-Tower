using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Managers;

public abstract class WorkTask : MonoBehaviour
{
    [HideInInspector] public WorkType workType;
    [SerializeField] protected Transform workLocationTransform; // Optional specific work location
    protected SettlerNPC currentWorker; // Reference to the NPC performing this task
    [SerializeField] public ResourceItemCount[] requiredResources; // Resources needed to perform this task

    // Work progress tracking
    protected float workProgress = 0f;
    protected float baseWorkTime = 5f;
    protected int resourceAmount = 1;
    protected Coroutine workCoroutine;

    // Task queue
    public Queue<object> taskQueue = new Queue<object>();
    protected object currentTaskData;

    // Property to access the assigned NPC
    public SettlerNPC AssignedNPC => currentWorker;
    public bool IsOccupied => currentWorker != null;
    public bool HasQueuedTasks => taskQueue.Count > 0;

    // Abstract method for NPC to perform the work task
    public virtual void PerformTask(SettlerNPC npc)
    {
        if (currentWorker == npc)
        {
            workCoroutine = StartCoroutine(WorkCoroutine());
        }
    }
    
    // Virtual method that can be overridden by specific tasks
    public virtual Transform WorkTaskTransform()
    {
        return workLocationTransform != null ? workLocationTransform : transform;
    }

    // Virtual method to check if the task can be performed
    public virtual bool CanPerformTask()
    {
        return true; // Default implementation assumes task can always be performed
    }

    // Method to check if the player has required resources
    public bool HasRequiredResources()
    {
        if (requiredResources == null || requiredResources.Length == 0)
            return true; // No resources required

        foreach (var resource in requiredResources)
        {
            if (PlayerInventory.Instance.GetItemCount(resource.resource) < resource.count)
            {
                return false;
            }
        }
        return true;
    }

    // Method to consume required resources
    protected void ConsumeResources()
    {
        foreach (var resourceItem in requiredResources)
        {
            PlayerInventory.Instance.RemoveItem(resourceItem.resource, resourceItem.count);
        }
    }

    // Declare StopWork as an event
    public event Action StopWork; // Called when a construction is complete, building is broken, etc..

    public event Action OnTaskCompleted;

    protected virtual void Start()
    {
        
    }

    protected void AddWorkTask()
    {
        CampManager.Instance.WorkManager.AddWorkTask(this);
    }

    // Helper method to trigger the event safely (other classes can call this to invoke StopWork)
    protected void InvokeStopWork()
    {
        StopWork?.Invoke();
    }

    // Method to assign an NPC to this task
    public void AssignNPC(SettlerNPC npc)
    {
        currentWorker = npc;
    }

    // Method to unassign the current NPC
    public void UnassignNPC()
    {
        currentWorker = null;
    }

    // Method to check if the task is currently assigned
    public bool IsAssigned()
    {
        return currentWorker != null;
    }

    // Virtual work coroutine that can be overridden by specific tasks
    protected virtual IEnumerator WorkCoroutine()
    {
        while (workProgress < baseWorkTime)
        {
            workProgress += Time.deltaTime;
            yield return null;
        }

        CompleteWork();
    }

    // Virtual method for completing work that can be overridden
    protected virtual void CompleteWork()
    {
        
        // Reset state
        workProgress = 0f;
        if (workCoroutine != null)
        {
            StopCoroutine(workCoroutine);
            workCoroutine = null;
        }
        
        // Process next task in queue if available
        if (taskQueue.Count > 0)
        {
            currentTaskData = taskQueue.Dequeue();
            SetupNextTask();
            
            // Start the next task immediately if we have a worker
            if (currentWorker != null)
            {
                workCoroutine = StartCoroutine(WorkCoroutine());
            }
            else
            {
                Debug.LogWarning("[WorkTask] No worker assigned for next task");
            }
        }
        else
        {
            // Only clear the worker if there are no more tasks
            currentWorker = null;
        }
        
        // Notify completion
        OnTaskCompleted?.Invoke();
        InvokeStopWork();
    }

    // Virtual method to setup the next task in queue
    protected virtual void SetupNextTask()
    {
        // To be implemented by derived classes
    }

    // Method to add a task to the queue
    public virtual void QueueTask(object taskData)
    {
        taskQueue.Enqueue(taskData);
    }

    // Method to clear the task queue
    public virtual void ClearTaskQueue()
    {
        taskQueue.Clear();
    }

    protected void AddResourceToInventory(ResourceItemCount resourceItemCount)
    {
        PlayerInventory.Instance.AddItem(resourceItemCount.resource, resourceItemCount.count);
    }

    protected virtual void OnDisable()
    {
        if (workCoroutine != null)
        {
            StopCoroutine(workCoroutine);
            workCoroutine = null;
        }
    }
}
