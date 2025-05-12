using UnityEngine;
using System.Collections;
using Managers;

public class ResearchTask : WorkTask
{
    [SerializeField] private ResearchScriptableObj currentResearch;
    public ResearchScriptableObj CurrentResearch => currentResearch;

    protected override void Start()
    {
        base.Start();
    }

    public void SetResearch(ResearchScriptableObj research)
    {
        if (research == null) return;

        // Check if research can be started
        if (!CampManager.Instance.ResearchManager.CanStartResearch(research, out string errorMessage))
        {
            PlayerUIManager.Instance.DisplayUIErrorMessage(errorMessage);
            return;
        }

        // Try to start the research
        if (!CampManager.Instance.ResearchManager.StartResearch(research))
        {
            return;
        }

        currentResearch = research;
        baseWorkTime = research.researchTime;
        requiredResources = research.requiredResources;
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