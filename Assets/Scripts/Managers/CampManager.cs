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
        private ResearchManager researchManager;
        private CleanlinessManager cleanlinessManager;
        [SerializeField] private WorkManager workManager;
        [SerializeField] private BuildManager buildManager;
        private CookingManager cookingManager;
        private ResourceUpgradeManager resourceUpgradeManager;

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

        // Electricity Management
        [SerializeField] private float maxElectricity = 1000f;
        [SerializeField] private float currentElectricity = 0f;
        [SerializeField] private float electricityConsumptionRate = 1f; // Per second consumption rate

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
            if (researchManager == null) researchManager = gameObject.GetComponentInChildren<ResearchManager>();
            if (cleanlinessManager == null) cleanlinessManager = gameObject.GetComponentInChildren<CleanlinessManager>();
            if (cookingManager == null) cookingManager = gameObject.GetComponentInChildren<CookingManager>();
            if (resourceUpgradeManager == null) resourceUpgradeManager = gameObject.GetComponentInChildren<ResourceUpgradeManager>();

            // Log warnings for any missing managers
            if (researchManager == null) Debug.LogWarning("ResearchManager not found in scene!");
            if (cleanlinessManager == null) Debug.LogWarning("CleanlinessManager not found in scene!");
            if (cookingManager == null) Debug.LogWarning("CookingManager not found in scene!");
            if (resourceUpgradeManager == null) Debug.LogWarning("ResourceUpgradeManager not found in scene!");

            // Initialize managers
            if (researchManager != null) researchManager.Initialize();
            if (cookingManager != null) cookingManager.Initialize();
            if (resourceUpgradeManager != null) resourceUpgradeManager.Initialize();
        }

        private void Update()
        {
            // Gradually decrease cleanliness over time
            currentCleanliness = Mathf.Max(0, currentCleanliness - dirtinessRate * Time.deltaTime);

            // Gradually decrease electricity over time
            currentElectricity = Mathf.Max(0, currentElectricity - electricityConsumptionRate * Time.deltaTime);
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

        // Electricity Methods
        public void AddElectricity(float amount)
        {
            currentElectricity = Mathf.Min(maxElectricity, currentElectricity + amount);
        }

        public bool ConsumeElectricity(float amount)
        {
            if (currentElectricity >= amount)
            {
                currentElectricity -= amount;
                return true;
            }
            return false;
        }

        public float GetElectricityPercentage()
        {
            return (currentElectricity / maxElectricity) * 100f;
        }

        public float GetCurrentElectricity()
        {
            return currentElectricity;
        }

        public float GetMaxElectricity()
        {
            return maxElectricity;
        }

        public bool HasEnoughElectricity(float requiredAmount)
        {
            return currentElectricity >= requiredAmount;
        }
    } 
}