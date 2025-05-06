using UnityEngine;
using System.Collections;
using Managers;

public class ResourceUpgradeTask : WorkTask
{
    public ResourceUpgradeScriptableObj currentUpgrade;

    protected override void Start()
    {
        base.Start();
    }

    public void SetUpgrade(ResourceUpgradeScriptableObj upgrade)
    {
        SetupTask(upgrade);
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

    public override string GetAnimationClipName()
    {
        return TaskAnimation.UPGRADE_RESOURCE.ToString();
    }
} 