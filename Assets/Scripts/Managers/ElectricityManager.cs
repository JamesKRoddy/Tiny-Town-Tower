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
            }

            buildingConsumption[building] = consumption;
            totalBuildingConsumption += consumption;
        }

        public void UnregisterBuildingConsumption(WorkTask building)
        {
            if (building == null || !buildingConsumption.ContainsKey(building)) return;

            float consumption = buildingConsumption[building];
            totalBuildingConsumption -= consumption;
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
