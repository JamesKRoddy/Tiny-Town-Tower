using UnityEngine;
using System.Collections.Generic;

namespace Managers
{
    public class CookingManager : MonoBehaviour
    {
        [SerializeField] private List<CookingRecipeScriptableObj> allRecipes = new List<CookingRecipeScriptableObj>();
        private List<CookingRecipeScriptableObj> availableRecipes = new List<CookingRecipeScriptableObj>();
        private List<CookingRecipeScriptableObj> unlockedRecipes = new List<CookingRecipeScriptableObj>();

        public void Initialize()
        {
            availableRecipes = new List<CookingRecipeScriptableObj>(allRecipes);
            unlockedRecipes = new List<CookingRecipeScriptableObj>();
        }

        public List<CookingRecipeScriptableObj> GetAllRecipes()
        {
            return allRecipes;
        }

        public List<CookingRecipeScriptableObj> GetAvailableRecipes()
        {
            return availableRecipes;
        }

        public List<CookingRecipeScriptableObj> GetUnlockedRecipes()
        {
            return unlockedRecipes;
        }

        public bool IsRecipeUnlocked(CookingRecipeScriptableObj recipe)
        {
            return unlockedRecipes.Contains(recipe);
        }

        public bool IsRecipeAvailable(CookingRecipeScriptableObj recipe)
        {
            return availableRecipes.Contains(recipe);
        }

        public bool UnlockRecipe(CookingRecipeScriptableObj recipe)
        {
            if (!availableRecipes.Contains(recipe) || recipe.isUnlocked)
                return false;

            recipe.isUnlocked = true;
            availableRecipes.Remove(recipe);
            unlockedRecipes.Add(recipe);
            return true;
        }

        public bool CanCookRecipe(CookingRecipeScriptableObj recipe, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!availableRecipes.Contains(recipe))
            {
                errorMessage = "This recipe is not available!";
                return false;
            }

            if (recipe.isUnlocked)
            {
                errorMessage = "This recipe has already been unlocked!";
                return false;
            }

            // Check if player has required ingredients
            if (recipe.requiredIngredients != null)
            {
                foreach (var ingredient in recipe.requiredIngredients)
                {
                    if (PlayerInventory.Instance.GetItemCount(ingredient.resource) < ingredient.count)
                    {
                        errorMessage = "Not enough ingredients!";
                        return false;
                    }
                }
            }

            return true;
        }
    }
} 