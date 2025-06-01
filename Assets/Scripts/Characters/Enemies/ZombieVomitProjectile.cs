using UnityEngine;

namespace Enemies
{
    public class ZombieVomitProjectile : MonoBehaviour
    {
        private Vector3 initialPosition;
        private Vector3 targetPosition;
        private Vector3 launchDirection;
        private float damage;
        [SerializeField] private float speed = 10f;
        [SerializeField] private float gravity = -9.81f;  // Simple gravity to simulate arc motion
        private float timeAlive = 0f;
        private bool hasHit = false;

        [SerializeField] public GameObject vomitPoolPrefab; // Made public to allow setting from RangedZombie
        [SerializeField] private float poolDuration = 5f; // Duration for which the vomit pool will stay
        [SerializeField] private float maxLifetime = 10f; // Maximum time before projectile is destroyed
        [SerializeField] private float maxHeight = 5f; // Maximum height of the arc

        public void Initialize(Vector3 direction, float dmg)
        {
            initialPosition = transform.position;
            launchDirection = direction;
            damage = dmg;
            timeAlive = 0f;
            hasHit = false;

            // Calculate initial velocity for arc
            float horizontalDistance = Vector3.Distance(
                new Vector3(initialPosition.x, 0, initialPosition.z),
                new Vector3(initialPosition.x + direction.x * 10f, 0, initialPosition.z + direction.z * 10f)
            );

            // Calculate initial vertical velocity to reach maxHeight
            float initialVerticalVelocity = Mathf.Sqrt(2f * -gravity * maxHeight);
            
            // Set the launch direction with proper vertical component
            launchDirection.y = initialVerticalVelocity / speed;
            launchDirection = launchDirection.normalized;
        }

        void Update()
        {
            if (hasHit) return;

            timeAlive += Time.deltaTime;

            // Destroy if exceeded max lifetime
            if (timeAlive >= maxLifetime)
            {
                CreateVomitPool();
                Destroy(gameObject);
                return;
            }

            // Calculate new position using projectile motion equations
            float horizontalSpeed = speed * Mathf.Sqrt(1 - (launchDirection.y * launchDirection.y));
            Vector3 horizontalMovement = new Vector3(launchDirection.x, 0, launchDirection.z) * horizontalSpeed * timeAlive;
            float verticalMovement = (launchDirection.y * speed * timeAlive) + (0.5f * gravity * timeAlive * timeAlive);

            // Update position
            transform.position = initialPosition + horizontalMovement + Vector3.up * verticalMovement;

            // Check if the projectile has hit the ground
            if (transform.position.y <= 0.1f)
            {
                CreateVomitPool();
                Destroy(gameObject);
            }
        }

        void CreateVomitPool()
        {
            if (hasHit) return;
            hasHit = true;

            if (vomitPoolPrefab == null)
            {
                Debug.LogError("Vomit pool prefab is not assigned to ZombieVomitProjectile on " + gameObject.name);
                return;
            }

            // Instantiate the vomit pool at the projectile's position
            GameObject vomitPool = Instantiate(vomitPoolPrefab, transform.position, Quaternion.identity);
            
            // Set up the vomit pool
            VomitPool poolScript = vomitPool.GetComponent<VomitPool>();
            if (poolScript != null)
            {
                poolScript.SetDamage(damage);
            }
            else
            {
                Debug.LogError("VomitPool component not found on vomit pool prefab");
            }

            // Destroy the pool after duration
            Destroy(vomitPool, poolDuration);
        }

        // Optional: Visualize the projectile path in editor
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.2f);
        }
    }
}
