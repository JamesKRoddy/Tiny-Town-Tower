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
        currentRecipe = recipe;
        if (recipe != null)
        {
            baseWorkTime = recipe.cookingTime;
            requiredResources = recipe.requiredIngredients;
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