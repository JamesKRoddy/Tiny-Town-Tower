using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class SettlerNPCPopup : PreviewPopupBase<SettlerNPC, string>
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

    public override void Setup(SettlerNPC npc, PreviewListMenuBase<string, SettlerNPC> menu, GameObject element)
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
            PlayerUIManager.Instance.utilityMenu.ReturnToGame(PlayerControlType.CAMP_NPC_MOVEMENT);
        }
    }

    private void OnAssignWorkClicked()
    {
        // TODO: Implement work assignment
    }

    private void OnRemoveWorkClicked()
    {
        if (currentItem != null)
        {
            // TODO: Remove current work assignment
            Debug.Log($"Removing work from {currentItem.nPCDataObj.nPCName}");
        }
    }
} 