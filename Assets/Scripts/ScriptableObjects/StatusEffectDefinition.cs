using UnityEngine;

/// <summary>
/// Defines a status effect with its visual representation and behavior
/// Extends the existing effect system to include status-specific configuration
/// </summary>
[System.Serializable]
public class StatusEffectDefinition
{
    [Header("Status Configuration")]
    [Tooltip("The type of status effect this represents")]
    public StatusEffectType statusType;
    
    [Tooltip("Priority level for displaying this effect when multiple are active")]
    public StatusEffectPriority priority = StatusEffectPriority.NORMAL;
    
    [Tooltip("How this effect behaves when applied to a character that already has it")]
    public StatusEffectBehavior behavior = StatusEffectBehavior.REPLACE_EXISTING;
    
    [Header("Visual Effect")]
    [Tooltip("The visual effect definition to play for this status")]
    public EffectDefinition visualEffect;
    
    [Header("Icon Display")]
    [Tooltip("Icon sprite to display above the character")]
    public Sprite iconSprite;
    
    [Tooltip("Color tint to apply to the character")]
    public Color characterTint = Color.white;
    
    [Tooltip("Scale multiplier for the icon")]
    [Range(0.1f, 3f)]
    public float iconScale = 1f;
    
    [Tooltip("Height offset above character for effects/icons")]
    [Range(0f, 5f)]
    public float heightOffset = 2f;
    
    [Header("Floating Text")]
    [Tooltip("Text to display when effect is applied")]
    public string floatingText = "";
    
    [Tooltip("Color for floating text")]
    public Color textColor = Color.white;
    
    [Tooltip("Font size for floating text")]
    [Range(8, 32)]
    public int fontSize = 14;
    
    [Header("Material Effects")]
    [Tooltip("Should this effect modify character materials?")]
    public bool modifyMaterials = false;
    
    [Tooltip("Alpha value for transparency effects")]
    [Range(0f, 1f)]
    public float alphaValue = 1f;
    
    [Tooltip("Emission color for glow effects")]
    public Color emissionColor = Color.black;
    
    [Tooltip("Emission intensity")]
    [Range(0f, 5f)]
    public float emissionIntensity = 0f;
    
    [Header("Animation Modifiers")]
    [Tooltip("Should this effect modify character animations?")]
    public bool modifyAnimations = false;
    
    [Tooltip("Animation speed multiplier")]
    [Range(0.1f, 3f)]
    public float animationSpeedMultiplier = 1f;
    
    [Tooltip("Animation trigger to set when effect starts")]
    public string animationTrigger = "";
    
    [Header("Gameplay Effects")]
    [Tooltip("Movement speed multiplier")]
    [Range(0f, 3f)]
    public float movementSpeedMultiplier = 1f;
    
    [Tooltip("Should this effect prevent actions?")]
    public bool preventActions = false;
    
    [Tooltip("Damage per second while this effect is active (0 = no damage)")]
    public float damagePerSecond = 0f;
    
    [Tooltip("Healing per second while this effect is active (0 = no healing)")]
    public float healingPerSecond = 0f;
    
    [Header("Duration")]
    [Tooltip("Default duration in seconds (0 = infinite until manually removed)")]
    public float defaultDuration = 0f;
    
    [Tooltip("Can this effect's duration be refreshed?")]
    public bool canRefreshDuration = true;
    
    /// <summary>
    /// Get the visual effect definition for this status effect
    /// </summary>
    public EffectDefinition GetVisualEffect()
    {
        return visualEffect;
    }
    
    /// <summary>
    /// Check if this status effect has a visual component
    /// </summary>
    public bool HasVisualEffect()
    {
        return visualEffect != null;
    }
    
    /// <summary>
    /// Check if this status effect has an icon
    /// </summary>
    public bool HasIcon()
    {
        return iconSprite != null;
    }
    
    /// <summary>
    /// Check if this status effect has floating text
    /// </summary>
    public bool HasFloatingText()
    {
        return !string.IsNullOrEmpty(floatingText);
    }
    
    /// <summary>
    /// Check if this effect modifies character appearance
    /// </summary>
    public bool ModifiesAppearance()
    {
        return modifyMaterials || characterTint != Color.white || alphaValue != 1f;
    }
    
    /// <summary>
    /// Check if this effect has gameplay impact
    /// </summary>
    public bool HasGameplayEffects()
    {
        return movementSpeedMultiplier != 1f || 
               preventActions || 
               damagePerSecond != 0f || 
               healingPerSecond != 0f ||
               modifyAnimations;
    }
}
