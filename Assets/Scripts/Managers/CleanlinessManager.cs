using UnityEngine;

namespace Managers
{
    public class CleanlinessManager : MonoBehaviour
    {
                // Cleanliness Management
        [SerializeField] private float maxCleanliness = 100f;
        [SerializeField] private float currentCleanliness = 100f;
        [SerializeField] private float dirtinessRate = 0.1f;

        private Coroutine cleanlinessCoroutine;

        // Events
        public event System.Action<float> OnCleanlinessChanged;
        
        public void Initialize()
        {
            cleanlinessCoroutine = StartCoroutine(CleanlinessCoroutine());
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
    } 
}