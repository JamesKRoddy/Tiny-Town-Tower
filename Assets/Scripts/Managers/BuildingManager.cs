using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    public class BuildingManager : MonoBehaviour
    {
        [Header("Building Settings")]
        [SerializeField] private List<BuildingDataScriptableObj> buildingDataScriptableObjs;
        
        private BuildingDataScriptableObj currentBuilding;
        private Transform buildingSpawn;
        public Transform BuildingSpawn => buildingSpawn;
        private GameObject currentRoomParent;        
        private int currentMaxRooms;
        private Vector3 lastPlayerSpawnPoint;
        private float roomSpacing = 100f;
        private Dictionary<Vector3, GameObject> spawnedRooms = new Dictionary<Vector3, GameObject>();

        public GameObject CurrentRoomParent => currentRoomParent;

        public BuildingDataScriptableObj CurrentBuilding => currentBuilding;
        public int BuildingDifficulty => DifficultyManager.Instance.GetCurrentBuildingDifficulty();
        public int CurrentRoom => DifficultyManager.Instance.GetCurrentRoomNumber();
        public int CurrentRoomDifficulty => DifficultyManager.Instance.GetCurrentRoomDifficulty();

        public BuildingDataScriptableObj SetBuildingData(BuildingType buildingType){
            // Find all buildings matching the door's building type
            List<BuildingDataScriptableObj> matchingBuildings = buildingDataScriptableObjs.FindAll(
                building => building.buildingType == buildingType
            );

            if (matchingBuildings.Count == 0)
            {
                Debug.LogError($"No buildings found matching type: {buildingType}");
                return null;
            }            

            // Select a random building from the matching ones
            int randomIndex = Random.Range(0, matchingBuildings.Count);

            currentBuilding = buildingDataScriptableObjs[randomIndex];

            // Note: Difficulty is now initialized by the OverWorldDoor before this method is called
            currentMaxRooms = currentBuilding.GetMaxRoomsForDifficulty(DifficultyManager.Instance.GetCurrentWaveDifficulty());

            buildingSpawn = matchingBuildings[randomIndex].buildingEntrance.GetComponent<BuildingEntrance>().PlayerSpawnPoint;
            if (buildingSpawn == null)
            {
                Debug.LogError($"No buildingEntranceSpawnPoint found on gameobject: {matchingBuildings[0].buildingEntrance.name}");
                return null;
            }

            return currentBuilding;
        }

        public bool EnterRoomCheck(RogueLikeRoomDoor rogueLiteDoor)
        {
            // Automatically calculate and set room difficulty based on building difficulty and room number
            DifficultyManager.Instance.SetNextRoomDifficulty();
            
            //Reached the end of the building check
            if(DifficultyManager.Instance.GetCurrentRoomNumber() >= currentMaxRooms){
                LeaveBuilding();
                return false;
            }

            // Calculate the new room position
            Vector3 currentPosition = currentRoomParent != null ? currentRoomParent.transform.position : Vector3.zero;
            Vector3 newPosition = CalculateNewRoomPosition(currentPosition, rogueLiteDoor);

            SpawnRoom(currentBuilding.buildingType, newPosition);
            
            return true;
        }

        public void ReturnToPreviousRoom(RogueLikeRoomDoor rogueLiteDoor)
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

            return newPosition;
        }

        private void SpawnRoom(BuildingType buildingType, Vector3 position)
        {
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
            int difficulty = DifficultyManager.Instance.GetCurrentWaveDifficulty();
            GameObject newBuildingParent = Instantiate(GetBuildingParent(buildingType, difficulty, out BuildingDataScriptableObj selectedBuilding));

            if (newBuildingParent != null && selectedBuilding != null)
            {
                // Set the position of the new building
                newBuildingParent.transform.position = position;

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
            return DifficultyManager.Instance.GetCurrentWaveDifficulty();
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

        private void LeaveBuilding()
        {
            currentBuilding = null;
            currentRoomParent = null;
            lastPlayerSpawnPoint = Vector3.zero;
            DifficultyManager.Instance.ResetDifficulty();
        }
    }
} 