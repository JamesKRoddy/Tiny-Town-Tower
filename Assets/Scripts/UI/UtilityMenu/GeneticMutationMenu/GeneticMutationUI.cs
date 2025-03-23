using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;

public class GeneticMutationUI : PreviewListMenuBase<GeneticMutation, GeneticMutationObj>, IControllerInput
{
    [Header("Mutation Inventory UI")]
    [SerializeField] public GeneticMutationGrid mutationGrid;
    [SerializeField] private Transform mutationGridPrefabContainer;
    [SerializeField] private GeneticMutationSelectPopup mutationSelectPopup;

    [Header("Mutation Data")]
    [SerializeField] private List<MutationQuantityEntry> mutationQuantities = new List<MutationQuantityEntry>();

    [System.Serializable]
    private class MutationQuantityEntry
    {
        public GeneticMutationObj mutation;
        public int quantity;
    }

    [Header("Selected Mutation")]
    private GeneticMutationObj selectedMutation;
    private MutationUIElement selectedMutationElement;
    private Vector2Int selectedPosition;
    private bool isPlacingMutation = false;
    private GameObject previouslySelectedButton;
    private bool isMovingExistingMutation = false;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 0.2f; // Time between movements in seconds
    private float lastMoveTime;
    private Vector2 lastInputDirection;
    private float warningLockEndTime = 0f;

    public override void Setup()
    {
        PlayerInput.Instance.OnUpdatePlayerControls += SetPlayerControlType;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.IN_MENU);
        UpdateMutationQuantities();
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
                // Enable all buttons in the current screen
                if (screens.ContainsKey(currentCategory) && screens[currentCategory] != null)
                {
                    foreach (Button button in screens[currentCategory].GetComponentsInChildren<Button>())
                    {
                        button.interactable = true;
                    }
                }
                break;
            case PlayerControlType.GENETIC_MUTATION_MOVEMENT:
                PlayerInput.Instance.OnLeftJoystick += MoveMutation;
                PlayerInput.Instance.OnAPressed += PlaceMutation;
                PlayerInput.Instance.OnXPressed += RotateMutation;
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

    public override void SetScreenActive(bool active, float delay = 0.0f, Action onDone = null)
    {
        PlayerUIManager.Instance.SetScreenActive(this, active, delay, onDone);
    }

    private List<MutationQuantityEntry> GetMutationQuantities()
    {
        List<MutationQuantityEntry> quantities = new List<MutationQuantityEntry>();
        foreach (var entry in mutationQuantities)
        {
            if (entry.mutation != null)
            {
                quantities.Add(new MutationQuantityEntry { mutation = entry.mutation, quantity = entry.quantity });
            }
        }
        return quantities;
    }

    private void UpdateMutationQuantities()
    {
        var quantities = GetMutationQuantities();

        // Subtract mutations that are equipped
        foreach (var mutation in PlayerInventory.Instance.EquippedMutations)
        {
            var entry = quantities.Find(e => e.mutation == mutation);
            if (entry != null)
            {
                entry.quantity--;
            }
        }

        // Update the serialized list
        foreach (var entry in mutationQuantities)
        {
            if (entry.mutation != null)
            {
                var matchingEntry = quantities.Find(e => e.mutation == entry.mutation);
                if (matchingEntry != null)
                {
                    entry.quantity = matchingEntry.quantity;
                }
            }
        }
    }

    public override IEnumerable<GeneticMutationObj> GetItems()
    {
        // Only return mutations that have a quantity greater than 0
        foreach (var entry in mutationQuantities)
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
        foreach (var entry in mutationQuantities)
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
        return mutation.description;
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

    public void SelectMutation(MutationUIElement uiElement)
    {
        selectedMutation = uiElement.mutation;
        isPlacingMutation = true;
        isMovingExistingMutation = true;
        selectedPosition = new Vector2Int(0, 0);

        // Find the existing UI element for this mutation
        selectedMutationElement = mutationGrid.GetMutationElement(uiElement.gameObject);
        mutationGrid.ClearPosition(selectedMutationElement);
        if (selectedMutationElement == null)
        {
            Debug.LogError($"Could not find existing UI element for mutation: {uiElement.mutation.objectName}");
            return;
        }
        selectedMutationElement.SetSelected(true); // Highlight selected mutation

        SetupMutationElementPosition(uiElement.mutation);
    }

    public void SelectMutation(GeneticMutationObj mutation)
    {
        selectedMutation = mutation;
        isPlacingMutation = true;
        isMovingExistingMutation = false;
        selectedPosition = new Vector2Int(0, 0);

        if (mutation.prefab == null)
        {
            Debug.LogError($"Mutation {mutation.objectName} is missing a UI prefab!");
            return;
        }

        // Create new UI element for new mutation
        GameObject newSlot = Instantiate(mutation.prefab, mutationGridPrefabContainer.transform);
        selectedMutationElement = newSlot.GetComponent<MutationUIElement>();

        if (selectedMutationElement == null)
        {
            Debug.LogError($"Mutation prefab {mutation.prefab.name} is missing a MutationUIElement component!");
            return;
        }

        selectedMutationElement.Initialize(mutation, mutationGrid);
        SetupMutationElementPosition(mutation);
    }

    private void SetupMutationElementPosition(GeneticMutationObj mutation)
    {
        // Get the cell size once and pass it in
        Vector2 cellSize = mutationGrid.GetCellSize();
        selectedMutationElement.SetGridPosition(selectedPosition, cellSize);

        // Ensure the UI element matches the mutation's intended size and positioning
        RectTransform rectTransform = selectedMutationElement.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // Set the size based on the mutation size and cell size
            rectTransform.sizeDelta = new Vector2(cellSize.x * mutation.size.x, cellSize.y * mutation.size.y);

            // Set anchors to stretch across the required number of cells
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(0, 0);
            rectTransform.pivot = new Vector2(0, 0);

            // Force layout update to ensure proper positioning
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
    }


    private void MoveMutation(Vector2 direction)
    {
        if (!isPlacingMutation || selectedMutationElement == null) return;

        // Check if we're in warning lock period
        if (Time.time < warningLockEndTime)
            return;

        // Store the last input direction
        lastInputDirection = direction;

        // Check if enough time has passed since last movement
        if (Time.time - lastMoveTime < moveSpeed)
            return;

        // Only move if there's significant input (to prevent drift)
        if (Mathf.Abs(direction.x) < 0.5f && Mathf.Abs(direction.y) < 0.5f)
            return;

        // Determine which axis has the stronger input
        bool moveHorizontal = Mathf.Abs(direction.x) > Mathf.Abs(direction.y);
        
        // Calculate new position based on dominant axis
        Vector2Int newPosition = selectedPosition;
        if (moveHorizontal)
        {
            newPosition.x += direction.x > 0 ? 1 : -1;
        }
        else
        {
            newPosition.y += direction.y > 0 ? 1 : -1;
        }

        // Ensure new position is within the grid bounds
        newPosition = mutationGrid.ClampToGrid(newPosition, selectedMutation.size);

        // Only move if the position is different
        if (newPosition != selectedPosition)
        {
            selectedPosition = newPosition;
            lastMoveTime = Time.time;

            // Pass cellSize when updating position
            Vector2 cellSize = mutationGrid.GetCellSize();
            selectedMutationElement.SetGridPosition(selectedPosition, cellSize);

            // Force layout update after position change
            LayoutRebuilder.ForceRebuildLayoutImmediate(selectedMutationElement.GetComponent<RectTransform>());
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
            selectedMutationElement.SetSelected(false);
            mutationGrid.PlaceMutation(selectedMutationElement, selectedPosition, selectedMutation.size);
            isPlacingMutation = false;
            selectedMutationElement = null;

            if (!isMovingExistingMutation)
            {
                // Remove mutation from available mutations and add it to player inventory
                for (int i = mutationQuantities.Count - 1; i >= 0; i--)
                {
                    if (mutationQuantities[i].mutation == selectedMutation)
                    {
                        mutationQuantities[i].quantity--;
                        if (mutationQuantities[i].quantity <= 0)
                        {
                            mutationQuantities.RemoveAt(i);
                        }
                        break;
                    }
                }
                PlayerInventory.Instance.AddMutation(selectedMutation);
                GeneticMutationSystem.Instance.AddMutation(selectedMutation);

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

        // Get mutations from GeneticMutationSystem (equipped mutations)
        foreach (var mutation in GeneticMutationSystem.Instance.activeMutations)
        {
            if (mutation == null) continue;
            mutationGrid.AddMutation(mutation);
        }

        // Update mutation quantities
        UpdateMutationQuantities();
    }

    public void AddMutationBackToQuantities(GeneticMutationObj mutation)
    {
        // Check if mutation already exists in quantities
        foreach (var entry in mutationQuantities)
        {
            if (entry.mutation == mutation)
            {
                entry.quantity++;
                return;
            }
        }

        // If not found, add new entry
        mutationQuantities.Add(new MutationQuantityEntry
        {
            mutation = mutation,
            quantity = 1
        });

        RefreshUIAndSelectFirst();
    }

    public void OnMutationClicked(GeneticMutationObj mutation, MutationUIElement uiElement)
    {
        if (mutationSelectPopup != null)
        {
            mutationSelectPopup.Setup(mutation, this, uiElement);
        }
    }
}
