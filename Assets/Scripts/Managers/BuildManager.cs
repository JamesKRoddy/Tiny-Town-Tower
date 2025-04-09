using UnityEngine;

namespace Managers
{
    public class BuildManager : MonoBehaviour
    {
        // Singleton instance
        private static BuildManager _instance;

        // Singleton property to get the instance
        public static BuildManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Find the GameManager instance if it hasn't been assigned
                    _instance = FindFirstObjectByType<BuildManager>();
                    if (_instance == null)
                    {
                        Debug.LogWarning("BuildManager instance not found in the scene!");
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject); // Destroy duplicate instances
            }
            else
            {
                _instance = this; // Set the instance
            }
        }
    }
}
