using UnityEngine;
using System;

namespace Managers
{
    public class GameManager : MonoBehaviour
    {
        // Singleton instance
        private static GameManager _instance;

        // Singleton property to get the instance
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Find the GameManager instance if it hasn't been assigned
                    _instance = FindFirstObjectByType<GameManager>();
                    if (_instance == null)
                    {
                        Debug.LogWarning("GameManager instance not found in the scene!");
                    }
                }
                return _instance;
            }
        }

        [SerializeField] private ResourceManager _resourceManager;
        public ResourceManager ResourceManager => _resourceManager;

        // Public event to notify when the game mode changes
        public event Action<GameMode> OnGameModeChanged;

        // Private backing field for the current game mode
        [SerializeField] private GameMode _currentGameMode = GameMode.NONE;

        // Property to get and set the current game mode
        public GameMode CurrentGameMode
        {
            get { return _currentGameMode; }
            set
            {
                // Only trigger event if the mode changes
                if (_currentGameMode != value)
                {
                    _currentGameMode = value;
                    // Invoke the event when game mode changes
                    OnGameModeChanged?.Invoke(_currentGameMode);
                }
            }
        }

        public PlayerControlType PlayerGameControlType() //Used as the default gamemode for returning to from menus and conversations
        {
            switch (_currentGameMode)
            {
                case GameMode.NONE:
                    return PlayerControlType.NONE;
                case GameMode.ROGUE_LITE:
                    return PlayerControlType.COMBAT_NPC_MOVEMENT;
                case GameMode.CAMP:
                    if (PlayerController.Instance._possessedNPC != null)
                    {
                        return PlayerController.Instance._possessedNPC switch
                        {
                            SettlerNPC => PlayerControlType.CAMP_NPC_MOVEMENT,
                            RobotCharacterController robot => robot.IsWorking() 
                                ? PlayerControlType.ROBOT_WORKING 
                                : PlayerControlType.ROBOT_MOVEMENT,
                            _ => PlayerControlType.CAMP_CAMERA_MOVEMENT
                        };
                    }
                    return PlayerControlType.CAMP_CAMERA_MOVEMENT;
                case GameMode.CAMP_ATTACK:
                    return PlayerControlType.CAMP_ATTACK_CAMERA_MOVEMENT;
                default:
                    return PlayerControlType.NONE;
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

        // Start is called once before the first execution of Update
        void Start()
        {
            OnGameModeChanged?.Invoke(_currentGameMode);
        }

        public Vector3 GetPlayerSpawnPoint()
        {
            SceneNames sceneName = SceneTransitionManager.Instance.NextScene;

            switch (sceneName)
            {
                case SceneNames.OverworldScene:
                    return RogueLiteManager.Instance.OverworldManager.GetOverWorldSpawnPoint();
                case SceneNames.RogueLikeScene:
                    return RogueLiteManager.Instance.BuildingManager.RogueLikeBuildingSpawn.position;
                default:
                    return Vector3.zero;
            }
        }
    }
}
