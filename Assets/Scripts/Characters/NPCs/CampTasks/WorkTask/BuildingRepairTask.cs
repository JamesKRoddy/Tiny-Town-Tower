using UnityEngine;
using System.Collections;
using Managers;

public class BuildingRepairTask : WorkTask
{
    private float repairTime = 20f;
    private float healthRestored = 50f;
    private ResourceScriptableObj[] requiredResources;
    private int[] resourceCosts;

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

    public void SetupRepairTask(float repairTime, float healthRestored, ResourceScriptableObj[] requiredResources, int[] resourceCosts)
    {
        this.repairTime = repairTime;
        this.healthRestored = healthRestored;
        this.requiredResources = requiredResources;
        this.resourceCosts = resourceCosts;
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