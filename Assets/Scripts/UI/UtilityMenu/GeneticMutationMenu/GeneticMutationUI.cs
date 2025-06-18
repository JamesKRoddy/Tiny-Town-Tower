using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;

[System.Serializable]
public class MutationQuantityEntry
{
    public GeneticMutationObj mutation;
    public int quantity;
}

public class GeneticMutationUI : PreviewListMenuBase<GeneticMutation, GeneticMutationObj>, IControllerInput
{
    [Header("Mutation Inventory UI")]
    [SerializeField] public GeneticMutationGrid mutationGrid;
    [SerializeField] private Transform mutationGridPrefabContainer;

    [Header("Selected Mutation")]
    private GeneticMutationObj selectedMutation;
    private MutationUIElement selectedMutationElement;
    private Vector2Int selectedPosition;
    private bool isPlacingMutation = false;
    private bool isMovingExistingMutation = false;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 0.2f; // Time between movements in seconds
    [SerializeField] private float inputThreshold = 0.5f; // Threshold for directional input
    private float lastMoveTime;
    private Vector2 lastInputDirection;
    private float warningLockEndTime = 0f;

    private void OnEnable()
    {
        // Reset placement state when menu is opened
        isPlacingMutation = false;
        isMovingExistingMutation = false;
        selectedMutation = null;
        selectedMutationElement = null;
    }

    public override void SetPlayerControls(PlayerControlType controlType)
    {
        base.SetPlayerControls(controlType);

        switch (controlType)
        {
            case PlayerControlType.IN_MENU:
                // Enable all buttons in the current screen
                if (screens.ContainsKey(currentCategory) && screens[currentCategory] != null)
                {
                    foreach (Button button in screens[currentCategory].GetComponentsInChildren<Button>())
                    {
                        button.interactable = true;
                    }
                }
                PlayerInput.Instance.OnBPressed += selectionPopup.OnCloseClicked;
                break;
            case PlayerControlType.GENETIC_MUTATION_MOVEMENT:
                PlayerInput.Instance.OnLeftJoystick += MoveMutation;
                PlayerInput.Instance.OnAPressed += PlaceMutation;
                PlayerInput.Instance.OnXPressed += RotateMutation;
                PlayerInput.Instance.OnBPressed += CancelPlacement;
                PlayerInput.Instance.OnLBPressed += RotateSelectedMutationLeft;
                PlayerInput.Instance.OnRBPressed += RotateSelectedMutationRight;
                // Disable all buttons in the current screen
                if (screens.ContainsKey(currentCategory) && screens[currentCategory] != null)
                {
                    foreach (Button button in screens[currentCategory].GetComponentsInChildren<Button>())
                    {
                        button.interactable = false;
                    }
                }
                break;
            default:
                break;
        }
    }

    private void CancelPlacement()
    {
        if (isPlacingMutation && selectedMutationElement != null)
        {
            // If we're moving an existing mutation, put it back in the grid
            if (isMovingExistingMutation)
            {
                // Find a suitable position
                for (int x = 0; x < mutationGrid.GetGridWidth(); x++)
                {
                    for (int y = 0; y < mutationGrid.GetGridHeight(); y++)
                    {
                        Vector2Int pos = new Vector2Int(x, y);
                        if (mutationGrid.CanPlaceMutation(pos, selectedMutationElement))
                        {
                            mutationGrid.PlaceMutation(selectedMutationElement, pos, selectedMutationElement.Size);
                            break;
                        }
                    }
                }
            }
            else
            {
                // If we're placing a new mutation, destroy it
                Destroy(selectedMutationElement.gameObject);
            }

            // Reset state
            selectedMutation = null;
            selectedMutationElement = null;
            isPlacingMutation = false;
            isMovingExistingMutation = false;

            // Switch back to menu controls
            PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.IN_MENU);
        }
    }

    public override IEnumerable<GeneticMutationObj> GetItems()
    {
        // Only return mutations that have a quantity greater than 0
        foreach (var entry in PlayerInventory.Instance.availableMutations)
        {
            if (entry.mutation != null && entry.quantity > 0)
            {
                yield return entry.mutation;
            }
        }
    }

    public override GeneticMutation GetItemCategory(GeneticMutationObj mutation)
    {
        return GeneticMutation.NONE; // Placeholder if categories are added later
    }

    public override void SetupItemButton(GeneticMutationObj mutation, GameObject button)
    {
        var buttonComponent = button.GetComponent<GeneticMutationBtn>();
        int quantity = 0;
        foreach (var entry in PlayerInventory.Instance.availableMutations)
        {
            if (entry.mutation == mutation)
            {
                quantity = entry.quantity;
                break;
            }
        }
        buttonComponent.SetupButton(mutation, quantity);
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
        string baseDescription = mutation.description;
        
        // Get stats description from the mutation effect
        string statsDescription = GetMutationStatsDescription(mutation);
        
        // Combine base description with stats
        if (!string.IsNullOrEmpty(statsDescription))
        {
            return $"{baseDescription}\n\n{statsDescription}";
        }
        
        return baseDescription;
    }

    private string GetMutationStatsDescription(GeneticMutationObj mutation)
    {
        if (mutation.prefab == null)
        {
            Debug.LogWarning($"Mutation {mutation.name} has no prefab assigned!");
            return string.Empty;
        }

        // Get the BaseMutationEffect component from the prefab
        BaseMutationEffect mutationEffect = mutation.prefab.GetComponent<BaseMutationEffect>();
        if (mutationEffect == null)
        {
            Debug.LogWarning($"Mutation prefab {mutation.prefab.name} has no BaseMutationEffect component!");
            return string.Empty;
        }

        return mutationEffect.GetStatsDescription();
    }

    public override IEnumerable<(string resourceName, int requiredCount, int playerCount)> GetPreviewResourceCosts(GeneticMutationObj item)
    {
        return null;
    }

    public override void UpdatePreviewSpecifics(GeneticMutationObj item)
    {
        //TODO  Implement with something I think, not sure
    }

    public override void DestroyPreviewSpecifics()
    {
        //TODO  Implement with something I think, not sure
    }

    /// <summary>
    /// Selects a mutation from the grid to move
    /// </summary>
    /// <param name="uiElement"></param>
    public void SelectMutation(MutationUIElement uiElement)
    {
        selectedMutation = uiElement.mutation;
        isPlacingMutation = true;
        isMovingExistingMutation = true;
        selectedPosition = new Vector2Int(0, 0);
        selectedMutationElement = uiElement;
        mutationGrid.ClearPosition(selectedMutationElement);
        if (selectedMutationElement == null)
        {
            Debug.LogError($"Could not find existing UI element for mutation: {uiElement.mutation.objectName}");
            return;
        }
        
        // Make sure it's in the correct container
        selectedMutationElement.transform.SetParent(mutationGridPrefabContainer, false);
        
        selectedMutationElement.SetSelected(true); // Highlight selected mutation
        selectedMutationElement.SetGridPosition(selectedPosition, mutationGrid.GetCellSize());
    }

    /// <summary>
    /// Selects a mutation from the inventory and initializes it in the grid
    /// </summary>
    /// <param name="mutation"></param>
    public void SelectMutation(GeneticMutationObj mutation)
    {
        selectedMutation = mutation;
        isPlacingMutation = true;
        isMovingExistingMutation = false;
        selectedPosition = new Vector2Int(0, 0);

        // Create new UI element for new mutation
        GameObject newSlot = Instantiate(mutationGrid.mutationSlotPrefab, mutationGridPrefabContainer);
        selectedMutationElement = newSlot.GetComponent<MutationUIElement>();
        if (selectedMutationElement == null)
        {
            selectedMutationElement = newSlot.AddComponent<MutationUIElement>();
        }

        if (selectedMutationElement == null)
        {
            Debug.LogError($"Failed to add MutationUIElement component to new mutation slot!");
            Destroy(newSlot);
            return;
        }

        selectedMutationElement.Initialize(mutation, mutationGrid);
        selectedMutationElement.SetSelected(true); // Highlight selected mutation
        selectedMutationElement.SetGridPosition(selectedPosition, mutationGrid.GetCellSize());
    }

    private void ClampSelectedPositionToGrid()
    {
        if (selectedMutationElement == null) return;
        int gridWidth = mutationGrid.GetGridWidth();
        int gridHeight = mutationGrid.GetGridHeight();

        // Use the new clamping method
        selectedPosition = selectedMutationElement.GetClampedPosition(selectedPosition, gridWidth, gridHeight);
    }

    private void MoveMutation(Vector2 direction)
    {
        if (!isPlacingMutation || selectedMutationElement == null) return;
        if (Time.time < warningLockEndTime) return;
        lastInputDirection = direction;
        if (Time.time - lastMoveTime < moveSpeed) return;
        if (Mathf.Abs(direction.x) < inputThreshold && Mathf.Abs(direction.y) < inputThreshold) return;

        Vector2Int newPosition = selectedPosition;
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            newPosition.x += direction.x > 0 ? 1 : -1;
        else
            newPosition.y += direction.y > 0 ? 1 : -1;

        // Check if the new position is valid before applying it
        if (selectedMutationElement.IsPositionValid(newPosition, mutationGrid.GetGridWidth(), mutationGrid.GetGridHeight()))
        {
            selectedPosition = newPosition;
            lastMoveTime = Time.time;
            selectedMutationElement.SetGridPosition(selectedPosition, mutationGrid.GetCellSize());
        }
        else
        {
            // Show warning if we can't move in that direction
            selectedMutationElement.ShowWarning();
            warningLockEndTime = Time.time + 0.5f;
            StartCoroutine(HideWarningAfterDelay());
        }
    }

    private void RotateMutation()
    {
        if (!isPlacingMutation || selectedMutationElement == null) return;
        
        // Rotate the mutation element clockwise
        selectedMutationElement.RotateRight();
        
        // Debug log to verify rotation
        Debug.Log("Rotated mutation clockwise");
    }

    private void PlaceMutation()
    {
        if (!isPlacingMutation || selectedMutationElement == null) return;

        Debug.Log($"[PlaceMutation] Trying to place at {selectedPosition} with size {selectedMutationElement.Size} on grid {mutationGrid.GetGridWidth()}x{mutationGrid.GetGridHeight()}");
        if (mutationGrid.CanPlaceMutation(selectedPosition, selectedMutationElement))
        {
            selectedMutationElement.SetSelected(false);
            mutationGrid.PlaceMutation(selectedMutationElement, selectedPosition, selectedMutationElement.Size);
            isPlacingMutation = false;
            selectedMutationElement = null;
            Debug.Log("[PlaceMutation] Placed mutation successfully.");

            if (!isMovingExistingMutation)
            {
                // Remove mutation from available mutations and add it to player inventory
                PlayerInventory.Instance.EquipMutation(selectedMutation);

                // Remove from available mutations
                foreach (var entry in PlayerInventory.Instance.availableMutations)
                {
                    if (entry.mutation == selectedMutation)
                    {
                        entry.quantity--;
                        if (entry.quantity <= 0)
                        {
                            PlayerInventory.Instance.availableMutations.Remove(entry);
                        }
                        break;
                    }
                }

                // Clear existing screens and rebuild UI
                foreach (var screen in screens.Values)
                {
                    if (screen != null)
                    {
                        Destroy(screen);
                    }
                }
                screens.Clear();
                SetupScreens();
            }

            // Switch control types with a frame delay
            PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.IN_MENU);
        }
        else
        {
            Debug.LogWarning($"[PlaceMutation] Cannot place mutation at {selectedPosition} with size {selectedMutationElement.Size}");
            // Show warning and lock movement
            selectedMutationElement.ShowWarning();
            warningLockEndTime = Time.time + 0.5f;

            // Show error message using PlayerUIManager
            PlayerUIManager.Instance.buildMenu.DisplayErrorMessage("Cannot place mutation here!");

            // Hide warning after lock period
            StartCoroutine(HideWarningAfterDelay());
        }
    }

    private IEnumerator HideWarningAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        if (selectedMutationElement != null)
        {
            selectedMutationElement.HideWarning();
        }
    }

    public void UpdateInventory()
    {
        mutationGrid.ClearGrid();

        // Get mutations from PlayerInventory (equipped mutations)
        foreach (var mutation in PlayerInventory.Instance.EquippedMutations)
        {
            if (mutation == null) continue;
            mutationGrid.AddMutation(mutation);
        }
    }

    public void AddMutationBackToQuantities(GeneticMutationObj mutation)
    {
        PlayerInventory.Instance.AddAvalibleMutation(mutation);
        RefreshUIAndSelectFirst();
    }

    public void OnMutationClicked(GeneticMutationObj mutation, MutationUIElement uiElement)
    {
        if (selectionPopup != null)
        {
            selectionPopup.DisplayPopup(mutation, this, uiElement.gameObject);
        }
    }

    public void RotateSelectedMutationLeft()
    {
        if (isPlacingMutation && selectedMutationElement != null)
        {
            selectedMutationElement.RotateLeft();
            
            // Check if the current position is still valid after rotation
            if (!selectedMutationElement.IsPositionValid(selectedPosition, mutationGrid.GetGridWidth(), mutationGrid.GetGridHeight()))
            {
                // If not valid, try to find a valid position
                bool foundValidPosition = false;
                for (int x = 0; x < mutationGrid.GetGridWidth(); x++)
                {
                    for (int y = 0; y < mutationGrid.GetGridHeight(); y++)
                    {
                        Vector2Int testPos = new Vector2Int(x, y);
                        if (selectedMutationElement.IsPositionValid(testPos, mutationGrid.GetGridWidth(), mutationGrid.GetGridHeight()))
                        {
                            selectedPosition = testPos;
                            foundValidPosition = true;
                            break;
                        }
                    }
                    if (foundValidPosition) break;
                }

                // If no valid position found, show warning
                if (!foundValidPosition)
                {
                    selectedMutationElement.ShowWarning();
                    warningLockEndTime = Time.time + 0.5f;
                    StartCoroutine(HideWarningAfterDelay());
                }
            }

            selectedMutationElement.SetGridPosition(selectedPosition, mutationGrid.GetCellSize());
        }
    }

    public void RotateSelectedMutationRight()
    {
        if (isPlacingMutation && selectedMutationElement != null)
        {
            selectedMutationElement.RotateRight();
            
            // Check if the current position is still valid after rotation
            if (!selectedMutationElement.IsPositionValid(selectedPosition, mutationGrid.GetGridWidth(), mutationGrid.GetGridHeight()))
            {
                // If not valid, try to find a valid position
                bool foundValidPosition = false;
                for (int x = 0; x < mutationGrid.GetGridWidth(); x++)
                {
                    for (int y = 0; y < mutationGrid.GetGridHeight(); y++)
                    {
                        Vector2Int testPos = new Vector2Int(x, y);
                        if (selectedMutationElement.IsPositionValid(testPos, mutationGrid.GetGridWidth(), mutationGrid.GetGridHeight()))
                        {
                            selectedPosition = testPos;
                            foundValidPosition = true;
                            break;
                        }
                    }
                    if (foundValidPosition) break;
                }

                // If no valid position found, show warning
                if (!foundValidPosition)
                {
                    selectedMutationElement.ShowWarning();
                    warningLockEndTime = Time.time + 0.5f;
                    StartCoroutine(HideWarningAfterDelay());
                }
            }

            selectedMutationElement.SetGridPosition(selectedPosition, mutationGrid.GetCellSize());
        }
    }
}
