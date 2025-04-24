using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.AI;

public class ConstructionTask : WorkTask
{
    private GameObject finalBuildingPrefab;
    private BuildingScriptableObj buildingScriptableObj;
    private float baseConstructionTime = 10f;
    private float currentProgress = 0f;
    private List<SettlerNPC> workers = new List<SettlerNPC>();
    private bool isConstructionComplete = false;
    private Coroutine constructionCoroutine;

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
        if (!isConstructionComplete && constructionCoroutine == null)
        {
            constructionCoroutine = StartCoroutine(ConstructionCoroutine());
        }
    }

    private IEnumerator ConstructionCoroutine()
    {
        
        while (currentProgress < baseConstructionTime && workers.Count > 0)
        {
            float effectiveTime = baseConstructionTime / Mathf.Sqrt(workers.Count);
            currentProgress += Time.deltaTime / effectiveTime;

            if (currentProgress >= baseConstructionTime)
            {
                currentProgress = baseConstructionTime;
                break;
            }

            yield return null;
        }

        CompleteConstruction();
        constructionCoroutine = null;
    }

    public void SetupConstruction(BuildingScriptableObj buildingScriptableObj)
    {
        this.buildingScriptableObj = buildingScriptableObj;
        finalBuildingPrefab = buildingScriptableObj.prefab;
        baseConstructionTime = buildingScriptableObj.constructionTime;
    }

    private void CompleteConstruction()
    {
        InvokeStopWork();

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
    }

    public void RemoveWorker(SettlerNPC npc)
    {
        if (workers.Contains(npc))
        {
            workers.Remove(npc);
        }
    }

    private void OnDisable()
    {
        if (constructionCoroutine != null)
        {
            StopCoroutine(constructionCoroutine);
            constructionCoroutine = null;
        }
    }
}


