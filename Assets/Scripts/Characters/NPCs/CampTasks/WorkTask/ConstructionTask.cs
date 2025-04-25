using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.AI;

/// <summary>
/// Used by construction sites to build buildings.
/// </summary>
public class ConstructionTask : WorkTask
{
    private GameObject finalBuildingPrefab;
    private BuildingScriptableObj buildingScriptableObj;
    private List<SettlerNPC> workers = new List<SettlerNPC>();
    private bool isConstructionComplete = false;

    protected override void Start()
    {
        base.Start();
        AddWorkTask();
        workType = WorkType.BUILD_STRUCTURE;

        NavMeshObstacle obstacle = GetComponent<NavMeshObstacle>();
        if (obstacle == null)
        {
            obstacle = gameObject.AddComponent<NavMeshObstacle>();
        }
        obstacle.carving = true;
        obstacle.size = new Vector3(buildingScriptableObj.size.x, 1.0f, buildingScriptableObj.size.y);
    }

    public override void PerformTask(SettlerNPC npc)
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

    public void SetupConstruction(BuildingScriptableObj buildingScriptableObj)
    {
        this.buildingScriptableObj = buildingScriptableObj;
        finalBuildingPrefab = buildingScriptableObj.prefab;
        baseWorkTime = buildingScriptableObj.constructionTime;
    }

    protected override void CompleteWork()
    {
        GameObject buildingObj = Instantiate(finalBuildingPrefab, transform.position, Quaternion.identity);
        Building buildingComponent = buildingObj.GetComponent<Building>();
        if (buildingComponent == null)
        {
            buildingComponent = buildingObj.AddComponent<Building>();
        }
        
        buildingComponent.SetupBuilding(buildingScriptableObj); //Enable all the tasks here
        buildingComponent.CompleteConstruction();

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
}


