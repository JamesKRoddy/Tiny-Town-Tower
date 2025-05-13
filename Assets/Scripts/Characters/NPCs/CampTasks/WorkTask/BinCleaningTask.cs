using System.Collections;
using UnityEngine;

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

    protected override IEnumerator WorkCoroutine()
    {
        if (targetBin == null) yield break;

        targetBin.StartEmptying();
        emptyProgress = 0f;

        while (emptyProgress < baseWorkTime)
        {
            emptyProgress += Time.deltaTime * emptySpeed;
            targetBin.AddEmptyProgress(Time.deltaTime * emptySpeed);
            yield return null;
        }

        CompleteWork();
    }

    public override string GetTooltipText()
    {
        if (!showTooltip) return string.Empty;

        string tooltip = "Bin Cleaning Task\n";
        tooltip += $"Time: {baseWorkTime} seconds\n";
        tooltip += $"Status: {(isOperational ? "Operational" : "Not Operational")}\n";
        return tooltip;
    }
} 