using UnityEngine;
using Managers;

[RequireComponent(typeof(CookingTask))]
public class CanteenBuilding : Building
{
    [Header("Canteen Settings")]
    [SerializeField] private int maxStoredMeals = 10;
    [SerializeField] private Transform foodStoragePoint; // Where the food will be visually stored
    private int currentStoredMeals = 0;

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

    public bool HasAvailableMeals()
    {
        return currentStoredMeals > 0;
    }

    public bool CanStoreMoreMeals()
    {
        return currentStoredMeals < maxStoredMeals;
    }

    public void AddMeal()
    {
        if (CanStoreMoreMeals())
        {
            currentStoredMeals++;
            // TODO: Spawn visual representation of food
        }
    }

    public void RemoveMeal()
    {
        if (currentStoredMeals > 0)
        {
            currentStoredMeals--;
            // TODO: Remove visual representation of food
        }
    }

    public Transform GetFoodStoragePoint()
    {
        return foodStoragePoint;
    }

    public int GetStoredMealsCount()
    {
        return currentStoredMeals;
    }

    public int GetMaxStoredMeals()
    {
        return maxStoredMeals;
    }
} 