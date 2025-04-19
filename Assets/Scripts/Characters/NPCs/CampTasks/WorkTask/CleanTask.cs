using UnityEngine;
using System.Collections;
using Managers;
public class CleanTask : ResourceWorkTask
{
    [SerializeField] private float cleanTime = 30f;
    [SerializeField] private float cleanlinessIncrease = 10f;

    protected override void Start()
    {
        base.Start();
        workType = WorkType.CLEANING;
        baseWorkTime = cleanTime;
    }

    protected override IEnumerator WorkCoroutine()
    {
        while (workProgress < baseWorkTime)
        {
            workProgress += Time.deltaTime;
            yield return null;
        }

        CompleteWork();
    }

    protected override void CompleteWork()
    {
        // Increase camp cleanliness
        CampManager.Instance.CleanlinessManager.IncreaseCleanliness(cleanlinessIncrease);
        Debug.Log($"Cleaning completed! Increased cleanliness by {cleanlinessIncrease}");
        
        base.CompleteWork();
    }
} 