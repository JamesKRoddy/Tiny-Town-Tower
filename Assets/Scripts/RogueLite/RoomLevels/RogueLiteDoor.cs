using UnityEngine;

public class RogueLiteDoor : MonoBehaviour, IInteractive<RogueLiteDoor>
{
    public GameObject doorModel; // TODO: Door models will change based on room difficulty and loot. GetCurrentRoomDifficulty
    public Transform playerSpawn;
    public DoorType doorType;
    public int doorRoomDifficulty;

    // Draw a gizmo arrow to show the door's local forward direction
    private void OnDrawGizmos()
    {
        // Set the color based on the door type
        switch (doorType)
        {
            case DoorType.ENTRANCE:
                Gizmos.color = Color.green;
                break;
            case DoorType.EXIT:
                Gizmos.color = Color.blue;
                break;
            case DoorType.LOCKED:
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
        if (RogueLiteManager.Instance.GetRoomState() != RoomSetupState.ROOM_CLEARED)
            return false;

        switch (doorType)
        {
            case DoorType.LOCKED:
                return false;
            case DoorType.ENTRANCE:
                return true;
            case DoorType.EXIT:
                return false;
            default:
                return false;
        }
    }

    public string GetInteractionText()
    {
        switch (doorType)
        {
            case DoorType.LOCKED:
                return "Door Locked";
            case DoorType.ENTRANCE:
                return "Enter Room";
            case DoorType.EXIT:
                return "Cant Go Back";
            default:
                return "INVALID";
        }
    }
}
