using UnityEngine;
using System.Collections;
using Managers;
using System;

public class CookingTask : QueuedWorkTask
{
    [ReadOnly] public CookingRecipeScriptableObj currentRecipe;
    private CanteenBuilding canteenBuilding;

    protected override void Start()
    {
        base.Start();
        canteenBuilding = GetComponent<CanteenBuilding>();
        if (canteenBuilding == null)
        {
            Debug.LogError("CookingTask requires a CanteenBuilding component!");
        }
    }

    public void SetRecipe(CookingRecipeScriptableObj recipe)
    {
        SetupTask(recipe);
    }

    protected override void SetupNextTask()
    {
        if (currentTaskData is CookingRecipeScriptableObj nextRecipe)
        {
            currentRecipe = nextRecipe;
            // Convert game hours to real seconds using TimeManager
            baseWorkTime = Managers.TimeManager.ConvertGameHoursToSecondsStatic(nextRecipe.craftTimeInGameHours);
            requiredResources = nextRecipe.requiredResources;
        }
        else
        {
            Debug.LogWarning($"[CookingTask] SetupNextTask called but currentTaskData is not a CookingRecipeScriptableObj: {currentTaskData}");
        }
    }

    protected override void CompleteWork()
    {
        if (currentRecipe != null && canteenBuilding != null)
        {
            // Create the cooked food and store it in the canteen
            for (int i = 0; i < currentRecipe.outputAmount; i++)
            {
                if (canteenBuilding.CanStoreMoreMeals())
                {
                    canteenBuilding.AddMeal(currentRecipe);
                }
                else
                {
                    // If canteen is full, store in player inventory as backup
                    AddResourceToInventory(currentRecipe.outputResources);
                }
            }
        }
        
        // Clear the current recipe before completing the work
        currentRecipe = null;
        
        base.CompleteWork();
    }
} 