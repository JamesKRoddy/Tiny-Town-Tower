using UnityEngine;
using System.Collections;
using Managers;

/// <summary>
/// Used by dirt piles and waste bins to clean them.
/// </summary>
public class CleaningTask : ManagerTask
{
    private DirtPileTask targetDirtPile;
    private WasteBin targetWasteBin;

    public DirtPileTask GetCurrentDirtPileTarget() => targetDirtPile;
    public WasteBin GetCurrentWasteBinTarget() => targetWasteBin;

    protected override void Start()
    {
        base.Start();
        taskType = WorkTaskType.Continuous; // Cleaning is continuous - new dirt piles and bins appear regularly
        CampManager.Instance.CleanlinessManager.OnDirtPileSpawned += HandleDirtPileSpawned;
        CampManager.Instance.CleanlinessManager.OnWasteBinFull += HandleWasteBinFull;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (CampManager.Instance?.CleanlinessManager != null)
        {
            CampManager.Instance.CleanlinessManager.OnDirtPileSpawned -= HandleDirtPileSpawned;
            CampManager.Instance.CleanlinessManager.OnWasteBinFull -= HandleWasteBinFull;
        }
    }

    private void HandleDirtPileSpawned(DirtPileTask dirtPile)
    {
        // If we have a worker but no current target, start cleaning the new dirt pile
        if (currentWorkers.Count > 0 && targetDirtPile == null && targetWasteBin == null)
        {
            SetupDirtPileTask(dirtPile);
        }
    }

    private void HandleWasteBinFull(WasteBin bin)
    {
        // If we have a worker but no current target, start emptying the full bin
        if (currentWorkers.Count > 0 && targetDirtPile == null && targetWasteBin == null)
        {
            SetupWasteBinTask(bin);
        }
    }

    public void SetupDirtPileTask(DirtPileTask dirtPile)
    {
        if (dirtPile == null) return;

        targetDirtPile = dirtPile;
        targetWasteBin = null;
        workLocationTransform = dirtPile.transform;
        dirtPile.StartCleaning();
        
        // Subscribe to the task completion event
        dirtPile.OnTaskCompleted += HandleSubtaskCompleted;
        
        // Notify the worker's WorkState to update its path
        if (currentWorkers.Count > 0)
        {
            var workState = currentWorkers[0].GetComponent<WorkState>();
            workState?.UpdateTaskDestination();
        }
    }

    public void SetupWasteBinTask(WasteBin bin)
    {
        if (bin == null) return;

        targetWasteBin = bin;
        targetDirtPile = null;
        workLocationTransform = bin.transform;
        
        // Subscribe to the task completion event
        var binTask = bin.GetCleaningTask();
        if (binTask != null)
        {
            binTask.OnTaskCompleted += HandleSubtaskCompleted;
        }
        
        // Notify the worker's WorkState to update its path
        if (currentWorkers.Count > 0)
        {
            var workState = currentWorkers[0].GetComponent<WorkState>();
            workState?.UpdateTaskDestination();
        }
    }

    private void FindNextCleaningTask()
    {
        if (!isOperational || currentWorkers.Count == 0)
        {
            return;
        }

        // First check for full waste bins
        var fullBins = CampManager.Instance.CleanlinessManager.GetFullWasteBins();
        foreach (var bin in fullBins)
        {
            if (bin != null && !bin.IsBeingEmptied())
            {
                SetupWasteBinTask(bin);
                return;
            }
        }

        // If no full bins, check for dirt piles
        var dirtPiles = CampManager.Instance.CleanlinessManager.GetActiveDirtPiles();
        foreach (var dirtPile in dirtPiles)
        {
            if (dirtPile != null && !dirtPile.IsOccupied)
            {
                SetupDirtPileTask(dirtPile);
                return;
            }
        }
    }

    public override void PerformTask(HumanCharacterController npc)
    {
        // If we don't have a current task, look for one
        if (targetDirtPile == null && targetWasteBin == null)
        {
            FindNextCleaningTask();
        }

        if (targetDirtPile != null)
        {
            // Transition to the dirt pile task
            TransitionToSubtask(targetDirtPile, npc);
        }
        else if (targetWasteBin != null)
        {
            // Transition to the waste bin task
            var binTask = targetWasteBin.GetCleaningTask();
            if (binTask != null)
            {
                TransitionToSubtask(binTask, npc);
            }
        }
        else
        {
            base.PerformTask(npc);
        }
    }

    public override void HandleSubtaskCompleted()
    {        
        // Unsubscribe from the completed task
        if (targetDirtPile != null)
        {
            targetDirtPile.OnTaskCompleted -= HandleSubtaskCompleted;
            targetDirtPile = null;
        }
        else if (targetWasteBin != null)
        {
            var binTask = targetWasteBin.GetCleaningTask();
            if (binTask != null)
            {
                binTask.OnTaskCompleted -= HandleSubtaskCompleted;
            }
            targetWasteBin = null;
        }
        
        // Call base to handle any base class cleanup
        base.HandleSubtaskCompleted();
        
        // Look for the next task
        if (currentWorkers.Count > 0)
        {
            FindNextCleaningTask();
        }
    }

    public override string GetTooltipText()
    {
        if (!showTooltip) return string.Empty;

        string tooltip = "Cleaning Task\n";
        tooltip += $"Status: {(isOperational ? "Operational" : "Not Operational")}\n";
        
        if (targetDirtPile != null)
        {
            tooltip += $"Current Task: Cleaning Dirt Pile\n";
            tooltip += $"Progress: {(targetDirtPile.GetProgress() * 100):F1}%\n";
        }
        else if (targetWasteBin != null)
        {
            tooltip += $"Current Task: Emptying Waste Bin\n";
            tooltip += $"Fill Level: {targetWasteBin.GetFillPercentage():F1}%\n";
        }
        else
        {
            tooltip += "Current Task: Looking for cleaning tasks\n";
        }
        return tooltip;
    }
} 