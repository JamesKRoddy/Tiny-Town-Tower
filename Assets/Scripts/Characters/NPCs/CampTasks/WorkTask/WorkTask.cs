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
    protected Coroutine workCoroutine;

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

    // Virtual method for NPC to perform the work task
    public virtual void PerformTask(HumanCharacterController npc)
    {
        if (!currentWorkers.Contains(npc) && currentWorkers.Count < maxWorkers)
        {
            currentWorkers.Add(npc);
            
            // Start work coroutine if this is the first worker and we're not already working
            if (currentWorkers.Count == 1 && workCoroutine == null)
            {
                workCoroutine = StartCoroutine(WorkCoroutine());
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
        
        // Start work coroutine if this is the first worker and we're not already working
        if (currentWorkers.Count == 1 && workCoroutine == null)
        {
            workCoroutine = StartCoroutine(WorkCoroutine());
        }
        
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

    // Helper method to calculate total work speed from all workers
    protected float GetTotalWorkSpeed()
    {
        if (currentWorkers.Count == 0) return 0f;
        
        float totalSpeed = 0f;
        foreach (var worker in currentWorkers)
        {
            if (worker is SettlerNPC settler)
            {
                totalSpeed += settler.GetWorkSpeedMultiplier();
            }
            else
            {
                totalSpeed += 1f; // Default speed for non-settler workers
            }
        }
        
        // For multi-worker tasks, apply a bonus (diminishing returns)
        if (currentWorkers.Count > 1)
        {
            totalSpeed = Mathf.Sqrt(currentWorkers.Count) * (totalSpeed / currentWorkers.Count);
        }
        
        return totalSpeed;
    }

    // Virtual work coroutine that can be overridden by specific tasks
    protected virtual IEnumerator WorkCoroutine()
    {
        // Reset work progress at the start of each task
        workProgress = 0f;
        
        while (workProgress < baseWorkTime)
        {
            // Get total work speed from all workers
            float workSpeed = GetTotalWorkSpeed();
            
            // If no work speed (all workers starving), stop working
            if (workSpeed <= 0)
            {
                // Take breaks for all starving workers
                foreach (var worker in currentWorkers)
                {
                    if (worker is SettlerNPC settler)
                    {
                        settler.TakeBreak(); // Take a break instead of stopping work completely
                    }
                }
                StopWorkCoroutine();
                yield break;
            }

            // Consume electricity based on work progress if required
            if (electricityRequired > 0)
            {
                float consumptionRate = electricityRequired / baseWorkTime; // Electricity per second
                if (!CampManager.Instance.ElectricityManager.ConsumeElectricity(consumptionRate, Time.deltaTime))
                {
                    // Not enough electricity, stop working
                    SetOperationalStatus(false);
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
        // Store the first worker as previous worker before clearing (for backward compatibility)
        if (currentWorkers.Count > 0)
        {
            CampManager.Instance.WorkManager.StorePreviousWorker(this, currentWorkers[0]);
        }
        
        // Reset state
        workProgress = 0f;
        
        StopWorkCoroutine();
        
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
                
                // Unassign all current workers
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
            
            // If no more workers, stop the work coroutine
            if (currentWorkers.Count == 0)
            {
                StopWorkCoroutine();
                
                if (taskStructure != null)
                {
                    taskStructure.SetCurrentWorkTask(null);
                }
            }
            
            return true;
        }
        return false;
    }
}
