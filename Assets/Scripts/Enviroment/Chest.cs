// Updated Chest Script
using System.Collections;
using UnityEngine;

public class Chest : MonoBehaviour, IInteractive<ResourcePickup>
{

    private ResourcePickup chestContents;

    public bool isOpened = false;

    [Header("Chest Animation Settings")]
    [SerializeField] private Transform door;
    [SerializeField] private Vector3 rotationAxis = Vector3.right;
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openSpeed = 2f;

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

    public void AssignChestLoot(int roomDifficulty, LootTableScriptableObj lootTableScriptableObj)
    {
        if (lootTableScriptableObj == null)
        {
            Debug.LogError("LootTableScriptableObj is not assigned! Please assign a LootTableScriptableObj.");
            return;
        }

        ResourceRarity rarity = DifficultyRarityMapper.GetResourceRarity(roomDifficulty);
        chestContents = lootTableScriptableObj.GetLootByRarity(rarity);

        if (chestContents != null)
        {
            Debug.Log($"Chest {name} contains {chestContents.GetResourceObj().objectName} (x{chestContents.count}), Rarity: {rarity}.");
        }
        else
        {
            Debug.Log($"Chest {name} is empty.");
        }
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

        if (chestContents == null || chestContents.GetResourceObj() == null)
        {
            Debug.Log("Chest is empty!");
            return null;
        }

        Debug.Log("Chest opened! It contains:");
        Debug.Log($"{chestContents.GetResourceObj().objectName}");

        return chestContents;
    }

    public bool CanInteract() => !isOpened;
}