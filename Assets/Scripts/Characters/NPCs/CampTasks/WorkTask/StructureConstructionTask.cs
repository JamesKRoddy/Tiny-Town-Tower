using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
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

        // Start the construction process if not already started
        if (!isConstructionComplete && workCoroutine == null)
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
            float effectiveTime = baseWorkTime / workers.Count;
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

    public void SetupConstruction(PlaceableObjectParent scriptableObj)
    {
        this.buildingScriptableObj = scriptableObj;
        finalBuildingPrefab = scriptableObj.prefab;
        baseWorkTime = scriptableObj.constructionTime;
    }

    protected override void CompleteWork()
    {
        GameObject structureObj = Instantiate(finalBuildingPrefab, transform.position, Quaternion.identity);
        
        // Handle both buildings and turrets
        if (buildingScriptableObj is BuildingScriptableObj buildingSO)
        {
            Building buildingComponent = structureObj.GetComponent<Building>();
            if (buildingComponent == null)
            {
                buildingComponent = structureObj.AddComponent<Building>();
            }
            
            buildingComponent.SetupBuilding(buildingSO);
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
            
            turretComponent.SetupTurret(turretSO);
            turretComponent.CompleteConstruction();
        }
        else
        {
            Debug.LogError($"Unknown scriptable object type: {buildingScriptableObj.GetType()}");
            Destroy(structureObj);
            return;
        }

        // Transfer grid slot occupation from construction site to the new structure
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
        return "Build " + buildingScriptableObj.name;
    }

    public object Interact()
    {
        return this;
    }

    protected override void OnDestroy()
    {
        // Grid slots are now properly transferred in CompleteWork, so we don't need to free them here
        base.OnDestroy();
    }
}


