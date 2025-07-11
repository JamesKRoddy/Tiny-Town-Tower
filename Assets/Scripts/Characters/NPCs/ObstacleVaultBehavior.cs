using UnityEngine;

/// <summary>
/// Component that defines how a player should navigate over/through an obstacle.
/// Attach this to obstacle GameObjects to override default height-based vault detection.
/// </summary>
public class ObstacleVaultBehavior : MonoBehaviour
{
    [Header("Vault Behavior")]
    [SerializeField] private VaultAnimationType vaultType = VaultAnimationType.Vault;
    
    [Header("Dynamic Obstacle Settings")]
    [SerializeField] private bool isDynamic = false;
    [SerializeField] private bool blockVaultingWhenBlocked = false;
    
    [Header("Optional Overrides")]
    [SerializeField] private bool overrideVaultDuration = false;
    [SerializeField] private float customVaultDuration = 0.4f;
    
    public enum VaultAnimationType
    {
        Vault,      // Standard vault animation (default)
        Roll,       // Roll/slide under animation
        Block,      // Cannot be vaulted over (stop movement)
        WalkOver    // Too low, just walk over normally
    }
    
    public VaultAnimationType VaultType => vaultType;
    public bool IsDynamic => isDynamic;
    public bool BlockVaultingWhenBlocked => blockVaultingWhenBlocked;
    public bool HasCustomDuration => overrideVaultDuration;
    public float CustomVaultDuration => customVaultDuration;
    
    /// <summary>
    /// Checks if vaulting is currently allowed (for dynamic obstacles)
    /// </summary>
    /// <returns>True if vaulting is allowed, false if blocked</returns>
    public bool IsVaultingAllowed()
    {
        if (!isDynamic) return true;
        
        // TODO: Add custom logic for dynamic obstacles
        // For example: check if a moving platform is in position
        // or if there's enough clearance above the obstacle
        
        return true; // Default: always allow for now
    }
    
    /// <summary>
    /// Gets the effective vault type, considering dynamic conditions
    /// </summary>
    /// <returns>The vault animation type to use</returns>
    public VaultAnimationType GetEffectiveVaultType()
    {
        if (isDynamic && blockVaultingWhenBlocked && !IsVaultingAllowed())
        {
            return VaultAnimationType.Block;
        }
        
        return vaultType;
    }
    
    /// <summary>
    /// Gets the vault duration to use for this obstacle
    /// </summary>
    /// <param name="defaultDuration">The default vault duration from the character controller</param>
    /// <returns>Duration to use for vaulting this obstacle</returns>
    public float GetVaultDuration(float defaultDuration)
    {
        return overrideVaultDuration ? customVaultDuration : defaultDuration;
    }
    
    // Future expansion methods:
    
    /// <summary>
    /// Called when a character starts vaulting over this obstacle
    /// </summary>
    /// <param name="character">The character performing the vault</param>
    public virtual void OnVaultStart(HumanCharacterController character)
    {
        // Override in derived classes for custom behavior
        // Example: trigger animations, sound effects, particle effects
    }
    
    /// <summary>
    /// Called when a character finishes vaulting over this obstacle
    /// </summary>
    /// <param name="character">The character that completed the vault</param>
    public virtual void OnVaultComplete(HumanCharacterController character)
    {
        // Override in derived classes for custom behavior
        // Example: destructible obstacles, moving platforms
    }
} 