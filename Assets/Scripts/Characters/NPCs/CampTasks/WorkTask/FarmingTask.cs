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
    private Coroutine currentActionCoroutine;

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

    protected override IEnumerator WorkCoroutine()
    {
        while (true)
        {
            workProgress = 0f; // Reset work progress at the start of each cycle
            DetermineNextAction();
            
            // If no action is needed, wait a bit and check again
            if (currentAction == FarmingAction.None)
            {
                yield return new WaitForSeconds(5f); // Longer wait time when no action needed
                continue;
            }

            Debug.Log($"<color=cyan>[FarmingTask] Starting {currentAction} action</color>");

            // Stop any existing action coroutine
            if (currentActionCoroutine != null)
            {
                StopCoroutine(currentActionCoroutine);
                currentActionCoroutine = null;
            }

            // Start and wait for the current action to complete
            switch (currentAction)
            {                
                case FarmingAction.Planting:
                    Debug.Log($"<color=cyan>[FarmingTask] Starting Planting action</color>");
                    currentActionCoroutine = StartCoroutine(PlantingCoroutine());
                    yield return currentActionCoroutine;
                    currentActionCoroutine = null;
                    break;
                case FarmingAction.Tending:
                    Debug.Log($"<color=cyan>[FarmingTask] Starting Tending action</color>");
                    currentActionCoroutine = StartCoroutine(TendingCoroutine());
                    yield return currentActionCoroutine;
                    currentActionCoroutine = null;
                    break;
                case FarmingAction.Harvesting:
                    Debug.Log($"<color=cyan>[FarmingTask] Starting Harvesting action</color>");
                    currentActionCoroutine = StartCoroutine(HarvestingCoroutine());
                    yield return currentActionCoroutine;
                    currentActionCoroutine = null;
                    break;
                case FarmingAction.Clearing:
                    Debug.Log($"<color=cyan>[FarmingTask] Starting Clearing action</color>");
                    currentActionCoroutine = StartCoroutine(ClearingCoroutine());
                    yield return currentActionCoroutine;
                    currentActionCoroutine = null;
                    break;
            }

            currentAction = FarmingAction.None;

            // If the farm is empty and we don't have seeds, stop farming
            if (!farmBuilding.IsOccupied && !HasRequiredResources())
            {
                CompleteWork();
                yield break;
            }

            // Small delay between actions
            yield return new WaitForSeconds(0.5f);
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (currentActionCoroutine != null)
        {
            StopCoroutine(currentActionCoroutine);
            currentActionCoroutine = null;
        }
    }

    private IEnumerator PlantingCoroutine()
    {
        // Check if we have seeds
        if (!HasRequiredResources())
        {
            CompleteWork();
            yield break;
        }

        if (currentWorker != null)
        {
            taskAnimation = TaskAnimation.PLANTING_SEEDS;
            currentWorker.PlayWorkAnimation(taskAnimation.ToString());
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
    }

    private IEnumerator TendingCoroutine()
    {
        if (currentWorker != null)
        {
            taskAnimation = TaskAnimation.WATERING_PLANTS;
            currentWorker.PlayWorkAnimation(taskAnimation.ToString());
        }
        
        while (workProgress < baseWorkTime)
        {
            workProgress += Time.deltaTime;
            yield return null;
        }

        farmBuilding.TendPlot();
    }

    private IEnumerator HarvestingCoroutine()
    {
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
            }
        }

        farmBuilding.ClearPlot();
    }

    private IEnumerator ClearingCoroutine()
    {
        if (currentWorker != null)
        {
            taskAnimation = TaskAnimation.CLEARING_PLOT;
            currentWorker.PlayWorkAnimation(taskAnimation.ToString());
        }
        
        while (workProgress < baseWorkTime)
        {
            workProgress += Time.deltaTime;
            yield return null;
        }

        farmBuilding.ClearPlot();
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