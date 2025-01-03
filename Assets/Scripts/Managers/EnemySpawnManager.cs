using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnManager : MonoBehaviour
{
    public EnemyWaveConfig waveConfig;

    private List<EnemySpawnPoint> spawnPoints;
    private int currentWave = 0;
    private List<GameObject> activeEnemies = new List<GameObject>();

    void Start()
    {
        // Get the waveConfig from the RogueLiteManager
        waveConfig = RogueLiteManager.Instance.GetWaveConfig();

        if (waveConfig == null)
        {
            Debug.LogError("No waveConfig provided by RogueLiteManager!");
            return;
        }

        // Get all spawn points in the scene
        spawnPoints = new List<EnemySpawnPoint>(FindObjectsOfType<EnemySpawnPoint>());
        if (spawnPoints.Count == 0)
        {
            Debug.LogError("No spawn points found!");
            return;
        }

        StartNextWave();
    }

    void StartNextWave()
    {
        if (currentWave >= waveConfig.maxWaves)
        {
            Debug.Log("All waves completed!");
            return;
        }

        currentWave++;
        Debug.Log($"Starting Wave {currentWave}");
        int enemiesToSpawn = Random.Range(waveConfig.minEnemiesPerWave, waveConfig.maxEnemiesPerWave + 1);

        SpawnWave(enemiesToSpawn);
    }

    void SpawnWave(int enemyCount)
    {
        for (int i = 0; i < enemyCount; i++)
        {
            SpawnEnemy();
        }
    }

    void SpawnEnemy()
    {
        if (spawnPoints.Count == 0 || waveConfig.enemyPrefabs.Length == 0)
            return;

        // Random spawn point and enemy
        EnemySpawnPoint spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
        GameObject enemyPrefab = waveConfig.enemyPrefabs[Random.Range(0, waveConfig.enemyPrefabs.Length)];

        // Delegate spawning to the spawn point
        GameObject enemy = spawnPoint.SpawnEnemy(enemyPrefab);

        if (enemy != null)
        {
            activeEnemies.Add(enemy);

            // Add listener to remove the enemy when it's destroyed
            enemy.GetComponent<EnemyBase>().OnEnemyKilled += () =>
            {
                activeEnemies.Remove(enemy);
                CheckForWaveCompletion();
            };
        }
    }

    void CheckForWaveCompletion()
    {
        if (activeEnemies.Count == 0)
        {
            Debug.Log("Wave Complete!");
            StartNextWave();
        }
    }
}
