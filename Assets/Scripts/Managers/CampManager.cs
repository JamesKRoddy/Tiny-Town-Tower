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
        private WorkManager workManager;
        private BuildManager buildManager;
        private CookingManager cookingManager;
        private ResourceUpgradeManager resourceUpgradeManager;
        private ElectricityManager electricityManager;
        private FarmingManager farmingManager;

        // Public access to other managers
        public ResearchManager ResearchManager => researchManager;
        public CleanlinessManager CleanlinessManager => cleanlinessManager;
        public WorkManager WorkManager => workManager;
        public BuildManager BuildManager => buildManager;
        public CookingManager CookingManager => cookingManager;
        public ResourceUpgradeManager ResourceUpgradeManager => resourceUpgradeManager;
        public ElectricityManager ElectricityManager => electricityManager;
        public FarmingManager FarmingManager => farmingManager;

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
            // Find and cache references to other managers
            if (researchManager == null) researchManager = gameObject.GetComponentInChildren<ResearchManager>();
            if (cleanlinessManager == null) cleanlinessManager = gameObject.GetComponentInChildren<CleanlinessManager>();
            if (cookingManager == null) cookingManager = gameObject.GetComponentInChildren<CookingManager>();
            if (resourceUpgradeManager == null) resourceUpgradeManager = gameObject.GetComponentInChildren<ResourceUpgradeManager>();
            if (workManager == null) workManager = gameObject.GetComponentInChildren<WorkManager>();
            if (buildManager == null) buildManager = gameObject.GetComponentInChildren<BuildManager>();
            if (electricityManager == null) electricityManager = gameObject.GetComponentInChildren<ElectricityManager>();
            if (farmingManager == null) farmingManager = gameObject.GetComponentInChildren<FarmingManager>();
            // Log warnings for any missing managers
            if (researchManager == null) Debug.LogWarning("ResearchManager not found in scene!");
            if (cleanlinessManager == null) Debug.LogWarning("CleanlinessManager not found in scene!");
            if (cookingManager == null) Debug.LogWarning("CookingManager not found in scene!");
            if (resourceUpgradeManager == null) Debug.LogWarning("ResourceUpgradeManager not found in scene!");
            if (workManager == null) Debug.LogWarning("WorkManager not found in scene!");
            if (buildManager == null) Debug.LogWarning("BuildManager not found in scene!");
            if (electricityManager == null) Debug.LogWarning("ElectricityManager not found in scene!");
            if (farmingManager == null) Debug.LogWarning("FarmingManager not found in scene!");
            // Initialize managers
            if (researchManager != null) researchManager.Initialize();
            if (cookingManager != null) cookingManager.Initialize();
            if (resourceUpgradeManager != null) resourceUpgradeManager.Initialize();
            if (electricityManager != null) electricityManager.Initialize();
            if (cleanlinessManager != null) cleanlinessManager.Initialize();
            if (farmingManager != null) farmingManager.Initialize();
        }

    } 
}