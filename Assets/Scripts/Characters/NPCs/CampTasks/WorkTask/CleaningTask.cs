using UnityEngine;
using System.Collections;
using Managers;

/// <summary>
/// Used by dirt piles to clean them.
/// </summary>
public class CleaningTask : WorkTask
{
    [SerializeField] private float cleaningTime = 5f;
    [SerializeField] private float cleanlinessIncrease = 10f;
    
    private float cleaningProgress = 0f;

    protected override void Start()
    {
        base.Start();
        workType = WorkType.CLEANING;
        baseWorkTime = cleaningTime;
    }

    private IEnumerator CleaningCoroutine()
    {
        while (cleaningProgress < cleaningTime)
        {
            cleaningProgress += Time.deltaTime;
            yield return null;
        }

        CompleteCleaning();
    }

    private void CompleteCleaning()
    {
        // Increase the camp's cleanliness
        CampManager.Instance.CleanlinessManager.IncreaseCleanliness(cleanlinessIncrease);
        Debug.Log($"Cleaning completed! Increased cleanliness by {cleanlinessIncrease}");
        
        // Reset state
        cleaningProgress = 0f;
        currentWorker = null;
        workCoroutine = null;
        
        // Notify completion
        InvokeStopWork();
    }

    protected override void CompleteWork()
    {
        // Increase the camp's cleanliness
        CampManager.Instance.CleanlinessManager.IncreaseCleanliness(cleanlinessIncrease);
        Debug.Log($"Cleaning completed! Increased cleanliness by {cleanlinessIncrease}");
        
        base.CompleteWork();
    }

    public override bool CanPerformTask()
    {
        // Check if the camp's cleanliness is below maximum
        return CampManager.Instance.CleanlinessManager.GetCleanliness() < CampManager.Instance.CleanlinessManager.GetCleanlinessPercentage();
    }
} 