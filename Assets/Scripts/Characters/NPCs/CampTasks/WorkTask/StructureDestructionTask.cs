using UnityEngine;
using UnityEngine.AI;

public class StructureDestructionTask : WorkTask, IInteractive<object>
{
    private PlaceableObjectParent structureScriptableObj;

    protected override void Start()
    {
        base.Start();
        
        taskType = WorkTaskType.Complete; // Destruction is a one-time task
        
        // Destruction tasks should support multiple workers and be automatically queued
        maxWorkers = 3; // Allow up to 3 workers on destruction sites
        autoQueue = true;
        
        taskAnimation = TaskAnimation.HAMMER_STANDING;
    }

    public void SetupDestructionTask<T>(PlaceableStructure<T> structure) where T : PlaceableObjectParent
    {
        this.structureScriptableObj = structure.GetStructureScriptableObj();
        // Convert game hours to real seconds using TimeManager
        baseWorkTime = Managers.TimeManager.ConvertGameHoursToSecondsStatic(structureScriptableObj.destructionTimeInGameHours);
        requiredResources = new ResourceItemCount[0]; // No resources required for destruction
        AddWorkTask();

        // Setup NavMeshObstacle on this object
        NavMeshObstacle obstacle = GetComponent<NavMeshObstacle>();
        if (obstacle == null)
        {
            obstacle = gameObject.AddComponent<NavMeshObstacle>();
        }
        obstacle.carving = true;
        obstacle.size = new Vector3(structureScriptableObj.size.x, 1.0f, structureScriptableObj.size.y);
    }

    // No need to override PerformTask - base implementation handles worker list management

    // Remove the custom WorkCoroutine since workers now manage their own work
    // The base class ProcessWork method will handle the work logic

    protected override void CompleteWork()
    {
        if (structureScriptableObj?.reclaimedResources != null)
        {
            foreach (var resource in structureScriptableObj.reclaimedResources)
            {
                if (resource.resourceScriptableObj != null)
                {
                    PlayerInventory.Instance.AddItem(resource.resourceScriptableObj, resource.count);
                }
            }
        }

        base.CompleteWork();
        Destroy(gameObject);
    }

    public void RemoveWorker(SettlerNPC npc)
    {
        RemoveWorker(npc as HumanCharacterController);
    }

    public bool CanInteract()
    {
        return true;
    }

    public string GetInteractionText()
    {
        return "Destroy " + (structureScriptableObj?.objectName ?? "Structure");
    }

    public object Interact()
    {
        return this;
    }


} 