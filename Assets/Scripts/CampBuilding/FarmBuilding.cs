using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Managers;

namespace CampBuilding
{
    [RequireComponent(typeof(FarmingTask))]
    public class FarmBuilding : Building
    {
        [Header("Farm Settings")]
        [SerializeField] private Transform cropPoint; // Where the crop will be planted
        private ResourceScriptableObj plantedCrop;
        private float growthProgress;
        private float timeSinceLastTended;
        private bool isOccupied;
        private bool isDead;

        private const float MAX_TIME_WITHOUT_TENDING = 60f; // 1 minute without tending before death
        private const float TENDING_THRESHOLD = 30f; // Need tending every 30 seconds

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
            // Unregister from any managers
            base.StartDestruction();
        }

        public void PlantCrop(ResourceScriptableObj crop)
        {
            plantedCrop = crop;
            growthProgress = 0f;
            timeSinceLastTended = 0f;
            isOccupied = true;
            isDead = false;
        }

        public void UpdateGrowth(float deltaTime)
        {
            if (!isOccupied || isDead) return;

            timeSinceLastTended += deltaTime;
            if (timeSinceLastTended >= MAX_TIME_WITHOUT_TENDING)
            {
                isDead = true;
                return;
            }

            if (timeSinceLastTended < TENDING_THRESHOLD)
            {
                growthProgress += deltaTime;
            }
        }

        public void TendPlot()
        {
            timeSinceLastTended = 0f;
        }

        public void ClearPlot()
        {
            plantedCrop = null;
            growthProgress = 0f;
            timeSinceLastTended = 0f;
            isOccupied = false;
            isDead = false;
        }

        public bool IsOccupied => isOccupied;
        public bool IsDead => isDead;
        public bool NeedsTending => isOccupied && !isDead && timeSinceLastTended >= TENDING_THRESHOLD;
        public bool IsReadyForHarvest => isOccupied && !isDead && growthProgress >= 100f;
        public ResourceScriptableObj PlantedCrop => plantedCrop;
        public Transform CropPoint => cropPoint;
    }
} 