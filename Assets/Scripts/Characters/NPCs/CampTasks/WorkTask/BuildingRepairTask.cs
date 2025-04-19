using UnityEngine;
using System.Collections;

public class BuildingRepairTask : WorkTask
{
    [SerializeField] private float repairTime = 20f;
    [SerializeField] private float healthRestored = 50f;
    [SerializeField] private ResourceScriptableObj[] requiredResources;
    [SerializeField] private int[] resourceCosts;

    private float repairProgress = 0f;
    private SettlerNPC currentWorker;
    private Coroutine repairCoroutine;

    protected override void Start()
    {
        base.Start();
        workType = WorkType.REPAIR_BUILDING;
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
        return CampInventory.Instance.HasResources(requiredResources, resourceCosts);
    }

    private void ConsumeResources()
    {
        CampInventory.Instance.ConsumeResources(requiredResources, resourceCosts);
    }

    private void CompleteRepair()
    {
        // Restore health to the building
        BuildingHealth buildingHealth = GetComponent<BuildingHealth>();
        if (buildingHealth != null)
        {
            buildingHealth.RestoreHealth(healthRestored);
        }
        
        Debug.Log($"Building repair completed! Restored {healthRestored} health");
        
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