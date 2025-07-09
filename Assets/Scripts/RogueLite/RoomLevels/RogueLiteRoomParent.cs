using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using System.Linq;
using Managers;
using Enemies;

public class RogueLiteRoomParent : MonoBehaviour
{
    [Header("Central Piece")]
    public Transform centerPiece;

    [Header("Room Spawn Points")]
    public Transform roomSpawnPointsParent;
    private Transform[] roomTransforms;

    [Header("Gizmo Settings")]
    [SerializeField] private bool showDirectionArrows = true;
    [SerializeField] private float arrowLength = 5f;
    [SerializeField] private float arrowHeadLength = 1.5f;
    [SerializeField] private float arrowHeadAngle = 20f;
    [SerializeField] private Color arrowColor = Color.cyan;

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

        // Get all child transforms from the parent
        if (roomSpawnPointsParent != null)
        {
            roomTransforms = new Transform[roomSpawnPointsParent.childCount];
            for (int i = 0; i < roomSpawnPointsParent.childCount; i++)
            {
                roomTransforms[i] = roomSpawnPointsParent.GetChild(i);
            }
        }
        else
        {
            Debug.LogError("Room Spawn Points Parent is not assigned!");
        }
    }

    public void GenerateRandomRooms(BuildingDataScriptableObj buildingScriptableObj)
    {
        if (buildingScriptableObj == null)
        {
            Debug.LogError("Building Scriptable Object is null!");
            return;
        }

        // Clear existing rooms
        ClearExistingRooms();

        // Clear props on the center piece
        ClearPropsOnCenterPiece();

        int currentDifficulty = DifficultyManager.Instance.GetCurrentWaveDifficulty();

        // Instantiate a room for each direction using the scriptable object's selection
        for (int i = 0; i < roomTransforms.Length; i++)
        {
            InstantiateRoom(roomTransforms[i], buildingScriptableObj.GetBuildingRoom(currentDifficulty));
        }

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

        foreach (Transform roomTransform in roomTransforms)
        {
            foreach (Transform child in roomTransform)
            {
                Destroy(child.gameObject);
            }
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

    private void InstantiateRoom(Transform targetTransform, GameObject roomPrefab)
    {
        if (!roomPrefab.GetComponent<RogueLiteRoom>())
        {
            Debug.LogError($"{roomPrefab.name} has no RogueLiteRoom Component");
            return;
        }

        // Calculate the world position for the room
        Vector3 worldPosition = targetTransform.position;

        GameObject room = Instantiate(roomPrefab, worldPosition, targetTransform.rotation, targetTransform);
        spawnedRooms[worldPosition] = room;

        RogueLiteRoom roomComponent = room.GetComponent<RogueLiteRoom>();
        roomComponent.Setup();

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
        }
        else
        {
            Debug.LogError("NavMeshSurface is not assigned.");
        }
    }

    private void SetupDoors()
    {
        List<RogueLikeRoomDoor> doors = new List<RogueLikeRoomDoor>(transform.GetComponentsInChildren<RogueLikeRoomDoor>());

        if (doors == null || doors.Count == 0)
        {
            Debug.LogWarning("No doors found in the scene.");
            return;
        }

        // Assign a random door as the entrance
        int entranceIndex = Random.Range(0, doors.Count);
        playerSpawnPoint = doors[entranceIndex].playerSpawn;
        doors[entranceIndex].doorType = DoorStatus.EXIT;

        // Store the exit door for connecting to the previous room
        RogueLikeRoomDoor exitDoor = doors[entranceIndex];
        doors.RemoveAt(entranceIndex);

        int exitCount = Mathf.Clamp(Random.Range(1, 4), 1, doors.Count);

        for (int i = 0; i < exitCount; i++)
        {
            int randomIndex = Random.Range(0, doors.Count);
            doors[randomIndex].doorType = DoorStatus.ENTRANCE;
            
            // Connect this entrance door to the previous room's exit door
            if (exitDoor != null)
            {
                // Forward connection: entrance door -> previous room
                doors[randomIndex].targetRoom = exitDoor.GetComponentInParent<RogueLiteRoomParent>();
                doors[randomIndex].targetSpawnPoint = exitDoor.playerSpawn;

                // Backward connection: exit door -> this room
                exitDoor.targetRoom = this;
                exitDoor.targetSpawnPoint = doors[randomIndex].playerSpawn;
            }

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
        List<ChestParent> chests = new List<ChestParent>(transform.GetComponentsInChildren<ChestParent>());

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

            chest.SetupChest(DifficultyManager.Instance.GetCurrentRoomDifficulty());
        }
    }

    public List<EnemySpawnPoint> GetEnemySpawnPoints()
    {
        return new List<EnemySpawnPoint>(transform.GetComponentsInChildren<EnemySpawnPoint>());
    }

    private void OnDrawGizmos()
    {
        if (!showDirectionArrows) return;

        // Get room transforms for gizmo drawing (works in editor)
        Transform[] gizmoRoomTransforms = GetRoomTransforms();
        if (gizmoRoomTransforms == null) return;

        Gizmos.color = arrowColor;

        for (int i = 0; i < gizmoRoomTransforms.Length; i++)
        {
            if (gizmoRoomTransforms[i] != null)
            {
                DrawDirectionArrow(gizmoRoomTransforms[i], $"Room {i}");
            }
        }
    }

    private Transform[] GetRoomTransforms()
    {
        // If we're in play mode and roomTransforms is already populated, use it
        if (Application.isPlaying && roomTransforms != null)
        {
            return roomTransforms;
        }

        // Otherwise, get from parent (for editor gizmos)
        if (roomSpawnPointsParent != null)
        {
            Transform[] transforms = new Transform[roomSpawnPointsParent.childCount];
            for (int i = 0; i < roomSpawnPointsParent.childCount; i++)
            {
                transforms[i] = roomSpawnPointsParent.GetChild(i);
            }
            return transforms;
        }

        return null;
    }

    private void DrawDirectionArrow(Transform targetTransform, string label)
    {
        Vector3 position = targetTransform.position;
        Vector3 forward = targetTransform.forward;
        
        // Draw the main arrow shaft
        Vector3 arrowEnd = position + forward * arrowLength;
        Gizmos.DrawLine(position, arrowEnd);
        
        // Draw arrow head
        Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;
        Vector3 up = Vector3.Cross(right, forward).normalized;
        
        Vector3 arrowHeadRight = arrowEnd - forward * arrowHeadLength + right * arrowHeadLength * 0.5f;
        Vector3 arrowHeadLeft = arrowEnd - forward * arrowHeadLength - right * arrowHeadLength * 0.5f;
        
        Gizmos.DrawLine(arrowEnd, arrowHeadRight);
        Gizmos.DrawLine(arrowEnd, arrowHeadLeft);
        
        // Draw label
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(position + Vector3.up * 0.5f, label);
        #endif
    }
}
