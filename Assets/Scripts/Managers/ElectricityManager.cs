using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Managers
{
    public class ElectricityManager : MonoBehaviour
    {
        [Header("Electricity Settings")]
        [SerializeField] private float maxElectricity = 1000f;
        [SerializeField] private float currentElectricity = 0f;
        private float totalBuildingConsumption = 0f;
        private Dictionary<WorkTask, float> buildingConsumption = new Dictionary<WorkTask, float>();

        private Coroutine electricityConsumptionCoroutine;

        // Events
        public event System.Action<float> OnElectricityChanged;

        public void Initialize()
        {
            electricityConsumptionCoroutine = StartCoroutine(ElectricityConsumptionCoroutine());
        }

        private IEnumerator ElectricityConsumptionCoroutine()
        {
            WaitForSeconds wait = new WaitForSeconds(0.5f); // Check every 0.5 seconds

            while (true)
            {
                if (totalBuildingConsumption > 0)
                {
                    float previousElectricity = currentElectricity;
                    float consumption = totalBuildingConsumption;
                    currentElectricity = Mathf.Max(0, currentElectricity - consumption);
                    
                    if (previousElectricity != currentElectricity)
                    {
                        Debug.Log($"[ElectricityManager] Electricity consumed: {consumption:F2}, Total: {currentElectricity:F2}, Buildings consuming: {totalBuildingConsumption:F2}");
                        OnElectricityChanged?.Invoke(GetElectricityPercentage());
                    }
                }

                yield return wait;
            }
        }

        public void RegisterBuildingConsumption(WorkTask building, float consumption)
        {
            if (building == null) return;

            if (buildingConsumption.ContainsKey(building))
            {
                totalBuildingConsumption -= buildingConsumption[building];
                Debug.Log($"[ElectricityManager] Updating building consumption for {building.GetType().Name}: {buildingConsumption[building]} -> {consumption}");
            }
            else
            {
                Debug.Log($"[ElectricityManager] Registering new building consumption: {building.GetType().Name} consuming {consumption}");
            }

            buildingConsumption[building] = consumption;
            totalBuildingConsumption += consumption;
            Debug.Log($"[ElectricityManager] Total building consumption: {totalBuildingConsumption}");
        }

        public void UnregisterBuildingConsumption(WorkTask building)
        {
            if (building == null || !buildingConsumption.ContainsKey(building)) return;

            float consumption = buildingConsumption[building];
            totalBuildingConsumption -= consumption;
            buildingConsumption.Remove(building);
            Debug.Log($"[ElectricityManager] Unregistered building consumption: {building.GetType().Name} was consuming {consumption}, New total: {totalBuildingConsumption}");
        }

        public void AddElectricity(float amount)
        {
            float previousElectricity = currentElectricity;
            currentElectricity = Mathf.Min(maxElectricity, currentElectricity + amount);
            
            if (previousElectricity != currentElectricity)
            {
                Debug.Log($"[ElectricityManager] Electricity added: {amount}, New total: {currentElectricity}");
                OnElectricityChanged?.Invoke(GetElectricityPercentage());
            }
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
