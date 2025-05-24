using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Managers;
using System.Collections;
using System;

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
        private bool isOccupiedWithCrop;
        private bool isDead;
        private Coroutine growthCoroutine;
        private GameObject currentGrowthStage; // Reference to the currently spawned growth stage
        private int currentStageIndex = -1; // Track the current growth stage index

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

        private void UpdateGrowthVisual()
        {
            if (plantedSeed == null || plantedSeed.growthStagePrefabs == null || plantedSeed.growthStagePrefabs.Length == 0)
            {
                Debug.LogError($"<color=red>[FarmBuilding] Cannot update growth visual - missing prefabs for {plantedSeed?.objectName}</color>");
                return;
            }
            
            // Destroy current growth stage if it exists
            if (currentGrowthStage != null)
            {
                Destroy(currentGrowthStage);
            }

            // Spawn new growth stage
            if (plantedSeed.growthStagePrefabs[currentStageIndex] != null)
            {
                currentGrowthStage = Instantiate(plantedSeed.growthStagePrefabs[currentStageIndex], cropPoint);
                currentGrowthStage.transform.localPosition = Vector3.zero;
                currentGrowthStage.transform.localRotation = Quaternion.identity;
            }
            else
            {
                Debug.LogError($"<color=red>[FarmBuilding] Missing prefab for stage {currentStageIndex} of {plantedSeed.objectName}</color>");
            }
        }

        public void PlantCrop(ResourceScriptableObj crop)
        {
            if (crop is SeedScriptableObject seed)
            {
                plantedSeed = seed;
                growthProgress = 0f;
                timeSinceLastTended = 0f;
                isOccupiedWithCrop = true;
                isDead = false;
                currentStageIndex = -1; // Reset stage index when planting new crop

                // Start growth coroutine
                if (growthCoroutine != null)
                {
                    StopCoroutine(growthCoroutine);
                }
                growthCoroutine = StartCoroutine(GrowthCoroutine());
                
                // Initial growth visual update
                UpdateGrowthVisual();
            }
            else
            {
                Debug.LogError($"<color=red>[FarmBuilding] Attempted to plant non-seed resource: {crop.objectName}</color>");
            }
        }

        private IEnumerator GrowthCoroutine()
        {
            while (isOccupiedWithCrop && !isDead && isOperational)
            {
                // Update time since last tending
                timeSinceLastTended += GROWTH_CHECK_INTERVAL;
                
                // Check if crop has died
                if (timeSinceLastTended >= MAX_TIME_WITHOUT_TENDING)
                {
                    Debug.Log($"<color=red>[FarmBuilding] Crop died due to lack of tending: {plantedSeed.objectName}</color>");
                    isDead = true;
                    if (currentGrowthStage != null)
                    {
                        Destroy(currentGrowthStage);
                        currentGrowthStage = null;
                    }
                    // Show dead crop model
                    if (plantedSeed.deadCropPrefab != null)
                    {
                        currentGrowthStage = Instantiate(plantedSeed.deadCropPrefab, cropPoint);
                        currentGrowthStage.transform.localPosition = Vector3.zero;
                        currentGrowthStage.transform.localRotation = Quaternion.identity;
                    }
                    break;
                }

                // Only grow if recently tended and not ready for harvest
                if (timeSinceLastTended < TENDING_THRESHOLD && growthProgress < 100f)
                {
                    // Use the seed's growth rate to determine growth speed
                    float growthAmount = GROWTH_CHECK_INTERVAL * plantedSeed.growthRate;
                    growthProgress += growthAmount;

                    // Calculate new stage index
                    int newStageIndex = Mathf.FloorToInt((growthProgress / 100f) * (plantedSeed.growthStagePrefabs.Length - 1));
                    newStageIndex = Mathf.Clamp(newStageIndex, 0, plantedSeed.growthStagePrefabs.Length - 1);

                    // Only update visual if stage has changed
                    if (newStageIndex != currentStageIndex)
                    {
                        currentStageIndex = newStageIndex;
                        UpdateGrowthVisual();
                    }
                }
                else if (growthProgress >= 100f)
                {
                    yield return new WaitForSeconds(GROWTH_CHECK_INTERVAL);
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
            }
        }

        public void TendPlot()
        {
            timeSinceLastTended = 0f;
        }

        public void ClearPlot()
        {
            // Stop growth coroutine if running
            if (growthCoroutine != null)
            {
                StopCoroutine(growthCoroutine);
                growthCoroutine = null;
            }

            // Clean up current growth stage
            if (currentGrowthStage != null)
            {
                Destroy(currentGrowthStage);
                currentGrowthStage = null;
            }

            plantedSeed = null;
            growthProgress = 0f;
            timeSinceLastTended = 0f;
            isOccupiedWithCrop = false;
            isDead = false;
            currentStageIndex = -1;
        }

        public bool IsOccupied => isOccupiedWithCrop;
        public bool IsDead => isDead;
        public bool NeedsTending => isOccupiedWithCrop && !isDead && timeSinceLastTended >= TENDING_THRESHOLD;
        public bool IsReadyForHarvest => isOccupiedWithCrop && !isDead && growthProgress >= 100f;
        public ResourceScriptableObj PlantedCrop => plantedSeed?.cropToGrow;
        public SeedScriptableObject PlantedSeed => plantedSeed;
        public Transform CropPoint => cropPoint;
    }
} 