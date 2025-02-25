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
                damageable.TakeDamage(damage);
            }
        }
    }
}
