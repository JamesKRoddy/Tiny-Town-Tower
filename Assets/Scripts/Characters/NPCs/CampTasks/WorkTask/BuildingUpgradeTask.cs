using UnityEngine;
using System.Collections;
using Managers;

public class BuildingUpgradeTask : WorkTask
{
    private BuildingScriptableObj upgradeTarget;
    private float upgradeTime = 30f;
    private ResourceScriptableObj[] requiredResources;
    private int[] resourceCosts;

    private float upgradeProgress = 0f;
    private SettlerNPC currentWorker;
    private Coroutine upgradeCoroutine;
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

    public void SetupUpgradeTask(BuildingScriptableObj upgradeTarget, float upgradeTime, ResourceScriptableObj[] requiredResources, int[] resourceCosts)
    {
        this.upgradeTarget = upgradeTarget;
        this.upgradeTime = upgradeTime;
        this.requiredResources = requiredResources;
        this.resourceCosts = resourceCosts;
    }

    public override void PerformTask(SettlerNPC npc)
    {
        if (currentWorker == null && HasRequiredResources())
        {
            currentWorker = npc;
            ConsumeResources();
            upgradeCoroutine = StartCoroutine(UpgradeCoroutine());
        }
    }

    private IEnumerator UpgradeCoroutine()
    {
        while (upgradeProgress < upgradeTime)
        {
            upgradeProgress += Time.deltaTime;
            yield return null;
        }

        CompleteUpgrade();
    }

    private bool HasRequiredResources()
    {
        for (int i = 0; i < requiredResources.Length; i++)
        {
            if (CampManager.Instance.PlayerInventory.GetItemCount(requiredResources[i]) < resourceCosts[i])
            {
                return false;
            }
        }
        return true;
    }

    private void ConsumeResources()
    {
        for (int i = 0; i < requiredResources.Length; i++)
        {
            CampManager.Instance.PlayerInventory.RemoveItem(requiredResources[i], resourceCosts[i]);
        }
    }

    private void CompleteUpgrade()
    {
        if (targetBuilding != null)
        {
            targetBuilding.Upgrade(upgradeTarget);
            Debug.Log($"Building upgrade completed!");
        }
        
        // Reset state
        upgradeProgress = 0f;
        currentWorker = null;
        upgradeCoroutine = null;
        
        // Notify completion
        InvokeStopWork();
    }

    public override Transform WorkTaskTransform()
    {
        return transform;
    }

    private void OnDisable()
    {
        if (upgradeCoroutine != null)
        {
            StopCoroutine(upgradeCoroutine);
            upgradeCoroutine = null;
        }
    }
} 