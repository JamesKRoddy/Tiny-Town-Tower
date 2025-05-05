using UnityEngine;
using System.Collections;
using Managers;
using System;

public class CookingTask : WorkTask
{
    public CookingRecipeScriptableObj currentRecipe;

    protected override void Start()
    {
        base.Start();
        workType = WorkType.COOKING;
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
            baseWorkTime = nextRecipe.cookingTime;
            requiredResources = nextRecipe.requiredIngredients;
        }
    }

    protected override void CompleteWork()
    {
        if (currentRecipe != null)
        {
            // Create the cooked food
            for (int i = 0; i < currentRecipe.outputAmount; i++)
            {
                AddResourceToInventory(currentRecipe.outputFood);
            }
        }
        
        // Clear the current recipe before completing the work
        currentRecipe = null;
        
        base.CompleteWork();
    }
} 