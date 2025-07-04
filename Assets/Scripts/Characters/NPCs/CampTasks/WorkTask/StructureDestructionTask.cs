using UnityEngine;
using Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public class StructureDestructionTask : WorkTask, IInteractive<object>
{
    private PlaceableObjectParent structureScriptableObj;
    private List<HumanCharacterController> workers = new List<HumanCharacterController>();
    private bool isDestructionComplete = false;
    protected override void Start()
    {
        base.Start();
        taskAnimation = TaskAnimation.HAMMER_STANDING;
    }

    public void SetupDestructionTask(PlaceableStructure structure)
    {
        this.structureScriptableObj = structure.GetStructureScriptableObj();
        baseWorkTime = structureScriptableObj.destructionTime;
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

    public override void PerformTask(HumanCharacterController npc)
    {
        // Add worker to the task
        if (!workers.Contains(npc))
        {
            workers.Add(npc);
        }

        // Start the destruction process if not already started
        if (!isDestructionComplete && workCoroutine == null)
        {
            workCoroutine = StartCoroutine(WorkCoroutine());

            // Register electricity consumption when the task starts
            if (electricityRequired > 0)
            {
                CampManager.Instance.ElectricityManager.RegisterBuildingConsumption(this, electricityRequired);
            }
        }
    }

    protected override IEnumerator WorkCoroutine()
    {
        while (workProgress < baseWorkTime && workers.Count > 0)
        {
            float workSpeed = Mathf.Sqrt(workers.Count);
            workProgress += Time.deltaTime * workSpeed;

            if (workProgress >= baseWorkTime)
            {
                workProgress = baseWorkTime;
                break;
            }

            yield return null;
        }

        CompleteWork();
    }

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

        isDestructionComplete = true;
        base.CompleteWork();
        Destroy(gameObject);
    }

    public void RemoveWorker(SettlerNPC npc)
    {
        if (workers.Contains(npc))
        {
            workers.Remove(npc);
        }
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