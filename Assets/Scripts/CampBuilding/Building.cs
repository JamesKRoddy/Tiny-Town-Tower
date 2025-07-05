using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using Managers;
using System;
using Enemies;

/// <summary>
/// Represents a building in the camp that can be constructed, repaired, and upgraded.
/// Inherits from PlaceableStructure to share common functionality with turrets.
/// </summary>
public class Building : PlaceableStructure<BuildingScriptableObj>, IInteractive<Building>
{
    #region Serialized Fields
    
    [Header("Building State")]
    // Note: isOperational, isUnderConstruction, and currentHealth are inherited from PlaceableStructure

    #endregion

    #region Properties
    
    public override float MaxHealth
    {
        get => StructureScriptableObj != null ? StructureScriptableObj.maxHealth : 100f;
        set
        {
            if (StructureScriptableObj != null)
                StructureScriptableObj.maxHealth = value;
        }
    }

    #endregion

    #region Unity Lifecycle

    protected override void OnDestroy()
    {
        // Free up grid slots when building is destroyed
        if (StructureScriptableObj != null && CampManager.Instance != null)
        {
            CampManager.Instance.MarkSharedGridSlotsUnoccupied(transform.position, StructureScriptableObj.size);
        }
        
        base.OnDestroy();
    }

    #endregion

    #region Building Setup

    public virtual void SetupBuilding(BuildingScriptableObj buildingScriptableObj)
    {
        base.SetupStructure(buildingScriptableObj);
    }

    protected override void OnStructureSetup()
    {
        // Base class handles repair and upgrade task setup
        base.OnStructureSetup();
    }

    #endregion

    #region Building State Management

    public override void CompleteConstruction()
    {
        base.CompleteConstruction();
        // Additional building-specific construction logic can go here
    }

    public override bool CanInteract()
    {
        return base.CanInteract();
    }

    #endregion

    #region Building Operations

    // Destruction logic is now handled in the base PlaceableStructure class

    #endregion

    #region Interaction

    public override string GetInteractionText()
    {
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

    internal string GetBuildingStatsText()
    {
        string upgradeTimeText = StructureScriptableObj.upgradeTarget != null 
            ? $"{StructureScriptableObj.upgradeTarget.constructionTime} seconds" 
            : "N/A";
            
        return $"Building Stats:\n" +
               $"Health: {currentHealth}/{MaxHealth}\n" +
               $"Repair Time: {StructureScriptableObj.repairTime} seconds\n" +
               $"Upgrade Time: {upgradeTimeText}\n" +
               $"Max Health: {MaxHealth}\n" +
               $"Health Restored Per Repair: {StructureScriptableObj.healthRestoredPerRepair}\n" +
               $"Upgrade Target: {StructureScriptableObj.upgradeTarget}\n";
    }

    #endregion
}

