using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using Managers;
using System;

/// <summary>
/// A building is a structure that can be built in the camp.
/// </summary>
/// 
[RequireComponent(typeof(BuildingRepairTask))]
[RequireComponent(typeof(BuildingUpgradeTask))]
public class Building : MonoBehaviour, IInteractive<Building>
{
    [Header("Building Configuration")]
    [SerializeField] BuildingScriptableObj buildingScriptableObj;

    [Header("Current Work Task")]
    [SerializeField, ReadOnly] protected WorkTask currentWorkTask;
    
    [Header("Building State")]
    [SerializeField, ReadOnly] protected bool isOperational = false;
    [SerializeField, ReadOnly] protected bool isUnderConstruction = true;
    [SerializeField, ReadOnly] protected float currentHealth;

    [Header("Repair and Upgrade")]
    [SerializeField, ReadOnly] protected BuildingRepairTask repairTask;
    [SerializeField, ReadOnly] protected BuildingUpgradeTask upgradeTask;

    // Events
    public event System.Action OnBuildingDestroyed;
    public event System.Action OnBuildingRepaired;
    public event System.Action OnBuildingUpgraded;
    public event System.Action<float> OnHealthChanged;

    protected virtual void Start()
    {

    }

    public virtual void SetupBuilding(BuildingScriptableObj buildingScriptableObj)
    {
        this.buildingScriptableObj = buildingScriptableObj;
        currentHealth = buildingScriptableObj.maxHealth;

        // Setup repair and upgrade tasks
        SetupRepairTask();
        SetupUpgradeTask();
        SetupNavmeshObstacle();

        if(GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();        
        }
    }

    private void SetupNavmeshObstacle()
    {
        // Ensure NavMeshObstacle exists and is configured
        NavMeshObstacle obstacle = GetComponent<NavMeshObstacle>();
        if (obstacle == null)
        {
            obstacle = gameObject.AddComponent<NavMeshObstacle>();
        }
        obstacle.carving = true;
        obstacle.size = new Vector3(buildingScriptableObj.size.x, 1.0f, buildingScriptableObj.size.y);
    }

    private void SetupRepairTask()
    {
        repairTask = GetComponent<BuildingRepairTask>();
        if (repairTask == null)
        {
            repairTask = gameObject.AddComponent<BuildingRepairTask>();
        }
        repairTask.transform.position = transform.position;
        
        // Configure repair task with scriptable object parameters
        repairTask.SetupRepairTask(
            buildingScriptableObj.repairTime,
            buildingScriptableObj.healthRestoredPerRepair
        );
    }

    private void SetupUpgradeTask()
    {
        upgradeTask = GetComponent<BuildingUpgradeTask>();
        if (upgradeTask == null)
        {
            upgradeTask = gameObject.AddComponent<BuildingUpgradeTask>();
        }
        upgradeTask.transform.position = transform.position;
        
        // Configure upgrade task with scriptable object parameters
        upgradeTask.SetupUpgradeTask(
            buildingScriptableObj.upgradeTarget,
            buildingScriptableObj.upgradeTime
        );
    }

    public virtual void CompleteConstruction()
    {
        isUnderConstruction = false;
        isOperational = true;
    }

    public virtual void TakeDamage(float damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        OnHealthChanged?.Invoke(currentHealth / buildingScriptableObj.maxHealth);
        
        if (currentHealth <= 0)
        {
            DestroyBuilding();
        }
    }

    public virtual void Repair(float repairAmount)
    {
        currentHealth = Mathf.Min(buildingScriptableObj.maxHealth, currentHealth + repairAmount);
        OnHealthChanged?.Invoke(currentHealth / buildingScriptableObj.maxHealth);
        OnBuildingRepaired?.Invoke();
    }

    public virtual void Upgrade(BuildingScriptableObj newBuildingData)
    {
        // Store position and rotation
        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;

        // Destroy current building
        Destroy(gameObject);

        // Create new building
        GameObject newBuilding = Instantiate(newBuildingData.prefab, position, rotation);
        Building buildingComponent = newBuilding.GetComponent<Building>();
        if (buildingComponent != null)
        {
            buildingComponent.SetupBuilding(newBuildingData);
            buildingComponent.CompleteConstruction();
        }

        OnBuildingUpgraded?.Invoke();
    }

    public virtual void StartDestruction()
    {
        // Unassign any NPCs working on repair or upgrade tasks
        if (repairTask.IsOccupied)
        {
            repairTask.UnassignNPC();
        }
        if (upgradeTask.IsOccupied)
        {
            upgradeTask.UnassignNPC();
        }

        // Get the destruction prefab
        GameObject destructionPrefab = CampManager.Instance.BuildManager.GetDestructionPrefab(buildingScriptableObj.size);
        if (destructionPrefab != null)
        {
            // Create the destruction task object
            GameObject destructionTaskObj = Instantiate(destructionPrefab, transform.position, transform.rotation);
            
            // Add the destruction task component
            BuildingDestructionTask destructionTask = destructionTaskObj.AddComponent<BuildingDestructionTask>();
            destructionTask.SetupDestructionTask(this);

            // Add the destruction task to the work manager
            if (CampManager.Instance != null)
            {
                CampManager.Instance.WorkManager.AddWorkTask(destructionTask);
            }
            else
            {
                Debug.LogError("CampManager.Instance is null. Cannot add destruction task.");
            }

            // Destroy the building
            Destroy(gameObject);
        }
    }

    public BuildingScriptableObj GetBuildingScriptableObj()
    {
        return buildingScriptableObj;
    }

    protected virtual void DestroyBuilding()
    {
        OnBuildingDestroyed?.Invoke();
        // The actual destruction is handled by the BuildingDestructionTask
    }

    // Getters
    public bool IsOperational() => isOperational;
    public bool IsUnderConstruction() => isUnderConstruction;
    public float GetHealthPercentage() => currentHealth / buildingScriptableObj.maxHealth;
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => buildingScriptableObj.maxHealth;
    public float GetTaskRadius() => buildingScriptableObj.taskRadius;

    /// <summary>
    /// Checks if a position is within the work area of this building
    /// </summary>
    public bool IsInWorkArea(Vector3 position)
    {
        return Vector3.Distance(position, transform.position) <= buildingScriptableObj.taskRadius;
    }

    /// <summary>
    /// Gets the repair task for this building
    /// </summary>
    public BuildingRepairTask GetRepairTask() => repairTask;

    /// <summary>
    /// Gets the upgrade task for this building
    /// </summary>
    public BuildingUpgradeTask GetUpgradeTask() => upgradeTask;

    public WorkTask GetCurrentWorkTask() => currentWorkTask;

    public void SetCurrentWorkTask(WorkTask workTask)
    {
        currentWorkTask = workTask;
    }

    public bool CanInteract()
    {
        return !isUnderConstruction && isOperational;
    }

    public string GetInteractionText()
    {
        if (isUnderConstruction) return "Building under construction";
        if (!isOperational) return "Building not operational";
        
        string text = "Building Tasks:\n";
        if (repairTask != null && repairTask.CanPerformTask())
            text += "- Repair\n";
        if (upgradeTask != null && upgradeTask.CanPerformTask())
            text += "- Upgrade\n";
        return text;
    }

    object IInteractiveBase.Interact()
    {
        return Interact();
    }

    public Building Interact()
    {
        return this;
    }

    protected virtual void OnDestroy()
    {
    }

    internal string GetBuildingStatsText()
    {
        return $"Building Stats:\n" +
               $"Health: {currentHealth}/{buildingScriptableObj.maxHealth}\n" +
               $"Repair Time: {buildingScriptableObj.repairTime} seconds\n" +
               $"Upgrade Time: {buildingScriptableObj.upgradeTime} seconds\n" +
               $"Task Radius: {buildingScriptableObj.taskRadius} meters\n" +
               $"Max Health: {buildingScriptableObj.maxHealth}\n" +
               $"Health Restored Per Repair: {buildingScriptableObj.healthRestoredPerRepair}\n" +
               $"Upgrade Target: {buildingScriptableObj.upgradeTarget}\n";
    }
}

