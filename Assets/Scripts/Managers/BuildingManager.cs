using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    public class BuildingManager : MonoBehaviour
    {
        [Header("Building Settings")]
        [SerializeField] private List<BuildingDataScriptableObj> buildingDataScriptableObjs;
        
        private BuildingType currentBuilding = BuildingType.NONE;
        private GameObject currentBuildingParent;
        private int buildingDifficulty;
        private int currentRoom;
        private int currentRoomDifficulty;

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
            if (currentBuildingParent != null)
            {
                Destroy(currentBuildingParent);
            }

            int difficulty = GetCurrentWaveDifficulty();
            currentBuildingParent = Instantiate(GetBuildingParent(buildingType, difficulty, out BuildingDataScriptableObj selectedBuilding));

            if (currentBuildingParent != null && selectedBuilding != null)
            {
                RoomSectionRandomizer randomizer = currentBuildingParent.GetComponent<RoomSectionRandomizer>();
                if (randomizer != null)
                {
                    randomizer.GenerateRandomRooms(selectedBuilding);
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
            if (playerTransform != null && currentBuildingParent != null)
            {
                RoomSectionRandomizer randomizer = currentBuildingParent.GetComponent<RoomSectionRandomizer>();
                if (randomizer != null)
                {
                    playerTransform.position = randomizer.GetPlayerSpawnPoint();
                }
            }
        }
    }
} 