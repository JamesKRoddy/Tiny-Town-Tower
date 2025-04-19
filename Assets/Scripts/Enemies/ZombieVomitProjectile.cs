using UnityEngine;

namespace Enemies
{
    public class ZombieVomitProjectile : MonoBehaviour
    {
        private Vector3 targetPosition;
        private Vector3 launchDirection;
        private float damage;
        public float speed = 10f;
        public float gravity = -9.81f;  // Simple gravity to simulate arc motion
        private float timeAlive = 0f;

        public GameObject vomitPoolPrefab; // Prefab for the vomit pool
        public float poolDuration = 5f; // Duration for which the vomit pool will stay

        public void Initialize(Vector3 target, float dmg)
        {
            targetPosition = target;
            damage = dmg;
            // Calculate the launch direction towards the target
            launchDirection = (targetPosition - transform.position).normalized;
        }

        void Update()
        {
            timeAlive += Time.deltaTime;
            float timeInAir = timeAlive;

            // Calculate the position of the projectile based on an arc trajectory
            float x = launchDirection.x * speed * timeInAir;
            float y = launchDirection.y * speed * timeInAir + (0.5f * gravity * Mathf.Pow(timeInAir, 2));
            float z = launchDirection.z * speed * timeInAir;

            // Update the projectile position
            transform.position = new Vector3(x, y, z);

            // Check if the projectile has hit the ground
            if (transform.position.y <= 0f)  // Assuming the ground is at y = 0
            {
                CreateVomitPool();
                Destroy(gameObject);  // Destroy the projectile after it hits the ground
            }
        }

        void CreateVomitPool()
        {
            // Instantiate the vomit pool at the projectile's position
            GameObject vomitPool = Instantiate(vomitPoolPrefab, transform.position, Quaternion.identity);
            Destroy(vomitPool, poolDuration); // Destroy the pool after a set duration

            // Optionally, you can add a script to handle damage to the player when they enter the pool
            VomitPool poolScript = vomitPool.GetComponent<VomitPool>();
            if (poolScript != null)
            {
                poolScript.SetDamage(damage);  // Set the damage for the vomit pool
            }
        }
    }
}
