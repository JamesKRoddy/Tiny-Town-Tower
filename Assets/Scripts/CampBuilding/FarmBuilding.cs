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
        [SerializeField] private int maxFarmPlots = 4;
        [SerializeField] private Transform[] farmPlotPoints; // Points where crops can be planted
        private List<FarmPlot> farmPlots = new List<FarmPlot>();

        private FarmingTask farmingTask;

        protected override void Start()
        {
            base.Start();
            farmingTask = GetComponent<FarmingTask>();
        }

        public override void SetupBuilding(BuildingScriptableObj buildingScriptableObj)
        {
            base.SetupBuilding(buildingScriptableObj);
            
            // Setup farm plot points if not set
            if (farmPlotPoints == null || farmPlotPoints.Length == 0)
            {
                farmPlotPoints = new Transform[maxFarmPlots];
                for (int i = 0; i < maxFarmPlots; i++)
                {
                    GameObject plotPoint = new GameObject($"FarmPlot_{i}");
                    plotPoint.transform.SetParent(transform);
                    // Arrange plots in a grid pattern
                    float x = (i % 2) * 2f - 1f;
                    float z = (i / 2) * 2f - 1f;
                    plotPoint.transform.localPosition = new Vector3(x, 0, z);
                    farmPlotPoints[i] = plotPoint.transform;
                }
            }

            // Initialize farm plots
            for (int i = 0; i < maxFarmPlots; i++)
            {
                farmPlots.Add(new FarmPlot(farmPlotPoints[i]));
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

        public FarmPlot GetAvailablePlot()
        {
            return farmPlots.Find(plot => !plot.IsOccupied);
        }

        public FarmPlot GetPlotNeedingTending()
        {
            return farmPlots.Find(plot => plot.NeedsTending);
        }

        public FarmPlot GetPlotNeedingHarvest()
        {
            return farmPlots.Find(plot => plot.IsReadyForHarvest);
        }

        public FarmPlot GetDeadPlot()
        {
            return farmPlots.Find(plot => plot.IsDead);
        }

        public int GetTotalPlots()
        {
            return farmPlots.Count;
        }

        public int GetOccupiedPlots()
        {
            return farmPlots.Count(plot => plot.IsOccupied);
        }
    }

    // Helper class to manage individual farm plots
    public class FarmPlot
    {
        public Transform plotTransform;
        public ResourceScriptableObj plantedCrop;
        public float growthProgress;
        public float timeSinceLastTended;
        public bool isOccupied;
        public bool isDead;

        private const float MAX_TIME_WITHOUT_TENDING = 60f; // 1 minute without tending before death
        private const float TENDING_THRESHOLD = 30f; // Need tending every 30 seconds

        public FarmPlot(Transform transform)
        {
            plotTransform = transform;
            Reset();
        }

        public void Reset()
        {
            plantedCrop = null;
            growthProgress = 0f;
            timeSinceLastTended = 0f;
            isOccupied = false;
            isDead = false;
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
            Reset();
        }

        public bool IsOccupied => isOccupied;
        public bool IsDead => isDead;
        public bool NeedsTending => isOccupied && !isDead && timeSinceLastTended >= TENDING_THRESHOLD;
        public bool IsReadyForHarvest => isOccupied && !isDead && growthProgress >= 100f;
    }
} 