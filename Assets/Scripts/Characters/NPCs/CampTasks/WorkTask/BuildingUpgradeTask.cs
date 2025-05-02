using UnityEngine;
using System.Collections;
using Managers;

public class BuildingUpgradeTask : WorkTask
{
    private BuildingScriptableObj upgradeTarget;
    private Building targetBuilding;

    protected override void Start()
    {
        base.Start();
        workType = WorkType.UPGRADE_BUILDING;
        targetBuilding = GetComponent<Building>();
        if (targetBuilding == null)
        {
            Debug.LogError("BuildingUpgradeTask requires a Building component on the same GameObject!");
            enabled = false;
        }
    }

    public void SetupUpgradeTask(BuildingScriptableObj upgradeTarget, float upgradeTime)
    {
        this.upgradeTarget = upgradeTarget;
        baseWorkTime = upgradeTime;
    }

    protected override IEnumerator WorkCoroutine()
    {
        if (!HasRequiredResources())
        {
            Debug.LogWarning("Not enough resources for upgrade");
            yield break;
        }

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
        if (targetBuilding != null && upgradeTarget != null)
        {
            targetBuilding.Upgrade(upgradeTarget);
        }
        else
        {
            Debug.LogError("Building upgrade failed: targetBuilding or upgradeTarget is null");
        }
        
        base.CompleteWork();
    }

    public override bool CanPerformTask()
    {
        if (targetBuilding == null || upgradeTarget == null) return false;
        
        // Check if the building has an upgrade target (can be upgraded)
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