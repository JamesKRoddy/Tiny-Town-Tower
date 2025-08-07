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

        [Header("Spawn Settings")]
        [Tooltip("Minimum distance from player's possessed NPC for enemy spawning")]
        public float minDistanceFromPlayer = 10f;
        [Tooltip("Minimum distance between enemy spawn positions")]
        public float minDistanceBetweenSpawns = 3f;
        [Tooltip("Maximum attempts to find a valid spawn position per enemy")]
        public int maxSpawnAttempts = 30;
        [Tooltip("Radius to sample around random points for NavMesh validation")]
        public float spawnSampleRadius = 5f;

        private EnemyWaveConfig currentWaveConfig; // Current wave configuration //TODO use GetCurrentRoomDifficulty

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

            if(GameManager.Instance.CurrentGameMode == GameMode.ROGUE_LITE)
            {
                // Check if the current room parent is friendly - if so, skip enemy spawning
                if (RogueLiteManager.Instance.BuildingManager.CurrentRoomParentComponent != null)
                {
                    var roomParentComponent = RogueLiteManager.Instance.BuildingManager.CurrentRoomParentComponent;
                    
                    if (roomParentComponent != null && roomParentComponent.RoomType == RogueLikeRoomType.FRIENDLY)
                    {
                        RogueLiteManager.Instance.SetEnemySetupState(EnemySetupState.ALL_WAVES_CLEARED);
                        return;
                    }
                }
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
            if (currentWaveConfig.enemyPrefabs.Length == 0)
                return;

            StartCoroutine(SpawnEnemyWithRetry());
        }

        private IEnumerator SpawnEnemyWithRetry()
        {
            // Find a valid spawn position using NavigationUtils
            Vector3 spawnPosition = NavigationUtils.FindRandomSpawnPosition(
                minDistanceFromPlayer, 
                maxSpawnAttempts, 
                spawnSampleRadius
            );

            // Wait until we find a valid spawn position
            while (spawnPosition == Vector3.zero)
            {
                yield return new WaitForSeconds(0.1f); // Small delay before trying again
                spawnPosition = NavigationUtils.FindRandomSpawnPosition(
                    minDistanceFromPlayer, 
                    maxSpawnAttempts, 
                    spawnSampleRadius
                );
            }

            GameObject enemyPrefab = currentWaveConfig.enemyPrefabs[Random.Range(0, currentWaveConfig.enemyPrefabs.Length)];

            // Play spawn effect before spawning the enemy
            Vector3 spawnNormal = Vector3.up;
            EffectManager.Instance.PlaySpawnEffect(spawnPosition, spawnNormal);

            // Small delay to let the spawn effect play
            yield return new WaitForSeconds(0.2f);

            // Spawn the enemy at the random position
            GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

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
    }
}
