using UnityEngine;
using Managers;

public class BuildingDestructionTask : WorkTask
{
    private Building building;
    private bool isDestroying = false;
    private GameObject destructionGameobj;

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
            // Destroy the building
            Destroy(building.gameObject);

            isDestroying = true;
            // Spawn destruction effect when task starts
            GameObject destructionPrefab = BuildManager.Instance.GetDestructionPrefab(building.GetBuildingScriptableObj().size);
            if (destructionPrefab != null)
            {
                destructionGameobj = Instantiate(destructionPrefab, 
                    building.transform.position, 
                    building.transform.rotation);
            }
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
        if (destructionGameobj != null)
        {
            Destroy(destructionGameobj);
        }
    }
} 