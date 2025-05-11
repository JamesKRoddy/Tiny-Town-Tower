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

    public override void Setup(HumanCharacterController npc, PreviewListMenuBase<string, HumanCharacterController> menu, GameObject element)
    {
        base.Setup(npc, menu, element);
    }

    protected override void SetupInitialSelection()
    {
        if (possessButton != null)
        {
            EventSystem.current.SetSelectedGameObject(possessButton.gameObject);
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
            PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.CAMP_WORK_ASSIGNMENT);
            // Store the NPC we're assigning work to
            CampManager.Instance.WorkManager.SetNPCForAssignment(currentItem as SettlerNPC);
        }
        OnCloseClicked();
    }

    private void OnRemoveWorkClicked()
    {
        if (currentItem != null)
        {
            // TODO: Remove current work assignment
            Debug.Log($"Removing work from {(currentItem as SettlerNPC).nPCDataObj.nPCName}");
        }
        OnCloseClicked();
    }
} 