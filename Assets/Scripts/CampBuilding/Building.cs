using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using Managers;
using System;
using Enemies;

/// <summary>
/// Base class for all buildings in the camp. Handles construction, damage, repair, and upgrade functionality.
/// </summary>
[RequireComponent(typeof(BuildingRepairTask))]
[RequireComponent(typeof(BuildingUpgradeTask))]
public class Building : MonoBehaviour, IInteractive<Building>, IDamageable
{
    #region Serialized Fields
    
    [Header("Building Configuration")]
    [SerializeField] BuildingScriptableObj buildingScriptableObj;

    [Header("Building State")]
    [SerializeField, ReadOnly] protected bool isOperational = false;
    [SerializeField, ReadOnly] protected bool isUnderConstruction = true;
    [SerializeField, ReadOnly] protected float currentHealth;

    [Header("Work Tasks")]
    [SerializeField, ReadOnly] protected BuildingRepairTask repairTask;
    [SerializeField, ReadOnly] protected BuildingUpgradeTask upgradeTask;
    [SerializeField, ReadOnly] protected WorkTask currentWorkTask;

    #endregion

    #region Events
    
    public event System.Action OnBuildingDestroyed;
    public event System.Action OnBuildingRepaired;
    public event System.Action OnBuildingUpgraded;
    public event System.Action<float> OnHealthChanged;
    public event Action<float, float> OnDamageTaken;
    public event Action<float, float> OnHeal;
    public event Action OnDeath;

    #endregion

    #region Properties
    
    public float Health 
    { 
        get => currentHealth; 
        set => currentHealth = Mathf.Clamp(value, 0, MaxHealth); 
    }
    
    private float _maxHealth = 100f;
    public float MaxHealth
    {
        get => buildingScriptableObj != null ? buildingScriptableObj.maxHealth : _maxHealth;
        set
        {
            _maxHealth = value;
            if (buildingScriptableObj != null)
                buildingScriptableObj.maxHealth = value;
        }
    }
    
    public CharacterType CharacterType => CharacterType.NONE;
    public Allegiance GetAllegiance() => Allegiance.FRIENDLY;

    #endregion

    #region Unity Lifecycle

    protected virtual void Start()
    {
        // Override in derived classes if needed
    }

    protected virtual void OnDestroy()
    {
        // Free up grid slots when building is destroyed
        if (buildingScriptableObj != null && CampManager.Instance != null)
        {
            CampManager.Instance.MarkSharedGridSlotsUnoccupied(transform.position, buildingScriptableObj.size);
        }
    }

    #endregion

    #region Building Setup

    public virtual void SetupBuilding(BuildingScriptableObj buildingScriptableObj)
    {
        this.buildingScriptableObj = buildingScriptableObj;
        currentHealth = buildingScriptableObj.maxHealth;

        SetupRepairTask();
        SetupUpgradeTask();
        SetupNavmeshObstacle();
        SetupCollider();
    }

    private void SetupCollider()
    {
        if (GetComponent<Collider>() == null)
        {
            Debug.LogWarning($"{gameObject.name}: Adding BoxCollider");
            gameObject.AddComponent<BoxCollider>();        
        }
    }

    private void SetupNavmeshObstacle()
    {
        NavMeshObstacle obstacle = GetComponent<NavMeshObstacle>();
        if (obstacle == null)
        {
            Debug.LogWarning($"{gameObject.name}: Adding NavMeshObstacle");
            obstacle = gameObject.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.size = new Vector3(buildingScriptableObj.size.x, 1.0f, buildingScriptableObj.size.y);
        }        
    }

    private void SetupRepairTask()
    {
        repairTask = GetComponent<BuildingRepairTask>();
        if (repairTask == null)
        {
            repairTask = gameObject.AddComponent<BuildingRepairTask>();
        }
        repairTask.transform.position = transform.position;
        
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
        
        upgradeTask.SetupUpgradeTask(
            buildingScriptableObj.upgradeTarget,
            buildingScriptableObj.upgradeTime
        );
    }

    #endregion

    #region Building State Management

    public virtual void CompleteConstruction()
    {
        isUnderConstruction = false;
        isOperational = true;
    }

    public bool IsOperational() => isOperational;
    public bool IsUnderConstruction() => isUnderConstruction;
    public float GetHealthPercentage() => currentHealth / MaxHealth;
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => MaxHealth;
    public float GetTaskRadius() => buildingScriptableObj.taskRadius;

    public bool CanInteract()
    {
        return !isUnderConstruction && isOperational;
    }

    #endregion

    #region Damage & Health

    public virtual void TakeDamage(float amount, Transform damageSource = null)
    {
        float previousHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - amount);
        
        OnDamageTaken?.Invoke(amount, currentHealth);
        OnHealthChanged?.Invoke(currentHealth / MaxHealth);
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public virtual void Heal(float amount)
    {
        float previousHealth = currentHealth;
        currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
        
        OnHeal?.Invoke(amount, currentHealth);
        OnHealthChanged?.Invoke(currentHealth / MaxHealth);
        OnBuildingRepaired?.Invoke();
    }

    public void Die()
    {
        OnDeath?.Invoke();
        DestroyBuilding();
    }

    protected virtual void DestroyBuilding()
    {
        OnBuildingDestroyed?.Invoke();
        EnemyBase.NotifyTargetDestroyed(transform);
        Destroy(gameObject);
    }

    #endregion

    #region Building Operations

    public virtual void Upgrade(BuildingScriptableObj newBuildingData)
    {
        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;

        Destroy(gameObject);

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
        UnassignWorkers();

        GameObject destructionPrefab = CampManager.Instance.BuildManager.GetDestructionPrefab(buildingScriptableObj.size);
        if (destructionPrefab != null)
        {
            CreateDestructionTask(destructionPrefab);
        }
    }

    private void UnassignWorkers()
    {
        if (repairTask.IsOccupied)
        {
            repairTask.UnassignNPC();
        }
        if (upgradeTask.IsOccupied)
        {
            upgradeTask.UnassignNPC();
        }
    }

    private void CreateDestructionTask(GameObject destructionPrefab)
    {
        GameObject destructionTaskObj = Instantiate(destructionPrefab, transform.position, transform.rotation);
        
        BuildingDestructionTask destructionTask = destructionTaskObj.AddComponent<BuildingDestructionTask>();
        destructionTask.SetupDestructionTask(this);

        if (CampManager.Instance != null)
        {
            CampManager.Instance.WorkManager.AddWorkTask(destructionTask);
        }
        else
        {
            Debug.LogError("CampManager.Instance is null. Cannot add destruction task.");
        }

        Destroy(gameObject);
    }

    #endregion

    #region Work Task Management

    public BuildingRepairTask GetRepairTask() => repairTask;
    public BuildingUpgradeTask GetUpgradeTask() => upgradeTask;
    public WorkTask GetCurrentWorkTask() => currentWorkTask;

    public void SetCurrentWorkTask(WorkTask workTask)
    {
        currentWorkTask = workTask;
    }

    public bool IsInWorkArea(Vector3 position)
    {
        return Vector3.Distance(position, transform.position) <= buildingScriptableObj.taskRadius;
    }

    #endregion

    #region Interaction

    public virtual string GetInteractionText()
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

    #endregion

    #region Utility Methods

    public BuildingScriptableObj GetBuildingScriptableObj()
    {
        return buildingScriptableObj;
    }

    internal string GetBuildingStatsText()
    {
        return $"Building Stats:\n" +
               $"Health: {currentHealth}/{MaxHealth}\n" +
               $"Repair Time: {buildingScriptableObj.repairTime} seconds\n" +
               $"Upgrade Time: {buildingScriptableObj.upgradeTime} seconds\n" +
               $"Task Radius: {buildingScriptableObj.taskRadius} meters\n" +
               $"Max Health: {MaxHealth}\n" +
               $"Health Restored Per Repair: {buildingScriptableObj.healthRestoredPerRepair}\n" +
               $"Upgrade Target: {buildingScriptableObj.upgradeTarget}\n";
    }

    #endregion
}

