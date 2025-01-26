using System;
using UnityEngine;

public class TurretManager : MonoBehaviour
{
    // Singleton instance
    private static TurretManager _instance;

    // Singleton property to get the instance
    public static TurretManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Find the GameManager instance if it hasn't been assigned
                _instance = FindFirstObjectByType<TurretManager>();
                if (_instance == null)
                {
                    Debug.LogWarning("TurretManager instance not found in the scene!");
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
            DontDestroyOnLoad(gameObject); // Optionally persist across scenes
        }
    }
}
