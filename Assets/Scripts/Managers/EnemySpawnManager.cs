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
            Debug.Log("[EnemySpawnManager] StartSpawningEnemies called");
            
            if (waveConfig == null)
            {
                Debug.LogError("[EnemySpawnManager] No waveConfig provided by RogueLiteManager!");
                return;
            }

            Debug.Log($"[EnemySpawnManager] WaveConfig received: {waveConfig.name} with {waveConfig.enemyPrefabs.Length} enemy prefabs, {waveConfig.maxWaves} max waves");
            currentWaveConfig = waveConfig;

            Debug.Log($"[EnemySpawnManager] Current game mode: {GameManager.Instance.CurrentGameMode}");
            
            if(GameManager.Instance.CurrentGameMode == GameMode.ROGUE_LITE)
            {
                Debug.Log("[EnemySpawnManager] In ROGUE_LITE mode, checking room type");
                
                // Check if the current room parent is friendly - if so, skip enemy spawning
                if (RogueLiteManager.Instance.BuildingManager.CurrentRoomParentComponent != null)
                {
                    var roomParentComponent = RogueLiteManager.Instance.BuildingManager.CurrentRoomParentComponent;
                    Debug.Log($"[EnemySpawnManager] Room parent component found, room type: {roomParentComponent.RoomType}");
                    
                    if (roomParentComponent != null && roomParentComponent.RoomType == RogueLikeRoomType.FRIENDLY)
                    {
                        Debug.Log("[EnemySpawnManager] Room is FRIENDLY, skipping enemy spawning");
                        RogueLiteManager.Instance.SetEnemySetupState(EnemySetupState.ALL_WAVES_CLEARED);
                        return;
                    }
                    else
                    {
                        Debug.Log("[EnemySpawnManager] Room is HOSTILE, proceeding with enemy spawning");
                    }
                }
                else
                {
                    Debug.LogWarning("[EnemySpawnManager] No room parent component found in ROGUE_LITE mode");
                }
            }

            Debug.Log("[EnemySpawnManager] Starting enemy spawning process...");
            
            // NavMesh readiness is now handled by RogueLiteManager, so we can spawn immediately
            StartNextWave();
        }

        private void StartNextWave()
        {
            Debug.Log($"[EnemySpawnManager] StartNextWave called - currentWave: {currentWave}, maxWaves: {currentWaveConfig.maxWaves}");
            
            if (currentWave >= currentWaveConfig.maxWaves)
            {
                Debug.Log("[EnemySpawnManager] All waves completed");
                switch (GameManager.Instance.CurrentGameMode)
                {
                    case GameMode.ROGUE_LITE:
                        Debug.Log("[EnemySpawnManager] Setting ROGUE_LITE enemy setup state to ALL_WAVES_CLEARED");
                        RogueLiteManager.Instance.SetEnemySetupState(EnemySetupState.ALL_WAVES_CLEARED);
                        break;
                    case GameMode.CAMP_ATTACK:
                        // For camp attack mode, we can handle this directly or delegate to CampManager
                        Debug.LogWarning("[EnemySpawnManager] Camp attack wave completed!");
                        break;
                    case GameMode.CAMP:
                        // For camp, we might want to handle wave completion differently
                        Debug.Log("[EnemySpawnManager] Camp wave completed!");
                        break;
                    default:
                        Debug.LogError("[EnemySpawnManager] Shouldnt be spawning enemies here!!!");
                        break;
                }

                return;
            }

            currentWave++;
            Debug.Log($"[EnemySpawnManager] Starting wave {currentWave} of {currentWaveConfig.maxWaves}");

            // Determine the number of enemies to spawn in this wave
            totalEnemiesInWave = Random.Range(currentWaveConfig.minEnemiesPerWave, currentWaveConfig.maxEnemiesPerWave + 1);
            enemiesSpawned = 0; // Reset for the new wave
            
            Debug.Log($"[EnemySpawnManager] Wave {currentWave} will spawn {totalEnemiesInWave} enemies (range: {currentWaveConfig.minEnemiesPerWave}-{currentWaveConfig.maxEnemiesPerWave})");

            SpawnWave(totalEnemiesInWave);
        }

        private void SpawnWave(int enemyCount)
        {
            Debug.Log($"[EnemySpawnManager] SpawnWave called to spawn {enemyCount} enemies");
            
            for (int i = 0; i < enemyCount; i++)
            {
                Debug.Log($"[EnemySpawnManager] Spawning enemy {i + 1} of {enemyCount}");
                SpawnEnemy();
            }
        }

        private void SpawnEnemy()
        {
            Debug.Log("[EnemySpawnManager] SpawnEnemy called");
            
            if (currentWaveConfig.enemyPrefabs.Length == 0)
            {
                Debug.LogError("[EnemySpawnManager] No enemy prefabs in wave config!");
                return;
            }

            Debug.Log($"[EnemySpawnManager] Enemy prefabs available: {currentWaveConfig.enemyPrefabs.Length}");
            StartCoroutine(SpawnEnemyWithRetry());
        }

        private IEnumerator SpawnEnemyWithRetry()
        {
            Debug.Log("[EnemySpawnManager] SpawnEnemyWithRetry coroutine started");
            
            // Find a valid spawn position using NavigationUtils
            Debug.Log($"[EnemySpawnManager] Calling NavigationUtils.FindRandomSpawnPosition with params: minDistance={minDistanceFromPlayer}, maxAttempts={maxSpawnAttempts}, sampleRadius={spawnSampleRadius}");
            Vector3 spawnPosition = NavigationUtils.FindRandomSpawnPosition(
                minDistanceFromPlayer, 
                maxSpawnAttempts, 
                spawnSampleRadius
            );

            Debug.Log($"[EnemySpawnManager] Initial spawn position result: {spawnPosition}");

            // Wait until we find a valid spawn position
            int retryCount = 0;
            while (spawnPosition == Vector3.zero)
            {
                retryCount++;
                Debug.Log($"[EnemySpawnManager] Spawn position was Vector3.zero, retry #{retryCount}");
                
                yield return new WaitForSeconds(0.1f); // Small delay before trying again
                spawnPosition = NavigationUtils.FindRandomSpawnPosition(
                    minDistanceFromPlayer, 
                    maxSpawnAttempts, 
                    spawnSampleRadius
                );
                
                Debug.Log($"[EnemySpawnManager] Retry #{retryCount} spawn position result: {spawnPosition}");
            }

            Debug.Log($"[EnemySpawnManager] Valid spawn position found: {spawnPosition} after {retryCount} retries");

            GameObject enemyPrefab = currentWaveConfig.enemyPrefabs[Random.Range(0, currentWaveConfig.enemyPrefabs.Length)];
            Debug.Log($"[EnemySpawnManager] Selected enemy prefab: {enemyPrefab.name}");

            // Play spawn effect before spawning the enemy
            Vector3 spawnNormal = Vector3.up;
            Debug.Log($"[EnemySpawnManager] Playing spawn effect at position: {spawnPosition}");
            
            if (EffectManager.Instance != null)
            {
                EffectManager.Instance.PlaySpawnEffect(spawnPosition, spawnNormal);
                Debug.Log("[EnemySpawnManager] Spawn effect played successfully");
            }
            else
            {
                Debug.LogWarning("[EnemySpawnManager] EffectManager.Instance is null, skipping spawn effect");
            }

            // Small delay to let the spawn effect play
            yield return new WaitForSeconds(0.2f);

            // Spawn the enemy at the random position
            Debug.Log($"[EnemySpawnManager] Instantiating enemy at position: {spawnPosition}");
            GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

            if (enemy != null)
            {
                Debug.Log($"[EnemySpawnManager] Enemy spawned successfully: {enemy.name}");
                activeEnemies.Add(enemy);
                enemiesSpawned++; // Track the number of enemies spawned
                Debug.Log($"[EnemySpawnManager] Enemies spawned: {enemiesSpawned}/{totalEnemiesInWave}, Active enemies: {activeEnemies.Count}");

                // Add listener to remove the enemy when it's destroyed
                var enemyBase = enemy.GetComponent<EnemyBase>();
                if (enemyBase != null)
                {
                    Debug.Log("[EnemySpawnManager] Adding death listener to enemy");
                    enemyBase.OnDeath += () =>
                    {
                        Debug.Log($"[EnemySpawnManager] Enemy {enemy.name} died, removing from active list");
                        activeEnemies.Remove(enemy);
                        CheckForWaveCompletion();
                    };
                }
                else
                {
                    Debug.LogError($"[EnemySpawnManager] Enemy {enemy.name} does not have EnemyBase component!");
                }
            }
            else
            {
                Debug.LogError("[EnemySpawnManager] Failed to instantiate enemy!");
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
