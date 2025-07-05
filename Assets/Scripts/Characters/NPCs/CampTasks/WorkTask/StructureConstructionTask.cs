using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using Managers;

/// <summary>
/// Used by construction sites to build buildings.
/// </summary>
public class StructureConstructionTask : WorkTask, IInteractive<object>
{
    private GameObject finalBuildingPrefab;
    private PlaceableObjectParent buildingScriptableObj;
    private List<HumanCharacterController> workers = new List<HumanCharacterController>();
    private bool isConstructionComplete = false;

    protected override void Start()
    {
        base.Start();
        AddWorkTask();
        taskAnimation = TaskAnimation.HAMMER_STANDING;
        
        if (buildingScriptableObj != null)
        {
            SetupNavMeshObstacle();
        }
    }
    
    private void SetupNavMeshObstacle()
    {
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
        if (!workers.Contains(npc))
        {
            workers.Add(npc);
        }

        if (!isConstructionComplete && workCoroutine == null)
        {
            workCoroutine = StartCoroutine(WorkCoroutine());

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

    public void SetupConstruction(PlaceableObjectParent scriptableObj, bool isUpgrade = false)
    {
        this.buildingScriptableObj = scriptableObj;
        finalBuildingPrefab = scriptableObj.prefab;
        baseWorkTime = scriptableObj.constructionTime;
        
        SetupNavMeshObstacle();
    }

    protected override void CompleteWork()
    {
        // Free grid slots from construction site
        if (CampManager.Instance != null)
        {
            CampManager.Instance.MarkSharedGridSlotsUnoccupied(transform.position, buildingScriptableObj.size);
        }

        // Create the new structure
        GameObject structureObj = Instantiate(finalBuildingPrefab, transform.position, Quaternion.identity);
        PlaceableStructure structureComponent = structureObj.GetComponent<PlaceableStructure>();
        
        if (structureComponent == null)
        {
            Debug.LogError($"Structure prefab {finalBuildingPrefab.name} must have a PlaceableStructure component!");
            Destroy(structureObj);
            return;
        }
        
        // Setup the structure with the scriptable object
        structureComponent.SetupStructure(buildingScriptableObj);
        structureComponent.CompleteConstruction();
        
        // Trigger upgrade event for all constructions (since this is used for both new builds and upgrades)
        structureComponent.TriggerUpgradeEvent();

        // Occupy grid slots with new structure
        if (CampManager.Instance != null)
        {
            CampManager.Instance.MarkSharedGridSlotsOccupied(transform.position, buildingScriptableObj.size, structureObj);
        }

        Destroy(gameObject);
        isConstructionComplete = true;
        
        base.CompleteWork();
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
        return $"Build {buildingScriptableObj.objectName}";
    }

    public object Interact()
    {
        return this;
    }
}


