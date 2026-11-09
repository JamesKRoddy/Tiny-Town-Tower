using UnityEngine;
using Managers;
using System.Collections;
using System;

public class RogueLiteDoor : MonoBehaviour, IInteractive<RogueLiteDoor>, IInteractiveBase
{
    [Header("Door Settings")]
    public DoorStatus doorType;
    public Transform playerSpawn;
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
        // Set color: Red for locked, Green for unlocked
        Gizmos.color = (doorType == DoorStatus.LOCKED) ? Color.red : Color.green;

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
        return doorType == DoorStatus.UNLOCKED;
    }

    public virtual string GetInteractionText()
    {
        return doorType == DoorStatus.LOCKED ? "Door Locked" : "Open Door";
    }

    protected virtual void ShowDoorEffects()
    {
        bool isUnlocked = doorType == DoorStatus.UNLOCKED;
        
        if (nextRoomDoorEffect != null) nextRoomDoorEffect.SetActive(isUnlocked);
        
        // previousRoomDoorEffect is deprecated (no longer used)
        if (previousRoomDoorEffect != null) previousRoomDoorEffect.SetActive(false);
    }

    protected virtual void HideDoorEffects()
    {
        if (nextRoomDoorEffect != null) nextRoomDoorEffect.SetActive(false);
        if (previousRoomDoorEffect != null) previousRoomDoorEffect.SetActive(false);
    }
}
