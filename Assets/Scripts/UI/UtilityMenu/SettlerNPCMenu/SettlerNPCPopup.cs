using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Managers;

public class SettlerNPCPopup : PreviewPopupBase<HumanCharacterController, string>
{
    [Header("Work Assignment UI")]
    [SerializeField] private Button possessButton;
    [SerializeField] private Button assignWorkButton;
    [SerializeField] private Button removeWorkButton;

    protected override void Start()
    {
        base.Start();
        
        if (possessButton != null)
            possessButton.onClick.AddListener(OnPossessClicked);
        if (assignWorkButton != null)
            assignWorkButton.onClick.AddListener(OnAssignWorkClicked);
        if (removeWorkButton != null)
            removeWorkButton.onClick.AddListener(OnRemoveWorkClicked);
    }

    public override void DisplayPopup(HumanCharacterController npc, PreviewListMenuBase<string, HumanCharacterController> menu, GameObject element)
    {
        base.DisplayPopup(npc, menu, element);
    }

    protected override void SetupInitialSelection()
    {
        if (possessButton != null)
        {
            PlayerUIManager.Instance.SetSelectedGameObject(possessButton.gameObject);
        }
    }

    private void OnPossessClicked()
    {
        if (currentItem != null)
        {
            PlayerController.Instance.PossessNPC(currentItem);
            PlayerUIManager.Instance.settlerNPCMenu.SetScreenActive(false, 0.05f);
            PlayerUIManager.Instance.utilityMenu.ReturnToGame();
        }
        OnCloseClicked();
    }

    private void OnAssignWorkClicked()
    {
        if (currentItem != null && currentItem is SettlerNPC)
        {
            PlayerController.Instance.PossessNPC(null);
            PlayerUIManager.Instance.settlerNPCMenu.SetScreenActive(false, 0.05f);
            // If there is no building for assignment, we are assigning work to the NPC. Happens by selecting an npc first then a building.
            if(CampManager.Instance.WorkManager.buildingForAssignment == null){
                PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.CAMP_WORK_ASSIGNMENT);
            }
            // If there is a building for assignment, we are assigning work to the building. Happens by selecting a building first then an npc.
            else{
                CampManager.Instance.WorkManager.ShowWorkTaskOptions(CampManager.Instance.WorkManager.buildingForAssignment, currentItem, (task) => {
                    CampManager.Instance.WorkManager.AssignWorkToBuilding(task);
                });
            }
            // Store the NPC we're assigning work to
            CampManager.Instance.WorkManager.SetNPCForAssignment(currentItem as SettlerNPC);
        }
        OnCloseClicked();
    }

    private void OnRemoveWorkClicked()
    {
        if (currentItem != null && currentItem is SettlerNPC settler)
        {
            settler.StopWork();
        }
        OnCloseClicked();
    }
} 