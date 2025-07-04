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
public class Building : PlaceableStructure, IInteractive<Building>
{
    #region Serialized Fields
    
    [Header("Building Configuration")]
    [SerializeField] BuildingScriptableObj buildingScriptableObj;

    [Header("Building State")]
    // Note: isOperational, isUnderConstruction, and currentHealth are inherited from PlaceableStructure

    [Header("Work Tasks")]
    [SerializeField, ReadOnly] protected BuildingRepairTask repairTask;
    [SerializeField, ReadOnly] protected BuildingUpgradeTask upgradeTask;
    [SerializeField, ReadOnly] protected WorkTask currentWorkTask;

    #endregion

    #region Events
    
    public event System.Action OnBuildingDestroyed;
    public event System.Action OnBuildingRepaired;
    public event System.Action OnBuildingUpgraded;

    #endregion

    #region Properties
    
    public override float MaxHealth
    {
        get => buildingScriptableObj != null ? buildingScriptableObj.maxHealth : 100f;
        set
        {
            if (buildingScriptableObj != null)
                buildingScriptableObj.maxHealth = value;
        }
    }

    #endregion

    #region Unity Lifecycle

    protected override void OnDestroy()
    {
        // Free up grid slots when building is destroyed
        if (buildingScriptableObj != null && CampManager.Instance != null)
        {
            CampManager.Instance.MarkSharedGridSlotsUnoccupied(transform.position, buildingScriptableObj.size);
        }
        
        base.OnDestroy();
    }

    #endregion

    #region Building Setup

    public virtual void SetupBuilding(BuildingScriptableObj buildingScriptableObj)
    {
        this.buildingScriptableObj = buildingScriptableObj;
        base.SetupStructure(buildingScriptableObj);
    }

    protected override void OnStructureSetup()
    {
        SetupRepairTask();
        SetupUpgradeTask();
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
        
        // Cast the upgrade target to BuildingScriptableObj since we know it's a building
        if (buildingScriptableObj.upgradeTarget is BuildingScriptableObj upgradeTarget)
        {
            upgradeTask.SetupUpgradeTask(upgradeTarget, buildingScriptableObj.upgradeTime);
        }
        else if (buildingScriptableObj.upgradeTarget != null)
        {
            Debug.LogWarning($"Building upgrade target is not a BuildingScriptableObj: {buildingScriptableObj.upgradeTarget.GetType()}");
        }
    }

    #endregion

    #region Building State Management

    public override void CompleteConstruction()
    {
        base.CompleteConstruction();
        // Additional building-specific construction logic can go here
    }

    public float GetTaskRadius() => buildingScriptableObj.taskRadius;

    public override bool CanInteract()
    {
        return base.CanInteract();
    }

    #endregion

    #region Damage & Health

    public override void TakeDamage(float amount, Transform damageSource = null)
    {
        base.TakeDamage(amount, damageSource);
        // Additional building-specific damage logic can go here
    }

    public override void Heal(float amount)
    {
        base.Heal(amount);
        OnBuildingRepaired?.Invoke();
    }

    public override void Die()
    {
        OnBuildingDestroyed?.Invoke();
        base.Die();
    }

    #endregion

    #region Building Operations

    // Note: Upgrade functionality is now handled by the base PlaceableStructure class
    // This method is kept for backward compatibility but delegates to the base class
    public virtual void Upgrade(BuildingScriptableObj newBuildingData)
    {
        // The base class handles upgrades through scriptable objects
        // This method can be used for building-specific upgrade logic if needed
        OnBuildingUpgraded?.Invoke();
    }

    public override void StartDestruction()
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

    public override string GetInteractionText()
    {
        string baseText = base.GetInteractionText();
        
        if (isUnderConstruction) return "Building under construction";
        if (!isOperational) return "Building not operational";
        
        string text = "Building Tasks:\n";
        if (repairTask != null && repairTask.CanPerformTask())
            text += "- Repair\n";
        if (upgradeTask != null && upgradeTask.CanPerformTask())
            text += "- Upgrade\n";
        if (CanUpgrade())
            text += "- Upgrade to Next Level\n";
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

