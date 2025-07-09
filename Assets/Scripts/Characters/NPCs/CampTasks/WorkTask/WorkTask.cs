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

    // Property to access the assigned NPC
    public HumanCharacterController AssignedNPC => currentWorker;
    public bool IsOccupied => currentWorker != null;
    public virtual bool IsTaskCompleted => true; // Base WorkTask is always completed when done
    public virtual bool HasQueuedTasks => false; // Base WorkTask has no queue

    private IPlaceableStructure taskStructure;

    protected virtual void Start()
    {
        taskStructure = GetComponent<IPlaceableStructure>();
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
        if (currentWorker == npc)
        {
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
        return workLocationTransform;
    }

    // Method for NavMeshAgent pathfinding - should always return a valid position
    public virtual Transform GetNavMeshDestination()
    {
        // If we have a specific work location, use that
        if (workLocationTransform != null)
        {
            return workLocationTransform;
        }
        // Otherwise use the task's position
        return transform;
    }

    // Method for precise positioning - can return null if no precise position needed
    public virtual Transform GetPrecisePosition()
    {
        return workLocationTransform;
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
        if(taskStructure != null)
        {
            taskStructure.SetCurrentWorkTask(this);
        }
    }

    // Method to unassign the current NPC
    public void UnassignNPC()
    {
        currentWorker = null;
        if(taskStructure != null)
        {
            taskStructure.SetCurrentWorkTask(null);
        }
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
            // Apply hunger-based work speed multiplier
            if (currentWorker != null)
            {
                workSpeed = (currentWorker as SettlerNPC).GetWorkSpeedMultiplier();
                
                // If starving, stop working
                if (workSpeed <= 0)
                {
                    if (currentWorker is SettlerNPC settler)
                    {
                        settler.TakeBreak(); // Take a break instead of stopping work completely
                    }
                    StopWorkCoroutine();
                    yield break;
                }
            }

            workProgress += Time.deltaTime * workSpeed;
            yield return null;
        }

        // Ensure we don't exceed the base work time
        workProgress = baseWorkTime;
        CompleteWork();
    }

    public virtual void StopWorkCoroutine()
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
        // Store the current worker as previous worker before clearing
        if (currentWorker != null)
        {
            CampManager.Instance.WorkManager.StorePreviousWorker(this, currentWorker);
        }
        
        // Reset state
        workProgress = 0f;
        
        StopWorkCoroutine();
        
        UnassignNPC();

        // Unregister electricity consumption when work is complete
        if (electricityRequired > 0)
        {
            CampManager.Instance.ElectricityManager.UnregisterBuildingConsumption(this);
        }
        
        // Notify completion
        OnTaskCompleted?.Invoke();
        InvokeStopWork();
    }

    // New method to notify task completion without stopping work
    protected void NotifyTaskCompletion()
    {
        OnTaskCompleted?.Invoke();
    }

    public string GetAnimationClipName()
    {
        return taskAnimation.ToString();
    }

    protected void AddResourceToInventory(ResourceItemCount resourceItemCount)
    {
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
                        settler.ClearAssignedWork(); // Clear the assigned work
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
                        settler.StartWork(this);
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

    public float GetProgress()
    {
        return workProgress / baseWorkTime;
    }
}
