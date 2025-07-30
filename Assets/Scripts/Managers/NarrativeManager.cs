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
    [SerializeField] private NarrativeAssetMapping[] narrativeMappings;

    // Cache for loaded dialogues
    private Dictionary<NPCNarrativeType, DialogueData> loadedDialogues = new Dictionary<NPCNarrativeType, DialogueData>();
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
        LoadDialogueForType(NPCNarrativeType.FRIENDLY_SETTLER);
        LoadDialogueForType(NPCNarrativeType.RECRUITMENT_DIALOGUE);
    }

    /// <summary>
    /// Start a conversation with dynamic dialogue loading based on NPCNarrativeType
    /// Called by NarrativeInteractive components
    /// </summary>
    public void StartConversation(NPCNarrativeType narrativeType, INarrativeTarget conversationTarget = null, NarrativeInteractive sourceComponent = null)
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

        // Load dialogue for the specified type
        DialogueData dialogue = LoadDialogueForType(narrativeType);
        
        if (dialogue == null)
        {
            Debug.LogError($"[NarrativeManager] Failed to load dialogue for type: {narrativeType}");
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
    /// Load dialogue for a specific NPCNarrativeType
    /// </summary>
    private DialogueData LoadDialogueForType(NPCNarrativeType narrativeType)
    {
        // Check cache first
        if (loadedDialogues.TryGetValue(narrativeType, out DialogueData cachedDialogue))
        {
            return cachedDialogue;
        }

        // Find the appropriate dialogue file
        TextAsset dialogueFile = GetDialogueFileForType(narrativeType);
        
        if (dialogueFile != null)
        {
            DialogueData dialogue = LoadDialogueFromAsset(dialogueFile);
            if (dialogue != null)
            {
                // Cache the loaded dialogue
                loadedDialogues[narrativeType] = dialogue;
                return dialogue;
            }
        }

        Debug.LogWarning($"[NarrativeManager] No dialogue file found for type: {narrativeType}");
        return null;
    }

    /// <summary>
    /// Get the appropriate dialogue file for a NPCNarrativeType
    /// </summary>
    private TextAsset GetDialogueFileForType(NPCNarrativeType narrativeType)
    {
        // Check configured mappings first
        foreach (var mapping in narrativeMappings)
        {
            if (mapping.narrativeType == narrativeType && mapping.dialogueFile != null)
            {
                return mapping.dialogueFile;
            }
        }

        // Fallback to default dialogues based on type
        return GetDefaultDialogueForType(narrativeType);
    }

    /// <summary>
    /// Get default dialogue files for narrative types (fallback system)
    /// </summary>
    private TextAsset GetDefaultDialogueForType(NPCNarrativeType narrativeType)
    {
        string fileName = "";
        
        switch (narrativeType)
        {
            case NPCNarrativeType.FRIENDLY_SETTLER:
            case NPCNarrativeType.RECRUITMENT_DIALOGUE:
                fileName = "RecruitmentExample";
                break;
            case NPCNarrativeType.GENERIC_CONVERSATION:
                fileName = "_TestDialogue";
                break;
            default:
                Debug.LogWarning($"[NarrativeManager] No default dialogue for type: {narrativeType}");
                return null;
        }

        // Try to load from Resources or specific path
        return Resources.Load<TextAsset>($"Dialogue/Camp/{fileName}");
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
            // Pass the component ID and SettlerNPC reference to enable appearance data capture
            string componentId = currentNarrativeComponent?.GetInstanceID().ToString();
            PlayerInventory.Instance.RecruitNPC(settlerNPC, componentId);
            
            // Set recruitment success flag on the component
            SetFlag("recruited");
            SetFlag($"recruited_{npcName.Replace(" ", "_").ToLower()}");
            
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
}

/// <summary>
/// Mapping configuration for NPCNarrativeType to dialogue files
/// </summary>
[System.Serializable]
public class NarrativeAssetMapping
{
    public NPCNarrativeType narrativeType;
    public TextAsset dialogueFile;
    [TextArea(2, 4)]
    public string description; // For editor documentation
}
