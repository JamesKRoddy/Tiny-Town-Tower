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
    private bool hasStartedCleaning = false;

    public DirtPile GetCurrentTarget() => targetDirtPile;

    protected override void Start()
    {
        base.Start();
        CampManager.Instance.CleanlinessManager.OnDirtPileSpawned += HandleDirtPileSpawned;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (CampManager.Instance != null && CampManager.Instance.CleanlinessManager != null)
        {
            CampManager.Instance.CleanlinessManager.OnDirtPileSpawned -= HandleDirtPileSpawned;
        }
    }

    private void HandleDirtPileSpawned(DirtPile dirtPile)
    {
        // If we have a worker but no current target, start cleaning the new dirt pile
        if (currentWorker != null && targetDirtPile == null)
        {
            SetupTask(dirtPile);
            workCoroutine = StartCoroutine(WorkCoroutine());
        }
    }

    public void SetupTask(DirtPile dirtPile)
    {
        targetDirtPile = dirtPile;
        baseWorkTime = 5f;
        hasStartedCleaning = false;
        if (dirtPile != null)
        {
            workLocationTransform = dirtPile.transform;
            // Notify the worker's WorkState to update its path
            if (currentWorker != null)
            {
                var workState = currentWorker.GetComponent<WorkState>();
                if (workState != null)
                {
                    workState.UpdateTaskDestination();
                }
            }
        }
    }

    protected override void CompleteWork()
    {
        if (targetDirtPile != null)
        {
            targetDirtPile.StopCleaning();
            targetDirtPile = null;
        }
        hasStartedCleaning = false;
        
        // Look for next dirt pile
        FindNextDirtPile();
    }

    private void FindNextDirtPile()
    {
        var dirtPiles = CampManager.Instance.CleanlinessManager.GetActiveDirtPiles();
        
        foreach (var dirtPile in dirtPiles)
        {
            if (dirtPile != null && !dirtPile.IsBeingCleaned())
            {
                StopWorkCoroutine();
                SetupTask(dirtPile);
                
                if (currentWorker != null)
                {
                    workCoroutine = StartCoroutine(WorkCoroutine());
                }
                return;
            }
        }
        
        // No dirt piles found, wait for new ones
        if (currentWorker != null)
        {
            workCoroutine = StartCoroutine(WorkCoroutine());
        }
    }

    protected override IEnumerator WorkCoroutine()
    {
        if (targetDirtPile == null)
        {
            // Wait a bit and check for new dirt piles
            yield return new WaitForSeconds(1f);
            FindNextDirtPile();
            yield break;
        }

        // Wait until the NPC is close enough to the dirt pile
        while (!hasStartedCleaning && targetDirtPile != null)
        {
            float distanceToDirtPile = Vector3.Distance(currentWorker.transform.position, targetDirtPile.transform.position);
            if (distanceToDirtPile <= 1f) // NPC is close enough to start cleaning
            {
                hasStartedCleaning = true;
                targetDirtPile.StartCleaning();
                cleanProgress = 0f;
                break;
            }
            yield return null;
        }

        // Now start the actual cleaning process
        while (cleanProgress < baseWorkTime && targetDirtPile != null && hasStartedCleaning)
        {
            cleanProgress += Time.deltaTime * cleanSpeed;
            if (targetDirtPile != null)
            {
                targetDirtPile.AddCleanProgress(Time.deltaTime * cleanSpeed);
            }
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
            if (hasStartedCleaning)
            {
                tooltip += $"Progress: {(cleanProgress / baseWorkTime * 100):F1}%\n";
            }
            else
            {
                tooltip += "Moving to dirt pile...\n";
            }
        }
        else
        {
            tooltip += "Current Task: Looking for dirt piles\n";
        }
        return tooltip;
    }
} 