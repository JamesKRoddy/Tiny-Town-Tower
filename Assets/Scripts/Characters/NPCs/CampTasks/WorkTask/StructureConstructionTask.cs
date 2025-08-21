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
        
        // Try to get the specific structure component based on the scriptable object type
        if (buildingScriptableObj is BuildingScriptableObj buildingSO)
        {
            Building buildingComponent = structureObj.GetComponent<Building>();
            if (buildingComponent == null)
            {
                Debug.LogError($"Building prefab {finalBuildingPrefab.name} must have a Building component!");
                Destroy(structureObj);
                return;
            }
            buildingComponent.SetupStructure(buildingSO);
            buildingComponent.CompleteConstruction();
        }
        else if (buildingScriptableObj is TurretScriptableObject turretSO)
        {
            BaseTurret turretComponent = structureObj.GetComponent<BaseTurret>();
            if (turretComponent == null)
            {
                Debug.LogError($"Turret prefab {finalBuildingPrefab.name} must have a BaseTurret component!");
                Destroy(structureObj);
                return;
            }
            turretComponent.SetupStructure(turretSO);
            turretComponent.CompleteConstruction();
        }
        else
        {
            Debug.LogError($"Unknown scriptable object type: {buildingScriptableObj.GetType()}");
            Destroy(structureObj);
            return;
        }
        
        // Get the structure component for triggering events and grid management
        IPlaceableStructure structureComponent = structureObj.GetComponent<IPlaceableStructure>();
        if (structureComponent != null)
        {
            // Trigger upgrade event for all constructions (since this is used for both new builds and upgrades)
            structureComponent.TriggerUpgradeEvent();
        }

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


