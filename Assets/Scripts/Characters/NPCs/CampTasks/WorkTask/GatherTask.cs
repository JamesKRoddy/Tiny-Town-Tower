using UnityEngine;
using System.Collections;
using Managers;
public class GatherTask : ResourceWorkTask
{
    [SerializeField] private float gatherTime = 20f;
    [SerializeField] private ResourceScriptableObj resource;
    [SerializeField] private int resourceAmount = 1;

    protected override void Start()
    {
        base.Start();
        workType = WorkType.GATHER;
        baseWorkTime = gatherTime;
    }

    protected override IEnumerator WorkCoroutine()
    {
        while (workProgress < baseWorkTime)
        {
            workProgress += Time.deltaTime;
            yield return null;
        }

        CompleteWork();
    }

    protected override void CompleteWork()
    {   
        // Add gathered resources to player inventory
        CampManager.Instance.PlayerInventory.AddItem(resource, resourceAmount);
        Debug.Log($"Gathering completed! Added {resourceAmount} {resource.objectName}");
        
        base.CompleteWork();
    }
} 