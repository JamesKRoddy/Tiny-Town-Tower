using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class ConstructionSite : WorkTask
{
    private GameObject finalBuildingPrefab;
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
        finalBuildingPrefab = buildingScriptableObj.buildingPrefab;
        baseConstructionTime = buildingScriptableObj.constructionTime;
    }

    private void CompleteConstruction()
    {
        // Safely invoke the event
        InvokeStopWork();  // Use the helper method to trigger the event

        Instantiate(finalBuildingPrefab, transform.position, Quaternion.identity);
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


