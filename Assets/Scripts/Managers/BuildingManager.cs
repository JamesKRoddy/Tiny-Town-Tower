using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    public class BuildingManager : MonoBehaviour
    {
        [Header("Building Settings")]
        [SerializeField] private List<BuildingDataScriptableObj> buildingDataScriptableObjs;
        [SerializeField] private RoomManager roomManager;
        
        private BuildingType currentBuilding = BuildingType.NONE;
        private GameObject currentBuildingParent;
        private int buildingDifficulty;
        private int currentRoom;
        private int currentRoomDifficulty;
        private Vector3 lastPlayerSpawnPoint;

        public BuildingType CurrentBuilding => currentBuilding;
        public int BuildingDifficulty => buildingDifficulty;
        public int CurrentRoom => currentRoom;
        public int CurrentRoomDifficulty => currentRoomDifficulty;

        public void InitializeBuilding(BuildingType buildingType, int difficulty)
        {
            currentBuilding = buildingType;
            buildingDifficulty = difficulty;
            currentRoom = 0;
            currentRoomDifficulty = 0;
        }

        public void EnterRoom(RogueLiteDoor rogueLiteDoor)
        {
            currentRoomDifficulty = rogueLiteDoor.doorRoomDifficulty;
            currentRoom++;

            if (currentBuilding == BuildingType.NONE)
            {
                currentBuilding = rogueLiteDoor.buildingType;
            }

            SetupLevel(currentBuilding);
        }

        private void SetupLevel(BuildingType buildingType)
        {
            // Store the current player spawn point before destroying the old building
            if (currentBuildingParent != null)
            {
                RoomSectionRandomizer oldRandomizer = currentBuildingParent.GetComponent<RoomSectionRandomizer>();
                if (oldRandomizer != null)
                {
                    lastPlayerSpawnPoint = oldRandomizer.GetPlayerSpawnPoint();
                }
            }

            // Create the new building
            int difficulty = GetCurrentWaveDifficulty();
            GameObject newBuildingParent = Instantiate(GetBuildingParent(buildingType, difficulty, out BuildingDataScriptableObj selectedBuilding));

            if (newBuildingParent != null && selectedBuilding != null)
            {
                // Set up the new building before destroying the old one
                RoomSectionRandomizer randomizer = newBuildingParent.GetComponent<RoomSectionRandomizer>();
                if (randomizer != null)
                {
                    randomizer.GenerateRandomRooms(selectedBuilding);
                }

                // Now safe to destroy the old building
                if (currentBuildingParent != null)
                {
                    Destroy(currentBuildingParent);
                }

                currentBuildingParent = newBuildingParent;
            }
            else
            {
                Debug.LogError($"No building parent found for {buildingType} at difficulty {difficulty}.");
            }
        }

        public GameObject GetBuildingParent(BuildingType buildingType, int difficulty, out BuildingDataScriptableObj selectedBuilding)
        {
            foreach (var buildingData in buildingDataScriptableObjs)
            {
                if (buildingData is BuildingDataScriptableObj building && building.buildingType == buildingType)
                {
                    selectedBuilding = building;
                    return building.GetBuildingParent(difficulty);
                }
            }
            Debug.LogWarning($"No building parent found for type {buildingType} with difficulty {difficulty}.");
            selectedBuilding = null;
            return null;
        }

        public int GetCurrentWaveDifficulty()
        {
            return (currentRoom * buildingDifficulty) + currentRoomDifficulty;
        }

        public void SetupPlayer(Transform playerTransform)
        {
            if (playerTransform == null) return;

            if (currentBuildingParent != null)
            {
                RoomSectionRandomizer randomizer = currentBuildingParent.GetComponent<RoomSectionRandomizer>();
                if (randomizer != null)
                {
                    Vector3 spawnPoint = randomizer.GetPlayerSpawnPoint();
                    if (spawnPoint != Vector3.zero)
                    {
                        playerTransform.position = spawnPoint;
                        Debug.Log($"Player spawned at {spawnPoint}");
                        return;
                    }
                }
            }

            // Fallback to last known spawn point if current building setup fails
            if (lastPlayerSpawnPoint != Vector3.zero)
            {
                playerTransform.position = lastPlayerSpawnPoint;
            }
        }
    }
} 