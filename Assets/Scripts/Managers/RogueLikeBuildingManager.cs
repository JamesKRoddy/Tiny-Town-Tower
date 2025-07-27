using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    [System.Serializable]
    public class RoomPlacementData
    {
        public Vector3 position;
        public GameObject roomObject;
        public Bounds bounds;
        public RogueLiteRoom roomComponent;
        
        public RoomPlacementData(Vector3 pos, GameObject obj, Bounds roomBounds, RogueLiteRoom room)
        {
            position = pos;
            roomObject = obj;
            bounds = roomBounds;
            roomComponent = room;
        }
    }
    
    public class RogueLikeBuildingManager : MonoBehaviour
    {
        [Header("RogueLike Building Settings")]
        [SerializeField] private List<RogueLikeBuildingDataScriptableObj> rogueLikeBuildingDataScriptableObjs;
        
        private RogueLikeBuildingDataScriptableObj currentBuilding;
        private Transform rogueLikeBuildingSpawn;
        public Transform RogueLikeBuildingSpawn => rogueLikeBuildingSpawn;
        private GameObject currentRoomParent;  
        private RogueLiteRoomParent currentRoomParentComponent;
        private int currentMaxRooms;
        private Vector3 lastPlayerSpawnPoint;
        private float minRoomSpacing = 20f;
        private Dictionary<Vector3, GameObject> spawnedRooms = new Dictionary<Vector3, GameObject>();
        private List<RoomPlacementData> placedRooms = new List<RoomPlacementData>();

        public GameObject CurrentRoomParent => currentRoomParent;
        public RogueLiteRoomParent CurrentRoomParentComponent => currentRoomParentComponent;

        public RogueLikeBuildingDataScriptableObj CurrentBuilding => currentBuilding;
        public int RogueLikeBuildingDifficulty => DifficultyManager.Instance.GetCurrentRogueLikeBuildingDifficulty();
        public int CurrentRoom => DifficultyManager.Instance.GetCurrentRoomNumber();
        public int CurrentRoomDifficulty => DifficultyManager.Instance.GetCurrentRoomDifficulty();

        public RogueLikeBuildingDataScriptableObj SetBuildingData(RogueLikeBuildingType buildingType){
            // Find all buildings matching the door's building type
            List<RogueLikeBuildingDataScriptableObj> matchingBuildings = rogueLikeBuildingDataScriptableObjs.FindAll(
                building => building.buildingType == buildingType
            );

            if (matchingBuildings.Count == 0)
            {
                Debug.LogError($"No buildings found matching type: {buildingType}");
                return null;
            }            

            // Select a random building from the matching ones
            int randomIndex = Random.Range(0, matchingBuildings.Count);

            currentBuilding = rogueLikeBuildingDataScriptableObjs[randomIndex];

            // Note: Difficulty is now initialized by the OverWorldDoor before this method is called
            currentMaxRooms = currentBuilding.GetMaxRoomsForDifficulty(DifficultyManager.Instance.GetCurrentWaveDifficulty());

            rogueLikeBuildingSpawn = matchingBuildings[randomIndex].buildingEntrance.GetComponent<RogueLikeBuildingEntrance>().PlayerSpawnPoint;
            if (rogueLikeBuildingSpawn == null)
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
            // Simple line formation: spawn building parents in a straight line along the X-axis
            // This ensures no overlapping and predictable placement
            
            int roomCount = placedRooms.Count;
            float buildingSpacing = 150f; // Larger spacing for entire building parents
            
            // Start at an offset to avoid overlapping with building entrance at (0,0,0)
            Vector3 newPosition = new Vector3((roomCount + 1) * buildingSpacing, 0, 0);
            
            Debug.Log($"[BuildingManager] Positioning building parent {roomCount} at: {newPosition}");
            
            return newPosition;
        }

        public void SpawnRoom(RogueLikeBuildingType buildingType, Vector3 position)
        {
            // Store the current player spawn point before creating the new building
            if (currentRoomParent != null)
            {
                if (currentRoomParentComponent != null)
                {
                    lastPlayerSpawnPoint = currentRoomParentComponent.GetPlayerSpawnPoint();
                }
            }

            // Create the new building
            int difficulty = DifficultyManager.Instance.GetCurrentWaveDifficulty();
            GameObject newBuildingParent = Instantiate(GetBuildingParent(buildingType, difficulty, out RogueLikeBuildingDataScriptableObj selectedBuilding));

            if (newBuildingParent != null && selectedBuilding != null)
            {
                // Set the position of the new building
                newBuildingParent.transform.position = position;

                // Set up the new building
                RogueLiteRoomParent randomizer = newBuildingParent.GetComponent<RogueLiteRoomParent>();
                if (randomizer != null)
                {
                    randomizer.GenerateRandomRooms(selectedBuilding);
                    
                    // Store building placement data for tracking
                    placedRooms.Add(new RoomPlacementData(position, newBuildingParent, new Bounds(position, Vector3.one * 100f), null));
                }
                else
                {
                    Debug.LogError("RoomSectionRandomizer component not found on new building parent!");
                }

                currentRoomParent = newBuildingParent;
                currentRoomParentComponent = newBuildingParent.GetComponent<RogueLiteRoomParent>();
                spawnedRooms[position] = newBuildingParent;
            }
            else
            {
                Debug.LogError($"No building parent found for {buildingType} at difficulty {difficulty}.");
            }
        }

        private GameObject GetBuildingParent(RogueLikeBuildingType buildingType, int difficulty, out RogueLikeBuildingDataScriptableObj selectedBuilding)
        {
            foreach (var buildingData in rogueLikeBuildingDataScriptableObjs)
            {
                if (buildingData is RogueLikeBuildingDataScriptableObj building && building.buildingType == buildingType)
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
                if (currentRoomParentComponent != null)
                {
                    Vector3 spawnPoint = currentRoomParentComponent.GetPlayerSpawnPoint();
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
            placedRooms.Clear();
            DifficultyManager.Instance.ResetDifficulty();
        }

        public void ClearDebugState()
        {
            currentRoomParent = null;
            spawnedRooms.Clear();
            placedRooms.Clear();
            Debug.Log("[BuildingManager] Debug state cleared");
        }
    }
} 