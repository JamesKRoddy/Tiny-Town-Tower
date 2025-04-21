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
        workType = WorkType.BUILD_STRUCTURE;
        Debug.Log($"[ConstructionTask] {gameObject.name} initialized");

        NavMeshObstacle obstacle = GetComponent<NavMeshObstacle>();
        if (obstacle == null)
        {
            obstacle = gameObject.AddComponent<NavMeshObstacle>();
        }
        obstacle.carving = true;
    }

    public override void PerformTask(SettlerNPC npc)
    {
        Debug.Log($"[ConstructionTask] {gameObject.name} performing task with worker {npc.gameObject.name}");
        
        // Add worker to the task
        if (!workers.Contains(npc))
        {
            workers.Add(npc);
            Debug.Log($"[ConstructionTask] {gameObject.name} added worker {npc.gameObject.name}. Total workers: {workers.Count}");
        }

        // Start the construction process if not already started
        if (!isConstructionComplete && constructionCoroutine == null)
        {
            Debug.Log($"[ConstructionTask] {gameObject.name} starting construction with {workers.Count} workers");
            constructionCoroutine = StartCoroutine(ConstructionCoroutine());
        }
    }

    public override Transform WorkTaskTransform()
    {
        return this.transform;
    }

    private IEnumerator ConstructionCoroutine()
    {
        Debug.Log($"[ConstructionTask] {gameObject.name} construction coroutine started");
        
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

        Debug.Log($"[ConstructionTask] {gameObject.name} construction complete");
        CompleteConstruction();
        constructionCoroutine = null;
    }

    public void SetupConstruction(BuildingScriptableObj buildingScriptableObj)
    {
        Debug.Log($"[ConstructionTask] {gameObject.name} setting up construction for {buildingScriptableObj.name}");
        this.buildingScriptableObj = buildingScriptableObj;
        finalBuildingPrefab = buildingScriptableObj.prefab;
        baseConstructionTime = buildingScriptableObj.constructionTime;
    }

    private void CompleteConstruction()
    {
        Debug.Log($"[ConstructionTask] {gameObject.name} completing construction");
        
        InvokeStopWork();

        GameObject buildingObj = Instantiate(finalBuildingPrefab, transform.position, Quaternion.identity);
        Building buildingComponent = buildingObj.GetComponent<Building>();
        if (buildingComponent != null)
        {
            buildingComponent.SetupBuilding(buildingScriptableObj);
            buildingComponent.CompleteConstruction();
            Debug.Log($"[ConstructionTask] {gameObject.name} building instantiated and set up");
        }
        else
        {
            Debug.LogError($"[ConstructionTask] {gameObject.name} failed to get Building component from prefab");
        }

        Destroy(gameObject);
        isConstructionComplete = true;
    }

    public void RemoveWorker(SettlerNPC npc)
    {
        if (workers.Contains(npc))
        {
            workers.Remove(npc);
            Debug.Log($"[ConstructionTask] {gameObject.name} removed worker {npc.gameObject.name}. Remaining workers: {workers.Count}");
        }
    }

    private void OnDisable()
    {
        if (constructionCoroutine != null)
        {
            Debug.Log($"[ConstructionTask] {gameObject.name} construction coroutine stopped due to disable");
            StopCoroutine(constructionCoroutine);
            constructionCoroutine = null;
        }
    }
}


