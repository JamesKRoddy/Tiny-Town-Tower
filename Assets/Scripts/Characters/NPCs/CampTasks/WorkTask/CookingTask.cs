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
        Debug.Log($"[CookingTask] SetRecipe called - Recipe: {(recipe != null ? recipe.name : "null")}, Current workers: {currentWorkers.Count}");
        SetupTask(recipe);
        Debug.Log($"[CookingTask] After SetupTask - HasQueuedTasks: {HasQueuedTasks}, HasCurrentWork: {HasCurrentWork}, IsOccupied: {IsOccupied}");
    }

    protected override void SetupNextTask()
    {
        Debug.Log($"[CookingTask] SetupNextTask called - currentTaskData: {(currentTaskData != null ? currentTaskData.GetType().Name : "null")}");
        
        if (currentTaskData is CookingRecipeScriptableObj nextRecipe)
        {
            currentRecipe = nextRecipe;
            baseWorkTime = nextRecipe.craftTime;
            requiredResources = nextRecipe.requiredResources;
            
            Debug.Log($"[CookingTask] Recipe setup complete - Recipe: {nextRecipe.name}, WorkTime: {baseWorkTime}, Resources: {(requiredResources != null ? requiredResources.Length : 0)}");
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