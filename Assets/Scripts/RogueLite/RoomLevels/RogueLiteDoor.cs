using UnityEngine;

public class RogueLiteDoor : MonoBehaviour, IInteractive<RogueLiteDoor>
{
    public GameObject doorModel; // TODO: Door models will change based on room difficulty and loot. GetCurrentRoomDifficulty
    public Transform playerSpawn;
    public DoorStatus doorType;
    public int doorRoomDifficulty;
    internal BuildingType buildingType; //The type of room this door leads to

    // Draw a gizmo arrow to show the door's local forward direction
    private void OnDrawGizmos()
    {
        // Set the color based on the door type
        switch (doorType)
        {
            case DoorStatus.ENTRANCE:
                Gizmos.color = Color.green;
                break;
            case DoorStatus.EXIT:
                Gizmos.color = Color.blue;
                break;
            case DoorStatus.LOCKED:
                Gizmos.color = Color.red;
                break;
        }

        // Draw an arrow indicating the forward direction
        Vector3 forward = transform.forward * 2f; // Adjust arrow length
        Vector3 position = transform.position + Vector3.up;
        Gizmos.DrawLine(position, position + forward);
        Gizmos.DrawSphere(position + forward, 0.1f); // Draw a sphere at the arrowhead
    }

    object IInteractiveBase.Interact() => Interact();

    public RogueLiteDoor Interact()
    {
        return this;
    }

    public bool CanInteract()
    {
        if (RogueLiteManager.Instance.GetRoomState() != EnemySetupState.ALL_WAVES_CLEARED)
            return false;

        switch (doorType)
        {
            case DoorStatus.LOCKED:
                return false;
            case DoorStatus.ENTRANCE:
                return true;
            case DoorStatus.EXIT:
                return false;
            default:
                return false;
        }
    }

    public string GetInteractionText()
    {
        switch (doorType)
        {
            case DoorStatus.LOCKED:
                return "Door Locked";
            case DoorStatus.ENTRANCE:
                return "Enter Room";
            case DoorStatus.EXIT:
                return "Cant Go Back";
            default:
                return "INVALID";
        }
    }
}
