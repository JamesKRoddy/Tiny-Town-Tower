using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Managers;
using Enemies;

/// <summary>
/// Base class for all placeable structures (buildings and turrets) in the camp.
/// Handles common functionality like construction, damage, repair, and upgrade.
/// </summary>
[RequireComponent(typeof(StructureRepairTask))]
[RequireComponent(typeof(StructureUpgradeTask))]
public abstract class PlaceableStructure<T> : MonoBehaviour, IDamageable, IPlaceableStructure where T : PlaceableObjectParent
{
    #region Serialized Fields
    
    [Header("Structure Configuration")]
    [SerializeField] private T structureScriptableObj;

    [Header("Structure State")]
    [SerializeField, ReadOnly] protected bool isOperational = false;
    [SerializeField, ReadOnly] protected bool isUnderConstruction = true;
    [SerializeField, ReadOnly] protected float currentHealth;
    [SerializeField, ReadOnly] protected WorkTask currentWorkTask;

    [Header("Work Tasks")]
    [SerializeField, ReadOnly] protected StructureRepairTask repairTask;
    [SerializeField, ReadOnly] protected StructureUpgradeTask upgradeTask;

    #endregion

    #region Properties
    
    protected T StructureScriptableObj => structureScriptableObj;
    
    public float Health 
    { 
        get => currentHealth; 
        set => currentHealth = Mathf.Clamp(value, 0, MaxHealth); 
    }
    
    public virtual float MaxHealth
    {
        get => structureScriptableObj != null ? structureScriptableObj.maxHealth : 100f;
        set { /* Override in derived classes if needed */ }
    }
    
    public CharacterType CharacterType => CharacterType.NONE;
    public Allegiance GetAllegiance() => Allegiance.FRIENDLY;
    public WorkTask GetCurrentWorkTask() => currentWorkTask;

    #endregion

    #region Unity Lifecycle

    protected virtual void Start()
    {
        // Override in derived classes if needed
    }

    protected virtual void OnDestroy()
    {
        // Unregister from CampManager target tracking
        if (CampManager.Instance != null)
        {
            CampManager.Instance.UnregisterTarget(this);
        }
        
        // Free up grid slots when structure is destroyed
        if (structureScriptableObj != null && CampManager.Instance != null)
        {
            CampManager.Instance.MarkSharedGridSlotsUnoccupied(transform.position, structureScriptableObj.size);
        }
    }

    #endregion

    #region Structure Setup

    public virtual void SetupStructure(T scriptableObj)
    {
        this.structureScriptableObj = scriptableObj;
        currentHealth = GetMaxHealthFromScriptableObject();

        SetupNavmeshObstacle();
        SetupCollider();
        OnStructureSetup();
    }

    protected virtual void OnStructureSetup()
    {
        SetupRepairTask();
        SetupUpgradeTask();
    }

    private void SetupCollider()
    {
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();        
        }
    }

    private void SetupNavmeshObstacle()
    {
        NavMeshObstacle obstacle = GetComponent<NavMeshObstacle>();
        if (obstacle == null)
        {
            Debug.LogWarning($"Adding NavMeshObstacle to {gameObject.name}");
            obstacle = gameObject.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.size = new Vector3(structureScriptableObj.size.x, 1.0f, structureScriptableObj.size.y);
        }
        else
        {
            obstacle.carving = true;
        }        
    }

    private void SetupRepairTask()
    {
        repairTask = GetComponent<StructureRepairTask>();
        if (repairTask == null)
        {
            repairTask = gameObject.AddComponent<StructureRepairTask>();
        }
        repairTask.transform.position = transform.position;
        
        repairTask.SetupRepairTask(
            StructureScriptableObj.repairTime,
            StructureScriptableObj.healthRestoredPerRepair
        );
    }

    private void SetupUpgradeTask()
    {
        upgradeTask = GetComponent<StructureUpgradeTask>();
        if (upgradeTask == null)
        {
            upgradeTask = gameObject.AddComponent<StructureUpgradeTask>();
        }
        upgradeTask.transform.position = transform.position;
        
        // Set up the upgrade task with the upgrade target
        if (StructureScriptableObj.upgradeTarget != null)
        {
            upgradeTask.SetupUpgradeTask(StructureScriptableObj.upgradeTarget);
        }
    }

    #endregion

    #region Structure State Management

    public virtual void CompleteConstruction()
    {
        isUnderConstruction = false;
        isOperational = true;
        
        // Register with CampManager for target tracking
        if (CampManager.Instance != null)
        {
            CampManager.Instance.RegisterTarget(this);
        }
    }

    public bool IsOperational() => isOperational;
    public bool IsUnderConstruction() => isUnderConstruction;
    public float GetHealthPercentage() => currentHealth / MaxHealth;
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => MaxHealth;

    public virtual bool CanInteract()
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
        
        // Play hit VFX
        Vector3 hitPoint = transform.position + Vector3.up * 1.5f;
        Vector3 hitNormal = damageSource != null 
            ? (transform.position - damageSource.position).normalized 
            : Vector3.up;
        EffectManager.Instance?.PlayHitEffect(hitPoint, hitNormal, this);
        
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
        OnStructureRepaired?.Invoke();
    }

    public virtual void Die()
    {
        Debug.Log($"{gameObject.name} has been destroyed!");
        OnDeath?.Invoke();
        OnStructureDestroyed?.Invoke();
        
        // Notify all enemies that this structure was destroyed
        EnemyBase.NotifyTargetDestroyed(transform);
        
        // Destroy the structure
        Destroy(gameObject);
    }

    #endregion

    #region Events
    
    public event Action OnStructureDestroyed;
    public event Action OnStructureRepaired;
    public event Action OnStructureUpgraded;
    public event Action<float> OnHealthChanged;
    public event Action<float, float> OnDamageTaken;
    public event Action<float, float> OnHeal;
    public event Action OnDeath;

    #endregion

    #region Work Task Management
    
    public virtual void SetCurrentWorkTask(WorkTask workTask)
    {
        currentWorkTask = workTask;
    }
    
    #endregion

    #region Upgrade System

    public virtual bool CanUpgrade()
    {
        return structureScriptableObj != null && 
               structureScriptableObj.upgradeTarget != null;
    }

    public void TriggerUpgradeEvent()
    {
        OnStructureUpgraded?.Invoke();
    }

    public void TriggerRepairEvent()
    {
        OnStructureRepaired?.Invoke();
    }

    public void TriggerDestroyEvent()
    {
        OnStructureDestroyed?.Invoke();
    }

    #endregion

    #region Destruction

    public virtual void StartDestruction()
    {
        UnassignWorkers();

        GameObject destructionPrefab = CampManager.Instance.BuildManager.GetDestructionPrefab(StructureScriptableObj.size);
        if (destructionPrefab != null)
        {
            CreateDestructionTask(destructionPrefab);
        }
    }

    private void UnassignWorkers()
    {
        if (repairTask != null && repairTask.IsOccupied)
        {
            repairTask.UnassignNPC();
        }
        if (upgradeTask != null && upgradeTask.IsOccupied)
        {
            upgradeTask.UnassignNPC();
        }
    }

    private void CreateDestructionTask(GameObject destructionPrefab)
    {
        GameObject destructionTaskObj = Instantiate(destructionPrefab, transform.position, transform.rotation);
        
        StructureDestructionTask destructionTask = destructionTaskObj.AddComponent<StructureDestructionTask>();
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

    #region Utility Methods

    protected virtual float GetMaxHealthFromScriptableObject()
    {
        return structureScriptableObj != null ? structureScriptableObj.maxHealth : 100f;
    }

    public T GetStructureScriptableObj()
    {
        return structureScriptableObj;
    }

    PlaceableObjectParent IPlaceableStructure.GetStructureScriptableObj()
    {
        return structureScriptableObj;
    }

    public virtual string GetInteractionText()
    {
        if (isUnderConstruction) return "Structure under construction";
        if (!isOperational) return "Structure not operational";
        
        string text = "Structure Options:\n";
        if (repairTask != null && repairTask.CanPerformTask())
            text += "- Repair\n";
        if (upgradeTask != null && upgradeTask.CanPerformTask())
            text += "- Upgrade\n";
        if (CanUpgrade())
            text += "- Upgrade to Next Level\n";
        return text;
    }

    public StructureRepairTask GetRepairTask() => repairTask;
    public StructureUpgradeTask GetUpgradeTask() => upgradeTask;

    #endregion
}

 