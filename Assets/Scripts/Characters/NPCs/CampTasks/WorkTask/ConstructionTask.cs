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

    private Coroutine constructionCoroutine;  // Store a reference to the running coroutine

    protected override void Start()
    {
        base.Start();
        workType = WorkType.BUILD_STRUCTURE;
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
            // Start the coroutine and store its reference
            constructionCoroutine = StartCoroutine(ConstructionCoroutine());
        }
    }

    public override Transform WorkTaskTransform()
    {
        return this.transform;
    }

    private IEnumerator ConstructionCoroutine()
    {
        while (currentProgress < baseConstructionTime && workers.Count > 0)
        {
            // Calculate the effective construction time based on the number of workers
            // This gives us diminishing returns for adding more workers
            float effectiveTime = baseConstructionTime / Mathf.Sqrt(workers.Count);

            // Calculate how much time has passed per frame based on effective time
            currentProgress += Time.deltaTime / effectiveTime;

            // Ensure we don't exceed baseConstructionTime
            if (currentProgress >= baseConstructionTime)
            {
                currentProgress = baseConstructionTime;
                break;
            }

            yield return null;
        }

        // When progress reaches or exceeds base construction time, complete the construction
        CompleteConstruction();

        constructionCoroutine = null; // Reset the coroutine reference once it's complete
    }

    public void SetupConstruction(BuildingScriptableObj buildingScriptableObj)
    {
        this.buildingScriptableObj = buildingScriptableObj;
        finalBuildingPrefab = buildingScriptableObj.prefab;
        baseConstructionTime = buildingScriptableObj.constructionTime;
        NavMeshObstacle obstacle = gameObject.GetComponent<NavMeshObstacle>() ?? gameObject.AddComponent<NavMeshObstacle>();
        obstacle.carving = true;
    }

    private void CompleteConstruction()
    {
        // Safely invoke the event
        InvokeStopWork();

        // Instantiate the building
        GameObject buildingObj = Instantiate(finalBuildingPrefab, transform.position, Quaternion.identity);
        Building buildingComponent = buildingObj.GetComponent<Building>();
        if (buildingComponent != null)
        {
            buildingComponent.SetupBuilding(buildingScriptableObj);
            buildingComponent.CompleteConstruction();
        }

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
}


