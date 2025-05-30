using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using System.Linq;
using Managers;

public class RoomSectionRandomizer : MonoBehaviour
{
    [Header("Central Piece")]
    public Transform centerPiece;

    [Header("Surrounding Transforms")]
    public Transform frontTransform;
    public Transform backTransform;
    public Transform leftTransform;
    public Transform rightTransform;

    private Transform playerSpawnPoint;
    private List<GameObject> roomPrefabs;
    private NavMeshSurface navMeshSurface;
    private Dictionary<Vector3, GameObject> spawnedRooms = new Dictionary<Vector3, GameObject>();

    private RogueLiteManager rogueLiteManager;

    private void Awake()
    {
        rogueLiteManager = FindFirstObjectByType<RogueLiteManager>();
        if (rogueLiteManager == null)
        {
            Debug.LogError("RogueLiteManager not found in scene!");
        }
    }

    public void GenerateRandomRooms(BuildingDataScriptableObj buildingScriptableObj)
    {
        List<GameObject> roomsScriptObj = buildingScriptableObj.buildingRooms.Select(room => room.buildingRoom).ToList();

        if (roomsScriptObj == null || roomsScriptObj.Count == 0)
        {
            Debug.LogError("No room prefabs assigned!");
            return;
        }

        roomPrefabs = roomsScriptObj;

        // Clear existing rooms
        ClearExistingRooms();

        // Clear props on the center piece
        ClearPropsOnCenterPiece();

        // Instantiate a random room for each direction
        InstantiateRoom(frontTransform, RoomPosition.FRONT);
        InstantiateRoom(backTransform, RoomPosition.BACK);
        InstantiateRoom(leftTransform, RoomPosition.LEFT);
        InstantiateRoom(rightTransform, RoomPosition.RIGHT);

        RandomizePropsInSection(centerPiece);

        SetupDoors();
        SetupChests();

        if (navMeshSurface == null)
        {
            navMeshSurface = FindAnyObjectByType<NavMeshSurface>();
        }

        StartCoroutine(DelayedBakeNavMesh());
    }

    private void ClearExistingRooms()
    {
        foreach (var room in spawnedRooms.Values)
        {
            if (room != null)
            {
                Destroy(room);
            }
        }
        spawnedRooms.Clear();

        foreach (Transform child in frontTransform)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in backTransform)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in leftTransform)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in rightTransform)
        {
            Destroy(child.gameObject);
        }
    }

    private void ClearPropsOnCenterPiece()
    {
        PropRandomizer propRandomizer = centerPiece.GetComponentInChildren<PropRandomizer>();
        if (propRandomizer != null)
        {
            foreach (Transform child in propRandomizer.transform)
            {
                if (child != propRandomizer.transform)
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }

    private void InstantiateRoom(Transform targetTransform, RoomPosition roomPosition)
    {
        GameObject randomPrefab = roomPrefabs[Random.Range(0, roomPrefabs.Count)];

        if (!randomPrefab.GetComponent<RogueLiteRoom>())
        {
            Debug.LogError($"{randomPrefab.name} has no RogueLiteRoom Component");
            return;
        }

        GameObject room = Instantiate(randomPrefab, targetTransform.position, targetTransform.rotation, targetTransform);
        spawnedRooms[targetTransform.position] = room;

        RogueLiteRoom roomComponent = room.GetComponent<RogueLiteRoom>();
        roomComponent.roomDifficulty = rogueLiteManager.GetCurrentWaveDifficulty();
        roomComponent.Setup();

        // Register the room with RoomManager
        rogueLiteManager.RoomManager.RegisterRoom(room, targetTransform.position, roomComponent.roomDifficulty);

        RandomizePropsInSection(room.transform);
    }

    private void RandomizePropsInSection(Transform sectionTransform)
    {
        if (sectionTransform == null) return;

        PropRandomizer[] propRandomizers = sectionTransform.GetComponentsInChildren<PropRandomizer>();

        foreach (PropRandomizer propRandomizer in propRandomizers)
        {
            propRandomizer.RandomizeProps();
        }
    }

    private IEnumerator DelayedBakeNavMesh()
    {
        yield return null;

        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();            
            rogueLiteManager.SetEnemySetupState(EnemySetupState.PRE_ENEMY_SPAWNING);
        }
        else
        {
            Debug.LogError("NavMeshSurface is not assigned.");
        }
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
        doors[entranceIndex].doorType = DoorStatus.EXIT;

        doors.RemoveAt(entranceIndex);

        int exitCount = Mathf.Clamp(Random.Range(1, 4), 1, doors.Count);

        for (int i = 0; i < exitCount; i++)
        {
            int randomIndex = Random.Range(0, doors.Count);
            doors[randomIndex].doorType = DoorStatus.ENTRANCE;
            doors.RemoveAt(randomIndex);
        }

        foreach (var door in doors)
        {
            door.doorType = DoorStatus.LOCKED;
            if (Random.value < 0.75f)
            {
                door.gameObject.SetActive(false);
            }
        }
    }

    internal Vector3 GetPlayerSpawnPoint()
    {
        if (playerSpawnPoint != null)
        {
            return playerSpawnPoint.position;
        }
        
        // Fallback to center piece if no spawn point is set
        return centerPiece != null ? centerPiece.position : Vector3.zero;
    }

    public void SetupChests()
    {
        List<ChestParent> chests = new List<ChestParent>(FindObjectsByType<ChestParent>(FindObjectsSortMode.None));

        if (chests == null || chests.Count == 0)
        {
            Debug.LogWarning("No chests found in the scene.");
            return;
        }

        foreach (var chest in chests)
        {
            if (Random.value < 0.75f)
            {
                chest.gameObject.SetActive(false);
                continue;
            }

            chest.SetupChest(rogueLiteManager.GetCurrentWaveDifficulty());
        }
    }
}
