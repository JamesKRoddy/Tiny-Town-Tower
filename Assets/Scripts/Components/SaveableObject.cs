using System;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Universal component for all objects that need to be saved/loaded.
/// Provides persistent UUID identification and consolidated save data.
/// </summary>
[System.Serializable]
public class SaveableObject : MonoBehaviour
{
    [SerializeField] private string uuid;
    
    // Consolidated narrative data (no longer stored separately)
    [SerializeField] private Dictionary<string, string> narrativeFlags = new Dictionary<string, string>();
    
    /// <summary>
    /// Get the persistent UUID for this object
    /// </summary>
    public string UUID 
    { 
        get 
        { 
            if (string.IsNullOrEmpty(uuid))
            {
                GenerateNewUUID();
            }
            return uuid; 
        } 
    }
    
    /// <summary>
    /// Generate a new UUID for this object (used during creation)
    /// </summary>
    public void GenerateNewUUID()
    {
        uuid = System.Guid.NewGuid().ToString();
        Debug.Log($"[SaveableObject] Generated new UUID {uuid} for {gameObject.name}");
    }
    
    /// <summary>
    /// Set UUID (used during loading from save)
    /// </summary>
    public void SetUUID(string newUuid)
    {
        uuid = newUuid;
    }
    
    /// <summary>
    /// Get narrative flag value
    /// </summary>
    public string GetNarrativeFlag(string flagName)
    {
        narrativeFlags.TryGetValue(flagName, out string value);
        return value;
    }
    
    /// <summary>
    /// Set narrative flag value
    /// </summary>
    public void SetNarrativeFlag(string flagName, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            narrativeFlags.Remove(flagName);
        }
        else
        {
            narrativeFlags[flagName] = value;
        }
    }
    
    /// <summary>
    /// Get all narrative flags for saving
    /// </summary>
    public Dictionary<string, string> GetAllNarrativeFlags()
    {
        return new Dictionary<string, string>(narrativeFlags);
    }
    
    /// <summary>
    /// Get all narrative flags in serializable format for saving
    /// </summary>
    public List<NarrativeFlagData> GetAllNarrativeFlagsSerializable()
    {
        var result = new List<NarrativeFlagData>();
        foreach (var kvp in narrativeFlags)
        {
            result.Add(new NarrativeFlagData(kvp.Key, kvp.Value));
        }
        return result;
    }
    
    /// <summary>
    /// Set all narrative flags (used during loading)
    /// </summary>
    public void SetAllNarrativeFlags(Dictionary<string, string> flags)
    {
        narrativeFlags.Clear();
        if (flags != null)
        {
            foreach (var kvp in flags)
            {
                narrativeFlags[kvp.Key] = kvp.Value;
            }
        }
    }
    
    /// <summary>
    /// Set all narrative flags from serializable format (used during loading)
    /// </summary>
    public void SetAllNarrativeFlagsFromSerializable(List<NarrativeFlagData> flags)
    {
        narrativeFlags.Clear();
        if (flags != null)
        {
            foreach (var flag in flags)
            {
                if (!string.IsNullOrEmpty(flag.flagName))
                {
                    narrativeFlags[flag.flagName] = flag.flagValue;
                }
            }
        }
    }
    
    /// <summary>
    /// Clear all narrative flags
    /// </summary>
    public void ClearNarrativeFlags()
    {
        narrativeFlags.Clear();
    }
    
    private void Awake()
    {
        // Ensure we have a UUID when the object is created
        if (string.IsNullOrEmpty(uuid))
        {
            GenerateNewUUID();
        }
        
        // Add a test narrative flag to verify serialization works
        if (Application.isPlaying && gameObject.GetComponent<SettlerNPC>() != null)
        {
            SetNarrativeFlag("test_flag", "test_value");
            Debug.Log($"[SaveableObject] Added test narrative flag to {gameObject.name} (UUID: {UUID})");
        }
    }
    
    private void OnValidate()
    {
        // Generate UUID in editor if missing
        if (string.IsNullOrEmpty(uuid) && Application.isPlaying == false)
        {
            GenerateNewUUID();
        }
    }
}

/// <summary>
/// Helper class for UUID-based save data with narrative integration
/// </summary>
[System.Serializable]
public class SaveableObjectData
{
    public string uuid;
    public Vector3 position;
    public string gameObjectName;
    public List<NarrativeFlagData> narrativeFlags = new List<NarrativeFlagData>();
    
    public SaveableObjectData(SaveableObject saveableObj)
    {
        uuid = saveableObj.UUID;
        position = saveableObj.transform.position;
        gameObjectName = saveableObj.gameObject.name;
        narrativeFlags = saveableObj.GetAllNarrativeFlagsSerializable();
    }
    
    public SaveableObjectData() { }
}