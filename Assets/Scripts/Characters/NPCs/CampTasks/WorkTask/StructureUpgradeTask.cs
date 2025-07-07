using UnityEngine;
using System.Collections;
using Managers;

public class StructureUpgradeTask : WorkTask
{
    private PlaceableObjectParent upgradeTarget;

    protected override void Start()
    {
        base.Start();
        taskAnimation = TaskAnimation.HAMMER_STANDING;
    }

    public void SetupUpgradeTask(PlaceableObjectParent upgradeTarget)
    {
        this.upgradeTarget = upgradeTarget;
        baseWorkTime = upgradeTarget?.constructionTime ?? 1f;
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
        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;

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
        CampManager.Instance.MarkSharedGridSlotsUnoccupied(position, GetComponent<IPlaceableStructure>().GetStructureScriptableObj().size);
        CampManager.Instance.MarkSharedGridSlotsOccupied(position, upgradeTarget.size, constructionSite);

        // Add to work manager
        CampManager.Instance.WorkManager.AddWorkTask(constructionTask);

        // Destroy the original structure
        Destroy(gameObject);
    }



    public override bool CanPerformTask()
    {
        if (GetComponent<IPlaceableStructure>() == null || upgradeTarget == null) return false;
        
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