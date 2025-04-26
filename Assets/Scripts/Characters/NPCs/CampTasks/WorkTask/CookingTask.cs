using UnityEngine;
using System.Collections;
using Managers;

public class CookingTask : WorkTask
{
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
        if (!HasRequiredResources())
        {
            Debug.LogWarning("Not enough ingredients for cooking");
            yield break;
        }

        // Cook the food
        while (workProgress < baseWorkTime)
        {
            workProgress += Time.deltaTime;
            yield return null;
        }

        CompleteWork();
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