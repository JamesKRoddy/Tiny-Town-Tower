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
        workLocationTransform = dirtPile.transform;
    }

    protected override void CompleteWork()
    {
        if (targetDirtPile != null)
        {
            targetDirtPile.StopCleaning();
        }
        base.CompleteWork();
    }

    protected override IEnumerator WorkCoroutine()
    {
        if (targetDirtPile == null) yield break;

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
        return tooltip;
    }

    public override bool CanPerformTask()
    {
        // Check if the camp's cleanliness is below maximum
        return CampManager.Instance.CleanlinessManager.GetCleanliness() < CampManager.Instance.CleanlinessManager.GetCleanlinessPercentage();
    }
} 