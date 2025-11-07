using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "NewCharacterEffects", menuName = "Scriptable Objects/Effects/Character Effects")]
public class CharacterEffects : BaseEffects
{
    [Tooltip("The type of character these effects are for")]
    public CharacterType characterType;

    [Header("Character-Specific Combat Effects")]
    [Tooltip("Blood/gore effects for organic characters, or fluid/particle effects for machines")]
    public EffectDefinition[] bloodEffects = new EffectDefinition[0];

    [Tooltip("Special effects played when the character dies (explosions, disintegration, etc.)")]
    public EffectDefinition[] deathEffects = new EffectDefinition[0];

    [Header("Movement Effects")]
    [Tooltip("DEPRECATED: Use surfaceFootstepEffects instead for surface-specific footsteps")]
    public EffectDefinition[] footstepEffects = new EffectDefinition[0];
    
    [Tooltip("Surface-specific footstep effects - one entry per surface type")]
    public SurfaceFootstepEffects[] surfaceFootstepEffects = new SurfaceFootstepEffects[0];

    [Header("Spawn Effects")]
    [Tooltip("Effects played when the character spawns (for enemies)")]
    public EffectDefinition[] spawnEffects = new EffectDefinition[0];

    [Header("Idle Effects")]
    [Tooltip("Random effects played while the character is idle")]
    public EffectDefinition[] idleEffects = new EffectDefinition[0];

    [Tooltip("Minimum time between idle effects")]
    public float minIdleInterval = 5f;

    [Tooltip("Maximum time between idle effects")]
    public float maxIdleInterval = 15f;



    protected override void OnEnable()
    {
        base.OnEnable();
        
        // Initialize character-specific arrays if they're null
        if (bloodEffects == null) bloodEffects = new EffectDefinition[0];
        if (deathEffects == null) deathEffects = new EffectDefinition[0];
        if (footstepEffects == null) footstepEffects = new EffectDefinition[0];
        if (surfaceFootstepEffects == null) surfaceFootstepEffects = new SurfaceFootstepEffects[0];
        if (spawnEffects == null) spawnEffects = new EffectDefinition[0];
        if (idleEffects == null) idleEffects = new EffectDefinition[0];
    }
    
    /// <summary>
    /// Gets footstep effects for a specific surface type
    /// Falls back to default footstepEffects if no surface-specific effects are defined
    /// </summary>
    /// <param name="surfaceType">The surface type to get effects for</param>
    /// <returns>Array of effect definitions for the surface, or null if none found</returns>
    public EffectDefinition[] GetFootstepEffectsForSurface(SurfaceType surfaceType)
    {
        // First try to find surface-specific effects
        if (surfaceFootstepEffects != null && surfaceFootstepEffects.Length > 0)
        {
            foreach (var surfaceEffect in surfaceFootstepEffects)
            {
                if (surfaceEffect.surfaceType == surfaceType && surfaceEffect.footstepEffects != null && surfaceEffect.footstepEffects.Length > 0)
                {
                    return surfaceEffect.footstepEffects;
                }
            }
            
            // If specific surface not found, try DEFAULT surface
            if (surfaceType != SurfaceType.DEFAULT)
            {
                foreach (var surfaceEffect in surfaceFootstepEffects)
                {
                    if (surfaceEffect.surfaceType == SurfaceType.DEFAULT && surfaceEffect.footstepEffects != null && surfaceEffect.footstepEffects.Length > 0)
                    {
                        return surfaceEffect.footstepEffects;
                    }
                }
            }
        }
        
        // Fallback to legacy footstepEffects array
        if (footstepEffects != null && footstepEffects.Length > 0)
        {
            return footstepEffects;
        }
        
        return null;
    }
}
