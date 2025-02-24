using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnManager : MonoBehaviour
{
    // Singleton instance
    private static EnemySpawnManager _instance;

    // Singleton property to get the instance
    public static EnemySpawnManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Find the GameManager instance if it hasn't been assigned
                _instance = FindFirstObjectByType<EnemySpawnManager>();
                if (_instance == null)
                {
                    Debug.LogWarning("EnemySpawnManager instance not found in the scene!");
                }
            }
            return _instance;
        }
    }

    private EnemyWaveConfig currentWaveConfig; // Current wave configuration //TODO use GetCurrentRoomDifficulty

    private List<EnemySpawnPoint> spawnPoints;
    private int currentWave = 0;
    private int totalEnemiesInWave; // Total enemies for the current wave
    private int enemiesSpawned; // Number of enemies spawned so far
    private List<GameObject> activeEnemies = new List<GameObject>(); // List of active enemies

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

    public void ResetWaveCount()
    {
        currentWave = 0;
    }

    public void StartSpawningEnemies(EnemyWaveConfig waveConfig)
    {
        if (waveConfig == null)
        {
            Debug.LogError("No waveConfig provided by RogueLiteManager!");
            return;
        }

        currentWaveConfig = waveConfig;

        // Get all spawn points in the scene
        spawnPoints = new List<EnemySpawnPoint>(FindObjectsByType<EnemySpawnPoint>(FindObjectsSortMode.None));
        if (spawnPoints.Count == 0)
        {
            Debug.LogError("No spawn points found!");
            return;
        }

        StartNextWave();
    }

    private void StartNextWave()
    {
        if (currentWave >= currentWaveConfig.maxWaves)
        {
            Debug.Log("All waves completed!");

            switch (GameManager.Instance.CurrentGameMode)
            {
                case CurrentGameMode.ROGUE_LITE:
                    RogueLiteManager.Instance.SetEnemySetupState(EnemySetupState.ALL_WAVES_CLEARED);
                    break;
                case CurrentGameMode.TURRET:
                    TurretManager.Instance.SetEnemySetupState(EnemySetupState.ALL_WAVES_CLEARED);
                    break;
                default:
                    Debug.LogError("Shouldnt be spawning enemies here!!!");
                    break;
            }

            return;
        }

        currentWave++;
        Debug.Log($"Starting Wave {currentWave}");

        // Determine the number of enemies to spawn in this wave
        totalEnemiesInWave = Random.Range(currentWaveConfig.minEnemiesPerWave, currentWaveConfig.maxEnemiesPerWave + 1);
        enemiesSpawned = 0; // Reset for the new wave

        SpawnWave(totalEnemiesInWave);
    }

    private void SpawnWave(int enemyCount)
    {
        for (int i = 0; i < enemyCount; i++)
        {
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        if (spawnPoints.Count == 0 || currentWaveConfig.enemyPrefabs.Length == 0)
            return;

        StartCoroutine(SpawnEnemyWithRetry());
    }

    private IEnumerator SpawnEnemyWithRetry()
    {
        EnemySpawnPoint spawnPoint = null;

        // Wait until a spawn point becomes available
        while (spawnPoint == null)
        {
            foreach (var potentialSpawnPoint in spawnPoints)
            {
                if (potentialSpawnPoint != null && potentialSpawnPoint.IsAvailable())
                {
                    spawnPoint = potentialSpawnPoint;
                    break;
                }
            }

            if (spawnPoint == null)
            {
                yield return new WaitForSeconds(0.1f); // Small delay before checking again
            }
        }

        GameObject enemyPrefab = currentWaveConfig.enemyPrefabs[Random.Range(0, currentWaveConfig.enemyPrefabs.Length)];

        // Delegate spawning to the spawn point
        GameObject enemy = spawnPoint.SpawnEnemy(enemyPrefab);

        if (enemy != null)
        {
            activeEnemies.Add(enemy);
            enemiesSpawned++; // Track the number of enemies spawned

            // Add listener to remove the enemy when it's destroyed
            enemy.GetComponent<EnemyBase>().OnEnemyKilled += () =>
            {
                activeEnemies.Remove(enemy);
                CheckForWaveCompletion();
            };
        }
    }

    private void CheckForWaveCompletion()
    {
        // Check if all enemies have been spawned and all active enemies are killed
        if (enemiesSpawned >= totalEnemiesInWave && activeEnemies.Count == 0)
        {
            Debug.Log("Wave Complete!");
            StartNextWave();
        }
    }

    /// <summary>
    /// USed by the PlacementManager to make sure a turrent is not blocking the enemy path
    /// </summary>
    /// <returns></returns>
    public Vector3? SpawnPointPosition()
    {
        if (spawnPoints.Count != 0)
        {
            return spawnPoints[0].transform.position;
        }
        return null;
    }
}
