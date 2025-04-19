using UnityEngine;

namespace Managers
{
    public class CleanlinessManager : MonoBehaviour
    {
        [SerializeField] private float maxCleanliness = 100f;
        [SerializeField] private float currentCleanliness = 100f;
        [SerializeField] private float dirtinessRate = 0.1f;

        private void Update()
        {
            // Gradually decrease cleanliness over time
            currentCleanliness = Mathf.Max(0, currentCleanliness - dirtinessRate * Time.deltaTime);
        }

        public void IncreaseCleanliness(float amount)
        {
            currentCleanliness = Mathf.Min(maxCleanliness, currentCleanliness + amount);
            Debug.Log($"Cleanliness increased by {amount}. Current: {currentCleanliness}");
        }

        public float GetCleanliness()
        {
            return currentCleanliness;
        }

        public float GetCleanlinessPercentage()
        {
            return (currentCleanliness / maxCleanliness) * 100f;
        }
    } 
}