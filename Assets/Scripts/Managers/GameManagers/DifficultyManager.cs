using UnityEngine;

namespace Managers
{
    public class DifficultyManager : MonoBehaviour
    {
        [Header("Difficulty Settings")]
        [SerializeField] private float difficultyScalingPerRoom = 1.5f;
        [SerializeField] private int maxDifficulty = 100;

        [Header("Resource Rarity Thresholds")]
        [SerializeField] private int commonRarityThreshold = 20;
        [SerializeField] private int rareRarityThreshold = 40;
        [SerializeField] private int epicRarityThreshold = 60;

        private int currentRogueLikeBuildingDifficulty;
        private int currentRoomNumber;
        private int currentRoomDifficulty;

        /// <summary>
        /// Initialize difficulty for a new building
        /// </summary>
        /// <param name="rogueLikeBuildingType">Type of building being entered</param>
        /// <param name="rogueLikeBuildingBaseDifficulty">Base difficulty of the building (from BuildingDataScriptableObj)</param>
        public void InitializeBuildingDifficulty(RogueLikeBuildingType rogueLikeBuildingType, int rogueLikeBuildingBaseDifficulty)
        {
            currentRogueLikeBuildingDifficulty = rogueLikeBuildingBaseDifficulty;
            currentRoomNumber = 0;
            currentRoomDifficulty = 0;
            
            Debug.Log($"<color=cyan>DifficultyManager: Initialized building {rogueLikeBuildingType} with base difficulty {rogueLikeBuildingBaseDifficulty}</color>");
        }

        /// <summary>
        /// Calculate room difficulty based on building difficulty and room number
        /// </summary>
        /// <param name="roomNumber">The room number (1-based)</param>
        /// <returns>Calculated room difficulty</returns>
        public int CalculateRoomDifficulty(int roomNumber)
        {
            int roomDifficulty = Mathf.RoundToInt(currentRogueLikeBuildingDifficulty * 0.5f + (roomNumber * difficultyScalingPerRoom));
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
        /// Get the current wave difficulty for enemy spawning
        /// </summary>
        /// <returns>Calculated difficulty for enemy waves</returns>
        public int GetCurrentWaveDifficulty()
        {
            int waveDifficulty = currentRogueLikeBuildingDifficulty + 
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
        public int GetCurrentRogueLikeBuildingDifficulty()
        {
            return currentRogueLikeBuildingDifficulty;
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
        public ItemRarity GetResourceRarity(int? difficulty = null)
        {
            int currentDifficulty = difficulty ?? GetCurrentWaveDifficulty();
            
            if (currentDifficulty < commonRarityThreshold)
                return ItemRarity.COMMON;
            if (currentDifficulty < rareRarityThreshold)
                return ItemRarity.RARE;
            if (currentDifficulty < epicRarityThreshold)
                return ItemRarity.EPIC;

            return ItemRarity.LEGENDARY;
        }

        /// <summary>
        /// Reset difficulty when leaving a building
        /// </summary>
        public void ResetDifficulty()
        {
            currentRogueLikeBuildingDifficulty = 0;
            currentRoomNumber = 0;
            currentRoomDifficulty = 0;
            
            Debug.Log("<color=cyan>DifficultyManager: Difficulty reset</color>");
        }

        /// <summary>
        /// Get difficulty info for debugging
        /// </summary>
        public string GetDifficultyInfo()
        {
            return $"Building: {currentRogueLikeBuildingDifficulty}, Room: {currentRoomNumber}, Room Mod: {currentRoomDifficulty}, Wave: {GetCurrentWaveDifficulty()}";
        }
    }
} 