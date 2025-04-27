using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Managers;

public class SelectionPreviewButton : PreviewButtonBase<ResearchScriptableObj>
{
    protected override void OnButtonClicked()
    {
        // Check if research can be started
        if (!CampManager.Instance.ResearchManager.CanStartResearch(data, out string errorMessage))
        {
            PlayerUIManager.Instance.DisplayUIErrorMessage(errorMessage);
            return;
        }

        // Start the research directly
        StartResearch();
    }

    private void StartResearch()
    {
        // Find the research task in the building and set the research
        var building = GetComponentInParent<Building>();
        if (building != null)
        {
            var researchTask = building.GetComponent<ResearchTask>();
            if (researchTask != null)
            {
                researchTask.SetResearch(data);
                PlayerUIManager.Instance.BackPressed(); // Return to previous menu
            }
        }
    }

    public void SetupButton(ResearchScriptableObj research)
    {
        base.SetupButton(research, research.sprite, research.objectName);
    }
}
