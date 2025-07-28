using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Managers;

/// <summary>
/// Central manager for all narrative interactions in the game.
/// Handles dynamic dialogue loading, conversation flow, and NPC recruitment.
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
            
            // Show the conversation UI
            if (PlayerUIManager.Instance.narrativeMenu != null)
            {
                PlayerUIManager.Instance.narrativeMenu.gameObject.SetActive(true);
                PlayerUIManager.Instance.narrativeMenu.DisplayDialogue(currentDialogue.lines[0]);
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
            Debug.Log($"[NarrativeManager] Successfully recruited NPC: {npcName}");
        }
        else
        {
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

    /// <summary>
    /// Get the current dialogue lines map for external access
    /// </summary>
    public Dictionary<string, DialogueLine> GetCurrentDialogueLinesMap()
    {
        return currentDialogueLinesMap;
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
