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
        private GameObject instantiatedBuildingEntrance; // Track the instantiated entrance
        
        /// <summary>
        /// Returns the spawn point position from the instantiated building entrance.
        /// If not yet instantiated, returns Vector3.zero.
        /// </summary>
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
        public int RogueLikeBuildingDifficulty => GameManager.Instance.DifficultyManager.GetCurrentRogueLikeBuildingDifficulty();
        public int CurrentRoom => GameManager.Instance.DifficultyManager.GetCurrentRoomNumber();
        public int CurrentRoomDifficulty => GameManager.Instance.DifficultyManager.GetCurrentRoomDifficulty();

        /// <summary>
        /// Sets up building data for the specified building type and prepares for scene transition.
        /// The actual building entrance is instantiated after scene load via the callback.
        /// </summary>
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
            currentMaxRooms = currentBuilding.GetMaxRoomsForDifficulty(GameManager.Instance.DifficultyManager.GetCurrentWaveDifficulty());

            // Verify the building entrance prefab has the required component
            if (matchingBuildings[randomIndex].buildingEntrance == null)
            {
                Debug.LogError($"Building entrance prefab is null for {buildingType}");
                return null;
            }

            RogueLikeBuildingEntrance entranceComponent = matchingBuildings[randomIndex].buildingEntrance.GetComponent<RogueLikeBuildingEntrance>();
            if (entranceComponent == null || entranceComponent.PlayerSpawnPoint == null)
            {
                Debug.LogError($"No RogueLikeBuildingEntrance or PlayerSpawnPoint found on prefab: {matchingBuildings[randomIndex].buildingEntrance.name}");
                return null;
            }

            Debug.Log($"[RogueLikeBuildingManager] Building data set for {buildingType}. Entrance will be instantiated after scene load.");

            // Track building progression milestone
            GameManager.Instance.GameProgressionManager.OnBuildingTypeReached(buildingType);

            return currentBuilding;
        }

        /// <summary>
        /// Instantiates the building entrance and sets up the spawn point.
        /// Called after the RogueLike scene has loaded.
        /// </summary>
        public void InstantiateBuildingEntrance()
        {
            Debug.Log("[RogueLikeBuildingManager] InstantiateBuildingEntrance called");
            
            if (currentBuilding == null)
            {
                Debug.LogError("[RogueLikeBuildingManager] Cannot instantiate building entrance - currentBuilding is null!");
                return;
            }

            if (currentBuilding.buildingEntrance == null)
            {
                Debug.LogError($"[RogueLikeBuildingManager] Cannot instantiate building entrance - buildingEntrance prefab is null on {currentBuilding.name}!");
                return;
            }

            Debug.Log($"[RogueLikeBuildingManager] Building: {currentBuilding.name}, Entrance Prefab: {currentBuilding.buildingEntrance.name}");

            // Clean up any existing entrance
            if (instantiatedBuildingEntrance != null)
            {
                Debug.Log("[RogueLikeBuildingManager] Cleaning up existing building entrance");
                Destroy(instantiatedBuildingEntrance);
            }

            // Instantiate the building entrance
            Debug.Log("[RogueLikeBuildingManager] Instantiating building entrance at Vector3.zero");
            instantiatedBuildingEntrance = Instantiate(currentBuilding.buildingEntrance, Vector3.zero, Quaternion.identity);
            Debug.Log($"[RogueLikeBuildingManager] Instantiated entrance GameObject: {instantiatedBuildingEntrance.name}");
            
            // Get the spawn point from the instantiated object (not the prefab)
            RogueLikeBuildingEntrance entrance = instantiatedBuildingEntrance.GetComponent<RogueLikeBuildingEntrance>();
            if (entrance == null)
            {
                Debug.LogError("[RogueLikeBuildingManager] No RogueLikeBuildingEntrance component found on instantiated entrance!");
                return;
            }

            Debug.Log($"[RogueLikeBuildingManager] Found RogueLikeBuildingEntrance component");

            if (entrance.PlayerSpawnPoint == null)
            {
                Debug.LogError("[RogueLikeBuildingManager] PlayerSpawnPoint is null on RogueLikeBuildingEntrance!");
                return;
            }

            rogueLikeBuildingSpawn = entrance.PlayerSpawnPoint;
            Debug.Log($"[RogueLikeBuildingManager] Building entrance instantiated successfully!");
            Debug.Log($"[RogueLikeBuildingManager] Spawn Transform: {rogueLikeBuildingSpawn.name}");
            Debug.Log($"[RogueLikeBuildingManager] Spawn Position: {rogueLikeBuildingSpawn.position}");
        }

        public bool EnterRoomCheck(RogueLikeRoomDoor rogueLiteDoor)
        {
            // Automatically calculate and set room difficulty based on building difficulty and room number
            GameManager.Instance.DifficultyManager.SetNextRoomDifficulty();
            
            //Reached the end of the building check
            if(GameManager.Instance.DifficultyManager.GetCurrentRoomNumber() >= currentMaxRooms){
                // Check if we should spawn a boss room instead of leaving
                if (TrySpawnBossRoom(rogueLiteDoor))
                {
                    return true; // Boss room spawned, continue gameplay
                }
                
                // No boss, leave the building
                LeaveBuilding();
                return false;
            }

            // Calculate the new room position
            Vector3 currentPosition = currentRoomParent != null ? currentRoomParent.transform.position : Vector3.zero;
            Vector3 newPosition = CalculateNewRoomPosition(currentPosition, rogueLiteDoor);

            SpawnRoom(currentBuilding.buildingType, newPosition);
            
            return true;
        }
        
        /// <summary>
        /// Attempt to spawn a boss room at the end of the building
        /// Returns true if boss room was spawned, false otherwise
        /// </summary>
        private bool TrySpawnBossRoom(RogueLikeRoomDoor entranceDoor)
        {
            if (currentBuilding == null)
            {
                Debug.LogWarning("[RogueLikeBuildingManager] No current building data for boss spawn check");
                return false;
            }
            
            // Check if a boss should spawn
            if (!currentBuilding.ShouldSpawnBoss())
            {
                Debug.Log("[RogueLikeBuildingManager] Boss spawn roll failed or no bosses configured");
                return false;
            }
            
            // Get a boss for the current difficulty
            int difficulty = GameManager.Instance.DifficultyManager.GetCurrentWaveDifficulty();
            BossScriptableObj boss = currentBuilding.GetBossForDifficulty(difficulty);
            
            if (boss == null)
            {
                Debug.LogWarning($"[RogueLikeBuildingManager] No suitable boss found for difficulty {difficulty}");
                return false;
            }
            
            // Get the boss room data
            RoomParentDataScriptableObj bossRoomData = currentBuilding.GetBossRoomData(boss);
            
            if (bossRoomData == null || !bossRoomData.IsValid())
            {
                Debug.LogError($"[RogueLikeBuildingManager] Invalid boss room data for boss: {boss.bossName}");
                return false;
            }
            
            // Calculate position for boss room
            Vector3 currentPosition = currentRoomParent != null ? currentRoomParent.transform.position : Vector3.zero;
            Vector3 bossRoomPosition = CalculateNewRoomPosition(currentPosition, entranceDoor);
            
            // Instantiate the boss room parent
            GameObject bossRoomParentPrefab = bossRoomData.roomParentPrefab;
            GameObject bossRoomInstance = Instantiate(bossRoomParentPrefab, bossRoomPosition, Quaternion.identity);
            
            // Get the BossRoomParent component
            BossRoomParent bossRoomParent = bossRoomInstance.GetComponent<BossRoomParent>();
            
            if (bossRoomParent == null)
            {
                Debug.LogError($"[RogueLikeBuildingManager] Boss room prefab does not have BossRoomParent component!");
                Destroy(bossRoomInstance);
                return false;
            }
            
            // Set up the boss room
            bossRoomParent.SetBoss(boss);
            bossRoomParent.GenerateBossArena(bossRoomData, difficulty);
            
            // Update current room tracking
            currentRoomParent = bossRoomInstance;
            currentRoomParentComponent = bossRoomParent;
            placedRooms.Add(new RoomPlacementData(bossRoomPosition, bossRoomInstance, new Bounds(bossRoomPosition, Vector3.one * 100f), null));
            spawnedRooms[bossRoomPosition] = bossRoomInstance;
            
            Debug.Log($"[RogueLikeBuildingManager] ✓ Successfully spawned boss room for: {boss.bossName}");
            
            return true;
        }

        /// <summary>
        /// Destroys the previous room/entrance. Called during fade out before spawning a new room.
        /// </summary>
        public void DestroyPreviousRoom()
        {
            if (currentRoomParent != null)
            {
                Debug.Log($"[RogueLikeBuildingManager] Destroying previous room: {currentRoomParent.name}");
                
                // Remove from tracking dictionaries
                Vector3 positionToRemove = currentRoomParent.transform.position;
                if (spawnedRooms.ContainsKey(positionToRemove))
                {
                    spawnedRooms.Remove(positionToRemove);
                }
                
                // Remove from placed rooms list
                placedRooms.RemoveAll(r => r.roomObject == currentRoomParent);
                
                // Destroy the room GameObject
                Destroy(currentRoomParent);
                currentRoomParent = null;
                currentRoomParentComponent = null;
            }
            
            // Also destroy the building entrance if it exists (first room transition)
            if (instantiatedBuildingEntrance != null)
            {
                Debug.Log($"[RogueLikeBuildingManager] Destroying building entrance: {instantiatedBuildingEntrance.name}");
                Destroy(instantiatedBuildingEntrance);
                instantiatedBuildingEntrance = null;
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
            int difficulty = GameManager.Instance.DifficultyManager.GetCurrentWaveDifficulty();
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
            return GameManager.Instance.DifficultyManager.GetCurrentWaveDifficulty();
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
            // Track building completion milestone before clearing current building
            if (currentBuilding != null)
            {
                GameManager.Instance.GameProgressionManager.OnBuildingTypeCleared(currentBuilding.buildingType);
            }

            // Clean up instantiated building entrance
            if (instantiatedBuildingEntrance != null)
            {
                Destroy(instantiatedBuildingEntrance);
                instantiatedBuildingEntrance = null;
            }

            currentBuilding = null;
            currentRoomParent = null;
            rogueLikeBuildingSpawn = null;
            lastPlayerSpawnPoint = Vector3.zero;
            placedRooms.Clear();
            GameManager.Instance.DifficultyManager.ResetDifficulty();
        }

        public void ClearDebugState()
        {
            if (instantiatedBuildingEntrance != null)
            {
                Destroy(instantiatedBuildingEntrance);
                instantiatedBuildingEntrance = null;
            }
            
            currentRoomParent = null;
            rogueLikeBuildingSpawn = null;
            spawnedRooms.Clear();
            placedRooms.Clear();
            Debug.Log("[BuildingManager] Debug state cleared");
        }
    }
} 