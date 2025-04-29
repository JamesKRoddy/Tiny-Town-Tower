using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Managers;

public class SelectionPreviewList : PreviewListMenuBase<string, ScriptableObject>
{
    private WorkTask currentTask;
    private Building parentBuilding;

    public void Setup(WorkTask task, Building building)
    {
        currentTask = task;
        parentBuilding = building;
        RefreshUIAndSelectFirst();
    }

    public override void DestroyPreviewSpecifics()
    {
        // No specific cleanup needed
    }

    public override string GetItemCategory(ScriptableObject item)
    {
        // Group items by their type
        return item.GetType().Name;
    }

    public override IEnumerable<ScriptableObject> GetItems()
    {
        // Return items based on the current task type
        switch (currentTask.workType)
        {
            case WorkType.RESEARCH:
                return CampManager.Instance.ResearchManager.GetAllResearch();
            case WorkType.COOKING:
                // Return cooking recipes
                return CampManager.Instance.CookingManager.GetAllRecipes();
            case WorkType.UPGRADE_RESOURCE:
                // Return resource upgrade options
                return CampManager.Instance.ResourceUpgradeManager.GetAllUpgrades();
            default:
                return new List<ScriptableObject>();
        }
    }

    public override string GetPreviewDescription(ScriptableObject item)
    {
        switch (currentTask.workType)
        {
            case WorkType.RESEARCH:
                var research = item as ResearchScriptableObj;
                if (research != null)
                {
                    string description = research.description + "\n\n";
                    if (research.requiredResources != null && research.requiredResources.Length > 0)
                    {
                        description += "Required Resources:\n";
                        foreach (var resource in research.requiredResources)
                        {
                            description += $"- {resource.resource.objectName}\n";
                        }
                    }
                    description += $"\nResearch Time: {research.researchTime} seconds";
                    if (research.unlockedItems != null && research.unlockedItems.Length > 0)
                    {
                        description += $"\n\nUnlocks:";
                        foreach (var unlockedItem in research.unlockedItems)
                        {
                            description += $"\n- {unlockedItem.objectName}";
                        }
                    }
                    return description;
                }
                break;
            case WorkType.COOKING:
                var recipe = item as CookingRecipeScriptableObj;
                if (recipe != null)
                {
                    string description = recipe.description + "\n\n";
                    if (recipe.requiredIngredients != null && recipe.requiredIngredients.Length > 0)
                    {
                        description += "Required Ingredients:\n";
                        foreach (var ingredient in recipe.requiredIngredients)
                        {
                            description += $"- {ingredient.resource.objectName}\n";
                        }
                    }
                    description += $"\nCooking Time: {recipe.cookingTime} seconds";
                    return description;
                }
                break;
            case WorkType.UPGRADE_RESOURCE:
                var upgrade = item as ResourceUpgradeScriptableObj;
                if (upgrade != null)
                {
                    string description = upgrade.description + "\n\n";
                    if (upgrade.requiredResources != null && upgrade.requiredResources.Length > 0)
                    {
                        description += "Required Resources:\n";
                        foreach (var resource in upgrade.requiredResources)
                        {
                            description += $"- {resource.resource.objectName}\n";
                        }
                    }
                    description += $"\nUpgrade Time: {upgrade.upgradeTime} seconds";
                    return description;
                }
                break;
        }
        return string.Empty;
    }

    public override string GetPreviewName(ScriptableObject item)
    {
        switch (currentTask.workType)
        {
            case WorkType.RESEARCH:
                return (item as ResearchScriptableObj)?.objectName ?? string.Empty;
            case WorkType.COOKING:
                return (item as CookingRecipeScriptableObj)?.objectName ?? string.Empty;
            case WorkType.UPGRADE_RESOURCE:
                return (item as ResourceUpgradeScriptableObj)?.objectName ?? string.Empty;
            default:
                return string.Empty;
        }
    }

    public override IEnumerable<(string resourceName, int requiredCount, int playerCount)> GetPreviewResourceCosts(ScriptableObject item)
    {
        ResourceItemCount[] requiredResources = null;
        
        switch (currentTask.workType)
        {
            case WorkType.RESEARCH:
                requiredResources = (item as ResearchScriptableObj)?.requiredResources;
                break;
            case WorkType.COOKING:
                requiredResources = (item as CookingRecipeScriptableObj)?.requiredIngredients;
                break;
            case WorkType.UPGRADE_RESOURCE:
                requiredResources = (item as ResourceUpgradeScriptableObj)?.requiredResources;
                break;
        }

        if (requiredResources != null)
        {
            foreach (var resource in requiredResources)
            {
                yield return (
                    resource.resource.objectName,
                    resource.count,
                    PlayerInventory.Instance.GetItemCount(resource.resource)
                );
            }
        }
    }

    public override Sprite GetPreviewSprite(ScriptableObject item)
    {
        switch (currentTask.workType)
        {
            case WorkType.RESEARCH:
                return (item as ResearchScriptableObj)?.sprite;
            case WorkType.COOKING:
                return (item as CookingRecipeScriptableObj)?.sprite;
            case WorkType.UPGRADE_RESOURCE:
                return (item as ResourceUpgradeScriptableObj)?.sprite;
            default:
                return null;
        }
    }

    public override void SetupItemButton(ScriptableObject item, GameObject button)
    {
        var buttonComponent = button.GetComponent<SelectionPreviewButton>();
        buttonComponent.SetupButton(item, parentBuilding, currentTask.workType);
    }

    public override void UpdatePreviewSpecifics(ScriptableObject item)
    {
        // No additional specifics needed
    }

    public void ReturnToGame(PlayerControlType playerControlType = PlayerControlType.NONE)
    {
        SetScreenActive(false);

        if (playerControlType != PlayerControlType.NONE)
        {
            PlayerInput.Instance.UpdatePlayerControls(playerControlType);
        }
        else
        {
            PlayerInput.Instance.UpdatePlayerControls(GameManager.Instance.PlayerGameControlType());
        }        
    }
}
