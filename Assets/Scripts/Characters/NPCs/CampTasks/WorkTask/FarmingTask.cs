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

    private enum FarmingAction
    {
        None,
        Planting,
        Tending,
        Harvesting,
        Clearing
    }

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
            
            // If still no action needed, no more work
            if (currentAction == FarmingAction.None)
            {
                return false; // Farm doesn't need attention right now
            }
            
            // Start the animation for the new action
            StartActionAnimation(worker);
        }

        // Call base DoWork to handle electricity and progress
        bool canContinue = base.DoWork(worker, deltaTime);
        
        // For harvesting, alternate animations
        if (canContinue && currentAction == FarmingAction.Harvesting)
        {
            HandleHarvestingAnimation(worker);
        }
        
        return canContinue;
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