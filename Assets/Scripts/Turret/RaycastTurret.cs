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

        RaycastHit hit;
        if (Physics.Raycast(firePoint.position, firePoint.forward, out hit, maxRaycastDistance))
        {
            IDamageable damageable = hit.collider.GetComponent<IDamageable>();

            if (damageable != null)
            {
                // Use the unified damage system
                DealDamage(damageable);
                
                // Play hit effect at impact point
                if (hitEffect != null && Managers.EffectManager.Instance != null)
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
    }
}
