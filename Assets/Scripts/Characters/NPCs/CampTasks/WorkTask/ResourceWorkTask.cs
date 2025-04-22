using UnityEngine;
using System.Collections;

public abstract class ResourceWorkTask : WorkTask
{
    protected float workProgress = 0f;
    protected float baseWorkTime = 5f;
    protected int resourceAmount = 1;
    protected SettlerNPC currentWorker;
    protected Coroutine workCoroutine;

    public bool IsOccupied => currentWorker != null;

    protected override void Start()
    {
        base.Start();
    }

    public override void PerformTask(SettlerNPC npc)
    {
        if (currentWorker == null)
        {
            currentWorker = npc;
            workCoroutine = StartCoroutine(WorkCoroutine());
        }
    }

    protected virtual IEnumerator WorkCoroutine()
    {
        while (workProgress < baseWorkTime)
        {
            workProgress += Time.deltaTime;
            yield return null;
        }

        CompleteWork();
    }

    protected virtual void CompleteWork()
    {
        // Reset state
        workProgress = 0f;
        currentWorker = null;
        workCoroutine = null;
        
        // Notify completion
        InvokeStopWork();
    }

    protected virtual void OnDisable()
    {
        if (workCoroutine != null)
        {
            StopCoroutine(workCoroutine);
            workCoroutine = null;
        }
    }
} 