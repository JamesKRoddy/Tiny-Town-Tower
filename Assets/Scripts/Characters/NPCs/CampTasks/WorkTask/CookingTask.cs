using UnityEngine;
using System.Collections;
using Managers;

public class CookingTask : WorkTask
{
    private CookingRecipeScriptableObj currentRecipe;

    protected override void Start()
    {
        base.Start();
        workType = WorkType.COOKING;
    }

    public void SetRecipe(CookingRecipeScriptableObj recipe)
    {
        if (currentRecipe == null)
        {
            // If no current recipe, set it directly
            currentRecipe = recipe;
            if (recipe != null)
            {
                baseWorkTime = recipe.cookingTime;
                requiredResources = recipe.requiredIngredients;
            }
        }
        else
        {
            // Otherwise queue it
            QueueTask(recipe);
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
    }
} 