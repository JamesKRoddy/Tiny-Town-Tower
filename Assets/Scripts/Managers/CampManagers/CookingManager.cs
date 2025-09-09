using UnityEngine;
using System.Collections.Generic;
using System;

namespace Managers
{
    public class CookingManager : MonoBehaviour
    {
        [SerializeField] private List<CookingRecipeScriptableObj> allRecipes = new List<CookingRecipeScriptableObj>();
        private List<CookingRecipeScriptableObj> availableRecipes = new List<CookingRecipeScriptableObj>();
        private List<CookingRecipeScriptableObj> unlockedRecipes = new List<CookingRecipeScriptableObj>();
        private List<CanteenBuilding> registeredCanteens = new List<CanteenBuilding>();

        // Event to notify when food becomes available in a canteen
        public event Action<CanteenBuilding> OnFoodAvailable;

        public void Initialize()
        {
            availableRecipes = new List<CookingRecipeScriptableObj>(allRecipes);
            unlockedRecipes = new List<CookingRecipeScriptableObj>();
            registeredCanteens = new List<CanteenBuilding>();
        }

        public void RegisterCanteen(CanteenBuilding canteen)
        {
            if (canteen != null && !registeredCanteens.Contains(canteen))
            {
                registeredCanteens.Add(canteen);
                // Subscribe to the canteen's food availability event
                canteen.OnFoodAvailable += HandleCanteenFoodAvailable;
            }
        }

        public void UnregisterCanteen(CanteenBuilding canteen)
        {
            if (canteen != null)
            {
                registeredCanteens.Remove(canteen);
                // Unsubscribe from the canteen's food availability event
                canteen.OnFoodAvailable -= HandleCanteenFoodAvailable;
            }
        }

        private void HandleCanteenFoodAvailable(CanteenBuilding canteen)
        {
            OnFoodAvailable?.Invoke(canteen);
        }

        public List<CanteenBuilding> GetRegisteredCanteens()
        {
            return new List<CanteenBuilding>(registeredCanteens);
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
            if (recipe.requiredResources != null)
            {
                foreach (var ingredient in recipe.requiredResources)
                {
                    if (PlayerInventory.Instance.GetItemCount(ingredient.resourceScriptableObj) < ingredient.count)
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