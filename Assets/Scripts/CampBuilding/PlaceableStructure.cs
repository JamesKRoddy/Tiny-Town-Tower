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
public abstract class PlaceableStructure : MonoBehaviour, IDamageable
{
    #region Serialized Fields
    
    [Header("Structure Configuration")]
    [SerializeField] public PlaceableObjectParent structureScriptableObj;

    [Header("Structure State")]
    [SerializeField, ReadOnly] protected bool isOperational = false;
    [SerializeField, ReadOnly] protected bool isUnderConstruction = true;
    [SerializeField, ReadOnly] protected float currentHealth;
    [SerializeField, ReadOnly] protected WorkTask currentWorkTask;

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

    #region Properties
    
    public float Health 
    { 
        get => currentHealth; 
        set => currentHealth = Mathf.Clamp(value, 0, MaxHealth); 
    }
    
    public virtual float MaxHealth
    {
        get => structureScriptableObj != null ? GetMaxHealthFromScriptableObject() : 100f;
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

    public virtual void SetupStructure(PlaceableObjectParent scriptableObj)
    {
        this.structureScriptableObj = scriptableObj;
        currentHealth = GetMaxHealthFromScriptableObject();

        SetupNavmeshObstacle();
        SetupCollider();
        OnStructureSetup();
    }

    protected virtual void OnStructureSetup()
    {
        // Override in derived classes for additional setup
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
            obstacle = gameObject.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.size = new Vector3(structureScriptableObj.size.x, 1.0f, structureScriptableObj.size.y);
        }
        else
        {
            // Update existing obstacle size if needed
            obstacle.carving = true;
            obstacle.size = new Vector3(structureScriptableObj.size.x, 1.0f, structureScriptableObj.size.y);
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
        // Base implementation - can be overridden by derived classes
        // This method is called when a structure should be destroyed
        Debug.Log($"{gameObject.name} starting destruction process");
    }

    #endregion

    #region Utility Methods

    protected virtual float GetMaxHealthFromScriptableObject()
    {
        return structureScriptableObj != null ? structureScriptableObj.maxHealth : 100f;
    }

    public PlaceableObjectParent GetStructureScriptableObj()
    {
        return structureScriptableObj;
    }

    public virtual string GetInteractionText()
    {
        if (isUnderConstruction) return "Structure under construction";
        if (!isOperational) return "Structure not operational";
        
        string text = "Structure Options:\n";
        if (CanUpgrade())
            text += "- Upgrade\n";
        return text;
    }

    #endregion
} 