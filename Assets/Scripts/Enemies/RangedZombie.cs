using UnityEngine;

namespace Enemies{
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
            if (Time.time - lastShootTime >= shootCooldown && Vector3.Distance(transform.position, navMeshTarget.position) <= attackRange)
            {
                StartAttack();
            }
        }

        private void ShootProjectile()
        {
            GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            Vector3 direction = (navMeshTarget.position - transform.position).normalized;
            projectile.GetComponent<ZombieVomitProjectile>().Initialize(direction, rangedDamage);
            lastShootTime = Time.time;
        }
    }
}
