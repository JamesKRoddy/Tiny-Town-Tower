using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace Managers
{
    public class RogueLiteManager : GameModeManager<RogueLikeEnemyWaveConfig>
    {
        [Header("RogueLite Manager References")]
        private RoomManager roomManager;
        private BuildingManager buildingManager;
        
        private int currentEnemyCount;

        public RoomManager RoomManager => roomManager;
        public BuildingManager BuildingManager => buildingManager;

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
                InitializeManagers();
            }
        }

        private void InitializeManagers()
        {
            // Find and cache references to other managers
            if (roomManager == null) roomManager = gameObject.GetComponentInChildren<RoomManager>();
            if (buildingManager == null) buildingManager = gameObject.GetComponentInChildren<BuildingManager>();
            // Log warnings for any missing managers
            if (roomManager == null) Debug.LogWarning("RoomManager not found in scene!");
            if (buildingManager == null) Debug.LogWarning("BuildingManager not found in scene!");
            // Initialize managers
        }

        protected override void Start()
        {
            base.Start();
            SetEnemySetupState(EnemySetupState.ALL_WAVES_CLEARED);
        }

        protected override void EnemySetupStateChanged(EnemySetupState newState)
        {
            switch (newState)
            {
                case EnemySetupState.NONE:
                    break;
                case EnemySetupState.WAVE_START:
                    EnemySpawnManager.Instance.ResetWaveCount();
                    break;
                case EnemySetupState.PRE_ENEMY_SPAWNING:
                    SetupPlayer();                    
                    break;
                case EnemySetupState.ENEMY_SPAWN_START:
                    EnemySpawnManager.Instance.StartSpawningEnemies(GetWaveConfig(GetCurrentWaveDifficulty()));
                    break;
                case EnemySetupState.ENEMIES_SPAWNED:
                    break;
                case EnemySetupState.ALL_WAVES_CLEARED:
                    break;
                default:
                    break;
            }
        }

        private void SetupPlayer()
        {
            if (PlayerController.Instance != null && PlayerController.Instance._possessedNPC != null)
            {
                buildingManager.SetupPlayer(PlayerController.Instance._possessedNPC.GetTransform());
            }
        }

        public void EnterRoomWithTransition(RogueLiteDoor door)
        {
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }
            transitionCoroutine = StartCoroutine(EnterRoomSequence(door));
        }

        private Coroutine transitionCoroutine;

        private IEnumerator EnterRoomSequence(RogueLiteDoor door)
        {
            // 1. Fade in
            yield return PlayerUIManager.Instance.transitionMenu.FadeIn();
            
            // 2. Setup room
            SetEnemySetupState(EnemySetupState.WAVE_START);
            buildingManager.EnterRoom(door);

            // 3. Move Player
            SetEnemySetupState(EnemySetupState.PRE_ENEMY_SPAWNING);
                        
            // 4. Short pause for camera transition
            yield return new WaitForSeconds(0.5f);

            // 5. Fade out
            yield return PlayerUIManager.Instance.transitionMenu.FadeOut();

            // Wait before enemies are spawned
            yield return new WaitForSeconds(1.0f);
            
            // 6. Spawn enemies
            SetEnemySetupState(EnemySetupState.ENEMY_SPAWN_START);

            // 7. Finish spawning enemies
            SetEnemySetupState(EnemySetupState.ENEMIES_SPAWNED);

            transitionCoroutine = null;
        }

        public override int GetCurrentWaveDifficulty()
        {
            return buildingManager.GetCurrentWaveDifficulty();
        }

        public int GetCurrentEnemyCount()
        {
            return currentEnemyCount;
        }

        public void OnEnemySpawned()
        {
            currentEnemyCount++;
        }

        public void OnEnemyDefeated()
        {
            currentEnemyCount--;
            if (currentEnemyCount <= 0)
            {
                SetEnemySetupState(EnemySetupState.ALL_WAVES_CLEARED);
            }
        }
    }
}
