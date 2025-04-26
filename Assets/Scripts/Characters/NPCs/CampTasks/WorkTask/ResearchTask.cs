using UnityEngine;
using System.Collections;
using Managers;

public class ResearchTask : WorkTask
{
    [SerializeField] private ResearchScriptableObj currentResearch;

    protected override void Start()
    {
        base.Start();
        workType = WorkType.RESEARCH;
    }

    public void SetResearch(ResearchScriptableObj research)
    {
        currentResearch = research;
        if (research != null)
        {
            baseWorkTime = research.researchTime;
            // Convert required resources from ResearchScriptableObj to ResourceItemCount[]
            if (research.requiredResources != null)
            {
                requiredResources = new ResourceItemCount[research.requiredResources.Length];
                for (int i = 0; i < research.requiredResources.Length; i++)
                {
                    requiredResources[i] = new ResourceItemCount
                    {
                        resource = research.requiredResources[i],
                        count = 1
                    };
                }
            }
        }
    }

    protected override void CompleteWork()
    {
        if (currentResearch != null)
        {
            // Complete the research and unlock associated items
            CampManager.Instance.ResearchManager.CompleteResearch(currentResearch);
            
            // Apply any immediate research benefits
            if (currentResearch.outputResources != null)
            {
                for (int i = 0; i < currentResearch.outputResources.Length; i++)
                {
                    if (i < currentResearch.outputAmounts.Length)
                    {
                        // Add output resources to player inventory
                        PlayerInventory.Instance.AddItem(
                            currentResearch.outputResources[i], 
                            (int)currentResearch.outputAmounts[i]
                        );
                    }
                }
            }
        }
        
        base.CompleteWork();
    }
} 