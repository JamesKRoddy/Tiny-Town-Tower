using System.Collections.Generic;
using UnityEngine;
using Managers;

public class SelectionPreviewList : PreviewListMenuBase<string, ResearchScriptableObj>
{
    private WorkTask currentTask;
    private GameObject parentBuilding;

    public void Setup(WorkTask task, GameObject building)
    {
        currentTask = task;
        parentBuilding = building;
        RefreshUIAndSelectFirst();
    }

    public override void DestroyPreviewSpecifics()
    {
        // No specific cleanup needed
    }

    public override string GetItemCategory(ResearchScriptableObj item)
    {
        // Group research by unlock type
        return item.unlockType.ToString();
    }

    public override IEnumerable<ResearchScriptableObj> GetItems()
    {
        return CampManager.Instance.ResearchManager.GetAllResearch();
    }

    public override string GetPreviewDescription(ResearchScriptableObj item)
    {
        string description = item.description + "\n\n";
        
        // Add resource requirements
        if (item.requiredResources != null && item.requiredResources.Length > 0)
        {
            description += "Required Resources:\n";
            foreach (var resource in item.requiredResources)
            {
                description += $"- {resource.resource.objectName}\n";
            }
        }

        // Add research time
        description += $"\nResearch Time: {item.researchTime} seconds";

        // Add unlock information
        if (item.unlockedItems != null && item.unlockedItems.Length > 0)
        {
            description += $"\n\nUnlocks:";
            foreach (var unlockedItem in item.unlockedItems)
            {
                description += $"\n- {unlockedItem.objectName}";
            }
        }

        return description;
    }

    public override string GetPreviewName(ResearchScriptableObj item)
    {
        return item.objectName;
    }

    public override IEnumerable<(string resourceName, int requiredCount, int playerCount)> GetPreviewResourceCosts(ResearchScriptableObj item)
    {
        if (item.requiredResources != null)
        {
            foreach (var resource in item.requiredResources)
            {
                yield return (
                    resource.resource.objectName,
                    resource.count,
                    PlayerInventory.Instance.GetItemCount(resource.resource)
                );
            }
        }
    }

    public override Sprite GetPreviewSprite(ResearchScriptableObj item)
    {
        return item.sprite;
    }

    public override void SetupItemButton(ResearchScriptableObj item, GameObject button)
    {
        var buttonComponent = button.GetComponent<SelectionPreviewButton>();
        buttonComponent.SetupButton(item);
    }

    public override void UpdatePreviewSpecifics(ResearchScriptableObj item)
    {
        // No additional specifics needed
    }
}
