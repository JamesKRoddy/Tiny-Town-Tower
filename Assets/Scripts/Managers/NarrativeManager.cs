using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Managers;
using System.Linq;

/// <summary>
/// Central manager for all narrative interactions in the game.
/// Handles dynamic dialogue loading, conversation flow, NPC recruitment, and interaction tracking.
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
    
    [Header("Interaction Tracking")]
    [SerializeField] private bool persistInteractionFlags = true; // Save flags between sessions
    [SerializeField] private bool debugInteractionFlags = false; // Log flag operations for debugging

    // Cache for loaded dialogues
    private Dictionary<NPCNarrativeType, DialogueData> loadedDialogues = new Dictionary<NPCNarrativeType, DialogueData>();
    private Dictionary<string, DialogueLine> currentDialogueLinesMap;
    private INarrativeTarget currentConversationTarget;
    private DialogueData currentDialogue;
    
    // Interaction tracking
    private Dictionary<string, HashSet<string>> npcInteractionFlags = new Dictionary<string, HashSet<string>>();
    private string currentNPCId; // ID of the current NPC we're talking to

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadInteractionFlags();
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

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && persistInteractionFlags)
        {
            SaveInteractionFlags();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && persistInteractionFlags)
        {
            SaveInteractionFlags();
        }
    }

    private void OnDestroy()
    {
        if (persistInteractionFlags)
        {
            SaveInteractionFlags();
        }
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
    /// </summary>
    public void StartConversation(NPCNarrativeType narrativeType, INarrativeTarget conversationTarget = null)
    {
        // Set the conversation target
        currentConversationTarget = conversationTarget ?? FindConversationTarget();
        
        if (currentConversationTarget == null)
        {
            Debug.LogWarning("[NarrativeManager] No conversation target found!");
            return;
        }

        // Generate NPC ID for interaction tracking
        currentNPCId = GenerateNPCId(currentConversationTarget, narrativeType);
        
        // Load flags for this NPC
        LoadFlagsForNPC(currentNPCId);

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
    public void StartConversation(NarrativeAsset narrativeAsset)
    {
        currentConversationTarget = FindConversationTarget();
        
        if (narrativeAsset?.dialogueFile != null)
        {
            DialogueData dialogue = LoadDialogueFromAsset(narrativeAsset.dialogueFile);
            if (dialogue != null)
            {
                // Use dialogue file name as NPC ID for legacy support
                currentNPCId = narrativeAsset.dialogueFile.name;
                
                // Load flags for this NPC
                LoadFlagsForNPC(currentNPCId);
                
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
            
            // Find the appropriate starting line based on interaction flags
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
    /// Get the appropriate starting dialogue line based on interaction flags
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
                    if (debugInteractionFlags)
                    {
                        Debug.Log($"[NarrativeManager] Using conditional start: {chosenStart.lineId} for NPC: {currentNPCId}");
                    }
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
            // Check if the line can be accessed based on flags
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
                if (debugInteractionFlags)
                {
                    Debug.LogWarning($"[NarrativeManager] Line {nextLineId} blocked by flag conditions for NPC {currentNPCId}");
                }
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
        // Save interaction flags
        if (persistInteractionFlags)
        {
            SaveInteractionFlags();
        }

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
        currentNPCId = null;
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

        // Get the NPCScriptableObj from the conversation target
        if (currentConversationTarget is SettlerNPC settlerNPC && settlerNPC.nPCDataObj != null)
        {
            PlayerInventory.Instance.RecruitNPC(settlerNPC.nPCDataObj);
            
            // Set recruitment success flag
            SetFlag("recruited");
            SetFlag($"recruited_{npcName.Replace(" ", "_").ToLower()}");
            
            Debug.Log($"[NarrativeManager] Successfully recruited NPC: {npcName}");
        }
        else
        {
            // Set recruitment failed flag
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

    #region Interaction Flag Management

    /// <summary>
    /// Generate a unique ID for an NPC based on the conversation target and narrative type
    /// </summary>
    private string GenerateNPCId(INarrativeTarget target, NPCNarrativeType narrativeType)
    {
        string baseId = "";
        
        // Try to get a unique identifier from the NPC
        if (target is SettlerNPC settlerNPC && settlerNPC.nPCDataObj != null)
        {
            baseId = settlerNPC.nPCDataObj.nPCName.Replace(" ", "_").ToLower();
        }
        else if (target is MonoBehaviour mb)
        {
            baseId = $"{mb.gameObject.name}_{mb.GetInstanceID()}";
        }
        else
        {
            baseId = $"unknown_npc_{target.GetHashCode()}";
        }

        return $"{baseId}_{narrativeType}";
    }

    /// <summary>
    /// Check if conditions are met based on required and blocked flags
    /// </summary>
    private bool CheckConditions(List<string> requiredFlags, List<string> blockedByFlags)
    {
        // Check required flags
        if (requiredFlags != null && requiredFlags.Count > 0)
        {
            foreach (string flag in requiredFlags)
            {
                if (!HasFlag(flag))
                {
                    return false;
                }
            }
        }

        // Check blocked flags
        if (blockedByFlags != null && blockedByFlags.Count > 0)
        {
            foreach (string flag in blockedByFlags)
            {
                if (HasFlag(flag))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Process flag operations (set and remove)
    /// </summary>
    private void ProcessFlags(List<string> setFlags, List<string> removeFlags)
    {
        if (setFlags != null)
        {
            foreach (string flag in setFlags)
            {
                SetFlag(flag);
            }
        }

        if (removeFlags != null)
        {
            foreach (string flag in removeFlags)
            {
                RemoveFlag(flag);
            }
        }
    }

    /// <summary>
    /// Set an interaction flag for the current NPC
    /// </summary>
    public void SetFlag(string flagName)
    {
        if (string.IsNullOrEmpty(currentNPCId) || string.IsNullOrEmpty(flagName))
            return;

        if (!npcInteractionFlags.ContainsKey(currentNPCId))
        {
            npcInteractionFlags[currentNPCId] = new HashSet<string>();
        }

        bool wasAdded = npcInteractionFlags[currentNPCId].Add(flagName);
        
        if (debugInteractionFlags && wasAdded)
        {
            Debug.Log($"[NarrativeManager] Set flag '{flagName}' for NPC '{currentNPCId}'");
        }
    }

    /// <summary>
    /// Remove an interaction flag for the current NPC
    /// </summary>
    public void RemoveFlag(string flagName)
    {
        if (string.IsNullOrEmpty(currentNPCId) || string.IsNullOrEmpty(flagName))
            return;

        if (npcInteractionFlags.ContainsKey(currentNPCId))
        {
            bool wasRemoved = npcInteractionFlags[currentNPCId].Remove(flagName);
            
            if (debugInteractionFlags && wasRemoved)
            {
                Debug.Log($"[NarrativeManager] Removed flag '{flagName}' for NPC '{currentNPCId}'");
            }
        }
    }

    /// <summary>
    /// Check if an interaction flag is set for the current NPC
    /// </summary>
    public bool HasFlag(string flagName)
    {
        if (string.IsNullOrEmpty(currentNPCId) || string.IsNullOrEmpty(flagName))
            return false;

        return npcInteractionFlags.ContainsKey(currentNPCId) && 
               npcInteractionFlags[currentNPCId].Contains(flagName);
    }

    /// <summary>
    /// Get all flags for the current NPC
    /// </summary>
    public HashSet<string> GetAllFlags()
    {
        if (string.IsNullOrEmpty(currentNPCId))
            return new HashSet<string>();

        return npcInteractionFlags.ContainsKey(currentNPCId) ? 
               new HashSet<string>(npcInteractionFlags[currentNPCId]) : 
               new HashSet<string>();
    }

    /// <summary>
    /// Clear all flags for the current NPC (useful for testing)
    /// </summary>
    public void ClearAllFlags()
    {
        if (!string.IsNullOrEmpty(currentNPCId) && npcInteractionFlags.ContainsKey(currentNPCId))
        {
            npcInteractionFlags[currentNPCId].Clear();
            
            if (debugInteractionFlags)
            {
                Debug.Log($"[NarrativeManager] Cleared all flags for NPC '{currentNPCId}'");
            }
        }
    }

    #endregion

    #region Persistence

    /// <summary>
    /// Save interaction flags to PlayerPrefs
    /// </summary>
    private void SaveInteractionFlags()
    {
        try
        {
            foreach (var npcData in npcInteractionFlags)
            {
                string flagsJson = string.Join(",", npcData.Value);
                PlayerPrefs.SetString($"NarrativeFlags_{npcData.Key}", flagsJson);
            }
            PlayerPrefs.Save();
            
            if (debugInteractionFlags)
            {
                Debug.Log($"[NarrativeManager] Saved interaction flags for {npcInteractionFlags.Count} NPCs");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[NarrativeManager] Failed to save interaction flags: {e.Message}");
        }
    }

    /// <summary>
    /// Load interaction flags from PlayerPrefs
    /// </summary>
    private void LoadInteractionFlags()
    {
        try
        {
            npcInteractionFlags.Clear();
            
            // PlayerPrefs doesn't have a way to enumerate keys, so we'll need to load flags as we encounter NPCs
            // This method will be called when we need to load flags for a specific NPC
            
            if (debugInteractionFlags)
            {
                Debug.Log("[NarrativeManager] Interaction flags system initialized");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[NarrativeManager] Failed to load interaction flags: {e.Message}");
        }
    }

    /// <summary>
    /// Load flags for a specific NPC ID
    /// </summary>
    private void LoadFlagsForNPC(string npcId)
    {
        if (string.IsNullOrEmpty(npcId) || npcInteractionFlags.ContainsKey(npcId))
            return;

        string savedFlags = PlayerPrefs.GetString($"NarrativeFlags_{npcId}", "");
        
        if (!string.IsNullOrEmpty(savedFlags))
        {
            npcInteractionFlags[npcId] = new HashSet<string>(savedFlags.Split(','));
            
            if (debugInteractionFlags)
            {
                Debug.Log($"[NarrativeManager] Loaded {npcInteractionFlags[npcId].Count} flags for NPC '{npcId}'");
            }
        }
        else
        {
            npcInteractionFlags[npcId] = new HashSet<string>();
        }
    }

    #endregion

    /// <summary>
    /// Get the current dialogue lines map for external access
    /// </summary>
    public Dictionary<string, DialogueLine> GetCurrentDialogueLinesMap()
    {
        return currentDialogueLinesMap;
    }

    /// <summary>
    /// Override the current NPC ID (useful for testing or special cases)
    /// </summary>
    public void SetCurrentNPCId(string npcId)
    {
        currentNPCId = npcId;
        LoadFlagsForNPC(npcId);
    }
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
