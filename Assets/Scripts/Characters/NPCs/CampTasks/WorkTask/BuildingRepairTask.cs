using UnityEngine;
using System.Collections;
using Managers;

public class BuildingRepairTask : WorkTask
{
    private float repairTime = 20f;
    private float healthRestored = 50f;
    private ResourceItemCount[] requiredResources;

    private float repairProgress = 0f;
    private SettlerNPC currentWorker;
    private Coroutine repairCoroutine;
    private Building targetBuilding;

    protected override void Start()
    {
        base.Start();
        workType = WorkType.REPAIR_BUILDING;
        targetBuilding = GetComponent<Building>();
        if (targetBuilding == null)
        {
            Debug.LogError("BuildingRepairTask requires a Building component on the same GameObject!");
            enabled = false;
        }
    }

    public void SetupRepairTask(float repairTime, float healthRestored, ResourceItemCount[] requiredResources)
    {
        this.repairTime = repairTime;
        this.healthRestored = healthRestored;
        this.requiredResources = requiredResources;
    }

    public override void PerformTask(SettlerNPC npc)
    {
        if (currentWorker == null && HasRequiredResources())
        {
            currentWorker = npc;
            ConsumeResources();
            repairCoroutine = StartCoroutine(RepairCoroutine());
        }
    }

    private IEnumerator RepairCoroutine()
    {
        while (repairProgress < repairTime)
        {
            repairProgress += Time.deltaTime;
            yield return null;
        }

        CompleteRepair();
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

    private void CompleteRepair()
    {
        if (targetBuilding != null)
        {
            targetBuilding.Repair(healthRestored);
            Debug.Log($"Building repair completed! Restored {healthRestored} health");
        }
        
        // Reset state
        repairProgress = 0f;
        currentWorker = null;
        repairCoroutine = null;
        
        // Notify completion
        InvokeStopWork();
    }

    public override Transform WorkTaskTransform()
    {
        return transform;
    }

    private void OnDisable()
    {
        if (repairCoroutine != null)
        {
            StopCoroutine(repairCoroutine);
            repairCoroutine = null;
        }
    }
} 