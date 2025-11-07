using UnityEngine;

/// <summary>
/// Contains all information about a damage instance
/// Provides a cleaner API than passing many parameters to TakeDamage
/// Supports both hostile combat damage and environmental/status damage
/// </summary>
public struct DamageInfo
{
    /// <summary>
    /// The amount of health damage to deal
    /// </summary>
    public float Amount;
    
    /// <summary>
    /// The amount of poise damage to deal (0 = no poise damage)
    /// </summary>
    public float PoiseDamage;
    
    /// <summary>
    /// The elemental type of damage (NONE = physical damage)
    /// </summary>
    public AttackElement ElementType;
    
    /// <summary>
    /// The transform of the damage source (optional, used for VFX positioning and directional reactions)
    /// </summary>
    public Transform SourceTransform;
    
    /// <summary>
    /// The damage dealer interface (optional, used for allegiance checking and hostile damage tracking)
    /// If null, this is treated as environmental/status damage
    /// </summary>
    public IDamageDealer DamageDealer;
    
    /// <summary>
    /// Whether to play hit visual effects (blood, impact particles, etc.)
    /// Set to false for environmental/status damage like hunger, sickness, cleanliness
    /// </summary>
    public bool PlayHitVFX;
    
    /// <summary>
    /// Whether this is environmental damage (cleanliness, hunger, etc.) vs hostile combat damage
    /// Environmental damage should not trigger flee behavior or combat responses
    /// </summary>
    public bool IsEnvironmentalDamage;
    
    /// <summary>
    /// Constructor for hostile/combat damage from a damage dealer
    /// Use this for weapons, enemies, projectiles, etc.
    /// </summary>
    /// <param name="dealer">The damage dealer (weapon, enemy, hazard, etc.)</param>
    /// <param name="amount">Amount of health damage (optional override, uses dealer.BaseDamage if not specified)</param>
    /// <param name="poiseDamage">Amount of poise damage (optional override, uses dealer.PoiseDamage if not specified)</param>
    /// <param name="playHitVFX">Whether to play hit visual effects</param>
    public DamageInfo(IDamageDealer dealer, float? amount = null, float? poiseDamage = null, bool playHitVFX = true)
    {
        DamageDealer = dealer;
        Amount = amount ?? dealer.BaseDamage;
        PoiseDamage = poiseDamage ?? dealer.PoiseDamage;
        ElementType = dealer.ElementType;
        SourceTransform = dealer.DamageSource;
        PlayHitVFX = playHitVFX;
        IsEnvironmentalDamage = false;
    }
    
    /// <summary>
    /// Create environmental/status effect damage (non-hostile)
    /// Use this for hunger, sickness, cleanliness penalties, etc.
    /// This damage will NOT trigger flee behavior or combat responses
    /// </summary>
    /// <param name="amount">Amount of health damage to deal</param>
    /// <param name="playHitVFX">Whether to play hit visual effects (usually false for environmental)</param>
    /// <returns>DamageInfo configured for environmental damage</returns>
    public static DamageInfo Environmental(float amount, bool playHitVFX = false)
    {
        return new DamageInfo
        {
            Amount = amount,
            PoiseDamage = 0f,
            ElementType = AttackElement.NONE,
            SourceTransform = null,
            DamageDealer = null,
            PlayHitVFX = playHitVFX,
            IsEnvironmentalDamage = true
        };
    }
    
    /// <summary>
    /// Create damage info from a dealer with optional overrides
    /// Use this when you have an IDamageDealer and want to use its properties
    /// </summary>
    /// <param name="dealer">The damage dealer providing base damage properties</param>
    /// <param name="amount">Optional override for damage amount (null = use dealer's value)</param>
    /// <param name="poiseDamage">Optional override for poise damage (null = use dealer's value)</param>
    /// <param name="playHitVFX">Whether to play hit visual effects</param>
    /// <returns>DamageInfo configured from the dealer</returns>
    public static DamageInfo Create(IDamageDealer dealer, float? amount = null, float? poiseDamage = null, bool playHitVFX = true)
    {
        return new DamageInfo
        {
            Amount = amount ?? dealer.BaseDamage,
            PoiseDamage = poiseDamage ?? dealer.PoiseDamage,
            ElementType = dealer.ElementType,
            SourceTransform = dealer.DamageSource,
            DamageDealer = dealer,
            PlayHitVFX = playHitVFX,
            IsEnvironmentalDamage = false
        };
    }
    
    /// <summary>
    /// Create damage info with explicit values (no dealer)
    /// Use this for custom damage that doesn't come from an IDamageDealer
    /// </summary>
    /// <param name="amount">Amount of health damage</param>
    /// <param name="poiseDamage">Amount of poise damage</param>
    /// <param name="elementType">Elemental damage type</param>
    /// <param name="sourceTransform">Transform of damage source (optional)</param>
    /// <param name="playHitVFX">Whether to play hit visual effects</param>
    /// <param name="isEnvironmental">Whether this is environmental damage (no flee behavior)</param>
    /// <returns>DamageInfo configured with the specified parameters</returns>
    public static DamageInfo Create(float amount, float poiseDamage = 0f, AttackElement elementType = AttackElement.NONE, 
                                     Transform sourceTransform = null, bool playHitVFX = true, bool isEnvironmental = false)
    {
        return new DamageInfo
        {
            Amount = amount,
            PoiseDamage = poiseDamage,
            ElementType = elementType,
            SourceTransform = sourceTransform,
            DamageDealer = null,
            PlayHitVFX = playHitVFX,
            IsEnvironmentalDamage = isEnvironmental
        };
    }
    
    /// <summary>
    /// Check if this damage is from a hostile source (enemy attack)
    /// Hostile damage should trigger flee behavior and combat responses
    /// </summary>
    /// <returns>True if damage is from a hostile source</returns>
    public bool IsHostileDamage()
    {
        return DamageDealer != null && DamageDealer.DealerAllegiance == Allegiance.HOSTILE;
    }
    
    /// <summary>
    /// Check if this damage has poise damage component
    /// </summary>
    /// <returns>True if poise damage is greater than 0</returns>
    public bool HasPoiseDamage()
    {
        return PoiseDamage > 0f;
    }
    
    /// <summary>
    /// Check if this damage has an elemental component
    /// </summary>
    /// <returns>True if damage type is not NONE</returns>
    public bool HasElementalDamage()
    {
        return ElementType != AttackElement.NONE;
    }
    
    /// <summary>
    /// Get the allegiance of the damage dealer (if available)
    /// </summary>
    /// <returns>The dealer's allegiance, or NEUTRAL if no dealer</returns>
    public Allegiance GetDealerAllegiance()
    {
        return DamageDealer?.DealerAllegiance ?? Allegiance.NEUTRAL;
    }
}

