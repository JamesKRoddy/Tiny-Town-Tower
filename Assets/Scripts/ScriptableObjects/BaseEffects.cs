using UnityEngine;

/// <summary>
/// Base class for all effect collections. Provides common effect types that can be shared
/// between characters and buildings (hit effects, destruction effects, etc.)
/// </summary>
public abstract class BaseEffects : ScriptableObject
{
    [Header("Combat Effects")]
    [Tooltip("Impact effects showing the force of the hit (dust, sparks, debris)")]
    public EffectDefinition[] impactEffects = new EffectDefinition[0];

    [Tooltip("Special effects played when the object is destroyed")]
    public EffectDefinition[] destructionEffects = new EffectDefinition[0];

    protected virtual void OnEnable()
    {
        // Initialize arrays if they're null
        if (impactEffects == null) impactEffects = new EffectDefinition[0];
        if (destructionEffects == null) destructionEffects = new EffectDefinition[0];
    }
} 