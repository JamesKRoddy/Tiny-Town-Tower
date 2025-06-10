using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace Managers
{
    public class RogueLiteManager : GameModeManager<RogueLikeEnemyWaveConfig>
    {
        [Header("RogueLite Manager References")]
        private BuildingManager buildingManager;
        
        private int currentEnemyCount;

        public BuildingManager BuildingManager => buildingManager;

        private Vector3 currentBuildingSpawnPoint;

        private EnemySetupState currentEnemySetupState;
        public bool IsWaveActive => currentEnemySetupState != EnemySetupState.ALL_WAVES_CLEARED;

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
            if (buildingManager == null) buildingManager = gameObject.GetComponentInChildren<BuildingManager>();
            // Log warnings for any missing managers
            if (buildingManager == null) Debug.LogWarning("BuildingManager not found in scene!");

            // Subscribe to the NPC possessed event
            PlayerController.Instance.OnNPCPossessed += OnNPCPossessed;
        }

        protected override void Start()
        {
            base.Start();
            SetEnemySetupState(EnemySetupState.ALL_WAVES_CLEARED);
        }

        //Call when entering a building
        public void EnteredBuilding(Vector3 spawnPoint){
            currentBuildingSpawnPoint = spawnPoint;
        }

        //Call when exiting a building
        public void ExitedBuilding(){
            SceneTransitionManager.Instance.LoadScene("OverworldScene", GameMode.ROGUE_LITE, true, OnSceneLoaded);
        }

        void OnSceneLoaded()
        {
            if (currentBuildingSpawnPoint != Vector3.zero)
            {
                PlayerController.Instance.UpdateNPCPosition(currentBuildingSpawnPoint);
                currentBuildingSpawnPoint = Vector3.zero;
            }
        }

        protected override void EnemySetupStateChanged(EnemySetupState newState)
        {
            Debug.Log($"<color=magenta>EnemySetupStateChanged: {newState}</color>");
            currentEnemySetupState = newState;

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
                    Debug.Log("ALL_WAVES_CLEARED");
                    if(transitionCoroutine != null){
                        Debug.Log("Stopping transition coroutine");
                        StopCoroutine(transitionCoroutine);
                        transitionCoroutine = null;
                    }
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

        public void ReturnToCamp(bool keepInventory)
        {
            if(keepInventory)
            {
                PlayerInventory.Instance.AddItem(PlayerController.Instance.GetCharacterInventory().GetFullInventory());
            }
            SceneTransitionManager.Instance.LoadScene("CampScene", GameMode.CAMP, false);
        }

        public void ReturnToPreviousRoom(RogueLiteDoor door)
        {
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }
            transitionCoroutine = StartCoroutine(ReturnToPreviousRoomSequence(door));
        }

        private IEnumerator ReturnToPreviousRoomSequence(RogueLiteDoor door)
        {
            // 1. Fade in
            yield return PlayerUIManager.Instance.transitionMenu.FadeIn();
            
            // 2. Setup room
            buildingManager.ReturnToPreviousRoom(door);

            // 3. Move Player
            SetEnemySetupState(EnemySetupState.PRE_ENEMY_SPAWNING);
                    
            // 4. Short pause for camera transition
            yield return new WaitForSeconds(0.5f);

            // 5. Fade out
            yield return PlayerUIManager.Instance.transitionMenu.FadeOut();

            //6. Dont spawn enemies
            SetEnemySetupState(EnemySetupState.ALL_WAVES_CLEARED);

            transitionCoroutine = null;
        }

        private Coroutine transitionCoroutine;

        private IEnumerator EnterRoomSequence(RogueLiteDoor door)
        {
            // 1. Fade in
            yield return PlayerUIManager.Instance.transitionMenu.FadeIn();
            
            // 2. Setup room
            SetEnemySetupState(EnemySetupState.WAVE_START);
            bool roomEntered = buildingManager.EnterRoomCheck(door);

            //Reached the end of the building
            if(!roomEntered){
                ExitedBuilding();
                SetEnemySetupState(EnemySetupState.ALL_WAVES_CLEARED);
                yield break;
            }

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
                        
            // If we're in ALL_WAVES_CLEARED state, theres no spawn points
            if (currentEnemySetupState == EnemySetupState.ALL_WAVES_CLEARED)
            {
                transitionCoroutine = null;
                yield break;
            }

            // 7. Finish spawning enemies
            SetEnemySetupState(EnemySetupState.ENEMIES_SPAWNED);

            transitionCoroutine = null;
        }

        private void OnNPCPossessed(IPossessable npc)
        {
            // Unsubscribe from previous NPC's health events if it was damageable
            if (PlayerController.Instance._possessedNPC is IDamageable previousDamageable)
            {
                previousDamageable.OnDeath -= PlayerDied;
            }

            // Subscribe to new NPC's health events
            if (npc is IDamageable damageable)
            {
                damageable.OnDeath += PlayerDied;
            }
        }

        private void PlayerDied()
        {
            SetEnemySetupState(EnemySetupState.ALL_WAVES_CLEARED);

            PlayerUIManager.Instance.deathMenu.SetScreenActive(true, 0.1f);       
        }

        public override int GetCurrentWaveDifficulty()
        {
            return buildingManager.GetCurrentWaveDifficulty();
        }
    

        protected override void OnDestroy()
        {
            base.OnDestroy();
            // Unsubscribe from the NPC possessed event
            if (PlayerController.Instance != null) PlayerController.Instance.OnNPCPossessed -= OnNPCPossessed;
        }
    }
}
