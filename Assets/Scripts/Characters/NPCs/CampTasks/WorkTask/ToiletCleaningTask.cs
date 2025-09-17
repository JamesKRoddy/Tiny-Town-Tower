using System.Collections;
using UnityEngine;
using Managers;

public class ToiletCleaningTask : WorkTask
{
    private Toilet targetToilet;
    private float emptyProgress = 0f;
    private float emptySpeed = 1f;

    public void SetupTask(Toilet toilet)
    {
        targetToilet = toilet;
        taskType = WorkTaskType.Complete; // Toilet cleaning is a one-time task
        baseWorkTime = 10f;
        workLocationTransform = toilet.transform;
    }

    protected override void CompleteWork()
    {
        if (targetToilet != null)
        {
            targetToilet.StopEmptying();
        }
        base.CompleteWork();
    }

    public override bool DoWork(HumanCharacterController worker, float deltaTime)
    {
        if (targetToilet == null)
        {
            return false;
        }

        // Start emptying if not already started
        if (emptyProgress == 0f)
        {
            targetToilet.StartEmptying();
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
            targetToilet.AddEmptyProgress(emptyDelta);
        }
        
        return canContinue;
    }

    public override string GetTooltipText()
    {
        if (!showTooltip) return string.Empty;

        string tooltip = "Toilet Cleaning Task\n";
        tooltip += $"Time: {baseWorkTime} seconds\n";
        tooltip += $"Status: {(isOperational ? "Operational" : "Not Operational")}\n";
        if (targetToilet != null)
        {
            tooltip += $"Fill Level: {targetToilet.GetFillPercentage():F1}%\n";
        }
        return tooltip;
    }

    public override bool CanPerformTask()
    {
        return targetToilet != null && targetToilet.IsFull() && !targetToilet.IsBeingEmptied();
    }
} 