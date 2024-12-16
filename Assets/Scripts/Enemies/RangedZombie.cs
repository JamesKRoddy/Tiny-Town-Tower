using UnityEngine;

public class RangedZombie : Zombie
{
    public GameObject projectilePrefab;
    public float rangedDamage = 15f;
    public float shootCooldown = 2f;
    private float lastShootTime;

    protected override void Update()
    {
        base.Update();

        // Ensure zombie doesn't shoot too frequently
        if (Time.time - lastShootTime >= shootCooldown && Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            StartAttack();
        }
    }

    protected override void StartAttack()
    {
        base.StartAttack();
        // Shoot projectile after attack animation starts
        ShootProjectile();
    }

    private void ShootProjectile()
    {
        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        Vector3 direction = (player.position - transform.position).normalized;
        projectile.GetComponent<ZombieVomitProjectile>().Initialize(direction, rangedDamage);
        lastShootTime = Time.time;
    }
}
