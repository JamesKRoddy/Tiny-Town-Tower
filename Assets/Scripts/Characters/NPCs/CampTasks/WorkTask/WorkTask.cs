using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Managers;

public abstract class WorkTask : MonoBehaviour
{
    [Header("Task Settings")]
    [SerializeField] protected Transform workLocationTransform; // Optional specific work location
    protected HumanCharacterController currentWorker; // Reference to the NPC performing this task
    [HideInInspector] public ResourceItemCount[] requiredResources; // Resources needed to perform this task
    [SerializeField] protected bool showTooltip = false; // Whether to show tooltips for this task

    [Header("Electricity Requirements")]
    [SerializeField] protected float electricityRequired = 0f;
    protected bool isOperational = true; // Whether the task is operational, different from the buildings operational status

    [Header("Task Animation")]
    public TaskAnimation taskAnimation;

    // Work progress tracking
    protected float workProgress = 0f;
    protected float baseWorkTime = 5f;
    protected int resourceAmount = 1;
    protected Coroutine workCoroutine;

    // Task queue
    public Queue<object> taskQueue = new Queue<object>();
    protected object currentTaskData;

    // Property to access the assigned NPC
    public HumanCharacterController AssignedNPC => currentWorker;
    public bool IsOccupied => currentWorker != null;
    public bool HasQueuedTasks => taskQueue.Count > 0;
    public bool IsTaskCompleted => currentTaskData == null && !HasQueuedTasks;

    // Helper method for derived classes to set up tasks
    protected void SetupTask(object taskData)
    {
        if (taskData == null) return;
        
        // Queue the task
        QueueTask(taskData);

        // If no current task, set it up immediately
        if (currentTaskData == null)
        {
            currentTaskData = taskQueue.Dequeue();
            SetupNextTask();
        }
    }

    // Virtual method for tooltip text
    public virtual string GetTooltipText()
    {
        if (!showTooltip) return string.Empty;

        string tooltip = $"{GetType().Name}\n";
        tooltip += $"Time: {baseWorkTime} seconds\n";
        tooltip += $"Status: {(isOperational ? "Operational" : "Not Operational")}\n";
        
        if (electricityRequired > 0)
        {
            tooltip += $"Electricity Required: {electricityRequired}\n";
            tooltip += $"Current Power: {CampManager.Instance.ElectricityManager.GetElectricityPercentage():F1}%\n";
        }
        
        if (requiredResources != null)
        {
            tooltip += "Required Resources:\n";
            foreach (var resource in requiredResources)
            {
                tooltip += $"- {resource.resourceScriptableObj.objectName}: {resource.count}\n";
            }
        }
        
        return tooltip;
    }

    // Virtual method for NPC to perform the work task
    public virtual void PerformTask(HumanCharacterController npc)
    {
        Debug.Log($"[WorkTask] Performing task {GetType().Name} with NPC {npc.name}");
        if (currentWorker == npc)
        {
            Debug.Log($"[WorkTask] Starting work coroutine for {GetType().Name}");
            workCoroutine = StartCoroutine(WorkCoroutine());

            // Register electricity consumption when the task starts
            if (electricityRequired > 0)
            {
                CampManager.Instance.ElectricityManager.RegisterBuildingConsumption(this, electricityRequired);
            }
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
        if (!isOperational)
        {
            return false;
        }

        // Check if there is any electricity available
        if (electricityRequired > 0 && CampManager.Instance.ElectricityManager.GetCurrentElectricity() <= 0)
        {
            SetOperationalStatus(false);
            return false;
        }

        return true;
    }

    // Method to check if the player has required resources
    public bool HasRequiredResources()
    {
        if (requiredResources == null || requiredResources.Length == 0)
            return true; // No resources required

        foreach (var resource in requiredResources)
        {
            if (resource == null || resource.resourceScriptableObj == null)
            {
                Debug.LogWarning($"[WorkTask] Invalid resource in requiredResources array");
                return false;
            }

            if (PlayerInventory.Instance.GetItemCount(resource.resourceScriptableObj) < resource.count)
            {
                return false;
            }
        }
        return true;
    }

    // Method to consume required resources
    protected void ConsumeResources()
    {
        if (requiredResources == null || requiredResources.Length == 0)
            return; // No resources to consume

        foreach (var resourceItem in requiredResources)
        {
            if (resourceItem == null || resourceItem.resourceScriptableObj == null)
            {
                Debug.LogWarning($"[WorkTask] Invalid resource in requiredResources array");
                continue;
            }

            PlayerInventory.Instance.RemoveItem(resourceItem.resourceScriptableObj, resourceItem.count);
        }
    }

    // Declare StopWork as an event
    public event Action StopWork; // Called when a construction is complete, building is broken, etc..

    public event Action OnTaskCompleted;

    protected virtual void Start()
    {

    }

    protected virtual void OnDestroy()
    {
        // Unregister electricity consumption when the task is destroyed
        if (electricityRequired > 0)
        {
            CampManager.Instance.ElectricityManager.UnregisterBuildingConsumption(this);
        }
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
    public void AssignNPC(HumanCharacterController npc)
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
        // Reset work progress at the start of each task
        workProgress = 0f;
        
        float workSpeed = 1f;

        while (workProgress < baseWorkTime)
        {
            workProgress += Time.deltaTime * workSpeed;
            yield return null;
        }

        // Ensure we don't exceed the base work time
        workProgress = baseWorkTime;
        CompleteWork();
    }

    public void StopWorkCoroutine()
    {
        if (workCoroutine != null)
        {
            StopCoroutine(workCoroutine);
            workCoroutine = null;
        }
    }

    // Virtual method for completing work that can be overridden
    protected virtual void CompleteWork()
    {
        currentTaskData = null;

        // Store the current worker as previous worker before clearing
        if (currentWorker != null)
        {
            CampManager.Instance.WorkManager.StorePreviousWorker(this, currentWorker);
        }
        
        // Reset state
        workProgress = 0f;
        
        StopWorkCoroutine();
        
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
        }
        else
        {
            UnassignNPC();

            // Unregister electricity consumption when work is complete
            if (electricityRequired > 0)
            {
                CampManager.Instance.ElectricityManager.UnregisterBuildingConsumption(this);
            }
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
        Debug.Log($"[WorkTask] Queueing task {taskData} for {GetType().Name} [Queue Count: {taskQueue.Count}]");
        
        // If we have a previous worker and no current worker, assign them to the new task
        if (currentWorker == null && taskQueue.Count == 1)
        {
            // Find the previous worker through the WorkManager
            var previousWorker = CampManager.Instance.WorkManager.GetPreviousWorkerForTask(this);
            if (previousWorker != null)
            {
                // Set the NPC for assignment before assigning the task
                CampManager.Instance.WorkManager.SetNPCForAssignment(previousWorker);
                AssignNPC(previousWorker);
                if (previousWorker is SettlerNPC settler)
                {
                    settler.ChangeTask(TaskType.WORK);
                }
            }
        }
    }
    public string GetAnimationClipName()
    {
        return taskAnimation.ToString();
    }

    // Method to clear the task queue
    public virtual void ClearTaskQueue()
    {
        taskQueue.Clear();
    }

    protected void AddResourceToInventory(ResourceItemCount resourceItemCount)
    {
        Debug.Log($"[WorkTask] Adding resource to inventory");
        PlayerInventory.Instance.AddItem(resourceItemCount.resourceScriptableObj, resourceItemCount.count);
    }

    protected virtual void OnDisable()
    {
        StopWorkCoroutine();
    }

    public void SetOperationalStatus(bool operational)
    {
        if (isOperational != operational)
        {
            isOperational = operational;
            
            if (!isOperational)
            {
                // Stop current work if any
                StopWorkCoroutine();
                
                // Unassign current worker if any
                if (currentWorker != null)
                {
                    if (currentWorker is SettlerNPC settler)
                    {
                        settler.ChangeTask(TaskType.WANDER);
                    }
                    else if (currentWorker is RobotCharacterController robot)
                    {
                        robot.StopWork();
                    }
                    UnassignNPC();
                }
            }
            else
            {
                // If we become operational again and have a previous worker, try to reassign them
                var previousWorker = CampManager.Instance.WorkManager.GetPreviousWorkerForTask(this);
                if (previousWorker != null)
                {
                    CampManager.Instance.WorkManager.SetNPCForAssignment(previousWorker);
                    AssignNPC(previousWorker);
                    if (previousWorker is SettlerNPC settler)
                    {
                        settler.ChangeTask(TaskType.WORK);
                    }
                }
            }
        }
    }

    public bool IsOperational()
    {
        return isOperational;
    }

    public float GetElectricityRequired()
    {
        return electricityRequired;
    }
}
