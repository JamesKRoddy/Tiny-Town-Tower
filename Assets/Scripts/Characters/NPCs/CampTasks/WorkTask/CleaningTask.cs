using UnityEngine;
using System.Collections;
using Managers;

/// <summary>
/// Used by dirt piles to clean them.
/// </summary>
public class CleaningTask : ManagerTask
{
    private DirtPileTask targetDirtPile;

    public DirtPileTask GetCurrentTarget() => targetDirtPile;

    protected override void Start()
    {
        base.Start();
        CampManager.Instance.CleanlinessManager.OnDirtPileSpawned += HandleDirtPileSpawned;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (CampManager.Instance?.CleanlinessManager != null)
        {
            CampManager.Instance.CleanlinessManager.OnDirtPileSpawned -= HandleDirtPileSpawned;
        }
    }

    private void HandleDirtPileSpawned(DirtPileTask dirtPile)
    {
        // If we have a worker but no current target, start cleaning the new dirt pile
        if (currentWorker != null && targetDirtPile == null)
        {
            SetupTask(dirtPile);
        }
    }

    public void SetupTask(DirtPileTask dirtPile)
    {
        if (dirtPile == null) return;

        targetDirtPile = dirtPile;
        workLocationTransform = dirtPile.transform;
        dirtPile.StartCleaning();
        
        // Notify the worker's WorkState to update its path
        if (currentWorker != null)
        {
            var workState = currentWorker.GetComponent<WorkState>();
            workState?.UpdateTaskDestination();
        }
    }

    protected override void CompleteWork()
    {
        if (targetDirtPile != null)
        {
            targetDirtPile.StopCleaning();
            targetDirtPile = null;
        }
        
        // Look for next dirt pile
        FindNextDirtPile();
    }

    private void FindNextDirtPile()
    {
        var dirtPiles = CampManager.Instance.CleanlinessManager.GetActiveDirtPiles();
        
        foreach (var dirtPile in dirtPiles)
        {
            if (dirtPile != null && !dirtPile.IsOccupied)
            {
                SetupTask(dirtPile);
                return;
            }
        }
    }

    public override void PerformTask(HumanCharacterController npc)
    {
        if (targetDirtPile != null)
        {
            // Transition to the dirt pile task
            TransitionToSubtask(targetDirtPile, npc);
        }
        else
        {
            base.PerformTask(npc);
        }
    }

    protected override void HandleSubtaskCompleted()
    {
        base.HandleSubtaskCompleted();
        targetDirtPile = null;
        FindNextDirtPile();
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
        else
        {
            tooltip += "Current Task: Looking for dirt piles\n";
        }
        return tooltip;
    }
} 