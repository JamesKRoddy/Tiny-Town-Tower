using UnityEngine;
using System.Collections;
using Managers;

public class ResourceUpgradeTask : WorkTask
{
    public ResourceUpgradeScriptableObj currentUpgrade;

    protected override void Start()
    {
        base.Start();
        workType = WorkType.UPGRADE_RESOURCE;
    }

    public void SetUpgrade(ResourceUpgradeScriptableObj upgrade)
    {
        if (upgrade == null) return;
        
        // Queue the upgrade
        QueueTask(upgrade);

        // If no current upgrade, set it up immediately
        if (currentUpgrade == null)
        {
            currentTaskData = taskQueue.Dequeue();
            SetupNextTask();
        }
    }

    protected override void SetupNextTask()
    {
        if (currentTaskData is ResourceUpgradeScriptableObj nextUpgrade)
        {
            currentUpgrade = nextUpgrade;
            baseWorkTime = nextUpgrade.upgradeTime;
            requiredResources = nextUpgrade.requiredResources;
        }
    }

    protected override IEnumerator WorkCoroutine()
    {
        // Check if we have the required input resources
        if (!HasRequiredResources())
        {
            Debug.LogWarning("Not enough resources for upgrade");
            yield break;
        }

        // Consume input resources
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
        if (currentUpgrade != null)
        {
            // Create the upgraded resources
            for (int i = 0; i < currentUpgrade.outputAmount; i++)
            {
                AddResourceToInventory(currentUpgrade.outputResource);
            }
        }
        
        // Clear the current upgrade before completing the work
        currentUpgrade = null;
        
        base.CompleteWork();
    }
} 