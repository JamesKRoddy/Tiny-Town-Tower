using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Managers;
using System;

public class SelectionPreviewButton : PreviewButtonBase<ScriptableObject>
{
    private Building building;
    private WorkType workType;
    [SerializeField] private TMP_Text queueCountText;
    private CookingTask cookingTask;

    protected override void OnButtonClicked()
    {
        switch (workType)
        {
            case WorkType.RESEARCH:
                HandleResearch();
                break;
            case WorkType.COOKING:
                HandleCooking();
                break;
            case WorkType.UPGRADE_RESOURCE:
                HandleResourceUpgrade();
                break;
        }
    }

    private void HandleResearch()
    {
        var research = data as ResearchScriptableObj;
        if (research == null) return;

        // Check if research can be started
        if (!CampManager.Instance.ResearchManager.CanStartResearch(research, out string errorMessage))
        {
            PlayerUIManager.Instance.DisplayUIErrorMessage(errorMessage);
            return;
        }

        // Start the research
        if (building != null)
        {
            var researchTask = building.GetComponent<ResearchTask>();
            if (researchTask != null)
            {
                researchTask.SetResearch(research);
                CampManager.Instance.WorkManager.AssignWorkToBuilding(researchTask);
                // Don't close the menu, just update the preview
                PlayerUIManager.Instance.selectionPreviewList.UpdatePreview(data);
            }
        }
    }

    private void HandleCooking()
    {
        var recipe = data as CookingRecipeScriptableObj;
        if (recipe == null)
        {
            Debug.LogWarning("[SelectionPreviewButton] Attempted to handle cooking with null recipe");
            return;
        }

        if (building == null)
        {
            Debug.LogWarning("[SelectionPreviewButton] Attempted to handle cooking with null building");
            return;
        }

        var cookingTask = building.GetComponent<CookingTask>();
        if (cookingTask == null)
        {
            Debug.LogWarning($"[SelectionPreviewButton] No CookingTask found on building {building.name}");
            return;
        }
        
        // Only assign work if this is the first recipe
        if (cookingTask.currentRecipe == null)
        {
            CampManager.Instance.WorkManager.AssignWorkToBuilding(cookingTask);
        }

        cookingTask.SetRecipe(recipe);
        
        // Don't close the menu, just update the preview
        PlayerUIManager.Instance.selectionPreviewList.UpdatePreview(data);
        UpdateQueueCount();
    }

    private void HandleResourceUpgrade()
    {
        var upgrade = data as ResourceUpgradeScriptableObj;
        if (upgrade == null) return;

        if (building != null)
        {
            var upgradeTask = building.GetComponent<ResourceUpgradeTask>();
            if (upgradeTask != null)
            {
                upgradeTask.SetUpgrade(upgrade);
                CampManager.Instance.WorkManager.AssignWorkToBuilding(upgradeTask);
                // Don't close the menu, just update the preview
                PlayerUIManager.Instance.selectionPreviewList.UpdatePreview(data);
            }
        }
    }

    private void UpdateQueueCount()
    {
        if (queueCountText == null)
        {
            Debug.LogWarning("[SelectionPreviewButton] queueCountText is null");
            return;
        }

        if (building == null)
        {
            Debug.LogWarning("[SelectionPreviewButton] building is null");
            return;
        }

        var cookingTask = building.GetComponent<CookingTask>();
        if (cookingTask == null)
        {
            Debug.LogWarning($"[SelectionPreviewButton] No CookingTask found on building {building.name}");
            return;
        }

        // Count how many of this specific recipe are in the queue
        int count = 0;
        var currentRecipe = data as CookingRecipeScriptableObj;
        
        if (currentRecipe != null)
        {
            // Count current recipe if it matches
            if (cookingTask.currentRecipe == currentRecipe)
            {
                count++;
            }
            
            // Count matching recipes in queue
            foreach (var queuedRecipe in cookingTask.taskQueue)
            {
                if (queuedRecipe is CookingRecipeScriptableObj recipe && recipe == currentRecipe)
                {
                    count++;
                }
            }
        }
        
        queueCountText.text = count > 0 ? count.ToString() : "";
        queueCountText.gameObject.SetActive(count > 0);
    }

    private void OnTaskCompleted()
    {
        UpdateQueueCount();
    }

    public void SetupButton(ScriptableObject item, Building building, WorkType workType)
    {        
        this.building = building;
        this.workType = workType;
        string name = string.Empty;
        Sprite sprite = null;

        // Unsubscribe from previous cooking task if it exists
        if (cookingTask != null)
        {
            cookingTask.OnTaskCompleted -= OnTaskCompleted;
        }

        switch (workType)
        {
            case WorkType.RESEARCH:
                var research = item as ResearchScriptableObj;
                if (research != null)
                {
                    name = research.objectName;
                    sprite = research.sprite;
                }
                break;
            case WorkType.COOKING:
                var recipe = item as CookingRecipeScriptableObj;
                if (recipe != null)
                {
                    name = recipe.objectName;
                    sprite = recipe.sprite;
                    cookingTask = building.GetComponent<CookingTask>();
                    if (cookingTask != null)
                    {
                        cookingTask.OnTaskCompleted += OnTaskCompleted;
                    }
                    UpdateQueueCount();
                }
                break;
            case WorkType.UPGRADE_RESOURCE:
                var upgrade = item as ResourceUpgradeScriptableObj;
                if (upgrade != null)
                {
                    name = upgrade.objectName;
                    sprite = upgrade.sprite;
                }
                break;
        }

        base.SetupButton(item, sprite, name);
    }

    private void OnDestroy()
    {
        if (cookingTask != null)
        {
            cookingTask.OnTaskCompleted -= OnTaskCompleted;
        }
    }
}
