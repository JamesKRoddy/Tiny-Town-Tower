using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Enemies;

namespace Managers
{
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

        private void Start()
        {
            spawnPoints = new List<EnemySpawnPoint>(FindObjectsByType<EnemySpawnPoint>(FindObjectsSortMode.None));
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

            if(GameManager.Instance.CurrentGameMode == GameMode.ROGUE_LITE)
            {
                spawnPoints = new List<EnemySpawnPoint>(RogueLiteManager.Instance.BuildingManager.CurrentRoomParent.GetComponent<RogueLiteRoomParent>().GetEnemySpawnPoints());

            } else if(GameManager.Instance.CurrentGameMode == GameMode.CAMP_ATTACK)
            {
                // For camp attack mode, we can handle this directly or delegate to CampManager
                Debug.LogWarning("No spawn points found for camp attack mode!");
            } else if(GameManager.Instance.CurrentGameMode == GameMode.CAMP)
            {
                // Get all spawn points in the scene for camp
                spawnPoints = new List<EnemySpawnPoint>(FindObjectsByType<EnemySpawnPoint>(FindObjectsSortMode.None));
            }

            
            if (spawnPoints.Count == 0)
            {
                Debug.LogError("No spawn points found!");
                switch (GameManager.Instance.CurrentGameMode)
                {
                    case GameMode.ROGUE_LITE:
                        RogueLiteManager.Instance.SetEnemySetupState(EnemySetupState.ALL_WAVES_CLEARED);
                        break;
                    case GameMode.CAMP_ATTACK:
                        // For camp attack mode, we can handle this directly or delegate to CampManager
                        Debug.LogWarning("No spawn points found for camp attack mode!");
                        break;
                    case GameMode.CAMP:
                        // For camp, we might want to handle this differently
                        Debug.LogWarning("No spawn points found for camp enemies!");
                        break;
                    default:
                        Debug.LogError("Shouldnt be spawning enemies here!!!");
                        break;
                }
                return;
            }

            StartNextWave();
        }

        private void StartNextWave()
        {
            if (currentWave >= currentWaveConfig.maxWaves)
            {
                switch (GameManager.Instance.CurrentGameMode)
                {
                    case GameMode.ROGUE_LITE:
                        RogueLiteManager.Instance.SetEnemySetupState(EnemySetupState.ALL_WAVES_CLEARED);
                        break;
                    case GameMode.CAMP_ATTACK:
                        // For camp attack mode, we can handle this directly or delegate to CampManager
                        Debug.LogWarning("No spawn points found for camp attack mode!");
                        break;
                    case GameMode.CAMP:
                        // For camp, we might want to handle wave completion differently
                        Debug.Log("Camp wave completed!");
                        break;
                    default:
                        Debug.LogError("Shouldnt be spawning enemies here!!!");
                        break;
                }

                return;
            }

            currentWave++;

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
                enemy.GetComponent<EnemyBase>().OnDeath += () =>
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
                // For camp mode, we don't automatically start the next wave
                // The wave will end when all enemies are killed or time expires
                if (GameManager.Instance.CurrentGameMode == GameMode.CAMP)
                {
                    Debug.Log("All enemies defeated in camp wave!");
                    // The CampManager will handle wave completion through its timer
                }
                else
                {
                    StartNextWave();
                }
            }
        }

        /// <summary>
        /// Used by the PlacementManager to make sure a turret is not blocking the enemy path
        /// </summary>
        public Vector3? SpawnPointPosition()
        {
            if (spawnPoints.Count != 0)
            {
                return spawnPoints[0].transform.position;
            }
            else
            {
                spawnPoints = new List<EnemySpawnPoint>(FindObjectsByType<EnemySpawnPoint>(FindObjectsSortMode.None));
                return spawnPoints[0].transform.position;
            }
        }
    }
}
