using System.Collections;
using UnityEngine;
using Managers;

public class BinCleaningTask : WorkTask
{
    private WasteBin targetBin;
    private float emptyProgress = 0f;
    private float emptySpeed = 1f;

    public void SetupTask(WasteBin bin)
    {
        targetBin = bin;
        baseWorkTime = 5f;
        workLocationTransform = bin.transform;
    }

    protected override void CompleteWork()
    {
        if (targetBin != null)
        {
            targetBin.StopEmptying();
        }
        base.CompleteWork();
    }

    public override bool DoWork(HumanCharacterController worker, float deltaTime)
    {
        if (targetBin == null)
        {
            return false;
        }

        // Start emptying if not already started
        if (emptyProgress == 0f)
        {
            targetBin.StartEmptying();
        }

        // Call base DoWork to handle electricity and progress
        bool canContinue = base.DoWork(worker, deltaTime);
        
        if (canContinue)
        {
            // Get worker speed
            float workSpeed = 1f;
            if (worker is SettlerNPC settler)
            {
                workSpeed = settler.GetWorkSpeedMultiplier();
            }
            
            // Process emptying (affected by work speed)
            float emptyDelta = deltaTime * emptySpeed * workSpeed;
            emptyProgress += emptyDelta;
            targetBin.AddEmptyProgress(emptyDelta);
        }
        
        return canContinue;
    }

    public override string GetTooltipText()
    {
        if (!showTooltip) return string.Empty;

        string tooltip = "Bin Cleaning Task\n";
        tooltip += $"Time: {baseWorkTime} seconds\n";
        tooltip += $"Status: {(isOperational ? "Operational" : "Not Operational")}\n";
        if (targetBin != null)
        {
            tooltip += $"Fill Level: {targetBin.GetFillPercentage():F1}%\n";
        }
        return tooltip;
    }

    public override bool CanPerformTask()
    {
        return targetBin != null && targetBin.IsFull() && !targetBin.IsBeingEmptied();
    }
} 