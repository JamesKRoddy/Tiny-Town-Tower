using System.Collections.Generic;
using UnityEngine;

public class CampInventory : MonoBehaviour
{
    private static CampInventory _instance;
    public static CampInventory Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<CampInventory>();
                if (_instance == null)
                {
                    Debug.LogWarning("CampInventory instance not found in the scene!");
                }
            }
            return _instance;
        }
    }

    private Dictionary<ResourceScriptableObj, int> resources = new Dictionary<ResourceScriptableObj, int>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    public void AddResource(ResourceScriptableObj resource, int amount = 1)
    {
        if (resources.ContainsKey(resource))
        {
            resources[resource] += amount;
        }
        else
        {
            resources.Add(resource, amount);
        }
    }

    public bool RemoveResource(ResourceScriptableObj resource, int amount = 1)
    {
        if (resources.ContainsKey(resource) && resources[resource] >= amount)
        {
            resources[resource] -= amount;
            if (resources[resource] <= 0)
            {
                resources.Remove(resource);
            }
            return true;
        }
        return false;
    }

    public int GetResourceCount(ResourceScriptableObj resource)
    {
        return resources.ContainsKey(resource) ? resources[resource] : 0;
    }

    public bool HasResources(ResourceScriptableObj[] requiredResources, int[] amounts)
    {
        if (requiredResources.Length != amounts.Length)
        {
            Debug.LogError("Resource arrays length mismatch");
            return false;
        }

        for (int i = 0; i < requiredResources.Length; i++)
        {
            if (GetResourceCount(requiredResources[i]) < amounts[i])
            {
                return false;
            }
        }
        return true;
    }

    public bool ConsumeResources(ResourceScriptableObj[] requiredResources, int[] amounts)
    {
        if (!HasResources(requiredResources, amounts))
        {
            return false;
        }

        for (int i = 0; i < requiredResources.Length; i++)
        {
            RemoveResource(requiredResources[i], amounts[i]);
        }
        return true;
    }
} 