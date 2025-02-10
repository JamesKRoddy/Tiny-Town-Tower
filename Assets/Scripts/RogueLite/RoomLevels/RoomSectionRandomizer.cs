using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using System.Linq;

public class RoomSectionRandomizer : MonoBehaviour
{
    [Header("Central Piece")]
    public Transform centerPiece;

    [Header("Surrounding Transforms")]
    public Transform frontTransform;
    public Transform backTransform;
    public Transform leftTransform;
    public Transform rightTransform;

    Transform playerSpawnPoint; //The door the player will spawn infront of;

    private List<GameObject> roomPrefabs;

    private NavMeshSurface navMeshSurface;

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

        RandomizePropsInSection(centerPiece); //Just to setup centre piece too

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
        // Select a random prefab from the list
        GameObject randomPrefab = roomPrefabs[Random.Range(0, roomPrefabs.Count)];

        if (!randomPrefab.GetComponent<RogueLiteRoom>())
        {
            Debug.LogError($"{randomPrefab.name} has no RogueLiteRoom Component");
            return;
        }

        // Instantiate the prefab at the position and rotation of the target transform
        GameObject room = Instantiate(randomPrefab, targetTransform.position, targetTransform.rotation);

        room.GetComponent<RogueLiteRoom>().Setup(roomPosition);

        // Set the instantiated room as a child of the target transform
        room.transform.SetParent(targetTransform);

        RandomizePropsInSection(room.transform);
    }

    private void RandomizePropsInSection(Transform sectionTransform)
    {
        if (sectionTransform == null) return;

        // Find all PropRandomizer components within the section
        PropRandomizer[] propRandomizers = sectionTransform.GetComponentsInChildren<PropRandomizer>();

        foreach (PropRandomizer propRandomizer in propRandomizers)
        {
            propRandomizer.RandomizeProps();
        }
    }

    private IEnumerator DelayedBakeNavMesh()
    {
        yield return null; // Wait for the next frame

        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();            
            Debug.Log("NavMesh baked at runtime.");
            RogueLiteManager.Instance.SetRoomState(EnemySetupState.PRE_ENEMY_SPAWNING);
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

        // Remove the entrance from the list
        doors.RemoveAt(entranceIndex);

        // Determine the number of exit doors (between 1 and 3, but not exceeding the remaining doors)
        int exitCount = Mathf.Clamp(Random.Range(1, 4), 1, doors.Count);

        // Randomly assign exit doors
        for (int i = 0; i < exitCount; i++)
        {
            int randomIndex = Random.Range(0, doors.Count);
            doors[randomIndex].doorType = DoorStatus.ENTRANCE;
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

    internal Vector3 GetPlayerSpawnPoint()
    {
        return playerSpawnPoint.position;
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

            chest.SetupChest(RogueLiteManager.Instance.GetCurrentRoomDifficulty());
        }

        Debug.Log("Chests have been setup.");
    }
}
