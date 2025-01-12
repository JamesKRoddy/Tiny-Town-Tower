using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnManager : MonoBehaviour
{
    private EnemyWaveConfig waveConfig; // Current wave configuration //TODO use GetCurrentRoomDifficulty

    private List<EnemySpawnPoint> spawnPoints;
    private int currentWave = 0;
    private int totalEnemiesInWave; // Total enemies for the current wave
    private int enemiesSpawned; // Number of enemies spawned so far
    private List<GameObject> activeEnemies = new List<GameObject>(); // List of active enemies

    private void Start()
    {
        // Subscribe to RogueLiteManager event
        RogueLiteManager.Instance.OnSetupStateChanged += StartSpawningEnemies;
    }

    private void OnDestroy()
    {
        // Unsubscribe from RogueLiteManager event
        RogueLiteManager.Instance.OnSetupStateChanged -= StartSpawningEnemies;
    }

    public void StartSpawningEnemies(RoomSetupState newState)
    {
        if (newState != RoomSetupState.PRE_ENEMY_SPAWNING)
            return;

        RogueLiteManager.Instance.SetRoomState(RoomSetupState.ENEMIES_SPAWNED);
        // Get the waveConfig from the RogueLiteManager
        waveConfig = RogueLiteManager.Instance.GetWaveConfig();

        if (waveConfig == null)
        {
            Debug.LogError("No waveConfig provided by RogueLiteManager!");
            return;
        }

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
        if (currentWave >= waveConfig.maxWaves)
        {
            Debug.Log("All waves completed!");
            RogueLiteManager.Instance.SetRoomState(RoomSetupState.ROOM_CLEARED);
            return;
        }

        currentWave++;
        Debug.Log($"Starting Wave {currentWave}");

        // Determine the number of enemies to spawn in this wave
        totalEnemiesInWave = Random.Range(waveConfig.minEnemiesPerWave, waveConfig.maxEnemiesPerWave + 1);
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
        if (spawnPoints.Count == 0 || waveConfig.enemyPrefabs.Length == 0)
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

        GameObject enemyPrefab = waveConfig.enemyPrefabs[Random.Range(0, waveConfig.enemyPrefabs.Length)];

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
}
