using UnityEngine;
using Managers;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(CookingTask))]
public class CanteenBuilding : Building
{
    // Event to notify when food becomes available
    public event Action<CanteenBuilding> OnFoodAvailable;

    [System.Serializable]
    private class StoredMeal
    {
        public CookingRecipeScriptableObj recipe;
        public GameObject visualRepresentation;

        public StoredMeal(CookingRecipeScriptableObj recipe, GameObject visualRepresentation)
        {
            this.recipe = recipe;
            this.visualRepresentation = visualRepresentation;
        }
    }

    [Header("Canteen Settings")]
    [SerializeField] private int maxStoredMeals = 10;
    [SerializeField] private Transform foodStoragePoint; // Where the food will be visually stored
    private List<StoredMeal> storedMeals = new List<StoredMeal>();

    private CookingTask cookingTask;

    protected override void Start()
    {
        base.Start();
        cookingTask = GetComponent<CookingTask>();
    }

    public override void SetupBuilding(BuildingScriptableObj buildingScriptableObj)
    {
        base.SetupBuilding(buildingScriptableObj);
        
        // Setup food storage point if not set
        if (foodStoragePoint == null)
        {
            GameObject storagePoint = new GameObject("FoodStoragePoint");
            storagePoint.transform.SetParent(transform);
            storagePoint.transform.localPosition = new Vector3(0, 0, 2f); // Place in front of building
            foodStoragePoint = storagePoint.transform;
        }
    }

    public override void CompleteConstruction()
    {
        base.CompleteConstruction();
        CampManager.Instance.CookingManager.RegisterCanteen(this);
    }

    public override void StartDestruction()
    {
        CampManager.Instance.CookingManager.UnregisterCanteen(this);
        base.StartDestruction();
    }

    public bool HasAvailableMeals()
    {
        return storedMeals.Count > 0;
    }

    public bool CanStoreMoreMeals()
    {
        return storedMeals.Count < maxStoredMeals;
    }

    public void AddMeal(CookingRecipeScriptableObj recipe)
    {
        if (CanStoreMoreMeals())
        {
            // TODO: Spawn visual representation of food based on recipe
            GameObject foodVisual = null; // Replace with actual food visual spawning
            storedMeals.Add(new StoredMeal(recipe, foodVisual));
            
            // Notify listeners that food is available
            OnFoodAvailable?.Invoke(this);
        }
    }

    public CookingRecipeScriptableObj RemoveMeal()
    {
        if (storedMeals.Count > 0)
        {
            StoredMeal meal = storedMeals[0];
            storedMeals.RemoveAt(0);
            
            // TODO: Remove visual representation of food
            if (meal.visualRepresentation != null)
            {
                Destroy(meal.visualRepresentation);
            }
            
            return meal.recipe;
        }
        return null;
    }

    public Transform GetFoodStoragePoint()
    {
        return foodStoragePoint;
    }

    public int GetStoredMealsCount()
    {
        return storedMeals.Count;
    }

    public int GetMaxStoredMeals()
    {
        return maxStoredMeals;
    }
} 