using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;

/// <summary>
/// A building is a structure that can be built in the camp.
/// This is an abstract base class that all building types should inherit from.
/// </summary>
public abstract class Building : MonoBehaviour
{
    [Header("Building Configuration")]
    [SerializeField, ReadOnly] BuildingScriptableObj buildingScriptableObj;
    
    [Header("Building State")]
    [SerializeField] protected bool isOperational = false;
    [SerializeField] protected bool isUnderConstruction = true;
    [SerializeField] protected float currentHealth;

    [Header("Repair and Upgrade")]
    [SerializeField, ReadOnly] protected BuildingRepairTask repairTask;
    [SerializeField, ReadOnly] protected BuildingUpgradeTask upgradeTask;

    // Events
    public event System.Action OnBuildingDestroyed;
    public event System.Action OnBuildingRepaired;
    public event System.Action OnBuildingUpgraded;
    public event System.Action<float> OnHealthChanged;

    protected virtual void Awake()
    {
        // Add NavMeshObstacle for pathfinding
        NavMeshObstacle obstacle = gameObject.GetComponent<NavMeshObstacle>() ?? gameObject.AddComponent<NavMeshObstacle>();
        obstacle.carving = true;
    }

    public virtual void SetupBuilding(BuildingScriptableObj buildingScriptableObj)
    {
        this.buildingScriptableObj = buildingScriptableObj;
        currentHealth = buildingScriptableObj.maxHealth;

        // Setup repair and upgrade tasks
        SetupRepairTask();
        SetupUpgradeTask();
    }

    private void SetupRepairTask()
    {
        if (repairTask == null)
        {
            repairTask = gameObject.AddComponent<BuildingRepairTask>();
        }
        repairTask.transform.position = transform.position;
        
        // Configure repair task with scriptable object parameters
        repairTask.SetupRepairTask(
            buildingScriptableObj.repairTime,
            buildingScriptableObj.healthRestoredPerRepair,
            buildingScriptableObj.repairResources,
            buildingScriptableObj.repairResourceCosts
        );
    }

    private void SetupUpgradeTask()
    {
        if (upgradeTask == null)
        {
            upgradeTask = gameObject.AddComponent<BuildingUpgradeTask>();
        }
        upgradeTask.transform.position = transform.position;
        
        // Configure upgrade task with scriptable object parameters
        upgradeTask.SetupUpgradeTask(
            buildingScriptableObj.upgradeTarget,
            buildingScriptableObj.upgradeTime,
            buildingScriptableObj.upgradeResources,
            buildingScriptableObj.upgradeResourceCosts
        );
    }

    public virtual void CompleteConstruction()
    {
        isUnderConstruction = false;
        isOperational = true;
        // Additional completion logic can be added in derived classes
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

    protected virtual void DestroyBuilding()
    {
        OnBuildingDestroyed?.Invoke();
        Destroy(gameObject);
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
}

