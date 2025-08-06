using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Managers;
using System.Linq;

/// <summary>
/// Central manager for all narrative interactions in the game.
/// Handles dynamic dialogue loading, conversation flow, and NPC recruitment.
/// Delegates progression tracking to individual NarrativeInteractive components.
/// </summary>
public class NarrativeManager : MonoBehaviour
{
    #region Singleton
    private static NarrativeManager _instance;
    public static NarrativeManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<NarrativeManager>();
                if (_instance == null)
                {
                    Debug.LogWarning("NarrativeManager instance not found in the scene!");
                }
            }
            return _instance;
        }
    }
    #endregion

    [Header("Narrative Configuration")]
    [SerializeField] private CharacterDialogueMapping[] characterDialogueMappings;

    // Cache for loaded dialogues - now supports multiple dialogues per character type
    private Dictionary<CharacterType, List<DialogueData>> loadedDialogues = new Dictionary<CharacterType, List<DialogueData>>();
    private Dictionary<string, DialogueLine> currentDialogueLinesMap;
    private INarrativeTarget currentConversationTarget;
    private DialogueData currentDialogue;
    
    // Current conversation context
    private NarrativeInteractive currentNarrativeComponent; // The component we're currently talking to

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeNarrativeSystem();
    }

    private void InitializeNarrativeSystem()
    {
        // Find narrative menu if not assigned
        if (PlayerUIManager.Instance.narrativeMenu == null)
        {
            PlayerUIManager.Instance.narrativeMenu = FindFirstObjectByType<NarrativeMenu>();
        }

        // Pre-load critical dialogues if needed
        PreloadCriticalDialogues();
    }

    private void PreloadCriticalDialogues()
    {
        // Pre-load commonly used dialogues for better performance
        LoadDialoguesForCharacterType(CharacterType.HUMAN_MALE_1);
        LoadDialoguesForCharacterType(CharacterType.HUMAN_FEMALE_1);
    }

    /// <summary>
    /// Start a conversation with dynamic dialogue loading based on CharacterType
    /// Called by NarrativeInteractive components
    /// </summary>
    public void StartConversation(CharacterType characterType, INarrativeTarget conversationTarget = null, NarrativeInteractive sourceComponent = null)
    {
        // Set the conversation target
        currentConversationTarget = conversationTarget ?? FindConversationTarget();
        
        if (currentConversationTarget == null)
        {
            Debug.LogWarning("[NarrativeManager] No conversation target found!");
            return;
        }

        // Set the source component for progression tracking
        currentNarrativeComponent = sourceComponent;
        
        if (currentNarrativeComponent == null)
        {
            // Try to find the narrative component
            var narrativeComponent = currentConversationTarget as MonoBehaviour;
            currentNarrativeComponent = narrativeComponent?.GetComponent<NarrativeInteractive>();
            
            if (currentNarrativeComponent == null)
            {
                Debug.LogWarning("[NarrativeManager] No NarrativeInteractive component found - progression tracking disabled!");
            }
        }

        // Load a dialogue for the specified character type, considering the narrative component's flags
        DialogueData dialogue = GetRandomDialogueForCharacterType(characterType, currentNarrativeComponent);
        
        if (dialogue == null)
        {
            Debug.LogError($"[NarrativeManager] Failed to load dialogue for character type: {characterType}");
            return;
        }

        StartConversationWithDialogue(dialogue);
    }

    /// <summary>
    /// Start a conversation with a specific narrative asset (legacy support)
    /// </summary>
    public void StartConversation(NarrativeAsset narrativeAsset, NarrativeInteractive sourceComponent = null)
    {
        currentConversationTarget = FindConversationTarget();
        
        if (narrativeAsset?.dialogueFile != null)
        {
            DialogueData dialogue = LoadDialogueFromAsset(narrativeAsset.dialogueFile);
            if (dialogue != null)
            {
                // Set the source component for progression tracking
                currentNarrativeComponent = sourceComponent;
                
                StartConversationWithDialogue(dialogue);
            }
        }
        else
        {
            Debug.LogError("[NarrativeManager] NarrativeAsset or dialogue file is null!");
        }
    }

    private void StartConversationWithDialogue(DialogueData dialogue)
    {
        currentDialogue = dialogue;
        BuildDialogueLinesMap();

        if (currentDialogue?.lines != null && currentDialogue.lines.Count > 0)
        {
            // Pause the conversation target
            currentConversationTarget?.PauseForConversation();
            
            // Update player controls
            PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.IN_CONVERSATION);
            
            // Find the appropriate starting line based on component's progression flags
            DialogueLine startingLine = GetStartingDialogueLine();
            
            // Show the conversation UI
            if (PlayerUIManager.Instance.narrativeMenu != null)
            {
                PlayerUIManager.Instance.narrativeMenu.gameObject.SetActive(true);
                PlayerUIManager.Instance.narrativeMenu.DisplayDialogue(startingLine);
            }
            else
            {
                Debug.LogError("[NarrativeManager] NarrativeMenu is null!");
            }
        }
        else
        {
            Debug.LogWarning("[NarrativeManager] Dialogue has no lines!");
        }
    }

    /// <summary>
    /// Get the appropriate starting dialogue line based on component's progression flags
    /// </summary>
    private DialogueLine GetStartingDialogueLine()
    {
        // Check for conditional starts first
        if (currentDialogue.conditionalStarts != null && currentDialogue.conditionalStarts.Count > 0)
        {
            var validStarts = currentDialogue.conditionalStarts
                .Where(cs => CheckConditions(cs.requiredFlags, cs.blockedByFlags))
                .OrderByDescending(cs => cs.priority)
                .ToList();

            if (validStarts.Count > 0)
            {
                var chosenStart = validStarts.First();
                if (currentDialogueLinesMap.TryGetValue(chosenStart.lineId, out DialogueLine conditionalLine))
                {
                    return conditionalLine;
                }
            }
        }

        // Fall back to default starting line
        string startLineId = !string.IsNullOrEmpty(currentDialogue.startingLineId) ? currentDialogue.startingLineId : "Start";
        
        if (currentDialogueLinesMap.TryGetValue(startLineId, out DialogueLine defaultLine))
        {
            return defaultLine;
        }

        // Ultimate fallback: return first line
        return currentDialogue.lines[0];
    }

    /// <summary>
    /// Load all dialogues for a specific CharacterType
    /// </summary>
    private void LoadDialoguesForCharacterType(CharacterType characterType)
    {
        // Check if already loaded
        if (loadedDialogues.ContainsKey(characterType))
        {
            return;
        }

        List<DialogueData> dialogues = new List<DialogueData>();
        List<TextAsset> dialogueFiles = GetDialogueFilesForCharacterType(characterType);
        
        foreach (var dialogueFile in dialogueFiles)
        {
            if (dialogueFile != null)
            {
                DialogueData dialogue = LoadDialogueFromAsset(dialogueFile);
                if (dialogue != null)
                {
                    dialogues.Add(dialogue);
                }
            }
        }

        if (dialogues.Count > 0)
        {
            // Cache the loaded dialogues
            loadedDialogues[characterType] = dialogues;
        }
        else
        {
            Debug.LogWarning($"[NarrativeManager] No dialogue files found for character type: {characterType}");
        }
    }

    /// <summary>
    /// Get a random dialogue for a specific CharacterType
    /// </summary>
    private DialogueData GetRandomDialogueForCharacterType(CharacterType characterType, NarrativeInteractive narrativeComponent = null)
    {
        // Load dialogues if not already cached
        LoadDialoguesForCharacterType(characterType);
        
        if (loadedDialogues.TryGetValue(characterType, out List<DialogueData> dialogues) && dialogues.Count > 0)
        {
            // Check if we have a narrative component to check flags
            if (narrativeComponent != null)
            {
                // Try to find a dialogue that has appropriate conditional starts for the current state
                var prioritizedDialogues = new List<DialogueData>();
                var fallbackDialogues = new List<DialogueData>();
                
                foreach (var dialogue in dialogues)
                {
                    if (dialogue.conditionalStarts != null && dialogue.conditionalStarts.Count > 0)
                    {
                        // Check if this dialogue has conditional starts that match current flags
                        var validStarts = dialogue.conditionalStarts
                            .Where(cs => CheckConditionsWithComponent(cs.requiredFlags, cs.blockedByFlags, narrativeComponent))
                            .OrderByDescending(cs => cs.priority)
                            .ToList();
                            
                        if (validStarts.Count > 0)
                        {
                            prioritizedDialogues.Add(dialogue);
                        }
                        else
                        {
                            fallbackDialogues.Add(dialogue);
                        }
                    }
                    else
                    {
                        fallbackDialogues.Add(dialogue);
                    }
                }
                
                // Prefer dialogues with matching conditional starts
                if (prioritizedDialogues.Count > 0)
                {
                    int prioritizedIndex = Random.Range(0, prioritizedDialogues.Count);
                    return prioritizedDialogues[prioritizedIndex];
                }
                else if (fallbackDialogues.Count > 0)
                {
                    int fallbackIndex = Random.Range(0, fallbackDialogues.Count);
                    return fallbackDialogues[fallbackIndex];
                }
            }
            
            // Fallback to random selection if no component or no appropriate dialogues found
            int randomIndex = Random.Range(0, dialogues.Count);
            return dialogues[randomIndex];
        }

        Debug.LogWarning($"[NarrativeManager] No dialogues available for character type: {characterType}");
        return null;
    }

    /// <summary>
    /// Get all dialogue files for a specific CharacterType
    /// </summary>
    private List<TextAsset> GetDialogueFilesForCharacterType(CharacterType characterType)
    {
        List<TextAsset> dialogueFiles = new List<TextAsset>();

        // Check configured mappings first
        foreach (var mapping in characterDialogueMappings)
        {
            if (mapping.characterType == characterType && mapping.dialogueFiles != null)
            {
                dialogueFiles.AddRange(mapping.dialogueFiles);
            }
        }

        // If no configured mappings, try to load default dialogues
        if (dialogueFiles.Count == 0)
        {
            dialogueFiles.AddRange(GetDefaultDialogueFilesForCharacterType(characterType));
        }

        return dialogueFiles;
    }

    /// <summary>
    /// Get default dialogue files for character types using dynamic file discovery
    /// </summary>
    private List<TextAsset> GetDefaultDialogueFilesForCharacterType(CharacterType characterType)
    {
        List<TextAsset> dialogueFiles = new List<TextAsset>();
        
        // Convert character type to search pattern
        string searchPattern = GetCharacterTypeSearchPattern(characterType);
        
        if (string.IsNullOrEmpty(searchPattern))
        {
            Debug.LogWarning($"[NarrativeManager] No search pattern for character type: {characterType}");
            // Fallback to generic dialogue
            TextAsset fallbackAsset = Resources.Load<TextAsset>("Dialogue/Camp/_TestDialogue");
            if (fallbackAsset != null)
            {
                dialogueFiles.Add(fallbackAsset);
            }
            return dialogueFiles;
        }
        
        // Load all TextAssets from the Dialogue/Camp folder
        TextAsset[] allDialogueAssets = Resources.LoadAll<TextAsset>("Dialogue/Camp");
        
        // Filter assets that match the character type pattern
        foreach (TextAsset asset in allDialogueAssets)
        {
            if (asset.name.StartsWith(searchPattern, System.StringComparison.OrdinalIgnoreCase))
            {
                dialogueFiles.Add(asset);
                Debug.Log($"[NarrativeManager] Found dialogue file for {characterType}: {asset.name}");
            }
        }
        
        // If no files found, try fallback patterns
        if (dialogueFiles.Count == 0)
        {
            dialogueFiles.AddRange(GetFallbackDialogueFiles(characterType));
        }
        
        Debug.Log($"[NarrativeManager] Loaded {dialogueFiles.Count} dialogue files for {characterType}");
        return dialogueFiles;
    }
    
    /// <summary>
    /// Convert character type to file search pattern
    /// </summary>
    private string GetCharacterTypeSearchPattern(CharacterType characterType)
    {
        switch (characterType)
        {
            case CharacterType.HUMAN_MALE_1:
                return "HumanMale1";
            case CharacterType.HUMAN_FEMALE_1:
                return "HumanFemale1";
            case CharacterType.HUMAN_MALE_2:
                return "HumanMale2";
            case CharacterType.HUMAN_FEMALE_2:
                return "HumanFemale2";
            case CharacterType.MACHINE_ROBOT:
                return "MachineRobot";
            case CharacterType.MACHINE_DRONE:
                return "MachineDrone";
            case CharacterType.ZOMBIE_MELEE:
                return "ZombieMelee";
            case CharacterType.ZOMBIE_SPITTER:
                return "ZombieSpitter";
            case CharacterType.ZOMBIE_TANK:
                return "ZombieTank";
            default:
                return null;
        }
    }
    
    /// <summary>
    /// Get fallback dialogue files when no character-specific files are found
    /// </summary>
    private List<TextAsset> GetFallbackDialogueFiles(CharacterType characterType)
    {
        List<TextAsset> fallbackFiles = new List<TextAsset>();
        
        // Try generic fallbacks based on character type
        string[] fallbackPatterns = GetFallbackPatterns(characterType);
        
        TextAsset[] allDialogueAssets = Resources.LoadAll<TextAsset>("Dialogue/Camp");
        
        foreach (string pattern in fallbackPatterns)
        {
            foreach (TextAsset asset in allDialogueAssets)
            {
                if (asset.name.StartsWith(pattern, System.StringComparison.OrdinalIgnoreCase))
                {
                    fallbackFiles.Add(asset);
                    Debug.Log($"[NarrativeManager] Found fallback dialogue file for {characterType}: {asset.name}");
                }
            }
            
            // Stop at first pattern that finds files
            if (fallbackFiles.Count > 0)
            {
                break;
            }
        }
        
        // Ultimate fallback to test dialogue
        if (fallbackFiles.Count == 0)
        {
            TextAsset testAsset = Resources.Load<TextAsset>("Dialogue/Camp/_TestDialogue");
            if (testAsset != null)
            {
                fallbackFiles.Add(testAsset);
                Debug.Log($"[NarrativeManager] Using ultimate fallback dialogue for {characterType}: {testAsset.name}");
            }
        }
        
        return fallbackFiles;
    }
    
    /// <summary>
    /// Get fallback search patterns for character types
    /// </summary>
    private string[] GetFallbackPatterns(CharacterType characterType)
    {
        switch (characterType)
        {
            case CharacterType.HUMAN_MALE_1:
            case CharacterType.HUMAN_MALE_2:
                return new string[] { "HumanMale", "Human", "Recruitment" };
            case CharacterType.HUMAN_FEMALE_1:
            case CharacterType.HUMAN_FEMALE_2:
                return new string[] { "HumanFemale", "Human", "Recruitment" };
            case CharacterType.MACHINE_ROBOT:
            case CharacterType.MACHINE_DRONE:
                return new string[] { "Machine", "Robot", "Mechanical" };
            case CharacterType.ZOMBIE_MELEE:
            case CharacterType.ZOMBIE_SPITTER:
            case CharacterType.ZOMBIE_TANK:
                return new string[] { "Zombie", "Undead" };
            default:
                return new string[] { "Recruitment", "Generic" };
        }
    }

    /// <summary>
    /// Load dialogue from a TextAsset
    /// </summary>
    private DialogueData LoadDialogueFromAsset(TextAsset dialogueFile)
    {
        if (dialogueFile == null) return null;

        try
        {
            string json = dialogueFile.text;
            DialogueData dialogue = JsonUtility.FromJson<DialogueData>(json);
            
            if (dialogue?.lines == null)
            {
                Debug.LogError($"[NarrativeManager] Failed to parse dialogue from {dialogueFile.name}");
                return null;
            }

            return dialogue;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[NarrativeManager] Error loading dialogue from {dialogueFile.name}: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Build the dialogue lines map for quick lookup
    /// </summary>
    private void BuildDialogueLinesMap()
    {
        currentDialogueLinesMap = new Dictionary<string, DialogueLine>();
        
        if (currentDialogue?.lines != null)
        {
            foreach (var line in currentDialogue.lines)
            {
                currentDialogueLinesMap[line.id] = line;
            }
        }
    }

    /// <summary>
    /// Handle option selection in dialogue
    /// </summary>
    public void HandleOptionSelected(DialogueOption option)
    {
        // Process inventory consumption if specified
        ConsumeInventoryItems(option);
        
        // Process flags for the selected option
        ProcessFlags(option.setFlags, option.removeFlags);

        // Handle NPC recruitment if specified
        if (!string.IsNullOrEmpty(option.recruitNPC))
        {
            RecruitNPC(option.recruitNPC);
        }

        // Continue to next dialogue line
        HandleOptionSelected(option.nextLine);
    }

    /// <summary>
    /// Handle option selection by next line ID
    /// </summary>
    public void HandleOptionSelected(string nextLineId)
    {
        if (!string.IsNullOrEmpty(nextLineId) && 
            currentDialogueLinesMap != null && 
            currentDialogueLinesMap.TryGetValue(nextLineId, out DialogueLine nextLine))
        {
            // Check if the line can be accessed based on component's flags
            if (CheckConditions(nextLine.requiredFlags, nextLine.blockedByFlags))
            {
                // Process flags for the line
                ProcessFlags(nextLine.setFlags, nextLine.removeFlags);
                
                if (PlayerUIManager.Instance.narrativeMenu != null)
                {
                    PlayerUIManager.Instance.narrativeMenu.DisplayDialogue(nextLine);
                }
            }
            else
            {
                EndConversation();
            }
        }
        else
        {
            EndConversation();
        }
    }

    /// <summary>
    /// End the current conversation
    /// </summary>
    public void EndConversation()
    {
        // Resume the conversation target
        currentConversationTarget?.ResumeAfterConversation();
        currentConversationTarget = null;

        // Update player controls
        PlayerInput.Instance.UpdatePlayerControls(GameManager.Instance.PlayerGameControlType());

        // Hide the conversation UI
        if (PlayerUIManager.Instance.narrativeMenu != null)
        {
            PlayerUIManager.Instance.narrativeMenu.gameObject.SetActive(false);
        }

        // Clear current dialogue data
        currentDialogue = null;
        currentDialogueLinesMap = null;
        currentNarrativeComponent = null;
    }

    /// <summary>
    /// Recruit an NPC through the dialogue system
    /// </summary>
    private void RecruitNPC(string npcName)
    {
        if (string.IsNullOrEmpty(npcName))
        {
            Debug.LogWarning("[NarrativeManager] Cannot recruit NPC with empty name!");
            return;
        }

        if (PlayerInventory.Instance == null)
        {
            Debug.LogWarning("[NarrativeManager] PlayerInventory not found!");
            return;
        }

        // Recruit the procedural settler directly
        if (currentConversationTarget is SettlerNPC settlerNPC)
        {
            // Set recruitment success flag on the component BEFORE recruitment to ensure it gets captured
            SetFlag("recruited");
            SetFlag($"recruited_{npcName.Replace(" ", "_").ToLower()}");
            
            // Pass the component ID and SettlerNPC reference to enable appearance data capture
            string componentId = currentNarrativeComponent?.GetInstanceID().ToString();
            PlayerInventory.Instance.RecruitNPC(settlerNPC, componentId);
            
            Debug.Log($"[NarrativeManager] Successfully recruited NPC: {npcName}");
        }
        else
        {
            // Set recruitment failed flag on the component
            SetFlag("recruitment_failed");
            Debug.LogError($"[NarrativeManager] Failed to recruit NPC '{npcName}' - could not get NPCScriptableObj from conversation target");
        }
    }

    /// <summary>
    /// Find the current conversation target using player interaction detection
    /// </summary>
    private INarrativeTarget FindConversationTarget()
    {
        if (PlayerInventory.Instance == null || PlayerController.Instance?._possessedNPC == null)
        {
            return null;
        }

        // Use the same detection logic as PlayerInventory
        RaycastHit hit;
        Vector3 startPos = PlayerController.Instance._possessedNPC.GetTransform().position + Vector3.up;
        Vector3 direction = PlayerController.Instance._possessedNPC.GetTransform().forward;
        Vector3 boxCastSize = new Vector3(0.5f, 0.5f, 0.5f);
        float interactionRange = 3f;
        
        if (Physics.BoxCast(startPos, boxCastSize * 0.5f, direction, out hit, 
            PlayerController.Instance._possessedNPC.GetTransform().rotation, interactionRange))
        {
            return hit.collider.GetComponent<INarrativeTarget>();
        }

        return null;
    }

    #region NarrativeAsset Flag Management (Delegates to NarrativeInteractive)

    /// <summary>
    /// Check if conditions are met based on the current narrative asset's flags
    /// </summary>
    private bool CheckConditions(List<string> requiredFlags, List<string> blockedByFlags)
    {
        if (currentNarrativeComponent == null)
        {
            // If no component, assume conditions are met (fallback behavior)
            return true;
        }

        return currentNarrativeComponent.CheckConditions(requiredFlags, blockedByFlags);
    }

    /// <summary>
    /// Check if conditions are met based on a specific narrative component's flags
    /// </summary>
    private bool CheckConditionsWithComponent(List<string> requiredFlags, List<string> blockedByFlags, NarrativeInteractive component)
    {
        if (component == null)
        {
            // If no component, assume conditions are met (fallback behavior)
            return true;
        }

        return component.CheckConditions(requiredFlags, blockedByFlags);
    }

    /// <summary>
    /// Process flag operations on the current narrative asset
    /// </summary>
    private void ProcessFlags(List<string> setFlags, List<string> removeFlags)
    {
        if (currentNarrativeComponent == null)
            return;

        currentNarrativeComponent.ProcessFlags(setFlags, removeFlags);
    }

    /// <summary>
    /// Set a flag on the current narrative asset
    /// </summary>
    public void SetFlag(string flagName, string value = "true")
    {
        if (currentNarrativeComponent == null)
        {
            Debug.LogWarning($"[NarrativeManager] Cannot set flag '{flagName}' - no current narrative component!");
            return;
        }

        currentNarrativeComponent.SetFlag(flagName, value);
    }

    /// <summary>
    /// Check if the current narrative asset has a flag
    /// </summary>
    public bool HasFlag(string flagName)
    {
        if (currentNarrativeComponent == null)
            return false;

        return currentNarrativeComponent.HasFlag(flagName);
    }

    #endregion

    /// <summary>
    /// Get the display name of the current conversation target
    /// </summary>
    public string GetCurrentConversationTargetName()
    {
        if (currentConversationTarget == null)
            return "Unknown NPC";

        // Handle SettlerNPC specifically
        if (currentConversationTarget is SettlerNPC settlerNPC)
        {
            string settlerName = settlerNPC.GetSettlerName();
            // Double-check that we got a valid name
            if (!string.IsNullOrEmpty(settlerName) && settlerName != "Unknown Settler")
            {
                return settlerName;
            }
        }

        // For other NPC types or if SettlerNPC name is not available, 
        // try to get the GameObject name as fallback
        var transform = currentConversationTarget.GetTransform();
        if (transform != null)
        {
            // Clean up the GameObject name for display (remove "Settler_" prefix if present)
            string gameObjectName = transform.name;
            if (gameObjectName.StartsWith("Settler_"))
            {
                return gameObjectName.Substring(8); // Remove "Settler_" prefix
            }
            return gameObjectName;
        }

        return "Unknown NPC";
    }

    #region Inventory Checking

    /// <summary>
    /// Check if the player has the required inventory items for a dialogue option
    /// </summary>
    public bool CheckInventoryRequirements(DialogueOption option)
    {
        if (option == null)
        {
            Debug.Log("[NarrativeManager] CheckInventoryRequirements: option is null, returning true");
            return true;
        }

        // Check legacy requiredItem field for backwards compatibility
        if (!string.IsNullOrEmpty(option.requiredItem))
        {
            if (!PlayerInventory.Instance.HasItemByName(option.requiredItem))
            {
                Debug.Log($"[NarrativeManager] Missing legacy item: {option.requiredItem}");
                return false;
            }
        }

        // Check new inventory requirements system
        if (option.requiredInventoryItems != null && option.requiredInventoryItems.Count > 0)
        {
            bool result = CheckInventoryRequirementsList(option.requiredInventoryItems);
            if (!result)
            {
                Debug.Log($"[NarrativeManager] Failed inventory requirements check for option: '{option.text}'");
            }
            return result;
        }
        return true;
    }

    /// <summary>
    /// Check if the player has all items in the requirements list
    /// </summary>
    private bool CheckInventoryRequirementsList(List<InventoryRequirement> requirements)
    {
        if (PlayerInventory.Instance == null)
        {
            Debug.LogWarning("[NarrativeManager] PlayerInventory not found for inventory check!");
            return false;
        }

        // Log current inventory state for debugging
        LogCurrentInventory();

        foreach (var requirement in requirements)
        {
            if (string.IsNullOrEmpty(requirement.itemName))
                continue;

            // Check if player has the item by name
            if (!PlayerInventory.Instance.HasItemByName(requirement.itemName))
            {
                Debug.Log($"[NarrativeManager] Missing item: {requirement.itemName}");
                return false;
            }

            // Check quantity if specified
            if (requirement.requiredQuantity > 1)
            {
                var playerInventory = PlayerInventory.Instance.GetFullInventory();
                var matchingItem = playerInventory.Find(item => 
                    item.resourceScriptableObj != null && 
                    item.resourceScriptableObj.objectName == requirement.itemName);

                if (matchingItem == null || matchingItem.count < requirement.requiredQuantity)
                {
                    Debug.Log($"[NarrativeManager] Insufficient {requirement.itemName}: has {matchingItem?.count ?? 0}, needs {requirement.requiredQuantity}");
                    return false;
                }
            }
        }
        return true;
    }

    /// <summary>
    /// Log the current player inventory state for debugging
    /// </summary>
    private void LogCurrentInventory()
    {
        if (PlayerInventory.Instance == null)
        {
            Debug.LogWarning("[NarrativeManager] PlayerInventory is null, cannot log inventory");
            return;
        }

        var inventory = PlayerInventory.Instance.GetFullInventory();
        Debug.Log($"[NarrativeManager] Current inventory state ({inventory.Count} items):");
        
        if (inventory.Count == 0)
        {
            Debug.Log("[NarrativeManager] Inventory is empty");
            return;
        }

        foreach (var item in inventory)
        {
            if (item.resourceScriptableObj != null)
            {
                Debug.Log($"[NarrativeManager]   - {item.resourceScriptableObj.objectName}: {item.count}");
            }
            else
            {
                Debug.Log($"[NarrativeManager]   - [NULL RESOURCE]: {item.count}");
            }
        }
    }

    /// <summary>
    /// Consume inventory items when a dialogue option is selected (if specified)
    /// </summary>
    private void ConsumeInventoryItems(DialogueOption option)
    {
        if (option?.requiredInventoryItems == null || option.requiredInventoryItems.Count == 0)
            return;

        if (PlayerInventory.Instance == null)
        {
            Debug.LogWarning("[NarrativeManager] PlayerInventory not found for item consumption!");
            return;
        }

        foreach (var requirement in option.requiredInventoryItems)
        {
            if (!requirement.consumeOnUse || string.IsNullOrEmpty(requirement.itemName))
                continue;

            // Find the item in player inventory
            var playerInventory = PlayerInventory.Instance.GetFullInventory();
            var matchingItem = playerInventory.Find(item => 
                item.resourceScriptableObj != null && 
                item.resourceScriptableObj.objectName == requirement.itemName);

            if (matchingItem != null)
            {
                // Remove the required quantity
                PlayerInventory.Instance.RemoveItem(matchingItem.resourceScriptableObj, requirement.requiredQuantity);
                Debug.Log($"[NarrativeManager] Consumed {requirement.requiredQuantity}x {requirement.itemName} from player inventory");
            }
        }
    }

    /// <summary>
    /// Get a formatted display text for inventory requirements (for UI tooltips)
    /// </summary>
    public string GetInventoryRequirementDisplayText(DialogueOption option)
    {
        if (option?.requiredInventoryItems == null || option.requiredInventoryItems.Count == 0)
        {
            // Check legacy requiredItem field
            if (!string.IsNullOrEmpty(option.requiredItem))
            {
                return $"Requires: {option.requiredItem}";
            }
            return string.Empty;
        }

        var requirements = new List<string>();
        foreach (var requirement in option.requiredInventoryItems)
        {
            if (!string.IsNullOrEmpty(requirement.displayText))
            {
                requirements.Add(requirement.displayText);
            }
            else if (!string.IsNullOrEmpty(requirement.itemName))
            {
                if (requirement.requiredQuantity > 1)
                {
                    requirements.Add($"{requirement.requiredQuantity}x {requirement.itemName}");
                }
                else
                {
                    requirements.Add(requirement.itemName);
                }
            }
        }

        return requirements.Count > 0 ? $"Requires: {string.Join(", ", requirements)}" : string.Empty;
    }

    /// <summary>
    /// Debug method to add test items to player inventory (for testing dialogue requirements)
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void AddTestItemsForDialogue()
    {
        if (PlayerInventory.Instance == null)
        {
            Debug.LogWarning("[NarrativeManager] PlayerInventory not found for adding test items");
            return;
        }

        // Load the test resources
        var metalScrap = Resources.Load<ResourceScriptableObj>("Resources/MaterialResources/Metal Scrap");
        var wood = Resources.Load<ResourceScriptableObj>("Resources/MaterialResources/Wood");

        if (metalScrap != null)
        {
            PlayerInventory.Instance.AddItem(metalScrap, 5);
            Debug.Log("[NarrativeManager] Added 5 Metal Scrap to inventory for testing");
        }
        else
        {
            Debug.LogError("[NarrativeManager] Could not load Metal Scrap resource");
        }

        if (wood != null)
        {
            PlayerInventory.Instance.AddItem(wood, 10);
            Debug.Log("[NarrativeManager] Added 10 Wood to inventory for testing");
        }
        else
        {
            Debug.LogError("[NarrativeManager] Could not load Wood resource");
        }
    }

    #endregion
}

/// <summary>
/// Mapping configuration for CharacterType to multiple dialogue files
/// </summary>
[System.Serializable]
public class CharacterDialogueMapping
{
    public CharacterType characterType;
    public TextAsset[] dialogueFiles;
    [TextArea(2, 4)]
    public string description; // For editor documentation
}

