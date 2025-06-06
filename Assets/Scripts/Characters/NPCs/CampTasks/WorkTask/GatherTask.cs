using UnityEngine;
using System.Collections;
using Managers;

public class GatherTask : WorkTask
{
    [SerializeField] private float gatherTime = 20f;
    [SerializeField] private ResourceScriptableObj resource;

    protected override void Start()
    {
        base.Start();
        baseWorkTime = gatherTime;
    }
    protected override void CompleteWork()
    {   
        // Add gathered resources to player inventory
        PlayerInventory.Instance.AddItem(resource, resourceAmount);
        Debug.Log($"Gathering completed! Added {resourceAmount} {resource.objectName}");
        
        base.CompleteWork();
    }
} 