using UnityEngine;
using System.Collections;
using Managers;
using System;

public class CookingTask : WorkTask
{
    public CookingRecipeScriptableObj currentRecipe;
    public event Action OnTaskCompleted;

    protected override void Start()
    {
        base.Start();
        workType = WorkType.COOKING;
    }

    public void SetRecipe(CookingRecipeScriptableObj recipe)
    {
        if (recipe == null) return;
        
        // Queue the recipe
        QueueTask(recipe);

        // If no current recipe, set it up immediately
        if (currentRecipe == null)
        {
            currentTaskData = taskQueue.Dequeue();
            SetupNextTask();
        }
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
        
        base.CompleteWork();
        OnTaskCompleted?.Invoke();
    }
} 