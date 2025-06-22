using UnityEngine;
using Managers;
using System.Collections;
using System;

public class RogueLiteDoor : MonoBehaviour, IInteractive<RogueLiteDoor>, IInteractiveBase
{
    [Header("Door Settings")]
    public DoorStatus doorType;
    public Transform playerSpawn;
    [SerializeField] protected GameObject lockedDoorEffect;
    [SerializeField] protected GameObject nextRoomDoorEffect;
    [SerializeField] protected GameObject previousRoomDoorEffect;

    protected bool isLocked;

    protected virtual void Awake()
    {
        // Initialize any base door functionality here if needed
    }

    protected virtual void Start()
    {
        isLocked = doorType == DoorStatus.LOCKED;
    }

    public virtual void OnDoorEntered()
    {
        if (isLocked) return;
        
        // Base implementation - override in derived classes
        Debug.Log($"Door {gameObject.name} entered");
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

    public virtual bool CanInteract()
    {
        switch (doorType)
        {
            case DoorStatus.LOCKED:
                return false;
            case DoorStatus.ENTRANCE:
                return true;
            case DoorStatus.EXIT:
                return true;
            default:
                return false;
        }
    }

    public virtual string GetInteractionText()
    {
        switch (doorType)
        {
            case DoorStatus.LOCKED:
                return "Door Locked";
            case DoorStatus.ENTRANCE:
                return "Enter";
            case DoorStatus.EXIT:
                return "Exit";
            default:
                return "INVALID";
        }
    }

    protected virtual void ShowDoorEffects()
    {
        if (lockedDoorEffect != null) lockedDoorEffect.SetActive(doorType == DoorStatus.LOCKED);
        if (nextRoomDoorEffect != null) nextRoomDoorEffect.SetActive(doorType == DoorStatus.ENTRANCE);
        if (previousRoomDoorEffect != null) previousRoomDoorEffect.SetActive(doorType == DoorStatus.EXIT);
    }

    protected virtual void HideDoorEffects()
    {
        if (lockedDoorEffect != null) lockedDoorEffect.SetActive(false);
        if (nextRoomDoorEffect != null) nextRoomDoorEffect.SetActive(false);
        if (previousRoomDoorEffect != null) previousRoomDoorEffect.SetActive(false);
    }
}
