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

    // No need to override PerformTask - base implementation handles worker list management

    // Remove the custom WorkCoroutine since workers now manage their own work
    // The base class ProcessWork method will handle the work logic

    public void SetupConstruction(PlaceableObjectParent scriptableObj, bool isUpgrade = false)
    {
        this.buildingScriptableObj = scriptableObj;
        finalBuildingPrefab = scriptableObj.prefab;
        // Convert game hours to real seconds using TimeManager
        baseWorkTime = Managers.TimeManager.ConvertGameHoursToSecondsStatic(scriptableObj.constructionTimeInGameHours);
        
        SetupNavMeshObstacle();
    }

    protected override void CompleteWork()
    {
        Debug.Log($"[StructureConstructionTask] CompleteWork called for {buildingScriptableObj?.objectName ?? "Unknown"}");
        Debug.Log($"[StructureConstructionTask] Current workers count: {currentWorkers.Count}");
        
        // Log all current workers before completion
        for (int i = 0; i < currentWorkers.Count; i++)
        {
            var worker = currentWorkers[i];
            Debug.Log($"[StructureConstructionTask] Worker {i}: {worker.name} - Type: {worker.GetType().Name}");
            if (worker is SettlerNPC settler)
            {
                Debug.Log($"[StructureConstructionTask] - Settler current task: {settler.GetCurrentTaskType()}");
                Debug.Log($"[StructureConstructionTask] - Settler assigned work: {settler.GetAssignedWork()?.GetType().Name ?? "null"}");
            }
        }
        
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

        Debug.Log($"[StructureConstructionTask] About to destroy construction site and call base.CompleteWork()");
        Debug.Log($"[StructureConstructionTask] Workers before base.CompleteWork(): {currentWorkers.Count}");
        
        Destroy(gameObject);
        isConstructionComplete = true;
        
        Debug.Log($"[StructureConstructionTask] Calling base.CompleteWork() to notify workers");
        base.CompleteWork();
        
        Debug.Log($"[StructureConstructionTask] Construction completed successfully for {buildingScriptableObj?.objectName ?? "Unknown"}");
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
        return $"Build {buildingScriptableObj?.objectName ?? "Unknown"}";
    }

    public object Interact()
    {
        return this;
    }
}


