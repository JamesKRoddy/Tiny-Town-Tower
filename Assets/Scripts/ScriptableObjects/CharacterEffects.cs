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
    [Tooltip("Effects played when the character takes a step")]
    public EffectDefinition[] footstepEffects = new EffectDefinition[0];

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
        if (spawnEffects == null) spawnEffects = new EffectDefinition[0];
        if (idleEffects == null) idleEffects = new EffectDefinition[0];
    }
}
