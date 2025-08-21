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

        // Events
        public event System.Action<float> OnElectricityChanged;

        public void Initialize()
        {
            // Subscribe to scene transition events
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.OnSceneTransitionBegin += HandleSceneTransitionBegin;
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from scene transition events
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.OnSceneTransitionBegin -= HandleSceneTransitionBegin;
            }
        }

        private void HandleSceneTransitionBegin(GameMode nextGameMode)
        {
            // No need for consumption coroutine anymore since WorkTasks handle their own consumption
        }

        /// <summary>
        /// Consume electricity based on work progress. Called by WorkTask classes during work.
        /// </summary>
        /// <param name="consumptionRate">Electricity consumed per second</param>
        /// <param name="deltaTime">Time since last frame</param>
        /// <returns>True if enough electricity was available, false if not</returns>
        public bool ConsumeElectricity(float consumptionRate, float deltaTime)
        {
            if (consumptionRate <= 0) return true; // No consumption needed
            
            float consumption = consumptionRate * deltaTime;
            
            if (currentElectricity < consumption)
            {
                // Not enough electricity
                return false;
            }
            
            float previousElectricity = currentElectricity;
            currentElectricity -= consumption;
            
            if (previousElectricity != currentElectricity)
            {
                OnElectricityChanged?.Invoke(GetElectricityPercentage());
            }
            
            return true;
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
    }
}
