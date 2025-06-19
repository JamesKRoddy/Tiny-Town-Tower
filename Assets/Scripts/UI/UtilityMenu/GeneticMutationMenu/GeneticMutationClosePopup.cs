using System;
using UnityEngine;
using UnityEngine.UI;

public class GeneticMutationClosePopup : PreviewPopupBase<GeneticMutationObj, GeneticMutation>
{

    [SerializeField] private Button exitMenuButton;

    protected override void Start()
    {
        base.Start();
        closeButton.onClick.AddListener(OnCloseClicked);
        exitMenuButton.onClick.AddListener(ReturnToMenu);
    }

    public void ReturnToMenu(){
        PlayerInventory.Instance.ClearAvailableMutations();
        PlayerUIManager.Instance.utilityMenu.EnableUtilityMenu();
        OnCloseClicked();
    }
}
