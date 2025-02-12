using System.Collections.Generic;
using UnityEngine;

public class RogueLiteManager : GameModeManager<RogueLikeEnemyWaveConfig>
{
    [SerializeField] protected List<BuildingDataScriptableObj> buildingDataScriptableObjs;
    public BuildingType currentBuilding = BuildingType.NONE;
    private GameObject currentBuildingParent;
    public int buildingDifficulty;
    public int currentRoom;
    private int currentRoomDifficulty;

    // Singleton instance
    private static RogueLiteManager _instance;

    // Singleton property to get the instance
    public static RogueLiteManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Find the GameManager instance if it hasn't been assigned
                _instance = FindFirstObjectByType<RogueLiteManager>();
                if (_instance == null)
                {
                    Debug.LogWarning("RogueLiteManager instance not found in the scene!");
                }
            }
            return _instance;
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
        }
    }

    protected override void EnemySetupStateChanged(EnemySetupState newState)
    {
        switch (newState)
        {
            case EnemySetupState.NONE:
                break;
            case EnemySetupState.WAVE_START:
                EnemySpawnManager.Instance.ResetWaveCount();
                break;
            case EnemySetupState.PRE_ENEMY_SPAWNING:
                SetupPlayer();
                SetEnemySetupState(EnemySetupState.ENEMIES_SPAWNED);
                EnemySpawnManager.Instance.StartSpawningEnemies(GetWaveConfig(GetCurrentWaveDifficulty()));
                break;
            case EnemySetupState.ENEMIES_SPAWNED:
                break;
            case EnemySetupState.ALL_WAVES_CLEARED:
                break;
            default:
                break;
        }               
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

    private void SetupPlayer()
    {
        if (PlayerController.Instance != null && PlayerController.Instance._possessedNPC != null && currentBuildingParent != null)
        {
            RoomSectionRandomizer randomizer = currentBuildingParent.GetComponent<RoomSectionRandomizer>();
            if (randomizer != null)
            {
                PlayerController.Instance._possessedNPC.GetTransform().position = randomizer.GetPlayerSpawnPoint();
            }
        }
    }

    public void EnterRoom(RogueLiteDoor rogueLiteDoor)
    {
        SetEnemySetupState(EnemySetupState.WAVE_START);
        currentRoomDifficulty = rogueLiteDoor.doorRoomDifficulty;
        currentRoom++;

        if (currentBuilding == BuildingType.NONE)
        {
            currentBuilding = rogueLiteDoor.buildingType;
        }

        SetupLevel(currentBuilding);
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

    public override int GetCurrentWaveDifficulty()
    {
        return (currentRoom * buildingDifficulty) + currentRoomDifficulty;
    }
}
