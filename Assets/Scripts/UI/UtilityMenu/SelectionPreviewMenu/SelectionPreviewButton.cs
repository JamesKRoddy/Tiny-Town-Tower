using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Managers;
using CampBuilding;
using System;

public class SelectionPreviewButton : PreviewButtonBase<ScriptableObject>
{
    private WorkTask workTask;
    [SerializeField] private TMP_Text countText;
    private HumanCharacterController characterToAssign;

    public void SetupButton(ScriptableObject item, WorkTask workTask, HumanCharacterController characterToAssign = null)
    {        
        this.workTask = workTask;
        this.characterToAssign = characterToAssign;
        string name = string.Empty;
        Sprite sprite = null;

        // Unsubscribe from previous task if it exists
        if (workTask != null)
        {
            workTask.OnTaskCompleted -= OnTaskCompleted;
        }

        // Setup based on task type
        switch (workTask)
        {
            case ResearchTask researchTask:
                var research = item as ResearchScriptableObj;
                if (research != null)
                {
                    name = research.objectName;
                    sprite = research.sprite;
                }
                countText.text = "";
                break;

            case CookingTask cookingTask:
                var recipe = item as CookingRecipeScriptableObj;
                if (recipe != null)
                {
                    name = recipe.objectName;
                    sprite = recipe.sprite;
                    cookingTask.OnTaskCompleted += OnTaskCompleted;
                }
                break;

            case ResourceUpgradeTask upgradeTask:
                var upgrade = item as ResourceUpgradeScriptableObj;
                if (upgrade != null)
                {
                    name = upgrade.objectName;
                    sprite = upgrade.sprite;
                    upgradeTask.OnTaskCompleted += OnTaskCompleted;
                }
                break;
        }

        base.SetupButton(item, sprite, name);
        UpdateCount();
    }
    
    protected override void OnButtonClicked()
    {
        CampManager.Instance.WorkManager.SetNPCForAssignment(characterToAssign);

        if (workTask == null)
        {
            Debug.LogWarning("[SelectionPreviewButton] No work task assigned");
            return;
        }

        // Handle task assignment
        if (characterToAssign != null && !workTask.IsOccupied)
        {
            CampManager.Instance.WorkManager.AssignWorkToBuilding(workTask);
        }

        // Set task data based on type
        switch (workTask)
        {
            case ResearchTask researchTask:
                var research = data as ResearchScriptableObj;
                if (research != null)
                {
                    if (!CampManager.Instance.ResearchManager.CanStartResearch(research, out string errorMessage))
                    {
                        PlayerUIManager.Instance.DisplayUIErrorMessage(errorMessage);
                        return;
                    }
                    researchTask.SetResearch(research);
                }
                break;

            case CookingTask cookingTask:
                var recipe = data as CookingRecipeScriptableObj;
                if (recipe != null)
                {
                    cookingTask.SetRecipe(recipe);
                }
                break;

            case ResourceUpgradeTask upgradeTask:
                var upgrade = data as ResourceUpgradeScriptableObj;
                if (upgrade != null)
                {
                    upgradeTask.SetUpgrade(upgrade);
                }
                break;

            case FarmingTask farmingTask:
                var seed = data as ResourceScriptableObj;
                if (seed != null)
                {
                    if (PlayerInventory.Instance.GetItemCount(seed) < 1)
                    {
                        PlayerUIManager.Instance.DisplayUIErrorMessage($"Not enough {seed.objectName} seeds");
                        return;
                    }
                    farmingTask.requiredResources = new ResourceItemCount[] { new ResourceItemCount(seed, 1) };
                    PlayerUIManager.Instance.selectionPreviewList.ReturnToGame();
                }
                break;
        }
        
        // Update preview without closing menu
        PlayerUIManager.Instance.selectionPreviewList.UpdatePreview(data);
        UpdateCount();
    }

    private void UpdateCount()
    {
        if (countText == null) return;

        int count = 0;
        switch (workTask)
        {
            case CookingTask cookingTask:
                var recipe = data as CookingRecipeScriptableObj;
                if (recipe != null)
                {
                    if (cookingTask.currentRecipe == recipe) count++;
                    foreach (var queuedRecipe in cookingTask.taskQueue)
                    {
                        if (queuedRecipe is CookingRecipeScriptableObj r && r == recipe) count++;
                    }
                }
                break;

            case ResourceUpgradeTask upgradeTask:
                var upgrade = data as ResourceUpgradeScriptableObj;
                if (upgrade != null)
                {
                    if (upgradeTask.currentUpgrade == upgrade) count++;
                    foreach (var queuedUpgrade in upgradeTask.taskQueue)
                    {
                        if (queuedUpgrade is ResourceUpgradeScriptableObj u && u == upgrade) count++;
                    }
                }
                break;

            case FarmingTask farmingTask:
                var seed = data as ResourceScriptableObj;
                if (seed != null)
                {
                    count = PlayerInventory.Instance.GetItemCount(seed);
                }
                break;
        }

        countText.text = count > 0 ? count.ToString() : "";
        countText.gameObject.SetActive(count > 0);
    }

    private void OnTaskCompleted()
    {
        UpdateCount();
    }

    private void OnDestroy()
    {
        if (workTask != null)
        {
            workTask.OnTaskCompleted -= OnTaskCompleted;
        }
    }
}
