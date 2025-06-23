using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.AI;
using Managers;

/// <summary>
/// Used by construction sites to build turrets in the camp.
/// </summary>
public class TurretConstructionTask : WorkTask, IInteractive<object>
{
    private GameObject finalTurretPrefab;
    private TurretScriptableObject turretScriptableObj;
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
        obstacle.size = new Vector3(turretScriptableObj.size.x, 1.0f, turretScriptableObj.size.y);
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

    public void SetupConstruction(TurretScriptableObject turretScriptableObj)
    {
        this.turretScriptableObj = turretScriptableObj;
        finalTurretPrefab = turretScriptableObj.prefab;
        baseWorkTime = turretScriptableObj.constructionTime;
    }

    protected override void CompleteWork()
    {
        GameObject turretObj = Instantiate(finalTurretPrefab, transform.position, Quaternion.identity);
        
        // Try to setup the turret if it has a BaseTurret component
        var turretComponent = turretObj.GetComponent<MonoBehaviour>();
        if (turretComponent != null && turretComponent.GetType().Name == "BaseTurret")
        {
            // Use reflection to call SetupTurret if it exists
            var setupMethod = turretComponent.GetType().GetMethod("SetupTurret", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            setupMethod?.Invoke(turretComponent, null);
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
        return "Build " + turretScriptableObj.name;
    }

    public object Interact()
    {
        return this;
    }
} 