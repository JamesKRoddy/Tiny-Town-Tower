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
    private ResourceUpgradeTask resourceUpgradeTask;

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
        if (upgrade == null)
        {
            Debug.LogWarning("[SelectionPreviewButton] Attempted to handle resource upgrade with null upgrade");
            return;
        }

        if (building == null)
        {
            Debug.LogWarning("[SelectionPreviewButton] Attempted to handle resource upgrade with null building");
            return;
        }

        var upgradeTask = building.GetComponent<ResourceUpgradeTask>();
        if (upgradeTask == null)
        {
            Debug.LogWarning($"[SelectionPreviewButton] No ResourceUpgradeTask found on building {building.name}");
            return;
        }
        
        // Only assign work if this is the first upgrade
        if (upgradeTask.currentUpgrade == null)
        {
            CampManager.Instance.WorkManager.AssignWorkToBuilding(upgradeTask);
        }

        upgradeTask.SetUpgrade(upgrade);
        
        // Don't close the menu, just update the preview
        PlayerUIManager.Instance.selectionPreviewList.UpdatePreview(data);
        UpdateQueueCount();
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

        switch (workType)
        {
            case WorkType.COOKING:
                var cookingTask = building.GetComponent<CookingTask>();
                if (cookingTask == null)
                {
                    Debug.LogWarning($"[SelectionPreviewButton] No CookingTask found on building {building.name}");
                    return;
                }

                // Count how many of this specific recipe are in the queue
                int cookingCount = 0;
                var currentRecipe = data as CookingRecipeScriptableObj;
                
                if (currentRecipe != null)
                {
                    // Count current recipe if it matches
                    if (cookingTask.currentRecipe == currentRecipe)
                    {
                        cookingCount++;
                    }
                    
                    // Count matching recipes in queue
                    foreach (var queuedRecipe in cookingTask.taskQueue)
                    {
                        if (queuedRecipe is CookingRecipeScriptableObj recipe && recipe == currentRecipe)
                        {
                            cookingCount++;
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("[SelectionPreviewButton] No current recipe found");
                }
                
                queueCountText.text = cookingCount > 0 ? cookingCount.ToString() : "";
                queueCountText.gameObject.SetActive(cookingCount > 0);
                break;

            case WorkType.UPGRADE_RESOURCE:
                var upgradeTask = building.GetComponent<ResourceUpgradeTask>();
                if (upgradeTask == null)
                {
                    Debug.LogWarning($"[SelectionPreviewButton] No ResourceUpgradeTask found on building {building.name}");
                    return;
                }

                // Count how many of this specific upgrade are in the queue
                int upgradeCount = 0;
                var currentUpgrade = data as ResourceUpgradeScriptableObj;
                
                if (currentUpgrade != null)
                {
                    // Count current upgrade if it matches
                    if (upgradeTask.currentUpgrade == currentUpgrade)
                    {
                        upgradeCount++;
                    }
                    
                    // Count matching upgrades in queue
                    foreach (var queuedUpgrade in upgradeTask.taskQueue)
                    {
                        if (queuedUpgrade is ResourceUpgradeScriptableObj upgrade && upgrade == currentUpgrade)
                        {
                            upgradeCount++;
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("[SelectionPreviewButton] No current upgrade found");
                }
                
                queueCountText.text = upgradeCount > 0 ? upgradeCount.ToString() : "";
                queueCountText.gameObject.SetActive(upgradeCount > 0);
                break;
        }
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

        // Unsubscribe from previous tasks if they exist
        if (cookingTask != null)
        {
            cookingTask.OnTaskCompleted -= OnTaskCompleted;
        }
        if (resourceUpgradeTask != null)
        {
            resourceUpgradeTask.OnTaskCompleted -= OnTaskCompleted;
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
                }
                break;
            case WorkType.UPGRADE_RESOURCE:
                var upgrade = item as ResourceUpgradeScriptableObj;
                if (upgrade != null)
                {
                    name = upgrade.objectName;
                    sprite = upgrade.sprite;
                    resourceUpgradeTask = building.GetComponent<ResourceUpgradeTask>();
                    if (resourceUpgradeTask != null)
                    {
                        resourceUpgradeTask.OnTaskCompleted += OnTaskCompleted;
                    }
                }
                break;
        }

        base.SetupButton(item, sprite, name);
        UpdateQueueCount();
    }

    private void OnDestroy()
    {
        if (cookingTask != null)
        {
            cookingTask.OnTaskCompleted -= OnTaskCompleted;
        }
        if (resourceUpgradeTask != null)
        {
            resourceUpgradeTask.OnTaskCompleted -= OnTaskCompleted;
        }
    }
}
