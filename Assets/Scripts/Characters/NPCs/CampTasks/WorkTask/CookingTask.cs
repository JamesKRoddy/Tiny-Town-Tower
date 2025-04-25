using UnityEngine;
using System.Collections;
using Managers;

public class CookingTask : WorkTask
{
    [SerializeField] private ResourceScriptableObj[] ingredients;
    [SerializeField] private int[] ingredientAmounts;
    [SerializeField] private ResourceScriptableObj cookedFood;
    [SerializeField] private int foodAmount = 1;
    [SerializeField] private float cookingTime = 10f;

    protected override void Start()
    {
        base.Start();
        workType = WorkType.COOKING;
        baseWorkTime = cookingTime;
    }

    protected override IEnumerator WorkCoroutine()
    {
        // Check if we have all required ingredients
        if (!HasRequiredIngredients())
        {
            Debug.LogWarning("Not enough ingredients for cooking");
            yield break;
        }

        // Consume ingredients
        ConsumeIngredients();

        // Cook the food
        while (workProgress < baseWorkTime)
        {
            workProgress += Time.deltaTime;
            yield return null;
        }

        CompleteWork();
    }

    private bool HasRequiredIngredients()
    {
        for (int i = 0; i < ingredients.Length; i++)
        {
            if (CampManager.Instance.PlayerInventory.GetItemCount(ingredients[i]) < ingredientAmounts[i])
            {
                return false;
            }
        }
        return true;
    }

    private void ConsumeIngredients()
    {
        for (int i = 0; i < ingredients.Length; i++)
        {
            CampManager.Instance.PlayerInventory.RemoveItem(ingredients[i], ingredientAmounts[i]);
        }
    }

    protected override void CompleteWork()
    {
        // Create the cooked food
        for (int i = 0; i < foodAmount; i++)
        {
            Resource food = Instantiate(cookedFood.prefab, transform.position + Random.insideUnitSphere, Quaternion.identity).GetComponent<Resource>();
            food.Initialize(cookedFood);
        }

        base.CompleteWork();
    }
} 