using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "NewCharacterEffects", menuName = "Scriptable Objects/Roguelite/Enemies/Effects/Character Effects")]
public class CharacterEffects : ScriptableObject
{
    [Tooltip("The type of character these effects are for")]
    public CharacterType characterType;

    [Header("Combat Effects")]
    [Tooltip("Blood/gore effects for organic characters, or fluid/particle effects for machines")]
    public EffectDefinition[] bloodEffects = new EffectDefinition[0];

    [Tooltip("Impact effects showing the force of the hit (dust, sparks, debris)")]
    public EffectDefinition[] impactEffects = new EffectDefinition[0];

    [Tooltip("Special effects played when the character dies (explosions, disintegration, etc.)")]
    public EffectDefinition[] deathEffects = new EffectDefinition[0];

    [Header("Movement Effects")]
    [Tooltip("Effects played when the character takes a step")]
    public EffectDefinition[] footstepEffects = new EffectDefinition[0];

    [Header("Idle Effects")]
    [Tooltip("Random effects played while the character is idle")]
    public EffectDefinition[] idleEffects = new EffectDefinition[0];

    [Tooltip("Minimum time between idle effects")]
    public float minIdleInterval = 5f;

    [Tooltip("Maximum time between idle effects")]
    public float maxIdleInterval = 15f;

    private void OnEnable()
    {
        // Initialize arrays if they're null
        if (bloodEffects == null) bloodEffects = new EffectDefinition[0];
        if (impactEffects == null) impactEffects = new EffectDefinition[0];
        if (deathEffects == null) deathEffects = new EffectDefinition[0];
        if (footstepEffects == null) footstepEffects = new EffectDefinition[0];
        if (idleEffects == null) idleEffects = new EffectDefinition[0];
    }
}
