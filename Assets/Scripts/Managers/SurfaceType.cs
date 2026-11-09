using UnityEngine;

/// <summary>
/// Defines different ground surface types for context-aware effects (footsteps, impact, etc.)
/// Surface types are detected via raycasts hitting colliders with specific layers or physics materials
/// </summary>
public enum SurfaceType
{
    /// <summary>Default surface when no specific type is detected</summary>
    DEFAULT,
    
    /// <summary>Grass, dirt, soil, mud</summary>
    GRASS,
    
    /// <summary>Stone, concrete, brick, asphalt</summary>
    STONE,
    
    /// <summary>Wood, wooden floors, wooden structures</summary>
    WOOD,
    
    /// <summary>Metal surfaces, machinery, vehicles</summary>
    METAL,
    
    /// <summary>Water, puddles, shallow water</summary>
    WATER,
    
    /// <summary>Sand, gravel, loose earth</summary>
    SAND,
    
    /// <summary>Snow, ice</summary>
    SNOW,

    /// <summary>Poison, acid, venom</summary>
    POISON
}

/// <summary>
/// Maps surface types to effects (footsteps, impacts, etc.) for a specific character type
/// This class pairs a character type with surface-specific effect variations
/// </summary>
[System.Serializable]
public class SurfaceFootstepEffects
{
    [Tooltip("The surface type these effects apply to")]
    public SurfaceType surfaceType;
    
    [Tooltip("Footstep effects for this surface type")]
    public EffectDefinition[] footstepEffects = new EffectDefinition[0];
}

/// <summary>
/// Utility class for detecting surface types via raycasting
/// </summary>
public static class SurfaceDetector
{
    /// <summary>
    /// Detects the surface type at a given position by raycasting downward
    /// First checks for SurfaceIdentifier component, then falls back to physics material or layer
    /// Prioritizes triggers (water, poison) over solid ground surfaces
    /// </summary>
    /// <param name="position">World position to check from</param>
    /// <param name="hitPosition">Out parameter for the world position where the surface was detected</param>
    /// <param name="rayDistance">How far down to raycast (default 0.5m)</param>
    /// <param name="ignoreTransform">Optional transform to ignore (useful for ignoring the character's own collider)</param>
    /// <returns>Detected surface type, or DEFAULT if nothing found</returns>
    public static SurfaceType DetectSurface(Vector3 position, out Vector3 hitPosition, float rayDistance = 0.5f, Transform ignoreTransform = null, bool debugLog = false)
    {
        hitPosition = position; // Default to input position
        
        // Create layer mask that ignores character layers
        int layerMask = GameConstants.Layers.GroundDetectionMask;
        
        // First, check for triggers (water, poison, etc.) - these take priority
        RaycastHit[] hits = Physics.RaycastAll(position, Vector3.down, rayDistance, layerMask, QueryTriggerInteraction.Collide);
        
        if (debugLog && hits.Length > 0)
        {
            Debug.Log($"[SurfaceDetector] Found {hits.Length} hits from {position} (rayDist: {rayDistance})");
        }
        
        // Look for triggers first (like water/poison zones)
        foreach (RaycastHit hit in hits)
        {
            // Skip if we hit the character we're supposed to ignore
            if (ignoreTransform != null && hit.transform.IsChildOf(ignoreTransform))
                continue;
                
            // Only consider triggers for priority check
            if (hit.collider.isTrigger)
            {
                if (debugLog)
                {
                    Debug.Log($"[SurfaceDetector] Found TRIGGER: {hit.collider.gameObject.name} at {hit.point}");
                }
                
                SurfaceIdentifier surfaceId = hit.collider.GetComponent<SurfaceIdentifier>();
                if (surfaceId != null)
                {
                    hitPosition = hit.point;
                    if (debugLog)
                    {
                        Debug.Log($"[SurfaceDetector] Trigger has SurfaceIdentifier: {surfaceId.surfaceType}");
                    }
                    return surfaceId.surfaceType;
                }
                else if (debugLog)
                {
                    Debug.LogWarning($"[SurfaceDetector] Trigger '{hit.collider.gameObject.name}' has NO SurfaceIdentifier component!");
                }
            }
        }
        
        // If no trigger found, use standard solid surface detection
        RaycastHit solidHit;
        if (Physics.Raycast(position, Vector3.down, out solidHit, rayDistance, layerMask, QueryTriggerInteraction.Ignore))
        {
            // Skip if we hit the character we're supposed to ignore
            if (ignoreTransform != null && solidHit.transform.IsChildOf(ignoreTransform))
            {
                return SurfaceType.DEFAULT;
            }
            
            hitPosition = solidHit.point;
            
            // Priority 1: Check for explicit SurfaceIdentifier component
            SurfaceIdentifier surfaceId = solidHit.collider.GetComponent<SurfaceIdentifier>();
            if (surfaceId != null)
            {
                return surfaceId.surfaceType;
            }
            
            // Priority 2: Check physics material name
            if (solidHit.collider.sharedMaterial != null)
            {
                string materialName = solidHit.collider.sharedMaterial.name.ToLower();
                
                if (materialName.Contains("grass") || materialName.Contains("dirt") || materialName.Contains("soil"))
                    return SurfaceType.GRASS;
                    
                if (materialName.Contains("stone") || materialName.Contains("concrete") || materialName.Contains("brick"))
                    return SurfaceType.STONE;
                    
                if (materialName.Contains("wood"))
                    return SurfaceType.WOOD;
                    
                if (materialName.Contains("metal"))
                    return SurfaceType.METAL;
                    
                if (materialName.Contains("water"))
                    return SurfaceType.WATER;
                    
                if (materialName.Contains("sand") || materialName.Contains("gravel"))
                    return SurfaceType.SAND;
                    
                if (materialName.Contains("snow") || materialName.Contains("ice"))
                    return SurfaceType.SNOW;

                if (materialName.Contains("poison") || materialName.Contains("acid") || materialName.Contains("venom") || materialName.Contains("vomit"))
                    return SurfaceType.POISON;
            }
            
            // Priority 3: Check collider's game object name (fallback)
            string objectName = solidHit.collider.gameObject.name.ToLower();
            
            if (objectName.Contains("grass") || objectName.Contains("ground") || objectName.Contains("terrain"))
                return SurfaceType.GRASS;
                
            if (objectName.Contains("stone") || objectName.Contains("concrete"))
                return SurfaceType.STONE;
                
            if (objectName.Contains("wood") || objectName.Contains("floor"))
                return SurfaceType.WOOD;
                
            if (objectName.Contains("metal"))
                return SurfaceType.METAL;

            if (objectName.Contains("poison") || objectName.Contains("acid") || objectName.Contains("venom") || objectName.Contains("vomit"))
                return SurfaceType.POISON;
        }
        
        // Default to GRASS if nothing specific detected (most common outdoor surface)
        return SurfaceType.DEFAULT;
    }
    
    /// <summary>
    /// Detects surface at a transform's position, accounting for character height
    /// </summary>
    /// <param name="characterTransform">Character's transform</param>
    /// <param name="heightOffset">Offset from transform position to start raycast (default 0.1m)</param>
    /// <param name="rayDistance">How far to raycast (default 1.0m for character height)</param>
    /// <param name="hitPosition">Out parameter for the world position where the surface was detected</param>
    /// <returns>Detected surface type</returns>
    public static SurfaceType DetectSurfaceAtCharacter(Transform characterTransform, out Vector3 hitPosition, float heightOffset = 0.1f, float rayDistance = 1.0f)
    {
        Vector3 startPosition = characterTransform.position + Vector3.up * heightOffset;
        return DetectSurface(startPosition, out hitPosition, rayDistance, characterTransform);
    }
}

