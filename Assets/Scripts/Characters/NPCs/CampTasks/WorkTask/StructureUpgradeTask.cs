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
        if (targetStructure == null)
        {
            Debug.LogError("StructureUpgradeTask requires a PlaceableStructure component on the same GameObject!");
            enabled = false;
        }
    }

    public void SetupUpgradeTask(PlaceableObjectParent upgradeTarget, float upgradeTime)
    {
        this.upgradeTarget = upgradeTarget;
        baseWorkTime = upgradeTime;
        
        // Set up required resources from the upgrade target
        if (upgradeTarget != null)
        {
            requiredResources = upgradeTarget.upgradeResources;
        }
    }

    protected override IEnumerator WorkCoroutine()
    {
        // Consume resources
        ConsumeResources();

        // Process the upgrade
        while (workProgress < baseWorkTime)
        {
            workProgress += Time.deltaTime;
            yield return null;
        }

        CompleteWork();
    }

    protected override void CompleteWork()
    {
        if (targetStructure != null && upgradeTarget != null)
        {
            targetStructure.StartUpgrade();
        }
        else
        {
            Debug.LogError("Structure upgrade failed: targetStructure or upgradeTarget is null");
        }
        
        base.CompleteWork();
    }

    public override bool CanPerformTask()
    {
        if (targetStructure == null || upgradeTarget == null) return false;
        
        // Check if the structure has an upgrade target (can be upgraded)
        return upgradeTarget != null;
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