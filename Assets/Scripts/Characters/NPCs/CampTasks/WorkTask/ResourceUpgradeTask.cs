using UnityEngine;
using System.Collections;
using Managers;

public class ResourceUpgradeTask : WorkTask
{
    private ResourceUpgradeScriptableObj currentUpgrade;

    protected override void Start()
    {
        base.Start();
        workType = WorkType.UPGRADE_RESOURCE;
    }

    public void SetUpgrade(ResourceUpgradeScriptableObj upgrade)
    {
        currentUpgrade = upgrade;
        if (upgrade != null)
        {
            baseWorkTime = upgrade.upgradeTime;
            requiredResources = upgrade.requiredResources;
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
                Resource upgradedResource = Instantiate(currentUpgrade.outputResource.prefab, transform.position + Random.insideUnitSphere, Quaternion.identity).GetComponent<Resource>();
                upgradedResource.Initialize(currentUpgrade.outputResource);
            }
        }
        
        base.CompleteWork();
    }
} 