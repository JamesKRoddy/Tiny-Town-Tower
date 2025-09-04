using UnityEngine;
using System.Collections;
using Managers;
using CampBuilding;

public class StructureUpgradeTask : WorkTask
{
    private PlaceableObjectParent upgradeTarget;

    protected override void Start()
    {
        base.Start();
        taskAnimation = TaskAnimation.HAMMER_STANDING;
    }

    public void SetupUpgradeTask(PlaceableObjectParent upgradeTarget)
    {
        this.upgradeTarget = upgradeTarget;
        // Convert game hours to real seconds using TimeManager
        float constructionTimeInGameHours = upgradeTarget?.constructionTimeInGameHours ?? 1f;
        baseWorkTime = Managers.TimeManager.ConvertGameHoursToSecondsStatic(constructionTimeInGameHours);
        requiredResources = upgradeTarget?.upgradeResources;
    }

    public void ExecuteUpgrade()
    {
        // Consume resources and create construction site immediately
        ConsumeResources();
        CreateConstructionSite();
    }



    private void CreateConstructionSite()
    {
        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;

        // Get construction site prefab
        GameObject constructionSitePrefab = CampManager.Instance.BuildManager.GetConstructionSitePrefab(upgradeTarget.size);
        if (constructionSitePrefab == null)
        {
            Debug.LogError($"No construction site prefab for size {upgradeTarget.size}");
            return;
        }

        // Create construction site
        GameObject constructionSite = Instantiate(constructionSitePrefab, position, rotation);
            
        // Setup construction task
        StructureConstructionTask constructionTask = constructionSite.GetComponent<StructureConstructionTask>();
        if (constructionTask == null)
        {
            constructionTask = constructionSite.AddComponent<StructureConstructionTask>();
        }
        constructionTask.SetupConstruction(upgradeTarget, true);

        // Handle grid slots
        CampManager.Instance.MarkSharedGridSlotsUnoccupied(position, GetComponent<IPlaceableStructure>().GetStructureScriptableObj().size);
        CampManager.Instance.MarkSharedGridSlotsOccupied(position, upgradeTarget.size, constructionSite);

        // Add to work manager
        CampManager.Instance.WorkManager.AddWorkTask(constructionTask);

        // Destroy the original structure
        Destroy(gameObject);
    }



    public override bool CanPerformTask()
    {
        if (GetComponent<IPlaceableStructure>() == null || upgradeTarget == null) return false;
        
        // Check if player has required resources
        if (requiredResources != null)
        {
            foreach (var resource in requiredResources)
            {
                if (PlayerInventory.Instance.GetItemCount(resource.resourceScriptableObj) < resource.count)
                {
                    return false;
                }
            }
        }
        
        return true;
    }

    public override string GetTooltipText()
    {
        if (upgradeTarget == null) return "No upgrade target selected";
        
        string tooltip = $"Upgrade to {upgradeTarget.objectName}\n";
        
        // Check if this is a turret upgrade
        BaseTurret currentTurret = GetComponent<BaseTurret>();
        if (currentTurret != null && upgradeTarget is TurretScriptableObject upgradeTurret)
        {
            tooltip += "\n=== TURRET UPGRADE ===\n\n";
            
            // Current turret stats
            tooltip += "Current Turret:\n";
            tooltip += $"- Damage: {currentTurret.damage:F1}\n";
            tooltip += $"- Range: {currentTurret.range:F1}\n";
            tooltip += $"- Fire Rate: {currentTurret.fireRate:F1} shots/sec\n";
            tooltip += $"- Turn Speed: {currentTurret.turretTurnSpeed:F1}\n";
            tooltip += $"- Health: {currentTurret.GetCurrentHealth():F0}/{currentTurret.GetMaxHealth():F0}\n";
            
            tooltip += "\nUpgrade Target:\n";
            tooltip += $"- Damage: {upgradeTurret.damage:F1} ";
            if (upgradeTurret.damage > currentTurret.damage)
                tooltip += $"(+{upgradeTurret.damage - currentTurret.damage:F1})";
            else if (upgradeTurret.damage < currentTurret.damage)
                tooltip += $"({upgradeTurret.damage - currentTurret.damage:F1})";
            tooltip += "\n";
            
            tooltip += $"- Range: {upgradeTurret.range:F1} ";
            if (upgradeTurret.range > currentTurret.range)
                tooltip += $"(+{upgradeTurret.range - currentTurret.range:F1})";
            else if (upgradeTurret.range < currentTurret.range)
                tooltip += $"({upgradeTurret.range - currentTurret.range:F1})";
            tooltip += "\n";
            
            tooltip += $"- Fire Rate: {upgradeTurret.fireRate:F1} shots/sec ";
            if (upgradeTurret.fireRate > currentTurret.fireRate)
                tooltip += $"(+{upgradeTurret.fireRate - currentTurret.fireRate:F1})";
            else if (upgradeTurret.fireRate < currentTurret.fireRate)
                tooltip += $"({upgradeTurret.fireRate - currentTurret.fireRate:F1})";
            tooltip += "\n";
            
            tooltip += $"- Turn Speed: {upgradeTurret.turretTurnSpeed:F1} ";
            if (upgradeTurret.turretTurnSpeed > currentTurret.turretTurnSpeed)
                tooltip += $"(+{upgradeTurret.turretTurnSpeed - currentTurret.turretTurnSpeed:F1})";
            else if (upgradeTurret.turretTurnSpeed < currentTurret.turretTurnSpeed)
                tooltip += $"({upgradeTurret.turretTurnSpeed - currentTurret.turretTurnSpeed:F1})";
            tooltip += "\n";
            
            tooltip += $"- Max Health: {upgradeTurret.maxHealth:F0} ";
            if (upgradeTurret.maxHealth > currentTurret.GetMaxHealth())
                tooltip += $"(+{upgradeTurret.maxHealth - currentTurret.GetMaxHealth():F0})";
            else if (upgradeTurret.maxHealth < currentTurret.GetMaxHealth())
                tooltip += $"({upgradeTurret.maxHealth - currentTurret.GetMaxHealth():F0})";
            tooltip += "\n";
            
            // Construction time
            tooltip += $"\nConstruction Time: {upgradeTurret.constructionTimeInGameHours:F1} game hours\n";
        }
        // Check if this is a building upgrade
        else if (GetComponent<Building>() != null && upgradeTarget is BuildingScriptableObj upgradeBuilding)
        {
            Building currentBuilding = GetComponent<Building>();
            
            tooltip += "\n=== BUILDING UPGRADE ===\n\n";
            
            // Current building stats
            tooltip += "Current Building:\n";
            tooltip += $"- Health: {currentBuilding.GetCurrentHealth():F0}/{currentBuilding.GetMaxHealth():F0}\n";
            tooltip += $"- Max Health: {currentBuilding.GetMaxHealth():F0}\n";
            tooltip += $"- Repair Time: {currentBuilding.GetStructureScriptableObj().repairTimeInGameHours:F1} game hours\n";
            tooltip += $"- Health Restored Per Repair: {currentBuilding.GetStructureScriptableObj().healthRestoredPerRepair:F0}\n";
            
            // Add building-specific stats
            AddBuildingSpecificStats(tooltip, currentBuilding);
            
            tooltip += "\nUpgrade Target:\n";
            tooltip += $"- Max Health: {upgradeBuilding.maxHealth:F0} ";
            if (upgradeBuilding.maxHealth > currentBuilding.GetMaxHealth())
                tooltip += $"(+{upgradeBuilding.maxHealth - currentBuilding.GetMaxHealth():F0})";
            else if (upgradeBuilding.maxHealth < currentBuilding.GetMaxHealth())
                tooltip += $"({upgradeBuilding.maxHealth - currentBuilding.GetMaxHealth():F0})";
            tooltip += "\n";
            
            tooltip += $"- Repair Time: {upgradeBuilding.repairTimeInGameHours:F1} game hours ";
            if (upgradeBuilding.repairTimeInGameHours < currentBuilding.GetStructureScriptableObj().repairTimeInGameHours)
                tooltip += $"(-{currentBuilding.GetStructureScriptableObj().repairTimeInGameHours - upgradeBuilding.repairTimeInGameHours:F1})";
            else if (upgradeBuilding.repairTimeInGameHours > currentBuilding.GetStructureScriptableObj().repairTimeInGameHours)
                tooltip += $"(+{upgradeBuilding.repairTimeInGameHours - currentBuilding.GetStructureScriptableObj().repairTimeInGameHours:F1})";
            tooltip += "\n";
            
            tooltip += $"- Health Restored Per Repair: {upgradeBuilding.healthRestoredPerRepair:F0} ";
            if (upgradeBuilding.healthRestoredPerRepair > currentBuilding.GetStructureScriptableObj().healthRestoredPerRepair)
                tooltip += $"(+{upgradeBuilding.healthRestoredPerRepair - currentBuilding.GetStructureScriptableObj().healthRestoredPerRepair:F0})";
            else if (upgradeBuilding.healthRestoredPerRepair < currentBuilding.GetStructureScriptableObj().healthRestoredPerRepair)
                tooltip += $"({upgradeBuilding.healthRestoredPerRepair - currentBuilding.GetStructureScriptableObj().healthRestoredPerRepair:F0})";
            tooltip += "\n";
            
            // Add upgrade target building-specific stats
            AddUpgradeTargetBuildingStats(tooltip, upgradeBuilding);
            
            // Construction time
            tooltip += $"\nConstruction Time: {upgradeBuilding.constructionTimeInGameHours:F1} game hours\n";
        }
        else
        {
            // For non-turret/non-building upgrades, show basic info
            tooltip += $"\nConstruction Time: {upgradeTarget.constructionTimeInGameHours:F1} game hours\n";
            if (upgradeTarget.maxHealth > 0)
            {
                tooltip += $"Max Health: {upgradeTarget.maxHealth:F0}\n";
            }
        }
        
        // Required resources
        if (requiredResources != null && requiredResources.Length > 0)
        {
            tooltip += "\nRequired Resources:\n";
            foreach (var resource in requiredResources)
            {
                if (resource != null && resource.resourceScriptableObj != null)
                {
                    int currentAmount = PlayerInventory.Instance.GetItemCount(resource.resourceScriptableObj);
                    string status = currentAmount >= resource.count ? "✓" : "✗";
                    tooltip += $"- {status} {resource.resourceScriptableObj.objectName}: {currentAmount}/{resource.count}\n";
                }
            }
        }
        
        // Check if player has all required resources
        bool hasAllResources = HasRequiredResources();
        tooltip += $"\nStatus: {(hasAllResources ? "Ready to Upgrade" : "Missing Resources")}";
        
        return tooltip;
    }
    
    private void AddBuildingSpecificStats(string tooltip, Building building)
    {
        // Check for specific building types and add their unique stats
        if (building is FarmBuilding farmBuilding)
        {
            tooltip += $"- Building Type: Farm\n";
            if (farmBuilding.IsOccupied)
            {
                tooltip += $"- Crop: {farmBuilding.PlantedCrop?.objectName ?? "Unknown"}\n";
                tooltip += $"- Status: {(farmBuilding.IsDead ? "Dead" : farmBuilding.IsReadyForHarvest ? "Ready for Harvest" : farmBuilding.NeedsTending ? "Needs Tending" : "Growing")}\n";
            }
            else
            {
                tooltip += "- Status: Empty Plot\n";
            }
        }
        else if (building is CanteenBuilding canteenBuilding)
        {
            tooltip += $"- Building Type: Canteen\n";
            tooltip += $"- Stored Meals: {canteenBuilding.GetStoredMealsCount()}/{canteenBuilding.GetMaxStoredMeals()}\n";
            tooltip += $"- Status: {(canteenBuilding.HasAvailableMeals() ? "Has Food Available" : "No Food Available")}\n";
        }
        else if (building is BunkerBuilding bunkerBuilding)
        {
            tooltip += $"- Building Type: Bunker\n";
            // Add bunker-specific stats if any
        }
        else if (building is WallBuilding wallBuilding)
        {
            tooltip += $"- Building Type: Wall\n";
            // Add wall-specific stats if any
        }
        else
        {
            tooltip += $"- Building Type: {building.GetType().Name}\n";
        }
    }
    
    private void AddUpgradeTargetBuildingStats(string tooltip, BuildingScriptableObj upgradeBuilding)
    {
        // For now, we can't easily determine the specific building type from the scriptable object
        // But we can add any upgrade-specific information here if needed
        if (upgradeBuilding.upgradeTarget != null)
        {
            tooltip += $"- Can Upgrade To: {upgradeBuilding.upgradeTarget.objectName}\n";
        }
    }
} 