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

    #region NarrativeAsset Flag Management

    /// <summary>
    /// Get the narrative asset (create one if using dynamic loading)
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

        var asset = GetOrCreateNarrativeAsset();
        return asset.flags.Any(f => f.flagName == flagName);
    }

    /// <summary>
    /// Get the value of a specific flag
    /// </summary>
    public string GetFlagValue(string flagName)
    {
        if (string.IsNullOrEmpty(flagName))
            return null;

        var asset = GetOrCreateNarrativeAsset();
        var flag = asset.flags.FirstOrDefault(f => f.flagName == flagName);
        return flag.flagName != null ? flag.value : null;
    }

    /// <summary>
    /// Set a flag on the narrative asset
    /// </summary>
    public void SetFlag(string flagName, string value = "true")
    {
        if (string.IsNullOrEmpty(flagName))
            return;

        var asset = GetOrCreateNarrativeAsset();
        
        // Find existing flag or create new one
        for (int i = 0; i < asset.flags.Count; i++)
        {
            if (asset.flags[i].flagName == flagName)
            {
                // Update existing flag
                var existingFlag = asset.flags[i];
                existingFlag.value = value;
                asset.flags[i] = existingFlag;
                
                if (debugFlags)
                {
                    Debug.Log($"[NarrativeInteractive] {gameObject.name} updated flag: {flagName} = {value}");
                }
                return;
            }
        }

        // Add new flag
        asset.flags.Add(new NarrativeAssetFlag { flagName = flagName, value = value });
        
        if (debugFlags)
        {
            Debug.Log($"[NarrativeInteractive] {gameObject.name} set flag: {flagName} = {value}");
        }
    }

    /// <summary>
    /// Remove a flag from the narrative asset
    /// </summary>
    public void RemoveFlag(string flagName)
    {
        if (string.IsNullOrEmpty(flagName))
            return;

        var asset = GetOrCreateNarrativeAsset();
        
        for (int i = asset.flags.Count - 1; i >= 0; i--)
        {
            if (asset.flags[i].flagName == flagName)
            {
                asset.flags.RemoveAt(i);
                
                if (debugFlags)
                {
                    Debug.Log($"[NarrativeInteractive] {gameObject.name} removed flag: {flagName}");
                }
                return;
            }
        }
    }

    /// <summary>
    /// Get all flags as a dictionary for easy access
    /// </summary>
    public Dictionary<string, string> GetAllFlags()
    {
        var asset = GetOrCreateNarrativeAsset();
        return asset.flags.ToDictionary(f => f.flagName, f => f.value);
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
