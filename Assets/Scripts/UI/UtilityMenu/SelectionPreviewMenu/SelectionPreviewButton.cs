using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Managers;
using System;

public class SelectionPreviewButton : PreviewButtonBase<ScriptableObject>
{
    private WorkTask workTask;
    [SerializeField] private TMP_Text queueCountText;
    private CookingTask cookingTask;
    private ResourceUpgradeTask resourceUpgradeTask;
    private HumanCharacterController characterToAssign;

    public void SetupButton(ScriptableObject item, WorkTask workTask, HumanCharacterController characterToAssign = null)
    {        
        this.workTask = workTask;
        this.characterToAssign = characterToAssign;
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

        if (workTask is ResearchTask)
        {
            var research = item as ResearchScriptableObj;
            if (research != null)
            {
                name = research.objectName;
                sprite = research.sprite;
            }
            queueCountText.text = "";
        }
        else if (workTask is CookingTask)
        {
            var recipe = item as CookingRecipeScriptableObj;
            if (recipe != null)
            {
                name = recipe.objectName;
                sprite = recipe.sprite;
                cookingTask = workTask as CookingTask;
                if (cookingTask != null)
                {
                    cookingTask.OnTaskCompleted += OnTaskCompleted;
                }
            }
        }
        else if (workTask is ResourceUpgradeTask)
        {
            var upgrade = item as ResourceUpgradeScriptableObj;
            if (upgrade != null)
            {
                name = upgrade.objectName;
                sprite = upgrade.sprite;
                resourceUpgradeTask = workTask as ResourceUpgradeTask;
                if (resourceUpgradeTask != null)
                {
                    resourceUpgradeTask.OnTaskCompleted += OnTaskCompleted;
                }
            }
        }

        base.SetupButton(item, sprite, name);
        UpdateQueueCount();
    }
    
    protected override void OnButtonClicked()
    {
        CampManager.Instance.WorkManager.SetNPCForAssignment(characterToAssign);

        if (workTask is ResearchTask)
        {
            HandleResearch();
        }
        else if (workTask is CookingTask)
        {
            HandleCooking();
        }
        else if (workTask is ResourceUpgradeTask)
        {
            HandleResourceUpgrade();
        }
    }

    private void HandleResearch()
    {
        var research = data as ResearchScriptableObj;
        if (research == null)
        {
            Debug.LogWarning("[SelectionPreviewButton] Attempted to handle research with null research");
            return;
        }

        var researchTask = workTask as ResearchTask;
        if (researchTask == null)
        {
            Debug.LogWarning($"[SelectionPreviewButton] No ResearchTask found on building");
            return;
        }

        // Check if research can be started
        if (!CampManager.Instance.ResearchManager.CanStartResearch(research, out string errorMessage))
        {
            Debug.Log($"[SelectionPreviewButton] Cannot start research: {errorMessage}");
            PlayerUIManager.Instance.DisplayUIErrorMessage(errorMessage);
            return;
        }

        Debug.Log($"[SelectionPreviewButton] Starting research assignment for {research.objectName}");
        // Only assign work if this is the first research
        if (researchTask.CurrentResearch == null)
        {
            if (characterToAssign != null)
            {
                Debug.Log($"[SelectionPreviewButton] Assigning character {characterToAssign.name} to research");
                CampManager.Instance.WorkManager.AssignWorkToBuilding(researchTask);
            }
            else
            {
                Debug.LogWarning("[SelectionPreviewButton] No character assigned for research");
            }
        }

        researchTask.SetResearch(research);
        
        // Don't close the menu, just update the preview
        PlayerUIManager.Instance.selectionPreviewList.UpdatePreview(data);
    }

    private void HandleCooking()
    {
        var recipe = data as CookingRecipeScriptableObj;
        if (recipe == null)
        {
            Debug.LogWarning("[SelectionPreviewButton] Attempted to handle cooking with null recipe");
            return;
        }

        var cookingTask = workTask as CookingTask;
        if (cookingTask == null)
        {
            Debug.LogWarning($"[SelectionPreviewButton] No CookingTask found on building");
            return;
        }
        
        // Only assign work if this is the first recipe
        if (cookingTask.currentRecipe == null)
        {
            if (characterToAssign != null)
            {
                CampManager.Instance.WorkManager.AssignWorkToBuilding(cookingTask);
            }
            else
            {
                Debug.LogWarning("[SelectionPreviewButton] No character assigned for cooking");
            }
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

        var upgradeTask = workTask as ResourceUpgradeTask;
        if (upgradeTask == null)
        {
            Debug.LogWarning($"[SelectionPreviewButton] No ResourceUpgradeTask found on building");
            return;
        }
        
        // Only assign work if this is the first upgrade
        if (upgradeTask.currentUpgrade == null)
        {
            if (characterToAssign != null)
            {
                CampManager.Instance.WorkManager.AssignWorkToBuilding(upgradeTask);
            }
            else
            {
                Debug.LogWarning("[SelectionPreviewButton] No character assigned for resource upgrade");
            }
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

        if (workTask is CookingTask cookingTask)
        {
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
        }
        else if (workTask is ResourceUpgradeTask upgradeTask)
        {
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
        }
    }

    private void OnTaskCompleted()
    {
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
