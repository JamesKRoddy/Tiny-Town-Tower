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
    public Action<RoomSetupState> OnSetupStateChanged;

    Transform playerSpawnPoint; //The door the player will spawn infront of;

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
                OnSetupStateChanged?.Invoke(roomSetupState); // Invoke the event with the new state
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
        OnSetupStateChanged += SetupPlayer;
        roomSetupState = RoomSetupState.ROOM_CLEARED; //TODO this is just for testing
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
                SetupDoors();
                SetupChests();
                return;
            }
        }

        Debug.LogError($"No building data for type {buildingType}");

    }

    private void SetupDoors()
    {
        List<RogueLiteDoor> doors = new List<RogueLiteDoor>(FindObjectsByType<RogueLiteDoor>(FindObjectsSortMode.None));

        if (doors == null || doors.Count == 0)
        {
            Debug.LogWarning("No doors found in the scene.");
            return;
        }

        // Assign a random door as the entrance
        int entranceIndex = Random.Range(0, doors.Count);
        playerSpawnPoint = doors[entranceIndex].playerSpawn;
        doors[entranceIndex].doorType = DoorStatus.ENTRANCE;

        // Remove the entrance from the list
        doors.RemoveAt(entranceIndex);

        // Determine the number of exit doors (between 1 and 3, but not exceeding the remaining doors)
        int exitCount = Mathf.Clamp(Random.Range(1, 4), 1, doors.Count);

        // Randomly assign exit doors
        for (int i = 0; i < exitCount; i++)
        {
            int randomIndex = Random.Range(0, doors.Count);
            doors[randomIndex].doorType = DoorStatus.EXIT;
            doors.RemoveAt(randomIndex);
        }

        // Set remaining doors as locked and apply a 75% chance to disable their GameObjects
        foreach (var door in doors)
        {
            door.doorType = DoorStatus.LOCKED;

            // 75% chance to disable the GameObject
            if (Random.value < 0.75f)
            {
                door.gameObject.SetActive(false);
            }
        }

        Debug.Log("Doors have been spawned and assigned.");
    }

    public void SetupChests()
    {
        // Find all chests in the scene
        List<ChestParent> chests = new List<ChestParent>(FindObjectsByType<ChestParent>(FindObjectsSortMode.None));

        if (chests == null || chests.Count == 0)
        {
            Debug.LogWarning("No chests found in the scene.");
            return;
        }

        foreach (var chest in chests)
        {
            // 75% chance to disable the chest
            if (Random.value < 0.75f)
            {
                chest.gameObject.SetActive(false);
                Debug.Log($"Chest {chest.name} is disabled.");
                continue; // Skip assigning loot to disabled chests
            }

            chest.SetupChest(GetCurrentRoomDifficulty());
        }

        Debug.Log("Chests have been setup.");
    }


    private void SetupPlayer(RoomSetupState newState)
    {
        if (newState != RoomSetupState.PRE_ENEMY_SPAWNING)
            return;

        if(PlayerController.Instance != null && PlayerController.Instance.possesedNPC != null)
        {
            PlayerController.Instance.possesedNPC.transform.position = playerSpawnPoint.transform.position;
        }
    }
}
