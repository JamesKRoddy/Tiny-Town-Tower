using UnityEngine;

/// <summary>
/// Projectile-based turret that fires physical projectiles
/// Uses the unified damage system and DamageUtils for projectile spawning
/// </summary>
public class ProjectileTurret : BaseTurret
{
    [Header("Projectile Settings")]
    [Tooltip("Visual effect for the projectile")]
    public EffectDefinition projectileEffect;
    [Tooltip("Visual effect on impact")]
    public EffectDefinition impactEffect;
    [Tooltip("Projectile speed")]
    public float projectileSpeed = 20f;
    [Tooltip("Maximum height of projectile arc")]
    public float projectileMaxHeight = 3f;
    [Tooltip("Whether to create a damage area on impact")]
    public bool createDamageAreaOnImpact = false;
    [Tooltip("Radius of damage area on impact")]
    public float impactDamageRadius = 2f;
    [Tooltip("Duration of damage area")]
    public float impactDamageDuration = 3f;

    protected override void Fire()
    {
        if (target == null) return;

        // Calculate direction to target
        Vector3 direction = (target.transform.position - firePoint.position).normalized;
        
        // Fire projectile using DamageUtils
        GameObject projectile = DamageUtils.FireProjectileWithEffect(
            firePoint.position,
            direction,
            firePoint.rotation,
            target.transform.position,
            damage,
            poiseDamage,
            transform, // Turret is the damage source
            elementType,
            projectileEffect,
            impactEffect,
            createDamageAreaOnImpact,
            impactDamageRadius,
            impactDamageDuration,
            false // Don't use trigger-based damage for turret projectiles
        );
    }
}