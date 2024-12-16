using System.Collections;
using UnityEngine;

public class Chest : MonoBehaviour, IInteractive<ResourcePickup>
{
    [Header("Chest Contents")]
    [SerializeField] private ResourcePickup chestContents;
    public bool isOpened = false; // Whether the chest is already opened

    [Header("Chest Animation Settings")]
    [SerializeField] private Transform door; // The lid or door of the chest
    [SerializeField] private Vector3 rotationAxis = Vector3.right; // Axis of rotation
    [SerializeField] private float openAngle = 90f; // Angle to rotate when opened
    [SerializeField] private float openSpeed = 2f; // Speed of opening/closing

    private Quaternion closedRotation;
    private Quaternion openRotation;
    private Coroutine animationCoroutine;

    private void Start()
    {
        if (door == null)
        {
            Debug.LogError("Door is not assigned! Please assign the door Transform.");
            return;
        }

        // Initialize closed and open rotations
        closedRotation = door.localRotation;
        openRotation = door.localRotation * Quaternion.Euler(rotationAxis * openAngle);
    }

    public void CloseChest()
    {
        if (!isOpened)
        {
            Debug.Log("Chest is already closed!");
            return;
        }

        isOpened = false;
        Debug.Log("Chest closed.");

        // Start the door closing animation
        StartDoorAnimation(closedRotation);
    }

    private void StartDoorAnimation(Quaternion targetRotation)
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        animationCoroutine = StartCoroutine(RotateDoor(targetRotation));
    }

    private IEnumerator RotateDoor(Quaternion targetRotation)
    {
        while (Quaternion.Angle(door.localRotation, targetRotation) > 0.01f)
        {
            door.localRotation = Quaternion.Lerp(door.localRotation, targetRotation, Time.deltaTime * openSpeed);
            yield return null;
        }

        // Ensure the door snaps precisely to the target rotation
        door.localRotation = targetRotation;
    }

    public string GetInteractionText() => isOpened ? "Chest already opened!" : "Press E to open chest";

    object IInteractiveBase.Interact() => Interact();

    public ResourcePickup Interact()
    {
        if (isOpened)
        {
            Debug.Log("Chest is already opened!");
            return null;
        }

        isOpened = true;

        // Start the door opening animation
        StartDoorAnimation(openRotation);

        if (chestContents.GetResourceObj() == null)
        {
            Debug.Log("Chest is empty!");
            return null;
        }

        Debug.Log("Chest opened! It contains:");
        Debug.Log($"{chestContents.GetResourceObj().resourceName}");

        return chestContents;
    }

    public bool CanInteract() => !isOpened;
}



