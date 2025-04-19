using UnityEngine;
using System.Collections;
using Managers;

public class ResourceUpgradeTask : ResourceWorkTask
{
    [SerializeField] private ResourceScriptableObj inputResource;
    [SerializeField] private int inputAmount = 1;
    [SerializeField] private ResourceScriptableObj outputResource;
    [SerializeField] private int outputAmount = 1;
    [SerializeField] private float upgradeTime = 15f;

    protected override void Start()
    {
        base.Start();
        workType = WorkType.UPGRADE_RESOURCE;
        baseWorkTime = upgradeTime;
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

    private bool HasRequiredResources()
    {
        return CampManager.Instance.PlayerInventory.GetItemCount(inputResource) >= inputAmount;
    }

    private void ConsumeResources()
    {
        CampManager.Instance.PlayerInventory.RemoveItem(inputResource, inputAmount);
    }

    protected override void CompleteWork()
    {
        // Create the upgraded resources
        for (int i = 0; i < outputAmount; i++)
        {
            Resource upgradedResource = Instantiate(outputResource.prefab, transform.position + Random.insideUnitSphere, Quaternion.identity).GetComponent<Resource>();
            upgradedResource.Initialize(outputResource);
        }

        base.CompleteWork();
    }
} 