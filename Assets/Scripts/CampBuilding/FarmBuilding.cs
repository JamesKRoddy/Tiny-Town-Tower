using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Managers;
using System.Collections;

namespace CampBuilding
{
    [RequireComponent(typeof(FarmingTask))]
    public class FarmBuilding : Building
    {
        [Header("Farm Settings")]
        [SerializeField] private Transform cropPoint; // Where the crop will be planted
        private SeedScriptableObject plantedSeed;
        private float growthProgress;
        private float timeSinceLastTended;
        private bool isOccupied;
        private bool isDead;
        private Coroutine growthCoroutine;

        private const float MAX_TIME_WITHOUT_TENDING = 60f; // 1 minute without tending before death
        private const float TENDING_THRESHOLD = 30f; // Need tending every 30 seconds
        private const float GROWTH_CHECK_INTERVAL = 0.1f; // How often to check growth conditions

        private FarmingTask farmingTask;

        protected override void Start()
        {
            base.Start();
            farmingTask = GetComponent<FarmingTask>();
        }

        public override void SetupBuilding(BuildingScriptableObj buildingScriptableObj)
        {
            base.SetupBuilding(buildingScriptableObj);
            
            // Setup crop point if not set
            if (cropPoint == null)
            {
                GameObject point = new GameObject("CropPoint");
                point.transform.SetParent(transform);
                point.transform.localPosition = new Vector3(0, 0, 0);
                cropPoint = point.transform;
            }
        }

        public override void CompleteConstruction()
        {
            base.CompleteConstruction();
            // Register with any necessary managers
        }

        public override void StartDestruction()
        {
            // Stop growth coroutine if running
            if (growthCoroutine != null)
            {
                StopCoroutine(growthCoroutine);
                growthCoroutine = null;
            }
            // Unregister from any managers
            base.StartDestruction();
        }

        public void PlantCrop(ResourceScriptableObj crop)
        {
            if (crop is SeedScriptableObject seed)
            {
                plantedSeed = seed;
                growthProgress = 0f;
                timeSinceLastTended = 0f;
                isOccupied = true;
                isDead = false;

                // Start growth coroutine
                if (growthCoroutine != null)
                {
                    StopCoroutine(growthCoroutine);
                }
                growthCoroutine = StartCoroutine(GrowthCoroutine());
                Debug.Log($"<color=blue>[FarmBuilding] Planted {seed.objectName} with growth rate {seed.growthRate}</color>");
            }
            else
            {
                Debug.LogError($"<color=red>[FarmBuilding] Attempted to plant non-seed resource: {crop.objectName}</color>");
            }
        }

        private IEnumerator GrowthCoroutine()
        {
            while (isOccupied && !isDead && isOperational)
            {
                // Update time since last tending
                timeSinceLastTended += GROWTH_CHECK_INTERVAL;
                
                // Check if crop has died
                if (timeSinceLastTended >= MAX_TIME_WITHOUT_TENDING)
                {
                    isDead = true;
                    Debug.Log($"<color=red>[FarmBuilding] Crop has died due to lack of tending</color>");
                    break;
                }

                // Only grow if recently tended and not ready for harvest
                if (timeSinceLastTended < TENDING_THRESHOLD && growthProgress < 100f)
                {
                    // Use the seed's growth rate to determine growth speed
                    float growthAmount = GROWTH_CHECK_INTERVAL * plantedSeed.growthRate;
                    growthProgress += growthAmount;
                    Debug.Log($"<color=green>[FarmBuilding] Growth progress: {growthProgress:F1}% (Rate: {plantedSeed.growthRate}x)</color>");
                }
                else if (growthProgress >= 100f)
                {
                    Debug.Log($"<color=yellow>[FarmBuilding] Crop is ready for harvest</color>");
                    yield return new WaitForSeconds(GROWTH_CHECK_INTERVAL);
                }
                else
                {
                    Debug.Log($"<color=orange>[FarmBuilding] Growth paused - needs tending</color>");
                }

                yield return new WaitForSeconds(GROWTH_CHECK_INTERVAL);
            }

            // Clean up coroutine reference
            growthCoroutine = null;
        }

        public void StartHarvesting()
        {
            // Stop growth coroutine if running
            if (growthCoroutine != null)
            {
                StopCoroutine(growthCoroutine);
                growthCoroutine = null;
                Debug.Log($"<color=cyan>[FarmBuilding] Growth stopped - harvesting in progress</color>");
            }
        }

        public void TendPlot()
        {
            timeSinceLastTended = 0f;
            Debug.Log($"<color=blue>[FarmBuilding] Crop has been tended</color>");
        }

        public void ClearPlot()
        {
            // Stop growth coroutine if running
            if (growthCoroutine != null)
            {
                StopCoroutine(growthCoroutine);
                growthCoroutine = null;
            }

            plantedSeed = null;
            growthProgress = 0f;
            timeSinceLastTended = 0f;
            isOccupied = false;
            isDead = false;
            Debug.Log($"<color=cyan>[FarmBuilding] Plot has been cleared</color>");
        }

        public bool IsOccupied => isOccupied;
        public bool IsDead => isDead;
        public bool NeedsTending => isOccupied && !isDead && timeSinceLastTended >= TENDING_THRESHOLD;
        public bool IsReadyForHarvest => isOccupied && !isDead && growthProgress >= 100f;
        public ResourceScriptableObj PlantedCrop => plantedSeed?.cropToGrow;
        public SeedScriptableObject PlantedSeed => plantedSeed;
        public Transform CropPoint => cropPoint;
    }
} 