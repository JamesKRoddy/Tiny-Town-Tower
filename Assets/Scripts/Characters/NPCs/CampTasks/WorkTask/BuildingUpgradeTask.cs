using UnityEngine;
using System.Collections;
using Managers;

public class BuildingUpgradeTask : WorkTask
{
    private BuildingScriptableObj upgradeTarget;
    private float upgradeTime = 30f;
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

    public void SetupUpgradeTask(BuildingScriptableObj upgradeTarget, float upgradeTime)
    {
        this.upgradeTarget = upgradeTarget;
        this.upgradeTime = upgradeTime;
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

    private void OnDisable()
    {
        if (upgradeCoroutine != null)
        {
            StopCoroutine(upgradeCoroutine);
            upgradeCoroutine = null;
        }
    }

    public override bool CanPerformTask()
    {
        if (targetBuilding == null || upgradeTarget == null) return false;
        
        // Check if the building has an upgrade target (can be upgraded)
        return upgradeTarget != null;
    }
} 