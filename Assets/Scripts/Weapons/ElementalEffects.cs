using UnityEngine;

/// <summary>
/// Simple class that pairs an AttackElement with its hit and impact effects
/// </summary>
[System.Serializable]
public class ElementalEffects
{
    [Tooltip("The elemental damage type")]
    public AttackElement elementType;
    
    [Tooltip("Effects played on the character when hit by this element")]
    public EffectDefinition[] hitEffects = new EffectDefinition[0];
    
    [Tooltip("Effects played on the ground/impact point for this element")]
    public EffectDefinition[] impactEffects = new EffectDefinition[0];
}
