using UnityEngine;

namespace Managers
{
    public class DifficultyManager : MonoBehaviour
    {
        // Singleton instance
        private static DifficultyManager _instance;
        public static DifficultyManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<DifficultyManager>();
                    if (_instance == null)
                    {
                        Debug.LogWarning("DifficultyManager instance not found in the scene!");
                    }
                }
                return _instance;
            }
        }

        [Header("Difficulty Settings")]
        [SerializeField] private float difficultyScalingPerRoom = 1.5f;
        [SerializeField] private int maxDifficulty = 100;

        [Header("Resource Rarity Thresholds")]
        [SerializeField] private int commonRarityThreshold = 20;
        [SerializeField] private int rareRarityThreshold = 40;
        [SerializeField] private int epicRarityThreshold = 60;

        private int currentBuildingDifficulty;
        private int currentRoomNumber;
        private int currentRoomDifficulty;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
            }
        }

        /// <summary>
        /// Initialize difficulty for a new building
        /// </summary>
        /// <param name="buildingType">Type of building being entered</param>
        /// <param name="buildingBaseDifficulty">Base difficulty of the building (from BuildingDataScriptableObj)</param>
        public void InitializeBuildingDifficulty(RogueLikeBuildingType buildingType, int buildingBaseDifficulty)
        {
            currentBuildingDifficulty = buildingBaseDifficulty;
            currentRoomNumber = 0;
            currentRoomDifficulty = 0;
            
            Debug.Log($"<color=cyan>DifficultyManager: Initialized building {buildingType} with base difficulty {buildingBaseDifficulty}</color>");
        }

        /// <summary>
        /// Calculate room difficulty based on building difficulty and room number
        /// </summary>
        /// <param name="roomNumber">The room number (1-based)</param>
        /// <returns>Calculated room difficulty</returns>
        public int CalculateRoomDifficulty(int roomNumber)
        {
            int roomDifficulty = Mathf.RoundToInt(currentBuildingDifficulty * 0.5f + (roomNumber * difficultyScalingPerRoom));
            return Mathf.Clamp(roomDifficulty, 1, maxDifficulty);
        }

        /// <summary>
        /// Set the difficulty for entering the next room (automatically calculates room difficulty)
        /// </summary>
        public void SetNextRoomDifficulty()
        {
            currentRoomNumber++;
            currentRoomDifficulty = CalculateRoomDifficulty(currentRoomNumber);
            
            Debug.Log($"<color=cyan>DifficultyManager: Room {currentRoomNumber} difficulty calculated as {currentRoomDifficulty}</color>");
        }

        /// <summary>
        /// Set the difficulty for entering a specific room (for backward compatibility)
        /// </summary>
        /// <param name="roomDifficulty">The room's specific difficulty modifier</param>
        public void SetRoomDifficulty(int roomDifficulty)
        {
            currentRoomDifficulty = roomDifficulty;
            currentRoomNumber++;
            
            Debug.Log($"<color=cyan>DifficultyManager: Room {currentRoomNumber} difficulty set to {roomDifficulty}</color>");
        }

        /// <summary>
        /// Get the current wave difficulty for enemy spawning
        /// </summary>
        /// <returns>Calculated difficulty for enemy waves</returns>
        public int GetCurrentWaveDifficulty()
        {
            int waveDifficulty = currentBuildingDifficulty + 
                               Mathf.RoundToInt(currentRoomNumber * difficultyScalingPerRoom) + 
                               currentRoomDifficulty;
            
            return Mathf.Clamp(waveDifficulty, 1, maxDifficulty);
        }

        /// <summary>
        /// Get the current room number (1-based)
        /// </summary>
        public int GetCurrentRoomNumber()
        {
            return currentRoomNumber;
        }

        /// <summary>
        /// Get the current building difficulty
        /// </summary>
        public int GetCurrentBuildingDifficulty()
        {
            return currentBuildingDifficulty;
        }

        /// <summary>
        /// Get the current room difficulty modifier
        /// </summary>
        public int GetCurrentRoomDifficulty()
        {
            return currentRoomDifficulty;
        }

        /// <summary>
        /// Get resource rarity based on current difficulty
        /// </summary>
        /// <param name="difficulty">Optional difficulty override, uses current wave difficulty if null</param>
        /// <returns>Resource rarity based on difficulty thresholds</returns>
        public ResourceRarity GetResourceRarity(int? difficulty = null)
        {
            int currentDifficulty = difficulty ?? GetCurrentWaveDifficulty();
            
            if (currentDifficulty < commonRarityThreshold)
                return ResourceRarity.COMMON;
            if (currentDifficulty < rareRarityThreshold)
                return ResourceRarity.RARE;
            if (currentDifficulty < epicRarityThreshold)
                return ResourceRarity.EPIC;

            return ResourceRarity.LEGENDARY;
        }

        /// <summary>
        /// Reset difficulty when leaving a building
        /// </summary>
        public void ResetDifficulty()
        {
            currentBuildingDifficulty = 0;
            currentRoomNumber = 0;
            currentRoomDifficulty = 0;
            
            Debug.Log("<color=cyan>DifficultyManager: Difficulty reset</color>");
        }

        /// <summary>
        /// Get difficulty info for debugging
        /// </summary>
        public string GetDifficultyInfo()
        {
            return $"Building: {currentBuildingDifficulty}, Room: {currentRoomNumber}, Room Mod: {currentRoomDifficulty}, Wave: {GetCurrentWaveDifficulty()}";
        }
    }
} 