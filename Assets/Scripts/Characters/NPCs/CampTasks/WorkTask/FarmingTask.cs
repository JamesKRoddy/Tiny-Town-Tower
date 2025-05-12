using UnityEngine;
using System.Collections;

public class FarmingTask : WorkTask
{
    [SerializeField] private ResourceScriptableObj cropType;
    [SerializeField] private int harvestAmount = 3;
    [SerializeField] private float growthTime = 30f;
    [SerializeField] private float harvestTime = 5f;

    private bool isGrowing = false;
    private float currentGrowth = 0f;

    protected override void Start()
    {
        base.Start();
        baseWorkTime = harvestTime;
    }

    protected override IEnumerator WorkCoroutine()
    {
        if (!isGrowing)
        {
            // Start growing phase
            isGrowing = true;
            currentGrowth = 0f;
            
            while (currentGrowth < growthTime)
            {
                currentGrowth += Time.deltaTime;
                yield return null;
            }

            // Crop is ready for harvest
            isGrowing = false;
        }

        // Harvest phase
        while (workProgress < baseWorkTime)
        {
            workProgress += Time.deltaTime;
            yield return null;
        }

        CompleteWork();
    }

    protected override void CompleteWork()
    {
        if (!isGrowing)
        {
            // Create harvested resources
            for (int i = 0; i < harvestAmount; i++)
            {
                Resource resource = Instantiate(cropType.prefab, transform.position + Random.insideUnitSphere, Quaternion.identity).GetComponent<Resource>();
                resource.Initialize(cropType);
            }
        }

        base.CompleteWork();
    }
} 