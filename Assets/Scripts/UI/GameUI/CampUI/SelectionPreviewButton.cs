using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Managers;

public class SelectionPreviewButton : PreviewButtonBase<ResearchScriptableObj>
{
    protected override void OnButtonClicked()
    {
        // When clicked, show the research details in the selection popup
        var options = new System.Collections.Generic.List<SelectionPopup.SelectionOption>
        {
            new SelectionPopup.SelectionOption
            {
                optionName = "Start Research",
                onSelected = () => StartResearch(),
                canSelect = () => CampManager.Instance.ResearchManager.CanStartResearch(data)
            }
        };

        PlayerUIManager.Instance.selectionPopup.Setup(options, gameObject);
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
