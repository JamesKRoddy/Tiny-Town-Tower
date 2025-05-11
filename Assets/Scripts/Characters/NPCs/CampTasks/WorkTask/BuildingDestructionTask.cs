using UnityEngine;
using Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public class BuildingDestructionTask : WorkTask, IInteractive<object>
{
    private BuildingScriptableObj buildingScriptableObj;
    private List<HumanCharacterController> workers = new List<HumanCharacterController>();
    private bool isDestructionComplete = false;
    private GameObject destructionGameobj;

    protected override void Start()
    {
        base.Start();
    }

    public void SetupDestructionTask(Building building)
    {
        this.buildingScriptableObj = building.GetBuildingScriptableObj();
        baseWorkTime = buildingScriptableObj.destructionTime;
        requiredResources = new ResourceItemCount[0]; // No resources required for destruction
        AddWorkTask();

        // Setup NavMeshObstacle on this object
        NavMeshObstacle obstacle = GetComponent<NavMeshObstacle>();
        if (obstacle == null)
        {
            obstacle = gameObject.AddComponent<NavMeshObstacle>();
        }
        obstacle.carving = true;
        obstacle.size = new Vector3(buildingScriptableObj.size.x, 1.0f, buildingScriptableObj.size.y);
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
        }
    }

    protected override IEnumerator WorkCoroutine()
    {
        while (workProgress < baseWorkTime && workers.Count > 0)
        {
            float effectiveTime = baseWorkTime / Mathf.Sqrt(workers.Count);
            workProgress += Time.deltaTime / effectiveTime;

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
        foreach (var resource in buildingScriptableObj.reclaimedResources)
        {
            PlayerInventory.Instance.AddItem(resource.resourceScriptableObj, resource.count);
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
        return "Destroy " + buildingScriptableObj.name;
    }

    public object Interact()
    {
        return this;
    }

    public override string GetAnimationClipName()
    {
        return TaskAnimation.DESTROY_STRUCTURE.ToString();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (destructionGameobj != null)
        {
            Destroy(destructionGameobj);
        }
    }
} 