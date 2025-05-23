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
    [SerializeField] private int harvestAmount = 3;
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

    private void DetermineNextAction()
    {
        // Priority order: Clear dead crop -> Harvest ready crop -> Tend needy crop -> Plant new crop
        if (farmBuilding.IsDead)
        {
            currentAction = FarmingAction.Clearing;
            baseWorkTime = clearingTime;
            Debug.Log($"<color=red>[FarmingTask] Found dead crop, will clear it</color>");
            return;
        }

        if (farmBuilding.IsReadyForHarvest)
        {
            currentAction = FarmingAction.Harvesting;
            baseWorkTime = harvestingTime;
            Debug.Log($"<color=yellow>[FarmingTask] Crop ready for harvest</color>");
            return;
        }

        if (!farmBuilding.IsOccupied)
        {
            // Check if we have seeds available
            if (HasRequiredResources())
            {
                currentAction = FarmingAction.Planting;
                baseWorkTime = plantingTime;
                Debug.Log($"<color=blue>[FarmingTask] Farm is empty, ready for planting</color>");
            }
            else
            {
                Debug.Log($"<color=orange>[FarmingTask] No seeds available, stopping farming task</color>");
                CompleteWork();
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
                baseWorkTime = tendingTime;
                Debug.Log($"<color=orange>[FarmingTask] Crop needs immediate tending</color>");
                return;
            }
            
            // Check if it's time for periodic tending
            if (Time.time - lastTendingTime >= tendingInterval)
            {
                currentAction = FarmingAction.Tending;
                baseWorkTime = tendingTime;
                lastTendingTime = Time.time;
                Debug.Log($"<color=orange>[FarmingTask] Performing periodic tending</color>");
                return;
            }

            currentAction = FarmingAction.None;
            Debug.Log($"<color=cyan>[FarmingTask] Crop is growing, next tending in {tendingInterval - (Time.time - lastTendingTime):F1} seconds</color>");
            return;
        }

        // If we get here, there's nothing to do right now
        currentAction = FarmingAction.None;
        Debug.Log($"<color=cyan>[FarmingTask] No action needed</color>");
    }

    protected override IEnumerator WorkCoroutine()
    {
        while (true)
        {
            workProgress = 0f; // Reset work progress at the start of each cycle
            DetermineNextAction();
            
            // If no action is needed, wait a bit and check again
            if (currentAction == FarmingAction.None)
            {
                Debug.Log($"<color=cyan>[FarmingTask] No immediate action needed, waiting...</color>");
                yield return new WaitForSeconds(5f); // Longer wait time when no action needed
                continue;
            }

            Debug.Log($"<color=cyan>[FarmingTask] Starting {currentAction} action</color>");

            switch (currentAction)
            {
                case FarmingAction.Planting:
                    yield return StartCoroutine(PlantingCoroutine());
                    break;
                case FarmingAction.Tending:
                    yield return StartCoroutine(TendingCoroutine());
                    break;
                case FarmingAction.Harvesting:
                    yield return StartCoroutine(HarvestingCoroutine());
                    break;
                case FarmingAction.Clearing:
                    yield return StartCoroutine(ClearingCoroutine());
                    break;
            }

            Debug.Log($"<color=green>[FarmingTask] Completed {currentAction} action</color>");
            currentAction = FarmingAction.None;

            // Small delay between actions
            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator PlantingCoroutine()
    {
        // Check if we have seeds
        if (!HasRequiredResources())
        {
            Debug.Log($"<color=red>[FarmingTask] Not enough seeds to plant</color>");
            CompleteWork();
            yield break;
        }

        Debug.Log($"<color=blue>[FarmingTask] Starting to plant {requiredResources[0].resourceScriptableObj.objectName}</color>");
        if (currentWorker != null)
        {
            currentWorker.PlayWorkAnimation(TaskAnimation.PLANTING_SEEDS.ToString());
        }
        
        // Plant the crop
        while (workProgress < baseWorkTime)
        {
            workProgress += Time.deltaTime;
            yield return null;
        }

        // Consume seeds and plant crop
        ConsumeResources();
        farmBuilding.PlantCrop(requiredResources[0].resourceScriptableObj);
        Debug.Log($"<color=green>[FarmingTask] Successfully planted {requiredResources[0].resourceScriptableObj.objectName}</color>");
    }

    private IEnumerator TendingCoroutine()
    {
        Debug.Log($"<color=orange>[FarmingTask] Starting to tend crop</color>");
        if (currentWorker != null)
        {
            currentWorker.PlayWorkAnimation(TaskAnimation.WATERING_PLANTS.ToString());
        }
        
        while (workProgress < baseWorkTime)
        {
            workProgress += Time.deltaTime;
            yield return null;
        }

        farmBuilding.TendPlot();
        Debug.Log($"<color=green>[FarmingTask] Successfully tended crop</color>");
    }

    private IEnumerator HarvestingCoroutine()
    {
        Debug.Log($"<color=yellow>[FarmingTask] Starting to harvest crop</color>");
        
        // Stop growth when harvesting starts
        farmBuilding.StartHarvesting();
        
        // Alternate between standing and kneeling animations during harvest
        bool isKneeling = false;
        float animationSwitchTime = 0f;
        
        while (workProgress < baseWorkTime)
        {
            workProgress += Time.deltaTime;
            
            // Switch animation every 2 seconds
            if (Time.time - animationSwitchTime >= 2f)
            {
                isKneeling = !isKneeling;
                if (currentWorker != null)
                {
                    taskAnimation = isKneeling ? TaskAnimation.HARVEST_PLANT_KNEELING : TaskAnimation.HARVEST_PLANT_STANDING;
                    currentWorker.PlayWorkAnimation(taskAnimation.ToString());
                }
                animationSwitchTime = Time.time;
            }
            
            yield return null;
        }

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
                Debug.Log($"<color=green>[FarmingTask] Successfully harvested {yieldAmount} {harvestedCrop.objectName} and added to inventory</color>");
            }
            else
            {
                Debug.LogError($"<color=red>[FarmingTask] Failed to add resources to inventory - PlayerInventory is null</color>");
            }
        }
        else
        {
            Debug.LogError($"<color=red>[FarmingTask] Failed to harvest - no crop found</color>");
        }

        farmBuilding.ClearPlot();
    }

    private IEnumerator ClearingCoroutine()
    {
        Debug.Log($"<color=red>[FarmingTask] Starting to clear dead crop</color>");
        if (currentWorker != null)
        {
            currentWorker.PlayWorkAnimation(TaskAnimation.CLEARING_PLOT.ToString());
        }
        
        while (workProgress < baseWorkTime)
        {
            workProgress += Time.deltaTime;
            yield return null;
        }

        farmBuilding.ClearPlot();
        Debug.Log($"<color=green>[FarmingTask] Successfully cleared dead crop</color>");
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