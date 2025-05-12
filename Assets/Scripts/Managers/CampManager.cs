using UnityEngine;
using System.Collections.Generic;
using System.Collections;

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
        private float totalBuildingConsumption = 0f;
        private Dictionary<WorkTask, float> buildingConsumption = new Dictionary<WorkTask, float>();

        // Events
        public event System.Action<float> OnElectricityChanged;
        public event System.Action<float> OnCleanlinessChanged;

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
                StartResourceCoroutines();
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

        private void StartResourceCoroutines()
        {
            StartCoroutine(CleanlinessCoroutine());
            StartCoroutine(ElectricityConsumptionCoroutine());
        }

        private System.Collections.IEnumerator CleanlinessCoroutine()
        {
            WaitForSeconds wait = new WaitForSeconds(0.1f); // Check every 0.1 seconds

            while (true)
            {
                float previousCleanliness = currentCleanliness;
                currentCleanliness = Mathf.Max(0, currentCleanliness - dirtinessRate * 0.1f);
                
                if (previousCleanliness != currentCleanliness)
                {
                    OnCleanlinessChanged?.Invoke(GetCleanlinessPercentage());
                }

                yield return wait;
            }
        }

        private System.Collections.IEnumerator ElectricityConsumptionCoroutine()
        {
            WaitForSeconds wait = new WaitForSeconds(0.1f); // Check every 0.1 seconds

            while (true)
            {
                if (totalBuildingConsumption > 0)
                {
                    float previousElectricity = currentElectricity;
                    currentElectricity = Mathf.Max(0, currentElectricity - totalBuildingConsumption * 0.1f);
                    
                    if (previousElectricity != currentElectricity)
                    {
                        OnElectricityChanged?.Invoke(GetElectricityPercentage());
                    }
                }

                yield return wait;
            }
        }

        // Cleanliness Methods
        public void IncreaseCleanliness(float amount)
        {
            float previousCleanliness = currentCleanliness;
            currentCleanliness = Mathf.Min(maxCleanliness, currentCleanliness + amount);
            
            // Only trigger event if cleanliness actually changed
            if (previousCleanliness != currentCleanliness)
            {
                OnCleanlinessChanged?.Invoke(GetCleanlinessPercentage());
            }
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
        public void RegisterBuildingConsumption(WorkTask building, float consumption)
        {
            if (building == null) return;

            if (buildingConsumption.ContainsKey(building))
            {
                totalBuildingConsumption -= buildingConsumption[building];
            }

            buildingConsumption[building] = consumption;
            totalBuildingConsumption += consumption;
        }

        public void UnregisterBuildingConsumption(WorkTask building)
        {
            if (building == null || !buildingConsumption.ContainsKey(building)) return;

            totalBuildingConsumption -= buildingConsumption[building];
            buildingConsumption.Remove(building);
        }

        public void AddElectricity(float amount)
        {
            float previousElectricity = currentElectricity;
            currentElectricity = Mathf.Min(maxElectricity, currentElectricity + amount);
            
            if (previousElectricity != currentElectricity)
            {
                OnElectricityChanged?.Invoke(GetElectricityPercentage());
            }
        }

        public bool ConsumeElectricity(float amount)
        {
            if (currentElectricity >= amount)
            {
                float previousElectricity = currentElectricity;
                currentElectricity -= amount;
                
                if (previousElectricity != currentElectricity)
                {
                    OnElectricityChanged?.Invoke(GetElectricityPercentage());
                }
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

        public float GetTotalBuildingConsumption()
        {
            return totalBuildingConsumption;
        }
    } 
}