using UnityEngine;
using Managers;
using System.Collections;
using System;

public class RogueLiteDoor : MonoBehaviour, IInteractive<RogueLiteDoor>, IInteractiveBase
{

    [Header("Door Settings")]
    public DoorStatus doorType;
    public BuildingType buildingType;
    public int doorRoomDifficulty;
    public Transform playerSpawn;
    [SerializeField] private GameObject lockedDoorEffect;
    [SerializeField] private GameObject unlockedDoorEffect;

    private RogueLiteRoom parentRoom;
    private bool isLocked;

    public void Initialize(RogueLiteRoom room)
    {
        parentRoom = room;
        isLocked = doorType == DoorStatus.LOCKED;
        RogueLiteManager.Instance.OnEnemySetupStateChanged += OnEnemySetupStateChanged;
    }

    private void OnEnemySetupStateChanged(EnemySetupState state)
    {
        if(state == EnemySetupState.ALL_WAVES_CLEARED)
        {
            if(doorType == DoorStatus.EXIT)
            {
                unlockedDoorEffect.SetActive(true);
            } else {
                lockedDoorEffect.SetActive(true);
            }
        }
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;
        doorType = locked ? DoorStatus.LOCKED : DoorStatus.ENTRANCE;
    }

    public void Reset()
    {
        isLocked = doorType == DoorStatus.LOCKED;
    }

    public void OnDoorEntered()
    {
        if (isLocked) return;

        if (doorType == DoorStatus.ENTRANCE)
        {
            RogueLiteManager.Instance.EnterRoomWithTransition(this);
        }
        else if (doorType == DoorStatus.EXIT)
        {
            // Handle exit door logic
            if (parentRoom != null)
            {
                parentRoom.OnRoomExited(this);
            }
        }
    }

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

    // IInteractiveBase implementation
    object IInteractiveBase.Interact()
    {
        return Interact();
    }

    // IInteractive<RogueLiteDoor> implementation
    public RogueLiteDoor Interact()
    {
        OnDoorEntered();
        return this;
    }

    public bool CanInteract()
    {
        if (RogueLiteManager.Instance.GetEnemySetupState() != EnemySetupState.ALL_WAVES_CLEARED)
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
                return "Can't Go Back";
            default:
                return "INVALID";
        }
    }
}
