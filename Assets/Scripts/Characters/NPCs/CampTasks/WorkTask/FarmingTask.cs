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

    private FarmBuilding farmBuilding;
    private FarmingAction currentAction = FarmingAction.None;

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
    }

    protected override IEnumerator WorkCoroutine()
    {
        DetermineNextAction();

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

        currentAction = FarmingAction.None;
        CompleteWork();
    }

    private void DetermineNextAction()
    {
        // Priority order: Clear dead crop -> Harvest ready crop -> Tend needy crop -> Plant new crop
        if (farmBuilding.IsDead)
        {
            currentAction = FarmingAction.Clearing;
            baseWorkTime = clearingTime;
            return;
        }

        if (farmBuilding.IsReadyForHarvest)
        {
            currentAction = FarmingAction.Harvesting;
            baseWorkTime = harvestingTime;
            return;
        }

        if (farmBuilding.NeedsTending)
        {
            currentAction = FarmingAction.Tending;
            baseWorkTime = tendingTime;
            return;
        }

        if (!farmBuilding.IsOccupied)
        {
            currentAction = FarmingAction.Planting;
            baseWorkTime = plantingTime;
            return;
        }
    }

    private IEnumerator PlantingCoroutine()
    {
        // Check if we have seeds
        if (!HasRequiredResources())
        {
            yield break;
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
        while (workProgress < baseWorkTime)
        {
            workProgress += Time.deltaTime;
            yield return null;
        }

        farmBuilding.TendPlot();
    }

    private IEnumerator HarvestingCoroutine()
    {
        while (workProgress < baseWorkTime)
        {
            workProgress += Time.deltaTime;
            yield return null;
        }

        // Create harvested resources
        for (int i = 0; i < harvestAmount; i++)
        {
            Resource resource = Instantiate(farmBuilding.PlantedCrop.prefab, 
                farmBuilding.CropPoint.position + Random.insideUnitSphere, 
                Quaternion.identity).GetComponent<Resource>();
            resource.Initialize(farmBuilding.PlantedCrop);
        }

        farmBuilding.ClearPlot();
    }

    private IEnumerator ClearingCoroutine()
    {
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