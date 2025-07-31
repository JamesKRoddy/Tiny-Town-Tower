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

    public List<MutationQuantityEntry> availableMutations = new List<MutationQuantityEntry>(); // List of available mutations, removed when mutation screen is closed

    [Header("NPC Management")]
    [SerializeField] private List<RecruitedNPCData> recruitedNPCs = new List<RecruitedNPCData>(); // NPCs recruited during roguelite exploration

    public int MaxMutationSlots => maxMutationSlots;
    public List<GeneticMutationObj> EquippedMutations => equippedMutations;



    // Events for NPC recruitment
    public event System.Action<Managers.SettlerData> OnNPCRecruited;
    public event System.Action<List<Managers.SettlerData>> OnNPCsTransferredToCamp;

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

    private void Update()
    {
        if (PlayerController.Instance._possessedNPC == null)
            return;

        if (PlayerInput.Instance.CurrentControlType != PlayerControlType.IN_CONVERSATION && PlayerInput.Instance.CurrentControlType != PlayerControlType.ROBOT_WORKING)
            DetectInteraction(); // Detect if the player is looking at a chest
        else
            ClearInteractive();
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
                        // Show popup for weapon equipped to NPC
                        PlayerUIManager.Instance.inventoryPopup.ShowWeaponPopup(weapon, false);
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
                                    // Show popup for weapon equipped to NPC
                                    PlayerUIManager.Instance.inventoryPopup.ShowWeaponPopup(weapon, false);
                                }
                            }
                        );
                    }
                }
                break;
            case GeneticMutationObj geneticMutation:
                Debug.Log("Adding genetic mutation to player inventory: " + geneticMutation.objectName);
                AddAvalibleMutation(geneticMutation);
                PlayerUIManager.Instance.utilityMenu.EnableGeneticMutationMenu();
                // Show popup for genetic mutation added to player inventory
                PlayerUIManager.Instance.inventoryPopup.ShowMutationPopup(geneticMutation, true);
                break;
            case ResourceScriptableObj resource:
                //Adding resources to the possessed NPC's inventory
                PlayerController.Instance.GetCharacterInventory().AddItem(resource, resourcePickup.count);
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
        // If the game is in rogue lite mode and the wave is active, don't allow the player to interact
        if (GameManager.Instance.CurrentGameMode == GameMode.ROGUE_LITE && RogueLiteManager.Instance.IsWaveActive)
        {
            ClearInteractive();
            return;
        }

        RaycastHit hit;
        Vector3 startPos = PlayerController.Instance._possessedNPC.GetTransform().position + Vector3.up;
        Vector3 direction = PlayerController.Instance._possessedNPC.GetTransform().forward;
        
        if (Physics.BoxCast(startPos, boxCastSize * 0.5f, direction, out hit, PlayerController.Instance._possessedNPC.GetTransform().rotation, interactionRange))
        {
            IInteractiveBase interactive = hit.collider.GetComponent<IInteractiveBase>();
            if (interactive != null && interactive.CanInteract())
            {
                if (interactive is IInteractive<object> typedInteractive)
                {
                    currentInteractive = typedInteractive;
                    PlayerUIManager.Instance.InteractionPrompt(interactive.GetInteractionText());
                    return;
                }
                else
                {
                    Debug.LogWarning($"Interactive object {interactive.GetType().Name} does not implement IInteractive<object>. Full type: {interactive.GetType().FullName}");
                }
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
        // If the game is in rogue lite mode and the wave is active, don't allow the player to interact
        if (GameManager.Instance.CurrentGameMode == GameMode.ROGUE_LITE && RogueLiteManager.Instance.IsWaveActive)
        {
            return;
        }

        if (currentInteractive != null && currentInteractive.CanInteract())
        {
            object interactReturnObj = currentInteractive.Interact();
            HandleInteractionResult(interactReturnObj);
        }
    }

    private void HandleInteractionResult(object result)
    {
        if (result == null) return;

        Debug.Log("Handling interaction result: " + result.GetType().Name);
        switch (result)
        {
            case ResourceItemCount resourcePickup:
                AddToPlayerInventory(resourcePickup);
                break;
            case NarrativeAsset narrative:
                NarrativeManager.Instance.StartConversation(narrative);
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
            case WorkTask[] workTasks:
                CampManager.Instance.WorkManager.ShowWorkTaskOptions(workTasks, (HumanCharacterController)PlayerController.Instance._possessedNPC, (task) => {
                    if (task != null && PlayerController.Instance._possessedNPC is RobotCharacterController robot)
                    {
                        robot.StartWork(task);
                    }
                    CampManager.Instance.WorkManager.CloseSelectionPopup();
                });
                break;
            case WorkTask workTask:
                if (PlayerController.Instance._possessedNPC is RobotCharacterController robot)
                {
                    PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.ROBOT_WORKING);
                    robot.StartWork(workTask);
                }
                break;
            default:
                Debug.Log($"<color=red> Unhandled interaction</color> result type: {result.GetType().Name}");
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
        if (mutation.prefab != null && PlayerController.Instance._possessedNPC != null)
        {
            GameObject effectObj = Instantiate(mutation.prefab, PlayerController.Instance._possessedNPC.GetTransform());
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

    public void ClearAvailableMutations(){
        availableMutations.Clear();
    }

    public override void AddItem(ResourceScriptableObj item, int count = 1)
    {
        // Call the base class method to add the item
        base.AddItem(item, count);
        
        // Show popup for item added to player inventory
        PlayerUIManager.Instance.inventoryPopup?.ShowInventoryPopup(item, count, true);
    }

    public override void AddItem(List<ResourceItemCount> items)
    {
        foreach (var item in items)
        {
            AddItem(item.resourceScriptableObj, item.count);
        }
    }

    #region NPC Recruitment

    /// <summary>
    /// Recruit a procedural settler during exploration (stores temporarily in inventory)
    /// </summary>
    public void RecruitNPC(SettlerNPC settlerNPCReference)
    {
        RecruitNPC(settlerNPCReference, null);
    }

    /// <summary>
    /// Recruit a procedural settler with component ID tracking during exploration
    /// </summary>
    public void RecruitNPC(SettlerNPC settlerNPCReference, string componentId)
    {
        if (settlerNPCReference == null)
        {
            Debug.LogWarning("[PlayerInventory] Attempted to recruit null SettlerNPC!");
            return;
        }

        string settlerName = settlerNPCReference.GetSettlerName();
        Debug.Log($"[PlayerInventory] RecruitNPC called with Settler: {settlerName}, ComponentID: {componentId}");
        Debug.Log($"[PlayerInventory] Current recruited NPCs count: {recruitedNPCs.Count}");
        
        // Check if this specific component has already been recruited
        if (!string.IsNullOrEmpty(componentId))
        {
            Debug.Log($"[PlayerInventory] Checking if component '{componentId}' is already recruited...");
            
            if (recruitedNPCs.Exists(data => data.componentId == componentId))
            {
                Debug.LogWarning($"[PlayerInventory] NPC component '{componentId}' ({settlerName}) is already recruited!");
                return;
            }
        }

        // Capture settler data from the SettlerNPC
        var settlerData = new Managers.SettlerData(
            settlerNPCReference.GetSettlerName(),
            settlerNPCReference.GetSettlerAge(),
            settlerNPCReference.GetSettlerDescription()
        );

        // Capture appearance data from the SettlerNPC
        NPCAppearanceData appearanceData = null;
        if (settlerNPCReference.GetAppearanceSystem() != null)
        {
            appearanceData = settlerNPCReference.GetAppearanceSystem().GetCurrentAppearanceData();
            Debug.Log($"[PlayerInventory] Captured appearance data for recruited settler '{settlerName}'");
        }
        else
        {
            Debug.LogWarning($"[PlayerInventory] Could not capture appearance data for '{settlerName}' - no appearance system");
            appearanceData = new NPCAppearanceData(); // Empty appearance data as fallback
        }

        Debug.Log($"[PlayerInventory] Adding '{settlerName}' to recruited NPCs list...");
        
        // Create and add the recruited NPC data
        var recruitedData = new RecruitedNPCData(settlerData, componentId, appearanceData);
        recruitedNPCs.Add(recruitedData);
        
        if (!string.IsNullOrEmpty(componentId))
        {
            Debug.Log($"[PlayerInventory] Tracked recruitment by component ID: {componentId}");
        }
        
        Debug.Log($"[PlayerInventory] Invoking OnNPCRecruited event for '{settlerName}'...");
        OnNPCRecruited?.Invoke(settlerData);
        
        Debug.Log($"[PlayerInventory] Successfully recruited settler '{settlerName}' - will be transferred to camp when returning");
        Debug.Log($"[PlayerInventory] New recruited NPCs count: {recruitedNPCs.Count}");
        
        // Show recruitment popup 
        Debug.Log($"[PlayerInventory] Attempting to show recruitment popup for '{settlerName}'...");
        if (PlayerUIManager.Instance?.inventoryPopup != null)
        {
            Debug.Log("[PlayerInventory] PlayerUIManager and inventoryPopup found, showing recruitment popup...");
            // We'll need to create a way to show settler recruitment in the UI
            // For now, just show a simple notification
            Debug.Log($"[PlayerInventory] Recruited {settlerName} (Age {settlerData.age})!");
        }
        else
        {
            Debug.LogWarning("[PlayerInventory] PlayerUIManager or inventoryPopup is null, cannot show recruitment popup!");
        }
    }

    /// <summary>
    /// Get all currently recruited settlers
    /// </summary>
    public List<Managers.SettlerData> GetRecruitedSettlers()
    {
        return recruitedNPCs.ConvertAll(data => data.settlerData);
    }

    /// <summary>
    /// Check if any NPCs are ready to be transferred to camp
    /// </summary>
    public bool HasRecruitedNPCs()
    {
        return recruitedNPCs.Count > 0;
    }

    /// <summary>
    /// Check if a specific component has been recruited
    /// </summary>
    public bool IsComponentRecruited(string componentId)
    {
        if (string.IsNullOrEmpty(componentId))
            return false;
            
        return recruitedNPCs.Exists(data => data.componentId == componentId);
    }

    /// <summary>
    /// Restore a recruited NPC from save data (used by SaveLoadManager)
    /// </summary>
    public void RestoreRecruitedNPC(RecruitedNPCData recruitedNPCData)
    {
        if (recruitedNPCData == null)
        {
            Debug.LogWarning("[PlayerInventory] Cannot restore null recruited NPC data");
            return;
        }

        // Check if this NPC is already in the list
        if (!string.IsNullOrEmpty(recruitedNPCData.componentId) && 
            recruitedNPCs.Exists(data => data.componentId == recruitedNPCData.componentId))
        {
            Debug.LogWarning($"[PlayerInventory] NPC with component ID '{recruitedNPCData.componentId}' is already recruited");
            return;
        }

        recruitedNPCs.Add(recruitedNPCData);
        Debug.Log($"[PlayerInventory] Restored recruited NPC from save data: {recruitedNPCData.settlerData.name}");
    }

    /// <summary>
    /// Get all recruited NPC data (for save system access)
    /// </summary>
    public List<RecruitedNPCData> GetRecruitedNPCData()
    {
        return new List<RecruitedNPCData>(recruitedNPCs);
    }

    /// <summary>
    /// Transfer all recruited settlers to the camp (clears temporary recruitment storage)
    /// </summary>
    public void TransferRecruitedNPCsToCamp()
    {
        if (recruitedNPCs.Count == 0)
        {
            Debug.Log("[PlayerInventory] No recruited settlers to transfer to camp");
            return;
        }

        var settlersToTransfer = new List<Managers.SettlerData>();
        
        foreach (var recruitedData in recruitedNPCs)
        {
            settlersToTransfer.Add(recruitedData.settlerData);
            Debug.Log($"[PlayerInventory] Transferring recruited settler '{recruitedData.settlerData.name}' to camp");
            SpawnSettlerInCamp(recruitedData.settlerData, recruitedData.appearanceData);
        }

        // Fire the event to notify other systems about the transfer
        OnNPCsTransferredToCamp?.Invoke(settlersToTransfer);

        recruitedNPCs.Clear();
        
        Debug.Log("[PlayerInventory] All recruited settlers transferred to camp and cleared from inventory");
    }

    /// <summary>
    /// Spawn a recruited settler in the camp
    /// </summary>
    private void SpawnSettlerInCamp(Managers.SettlerData settlerData, NPCAppearanceData appearanceData = null)
    {
        // Get the settler prefab from NPCManager
        if (NPCManager.Instance == null || !NPCManager.Instance.IsSettlerGenerationConfigured())
        {
            Debug.LogError($"[PlayerInventory] NPCManager not configured for settler generation. Cannot spawn settler '{settlerData.name}'");
            return;
        }

        GameObject settlerPrefab = NPCManager.Instance.GetSettlerPrefab();
        if (settlerPrefab == null)
        {
            Debug.LogError($"[PlayerInventory] No settler prefab available from NPCManager!");
            return;
        }

        // Find a suitable spawn location in the camp
        Vector3 spawnPosition = FindCampSpawnPosition();
        
        // Instantiate the settler prefab
        GameObject settlerObject = Instantiate(settlerPrefab, spawnPosition, Quaternion.identity);
        
        // Set up the settler
        if (settlerObject.TryGetComponent<SettlerNPC>(out var settlerNPC))
        {
            // Apply settler data first
            settlerNPC.ApplySettlerData(settlerData);
            
            // Set the initialization context for recruited settlers before Start() is called
            settlerNPC.SetInitializationContext(NPCInitializationContext.RECRUITED);
            
            // Set the recruited appearance data if available
            if (appearanceData != null)
            {
                settlerNPC.SetRecruitedAppearanceData(appearanceData);
                Debug.Log($"[PlayerInventory] Set recruited appearance data for '{settlerData.name}'");
            }
            else
            {
                Debug.LogWarning($"[PlayerInventory] No appearance data available for recruited settler '{settlerData.name}' - will use random appearance");
            }
            
            Debug.Log($"[PlayerInventory] Successfully spawned recruited settler '{settlerData.name}' (Age {settlerData.age}) in camp at {spawnPosition}");
        }
        else
        {
            Debug.LogWarning($"[PlayerInventory] Spawned settler prefab does not have SettlerNPC component!");
        }
    }

    /// <summary>
    /// Find a suitable spawn position in the camp for a new NPC
    /// </summary>
    private Vector3 FindCampSpawnPosition()
    {
        // Try to find CampManager for spawn bounds
        if (CampManager.Instance != null)
        {
            // Use camp bounds to find a suitable spawn location
            Vector2 xBounds = CampManager.Instance.SharedXBounds;
            Vector2 zBounds = CampManager.Instance.SharedZBounds;
            
            Debug.Log($"[PlayerInventory] Camp bounds - X: {xBounds}, Z: {zBounds}");
            
            // Try to find a clear position within camp bounds
            for (int attempts = 0; attempts < 10; attempts++)
            {
                Vector3 randomPosition = new Vector3(
                    UnityEngine.Random.Range(xBounds.x, xBounds.y),
                    0,
                    UnityEngine.Random.Range(zBounds.x, zBounds.y)
                );
                
                // Simple ground raycast to place on terrain
                if (Physics.Raycast(randomPosition + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
                {
                    Debug.Log($"[PlayerInventory] Found spawn position: {hit.point}");
                    return hit.point;
                }
            }
            
            // Fallback to center of camp if no clear position found
            Vector3 fallbackPosition = new Vector3((xBounds.x + xBounds.y) / 2f, 0f, (zBounds.x + zBounds.y) / 2f);
            Debug.LogWarning($"[PlayerInventory] No clear spawn position found after 10 attempts, using fallback: {fallbackPosition}");
            return fallbackPosition;
        }
        
        // Ultimate fallback to origin
        Debug.LogWarning("[PlayerInventory] CampManager not found, spawning NPC at origin");
        return Vector3.zero;
    }

    /// <summary>
    /// Remove a recruited settler (in case of cancellation or other reasons)
    /// </summary>
    public void RemoveRecruitedNPC(string componentId)
    {
        var dataToRemove = recruitedNPCs.Find(data => data.componentId == componentId);
        if (dataToRemove != null)
        {
            recruitedNPCs.Remove(dataToRemove);
            Debug.Log($"[PlayerInventory] Removed recruited settler '{dataToRemove.settlerData.name}' from temporary storage");
        }
    }

    #endregion
}

/// <summary>
/// Data class to store all information about a recruited procedural settler
/// </summary>
[System.Serializable]
public class RecruitedNPCData
{
    public Managers.SettlerData settlerData;
    public string componentId;
    public NPCAppearanceData appearanceData;
    
    public RecruitedNPCData(Managers.SettlerData settler, string id, NPCAppearanceData appearance)
    {
        settlerData = settler;
        componentId = id;
        appearanceData = appearance;
    }
}
