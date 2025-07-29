using UnityEngine;

/// <summary>
/// Interactive component for NPCs that can start narrative conversations.
/// Uses NPCNarrativeType for dynamic dialogue loading through NarrativeManager.
/// </summary>
public class NarrativeInteractive : MonoBehaviour, IInteractive<NarrativeAsset>
{
    [Header("Narrative Configuration")]
    [SerializeField] private NPCNarrativeType npcNarrativeType = NPCNarrativeType.GENERIC_CONVERSATION;
    
    [Header("Unique Conversation (Optional)")]
    [SerializeField] private NarrativeAsset narrativeAsset;

    public string GetInteractionText() => "Start Conversation";

    public bool CanInteract() => true;

    /// <summary>
    /// Interact method that starts conversation based on configuration
    /// </summary>
    public NarrativeAsset Interact() 
    {
        // If a specific narrative asset is assigned, use it
        if (narrativeAsset != null && narrativeAsset.dialogueFile != null)
        {
            Debug.Log($"[NarrativeInteractive] Interacting with narrative asset: {narrativeAsset}");
            return narrativeAsset;
        }
        // Otherwise use dynamic loading based on NPCNarrativeType
        else if (NarrativeManager.Instance != null)
        {
            Debug.Log($"[NarrativeInteractive] Interacting with narrative type: {npcNarrativeType}");
            // Use dynamic loading based on NPCNarrativeType
            NarrativeManager.Instance.StartConversation(npcNarrativeType, GetNarrativeTarget());
            
            // Return null since we're handling the conversation through NarrativeManager
            return null;
        }         
        else
        {
            Debug.LogWarning($"[NarrativeInteractive] No narrative configuration found on {gameObject.name}!");
            return null;
        }
    }

    /// <summary>
    /// Get the narrative target from this GameObject or its parent
    /// </summary>
    private INarrativeTarget GetNarrativeTarget()
    {
        // Try to get from this GameObject
        INarrativeTarget target = GetComponent<INarrativeTarget>();
        
        // If not found, try parent
        if (target == null)
        {
            target = GetComponentInParent<INarrativeTarget>();
        }

        return target;
    }

    /// <summary>
    /// Public method to start conversation directly (for external callers)
    /// </summary>
    public void StartConversation()
    {
        Interact();
    }

    /// <summary>
    /// Set the narrative type dynamically (useful for procedural NPCs)
    /// </summary>
    public void SetNarrativeType(NPCNarrativeType newType)
    {
        npcNarrativeType = newType;
        // Clear narrative asset to ensure we use the narrative type
        narrativeAsset = null;
    }

    /// <summary>
    /// Set a custom narrative asset (useful for unique conversations)
    /// </summary>
    public void SetNarrativeAsset(NarrativeAsset asset)
    {
        narrativeAsset = asset;
    }

    /// <summary>
    /// Get the current narrative type
    /// </summary>
    public NPCNarrativeType GetNarrativeType()
    {
        return npcNarrativeType;
    }

    object IInteractiveBase.Interact() => Interact();

    #region Editor Helper Methods
#if UNITY_EDITOR
    [ContextMenu("Test Conversation")]
    private void TestConversation()
    {
        if (Application.isPlaying)
        {
            StartConversation();
        }
        else
        {
            Debug.Log($"[NarrativeInteractive] Would start conversation with type: {npcNarrativeType}");
        }
    }
#endif
    #endregion
}

/// <summary>
/// Data structure for narrative assets (unchanged for compatibility)
/// </summary>
[System.Serializable]
public class NarrativeAsset
{
    public TextAsset dialogueFile;
}
