using UnityEngine;

/// <summary>
/// Raycast-based turret that fires instant hit projectiles
/// Uses the unified damage system via IDamageDealer
/// </summary>
public class RaycastTurret : BaseTurret
{
    [Header("Raycast Settings")]
    [Tooltip("Muzzle flash effect configuration (typically parented to firePoint)")]
    public EffectSpawnData muzzleFlashEffect;
    [Tooltip("Hit effect to play on impact")]
    public EffectDefinition hitEffect;
    [Tooltip("Maximum raycast distance")]
    public float maxRaycastDistance = 100f;

    protected override void Fire()
    {
        // Play muzzle flash effect using EffectSpawnData
        if (muzzleFlashEffect != null && muzzleFlashEffect.IsValid())
        {
            muzzleFlashEffect.SpawnEffect(firePoint);
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
                null, // No parenting needed for hit effects
                hitEffect
            );
        }
    }
}
