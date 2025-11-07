using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Managers;

public class SelectionPreviewList : PreviewListMenuBase<string, ScriptableObject>
{
    private WorkTask currentTask;
    private HumanCharacterController characterToAssign;
    private Dictionary<GameObject, WorkTask> buttonToTaskMap = new Dictionary<GameObject, WorkTask>();

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
                description += $"\nResearch Time: {research.craftTimeInGameHours} game hours";
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
                if (recipe.requiredResources != null && recipe.requiredResources.Length > 0)
                {
                    description += "Required Ingredients:\n";
                    foreach (var ingredient in recipe.requiredResources)
                    {
                        description += $"- {ingredient.resourceScriptableObj.objectName}\n";
                    }
                }
                description += $"\nCooking Time: {recipe.craftTimeInGameHours} game hours";
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
                description += $"\nUpgrade Time: {upgrade.craftTimeInGameHours} game hours";
                return description;
            }
        } else if (currentTask is FarmingTask){
            var crop = item as ResourceScriptableObj;
            if (crop != null)
            {
                return crop.description;
            }
        }
        return string.Empty;
    }

    public override string GetPreviewName(ScriptableObject item)
    {
        return (item as WorldItemBase)?.objectName;
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
            requiredResources = (item as CookingRecipeScriptableObj)?.requiredResources;
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
        return (item as WorldItemBase)?.sprite;
    }

    public override void SetupItemButton(ScriptableObject item, GameObject button)
    {
        var buttonComponent = button.GetComponent<PreviewButtonBase>();
        if (buttonComponent == null) return;

        string name = string.Empty;
        Sprite sprite = null;
        string countDisplay = "";

        // Unsubscribe from previous task if it exists
        if (currentTask != null)
        {
            currentTask.OnTaskCompleted -= () => OnTaskCompleted(button);
        }

        // Setup based on task type
        switch (currentTask)
        {
            case ResearchTask researchTask:
                var research = item as ResearchScriptableObj;
                if (research != null)
                {
                    name = research.objectName;
                    sprite = research.sprite;
                }
                break;

            case CookingTask cookingTask:
                var recipe = item as CookingRecipeScriptableObj;
                if (recipe != null)
                {
                    name = recipe.objectName;
                    sprite = recipe.sprite;
                    cookingTask.OnTaskCompleted += () => OnTaskCompleted(button);
                    countDisplay = GetItemCount(item, currentTask);
                }
                break;

            case ResourceUpgradeTask upgradeTask:
                var upgrade = item as ResourceUpgradeScriptableObj;
                if (upgrade != null)
                {
                    name = upgrade.objectName;
                    sprite = upgrade.sprite;
                    upgradeTask.OnTaskCompleted += () => OnTaskCompleted(button);
                    countDisplay = GetItemCount(item, currentTask);
                }
                break;

            case FarmingTask farmingTask:
                var seed = item as ResourceScriptableObj;
                if (seed != null)
                {
                    name = seed.objectName;
                    sprite = seed.sprite;
                    countDisplay = GetItemCount(item, currentTask);
                }
                break;
        }

        buttonComponent.SetupButton(item, (obj) => OnItemButtonClicked(obj as ScriptableObject, button), sprite, name, countDisplay);
        buttonToTaskMap[button] = currentTask;
    }

    private string GetItemCount(ScriptableObject item, WorkTask task)
    {
        int count = 0;
        
        switch (task)
        {
            case CookingTask cookingTask:
                var recipe = item as CookingRecipeScriptableObj;
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
                var upgrade = item as ResourceUpgradeScriptableObj;
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
                var seed = item as ResourceScriptableObj;
                if (seed != null)
                {
                    count = PlayerInventory.Instance.GetItemCount(seed);
                }
                break;
        }

        return count > 0 ? count.ToString() : "";
    }

    private void OnItemButtonClicked(ScriptableObject item, GameObject button)
    {
        if (item == null)
        {
            Debug.LogWarning("[SelectionPreviewList] Item is null after cast");
            return;
        }

        WorkTask workTask = buttonToTaskMap.ContainsKey(button) ? buttonToTaskMap[button] : null;
        
        if (workTask == null)
        {
            Debug.LogWarning("[SelectionPreviewList] No work task assigned");
            return;
        }

        // Handle task assignment
        if (characterToAssign != null && !workTask.IsOccupied)
        {
            CampManager.Instance.WorkManager.SetNPCForAssignment(characterToAssign);
            CampManager.Instance.WorkManager.AssignWorkToBuilding(workTask);
        }

        // Set task data based on type
        switch (workTask)
        {
            case ResearchTask researchTask:
                var research = item as ResearchScriptableObj;
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
                var recipe = item as CookingRecipeScriptableObj;
                if (recipe != null)
                {
                    cookingTask.SetRecipe(recipe);
                }
                break;

            case ResourceUpgradeTask upgradeTask:
                var upgrade = item as ResourceUpgradeScriptableObj;
                if (upgrade != null)
                {
                    upgradeTask.SetUpgrade(upgrade);
                }
                break;

            case FarmingTask farmingTask:
                var seed = item as ResourceScriptableObj;
                if (seed != null)
                {
                    if (PlayerInventory.Instance.GetItemCount(seed) < 1)
                    {
                        PlayerUIManager.Instance.DisplayUIErrorMessage($"Not enough {seed.objectName} seeds");
                        return;
                    }
                    farmingTask.requiredResources = new ResourceItemCount[] { new ResourceItemCount(seed, 1) };
                    ReturnToGame();
                }
                break;
        }
        
        // Update preview without closing menu
        UpdatePreview(item);
        UpdateButtonCount(button);
    }

    private void UpdateButtonCount(GameObject button)
    {
        if (button == null) return;

        var buttonComponent = button.GetComponent<PreviewButtonBase>();
        if (buttonComponent == null || !buttonToTaskMap.ContainsKey(button)) return;

        var data = buttonComponent.Data as ScriptableObject;
        if (data == null) return;

        string countDisplay = GetItemCount(data, buttonToTaskMap[button]);
        buttonComponent.UpdateCountText(countDisplay);
    }

    private void OnTaskCompleted(GameObject button)
    {
        UpdateButtonCount(button);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        
        // Unsubscribe from all task completion events
        if (currentTask != null)
        {
            currentTask.OnTaskCompleted -= () => OnTaskCompleted(null);
        }
        buttonToTaskMap.Clear();
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
