using UnityEngine;
using Managers;

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

        [SerializeField] private float maxLifetime = 10f; // Maximum time before projectile is destroyed
        [SerializeField] private float maxHeight = 5f; // Maximum height of the arc
        [SerializeField] private EffectDefinition vomitPoolEffect;

        public void Initialize(Vector3 direction, float dmg, EffectDefinition poolEffect)
        {
            initialPosition = transform.position;
            launchDirection = direction;
            damage = dmg;
            vomitPoolEffect = poolEffect;
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

            Debug.Log("Creating vomit pool");
            if (vomitPoolEffect == null)
            {
                Debug.LogError("Vomit pool effect is not assigned to ZombieVomitProjectile on " + gameObject.name);
                return;
            }

            // Play the vomit pool effect and get the spawned GameObject
            GameObject poolObj = EffectManager.Instance.PlayEffect(
                transform.position,
                Vector3.up,
                Quaternion.identity,
                null,
                vomitPoolEffect,
                5.0f
            );

            // Initialize the vomit pool
            if (poolObj != null)
            {
                ZombieVomitPool pool = poolObj.GetComponent<ZombieVomitPool>();
                if (pool == null)
                {
                    pool = poolObj.AddComponent<ZombieVomitPool>();
                }
                pool.Setup(damage, 0.5f, new Vector3(0.7f, 0.4f, 0.7f));
            }
        }

        // Optional: Visualize the projectile path in editor
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.2f);
        }
    }
}
