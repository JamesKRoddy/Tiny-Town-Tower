using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Managers;

public abstract class WorkTask : MonoBehaviour
{
    [Header("Task Settings")]
    [SerializeField] protected Transform workLocationTransform; // Optional specific work location
    [SerializeField] protected int maxWorkers = 1; // Maximum number of workers that can be assigned to this task
    protected List<HumanCharacterController> currentWorkers = new List<HumanCharacterController>(); // List of NPCs performing this task
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

    // Properties to access the assigned NPCs
    public List<HumanCharacterController> AssignedNPCs => currentWorkers;
    public HumanCharacterController AssignedNPC => currentWorkers.Count > 0 ? currentWorkers[0] : null; // For backward compatibility
    public bool IsOccupied => currentWorkers.Count > 0;
    public bool IsFullyOccupied => currentWorkers.Count >= maxWorkers;
    public int CurrentWorkerCount => currentWorkers.Count;
    public int MaxWorkerCount => maxWorkers;
    public bool IsMultiWorkerTask => maxWorkers > 1;
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
        tooltip += $"Workers: {currentWorkers.Count}/{maxWorkers}\n";
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

    // Virtual method called by WorkState when NPC reaches work position
    // This is now primarily for animation/positioning setup since work execution is handled by worker coroutines
    public virtual void PerformTask(HumanCharacterController npc)
    {
        // Ensure worker is in the list (in case this is called before AssignNPC)
        if (!currentWorkers.Contains(npc) && currentWorkers.Count < maxWorkers)
        {
            currentWorkers.Add(npc);
        }
        
        // Work execution is now handled by the worker's coroutine started in AssignNPC
        // This method is mainly for WorkState coordination and animation
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

        // Check if there is enough electricity for the entire task duration
        if (electricityRequired > 0)
        {
            float totalElectricityNeeded = electricityRequired;
            if (!CampManager.Instance.ElectricityManager.HasEnoughElectricity(totalElectricityNeeded))
            {
                SetOperationalStatus(false);
                return false;
            }
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
        // No need to unregister electricity consumption anymore since it's handled during work
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
    public bool AssignNPC(HumanCharacterController npc)
    {
        if (currentWorkers.Contains(npc))
        {
            return false; // Already assigned
        }
        
        if (currentWorkers.Count >= maxWorkers)
        {
            return false; // Task is full
        }
        
        currentWorkers.Add(npc);
        
        if (taskStructure != null)
        {
            taskStructure.SetCurrentWorkTask(this);
        }
        
        // Notify the worker that they can start working
        npc.StartWork(this);
        
        return true;
    }

    // Method to unassign the current NPC
    public void UnassignNPC()
    {
        if (currentWorkers.Count > 0)
        {
            currentWorkers.RemoveAt(currentWorkers.Count - 1); // Remove the last assigned NPC
        }
        
        if (currentWorkers.Count == 0 && taskStructure != null)
        {
            taskStructure.SetCurrentWorkTask(null);
        }
    }

    // Method to unassign a specific NPC
    public void UnassignNPC(HumanCharacterController npc)
    {
        if (currentWorkers.Contains(npc))
        {
            currentWorkers.Remove(npc);
        }
        
        if (currentWorkers.Count == 0 && taskStructure != null)
        {
            taskStructure.SetCurrentWorkTask(null);
        }
    }

    // Method to check if the task is currently assigned
    public bool IsAssigned()
    {
        return currentWorkers.Count > 0;
    }

    /// <summary>
    /// Simple method called by WorkState to advance work progress
    /// </summary>
    /// <param name="worker">The worker performing the work</param>
    /// <param name="deltaTime">Time since last frame</param>
    /// <returns>True if work can continue, false if should stop</returns>
    public virtual bool DoWork(HumanCharacterController worker, float deltaTime)
    {
        if (!isOperational || !currentWorkers.Contains(worker))
        {
            return false;
        }

        // Get worker speed multiplier
        float workSpeed = 1f;
        if (worker is SettlerNPC settler)
        {
            workSpeed = settler.GetWorkSpeedMultiplier();
            // If settler is starving, they can't work
            if (workSpeed <= 0)
            {
                return false;
            }
        }

        // Calculate work progress for this frame
        float workDelta = deltaTime * workSpeed;
        
        // All tasks consume electricity while working (default 1 unit per baseWorkTime)
        // Distribute electricity consumption across all current workers
        float electricityConsumption = electricityRequired > 0 ? electricityRequired : 1f;
        float electricityRate = electricityConsumption / baseWorkTime;
        float electricityPerWorker = electricityRate / Mathf.Max(1, currentWorkers.Count);
        float electricityNeeded = electricityPerWorker * workDelta;
        
        // Check and consume electricity
        if (electricityNeeded > 0)
        {
            if (!CampManager.Instance.ElectricityManager.ConsumeElectricity(electricityNeeded, 1f))
            {
                // Not enough electricity, task becomes non-operational
                SetOperationalStatus(false);
                return false;
            }
        }
        
        // Advance work progress
        workProgress += workDelta;
        
        // Check if work is complete
        if (workProgress >= baseWorkTime)
        {
            workProgress = baseWorkTime;
            CompleteWork();
            return false; // Work is done
        }
        
        return true; // Continue working
    }

    // Virtual method for completing work that can be overridden
    protected virtual void CompleteWork()
    {
        // Store the first worker as previous worker before clearing (for backward compatibility)
        if (currentWorkers.Count > 0)
        {
            CampManager.Instance.WorkManager.StorePreviousWorker(this, currentWorkers[0]);
        }
        
        // Reset state
        workProgress = 0f;
        
        // Stop all workers from working on this task
        var workersToStop = new List<HumanCharacterController>(currentWorkers);
        foreach (var worker in workersToStop)
        {
            worker.StopWork();
        }
        
        // Clear all workers
        currentWorkers.Clear();
        
        if (taskStructure != null)
        {
            taskStructure.SetCurrentWorkTask(null);
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

    protected void AddResourceToInventory(ResourceItemCount[] resourceItemCounts)
    {
        foreach (var resourceItemCount in resourceItemCounts)
        {
            PlayerInventory.Instance.AddItem(resourceItemCount.resourceScriptableObj, resourceItemCount.count);
        }
    }

    protected virtual void OnDisable()
    {
        // No need to stop coroutine here, workers manage their own
    }

    public void SetOperationalStatus(bool operational)
    {
        if (isOperational != operational)
        {
            isOperational = operational;
            
            if (!isOperational)
            {
                // Stop all current workers
                if (currentWorkers.Count > 0)
                {
                    var workersToUnassign = new List<HumanCharacterController>(currentWorkers);
                    foreach (var worker in workersToUnassign)
                    {
                        if (worker is SettlerNPC settler)
                        {
                            settler.ClearAssignedWork(); // Clear the assigned work
                            settler.ChangeTask(TaskType.WANDER);
                        }
                        else if (worker is RobotCharacterController robot)
                        {
                            robot.StopWork();
                        }
                    }
                    currentWorkers.Clear();
                    
                    if (taskStructure != null)
                    {
                        taskStructure.SetCurrentWorkTask(null);
                    }
                }
            }
            else
            {
                // If we become operational again and have a previous worker, try to reassign them
                var previousWorker = CampManager.Instance.WorkManager.GetPreviousWorkerForTask(this);
                if (previousWorker != null)
                {
                    // Check if we have enough electricity before reassigning
                    if (electricityRequired > 0 && !CampManager.Instance.ElectricityManager.HasEnoughElectricity(electricityRequired))
                    {
                        // Still not enough electricity, keep as non-operational
                        isOperational = false;
                        return;
                    }
                    
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

    // Method to remove a specific worker from the task
    public bool RemoveWorker(HumanCharacterController npc)
    {
        if (currentWorkers.Contains(npc))
        {
            currentWorkers.Remove(npc);
            
            // Stop the worker from working on this task
            npc.StopWork();
            
            if (currentWorkers.Count == 0 && taskStructure != null)
            {
                taskStructure.SetCurrentWorkTask(null);
            }
            
            return true;
        }
        return false;
    }
}
