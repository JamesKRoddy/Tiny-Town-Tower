using UnityEngine;

public class ProjectileTurret : BaseTurret
{
    public GameObject projectilePrefab;

    protected override void Fire()
    {
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        // Add projectile-specific behavior
    }
}