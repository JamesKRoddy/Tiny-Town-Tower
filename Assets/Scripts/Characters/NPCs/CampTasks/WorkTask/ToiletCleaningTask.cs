using System.Collections;
using UnityEngine;

public class ToiletCleaningTask : WorkTask
{
    private Toilet targetToilet;
    private float emptyProgress = 0f;
    private float emptySpeed = 1f;

    public void SetupTask(Toilet toilet)
    {
        targetToilet = toilet;
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

    protected override IEnumerator WorkCoroutine()
    {
        if (targetToilet == null) yield break;

        targetToilet.StartEmptying();
        emptyProgress = 0f;

        while (emptyProgress < baseWorkTime)
        {
            emptyProgress += Time.deltaTime * emptySpeed;
            targetToilet.AddEmptyProgress(Time.deltaTime * emptySpeed);
            yield return null;
        }

        CompleteWork();
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