using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Managers;

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
    [SerializeField] private Vector3 boxCastSize = new Vector3(0.5f, 0.5f, 0.5f); // Size of the box cast for interaction detection

    [Header("Players currently equipped items")] 
    public WeaponElement dashElement = WeaponElement.NONE;

    [Header("Mutation Grid")]
    [SerializeField] private int maxMutationSlots = 9; // Default to a 3x3 grid
    private List<GeneticMutationObj> equippedMutations = new List<GeneticMutationObj>();

    public List<MutationQuantityEntry> availableMutations = new List<MutationQuantityEntry>();

    public int MaxMutationSlots => maxMutationSlots;
    public List<GeneticMutationObj> EquippedMutations => equippedMutations;

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

    public override void Start()
    {

    }

    private void OnDestroy()
    {
        PlayerInput.Instance.OnUpdatePlayerControls -= SetPlayerControlType;
    }

    private void Update()
    {
        if (PlayerController.Instance._possessedNPC == null)
            return;

        if (PlayerInput.Instance.CurrentControlType != PlayerControlType.IN_CONVERSATION)
            DetectInteraction(); // Detect if the player is looking at a chest
        else
            ClearInteractive();
    }

    public void AddToPlayerInventory(ResourceScriptableObj resource, int count = 1)
    {
        AddItem(resource, count);
    }

    public void AddToPlayerInventory(ResourceItemCount resourcePickup)
    {
        //TODO Display this on the UI so that the player can seee the inventory items being added

        var resourceItem = resourcePickup.GetResourceObj();

        switch (resourceItem)
        {
            case WeaponScriptableObj weapon:
                if(PlayerController.Instance._possessedNPC != null)
                {
                    if (PlayerController.Instance._possessedNPC.GetEquipped() == null)
                    {
                        PlayerController.Instance._possessedNPC.EquipWeapon(weapon);
                    }
                    else
                    {
                        // Show weapon comparison menu
                        WeaponScriptableObj currentWeapon = PlayerController.Instance._possessedNPC.GetEquipped();
                        PlayerUIManager.Instance.weaponComparisonMenu.ShowWeaponComparison(
                            currentWeapon,
                            weapon,
                            (equipNew) => {
                                if (equipNew)
                                {
                                    PlayerController.Instance._possessedNPC.EquipWeapon(weapon);
                                }
                            }
                        );
                    }
                }
                break;
            case GeneticMutationObj geneticMutation:
                AddAvalibleMutation(geneticMutation);
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

    public void AddAvalibleMutation(GeneticMutationObj geneticMutation)
    {
        bool mutationFound = false;
        for (int i = 0; i < availableMutations.Count; i++)
        {
            if (availableMutations[i].mutation == geneticMutation)
            {
                availableMutations[i].quantity++;
                mutationFound = true;
                break;
            }
        }

        if (!mutationFound)
        {
            availableMutations.Add(new MutationQuantityEntry
            {
                mutation = geneticMutation,
                quantity = 1
            });
        }
    }

    private void DetectInteraction()
    {
        RaycastHit hit;
        Vector3 startPos = PlayerController.Instance._possessedNPC.GetTransform().position + Vector3.up;
        Vector3 direction = PlayerController.Instance._possessedNPC.GetTransform().forward;
        
        if (Physics.BoxCast(startPos, boxCastSize * 0.5f, direction, out hit, PlayerController.Instance._possessedNPC.GetTransform().rotation, interactionRange))
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

    private void OnBPressed()
    {
        if (currentInteractive != null && currentInteractive.CanInteract())
        {
            object interactReturnObj = currentInteractive.Interact();
            HandleInteractionResult(interactReturnObj);
        }
    }

    private void HandleInteractionResult(object result)
    {
        if (result == null) return;

        switch (result)
        {
            case ResourceItemCount resourcePickup:
                AddToPlayerInventory(resourcePickup);
                break;
            case NarrativeAsset narrative:
                PlayerUIManager.Instance.narrativeSystem.StartConversation(narrative);
                break;
            case RogueLiteDoor rogueLiteDoor:
                RogueLiteManager.Instance.EnterRoom(rogueLiteDoor);
                break;
            case Building building:
                // Show the work task selection popup
                CampManager.Instance.WorkManager.ShowWorkTaskOptions(building, (HumanCharacterController)PlayerController.Instance._possessedNPC, (task) => {
                    if (task != null && PlayerController.Instance._possessedNPC is RobotCharacterController robot)
                    {
                        robot.StartWork(task);
                    }
                    CampManager.Instance.WorkManager.CloseSelectionPopup();
                });
                break;
            default:
                Debug.Log($"Unhandled interaction result type: {result.GetType().Name}");
                break;
        }
    }

    public void SetPlayerControlType(PlayerControlType controlType)
    {
        switch (controlType)
        {
            case PlayerControlType.COMBAT_NPC_MOVEMENT:
                PlayerInput.Instance.OnBPressed += OnBPressed;
                break;
            case PlayerControlType.CAMP_NPC_MOVEMENT:
                PlayerInput.Instance.OnBPressed += OnBPressed;
                break;
            case PlayerControlType.ROBOT_MOVEMENT:
                PlayerInput.Instance.OnBPressed += OnBPressed;
                break;
            default:
                break;
        }
    }

    protected override Transform GetCharacterTransform()
    {
        if (PlayerController.Instance._possessedNPC == null)
            return null;

        return PlayerController.Instance._possessedNPC.GetTransform();
    }

    public void EquipMutation(GeneticMutationObj mutation)
    {
        equippedMutations.Add(mutation);
        if (mutation.mutationEffectPrefab != null && PlayerController.Instance._possessedNPC != null)
        {
            GameObject effectObj = Instantiate(mutation.mutationEffectPrefab, PlayerController.Instance._possessedNPC.GetTransform());
            BaseMutationEffect effect = effectObj.GetComponent<BaseMutationEffect>();
            if (effect != null)
            {
                effect.Initialize(mutation);
                effect.OnEquip();
            }
        } else{
            Debug.LogWarning("Trying to equip mutation but no NPC is possessed or mutation effect prefab is null");
        }
    }

    public void RemoveMutation(GeneticMutationObj mutation)
    {
        if (equippedMutations.Remove(mutation))
        {
            // Find all mutation effects and find the one that matches our mutation
            BaseMutationEffect[] effects = new BaseMutationEffect[0];
            if (PlayerController.Instance._possessedNPC != null)
            {
                effects = PlayerController.Instance._possessedNPC.GetTransform()
                    .GetComponentsInChildren<BaseMutationEffect>();
            }
            else
            {
                Debug.LogWarning("Trying to remove mutation but no NPC is possessed");
            }
            
            foreach (BaseMutationEffect effect in effects)
            {
                if (effect.MutationData == mutation)
                {
                    effect.OnUnequip();
                    Destroy(effect.gameObject);
                    break;
                }
            }
        }
    }

    public void SetMaxMutationSlots(int slots)
    {
        maxMutationSlots = slots;
    }
}
