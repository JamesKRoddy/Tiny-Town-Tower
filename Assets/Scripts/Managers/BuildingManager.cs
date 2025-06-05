using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    public class BuildingManager : MonoBehaviour
    {
        [Header("Building Settings")]
        [SerializeField] private List<BuildingDataScriptableObj> buildingDataScriptableObjs;
        
        private BuildingType currentBuildingType = BuildingType.NONE;
        private GameObject currentRoomParent;        
        private int buildingDifficulty;
        private int currentRoom;
        private int currentRoomDifficulty;
        private Vector3 lastPlayerSpawnPoint;
        private float roomSpacing = 100f;
        private Dictionary<Vector3, GameObject> spawnedRooms = new Dictionary<Vector3, GameObject>();

        public GameObject CurrentRoomParent => currentRoomParent;

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
            Vector3 currentPosition = currentRoomParent != null ? currentRoomParent.transform.position : Vector3.zero;
            Vector3 newPosition = CalculateNewRoomPosition(currentPosition, rogueLiteDoor);

            Debug.Log($"Entering room: Current={currentPosition}, New={newPosition}, Door={rogueLiteDoor.transform.position}");

            SpawnRoom(currentBuildingType, newPosition);
        }

        public void ReturnToPreviousRoom(RogueLiteDoor rogueLiteDoor)
        {
            if (rogueLiteDoor.targetRoom == null)
            {
                Debug.LogError("Door has no target room set!");
                return;
            }

            // Set the target room as the current room
            currentRoomParent = rogueLiteDoor.targetRoom.gameObject;
            
            // Setup the player at the target spawn point
            if (PlayerController.Instance != null && PlayerController.Instance._possessedNPC != null)
            {
                if (rogueLiteDoor.targetSpawnPoint != null)
                {
                    PlayerController.Instance._possessedNPC.GetTransform().position = rogueLiteDoor.targetSpawnPoint.position;
                }
                else
                {
                    SetupPlayer(PlayerController.Instance._possessedNPC.GetTransform());
                }
            }
        }

        private Vector3 CalculateNewRoomPosition(Vector3 currentPosition, RogueLiteDoor entranceDoor)
        {
            // Get the door's forward direction in world space
            Vector3 doorDirection = entranceDoor.transform.forward;
            
            // Calculate the new position based on the door's direction
            Vector3 newPosition = currentPosition + (doorDirection * roomSpacing);
            
            // Round the position to avoid floating point issues
            newPosition = new Vector3(
                Mathf.Round(newPosition.x / roomSpacing) * roomSpacing,
                Mathf.Round(newPosition.y / roomSpacing) * roomSpacing,
                Mathf.Round(newPosition.z / roomSpacing) * roomSpacing
            );
            
            Debug.Log($"Calculating new room position: Current={currentPosition}, Door Direction={doorDirection}, New={newPosition}");
            return newPosition;
        }

        private Vector3 CalculatePreviousRoomPosition(Vector3 currentPosition, RogueLiteDoor exitDoor)
        {
            // Get the door's forward direction in world space
            Vector3 doorDirection = exitDoor.transform.forward;
            
            // Calculate the previous position based on the door's direction (opposite of forward)
            Vector3 previousPosition = currentPosition - (doorDirection * roomSpacing);
            
            // Round the position to avoid floating point issues
            previousPosition = new Vector3(
                Mathf.Round(previousPosition.x / roomSpacing) * roomSpacing,
                Mathf.Round(previousPosition.y / roomSpacing) * roomSpacing,
                Mathf.Round(previousPosition.z / roomSpacing) * roomSpacing
            );
            
            Debug.Log($"Calculating previous room position: Current={currentPosition}, Door Direction={doorDirection}, Previous={previousPosition}");
            return previousPosition;
        }

        private void SpawnRoom(BuildingType buildingType, Vector3 position)
        {
            Debug.Log($"Setting up level at position: {position}");

            // Store the current player spawn point before creating the new building
            if (currentRoomParent != null)
            {
                RogueLiteRoomParent oldRandomizer = currentRoomParent.GetComponent<RogueLiteRoomParent>();
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

                currentRoomParent = newBuildingParent;
                spawnedRooms[position] = newBuildingParent;
            }
            else
            {
                Debug.LogError($"No building parent found for {buildingType} at difficulty {difficulty}.");
            }
        }

        private GameObject GetBuildingParent(BuildingType buildingType, int difficulty, out BuildingDataScriptableObj selectedBuilding)
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

            if (currentRoomParent != null)
            {
                RogueLiteRoomParent randomizer = currentRoomParent.GetComponent<RogueLiteRoomParent>();
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