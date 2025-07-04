using UnityEngine;
using System.Collections;
using Managers;

public class StructureUpgradeTask : WorkTask
{
    private PlaceableObjectParent upgradeTarget;
    private PlaceableStructure targetStructure;

    protected override void Start()
    {
        base.Start();
        targetStructure = GetComponent<PlaceableStructure>();
        taskAnimation = TaskAnimation.HAMMER_STANDING;
    }

    public void SetupUpgradeTask(PlaceableObjectParent upgradeTarget, float upgradeTime)
    {
        this.upgradeTarget = upgradeTarget;
        baseWorkTime = upgradeTime;
        requiredResources = upgradeTarget?.upgradeResources;
    }

    public void ExecuteUpgrade()
    {
        // Consume resources and create construction site immediately
        ConsumeResources();
        CreateConstructionSite();
    }



    private void CreateConstructionSite()
    {
        Vector3 position = targetStructure.transform.position;
        Quaternion rotation = targetStructure.transform.rotation;

        // Get old structure info before destroying it
        var oldSize = targetStructure.GetStructureScriptableObj()?.size ?? new Vector2Int(1, 1);

        // Get construction site prefab
        GameObject constructionSitePrefab = CampManager.Instance.BuildManager.GetConstructionSitePrefab(upgradeTarget.size);
        if (constructionSitePrefab == null)
        {
            Debug.LogError($"No construction site prefab for size {upgradeTarget.size}");
            return;
        }

        // Create construction site
        GameObject constructionSite = Instantiate(constructionSitePrefab, position, rotation);
        
        // Setup construction task
        StructureConstructionTask constructionTask = constructionSite.GetComponent<StructureConstructionTask>();
        if (constructionTask == null)
        {
            constructionTask = constructionSite.AddComponent<StructureConstructionTask>();
        }
        constructionTask.SetupConstruction(upgradeTarget, true);

        // Handle grid slots
        CampManager.Instance.MarkSharedGridSlotsUnoccupied(position, oldSize);
        CampManager.Instance.MarkSharedGridSlotsOccupied(position, upgradeTarget.size, constructionSite);

        // Add to work manager
        CampManager.Instance.WorkManager.AddWorkTask(constructionTask);

        // Destroy the original structure
        Destroy(targetStructure.gameObject);
    }

    public override bool CanPerformTask()
    {
        if (targetStructure == null || upgradeTarget == null) return false;
        
        // Check if player has required resources
        if (requiredResources != null)
        {
            foreach (var resource in requiredResources)
            {
                if (PlayerInventory.Instance.GetItemCount(resource.resourceScriptableObj) < resource.count)
                {
                    return false;
                }
            }
        }
        
        return true;
    }

    public override string GetTooltipText()
    {
        if (upgradeTarget == null) return "No upgrade target selected";
        
        string tooltip = $"Upgrade to {upgradeTarget.objectName}\n";
        tooltip += $"Time: {baseWorkTime} seconds\n";
        
        if (requiredResources != null)
        {
            tooltip += "Required Resources:\n";
            foreach (var resource in requiredResources)
            {
                tooltip += $"- {resource.resourceScriptableObj.objectName}: {resource.count}\n";
            }
        }
        
        return tooltip;
    }
} 