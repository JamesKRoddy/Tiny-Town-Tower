using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Interactive component for NPCs that can start narrative conversations.
/// Uses NPCNarrativeType for dynamic dialogue loading through NarrativeManager.
/// Progression state is stored on the NarrativeAsset itself.
/// </summary>
public class NarrativeInteractive : MonoBehaviour, IInteractive<NarrativeAsset>
{
    [Header("Narrative Configuration")]
    [SerializeField] private NPCNarrativeType npcNarrativeType = NPCNarrativeType.GENERIC_CONVERSATION;
    
    [Header("Unique Conversation")]
    [SerializeField] private NarrativeAsset narrativeAsset;

    [Header("Debug")]
    [SerializeField] private bool debugFlags = false; // Show flag operations in inspector

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
            Debug.Log($"[NarrativeInteractive] Interacting with narrative asset: {narrativeAsset.dialogueFile.name}");
            
            // Start conversation through NarrativeManager with this component as source
            if (NarrativeManager.Instance != null)
            {
                NarrativeManager.Instance.StartConversation(narrativeAsset, this);
                return null; // We're handling it through NarrativeManager
            }
            
            return narrativeAsset; // Fallback to legacy system
        }
        // Otherwise use dynamic loading based on NPCNarrativeType
        else if (NarrativeManager.Instance != null)
        {
            Debug.Log($"[NarrativeInteractive] Interacting with narrative type: {npcNarrativeType}");
            // Use dynamic loading based on NPCNarrativeType, passing this component as source
            NarrativeManager.Instance.StartConversation(npcNarrativeType, GetNarrativeTarget(), this);
            
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

    #region NarrativeAsset Flag Management - UUID System Integration

    /// <summary>
    /// Get the SaveableObject component for UUID-based flag management
    /// </summary>
    private SaveableObject GetSaveableObject()
    {
        SaveableObject saveableObj = GetComponent<SaveableObject>();
        if (saveableObj == null)
        {
            saveableObj = gameObject.AddComponent<SaveableObject>();
            if (debugFlags)
            {
                Debug.Log($"[NarrativeInteractive] Added SaveableObject component to {gameObject.name}");
            }
        }
        return saveableObj;
    }

    /// <summary>
    /// Get the narrative asset (create one if using dynamic loading) - maintains legacy compatibility
    /// </summary>
    public NarrativeAsset GetOrCreateNarrativeAsset()
    {
        if (narrativeAsset == null)
        {
            // Create a runtime narrative asset for dynamic loading
            narrativeAsset = new NarrativeAsset();
            if (debugFlags)
            {
                Debug.Log($"[NarrativeInteractive] Created runtime NarrativeAsset for {gameObject.name}");
            }
        }

        // Initialize flags list if null
        if (narrativeAsset.flags == null)
        {
            narrativeAsset.flags = new List<NarrativeAssetFlag>();
        }

        return narrativeAsset;
    }

    /// <summary>
    /// Check if the narrative asset has a specific flag
    /// </summary>
    public bool HasFlag(string flagName)
    {
        if (string.IsNullOrEmpty(flagName))
            return false;

        // Use UUID-based flag system
        var saveableObj = GetSaveableObject();
        string flagValue = saveableObj.GetNarrativeFlag(flagName);
        return !string.IsNullOrEmpty(flagValue);
    }

    /// <summary>
    /// Get the value of a specific flag
    /// </summary>
    public string GetFlagValue(string flagName)
    {
        if (string.IsNullOrEmpty(flagName))
            return null;

        // Use UUID-based flag system
        var saveableObj = GetSaveableObject();
        return saveableObj.GetNarrativeFlag(flagName);
    }

    /// <summary>
    /// Set a flag on the narrative asset
    /// </summary>
    public void SetFlag(string flagName, string value = "true")
    {
        if (string.IsNullOrEmpty(flagName))
            return;

        // Use UUID-based flag system
        var saveableObj = GetSaveableObject();
        saveableObj.SetNarrativeFlag(flagName, value);
        
        if (debugFlags)
        {
            Debug.Log($"[NarrativeInteractive] {gameObject.name} (UUID: {saveableObj.UUID}) set flag: {flagName} = {value}");
        }
    }

    /// <summary>
    /// Remove a flag from the narrative asset
    /// </summary>
    public void RemoveFlag(string flagName)
    {
        if (string.IsNullOrEmpty(flagName))
            return;

        // Use UUID-based flag system
        var saveableObj = GetSaveableObject();
        saveableObj.SetNarrativeFlag(flagName, null); // Setting to null removes the flag
        
        if (debugFlags)
        {
            Debug.Log($"[NarrativeInteractive] {gameObject.name} (UUID: {saveableObj.UUID}) removed flag: {flagName}");
        }
    }

    /// <summary>
    /// Get all flags as a dictionary for easy access
    /// </summary>
    public Dictionary<string, string> GetAllFlags()
    {
        // Use UUID-based flag system
        var saveableObj = GetSaveableObject();
        return saveableObj.GetAllNarrativeFlags();
    }

    /// <summary>
    /// Check if conditions are met based on required and blocked flags
    /// </summary>
    public bool CheckConditions(List<string> requiredFlags, List<string> blockedByFlags)
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
    public void ProcessFlags(List<string> setFlags, List<string> removeFlags)
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

    #endregion    

    object IInteractiveBase.Interact() => Interact();
}

/// <summary>
/// Data structure for narrative assets with progression tracking
/// </summary>
[System.Serializable]
public class NarrativeAsset
{
    public TextAsset dialogueFile;

    public List<NarrativeAssetFlag> flags;
}

/// <summary>
/// Flag structure for tracking narrative progression on assets
/// </summary>
[System.Serializable]
public struct NarrativeAssetFlag 
{
    public string flagName;
    public string value;
}
