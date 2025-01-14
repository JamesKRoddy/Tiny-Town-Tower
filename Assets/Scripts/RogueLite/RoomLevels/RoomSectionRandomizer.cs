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

        RandomizePropsInSection(centerPiece);

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
            RogueLiteManager.Instance.SetRoomState(RoomSetupState.PRE_ENEMY_SPAWNING);
        }
        else
        {
            Debug.LogError("NavMeshSurface is not assigned.");
        }
    }
}
