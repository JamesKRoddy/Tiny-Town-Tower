using System;
using System.Collections.Generic;
using UnityEngine;

public class RogueLiteManager : MonoBehaviour
{
    // Singleton instance
    private static RogueLiteManager _instance;

    // Singleton property to get the instance
    public static RogueLiteManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Find the GameManager instance if it hasn't been assigned
                _instance = FindFirstObjectByType<RogueLiteManager>();
                if (_instance == null)
                {
                    Debug.LogWarning("RogueLiteManager instance not found in the scene!");
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

    public List<EnemyWaveConfig> waveConfigs; // List of all possible wave configurations

    public BuildingType currentBuilding;
    public int currentFloor;

    public Action OnLevelReady;

    public EnemyWaveConfig GetWaveConfig()
    {
        foreach (var config in waveConfigs)
        {
            // Match building, environment, and floor
            if (config.buildingType == currentBuilding &&
                config.floor == currentFloor)
            {
                return config;
            }
        }

        // Return a default or null if no exact match is found
        Debug.LogWarning("No matching waveConfig found. Returning null.");
        return null;
    }
}
