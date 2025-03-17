using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GeneticMutationUI : PreviewListMenuBase<GeneticMutation, GeneticMutationObj>, IControllerInput
{
    [Header("Mutation Inventory UI")]
    [SerializeField] private GeneticMutationGrid mutationGrid;

    [Header("Mutation Data")]
    public GeneticMutationObj[] allMutations;

    [Header("Selected Mutation")]
    private GeneticMutationObj selectedMutation;
    private MutationUIElement selectedMutationElement;
    private Vector2Int selectedPosition;
    private bool isPlacingMutation = false;

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
            case PlayerControlType.GENETIC_MUTATION_MOVEMENT:
                PlayerInput.Instance.OnLeftJoystick += MoveMutation;
                PlayerInput.Instance.OnAPressed += PlaceMutation;
                PlayerInput.Instance.OnXPressed += RotateMutation;
                break;
            default:
                break;
        }
    }

    public override void SetScreenActive(bool active, float delay = 0.0f, Action onDone = null)
    {
        PlayerUIManager.Instance.SetScreenActive(this, active, delay, onDone);
    }

    public override IEnumerable<GeneticMutationObj> GetItems()
    {
        return allMutations; // Return all available mutations
    }

    public override GeneticMutation GetItemCategory(GeneticMutationObj mutation)
    {
        return GeneticMutation.NONE; // Placeholder if categories are added later
    }

    public override void SetupItemButton(GeneticMutationObj mutation, GameObject button)
    {
        var buttonComponent = button.GetComponent<GeneticMutationBtn>();
        buttonComponent.SetupButton(mutation);
    }

    public override string GetPreviewName(GeneticMutationObj mutation)
    {
        return mutation.objectName;
    }

    public override Sprite GetPreviewSprite(GeneticMutationObj mutation)
    {
        return mutation.sprite;
    }

    public override string GetPreviewDescription(GeneticMutationObj mutation)
    {
        return mutation.description;
    }

    public override IEnumerable<(string resourceName, int requiredCount, int playerCount)> GetPreviewResourceCosts(GeneticMutationObj item)
    {
        throw new NotImplementedException();
    }

    public override void UpdatePreviewSpecifics(GeneticMutationObj item)
    {
        throw new NotImplementedException();
    }

    public override void DestroyPreviewSpecifics()
    {
        throw new NotImplementedException();
    }

    public void SelectMutation(GeneticMutationObj mutation)
    {
        selectedMutation = mutation;
        isPlacingMutation = true;
        selectedPosition = new Vector2Int(0, 0);

        // Create mutation preview in grid
        GameObject newSlot = Instantiate(mutationGrid.mutationSlotPrefab, mutationGrid.transform);
        selectedMutationElement = newSlot.GetComponent<MutationUIElement>();
        selectedMutationElement.Initialize(mutation, mutationGrid);
        selectedMutationElement.SetGridPosition(selectedPosition);
    }

    private void MoveMutation(Vector2 direction)
    {
        if (!isPlacingMutation || selectedMutationElement == null) return;

        Vector2Int newPosition = selectedPosition + new Vector2Int((int)direction.x, (int)direction.y);
        newPosition = mutationGrid.ClampToGrid(newPosition);

        if (mutationGrid.CanPlaceMutation(newPosition, selectedMutation.size))
        {
            selectedPosition = newPosition;
            selectedMutationElement.SetGridPosition(selectedPosition);
        }
    }

    private void RotateMutation()
    {
        if (!isPlacingMutation || selectedMutationElement == null) return;
        selectedMutationElement.Rotate();
    }

    private void PlaceMutation()
    {
        if (!isPlacingMutation || selectedMutationElement == null) return;

        if (mutationGrid.CanPlaceMutation(selectedPosition, selectedMutation.size))
        {
            mutationGrid.PlaceMutation(selectedMutationElement, selectedPosition, selectedMutation.size);
            isPlacingMutation = false;
            selectedMutationElement = null;
        }
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
}
