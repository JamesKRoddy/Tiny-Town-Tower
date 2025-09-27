using UnityEngine;

public class RaycastTurret : BaseTurret
{
    protected override void Fire()
    {
        RaycastHit hit;
        if (Physics.Raycast(firePoint.position, firePoint.forward, out hit))
        {
            IDamageable damageable = hit.collider.GetComponent<IDamageable>();

            if (damageable != null)
            {
                // Turrets deal moderate poise damage
                float poiseDamage = damage * 0.5f; // 50% of health damage as poise damage
                damageable.TakeDamage(damage, poiseDamage);
            }
        }
    }
}
