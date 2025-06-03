using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    public class BuildingManager : MonoBehaviour
    {
        [Header("Building Settings")]
        [SerializeField] private List<BuildingDataScriptableObj> buildingDataScriptableObjs;
        
        private BuildingType currentBuildingType = BuildingType.NONE;
        private GameObject currentBuildingParent;
        private int buildingDifficulty;
        private int currentRoom;
        private int currentRoomDifficulty;
        private Vector3 lastPlayerSpawnPoint;
        private List<GameObject> spawnedBuildings = new List<GameObject>(); // Track all spawned buildings

        public BuildingType CurrentBuilding => currentBuildingType;
        public int BuildingDifficulty => buildingDifficulty;
        public int CurrentRoom => currentRoom;
        public int CurrentRoomDifficulty => currentRoomDifficulty;

        public void EnterRoom(RogueLiteDoor rogueLiteDoor)
        {
            currentRoomDifficulty = rogueLiteDoor.doorRoomDifficulty;
            currentRoom++;

            if (currentBuildingType == BuildingType.NONE)
            {
                currentBuildingType = rogueLiteDoor.buildingType;
            }

            // Calculate the new room position
            Vector3 currentPosition = currentBuildingParent != null ? currentBuildingParent.transform.position : Vector3.zero;
            Vector3 newPosition = RogueLiteManager.Instance.RoomManager.CalculateNewRoomPosition(currentPosition, rogueLiteDoor);

            Debug.Log($"Entering room: Current={currentPosition}, New={newPosition}, Door={rogueLiteDoor.transform.position}");

            // Check if a room already exists at this position
            if (RogueLiteManager.Instance.RoomManager.GetRoomAtPosition(newPosition) != null)
            {
                Debug.LogWarning($"Room already exists at position {newPosition}, using existing room");
                RogueLiteManager.Instance.RoomManager.EnterRoom(newPosition, rogueLiteDoor);
                return;
            }

            SetupLevel(currentBuildingType, newPosition);
        }

        private void SetupLevel(BuildingType buildingType, Vector3 position)
        {
            Debug.Log($"Setting up level at position: {position}");

            // Store the current player spawn point before creating the new building
            if (currentBuildingParent != null)
            {
                RogueLiteRoomParent oldRandomizer = currentBuildingParent.GetComponent<RogueLiteRoomParent>();
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
                // Set the position of the new building
                newBuildingParent.transform.position = position;
                Debug.Log($"New building parent position set to: {position}");

                // Set up the new building
                RogueLiteRoomParent randomizer = newBuildingParent.GetComponent<RogueLiteRoomParent>();
                if (randomizer != null)
                {
                    randomizer.GenerateRandomRooms(selectedBuilding);
                }
                else
                {
                    Debug.LogError("RoomSectionRandomizer component not found on new building parent!");
                }

                // Add to spawned buildings list and update current
                spawnedBuildings.Add(newBuildingParent);
                currentBuildingParent = newBuildingParent;

                // Get the current room data after setup
                var currentRoom = RogueLiteManager.Instance.RoomManager.GetCurrentRoom();
                if (currentRoom != null)
                {
                    Debug.Log($"Current room setup complete at {position} with difficulty {currentRoom.difficulty}");
                }
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
                RogueLiteRoomParent randomizer = currentBuildingParent.GetComponent<RogueLiteRoomParent>();
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