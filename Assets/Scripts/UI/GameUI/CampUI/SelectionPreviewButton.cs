using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Managers;

public class SelectionPreviewButton : PreviewButtonBase<ResearchScriptableObj>
{
    private Building building;

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
        if (building != null)
        {
            var researchTask = building.GetComponent<ResearchTask>();
            if (researchTask != null)
            {
                researchTask.SetResearch(data);
                // Assign the task to an NPC through the WorkManager
                CampManager.Instance.WorkManager.AssignWorkToBuilding(researchTask);
                PlayerUIManager.Instance.selectionPreviewList.SetScreenActive(false);
                PlayerController.Instance.SetPlayerControlType(PlayerControlType.CAMP_CAMERA_MOVEMENT);
            }
        }
    }

    public void SetupButton(ResearchScriptableObj research, Building building)
    {
        this.building = building;
        base.SetupButton(research, research.sprite, research.objectName);
    }
}
