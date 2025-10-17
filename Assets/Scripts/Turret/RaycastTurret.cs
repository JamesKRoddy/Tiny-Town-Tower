using UnityEngine;

/// <summary>
/// Raycast-based turret that fires instant hit projectiles
/// Uses the unified damage system via IDamageDealer
/// </summary>
public class RaycastTurret : BaseTurret
{
    [Header("Raycast Settings")]
    [Tooltip("Visual effect to play when firing")]
    public EffectDefinition muzzleFlashEffect;
    [Tooltip("Visual effect to play on hit")]
    public EffectDefinition hitEffect;
    [Tooltip("Maximum raycast distance")]
    public float maxRaycastDistance = 100f;

    protected override void Fire()
    {
        // Play muzzle flash effect
        if (muzzleFlashEffect != null && Managers.EffectManager.Instance != null)
        {
            Managers.EffectManager.Instance.PlayEffect(
                firePoint.position,
                firePoint.forward,
                firePoint.rotation,
                firePoint,
                muzzleFlashEffect
            );
        }

        // Use the unified raycast damage system
        RaycastHit hit;
        bool didHit = DamageUtils.PerformRaycastDamage(
            this,
            firePoint.position,
            firePoint.forward,
            maxRaycastDistance,
            -1, // All layers
            out hit
        );
        
        // Play hit effect if we hit something
        if (didHit && hitEffect != null && Managers.EffectManager.Instance != null)
        {
            Managers.EffectManager.Instance.PlayEffect(
                hit.point,
                hit.normal,
                Quaternion.LookRotation(hit.normal),
                hit.transform,
                hitEffect
            );
        }
    }
}
