using UnityEngine;

/// <summary>
/// Wraps an EffectDefinition with spawn configuration (position, rotation, parenting).
/// Can be used anywhere effects need to be spawned with specific placement control.
/// </summary>
[System.Serializable]
public class EffectSpawnData
{
    [Tooltip("The effect definition to spawn")]
    public EffectDefinition effectDefinition;
    
    [Tooltip("Transform where the effect should spawn (optional - uses caller's transform if null)")]
    public Transform spawnPoint;
    
    [Tooltip("Local position offset from the spawn point or parent transform")]
    public Vector3 positionOffset = Vector3.zero;
    
    [Tooltip("Local rotation offset (in euler angles)")]
    public Vector3 rotationOffset = Vector3.zero;
    
    [Tooltip("Should the effect be parented to the spawn transform?")]
    public bool parentToTransform = false;
    
    [Tooltip("Custom duration override (0 = use effect definition's duration)")]
    public float durationOverride = 0f;
    
    /// <summary>
    /// Check if this spawn data is valid (has an effect definition)
    /// </summary>
    public bool IsValid()
    {
        return effectDefinition != null;
    }
    
    /// <summary>
    /// Get the world position where the effect should spawn
    /// </summary>
    /// <param name="fallbackTransform">Transform to use if spawnPoint is null</param>
    /// <returns>World position for effect spawn</returns>
    public Vector3 GetSpawnPosition(Transform fallbackTransform)
    {
        Transform sourceTransform = spawnPoint != null ? spawnPoint : fallbackTransform;
        
        if (sourceTransform == null)
        {
            Debug.LogWarning("[EffectSpawnData] No transform available for effect spawn position");
            return Vector3.zero;
        }
        
        // Apply position offset in local space
        return sourceTransform.position + sourceTransform.TransformDirection(positionOffset);
    }
    
    /// <summary>
    /// Get the world rotation where the effect should spawn
    /// </summary>
    /// <param name="fallbackTransform">Transform to use if spawnPoint is null</param>
    /// <returns>World rotation for effect spawn</returns>
    public Quaternion GetSpawnRotation(Transform fallbackTransform)
    {
        Transform sourceTransform = spawnPoint != null ? spawnPoint : fallbackTransform;
        
        if (sourceTransform == null)
        {
            Debug.LogWarning("[EffectSpawnData] No transform available for effect spawn rotation");
            return Quaternion.identity;
        }
        
        // Apply rotation offset
        return sourceTransform.rotation * Quaternion.Euler(rotationOffset);
    }
    
    /// <summary>
    /// Get the parent transform for the effect (or null if not parenting)
    /// </summary>
    /// <param name="fallbackTransform">Transform to use if spawnPoint is null</param>
    /// <returns>Parent transform, or null if parentToTransform is false</returns>
    public Transform GetParentTransform(Transform fallbackTransform)
    {
        if (!parentToTransform)
            return null;
        
        return spawnPoint != null ? spawnPoint : fallbackTransform;
    }
    
    /// <summary>
    /// Get the effective duration for this effect
    /// </summary>
    /// <returns>Duration to use (override if set, otherwise effect definition's duration)</returns>
    public float GetEffectDuration()
    {
        return durationOverride > 0f ? durationOverride : effectDefinition?.duration ?? 0f;
    }
    
    /// <summary>
    /// Spawn the effect using EffectManager
    /// </summary>
    /// <param name="fallbackTransform">Transform to use if spawnPoint is null</param>
    /// <returns>The spawned effect GameObject, or null if failed</returns>
    public GameObject SpawnEffect(Transform fallbackTransform)
    {
        if (!IsValid())
        {
            Debug.LogWarning("[EffectSpawnData] Cannot spawn effect - no effect definition assigned");
            return null;
        }
        
        if (Managers.EffectManager.Instance == null)
        {
            Debug.LogWarning("[EffectSpawnData] Cannot spawn effect - EffectManager not found");
            return null;
        }
        
        Vector3 position = GetSpawnPosition(fallbackTransform);
        Quaternion rotation = GetSpawnRotation(fallbackTransform);
        Transform parent = GetParentTransform(fallbackTransform);
        float duration = GetEffectDuration();
        
        return Managers.EffectManager.Instance.PlayEffect(
            position,
            Vector3.up,
            rotation,
            parent,
            effectDefinition,
            duration
        );
    }
}


