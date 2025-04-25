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

    private bool HasRequiredResources()
    {
        foreach (var resourceItem in requiredResources)
        {
            if (CampManager.Instance.PlayerInventory.GetItemCount(resourceItem.resource) < resourceItem.count)
            {
                return false;
            }
        }
        return true;
    }

    private void ConsumeResources()
    {
        foreach (var resourceItem in requiredResources)
        {
            CampManager.Instance.PlayerInventory.RemoveItem(resourceItem.resource, resourceItem.count);
        }
    }

    protected override void CompleteWork()
    {
        if (targetBuilding != null)
        {
            targetBuilding.Upgrade(upgradeTarget);
            Debug.Log($"Building upgrade completed!");
        }
        
        base.CompleteWork();
    }

    public override bool CanPerformTask()
    {
        if (targetBuilding == null || upgradeTarget == null) return false;
        
        // Check if the building has an upgrade target (can be upgraded)
        return upgradeTarget != null;
    }
} 