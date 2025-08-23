using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Managers;
using CampBuilding;

public class FarmingTask : WorkTask
{
    [Header("Farming Settings")]
    [SerializeField] private float plantingTime = 5f;
    [SerializeField] private float tendingTime = 3f;
    [SerializeField] private float harvestingTime = 5f;
    [SerializeField] private float clearingTime = 4f;
    [SerializeField] private float tendingInterval = 30f; // Time between tending checks

    private FarmBuilding farmBuilding;
    private FarmingAction currentAction = FarmingAction.None;
    private float lastTendingTime = 0f;
    private float lastMonitoringCheck = 0f;
    private const float MONITORING_CHECK_INTERVAL = 5f; // Check every 5 seconds when monitoring

    private enum FarmingAction
    {
        None,
        Planting,
        Tending,
        Harvesting,
        Clearing
    }

    // Override to ensure task is only complete when no work is needed and no crop to monitor
    public override bool IsTaskCompleted => currentAction == FarmingAction.None && 
                                           (!farmBuilding.IsOccupied || farmBuilding.IsDead);

    protected override void Start()
    {
        base.Start();
        farmBuilding = GetComponent<FarmBuilding>();
        if (farmBuilding == null)
        {
            Debug.LogError("FarmingTask requires a FarmBuilding component!");
        }
        lastTendingTime = Time.time;
    }

    public bool IsOccupiedWithCrop()
    {
        return farmBuilding.IsOccupied;
    }

    private void DetermineNextAction()
    {
        // Priority order: Clear dead crop -> Harvest ready crop -> Tend needy crop -> Plant new crop
        if (farmBuilding.IsDead)
        {
            currentAction = FarmingAction.Clearing;
            Debug.Log($"<color=cyan>[FarmingTask] Starting Clearing action</color>");
            baseWorkTime = clearingTime;
            return;
        }

        if (farmBuilding.IsReadyForHarvest)
        {
            currentAction = FarmingAction.Harvesting;
            Debug.Log($"<color=cyan>[FarmingTask] Starting Harvesting action</color>");
            baseWorkTime = harvestingTime;
            return;
        }

        if (!farmBuilding.IsOccupied)
        {
            // Check if we have seeds available
            if (HasRequiredResources())
            {
                currentAction = FarmingAction.Planting;
                Debug.Log($"<color=cyan>[FarmingTask] Starting Planting action</color>");
                baseWorkTime = plantingTime;
            }
            else
            {
                CompleteWork();
                return;
            }
            return;
        }

        // If we have a growing crop, check if it needs tending
        if (farmBuilding.IsOccupied && !farmBuilding.IsReadyForHarvest)
        {
            // Check if crop needs immediate tending
            if (farmBuilding.NeedsTending)
            {
                currentAction = FarmingAction.Tending;
                Debug.Log($"<color=cyan>[FarmingTask] Starting Tending action</color>");
                baseWorkTime = tendingTime;
                return;
            }
            
            // Check if it's time for periodic tending
            if (Time.time - lastTendingTime >= tendingInterval)
            {
                currentAction = FarmingAction.Tending;
                Debug.Log($"<color=cyan>[FarmingTask] Starting Tending action</color>");
                baseWorkTime = tendingTime;
                lastTendingTime = Time.time;
                return;
            }

            currentAction = FarmingAction.None;
            return;
        }

        // If we get here, there's nothing to do right now
        currentAction = FarmingAction.None;
    }

    public override bool DoWork(HumanCharacterController worker, float deltaTime)
    {
        // If no current action or action is complete, determine next action
        if (currentAction == FarmingAction.None || workProgress >= baseWorkTime)
        {
            // Complete current action first if we have one
            if (currentAction != FarmingAction.None && workProgress >= baseWorkTime)
            {
                CompleteCurrentAction();
            }
            
            // Reset progress and determine next action
            workProgress = 0f;
            DetermineNextAction();
            
            // If still no action needed, check if we should keep the NPC assigned
            if (currentAction == FarmingAction.None)
            {
                // If farm is occupied with a growing crop, keep NPC assigned for future tending
                if (farmBuilding.IsOccupied && !farmBuilding.IsDead)
                {
                    Debug.Log($"<color=yellow>[FarmingTask] No immediate action needed, but keeping {worker.name} assigned to monitor growing crop</color>");
                    lastMonitoringCheck = Time.time;
                    return true; // Keep NPC assigned but not actively working
                }
                else
                {
                    Debug.Log($"<color=yellow>[FarmingTask] No work needed and no crop to monitor, releasing {worker.name}</color>");
                    return false; // Farm doesn't need attention right now
                }
            }
            
            // Start the animation for the new action
            StartActionAnimation(worker);
        }

        // Handle electricity and work progress manually (don't call base.DoWork to avoid auto-completion)
        if (!isOperational)
        {
            Debug.LogError($"[FarmingTask] Task is not operational for {worker.name}");
            return false;
        }
        
        if (!currentWorkers.Contains(worker))
        {
            Debug.LogError($"[FarmingTask] Worker {worker.name} is not in currentWorkers list. Current workers: {currentWorkers.Count}");
            return false;
        }

        // Validate work task data
        if (baseWorkTime <= 0)
        {
            Debug.LogError($"[FarmingTask] Invalid baseWorkTime ({baseWorkTime}) for {GetType().Name}. Work task cannot be performed. NPC {worker.name} will return to wander state.");
            SetOperationalStatus(false);
            return false;
        }

        // Get worker speed multiplier
        float workSpeed = 1f;
        if (worker is SettlerNPC settler)
        {
            workSpeed = settler.GetWorkSpeedMultiplier();
            if (workSpeed <= 0)
            {
                Debug.LogError($"[FarmingTask] Worker {worker.name} has invalid work speed: {workSpeed}");
                return false;
            }
        }

        // Calculate work progress for this frame
        float workDelta = deltaTime * workSpeed;
        
        // Handle electricity consumption
        float electricityConsumption = electricityRequired > 0 ? electricityRequired : 1f;
        float electricityRate = electricityConsumption / baseWorkTime;
        float electricityPerWorker = electricityRate / Mathf.Max(1, currentWorkers.Count);
        float electricityNeeded = electricityPerWorker * workDelta;
        
        Debug.Log($"[FarmingTask] Electricity check for {worker.name} - Action: {currentAction}, BaseWorkTime: {baseWorkTime}, ElectricityNeeded: {electricityNeeded}, WorkDelta: {workDelta}");
        
        if (electricityNeeded > 0)
        {
            if (!CampManager.Instance.ElectricityManager.ConsumeElectricity(electricityNeeded, 1f))
            {
                Debug.LogError($"[FarmingTask] Not enough electricity for {worker.name} - needed: {electricityNeeded}");
                SetOperationalStatus(false);
                return false;
            }
        }
        
        // If we're actively working on an action, advance work progress
        if (currentAction != FarmingAction.None)
        {
            workProgress += workDelta;
            
            // For harvesting, alternate animations
            if (currentAction == FarmingAction.Harvesting)
            {
                HandleHarvestingAnimation(worker);
            }
        }
        else
        {
            // We're in monitoring mode - periodically check for new actions
            if (Time.time - lastMonitoringCheck >= MONITORING_CHECK_INTERVAL)
            {
                Debug.Log($"<color=cyan>[FarmingTask] Monitoring check for {worker.name} - checking farm status</color>");
                lastMonitoringCheck = Time.time;
                
                // Check if farm needs attention now
                DetermineNextAction();
                if (currentAction != FarmingAction.None)
                {
                    Debug.Log($"<color=green>[FarmingTask] New action needed during monitoring: {currentAction}</color>");
                    StartActionAnimation(worker);
                    workProgress = 0f; // Reset progress for new action
                }
            }
        }
        
        Debug.Log($"[FarmingTask] DoWork completed successfully for {worker.name} - Action: {currentAction}, Progress: {workProgress}/{baseWorkTime}");
        return true; // Continue working/monitoring
    }

    private void StartActionAnimation(HumanCharacterController worker)
    {
        if (worker == null) return;
        
        switch (currentAction)
        {
            case FarmingAction.Planting:
                taskAnimation = TaskAnimation.PLANTING_SEEDS;
                worker.PlayWorkAnimation(taskAnimation.ToString());
                break;
            case FarmingAction.Tending:
                taskAnimation = TaskAnimation.WATERING_PLANTS;
                worker.PlayWorkAnimation(taskAnimation.ToString());
                break;
            case FarmingAction.Harvesting:
                // Start with standing animation, will alternate in ProcessWork
                taskAnimation = TaskAnimation.HARVEST_PLANT_STANDING;
                worker.PlayWorkAnimation(taskAnimation.ToString());
                farmBuilding.StartHarvesting(); // Stop growth when harvesting starts
                break;
            case FarmingAction.Clearing:
                taskAnimation = TaskAnimation.CLEARING_PLOT;
                worker.PlayWorkAnimation(taskAnimation.ToString());
                break;
        }
    }

    private float lastAnimationSwitch = 0f;
    private bool isKneelingAnimation = false;

    private void HandleHarvestingAnimation(HumanCharacterController worker)
    {
        // Switch animation every 2 seconds during harvesting
        if (Time.time - lastAnimationSwitch >= 2f)
        {
            isKneelingAnimation = !isKneelingAnimation;
            taskAnimation = isKneelingAnimation ? TaskAnimation.HARVEST_PLANT_KNEELING : TaskAnimation.HARVEST_PLANT_STANDING;
            worker.PlayWorkAnimation(taskAnimation.ToString());
            lastAnimationSwitch = Time.time;
        }
    }

    private void CompleteCurrentAction()
    {
        switch (currentAction)
        {
            case FarmingAction.Planting:
                CompletePlanting();
                break;
            case FarmingAction.Tending:
                CompleteTending();
                break;
            case FarmingAction.Harvesting:
                CompleteHarvesting();
                break;
            case FarmingAction.Clearing:
                CompleteClearing();
                break;
        }
        
        currentAction = FarmingAction.None;
    }

    private void CompletePlanting()
    {
        // Check if we still have seeds
        if (!HasRequiredResources())
        {
            return;
        }

        // Consume seeds and plant crop
        ConsumeResources();
        farmBuilding.PlantCrop(requiredResources[0].resourceScriptableObj);
    }

    private void CompleteTending()
    {
        farmBuilding.TendPlot();
        lastTendingTime = Time.time;
    }

    private void CompleteHarvesting()
    {
        // Add harvested resources to player's inventory
        ResourceScriptableObj harvestedCrop = farmBuilding.PlantedCrop;
        if (harvestedCrop != null)
        {
            // Get the yield amount from the seed
            int yieldAmount = farmBuilding.PlantedSeed.yieldAmount;
            
            // Add resources to inventory
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.AddItem(harvestedCrop, yieldAmount);
            }
        }

        farmBuilding.ClearPlot();
    }

    private void CompleteClearing()
    {
        farmBuilding.ClearPlot();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        // No longer need to manage action coroutines since workers handle their own work
    }

    public override string GetTooltipText()
    {
        if (!showTooltip) return string.Empty;

        string tooltip = "Farm\n";
        
        switch (currentAction)
        {
            case FarmingAction.Planting:
                tooltip += "Action: Planting\n";
                break;
            case FarmingAction.Tending:
                tooltip += "Action: Tending\n";
                break;
            case FarmingAction.Harvesting:
                tooltip += "Action: Harvesting\n";
                break;
            case FarmingAction.Clearing:
                tooltip += "Action: Clearing Dead Crop\n";
                break;
            default:
                tooltip += "No current action\n";
                break;
        }

        if (requiredResources != null && requiredResources.Length > 0)
        {
            tooltip += "Required Resources:\n";
            foreach (var resource in requiredResources)
            {
                tooltip += $"- {resource.resourceScriptableObj.objectName}: {resource.count}\n";
            }
        }

        return tooltip;
    }
} 