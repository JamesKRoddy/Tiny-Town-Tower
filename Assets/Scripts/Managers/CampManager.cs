using UnityEngine;
using System.Collections.Generic;

namespace Managers
{
    public class CampManager : MonoBehaviour
    {
        private static CampManager _instance;
        public static CampManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CampManager>();
                    if (_instance == null)
                    {
                        Debug.LogWarning("CampManager instance not found in the scene!");
                    }
                }
                return _instance;
            }
        }

        // References to other managers
        [SerializeField] private ResearchManager researchManager;
        [SerializeField] private CleanlinessManager cleanlinessManager;
        [SerializeField] private WorkManager workManager;
        [SerializeField] private BuildManager buildManager;
        [SerializeField] private CookingManager cookingManager;
        [SerializeField] private ResourceUpgradeManager resourceUpgradeManager;

        // Public access to other managers
        public ResearchManager ResearchManager => researchManager;
        public CleanlinessManager CleanlinessManager => cleanlinessManager;
        public PlayerInventory PlayerInventory => PlayerInventory.Instance;
        public WorkManager WorkManager => workManager;
        public BuildManager BuildManager => buildManager;
        public CookingManager CookingManager => cookingManager;
        public ResourceUpgradeManager ResourceUpgradeManager => resourceUpgradeManager;


        // Inventory Management
        private Dictionary<ResourceScriptableObj, int> resources = new Dictionary<ResourceScriptableObj, int>();

        // Research Management
        private int researchPoints = 0;

        // Cleanliness Management
        [SerializeField] private float maxCleanliness = 100f;
        [SerializeField] private float currentCleanliness = 100f;
        [SerializeField] private float dirtinessRate = 0.1f;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
                InitializeManagers();
            }
        }

        private void InitializeManagers()
        {
            // Find and cache references to other managers if not set in inspector
            if (researchManager == null) researchManager = FindFirstObjectByType<ResearchManager>();
            if (cleanlinessManager == null) cleanlinessManager = FindFirstObjectByType<CleanlinessManager>();

            // Log warnings for any missing managers
            if (researchManager == null) Debug.LogWarning("ResearchManager not found in scene!");
            if (cleanlinessManager == null) Debug.LogWarning("CleanlinessManager not found in scene!");

            researchManager.Initialize();
            //cleanlinessManager.Initialize();
            //workManager.Initialize();
            //buildManager.Initialize();
        }

        private void Update()
        {
            // Gradually decrease cleanliness over time
            currentCleanliness = Mathf.Max(0, currentCleanliness - dirtinessRate * Time.deltaTime);
        }

        // Inventory Methods
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

        // Research Methods
        public void AddResearchPoints(int points)
        {
            researchPoints += points;
            Debug.Log($"Total research points: {researchPoints}");
        }

        public bool SpendResearchPoints(int points)
        {
            if (researchPoints >= points)
            {
                researchPoints -= points;
                return true;
            }
            return false;
        }

        public int GetResearchPoints()
        {
            return researchPoints;
        }

        // Cleanliness Methods
        public void IncreaseCleanliness(float amount)
        {
            currentCleanliness = Mathf.Min(maxCleanliness, currentCleanliness + amount);
        }

        public float GetCleanliness()
        {
            return currentCleanliness;
        }

        public float GetCleanlinessPercentage()
        {
            return (currentCleanliness / maxCleanliness) * 100f;
        }
    } 
}