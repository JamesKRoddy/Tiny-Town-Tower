using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInventory : CharacterInventory, IControllerInput
{
    // Static instance of the PlayerUIManager class
    private static PlayerInventory _instance;

    // Public property to access the instance
    public static PlayerInventory Instance
    {
        get
        {
            // Check if the instance is null
            if (_instance == null)
            {
                // Try to find the PlayerCombat in the scene
                _instance = FindFirstObjectByType<PlayerInventory>();

                // If not found, log a warning
                if (_instance == null)
                {
                    Debug.LogWarning("PlayerInventory instance not found in the scene!");
                }
            }
            return _instance;
        }
    }

    [Header ("Chest handling")]
    private IInteractive<object> currentInteractive; // The interaction the player is currently looking at
    public float interactionRange = 3f; // Distance to detect weapons    

    [Header("Players currently equipped items")] 
    public GeneticMutation geneticMutation = GeneticMutation.NONE;
    public WeaponElement dashElement = WeaponElement.NONE;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
        else
        {
            _instance = this; // Set the instance
            DontDestroyOnLoad(gameObject); // Optionally persist across scenes
        }

        PlayerInput.Instance.OnUpdatePlayerControls += SetPlayerControlType;
    }

    private void OnDestroy()
    {
        PlayerInput.Instance.OnUpdatePlayerControls -= SetPlayerControlType;
    }

    public override void Start()
    {
        base.Start();
    }


    private void Update()
    {
        if (PlayerInput.Instance.currentControlType != PlayerControlType.IN_CONVERSATION)
            DetectInteraction(); // Detect if the player is looking at a chest
        else
            ClearInteractive();
    }

    public void AddToCharacterInventory(ResourcePickup resourcePickup)
    {
        //TODO might move this to base class
        //TODO Display this on the UI so that the player can seee the inventory items being added
        //TODO go through inventory and add correctly, if its a usable weapon then equip it, mutation same etc.

        var resourceItem = resourcePickup.GetResourceObj();

        switch (resourceItem)
        {
            case WeaponScriptableObj weapon:

                if (equippedWeaponScriptObj == null)
                {
                    EquipWeapon(weapon);                    
                }
                else
                {
                    Debug.LogError("//TODO have to drop current weapon and equip new one");
                }
                break;

            case ResourceScriptableObj resource:
                AddItem(resource, resourcePickup.count);
                break;

            // Add additional cases here for other item types if necessary.
            default:
                Debug.LogWarning("Unhandled chest item type.");
                break;
        }
    }

    public void EquipDash()
    {
        //TODO flesh this out set dashElement
    }

    public void EquipGeneticMutation()
    {
        //TODO flesh this out set geneticMutation
    }

    private void DetectInteraction()
    {
        RaycastHit hit;
        if (Physics.Raycast(PlayerController.Instance.possesedNPC.transform.position + Vector3.up, PlayerController.Instance.possesedNPC.transform.transform.forward, out hit, interactionRange))
        {
            IInteractiveBase interactive = hit.collider.GetComponent<IInteractiveBase>();

            if (interactive != null && interactive.CanInteract())
            {
                currentInteractive = (IInteractive<object>)interactive;
                PlayerUIManager.Instance.InteractionPrompt(interactive.GetInteractionText());
                return;
            }
        }

        ClearInteractive();
    }


    private void ClearInteractive()
    {
        currentInteractive = null;
        PlayerUIManager playerUIManager = PlayerUIManager.Instance;
        if (playerUIManager != null)
        {
            playerUIManager.HideInteractionPropt();
        }
    }

    private void HandleInteraction()
    {
        Debug.Log("HandleInteraction");
        if (currentInteractive != null && currentInteractive.CanInteract())
        {
            object interactReturnObj = currentInteractive.Interact();

            switch (interactReturnObj)
            {
                case ResourcePickup resourcePickup:
                    AddToCharacterInventory(resourcePickup);
                    break;
                case NarrativeAsset narrative:
                    NarrativeSystem.Instance.StartConversation(narrative);
                    break;
                case RogueLiteDoor rogueLiteDoor:
                    RogueLiteManager.Instance.EnterRoom(rogueLiteDoor);
                case null:
                    Debug.Log($"Cannot interact with {currentInteractive.GetType().Name}");
                    break;
                default:
                    Debug.Log($"Unhandled interaction result type: {interactReturnObj.GetType().Name}");
                    break;
            }
        }
    }

    public void SetPlayerControlType(PlayerControlType controlType)
    {
        switch (controlType)
        {
            case PlayerControlType.COMBAT_MOVEMENT:
                PlayerInput.Instance.OnBPressed += HandleInteraction;
                break;
            case PlayerControlType.CAMP_MOVEMENT:
                PlayerInput.Instance.OnBPressed += HandleInteraction;
                break;
            default:
                break;
        }
    }

    protected override Transform GetCharacterTransform()
    {
        return PlayerController.Instance.possesedNPC.transform;
    }
}
