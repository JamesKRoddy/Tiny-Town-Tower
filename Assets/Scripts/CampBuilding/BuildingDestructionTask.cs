using UnityEngine;
using Managers;

public class BuildingDestructionTask : WorkTask
{
    private Building building;
    private bool isDestroying = false;

    protected override void Start()
    {
        base.Start();
        workType = WorkType.DESTROY_STRUCTURE;
        building = GetComponent<Building>();
        if (building == null)
        {
            Debug.LogError("BuildingDestructionTask requires a Building component on the same GameObject!");
            enabled = false;
        }
    }

    public void SetupDestructionTask(Building building)
    {
        this.building = building;
        baseWorkTime = building.GetBuildingScriptableObj().destructionTime;
        requiredResources = new ResourceItemCount[0]; // No resources required for destruction
        AddWorkTask();
    }

    public override void PerformTask(SettlerNPC npc)
    {
        if (!isDestroying)
        {
            isDestroying = true;
            base.PerformTask(npc);
        }
    }

    protected override void CompleteWork()
    {
        if (building != null)
        {
            // Reclaim resources
            foreach (var resource in building.GetBuildingScriptableObj().reclaimedResources)
            {
                PlayerInventory.Instance.AddItem(resource.resourceScriptableObj, resource.count);
            }

            // Spawn destruction prefab
            if (building.GetBuildingScriptableObj().destructionPrefab != null)
            {
                Instantiate(building.GetBuildingScriptableObj().destructionPrefab, 
                    building.transform.position, 
                    building.transform.rotation);
            }

            // Destroy the building
            Destroy(building.gameObject);
        }

        base.CompleteWork();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (building != null)
        {
            building = null;
        }
    }
} 