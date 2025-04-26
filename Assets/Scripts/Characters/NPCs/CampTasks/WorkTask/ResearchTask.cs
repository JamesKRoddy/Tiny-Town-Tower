using UnityEngine;
using System.Collections;
using Managers;

public class ResearchTask : WorkTask
{
    [SerializeField] private float researchTime = 60f;
    [SerializeField] private int researchPoints = 1;

    protected override void Start()
    {
        base.Start();
        workType = WorkType.RESEARCH;
        baseWorkTime = researchTime;
    }

    protected override IEnumerator WorkCoroutine()
    {
        // Check if we have the required resources
        if (!HasRequiredResources())
        {
            Debug.LogWarning("Not enough resources for research");
            yield break;
        }

        // Consume resources
        ConsumeResources();

        // Perform research
        while (workProgress < baseWorkTime)
        {
            workProgress += Time.deltaTime;
            yield return null;
        }

        CompleteWork();
    }

    protected override void CompleteWork()
    {
        // Add research points to the camp's research pool
        CampManager.Instance.ResearchManager.AddResearchPoints(researchPoints);
        Debug.Log($"Research completed! Gained {researchPoints} research points");
        
        base.CompleteWork();
    }
} 