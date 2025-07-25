using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace Managers
{
    public class RogueLiteManager : GameModeManager<RogueLikeEnemyWaveConfig>
    {
        [Header("RogueLite Manager References")]
        [SerializeField] private RogueLikeBuildingManager buildingManager;
        [SerializeField] private OverworldManager overworldManager;
        
        private int currentEnemyCount;

        public RogueLikeBuildingManager BuildingManager => buildingManager;
        public OverworldManager OverworldManager => overworldManager;

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
            // Log warnings for any missing managers
            if (buildingManager == null) Debug.LogWarning("BuildingManager not found in scene!");

            // Subscribe to the NPC possessed event
            PlayerController.Instance.OnNPCPossessed += OnNPCPossessed;
            
            // Subscribe to game mode changes
            GameManager.Instance.OnGameModeChanged += OnGameModeChanged;
        }

        protected override void Start()
        {
            base.Start();
            SetEnemySetupState(EnemySetupState.ALL_WAVES_CLEARED);
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
                    if(transitionCoroutine != null){
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

        public void EnterRoomWithTransition(RogueLikeRoomDoor door)
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
                var npcInventory = PlayerController.Instance.GetCharacterInventory().GetFullInventory();
                PlayerInventory.Instance.AddItem(npcInventory);
                
                // Show popups for items transferred from NPC to player inventory
                if (PlayerUIManager.Instance?.inventoryPopup != null)
                {
                    foreach (var item in npcInventory)
                    {
                        PlayerUIManager.Instance.inventoryPopup.ShowInventoryPopup(
                            item.resourceScriptableObj, 
                            item.count, 
                            true // This is now going to player inventory
                        );
                    }
                }
            }
            SceneTransitionManager.Instance.LoadScene(SceneNames.CampScene, GameMode.CAMP, false);
        }

        public void ReturnToPreviousRoom(RogueLikeRoomDoor door)
        {
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }
            transitionCoroutine = StartCoroutine(ReturnToPreviousRoomSequence(door));
        }

        private IEnumerator ReturnToPreviousRoomSequence(RogueLikeRoomDoor door)
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

        private IEnumerator EnterRoomSequence(RogueLikeRoomDoor door)
        {
            // 1. Fade in
            yield return PlayerUIManager.Instance.transitionMenu.FadeIn();
            
            // 2. Setup room
            SetEnemySetupState(EnemySetupState.WAVE_START);
            bool roomEntered = buildingManager.EnterRoomCheck(door);

            //Reached the end of the building
            if(!roomEntered){
                overworldManager.ExitedBuilding();
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

        private void OnNPCPossessed(IPossessable oldNPC, IPossessable newNPC)
        {
            // Always unsubscribe from old NPC's death events if it was damageable
            if (oldNPC is IDamageable oldDamageable)
            {
                oldDamageable.OnDeath -= PlayerDied;
            }

            // Only subscribe to new NPC's death events in roguelike mode
            if (GameManager.Instance.CurrentGameMode == GameMode.ROGUE_LITE && newNPC is IDamageable newDamageable)
            {
                newDamageable.OnDeath += PlayerDied;
            }
        }

        private void OnGameModeChanged(GameMode newGameMode)
        {
            // If entering ROGUE_LITE mode and there's a possessed NPC, subscribe to its death events
            if (newGameMode == GameMode.ROGUE_LITE && PlayerController.Instance._possessedNPC is IDamageable damageable)
            {
                damageable.OnDeath += PlayerDied;
            }
            // If leaving ROGUE_LITE mode and there's a possessed NPC, unsubscribe from its death events
            else if (newGameMode != GameMode.ROGUE_LITE && PlayerController.Instance._possessedNPC is IDamageable damageableOld)
            {
                damageableOld.OnDeath -= PlayerDied;
            }
        }

        private void PlayerDied()
        {
            // Only handle player death in roguelike mode
            if (GameManager.Instance.CurrentGameMode != GameMode.ROGUE_LITE)
            {
                return;
            }
            
            SetEnemySetupState(EnemySetupState.ALL_WAVES_CLEARED);
            PlayerUIManager.Instance.deathMenu.SetScreenActive(true, 0.1f);       
        }

        public override int GetCurrentWaveDifficulty()
        {
            return DifficultyManager.Instance.GetCurrentWaveDifficulty();
        }
    

        protected override void OnDestroy()
        {
            base.OnDestroy();
            // Unsubscribe from the NPC possessed event
            if (PlayerController.Instance != null) PlayerController.Instance.OnNPCPossessed -= OnNPCPossessed;
            
            // Unsubscribe from game mode changes
            if (GameManager.Instance != null) GameManager.Instance.OnGameModeChanged -= OnGameModeChanged;
        }
    }
}
