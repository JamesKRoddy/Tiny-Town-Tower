using UnityEngine;

public class RaycastTurret : BaseTurret
{
    protected override void Fire()
    {
        RaycastHit hit;
        if (Physics.Raycast(firePoint.position, firePoint.forward, out hit))
        {
            if (hit.collider.CompareTag("Enemy"))
            {
                Debug.Log("Enemy hit by raycast turret!");
                // Add damage logic here
            }
        }
    }
}
