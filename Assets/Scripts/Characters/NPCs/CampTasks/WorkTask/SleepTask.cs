using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Managers;

/// <summary>
/// SleepTask handles bed assignments and sleeping behavior for NPCs.
/// Each bed can be assigned to one NPC, and NPCs will navigate to their assigned bed when tired.
/// </summary>
public class SleepTask : WorkTask
{
    [Header("Sleep Settings")]
    [SerializeField] private float sleepTime = 8f; // How long it takes to complete sleeping (8 hours)
    
    [Header("Bed Assignment")]
    [SerializeField, ReadOnly] private SettlerNPC assignedSettler; // The NPC assigned to this bed
    [SerializeField] private bool isBedAssigned = false; // Whether this bed has an assigned NPC
    
    // Properties
    public bool IsBedAssigned => isBedAssigned;
    public SettlerNPC AssignedSettler => assignedSettler;
    
    protected override void Start()
    {
        base.Start();
        
        // Sleep tasks are single-worker tasks (one bed per NPC)
        maxWorkers = 1;
        
        // Set sleep animation
        taskAnimation = TaskAnimation.SLEEPING;
        
        // Set work time to sleep duration
        baseWorkTime = sleepTime;
        
        // No electricity required for sleeping
        electricityRequired = 0f;
        
        // Show tooltip for bed information
        showTooltip = true;
        
        // If no specific work location is set, use this transform
        if (workLocationTransform == null)
        {
            workLocationTransform = transform;
        }
        
        // Add this task to the work manager
        AddWorkTask();
    }
    
    /// <summary>
    /// Assign a specific settler to this bed
    /// </summary>
    /// <param name="settler">The settler to assign to this bed</param>
    /// <returns>True if assignment successful, false otherwise</returns>
    public bool AssignSettlerToBed(SettlerNPC settler)
    {
        if (settler == null)
        {
            Debug.LogWarning("[SleepTask] Cannot assign null settler to bed");
            return false;
        }
        
        if (isBedAssigned && assignedSettler != settler)
        {
            Debug.LogWarning($"[SleepTask] Bed {name} is already assigned to {assignedSettler.name}");
            return false;
        }
        
        // Unassign current settler if different
        if (isBedAssigned && assignedSettler != settler)
        {
            UnassignSettlerFromBed();
        }
        
        // Assign new settler
        assignedSettler = settler;
        isBedAssigned = true;
        
        Debug.Log($"[SleepTask] Assigned {settler.name} to bed {name}");
        
        return true;
    }
    
    /// <summary>
    /// Unassign the current settler from this bed
    /// </summary>
    public void UnassignSettlerFromBed()
    {
        if (assignedSettler != null)
        {
            Debug.Log($"[SleepTask] Unassigned {assignedSettler.name} from bed {name}");
            
            // Stop any current work on this task
            if (currentWorkers.Contains(assignedSettler))
            {
                assignedSettler.StopWork();
            }
            
            assignedSettler = null;
        }
        
        isBedAssigned = false;
        
        // Clear any current workers
        if (currentWorkers.Count > 0)
        {
            var workersToStop = new List<HumanCharacterController>(currentWorkers);
            foreach (var worker in workersToStop)
            {
                worker.StopWork();
            }
            currentWorkers.Clear();
        }
    }
    
    /// <summary>
    /// Check if a specific settler can use this bed
    /// </summary>
    /// <param name="settler">The settler to check</param>
    /// <returns>True if the settler can use this bed</returns>
    public bool CanSettlerUseBed(SettlerNPC settler)
    {
        if (settler == null) return false;
        
        // If bed is unassigned, anyone can use it
        if (!isBedAssigned) return true;
        
        // If bed is assigned to this settler, they can use it
        return assignedSettler == settler;
    }
    
    /// <summary>
    /// Override to handle sleep-specific work logic
    /// </summary>
    public override bool DoWork(HumanCharacterController worker, float deltaTime)
    {
        if (!isOperational || !currentWorkers.Contains(worker))
        {
            return false;
        }
        
        // Check if this is the assigned settler for this bed
        if (worker is SettlerNPC settler && isBedAssigned && assignedSettler != settler)
        {
            Debug.LogWarning($"[SleepTask] {settler.name} trying to use bed assigned to {assignedSettler.name}");
            return false;
        }
        
        // Get final work speed (including cleanliness modifier)
        float finalWorkSpeed = GetFinalWorkSpeed(worker);
        
        // If worker can't work (starving, etc.), stop
        if (finalWorkSpeed <= 0)
        {
            return false;
        }
        
        // Calculate work progress for this frame
        float workDelta = deltaTime * finalWorkSpeed;
        
        // Don't generate dirt from sleeping (Sleeping is a clean activity)
        
        // Validate work task data
        if (baseWorkTime <= 0)
        {
            Debug.LogError($"[SleepTask] Invalid baseWorkTime ({baseWorkTime}) for {GetType().Name}. Sleep task cannot be performed. NPC {worker.name} will return to wander state.");
            SetOperationalStatus(false);
            return false;
        }
        
        // No electricity consumption for sleeping
        
        // Advance work progress
        workProgress += workDelta;
        
        // Check if sleep is complete
        if (workProgress >= baseWorkTime)
        {
            workProgress = baseWorkTime;
            CompleteSleep();
            return false; // Sleep is done
        }
        
        return true; // Continue sleeping
    }
    
    /// <summary>
    /// Handle sleep completion
    /// </summary>
    protected virtual void CompleteSleep()
    {
        // Store the first worker as previous worker before clearing (for backward compatibility)
        if (currentWorkers.Count > 0)
        {
            CampManager.Instance.WorkManager.StorePreviousWorker(this, currentWorkers[0]);
        }
        
        // Reset state
        workProgress = 0f;
        
        // Stop all workers from sleeping on this task
        var workersToStop = new List<HumanCharacterController>(currentWorkers);
        foreach (var worker in workersToStop)
        {
            worker.StopWork();
        }
        
        // Clear all workers
        currentWorkers.Clear();
        
        // Notify completion
        InvokeStopWork();
    }
    
    /// <summary>
    /// Override tooltip to show bed assignment information
    /// </summary>
    public override string GetTooltipText()
    {
        if (!showTooltip) return string.Empty;

        string tooltip = $"Bed\n";
        tooltip += $"Sleep Time: {sleepTime} hours\n";
        
        if (isBedAssigned && assignedSettler != null)
        {
            tooltip += $"Assigned to: {assignedSettler.name}\n";
            tooltip += $"Status: Occupied";
        }
        else
        {
            tooltip += $"Status: Available";
        }
        
        return tooltip;
    }
    
    protected override void OnDestroy()
    {
        // Unassign settler when bed is destroyed
        if (assignedSettler != null)
        {
            UnassignSettlerFromBed();
        }
        
        base.OnDestroy();
    }
}
