using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

public class RogueLiteManager : MonoBehaviour
{
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
            DontDestroyOnLoad(gameObject); // Optionally persist across scenes
        }
    }

    private RoomSetupState roomSetupState;
    public List<BuildingDataScriptableObj> buildingDataScriptableObjs;
    public List<EnemyWaveConfig> waveConfigs; // List of all possible wave configurations //TODO move this to a separate container with all the waves divided up into different buildings
    public BuildingType currentBuilding = BuildingType.NONE;
    private GameObject currentbuildingParent;
    public int buildingDifficulty; //The difficulty of the current building
    public int currentRoom; //The current room the player is in
    int currentRoomDifficulty; //The difficulty of the current room

    /// <summary>
    /// All actions relating to roguelite gameplay loop
    /// </summary>
    public Action<RoomSetupState> OnRoomSetupStateChanged;

    /// <summary>
    /// Property to encapsulate roomSetupState and invoke OnSetupStateChanged
    /// whenever it changes.
    /// </summary>
    private RoomSetupState RoomSetupState
    {
        get => roomSetupState;
        set
        {
            if (roomSetupState != value)
            {
                Debug.Log($"Updating RoomSetupState to {value}");
                roomSetupState = value;
                OnRoomSetupStateChanged?.Invoke(roomSetupState); // Invoke the event with the new state
            }
        }
    }

    //Call this whenever the state needs to be changed
    public void SetRoomState(RoomSetupState newState)
    {
        RoomSetupState = newState;
    }

    public RoomSetupState GetRoomState()
    {
        return RoomSetupState;
    }

    private void Start()
    {
        OnRoomSetupStateChanged += RoomSetupStateChanged;
        roomSetupState = RoomSetupState.ROOM_CLEARED; //TODO this is just for testing
    }

    private void OnDestroy()
    {
        OnRoomSetupStateChanged -= RoomSetupStateChanged;
    }

    private void RoomSetupStateChanged(RoomSetupState newState)
    {
        switch (newState)
        {
            case RoomSetupState.NONE:
                break;
            case RoomSetupState.ENTERING_ROOM:
                break;
            case RoomSetupState.PRE_ENEMY_SPAWNING:
                SetupPlayer();
                break;
            case RoomSetupState.ENEMIES_SPAWNED:
                break;
            case RoomSetupState.ROOM_CLEARED:
                break;
            default:
                break;
        }
    }

    public void EnterRoom(RogueLiteDoor rogueLiteDoor)
    {
        SetRoomState(RoomSetupState.ENTERING_ROOM);

        currentRoomDifficulty = rogueLiteDoor.doorRoomDifficulty;
        currentRoom++;

        if(currentBuilding == BuildingType.NONE)
        {
            currentBuilding = rogueLiteDoor.buildingType;
        }

        SetupLevel(currentBuilding);
    }

    public int GetCurrentRoomDifficulty() //TODO this is going to require a lot of testing
    {
        int baseDifficulty = currentRoom * buildingDifficulty;
        int adjustedDifficulty = baseDifficulty + currentRoomDifficulty;
        return adjustedDifficulty;
    }

    public EnemyWaveConfig GetWaveConfig()
    {
        foreach (var config in waveConfigs) //TODO pick a wave config based on difficulty
        {
            return config; //TODO ********************************************************* this will only return the first wave, doing now for testing
        }

        // Return a default or null if no exact match is found
        Debug.LogWarning("No matching waveConfig found. Returning null.");
        return null;
    }

    public void SetupLevel(BuildingType buildingType)
    {
        if(buildingDataScriptableObjs == null)
        {
            Debug.LogError("RogueLiteManager BuildingDataScriptableObjs are null");
        }

        if(currentbuildingParent != null)
        {
            Destroy(currentbuildingParent);
        }

        foreach (var building in buildingDataScriptableObjs)
        {
            if(building.buildingType == buildingType)
            {
                currentbuildingParent = GameObject.Instantiate(building.GetBuildingParent(GetCurrentRoomDifficulty()), Vector3.zero, Quaternion.identity);

                currentbuildingParent.GetComponent<RoomSectionRandomizer>().GenerateRandomRooms(building);
                return;
            }
        }

        Debug.LogError($"No building data for type {buildingType}");

    }

    private void SetupPlayer()
    {
        if (PlayerController.Instance != null && PlayerController.Instance.PossessedNPC != null)
        {
            PlayerController.Instance.PossessedNPC.transform.position = currentbuildingParent.GetComponent<RoomSectionRandomizer>().GetPlayerSpawnPoint();
        }
    }
}
