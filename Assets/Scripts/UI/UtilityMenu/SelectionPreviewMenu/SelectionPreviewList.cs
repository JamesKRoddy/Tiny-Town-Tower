using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Managers;

public class SelectionPreviewList : PreviewListMenuBase<string, ScriptableObject>
{
    private WorkTask currentTask;
    private HumanCharacterController characterToAssign;

    public void Setup(WorkTask task, HumanCharacterController characterToAssign)
    {
        currentTask = task;
        this.characterToAssign = characterToAssign;
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
        if (currentTask is ResearchTask)
        {
            return CampManager.Instance.ResearchManager.GetAllResearch();
        }
        else if (currentTask is CookingTask)
        {
            return CampManager.Instance.CookingManager.GetAllRecipes();
        }
        else if (currentTask is ResourceUpgradeTask)
        {
            return CampManager.Instance.ResourceUpgradeManager.GetAllUpgrades();
        } else if (currentTask is FarmingTask)
        {
            return CampManager.Instance.FarmingManager.GetAllCrops();
        }
        return new List<ScriptableObject>();
    }

    public override string GetPreviewDescription(ScriptableObject item)
    {
        if (currentTask is ResearchTask)
        {
            var research = item as ResearchScriptableObj;
            if (research != null)
            {
                string description = research.description + "\n\n";
                if (research.requiredResources != null && research.requiredResources.Length > 0)
                {
                    description += "Required Resources:\n";
                    foreach (var resource in research.requiredResources)
                    {
                        description += $"- {resource.resourceScriptableObj.objectName}\n";
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
        }
        else if (currentTask is CookingTask)
        {
            var recipe = item as CookingRecipeScriptableObj;
            if (recipe != null)
            {
                string description = recipe.description + "\n\n";
                if (recipe.requiredIngredients != null && recipe.requiredIngredients.Length > 0)
                {
                    description += "Required Ingredients:\n";
                    foreach (var ingredient in recipe.requiredIngredients)
                    {
                        description += $"- {ingredient.resourceScriptableObj.objectName}\n";
                    }
                }
                description += $"\nCooking Time: {recipe.cookingTime} seconds";
                return description;
            }
        }
        else if (currentTask is ResourceUpgradeTask)
        {
            var upgrade = item as ResourceUpgradeScriptableObj;
            if (upgrade != null)
            {
                string description = upgrade.description + "\n\n";
                if (upgrade.requiredResources != null && upgrade.requiredResources.Length > 0)
                {
                    description += "Required Resources:\n";
                    foreach (var resource in upgrade.requiredResources)
                    {
                        description += $"- {resource.resourceScriptableObj.objectName}\n";
                    }
                }
                description += $"\nUpgrade Time: {upgrade.upgradeTime} seconds";
                return description;
            }
        }
        return string.Empty;
    }

    public override string GetPreviewName(ScriptableObject item)
    {
        if (currentTask is ResearchTask)
        {
            return (item as ResearchScriptableObj)?.objectName ?? string.Empty;
        }
        else if (currentTask is CookingTask)
        {
            return (item as CookingRecipeScriptableObj)?.objectName ?? string.Empty;
        }
        else if (currentTask is ResourceUpgradeTask)
        {
            return (item as ResourceUpgradeScriptableObj)?.objectName ?? string.Empty;
        }
        return string.Empty;
    }

    public override IEnumerable<(string resourceName, int requiredCount, int playerCount)> GetPreviewResourceCosts(ScriptableObject item)
    {
        ResourceItemCount[] requiredResources = null;
        
        if (currentTask is ResearchTask)
        {
            requiredResources = (item as ResearchScriptableObj)?.requiredResources;
        }
        else if (currentTask is CookingTask)
        {
            requiredResources = (item as CookingRecipeScriptableObj)?.requiredIngredients;
        }
        else if (currentTask is ResourceUpgradeTask)
        {
            requiredResources = (item as ResourceUpgradeScriptableObj)?.requiredResources;
        }

        if (requiredResources != null)
        {
            foreach (var resource in requiredResources)
            {
                yield return (
                    resource.resourceScriptableObj.objectName,
                    resource.count,
                    PlayerInventory.Instance.GetItemCount(resource.resourceScriptableObj)
                );
            }
        }
    }

    public override Sprite GetPreviewSprite(ScriptableObject item)
    {
        if (currentTask is ResearchTask)
        {
            return (item as ResearchScriptableObj)?.sprite;
        }
        else if (currentTask is CookingTask)
        {
            return (item as CookingRecipeScriptableObj)?.sprite;
        }
        else if (currentTask is ResourceUpgradeTask)
        {
            return (item as ResourceUpgradeScriptableObj)?.sprite;
        }
        return null;
    }

    public override void SetupItemButton(ScriptableObject item, GameObject button)
    {
        var buttonComponent = button.GetComponent<SelectionPreviewButton>();
        buttonComponent.SetupButton(item, currentTask, characterToAssign);
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
