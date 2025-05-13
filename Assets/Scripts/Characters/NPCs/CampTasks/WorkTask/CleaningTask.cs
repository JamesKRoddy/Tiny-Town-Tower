using UnityEngine;
using System.Collections;
using Managers;

/// <summary>
/// Used by dirt piles to clean them.
/// </summary>
public class CleaningTask : WorkTask
{
    private DirtPile targetDirtPile;
    private float cleanProgress = 0f;
    private float cleanSpeed = 1f;

    public void SetupTask(DirtPile dirtPile)
    {
        targetDirtPile = dirtPile;
        baseWorkTime = 5f;
        if (dirtPile != null)
        {
            workLocationTransform = dirtPile.transform;
        }
    }

    protected override void CompleteWork()
    {
        if (targetDirtPile != null)
        {
            targetDirtPile.StopCleaning();
            targetDirtPile = null;
        }
        base.CompleteWork();
    }

    protected override IEnumerator WorkCoroutine()
    {
        if (targetDirtPile == null)
        {
            // If no target, wait a bit and then check for new tasks
            yield return new WaitForSeconds(1f);
            yield break;
        }

        targetDirtPile.StartCleaning();
        cleanProgress = 0f;

        while (cleanProgress < baseWorkTime)
        {
            cleanProgress += Time.deltaTime * cleanSpeed;
            targetDirtPile.AddCleanProgress(Time.deltaTime * cleanSpeed);
            yield return null;
        }

        CompleteWork();
    }

    public override string GetTooltipText()
    {
        if (!showTooltip) return string.Empty;

        string tooltip = "Cleaning Task\n";
        tooltip += $"Time: {baseWorkTime} seconds\n";
        tooltip += $"Status: {(isOperational ? "Operational" : "Not Operational")}\n";
        if (targetDirtPile != null)
        {
            tooltip += $"Current Task: Cleaning Dirt Pile\n";
            tooltip += $"Progress: {(cleanProgress / baseWorkTime * 100):F1}%\n";
        }
        else
        {
            tooltip += "Current Task: Looking for cleaning tasks\n";
        }
        return tooltip;
    }
} 