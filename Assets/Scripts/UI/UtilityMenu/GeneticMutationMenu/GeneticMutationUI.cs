using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GeneticMutationUI : PreviewListMenuBase<GeneticMutation, GeneticMutationData>, IControllerInput
{
    [Header("Mutation Inventory UI")]
    [SerializeField] private GeneticMutationGrid mutationGrid;
    [SerializeField] private GameObject previewContaminationStatus;
    [SerializeField] private TextMeshProUGUI contaminationText;

    [Header("Mutation Data")]
    public GeneticMutationData[] allMutations;

    public override void Setup()
    {
        PlayerInput.Instance.OnUpdatePlayerControls += SetPlayerControlType;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.IN_MENU);
    }

    public override void OnDisable()
    {
        base.OnDisable();
    }

    public void OnDestroy()
    {
        if (PlayerInput.Instance != null)
            PlayerInput.Instance.OnUpdatePlayerControls -= SetPlayerControlType;
    }

    public void SetPlayerControlType(PlayerControlType controlType)
    {
        if (PlayerUIManager.Instance.currentMenu != this)
            return;

        switch (controlType)
        {
            case PlayerControlType.IN_MENU:
                PlayerInput.Instance.OnRBPressed += rightScreenBtn.onClick.Invoke;
                PlayerInput.Instance.OnLBPressed += leftScreenBtn.onClick.Invoke;
                PlayerInput.Instance.OnBPressed += () => PlayerUIManager.Instance.utilityMenu.EnableUtilityMenu();
                break;
            default:
                break;
        }
    }

    public override void SetScreenActive(bool active, float delay = 0.0f, Action onDone = null)
    {
        PlayerUIManager.Instance.SetScreenActive(this, active, delay, onDone);
    }

    public override IEnumerable<GeneticMutationData> GetItems()
    {
        return allMutations; // Return all available mutations
    }

    public override GeneticMutation GetItemCategory(GeneticMutationData mutation)
    {
        return GeneticMutation.NONE; // Placeholder if categories are added later
    }

    public override void SetupItemButton(GeneticMutationData mutation, GameObject button)
    {
        var buttonComponent = button.GetComponent<GeneticMutationBtn>();
        buttonComponent.SetupButton(mutation);
    }

    public override string GetPreviewName(GeneticMutationData mutation)
    {
        return mutation.mutationName;
    }

    public override Sprite GetPreviewSprite(GeneticMutationData mutation)
    {
        return mutation.mutationSprite;
    }

    public override string GetPreviewDescription(GeneticMutationData mutation)
    {
        return mutation.isContaminated ? "Contaminated Mutation - Requires Purification!" : "Genetic Enhancement Ready for Use.";
    }

    public override void UpdatePreviewSpecifics(GeneticMutationData mutation)
    {
        previewContaminationStatus.SetActive(mutation.isContaminated);
        contaminationText.text = mutation.isContaminated ? "Contaminated" : "Clean";
    }

    public override void DestroyPreviewSpecifics()
    {
        previewContaminationStatus.SetActive(false);
    }

    public void UpdateInventory()
    {
        mutationGrid.ClearGrid();

        foreach (var mutation in GeneticMutationSystem.Instance.activeMutations)
        {
            if (mutation == null) continue;
            mutationGrid.AddMutation(mutation);
        }
    }

    public override IEnumerable<(string resourceName, int requiredCount, int playerCount)> GetPreviewResourceCosts(GeneticMutationData item)
    {
        throw new NotImplementedException();
    }
}
